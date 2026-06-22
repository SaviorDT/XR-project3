using UnityEngine;

public class TriggerStatusChanger : MonoBehaviour
{
    [Header("Status 設定")]
    [Min(0)]
    public int status = 0;

    [Min(1)]
    public int maxStatus = 3;

    [Header("按下 Trigger 後")]
    public bool increaseStatus = true;
    public int targetStatus = 1;

    [Header("玩家")]
    public Transform player;
    public PlayerFlyController playerFlyController;

    [Header("互動設定")]
    public float interactDistance = 2f;
    public bool requireGrounded = true;

    private void Start()
    {
        AutoFindPlayer();
    }

    private void Update()
    {
        if (player == null)
            return;

        float distance = Vector3.Distance(player.position, transform.position);

        if (distance > interactDistance)
            return;

        if (requireGrounded && !IsPlayerGrounded())
            return;

        if (!CheckTriggerPressed())
            return;

        if (increaseStatus)
            IncreaseStatus();
        else
            SetStatus(targetStatus);
    }

    private void AutoFindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj == null)
        {
            Debug.LogWarning("找不到 Tag 為 Player 的物件");
            return;
        }

        player = playerObj.transform;
        playerFlyController = playerObj.GetComponent<PlayerFlyController>();
    }

    private bool IsPlayerGrounded()
    {
        if (playerFlyController == null)
            return false;

        return playerFlyController.IsGrounded();
    }

    private bool CheckTriggerPressed()
    {
        return OVRInput.GetDown(
            OVRInput.Button.PrimaryIndexTrigger,
            OVRInput.Controller.RTouch)
            ||
            OVRInput.GetDown(
            OVRInput.Button.PrimaryIndexTrigger,
            OVRInput.Controller.LTouch);
    }

    public void SetStatus(int newStatus)
    {
        status = Mathf.Clamp(newStatus, 0, maxStatus);
        Debug.Log($"{gameObject.name} Status = {status}");
    }

    public void IncreaseStatus()
    {
        status++;

        if (status > maxStatus)
            status = 0;

        Debug.Log($"{gameObject.name} Status = {status}");
    }

    public void DecreaseStatus()
    {
        status++;

        if (status <0 )
            status = maxStatus;

        Debug.Log($"{gameObject.name} Status = {status}");
    }

    private void OnValidate()
    {
        status = Mathf.Clamp(status, 0, maxStatus);
        targetStatus = Mathf.Clamp(targetStatus, 0, maxStatus);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactDistance);
    }
}