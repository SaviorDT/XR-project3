using UnityEngine;

public class PlayerFallRespawn : MonoBehaviour
{
    public enum SafeAreaMode
    {
        XYZBox,      // x/y/z 上下限
        CircleAndY, // xz 圓形 + y 上下限
        Sphere      // 球體範圍
    }

    [Header("掉落判定")]
    public float fallLimitY = -20f;

    [Header("地面判定")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 1.2f;
    public float groundCheckRadius = 0.3f;

    [Header("安全範圍模式")]
    public SafeAreaMode safeAreaMode = SafeAreaMode.XYZBox;

    [Header("XYZ 立方體範圍")]
    public float minX = -10f;
    public float maxX = 10f;
    public float minY = -2f;
    public float maxY = 5f;
    public float minZ = -10f;
    public float maxZ = 10f;

    [Header("Circle + Y 範圍")]
    public Vector2 circleCenterXZ = Vector2.zero;
    public float circleRadius = 10f;
    public float circleMinY = -2f;
    public float circleMaxY = 5f;

    [Header("Sphere 範圍")]
    public Vector3 sphereCenter = Vector3.zero;
    public float sphereRadius = 10f;

    [Header("牆面 / 障礙檢查")]
    public LayerMask wallLayer;
    public float wallCheckRadius = 0.35f;
    public float wallCheckDistance = 0.6f;
    public float wallCheckHeight = 0.8f;

    [Header("重生設定")]
    public float respawnYOffset = 1.5f;
    public bool resetVelocity = true;

    private Vector3 lastGroundPosition;
    private Rigidbody rb;
    private CharacterController characterController;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        characterController = GetComponent<CharacterController>();
        lastGroundPosition = transform.position;
    }

    void Update()
    {
        UpdateLastGroundPosition();

        if (transform.position.y < fallLimitY)
        {
            RespawnToLastGround();
        }
    }

    private void UpdateLastGroundPosition()
    {
        if (!IsOnGround(out RaycastHit hit))
            return;

        Vector3 candidate = transform.position;
        candidate.y = hit.point.y;

        if (!IsInsideSafeArea(candidate))
            return;

        if (IsNearWall(candidate))
            return;

        lastGroundPosition = candidate;
    }

    private bool IsOnGround(out RaycastHit hit)
    {
        Vector3 origin = transform.position + Vector3.up * 0.2f;

        return Physics.SphereCast(
            origin,
            groundCheckRadius,
            Vector3.down,
            out hit,
            groundCheckDistance,
            groundLayer
        );
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
                {
                    Vector2 p = new Vector2(pos.x, pos.z);
                    bool insideCircle = Vector2.Distance(p, circleCenterXZ) <= circleRadius;
                    bool insideY = pos.y >= circleMinY && pos.y <= circleMaxY;

                    return insideCircle && insideY;
                }

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
            if (Physics.SphereCast(
                origin,
                wallCheckRadius,
                dir,
                out _,
                wallCheckDistance,
                wallLayer
            ))
            {
                return true;
            }
        }

        return false;
    }

    private void RespawnToLastGround()
    {
        Vector3 respawnPosition = lastGroundPosition + Vector3.up * respawnYOffset;

        if (characterController != null)
        {
            characterController.enabled = false;
            transform.position = respawnPosition;
            characterController.enabled = true;
        }
        else
        {
            transform.position = respawnPosition;
        }

        if (resetVelocity && rb != null)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector3.zero;
#else
            rb.velocity = Vector3.zero;
#endif
            rb.angularVelocity = Vector3.zero;
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