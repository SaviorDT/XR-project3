using UnityEngine;
using TMPro;
using System.Collections;

public class EndingSubtitle : MonoBehaviour
{
    [Header("顯示文字的 TextMeshPro(HUD_SourceCanvas 底下的 Text 中文)")]
    public TMP_Text subtitleText;

    [Header("淡入淡出的根(通常拖 HUD_SourceCanvas,抓底下所有 CanvasGroup)")]
    public Transform curvedUIRoot;

    [Header("淡入淡出時間")]
    public float fadeInDuration = 0.5f;
    public float fadeOutDuration = 0.5f;

    private CanvasGroup[] uiCanvasGroups;

    void Awake()
    {
        CollectCanvasGroups();
        SetAlpha(0f);   // 一開始隱藏(透明)
        if (subtitleText != null) subtitleText.gameObject.SetActive(true);
    }

    // 顯示一段文字:淡入 → 停留 → 淡出(整段做完才返回)
    public IEnumerator ShowText(string text, float holdSeconds)
    {
        if (string.IsNullOrEmpty(text)) yield break;   // 沒文字就跳過

        if (subtitleText != null) subtitleText.text = text;
        yield return Fade(0f, 1f, fadeInDuration);     // 淡入
        yield return new WaitForSeconds(holdSeconds);  // 停留
        yield return Fade(1f, 0f, fadeOutDuration);    // 淡出
    }

    // 只淡入(不自動淡出,給「文字要停整個鏡頭」的情況)
    public IEnumerator FadeInText(string text)
    {
        if (string.IsNullOrEmpty(text)) yield break;
        if (subtitleText != null) subtitleText.text = text;
        yield return Fade(0f, 1f, fadeInDuration);
    }

    // 淡出
    public IEnumerator FadeOutText()
    {
        yield return Fade(1f, 0f, fadeOutDuration);
    }

    // 立刻隱藏(不淡出)
    public void HideImmediate()
    {
        SetAlpha(0f);
    }

    void CollectCanvasGroups()
    {
        if (curvedUIRoot == null) { uiCanvasGroups = new CanvasGroup[0]; return; }
        Canvas[] canvases = curvedUIRoot.GetComponentsInChildren<Canvas>(true);
        uiCanvasGroups = new CanvasGroup[canvases.Length];
        for (int i = 0; i < canvases.Length; i++)
        {
            CanvasGroup g = canvases[i].GetComponent<CanvasGroup>();
            if (g == null) g = canvases[i].gameObject.AddComponent<CanvasGroup>();
            uiCanvasGroups[i] = g;
        }
    }

    void SetAlpha(float a)
    {
        if (uiCanvasGroups == null) return;
        foreach (var g in uiCanvasGroups) if (g != null) g.alpha = a;
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            SetAlpha(Mathf.Lerp(from, to, t / duration));
            yield return null;
        }
        SetAlpha(to);
    }
}