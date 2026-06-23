using UnityEngine;
using System.Collections;
public class StatusSlerpMover : MonoBehaviour
{
    public enum MoveMode
    {
        Position,
        Rotation,
        PositionAndRotation
    }

    [System.Serializable]
    public class StatusTarget
    {
        public int status;
        public Vector3 position;
        public Vector3 eulerAngle;
    }

    [Header("Status 來源")]
    public TriggerStatusChanger statusSource;

    [Header("Status 對應座標 / 角度")]
    public StatusTarget[] statusTargets;

    [Header("移動模式")]
    public MoveMode moveMode = MoveMode.PositionAndRotation;

    [Header("Slerp 速度")]
    public float positionSlerpSpeed = 2f;
    public float rotationSlerpSpeed = 2f;

    [Header("距離很近時直接貼齊")]
    public float positionSnapDistance = 0.01f;
    public float rotationSnapAngle = 0.5f;

    [Header("Scene Gizmos")]
    public bool showGizmos = true;
    public float gizmoSphereRadius = 0.15f;
    public float gizmoAxisLength = 0.75f;

    [Header("移動/旋轉時光柱")]
    public GameObject movingBeaconPrefab;
    public Vector3 movingBeaconOffset = Vector3.zero;
    public float movingBeaconFadeDuration = 0.5f;
    public float movingBeaconShowPositionThreshold = 0.05f;
    public float movingBeaconShowRotationThreshold = 1f;

    private GameObject movingBeaconInstance;
    private Renderer[] movingBeaconRenderers;
    private Coroutine movingBeaconFadeCoroutine;
    private bool movingBeaconVisible = false;
    private void Start()
    {
        CreateMovingBeacon();
    }
    private void Update()
    {
        if (statusSource == null)
            return;

        StatusTarget target = GetTargetByStatus(statusSource.status);

        if (target == null)
            return;

        MoveToTarget(target);
        UpdateMovingBeacon(target);
        UpdateMovingBeaconTransform();
        UpdateMovingBeacon(target);
    }
    private void CreateMovingBeacon()
    {
        if (movingBeaconPrefab == null)
            return;

        movingBeaconInstance = Instantiate(
            movingBeaconPrefab,
            transform.position + movingBeaconOffset,
            Quaternion.identity
        );

        movingBeaconInstance.transform.SetParent(null);

        movingBeaconInstance.transform.position = transform.position + movingBeaconOffset;
        movingBeaconInstance.transform.rotation = Quaternion.identity;

        movingBeaconRenderers =
            movingBeaconInstance.GetComponentsInChildren<Renderer>();

        SetMovingBeaconAlpha(0f);
        movingBeaconInstance.SetActive(true);
    }
    private void UpdateMovingBeaconTransform()
    {
        if (movingBeaconInstance == null)
            return;

        movingBeaconInstance.transform.position =
            transform.position + movingBeaconOffset;

        // 永遠朝世界座標天空方向，不跟著物件轉
        movingBeaconInstance.transform.rotation =
            Quaternion.identity;
    }
    private void UpdateMovingBeacon(StatusTarget target)
    {
        if (movingBeaconInstance == null)
            return;

        bool positionMoving =
            moveMode == MoveMode.Position ||
            moveMode == MoveMode.PositionAndRotation;

        bool rotationMoving =
            moveMode == MoveMode.Rotation ||
            moveMode == MoveMode.PositionAndRotation;

        bool isPositionNotArrived =
            positionMoving &&
            Vector3.Distance(transform.position, target.position)
                > movingBeaconShowPositionThreshold;

        bool isRotationNotArrived =
            rotationMoving &&
            Quaternion.Angle(
                transform.rotation,
                Quaternion.Euler(target.eulerAngle))
                > movingBeaconShowRotationThreshold;

        bool shouldShow =
            isPositionNotArrived || isRotationNotArrived;

        if (shouldShow == movingBeaconVisible)
            return;

        movingBeaconVisible = shouldShow;

        if (movingBeaconFadeCoroutine != null)
            StopCoroutine(movingBeaconFadeCoroutine);

        movingBeaconFadeCoroutine =
            StartCoroutine(FadeMovingBeacon(
                shouldShow ? 1f : 0f
            ));
    }

    private IEnumerator FadeMovingBeacon(float targetAlpha)
    {
        float startAlpha = GetMovingBeaconAlpha();
        float timer = 0f;

        while (timer < movingBeaconFadeDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(
                timer / movingBeaconFadeDuration);

            float alpha = Mathf.Lerp(
                startAlpha,
                targetAlpha,
                t);

            SetMovingBeaconAlpha(alpha);

            yield return null;
        }

        SetMovingBeaconAlpha(targetAlpha);
    }

    private float GetMovingBeaconAlpha()
    {
        if (movingBeaconRenderers == null ||
            movingBeaconRenderers.Length == 0)
            return 0f;

        return movingBeaconRenderers[0].material.color.a;
    }

    private void SetMovingBeaconAlpha(float alpha)
    {
        if (movingBeaconRenderers == null)
            return;

        foreach (Renderer renderer in movingBeaconRenderers)
        {
            if (renderer == null)
                continue;

            foreach (Material mat in renderer.materials)
            {
                Color color = mat.color;
                color.a = alpha;
                mat.color = color;
            }
        }
    }
    private void MoveToTarget(StatusTarget target)
    {
        if (moveMode == MoveMode.Position ||
            moveMode == MoveMode.PositionAndRotation)
        {
            Vector3 targetPosition = target.position;

            transform.position = Vector3.Slerp(
                transform.position,
                targetPosition,
                Time.deltaTime * positionSlerpSpeed
            );

            if (Vector3.Distance(transform.position, targetPosition)
                <= positionSnapDistance)
            {
                transform.position = targetPosition;
            }
        }

        if (moveMode == MoveMode.Rotation ||
            moveMode == MoveMode.PositionAndRotation)
        {
            Quaternion targetRotation =
                Quaternion.Euler(target.eulerAngle);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * rotationSlerpSpeed
            );

            if (Quaternion.Angle(transform.rotation, targetRotation)
                <= rotationSnapAngle)
            {
                transform.rotation = targetRotation;
            }
        }
    }

    private StatusTarget GetTargetByStatus(int currentStatus)
    {
        if (statusTargets == null)
            return null;

        foreach (StatusTarget target in statusTargets)
        {
            if (target.status == currentStatus)
                return target;
        }

        return null;
    }

    public void SnapToStatus(int targetStatus)
    {
        StatusTarget target = GetTargetByStatus(targetStatus);

        if (target == null)
            return;

        transform.position = target.position;
        transform.rotation = Quaternion.Euler(target.eulerAngle);
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos)
            return;

        if (statusTargets == null)
            return;

        foreach (StatusTarget target in statusTargets)
        {
            DrawTargetGizmo(target);
        }
    }

    private void DrawTargetGizmo(StatusTarget target)
    {
        Quaternion rotation = Quaternion.Euler(target.eulerAngle);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            target.position,
            gizmoSphereRadius
        );

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(
            target.position,
            target.position + rotation * Vector3.forward * gizmoAxisLength
        );

        Gizmos.color = Color.green;
        Gizmos.DrawLine(
            target.position,
            target.position + rotation * Vector3.up * gizmoAxisLength
        );

        Gizmos.color = Color.red;
        Gizmos.DrawLine(
            target.position,
            target.position + rotation * Vector3.right * gizmoAxisLength
        );
    }
}