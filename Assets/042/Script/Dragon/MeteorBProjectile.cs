using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MeteorBProjectile : MonoBehaviour
{
    public float lifeTime = 8f;
    public float turnSpeed = 3f;
    public float turnDuration = 2.5f;

    private Rigidbody rb;
    private Vector3 currentDirection;
    private Vector3 targetDirection;
    private float speed;
    private float timer;

    public void Init(
    Vector3 startDirection,
    Transform launcher,
    Transform player,
    float moveSpeed)
    {
        rb = GetComponent<Rigidbody>();

        currentDirection = startDirection.normalized;

        if (launcher != null && player != null)
        {
            targetDirection =
                (player.position - launcher.position).normalized;
        }
        else
        {
            targetDirection = currentDirection;
        }

        speed = moveSpeed;
        timer = 0f;

        if (currentDirection != Vector3.zero)
        {
            transform.forward = currentDirection;
        }

        Destroy(gameObject, lifeTime);
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        timer += Time.fixedDeltaTime;

        if (timer < turnDuration)
        {
            currentDirection = Vector3.Slerp(
                currentDirection,
                targetDirection,
                turnSpeed * Time.fixedDeltaTime
            ).normalized;
        }
        else
        {
            currentDirection = targetDirection;
        }

        rb.MovePosition(rb.position + currentDirection * speed * Time.fixedDeltaTime);

        if (currentDirection != Vector3.zero)
        {
            rb.MoveRotation(Quaternion.LookRotation(currentDirection));
        }
    }
}