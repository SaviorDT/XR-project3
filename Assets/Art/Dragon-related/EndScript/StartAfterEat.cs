using UnityEngine;
using System.Collections;

public class EndingDirector : MonoBehaviour
{
    [Header("龍的 Animator")]
    public Animator dragonAnimator;

    [Header("Healing 特效")]
    public ParticleSystem healingPS;

    [Header("觸發起點:Eat 開始後多久算 Eat 播完")]
    public float eatDuration = 1.625f;

    [Header("各段時間參數(秒)")]
    public float waitAfterEat = 2f;
    public float healingDuration = 3f;
    public float waitAfterHealing = 2f;
    public float yesDuration = 2.5f;     // Yes 播多久(你設2.5=2次)
    public float jumpDuration = 4f;      // Jump 播多久(你設4=4次)
    public float restWaitDuration = 10f;   // Rest 凍住休息的時間(預留文字用,可調或設0)

    [Header("Trigger 名稱")]
    public string idleTrigger = "ToIdle";   // 新增:Eat後填空的idle
    public string yesTrigger = "ToYes";
    public string jumpTrigger = "ToJump";
    public string restTrigger = "ToRest";   // 新增:Jump播完主動切Rest
    public string blowTrigger = "ToBlow";

    [Header("Fade 漸黑")]
    public CanvasGroup fadeCanvasGroup;   // 黑幕的 CanvasGroup
    public float fadeDuration = 3f;       // 漸黑秒數(對齊程式組推玩家的時間)
    [Header("第4階段:五鏡頭序列")]
    public CameraSequence cameraSequence;

    public void StartAfterEat()
    {
        StartCoroutine(Sequence());
    }

    IEnumerator Sequence()
    {
        // 等 Eat 動畫播完
        yield return new WaitForSeconds(eatDuration);

        // ★新增:Eat完先切到idle loop填空(整個等待+特效期間龍不會僵住)
        dragonAnimator.SetTrigger(idleTrigger);

        // Eat 完,靜待
        yield return new WaitForSeconds(waitAfterEat);

        // Healing 特效:順進
        if (healingPS != null) healingPS.Play();
        yield return new WaitForSeconds(healingDuration);
        if (healingPS != null) healingPS.Stop();

        // 特效後靜待
        yield return new WaitForSeconds(waitAfterHealing);

        // 切 Yes,播一段
        dragonAnimator.SetTrigger(yesTrigger);
        yield return new WaitForSeconds(yesDuration);

        // 切 Jump,播一段
        dragonAnimator.SetTrigger(jumpTrigger);
        yield return new WaitForSeconds(jumpDuration);

        // ★新增:Jump播滿後,主動切到Rest(不靠Has Exit Time)
        dragonAnimator.SetTrigger(restTrigger);

        // ★新增:Rest 休息等待(預留文字/謝謝的時間,可設0)
        yield return new WaitForSeconds(restWaitDuration);

        // ★新增:休息結束,吹氣
        dragonAnimator.SetTrigger(blowTrigger);
        yield return StartCoroutine(FadeToBlack(fadeDuration));  // 等它確實全黑
        if (cameraSequence != null) cameraSequence.StartSequence();
    }

    IEnumerator FadeToBlack(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / duration);
            if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = alpha;
            yield return null;
        }
        if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = 1f; // 確保完全黑
    }
}