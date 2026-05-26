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
    public float chaseUpForce = 25f;
    public float diveForce = 20f;
    public float heightTolerance = 2f;
    public float turnAngleThreshold = 10f;

    [Header("Random Movement")]
    public float randomForce = 15f;
    public float randomVerticalForce = 25f;
    public float randomChangeInterval = 2f;

    [Header("Random Height Control")]
    public LayerMask groundMask = ~0;
    public float groundCheckDistance = 50f;

    public float lowHeight = 5f;
    public float highHeight = 20f;

    [Range(0f, 1f)]
    public float lowHeightUpChance = 0.8f;

    [Range(0f, 1f)]
    public float midHeightUpChance = 0.35f;

    [Range(0f, 1f)]
    public float highHeightUpChance = 0.05f;

    private EnemyPhysics physics;

    private Vector3 currentForwardForce;
    private Vector3 currentSideForce;
    private Vector3 currentVerticalForce;
    private Vector3 currentRandomForce;

    private float nextRandomChangeTime = 0f;

    void Awake()
    {
        physics = GetComponent<EnemyPhysics>();

        if (physics == null)
        {
            Debug.LogError("EnemyAI 找不到 EnemyPhysics", this);
            enabled = false;
            return;
        }

        if (player == null)
        {
            PlayerFlyController playerFly =
                FindFirstObjectByType<PlayerFlyController>();

            if (playerFly != null)
            {
                player = playerFly.transform;
            }
        }

        if (player == null)
        {
            Flying flyingPlayer =
                FindFirstObjectByType<Flying>();

            if (flyingPlayer != null)
            {
                player = flyingPlayer.transform;
            }
        }

        if (player == null)
        {
            Debug.LogWarning("EnemyAI 找不到 Player，將先隨機移動", this);
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
            float heightFromGround = GetHeightFromGround();

            float upChance = midHeightUpChance;

            if (heightFromGround < lowHeight)
            {
                upChance = lowHeightUpChance;
            }
            else if (heightFromGround > highHeight)
            {
                upChance = highHeightUpChance;
            }

            float yDir;

            if (Random.value < upChance)
            {
                yDir = Random.Range(0.2f, 1f);
            }
            else
            {
                yDir = Random.Range(-0.8f, 0.2f);
            }

            Vector3 randomDir = new Vector3(
                Random.Range(-1f, 1f),
                yDir,
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

    private float GetHeightFromGround()
    {
        if (Physics.Raycast(
            transform.position,
            Vector3.down,
            out RaycastHit hit,
            groundCheckDistance,
            groundMask,
            QueryTriggerInteraction.Ignore
        ))
        {
            return hit.distance;
        }

        return groundCheckDistance;
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

        // currentRandomForce 不歸零，讓隨機方向持續到下一次更換
    }
}