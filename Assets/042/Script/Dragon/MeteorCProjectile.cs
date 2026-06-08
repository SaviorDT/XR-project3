using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MeteorCProjectile : MonoBehaviour
{
    public float lifeTime = 10f;
    public float turnSpeed = 2.5f;
    public float aimTurnDuration = 1.5f;

    private Rigidbody rb;
    private Transform player;

    private Vector3 currentDirection;
    private Vector3 targetDirection;

    private float speed;
    private float lockDelay;
    private float timer;
    private bool targetLocked;
    private bool finishedTurning;

    public void Init(Vector3 startDirection, float moveSpeed, Transform playerTarget, float delay)
    {
        rb = GetComponent<Rigidbody>();

        currentDirection = startDirection.normalized;
        speed = moveSpeed;
        player = playerTarget;
        lockDelay = delay;

        timer = 0f;
        targetLocked = false;
        finishedTurning = false;

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

        if (!targetLocked && timer >= lockDelay)
        {
            LockPlayerCurrentPosition();
        }

        if (targetLocked && !finishedTurning)
        {
            currentDirection = Vector3.Slerp(
                currentDirection,
                targetDirection,
                turnSpeed * Time.fixedDeltaTime
            ).normalized;

            float angle = Vector3.Angle(currentDirection, targetDirection);

            if (angle < 2f)
            {
                currentDirection = targetDirection;
                finishedTurning = true;
            }
        }

        rb.MovePosition(rb.position + currentDirection * speed * Time.fixedDeltaTime);

        if (currentDirection != Vector3.zero)
        {
            rb.MoveRotation(Quaternion.LookRotation(currentDirection));
        }
    }

    private void LockPlayerCurrentPosition()
    {
        if (player == null)
        {
            targetDirection = currentDirection;
        }
        else
        {
            Vector3 toPlayer = player.position - transform.position;

            if (toPlayer.sqrMagnitude > 0.01f)
            {
                targetDirection = toPlayer.normalized;
            }
            else
            {
                targetDirection = currentDirection;
            }
        }

        targetLocked = true;
    }
}