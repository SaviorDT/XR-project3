using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
public class TutorialFlowController : MonoBehaviour
{
    public enum TaskType
    {
        ReachPosition,
        FaceTarget,
        ReachAndFace,
        WaitTrigger,
        WaitSeconds
    }

    [System.Serializable]
    public class TutorialStep
    {
        [TextArea(2, 5)]
        public string instructionTextEN;
        [TextArea(2, 5)]
        public string instructionTextCN;

        public TaskType taskType;

        [Header("位置任務")]
        public Transform targetPosition;
        public float reachDistance = 2f;

        [Header("面向任務")]
        public Transform faceTarget;
        public float faceAngleThreshold = 15f;

        [Header("等待任務")]
        public float waitTime = 2f;

        [Header("本階段要升起的地形")]
        public List<RisingTerrain> terrainsToRise;

        [Header("本階段要隱藏的地形")]
        public List<RisingTerrain> terrainsToHide;


    }

    [Header("玩家")]
    public Transform player;

    [Header("角色朝向來源")]
    public Transform characterForwardRoot;

    [Header("UI")]
    public TMP_Text tutorialTextEN;
    public TMP_Text tutorialTextCN;
    public bool chinese;

    [Header("教學步驟")]
    public List<TutorialStep> steps = new List<TutorialStep>();

    [Header("設定")]
    public bool autoStart = true;
    public bool hideTextWhenFinished = true;

    [Header("地形啟用設定")]
    public bool disableTerrainBeforeRise = true;

    private HashSet<RisingTerrain> initializedDisabledTerrains = new HashSet<RisingTerrain>();

    [Header("目標光柱")]
    public GameObject targetBeamPrefab;
    public float beamYOffset = 0f;
    public Vector3 beamScale = new Vector3(2f, 2f, 2f);

    private Dictionary<Transform, GameObject> targetBeams = new Dictionary<Transform, GameObject>();

    [Header("文字淡入淡出設定")]
    public Transform curvedUIRoot;
    public float fadeOutDuration = 0.25f;
    public float fadeInDuration = 0.35f;

    private CanvasGroup[] uiCanvasGroups;
    private Coroutine fadeCoroutine;
    private bool isFading = false;

    private int currentStepIndex = -1;
    private float stepTimer = 0f;
    private bool tutorialFinished = false;

    [Header("完成後切換場景")]
    public bool loadSceneWhenFinished = true;
    public string nextSceneName;
    public float loadSceneDelay = 1f;
    void Start()
    {
        CollectCanvasGroups();

        if (disableTerrainBeforeRise)
        {
            DisableTerrainsBeforeRise();
        }

        if (autoStart)
        {
            StartTutorial();
        }
    }

    void Update()
    {
        if (tutorialFinished) return;
        if (isFading) return;
        if (currentStepIndex < 0 || currentStepIndex >= steps.Count) return;

        TutorialStep step = steps[currentStepIndex];

        if (IsStepCompleted(step))
        {
            ClearTargetBeam(step);
            NextStep();
        }
    }
    private void DisableTerrainsBeforeRise()
    {
        foreach (TutorialStep step in steps)
        {
            foreach (RisingTerrain terrain in step.terrainsToRise)
            {
                if (terrain == null)
                    continue;

                if (initializedDisabledTerrains.Contains(terrain))
                    continue;

                initializedDisabledTerrains.Add(terrain);
                terrain.gameObject.SetActive(false);
            }
        }
    }

    public void StartTutorial()
    {
        tutorialFinished = false;
        currentStepIndex = -1;
        NextStep();
    }

    private void NextStep()
    {
        currentStepIndex++;
        stepTimer = 0f;

        if (currentStepIndex >= steps.Count)
        {
            FinishTutorial();
            return;
        }

        TutorialStep step = steps[currentStepIndex];
        SpawnTargetBeam(step);
        if (chinese)
        {
            if (tutorialTextCN != null)
            {
                tutorialTextCN.gameObject.SetActive(true);

                if (fadeCoroutine != null)
                {
                    StopCoroutine(fadeCoroutine);
                }

                fadeCoroutine = StartCoroutine(ChangeTextWithFade(step.instructionTextCN));
            }
        }
        else
        {
            if (tutorialTextEN != null)
            {
                tutorialTextEN.gameObject.SetActive(true);

                if (fadeCoroutine != null)
                {
                    StopCoroutine(fadeCoroutine);
                }

                fadeCoroutine = StartCoroutine(ChangeTextWithFade(step.instructionTextEN));
            }
        }
        foreach (RisingTerrain terrain in step.terrainsToRise)
        {
            if (terrain != null)
            {
                terrain.gameObject.SetActive(true);
                StartCoroutine(RiseTerrainNextFrame(terrain));
            }
        }
        foreach (RisingTerrain terrain in step.terrainsToHide)
        {
            if (terrain != null)
            {
                StartCoroutine(HideAndDisableTerrain(terrain));
            }
        }
    }
    private IEnumerator RiseTerrainNextFrame(RisingTerrain terrain)
    {
        yield return null;

        if (terrain != null)
        {
            terrain.Rise();
        }
    }
    private IEnumerator HideAndDisableTerrain(RisingTerrain terrain)
    {
        terrain.Hide();

        yield return new WaitForSeconds(terrain.moveDuration);

        if (terrain != null)
        {
            terrain.gameObject.SetActive(false);
        }
    }
    private void SpawnTargetBeam(TutorialStep step)
    {
        if (targetBeamPrefab == null)
            return;

        Transform target = null;

        if (step.taskType == TaskType.ReachPosition || step.taskType == TaskType.ReachAndFace)
        {
            target = step.targetPosition;
        }
        else if (step.taskType == TaskType.FaceTarget)
        {
            target = step.faceTarget;
        }

        if (target == null)
            return;

        if (targetBeams.ContainsKey(target))
            return;

        Vector3 spawnPosition = target.position + Vector3.up * beamYOffset;

        GameObject beam = Instantiate(
            targetBeamPrefab,
            spawnPosition,
            Quaternion.identity
        );

        beam.transform.localScale = beamScale;

        targetBeams.Add(target, beam);
    }

    private void ClearTargetBeam(TutorialStep step)
    {
        Transform target = null;

        if (step.taskType == TaskType.ReachPosition || step.taskType == TaskType.ReachAndFace)
        {
            target = step.targetPosition;
        }
        else if (step.taskType == TaskType.FaceTarget)
        {
            target = step.faceTarget;
        }

        if (target == null)
            return;

        if (targetBeams.TryGetValue(target, out GameObject beam))
        {
            if (beam != null)
                Destroy(beam);

            targetBeams.Remove(target);
        }
    }
    private bool IsStepCompleted(TutorialStep step)
    {
        switch (step.taskType)
        {
            case TaskType.ReachPosition:
                return CheckReachPosition(step);

            case TaskType.FaceTarget:
                return CheckFaceTarget(step);

            case TaskType.ReachAndFace:
                return CheckReachPosition(step) && CheckFaceTarget(step);

            case TaskType.WaitSeconds:
                stepTimer += Time.deltaTime;
                return stepTimer >= step.waitTime;

            case TaskType.WaitTrigger:
                return CheckTriggerPressed(step);
        }

        return false;
    }

    private bool CheckTriggerPressed(TutorialStep step)
    {
        return OVRInput.GetDown(
            OVRInput.Button.PrimaryIndexTrigger,
            OVRInput.Controller.RTouch
        )
        ||
        OVRInput.GetDown(
            OVRInput.Button.PrimaryIndexTrigger,
            OVRInput.Controller.LTouch
        );
    }
    private bool CheckReachPosition(TutorialStep step)
    {
        if (player == null || step.targetPosition == null) return false;

        float distance = Vector3.Distance(player.position, step.targetPosition.position);
        return distance <= step.reachDistance;
    }

    private bool CheckFaceTarget(TutorialStep step)
    {
        if (characterForwardRoot == null || step.faceTarget == null) return false;

        Vector3 toTarget = step.faceTarget.position - characterForwardRoot.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude < 0.001f) return true;

        Vector3 forward = characterForwardRoot.forward;
        forward.y = 0f;

        float angle = Vector3.Angle(forward.normalized, toTarget.normalized);
        return angle <= step.faceAngleThreshold;
    }

    private void FinishTutorial()
    {
        tutorialFinished = true;
        if (chinese)
        {
            if (tutorialTextCN != null)
            {
                tutorialTextCN.text = "教學完成！";

                if (hideTextWhenFinished)
                {
                    tutorialTextCN.gameObject.SetActive(false);
                }
            }
        }
        else
        {
            if (tutorialTextEN != null)
            {
                tutorialTextEN.text = "Tutorial End！";

                if (hideTextWhenFinished)
                {
                    tutorialTextEN.gameObject.SetActive(false);
                }
            }
        }
        if (loadSceneWhenFinished)
        {
            StartCoroutine(LoadNextSceneAfterDelay());
        }
    }
    private IEnumerator LoadNextSceneAfterDelay()
    {
        yield return new WaitForSeconds(loadSceneDelay);

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
    private IEnumerator ChangeTextWithFade(string newText)
    {
        isFading = true;

        yield return FadeAllCanvasGroups(1f, 0f, fadeOutDuration);
        if (chinese)
        {
            tutorialTextCN.text = newText;
        }
        else
        {
            tutorialTextEN.text = newText;
        }


        yield return FadeAllCanvasGroups(0f, 1f, fadeInDuration);

        isFading = false;
    }


    private void CollectCanvasGroups()
    {
        Canvas[] canvases = curvedUIRoot.GetComponentsInChildren<Canvas>(true);
        uiCanvasGroups = new CanvasGroup[canvases.Length];

        for (int i = 0; i < canvases.Length; i++)
        {
            CanvasGroup group = canvases[i].GetComponent<CanvasGroup>();

            if (group == null)
            {
                group = canvases[i].gameObject.AddComponent<CanvasGroup>();
            }

            uiCanvasGroups[i] = group;
        }
    }
    private IEnumerator FadeAllCanvasGroups(float from, float to, float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            float alpha = Mathf.Lerp(from, to, t);

            foreach (CanvasGroup group in uiCanvasGroups)
            {
                if (group != null)
                {
                    group.alpha = alpha;
                }
            }

            yield return null;
        }

        foreach (CanvasGroup group in uiCanvasGroups)
        {
            if (group != null)
            {
                group.alpha = to;
            }
        }
    }
}