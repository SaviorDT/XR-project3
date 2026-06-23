using UnityEngine;

public class PlayerBeastRespawnArea : MonoBehaviour
{
    public enum SafeAreaMode
    {
        XYZBox,
        CircleAndY,
        Sphere
    }

    [Header("神獸")]
    public Transform beast;

    [Header("神獸前方重生設定")]
    public float extraDistanceInFrontOfBeast = 20f;
    public float respawnYOffset = 1.5f;
    public bool faceBeast = true;
    public bool resetVelocity = true;

    [Header("安全範圍模式")]
    public SafeAreaMode safeAreaMode = SafeAreaMode.XYZBox;

    [Header("XYZ Box 範圍")]
    public bool useRotatedXYZBox = false;

    public float minX = -10f;
    public float maxX = 10f;
    public float minY = -2f;
    public float maxY = 5f;
    public float minZ = -10f;
    public float maxZ = 10f;

    [Header("旋轉 XYZ Box 範圍")]
    public Vector3 xyzBoxCenter = Vector3.zero;
    public Vector3 xyzBoxSize = new Vector3(20f, 7f, 20f);
    public Vector3 xyzBoxEulerAngles = Vector3.zero;

    [Header("Circle + Y 範圍")]
    public Vector2 circleCenterXZ = Vector2.zero;
    public float circleRadius = 10f;
    public float circleMinY = -2f;
    public float circleMaxY = 5f;

    [Header("Sphere 範圍")]
    public Vector3 sphereCenter = Vector3.zero;
    public float sphereRadius = 10f;

    [Header("Debug")]
    public bool pressAToRespawn = true;

    private Rigidbody rb;
    private CharacterController characterController;
    private PlayerFlyController flyController;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        characterController = GetComponent<CharacterController>();
        flyController = GetComponent<PlayerFlyController>();
    }

    private void Update()
    {
        if (pressAToRespawn && OVRInput.GetDown(OVRInput.RawButton.A))
        {
            RespawnInFrontOfBeast();
            return;
        }

        if (!IsInsideSafeArea(transform.position))
        {
            RespawnInFrontOfBeast();
        }
    }

    private bool IsInsideSafeArea(Vector3 pos)
    {
        switch (safeAreaMode)
        {
            case SafeAreaMode.XYZBox:
                if (useRotatedXYZBox)
                    return IsInsideRotatedXYZBox(pos);

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

    private bool IsInsideRotatedXYZBox(Vector3 pos)
    {
        Quaternion rotation = Quaternion.Euler(xyzBoxEulerAngles);

        Vector3 localPos =
            Quaternion.Inverse(rotation) * (pos - xyzBoxCenter);

        Vector3 halfSize = xyzBoxSize * 0.5f;

        return Mathf.Abs(localPos.x) <= halfSize.x &&
               Mathf.Abs(localPos.y) <= halfSize.y &&
               Mathf.Abs(localPos.z) <= halfSize.z;
    }

    private void RespawnInFrontOfBeast()
    {
        if (beast == null)
        {
            Debug.LogWarning("尚未指定 Beast，無法依神獸位置重生");
            return;
        }

        Vector3 beastForward = beast.forward;
        beastForward.y = 0f;

        if (beastForward.sqrMagnitude < 0.001f)
            beastForward = Vector3.forward;

        beastForward.Normalize();

        float currentDistanceXZ = GetDistanceXZ(
            transform.position,
            beast.position
        );

        float respawnDistance =
            currentDistanceXZ + extraDistanceInFrontOfBeast;

        Vector3 respawnPosition =
            beast.position +
            beastForward * respawnDistance +
            Vector3.up * respawnYOffset;

        Quaternion respawnRotation =
            GetRespawnRotation(respawnPosition, beastForward);

        if (characterController != null)
            characterController.enabled = false;

        if (flyController != null)
        {
            flyController.SetTransform(
                respawnPosition,
                respawnRotation,
                resetVelocity
            );
        }
        else
        {
            transform.SetPositionAndRotation(
                respawnPosition,
                respawnRotation
            );

            if (resetVelocity && rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        if (characterController != null)
            characterController.enabled = true;
    }

    private float GetDistanceXZ(Vector3 a, Vector3 b)
    {
        Vector2 aXZ = new Vector2(a.x, a.z);
        Vector2 bXZ = new Vector2(b.x, b.z);

        return Vector2.Distance(aXZ, bXZ);
    }

    private Quaternion GetRespawnRotation(
        Vector3 respawnPosition,
        Vector3 beastForward)
    {
        if (faceBeast)
        {
            Vector3 lookDirection =
                beast.position - respawnPosition;

            lookDirection.y = 0f;

            if (lookDirection.sqrMagnitude < 0.001f)
                lookDirection = -beastForward;

            return Quaternion.LookRotation(
                lookDirection.normalized,
                Vector3.up
            );
        }

        return Quaternion.LookRotation(
            beastForward,
            Vector3.up
        );
    }

    private void OnDrawGizmosSelected()
    {
        DrawSafeAreaGizmos();
        DrawBeastRespawnGizmos();
    }

    private void DrawSafeAreaGizmos()
    {
        Gizmos.color = Color.green;

        switch (safeAreaMode)
        {
            case SafeAreaMode.XYZBox:
                if (useRotatedXYZBox)
                {
                    Matrix4x4 oldMatrix = Gizmos.matrix;

                    Gizmos.matrix = Matrix4x4.TRS(
                        xyzBoxCenter,
                        Quaternion.Euler(xyzBoxEulerAngles),
                        Vector3.one
                    );

                    Gizmos.DrawWireCube(Vector3.zero, xyzBoxSize);

                    Gizmos.matrix = oldMatrix;
                }
                else
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
                }

                break;

            case SafeAreaMode.CircleAndY:
                DrawCircleXZ(circleCenterXZ, circleRadius, circleMinY);
                DrawCircleXZ(circleCenterXZ, circleRadius, circleMaxY);
                DrawCircleWall(
                    circleCenterXZ,
                    circleRadius,
                    circleMinY,
                    circleMaxY
                );
                break;

            case SafeAreaMode.Sphere:
                Gizmos.DrawWireSphere(sphereCenter, sphereRadius);
                break;
        }
    }

    private void DrawBeastRespawnGizmos()
    {
        if (beast == null)
            return;

        Vector3 beastForward = beast.forward;
        beastForward.y = 0f;

        if (beastForward.sqrMagnitude < 0.001f)
            beastForward = Vector3.forward;

        beastForward.Normalize();

        float previewDistance =
            GetDistanceXZ(transform.position, beast.position)
            + extraDistanceInFrontOfBeast;

        Vector3 respawnPosition =
            beast.position +
            beastForward * previewDistance +
            Vector3.up * respawnYOffset;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(respawnPosition, 0.35f);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(
            beast.position,
            beast.position + beastForward * previewDistance
        );
    }

    private void DrawCircleXZ(Vector2 centerXZ, float radius, float y)
    {
        int segments = 64;

        Vector3 prev = new Vector3(
            centerXZ.x + radius,
            y,
            centerXZ.y
        );

        for (int i = 1; i <= segments; i++)
        {
            float angle =
                i / (float)segments * Mathf.PI * 2f;

            Vector3 next = new Vector3(
                centerXZ.x + Mathf.Cos(angle) * radius,
                y,
                centerXZ.y + Mathf.Sin(angle) * radius
            );

            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }

    private void DrawCircleWall(
        Vector2 centerXZ,
        float radius,
        float bottomY,
        float topY)
    {
        int segments = 16;

        for (int i = 0; i < segments; i++)
        {
            float angle =
                i / (float)segments * Mathf.PI * 2f;

            Vector3 bottom = new Vector3(
                centerXZ.x + Mathf.Cos(angle) * radius,
                bottomY,
                centerXZ.y + Mathf.Sin(angle) * radius
            );

            Vector3 top =
                new Vector3(bottom.x, topY, bottom.z);

            Gizmos.DrawLine(bottom, top);
        }
    }
}