using UnityEngine;
using System.Collections;

public class PotionFlight : MonoBehaviour
{
    [Header("錨點")]
    public Transform startPoint;
    public Transform endPoint;

    [Header("時間參數(秒)")]
    public float startDelay = 1f;
    public float holdAtStart = 2f;
    public float flyDuration = 4f;
    public float holdAtEnd = 1f;

    [Header("大小(飛行中漸變)")]
    public float startScale = 1.5f;
    public float endScale = 0.5f;

    [Header("吃藥銜接")]
    public bool triggerEat = false;
    public Animator dragonAnimator;
    public string eatTrigger = "ToEat";
    [Range(0f, 1f)]
    public float eatTriggerAtProgress = 0.6f;  // 飛行進度到此%時觸發Eat(讓龍提前張嘴)

    void Start()
    {
        gameObject.SetActive(true);
        StartCoroutine(Sequence());
    }

    IEnumerator Sequence()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        // 階段0:隱藏
        SetVisible(renderers, false);
        yield return new WaitForSeconds(startDelay);

        // 階段1:出現,停起點
        transform.position = startPoint.position;
        transform.localScale = Vector3.one * startScale;
        SetVisible(renderers, true);
        yield return new WaitForSeconds(holdAtStart);

        // 階段2:飛行 + 放大,途中觸發Eat
        bool eatTriggered = false;
        float t = 0f;
        while (t < flyDuration)
        {
            t += Time.deltaTime;
            float p = t / flyDuration;
            transform.position = Vector3.Lerp(startPoint.position, endPoint.position, p);
            transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, p);

            // 飛到指定進度時,觸發龍張嘴(只觸發一次)
            if (triggerEat && !eatTriggered && p >= eatTriggerAtProgress)
            {
                if (dragonAnimator != null)
                    dragonAnimator.SetTrigger(eatTrigger);
                EndingDirector director = FindObjectOfType<EndingDirector>();
                if (director != null) director.StartAfterEat(); 
                eatTriggered = true;
            }
            yield return null;
        }
        transform.position = endPoint.position;
        transform.localScale = Vector3.one * endScale;

        // 階段3:到終點停留
        yield return new WaitForSeconds(holdAtEnd);

        // 階段4:隱藏
        SetVisible(renderers, false);
    }

    void SetVisible(Renderer[] renderers, bool visible)
    {
        foreach (Renderer r in renderers)
            r.enabled = visible;
    }
}