using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Flying : MonoBehaviour
{
    public float maxSpeed = 5f;
    public float rotateSpeed = 10f;
    public float damping = 2f;

    private Rigidbody rb;
    private Vector3 externalForce = Vector3.down;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.freezeRotation = true;
    }

    void FixedUpdate()
    {
        Vector3 acceleration = externalForce / rb.mass;

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

        FaceMovingDirection();
    }

    private void FaceMovingDirection()
    {
        Vector3 direction = rb.linearVelocity;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            rb.MoveRotation(
                Quaternion.Slerp(
                    rb.rotation,
                    targetRotation,
                    rotateSpeed * Time.fixedDeltaTime
                )
            );
        }
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
}