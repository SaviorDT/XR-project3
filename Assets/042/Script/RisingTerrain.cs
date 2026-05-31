using UnityEngine;

public class RisingTerrain : MonoBehaviour
{
    [Header("升起位置設定")]
    public Vector3 hiddenPositionOffset = new Vector3(0f, -10f, 0f);
    public Vector3 shownLocalPosition;

    [Header("動畫設定")]
    public float riseDuration = 2f;
    public AnimationCurve riseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("啟動設定")]
    public bool startHidden = true;

    private Vector3 hiddenLocalPosition;
    private float timer = 0f;
    private bool isRising = false;
    private bool hasRisen = false;

    void Start()
    {
        hiddenLocalPosition = shownLocalPosition + hiddenPositionOffset;

        if (startHidden)
        {
            transform.localPosition = hiddenLocalPosition;
        }
        else
        {
            transform.localPosition = shownLocalPosition;
            hasRisen = true;
        }
    }

    void Update()
    {
        if (!isRising) return;

        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / riseDuration);
        float curvedT = riseCurve.Evaluate(t);

        transform.localPosition = Vector3.Lerp(hiddenLocalPosition, shownLocalPosition, curvedT);

        if (t >= 1f)
        {
            isRising = false;
            hasRisen = true;
            transform.localPosition = shownLocalPosition;
        }
    }

    public void Rise()
    {
        if (hasRisen) return;

        timer = 0f;
        isRising = true;
    }

    public void ResetTerrain()
    {
        timer = 0f;
        isRising = false;
        hasRisen = false;
        transform.localPosition = hiddenLocalPosition;
    }
}