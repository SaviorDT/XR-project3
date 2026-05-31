using UnityEngine;

public class RisingTerrain : MonoBehaviour
{
    public enum TerrainState
    {
        Hidden,
        Shown,
        Moving
    }

    [Header("位置設定")]
    public Vector3 hiddenPositionOffset = new Vector3(0f, -10f, 0f);

    [Header("動畫設定")]
    public float moveDuration = 2f;
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("啟動設定")]
    public bool startHidden = true;

    private Vector3 shownLocalPosition;
    private Vector3 hiddenLocalPosition;

    private Vector3 moveStartPosition;
    private Vector3 moveTargetPosition;

    private float timer = 0f;
    private TerrainState state;

    void Start()
    {
        shownLocalPosition = transform.localPosition;
        hiddenLocalPosition = shownLocalPosition + hiddenPositionOffset;

        if (startHidden)
        {
            transform.localPosition = hiddenLocalPosition;
            state = TerrainState.Hidden;
        }
        else
        {
            transform.localPosition = shownLocalPosition;
            state = TerrainState.Shown;
        }
    }

    void Update()
    {
        if (state != TerrainState.Moving) return;

        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / moveDuration);
        float curvedT = moveCurve.Evaluate(t);

        transform.localPosition = Vector3.Lerp(moveStartPosition, moveTargetPosition, curvedT);

        if (t >= 1f)
        {
            transform.localPosition = moveTargetPosition;

            if (Vector3.Distance(moveTargetPosition, shownLocalPosition) < 0.01f)
            {
                state = TerrainState.Shown;
            }
            else
            {
                state = TerrainState.Hidden;
            }
        }
    }

    public void Rise()
    {
        MoveTo(shownLocalPosition);
    }

    public void Hide()
    {
        MoveTo(hiddenLocalPosition);
    }

    public void Toggle()
    {
        if (state == TerrainState.Shown)
        {
            Hide();
        }
        else
        {
            Rise();
        }
    }

    private void MoveTo(Vector3 targetPosition)
    {
        moveStartPosition = transform.localPosition;
        moveTargetPosition = targetPosition;
        timer = 0f;
        state = TerrainState.Moving;
    }
}