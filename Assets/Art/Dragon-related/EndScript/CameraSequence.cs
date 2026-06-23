using UnityEngine;
using System.Collections;

public class CameraSequence : MonoBehaviour
{
    [System.Serializable]
    public class Shot
    {
        public string name;
        public Transform startPoint;
        public Transform endPoint;
        public float moveDuration = 3f;
        public float holdAtStart = 0f;
        public GameObject sceneGroup;
        public EnvironmentProfile environment;

        [Header("這個鏡頭的故事文字(留空=不顯示)")]
        [TextArea] public string storyText;
        public float textHoldDuration = 3f;   // 文字停留多久
    }

    [Header("要移動的玩家(最外層 Player)")]
    public Transform player;

    [Header("結局時要停用的玩家移動元件")]
    public Rigidbody playerRigidbody;
    public MonoBehaviour[] scriptsToDisable;

    [Header("Fade 黑幕")]
    public MeshRenderer fadeMesh;
    public float fadeDuration = 2f;

    [Header("字幕系統")]
    public EndingSubtitle subtitle;

    [Header("所有假景組(切換時除了當前鏡頭的,其餘都關)")]
    public GameObject[] allSceneGroups;

    [Header("鏡頭清單(依序播放)")]
    public Shot[] shots;

    [Header("全部播完後是否停在全黑")]
    public bool stayBlackAtEnd = true;

    [Header("全部播完後是否恢復玩家控制")]
    public bool restorePlayerAtEnd = false;

    [Header("每個鏡頭漸亮前的全黑停留(秒)")]
    public float holdBlackBeforeFadeIn = 0f;

    [Header("移動到幾%時開始漸黑(0~1)")]
    public float fadeOutStartProgress = 0.7f;

    public void StartSequence()
    {
        StopAllCoroutines();
        StartCoroutine(RunShots());
    }

    IEnumerator RunShots()
    {
        FreezePlayer(true);
        SetMeshAlpha(1f);

        foreach (Shot shot in shots)
        {
            // 1. 全黑下切假景組
            SwitchSceneGroup(shot.sceneGroup);

            // 2. 套用環境
            if (shot.environment != null) shot.environment.Apply();

            // 3. 瞬移玩家到起點
            if (shot.startPoint != null)
            {
                player.position = shot.startPoint.position;
                player.rotation = shot.startPoint.rotation;
            }

            // 3.5 全黑停留
            if (holdBlackBeforeFadeIn > 0f)
                yield return new WaitForSeconds(holdBlackBeforeFadeIn);

            // 4. 漸亮
            StartCoroutine(Fade(1f, 0f, fadeDuration));

            // 5. 起點停留
            if (shot.holdAtStart > 0f)
                yield return new WaitForSeconds(shot.holdAtStart);

            // 6. 緩慢移動 + 並行顯示文字
            // 啟動文字(並行,不等它)
            if (subtitle != null && !string.IsNullOrEmpty(shot.storyText))
                StartCoroutine(subtitle.ShowText(shot.storyText, shot.textHoldDuration));

            if (shot.endPoint != null && shot.startPoint != null)
            {
                bool fadeStarted = false;
                float t = 0f;
                Vector3 from = shot.startPoint.position;
                Vector3 to = shot.endPoint.position;
                Quaternion fromRot = shot.startPoint.rotation;
                Quaternion toRot = shot.endPoint.rotation;
                while (t < shot.moveDuration)
                {
                    t += Time.deltaTime;
                    float p = t / shot.moveDuration;
                    player.position = Vector3.Lerp(from, to, p);
                    player.rotation = Quaternion.Slerp(fromRot, toRot, p);

                    if (!fadeStarted && p >= fadeOutStartProgress)
                    {
                        StartCoroutine(Fade(0f, 1f, fadeDuration));
                        fadeStarted = true;
                    }
                    yield return null;
                }
                player.position = to;
                player.rotation = toRot;

                if (!fadeStarted)
                    yield return StartCoroutine(Fade(0f, 1f, fadeDuration));
                else
                    yield return new WaitForSeconds(fadeDuration * (1f - fadeOutStartProgress));
            }
            else
            {
                yield return StartCoroutine(Fade(0f, 1f, fadeDuration));
            }
        }

        // 全部播完 — 先強制全黑
        SetMeshAlpha(1f);

        if (restorePlayerAtEnd)
        {
            yield return new WaitForSeconds(0.5f);
            yield return StartCoroutine(Fade(1f, 0f, fadeDuration));
            FreezePlayer(false);
        }
        else
        {
            SetMeshAlpha(stayBlackAtEnd ? 1f : 0f);
        }
    }

    void FreezePlayer(bool freeze)
    {
        if (scriptsToDisable != null)
            foreach (var s in scriptsToDisable)
                if (s != null) s.enabled = !freeze;

        if (playerRigidbody != null)
        {
            if (freeze)
            {
                playerRigidbody.linearVelocity = Vector3.zero;
                playerRigidbody.angularVelocity = Vector3.zero;
                playerRigidbody.isKinematic = true;
            }
            else
            {
                playerRigidbody.isKinematic = false;
            }
        }
    }

    void SwitchSceneGroup(GameObject active)
    {
        foreach (GameObject g in allSceneGroups)
            if (g != null) g.SetActive(g == active);
    }

    void SetMeshAlpha(float alpha)
    {
        if (fadeMesh == null) return;
        Color c = fadeMesh.material.color;
        c.a = alpha;
        fadeMesh.material.color = c;
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        if (fadeMesh == null) yield break;

        Material mat = fadeMesh.material; // 取得材質實例
        Color startColor = mat.color;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float currentAlpha = Mathf.Lerp(from, to, t / duration);
            mat.color = new Color(startColor.r, startColor.g, startColor.b, currentAlpha);
            yield return null;
        }
        mat.color = new Color(startColor.r, startColor.g, startColor.b, to);
    }
}