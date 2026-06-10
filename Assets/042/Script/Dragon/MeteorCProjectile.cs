using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MeteorCProjectile : MonoBehaviour
{
    public float lifeTime = 10f;
    public float turnSpeed = 2.5f;
    public float trackingDelay = 0.5f;

    private Rigidbody rb;
    private Transform player;

    private Vector3 currentDirection;
    private float speed;
    private float timer;

    public void Init(Vector3 startDirection, float moveSpeed, Transform playerTarget, float delay)
    {
        rb = GetComponent<Rigidbody>();

        currentDirection = startDirection.normalized;
        speed = moveSpeed;
        player = playerTarget;
        trackingDelay = delay;

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

        if (timer >= trackingDelay && player != null)
        {
            Vector3 toPlayer = player.position - transform.position;

            if (toPlayer.sqrMagnitude > 0.01f)
            {
                Vector3 targetDirection = toPlayer.normalized;

                currentDirection = Vector3.Slerp(
                    currentDirection,
                    targetDirection,
                    turnSpeed * Time.fixedDeltaTime
                ).normalized;
            }
        }

        rb.MovePosition(rb.position + currentDirection * speed * Time.fixedDeltaTime);

        if (currentDirection != Vector3.zero)
        {
            rb.MoveRotation(Quaternion.LookRotation(currentDirection));
        }
    }
}