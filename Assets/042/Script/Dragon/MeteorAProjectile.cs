using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MeteorAProjectile : MonoBehaviour
{
    public float lifeTime = 8f;

    private Rigidbody rb;
    private Vector3 velocity;

    public void Init(Vector3 direction, float speed)
    {
        rb = GetComponent<Rigidbody>();

        velocity = direction.normalized * speed;

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
        rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
    }
}