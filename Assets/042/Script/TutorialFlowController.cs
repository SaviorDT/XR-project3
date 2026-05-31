using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Collections;
public class TutorialFlowController : MonoBehaviour
{
    public enum TaskType
    {
        ReachPosition,
        FaceTarget,
        ReachAndFace,
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

    void Start()
    {
        CollectCanvasGroups();

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
            NextStep();
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
                    terrain.Rise();
                }
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
        }

        return false;
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