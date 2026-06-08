using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ProjectileCollisionSetup : MonoBehaviour
{
    [Header("碰撞設定")]
    public bool autoSetupOnAwake = true;

    [Header("Kinematic Rigidbody 設定")]
    public bool useGravity = false;
    public bool isKinematic = true;

    private void Awake()
    {
        if (autoSetupOnAwake)
        {
            Setup();
        }
    }

    public void Setup()
    {
        Rigidbody rb = GetComponent<Rigidbody>();

        rb.useGravity = useGravity;
        rb.isKinematic = isKinematic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        Collider[] colliders = GetComponentsInChildren<Collider>();

        foreach (Collider col in colliders)
        {
            col.isTrigger = false;
        }
    }
}