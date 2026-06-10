using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MeteorCProjectile : MonoBehaviour
{
    public float lifeTime = 10f;
    public float turnSpeed = 2.5f;
    public float trackingDelay = 0.5f;

    [Header("¹w´ú³]©w")]
    public float minPredictTime = 1f;
    public float maxPredictTime = 3f;
    public float predictDistanceFactor = 0.15f;

    private Rigidbody rb;
    private Transform player;

    private Vector3 currentDirection;
    private float speed;
    private float timer;

    private Vector3 lastPlayerPosition;
    private Vector3 playerVelocity;
    private bool hasLastPlayerPosition = false;
    private Vector3 launcherForward;
    public void Init(
    Vector3 startDirection,
    float moveSpeed,
    Transform playerTarget,
    float delay,
    Vector3 launcherForwardDirection)
    {
        rb = GetComponent<Rigidbody>();

        currentDirection = startDirection.normalized;
        speed = moveSpeed;
        player = playerTarget;
        trackingDelay = delay;

        launcherForward = launcherForwardDirection.normalized;

        timer = 0f;

        if (player != null)
        {
            lastPlayerPosition = player.position;
            hasLastPlayerPosition = true;
        }

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

        UpdatePlayerVelocity();

        if (timer >= trackingDelay && player != null)
        {
            Vector3 predictedPosition = GetPredictedPlayerPosition();
            Vector3 toPredictedPosition = predictedPosition - transform.position;

            if (toPredictedPosition.sqrMagnitude > 0.01f)
            {
                Vector3 targetDirection = ClampToForward90(
    toPredictedPosition.normalized
);

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
    private Vector3 ClampToForward90(Vector3 desiredDirection)
    {
        float angle = Vector3.Angle(
            launcherForward,
            desiredDirection
        );

        if (angle <= 90f)
            return desiredDirection;

        float sign = Mathf.Sign(
            Vector3.Dot(
                Vector3.Cross(launcherForward, desiredDirection),
                Vector3.up
            )
        );

        Quaternion limitRotation =
            Quaternion.AngleAxis(
                90f * sign,
                Vector3.up
            );

        return (limitRotation * launcherForward).normalized;
    }
    private void UpdatePlayerVelocity()
    {
        if (player == null)
            return;

        if (!hasLastPlayerPosition)
        {
            lastPlayerPosition = player.position;
            hasLastPlayerPosition = true;
            playerVelocity = Vector3.zero;
            return;
        }

        playerVelocity =
            (player.position - lastPlayerPosition) / Time.fixedDeltaTime;

        lastPlayerPosition = player.position;
    }

    private Vector3 GetPredictedPlayerPosition()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        float predictTime = Mathf.Clamp(
            distanceToPlayer * predictDistanceFactor,
            minPredictTime,
            maxPredictTime
        );

        return player.position + playerVelocity * predictTime;
    }
}