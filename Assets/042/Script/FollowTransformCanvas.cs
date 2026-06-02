using UnityEngine;

public class FollowTransformCanvas : MonoBehaviour
{
    public Transform target;

    public float distance = 1.0f;
    public float heightOffset = 0.0f;
    public Vector3 localOffset = Vector3.zero;

    public bool followYawOnly = true;
    public bool faceTarget = true;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 forward = followYawOnly
            ? Vector3.ProjectOnPlane(target.forward, Vector3.up).normalized
            : target.forward;

        if (forward.sqrMagnitude < 0.001f)
            forward = target.forward;

        transform.position =
            target.position +
            forward * distance +
            Vector3.up * heightOffset +
            target.TransformDirection(localOffset);

        if (faceTarget)
        {
            Vector3 dir = transform.position - target.position;

            if (followYawOnly)
                dir.y = 0f;

            if (dir.sqrMagnitude < 0.001f)
                dir = forward;

            transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }
        else
        {
            transform.rotation = followYawOnly
                ? Quaternion.Euler(0f, target.eulerAngles.y, 0f)
                : target.rotation;
        }
    }
}