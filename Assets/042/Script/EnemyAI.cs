using UnityEngine;

[RequireComponent(typeof(EnemyPhysics))]
public class EnemyAI : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Detection")]
    public float detectDistance = 30f;

    [Header("Chase Forces")]
    public float chaseForwardForce = 18f;
    public float chaseSideForce = 10f;
    public float chaseUpForce = 12f;
    public float diveForce = 18f;
    public float heightTolerance = 2f;
    public float turnAngleThreshold = 10f;

    [Header("Random Movement")]
    public float randomForce = 10f;
    public float randomVerticalForce = 6f;
    public float randomChangeInterval = 2f;

    private EnemyPhysics physics;

    private Vector3 currentForwardForce;
    private Vector3 currentSideForce;
    private Vector3 currentVerticalForce;
    private Vector3 currentRandomForce;

    private float nextRandomChangeTime = 0f;

    void Awake()
    {
        physics = GetComponent<EnemyPhysics>();
        if(player == null)
        {
            player = FindFirstObjectByType<PlayerFlyController>().transform;
        }
    }

    void FixedUpdate()
    {
        ClearPreviousAIForces();

        if (player != null && IsPlayerInRange())
        {
            ChasePlayer();
        }
        else
        {
            RandomMove();
        }
    }

    private bool IsPlayerInRange()
    {
        float distance = Vector3.Distance(transform.position, player.position);
        return distance <= detectDistance;
    }

    private void ChasePlayer()
    {
        Vector3 toPlayer = player.position - transform.position;

        Vector3 horizontalToPlayer = toPlayer;
        horizontalToPlayer.y = 0f;

        if (horizontalToPlayer.sqrMagnitude > 0.001f)
        {
            Vector3 dirToPlayer = horizontalToPlayer.normalized;

            float angle = Vector3.SignedAngle(
                transform.forward,
                dirToPlayer,
                Vector3.up
            );

            currentForwardForce = transform.forward * chaseForwardForce;
            physics.AddForce(currentForwardForce);

            if (angle > turnAngleThreshold)
            {
                currentSideForce = transform.right * chaseSideForce;
                physics.AddForce(currentSideForce);
            }
            else if (angle < -turnAngleThreshold)
            {
                currentSideForce = -transform.right * chaseSideForce;
                physics.AddForce(currentSideForce);
            }
        }

        float heightDiff = player.position.y - transform.position.y;

        if (heightDiff > heightTolerance)
        {
            currentVerticalForce = Vector3.up * chaseUpForce;
            physics.AddForce(currentVerticalForce);
        }
        else if (heightDiff < -heightTolerance)
        {
            currentVerticalForce = Vector3.down * diveForce;
            physics.AddForce(currentVerticalForce);
        }
    }

    private void RandomMove()
    {
        if (Time.time >= nextRandomChangeTime)
        {
            Vector3 randomDir = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-0.5f, 0.5f),
                Random.Range(-1f, 1f)
            ).normalized;

            currentRandomForce = new Vector3(
                randomDir.x * randomForce,
                randomDir.y * randomVerticalForce,
                randomDir.z * randomForce
            );

            nextRandomChangeTime = Time.time + randomChangeInterval;
        }

        physics.AddForce(currentRandomForce);
    }

    private void ClearPreviousAIForces()
    {
        physics.RemoveForce(currentForwardForce);
        physics.RemoveForce(currentSideForce);
        physics.RemoveForce(currentVerticalForce);
        physics.RemoveForce(currentRandomForce);

        currentForwardForce = Vector3.zero;
        currentSideForce = Vector3.zero;
        currentVerticalForce = Vector3.zero;

        // currentRandomForce 不要在這裡歸零
        // 否則隨機方向每幀都會消失
    }
}