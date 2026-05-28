using UnityEngine;

[RequireComponent(typeof(CapsuleCollider))]
public class WindColliderScaler : MonoBehaviour
{
    [Header("Collider Direction")]
    [Tooltip("0 = X, 1 = Y, 2 = Z")]
    public int direction = 2;

    [Header("Base Local Size")]
    public float baseRadius = 1f;
    public float baseHeight = 6f;

    [Header("Center Offset")]
    public bool centerAlongWind = true;
    public Vector3 centerOffset = Vector3.zero;

    private CapsuleCollider capsule;

    void Awake()
    {
        capsule = GetComponent<CapsuleCollider>();
        capsule.isTrigger = true;
    }

    void Start()
    {
        ApplyColliderSettings();
    }

    void LateUpdate()
    {
        if (!TestModeController.ShouldUpdateEveryFrame()) return;

        ApplyColliderSettings();
    }
    private void ApplyColliderSettings()
    {
        capsule.direction = direction;

        capsule.radius = baseRadius;
        capsule.height = Mathf.Max(baseHeight, baseRadius * 2f);

        Vector3 center = centerOffset;

        if (centerAlongWind)
        {
            float halfLength = capsule.height * 0.5f;

            if (direction == 0)
                center += Vector3.right * halfLength;
            else if (direction == 1)
                center += Vector3.up * halfLength;
            else
                center += Vector3.forward * halfLength;
        }

        capsule.center = center;
    }
}