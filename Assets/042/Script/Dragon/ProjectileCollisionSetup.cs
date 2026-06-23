using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ProjectileCollisionSetup : MonoBehaviour
{
    [Header("�I���]�w")]
    public bool autoSetupOnAwake = true;

    [Header("Kinematic Rigidbody �]�w")]
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