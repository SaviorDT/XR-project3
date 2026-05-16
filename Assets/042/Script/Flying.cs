using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Flying : MonoBehaviour
{
    [Header("Fly Physics")]
    public float maxFlySpeed = 8f;
    public float damping = 1.5f;

    [Header("Fly Rotation")]
    public float flyRotateSpeed = 6f;

    [Header("Gravity")]
    public Vector3 gravityForce = new Vector3(0f, -9.8f, 0f);

    [Header("Ground Check")]
    public Transform footPoint;
    public float groundCheckRadius = 0.25f;
    public LayerMask groundMask = ~0;

    [Header("Take Off")]
    public float forceFlyDuration = 0.4f;

    private Rigidbody rb;
    private Vector3 externalForce = Vector3.zero;
    private float forceFlyUntilTime = 0f;

    public bool IsGrounded { get; private set; }
    public bool IsFlyingMode { get; private set; }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;

        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationZ;
    }

    void FixedUpdate()
    {
        CheckGround();

        bool forceFly = Time.time < forceFlyUntilTime;

        if (forceFly)
            IsFlyingMode = true;
        else
            IsFlyingMode = !IsGrounded;

        if (IsFlyingMode)
        {
            ApplyFlyPhysics();
            FaceFlyMovingDirection();
        }
        else
        {
            rb.linearVelocity = Vector3.zero;
            ClearForce();
        }
    }
    private void FaceFlyMovingDirection()
    {
        Vector3 moveDirection = rb.linearVelocity;
        moveDirection.y = 0f;

        if (moveDirection.sqrMagnitude < 0.001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection.normalized, Vector3.up);

        rb.MoveRotation(
            Quaternion.Slerp(
                rb.rotation,
                targetRotation,
                flyRotateSpeed * Time.fixedDeltaTime
            )
        );
    }
    private void ApplyFlyPhysics()
    {
        Vector3 totalForce = externalForce + gravityForce;
        Vector3 acceleration = totalForce / rb.mass;

        rb.linearVelocity += acceleration * Time.fixedDeltaTime;
        rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, maxFlySpeed);

        if (externalForce.sqrMagnitude < 0.001f)
        {
            rb.linearVelocity = Vector3.Lerp(
                rb.linearVelocity,
                Vector3.zero,
                damping * Time.fixedDeltaTime
            );
        }
    }

    private void CheckGround()
    {
        if (footPoint == null)
        {
            IsGrounded = false;
            return;
        }

        IsGrounded = Physics.CheckSphere(
            footPoint.position,
            groundCheckRadius,
            groundMask,
            QueryTriggerInteraction.Ignore
        );
    }

    public void AddForce(Vector3 force)
    {
        externalForce += force;
    }

    public void RemoveForce(Vector3 force)
    {
        externalForce -= force;
    }

    public void ClearForce()
    {
        externalForce = Vector3.zero;
    }

    public void ForceFlyMode()
    {
        forceFlyUntilTime = Time.time + forceFlyDuration;
        IsFlyingMode = true;
    }

    public Rigidbody GetRigidbody()
    {
        return rb;
    }

    private void OnDrawGizmosSelected()
    {
        if (footPoint != null)
        {
            Gizmos.DrawWireSphere(footPoint.position, groundCheckRadius);
        }
    }
}