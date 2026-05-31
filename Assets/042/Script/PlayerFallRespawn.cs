using UnityEngine;

public class PlayerFallRespawn : MonoBehaviour
{
    [Header("掉落判定")]
    public float fallLimitY = -20f;

    [Header("地面判定")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 1.2f;
    public float groundCheckRadius = 0.3f;

    [Header("重生設定")]
    public float respawnYOffset = 1.5f;
    public bool resetVelocity = true;

    private Vector3 lastGroundPosition;
    private Rigidbody rb;
    private CharacterController characterController;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        characterController = GetComponent<CharacterController>();

        lastGroundPosition = transform.position;
    }

    void Update()
    {
        UpdateLastGroundPosition();

        if (transform.position.y < fallLimitY)
        {
            RespawnToLastGround();
        }
    }

    private void UpdateLastGroundPosition()
    {
        if (IsOnGround())
        {
            lastGroundPosition = transform.position;
        }
    }

    private bool IsOnGround()
    {
        Vector3 origin = transform.position + Vector3.up * 0.2f;

        return Physics.SphereCast(
            origin,
            groundCheckRadius,
            Vector3.down,
            out RaycastHit hit,
            groundCheckDistance,
            groundLayer
        );
    }

    private void RespawnToLastGround()
    {
        Vector3 respawnPosition = lastGroundPosition + Vector3.up * respawnYOffset;

        if (characterController != null)
        {
            characterController.enabled = false;
            transform.position = respawnPosition;
            characterController.enabled = true;
        }
        else
        {
            transform.position = respawnPosition;
        }

        if (resetVelocity && rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}