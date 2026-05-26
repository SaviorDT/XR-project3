using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyPhysics : MonoBehaviour
{
    [Header("Health")]
    public float maxHP = 100f;
    public float currentHP = 100f;

    [Header("Physics")]
    public float mass = 1f;
    public float maxSpeed = 8f;
    public float damping = 1.5f;
    public Vector3 gravityForce = new Vector3(0f, -9.8f, 0f);

    [Header("Rotation")]
    public float rotateSpeed = 6f;
    public bool faceMovingDirection = true;

    private Rigidbody rb;
    private Vector3 externalForce = Vector3.zero;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.useGravity = false;
        rb.mass = mass;

        // 避免撞到物體後被物理亂轉
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        currentHP = maxHP;
    }

    void FixedUpdate()
    {
        ApplyPhysics();

        if (faceMovingDirection)
        {
            FaceMovingDirection();
        }
    }

    private void ApplyPhysics()
    {
        Vector3 totalForce = externalForce + gravityForce;
        Vector3 acceleration = totalForce / rb.mass;

        rb.linearVelocity += acceleration * Time.fixedDeltaTime;
        rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, maxSpeed);

        if (externalForce.sqrMagnitude < 0.001f)
        {
            rb.linearVelocity = Vector3.Lerp(
                rb.linearVelocity,
                Vector3.zero,
                damping * Time.fixedDeltaTime
            );
        }
    }

    private void FaceMovingDirection()
    {
        Vector3 dir = rb.linearVelocity;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.05f) return;

        Quaternion targetRotation = Quaternion.LookRotation(dir.normalized, Vector3.up);

        rb.MoveRotation(
            Quaternion.Slerp(
                rb.rotation,
                targetRotation,
                rotateSpeed * Time.fixedDeltaTime
            )
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

    public void DealDamage(float damage)
    {
        currentHP -= damage;

        if (currentHP <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        Destroy(gameObject);
    }

    public Rigidbody GetRigidbody()
    {
        return rb;
    }
}