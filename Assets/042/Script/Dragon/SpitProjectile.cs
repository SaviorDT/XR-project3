using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SpitProjectile : MonoBehaviour
{
    public float lifeTime = 8f;
    public float gravity = 9.8f;

    private Rigidbody rb;
    private Vector3 velocity;

    public void Init(Vector3 direction, float speed, float upwardSpeed)
    {
        rb = GetComponent<Rigidbody>();

        velocity = direction.normalized * speed;
        velocity += Vector3.up * upwardSpeed;

        if (velocity != Vector3.zero)
        {
            transform.forward = velocity.normalized;
        }

        Destroy(gameObject, lifeTime);
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        velocity += Vector3.down * gravity * Time.fixedDeltaTime;

        rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);

        if (velocity.sqrMagnitude > 0.01f)
        {
            rb.MoveRotation(Quaternion.LookRotation(velocity.normalized));
        }
    }
}