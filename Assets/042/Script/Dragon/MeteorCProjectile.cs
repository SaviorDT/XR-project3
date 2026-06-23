using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MeteorCProjectile : MonoBehaviour
{
    public float lifeTime = 10f;
    public float turnSpeed = 2.5f;
    public float trackingDelay = 0.5f;
    public float InitSpeed = 30f;

    [Header("�w���]�w")]
    public float minPredictTime = 1f;
    public float maxPredictTime = 3f;
    public float predictDistanceFactor = 0.15f;

    private Rigidbody rb;
    private Transform player;

    private Vector3 currentDirection;
    private float speed;
    private float moveSpeed;
    private float timer;

    private Vector3 lastPlayerPosition;
    private Vector3 playerVelocity;
    private Vector3 predictedPosition;
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
        speed = Mathf.Max(InitSpeed * (transform.position - playerTarget.position).magnitude / 500f, moveSpeed);
        this.moveSpeed = moveSpeed;
        player = playerTarget;
        trackingDelay = delay;

        launcherForward = launcherForwardDirection.normalized;
        predictedPosition = transform.position + launcherForward * 1000f;

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
            speed = moveSpeed;
            predictedPosition = GetPredictedPlayerPosition();

            timer = 0;
        }
        Vector3 toPredictedPosition = predictedPosition - transform.position;

        if (toPredictedPosition.sqrMagnitude > 0.01f)
        {
            Vector3 targetDirection = ClampToForward90(toPredictedPosition.normalized);

            currentDirection = Vector3.Slerp(
                currentDirection,
                targetDirection,
                turnSpeed * Time.fixedDeltaTime
            ).normalized;
        }

        // rb.MovePosition(rb.position + currentDirection * speed * Time.fixedDeltaTime);
        rb.linearVelocity = currentDirection * speed;

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
        if (player == null)
            return transform.position + rb.linearVelocity * 100f;

        // If the player is behind the projectile relative to currentDirection, return a point far ahead on currentDirection
        float dot = Vector3.Dot(player.position - transform.position, currentDirection);
        if (dot < 0f)
        {
            return transform.position + rb.linearVelocity * 100f;
        }

        float v = playerVelocity.magnitude;
        float s = speed;

        // Fallback to original simple prediction when velocities are negligible
        if (v <= Mathf.Epsilon || s <= Mathf.Epsilon)
        {
            return player.position;
        }

        Vector3 dir = playerVelocity.normalized;
        Vector3 A = player.position - transform.position;
        float alpha = Vector3.Dot(A, dir);
        float A2 = A.sqrMagnitude;
        float k = v * v / (s * s);

        // dp / | predictedPosition - player.position | = sqrt(k)
        // Solve (1-k) dp^2 - 2k*alpha dp - k*A2 = 0 for dp (distance from player along playerVelocity direction)
        float a = 1f - k;
        float b = -2f * k * alpha;
        float c = -k * A2;

        float dp = -1f;

        if (Mathf.Abs(a) < 1e-6f)
        {
            if (Mathf.Abs(b) > 1e-6f)
                dp = -c / b;
        }
        else
        {
            float disc = b * b - 4f * a * c;
            if (disc >= 0f)
            {
                float sqrtD = Mathf.Sqrt(disc);
                float dp1 = (-b + sqrtD) / (2f * a);
                float dp2 = (-b - sqrtD) / (2f * a);
                dp = float.MaxValue;
                if (dp1 >= 0f) dp = Mathf.Min(dp, dp1);
                if (dp2 >= 0f) dp = Mathf.Min(dp, dp2);
                if (dp == float.MaxValue) dp = -1f;
            }
        }

        if (dp < 0f)
        {
            return transform.position + rb.linearVelocity * 100f;
        }

        return player.position + dir * dp;
    }
}