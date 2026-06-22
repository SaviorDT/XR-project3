using UnityEngine;

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

    private void Update()
    {
        if (statusSource == null)
            return;

        StatusTarget target = GetTargetByStatus(statusSource.status);

        if (target == null)
            return;

        MoveToTarget(target);
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