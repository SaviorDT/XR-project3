using UnityEngine;

public class PlayerFallRespawn : MonoBehaviour
{
    public enum SafeAreaMode
    {
        XYZBox,
        CircleAndY,
        Sphere
    }

    [Header("�a���P�w")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 1.2f;
    public float groundCheckRadius = 0.3f;

    [Header("�ߨ��I�Y�׭���")]
    public float maxGroundSlopeAngle = 5f;

    [Header("�w���d��Ҧ�")]
    public SafeAreaMode safeAreaMode = SafeAreaMode.XYZBox;

    [Header("XYZ �ߤ���d��")]
    public float minX = -10f;
    public float maxX = 10f;
    public float minY = -2f;
    public float maxY = 5f;
    public float minZ = -10f;
    public float maxZ = 10f;

    [Header("Circle + Y �d��")]
    public Vector2 circleCenterXZ = Vector2.zero;
    public float circleRadius = 10f;
    public float circleMinY = -2f;
    public float circleMaxY = 5f;

    [Header("Sphere �d��")]
    public Vector3 sphereCenter = Vector3.zero;
    public float sphereRadius = 10f;

    [Header("�� / ��ê�ˬd")]
    public LayerMask wallLayer;
    public float wallCheckRadius = 0.35f;
    public float wallCheckDistance = 0.6f;
    public float wallCheckHeight = 0.8f;

    [Header("���ͳ]�w")]
    public float respawnYOffset = 1.5f;
    public bool resetVelocity = true;

    [Header("�ߨ��I�Ŷ��ˬd")]
    public Collider safeStandCollider;
    public LayerMask safeStandBlockLayer;
    public float safeStandCheckYOffset = 0f;

    private Vector3 lastGroundPosition;
    private Quaternion lastGroundRotation;
    private Rigidbody rb;
    private CharacterController characterController;
    public PlayerFlyController flyController;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        characterController = GetComponent<CharacterController>();
        flyController = GetComponent<PlayerFlyController>();
        lastGroundPosition = transform.position;
        lastGroundRotation = transform.rotation;
    }

    void Update()
    {
        UpdateLastGroundPosition();

        if (OVRInput.GetDown(OVRInput.RawButton.A))
        {
            RespawnToLastGround();
            return;
        }

        if (!IsInsideSafeArea(transform.position))
        {
            RespawnToLastGround();
        }
    }

    private void UpdateLastGroundPosition()
    {
        if (!IsOnGround(out RaycastHit hit))
            return;

        float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);

        if (slopeAngle >= maxGroundSlopeAngle)
            return;

        Vector3 candidate = transform.position;
        // candidate.y = hit.point.y;

        if (!IsInsideSafeArea(candidate))
            return;

        if (IsNearWall(candidate))
            return;

        if (!IsSafeStandColliderClear(candidate))
            return;

        lastGroundPosition = candidate;
        lastGroundRotation = transform.rotation;
    }

    private bool IsOnGround(out RaycastHit hit)
    {
        // Vector3 origin = transform.position + Vector3.up * 0.2f;

        // return Physics.SphereCast(
        //     origin,
        //     groundCheckRadius,
        //     Vector3.down,
        //     out hit,
        //     groundCheckDistance,
        //     groundLayer
        // );
        hit = new()
        {
            normal = transform.up
        };
        return flyController.IsGrounded();
    }

    private bool IsInsideSafeArea(Vector3 pos)
    {
        switch (safeAreaMode)
        {
            case SafeAreaMode.XYZBox:
                return pos.x >= minX && pos.x <= maxX &&
                       pos.y >= minY && pos.y <= maxY &&
                       pos.z >= minZ && pos.z <= maxZ;

            case SafeAreaMode.CircleAndY:
                Vector2 p = new Vector2(pos.x, pos.z);
                return Vector2.Distance(p, circleCenterXZ) <= circleRadius &&
                       pos.y >= circleMinY && pos.y <= circleMaxY;

            case SafeAreaMode.Sphere:
                return Vector3.Distance(pos, sphereCenter) <= sphereRadius;
        }

        return true;
    }

    private bool IsNearWall(Vector3 pos)
    {
        Vector3 origin = pos + Vector3.up * wallCheckHeight;

        Vector3[] dirs =
        {
            Vector3.forward,
            Vector3.back,
            Vector3.right,
            Vector3.left,
            (Vector3.forward + Vector3.right).normalized,
            (Vector3.forward + Vector3.left).normalized,
            (Vector3.back + Vector3.right).normalized,
            (Vector3.back + Vector3.left).normalized
        };

        foreach (Vector3 dir in dirs)
        {
            if (Physics.SphereCast(origin, wallCheckRadius, dir, out _, wallCheckDistance, wallLayer))
                return true;
        }

        return false;
    }
    private bool IsSafeStandColliderClear(Vector3 candidatePosition)
    {
        if (safeStandCollider == null)
            return true;

        Vector3 originalPosition = transform.position;
        Quaternion originalRotation = transform.rotation;

        if (safeStandCheckYOffset > 0.01f)
        {
            transform.position = candidatePosition + Vector3.up * safeStandCheckYOffset;

            Physics.SyncTransforms();
        }


        Bounds bounds = safeStandCollider.bounds;

        Collider[] hits = Physics.OverlapBox(
            bounds.center,
            bounds.extents,
            safeStandCollider.transform.rotation,
            safeStandBlockLayer,
            QueryTriggerInteraction.Ignore
        );

        if (safeStandCheckYOffset > 0.01f)
        {
            transform.position = originalPosition;
            transform.rotation = originalRotation;

            Physics.SyncTransforms();
        }

        foreach (Collider hit in hits)
        {
            if (hit == null)
                continue;

            if (hit == safeStandCollider)
                continue;

            if (hit.transform.IsChildOf(transform))
                continue;

            return false;
        }

        return true;
    }
    private void RespawnToLastGround()
    {
        Vector3 respawnPosition = lastGroundPosition + Vector3.up * respawnYOffset;

        if (characterController != null)
        {
            characterController.enabled = false;
            flyController.SetTransform(respawnPosition, lastGroundRotation, resetVelocity);
            characterController.enabled = true;
        }
        else
        {
            flyController.SetTransform(respawnPosition, lastGroundRotation, resetVelocity);
        }
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        switch (safeAreaMode)
        {
            case SafeAreaMode.XYZBox:
                {
                    Vector3 center = new Vector3(
                        (minX + maxX) * 0.5f,
                        (minY + maxY) * 0.5f,
                        (minZ + maxZ) * 0.5f
                    );

                    Vector3 size = new Vector3(
                        maxX - minX,
                        maxY - minY,
                        maxZ - minZ
                    );

                    Gizmos.DrawWireCube(center, size);
                    break;
                }

            case SafeAreaMode.CircleAndY:
                DrawCircleXZ(circleCenterXZ, circleRadius, circleMinY);
                DrawCircleXZ(circleCenterXZ, circleRadius, circleMaxY);
                DrawCircleWall(circleCenterXZ, circleRadius, circleMinY, circleMaxY);
                break;

            case SafeAreaMode.Sphere:
                Gizmos.DrawWireSphere(sphereCenter, sphereRadius);
                break;
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(lastGroundPosition + Vector3.up * respawnYOffset, 0.35f);
    }

    private void DrawCircleXZ(Vector2 centerXZ, float radius, float y)
    {
        int segments = 64;
        Vector3 prev = new Vector3(centerXZ.x + radius, y, centerXZ.y);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;

            Vector3 next = new Vector3(
                centerXZ.x + Mathf.Cos(angle) * radius,
                y,
                centerXZ.y + Mathf.Sin(angle) * radius
            );

            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }

    private void DrawCircleWall(Vector2 centerXZ, float radius, float bottomY, float topY)
    {
        int segments = 16;

        for (int i = 0; i < segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;

            Vector3 bottom = new Vector3(
                centerXZ.x + Mathf.Cos(angle) * radius,
                bottomY,
                centerXZ.y + Mathf.Sin(angle) * radius
            );

            Vector3 top = new Vector3(bottom.x, topY, bottom.z);

            Gizmos.DrawLine(bottom, top);
        }
    }
}