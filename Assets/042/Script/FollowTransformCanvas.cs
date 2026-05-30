using UnityEngine;

public class FollowTransformCanvas : MonoBehaviour
{
    public Transform target;

    public float distance = 2.0f;
    public float heightOffset = 0.0f;
    public Vector3 localOffset = Vector3.zero;

    public bool followYawOnly = true;
    public bool faceTarget = true;

    public bool smoothFollow = true;
    public float positionSmooth = 10f;
    public float rotationSmooth = 10f;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 forward = followYawOnly
            ? Vector3.ProjectOnPlane(target.forward, Vector3.up).normalized
            : target.forward;

        if (forward.sqrMagnitude < 0.001f)
            forward = target.forward;

        Vector3 targetPos =
            target.position +
            forward * distance +
            Vector3.up * heightOffset +
            target.TransformDirection(localOffset);

        Quaternion targetRot;

        if (faceTarget)
        {
            Vector3 dir = transform.position - target.position;

            if (followYawOnly)
                dir.y = 0f;

            if (dir.sqrMagnitude < 0.001f)
                dir = forward;

            targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }
        else
        {
            targetRot = followYawOnly
                ? Quaternion.Euler(0f, target.eulerAngles.y, 0f)
                : target.rotation;
        }

        if (smoothFollow)
        {
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * positionSmooth);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSmooth);
        }
        else
        {
            transform.position = targetPos;
            transform.rotation = targetRot;
        }
    }
}