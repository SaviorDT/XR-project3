using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class StarProjectile : MonoBehaviour
{
    public float lifeTime = 8f;
    public float gravity = 9.8f;

    private Rigidbody rb;
    private Vector3 velocity;
    private Vector3 randomAngularVelocity;

    public void Init(Vector3 direction, float speed, float upwardSpeed, Vector3 angularVelocity)
    {
        rb = GetComponent<Rigidbody>();

        velocity = direction.normalized * speed;
        velocity += Vector3.up * upwardSpeed;

        randomAngularVelocity = angularVelocity;

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

        Quaternion deltaRotation = Quaternion.Euler(randomAngularVelocity * Time.fixedDeltaTime);
        rb.MoveRotation(rb.rotation * deltaRotation);
    }
}