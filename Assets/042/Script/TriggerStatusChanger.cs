using UnityEngine;
using System.Collections;

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

    [Header("光柱")]
    public GameObject beaconPrefab;
    public float beaconShowDistance = 8f;
    public float fadeDuration = 0.5f;

    private GameObject beaconInstance;
    private Renderer[] beaconRenderers;
    private Coroutine beaconFadeCoroutine;
    private bool beaconVisible = false;

    private void Start()
    {
        AutoFindPlayer();
        CreateBeacon();
    }

    private void Update()
    {
        if (player == null)
            return;

        float distance = Vector3.Distance(player.position, transform.position);
        UpdateBeacon(distance);

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
    private void CreateBeacon()
    {
        if (beaconPrefab == null)
            return;

        beaconInstance = Instantiate(
            beaconPrefab,
            transform.position,
            Quaternion.identity
        );

        beaconRenderers =
            beaconInstance.GetComponentsInChildren<Renderer>();

        SetBeaconAlpha(0f);

        beaconInstance.SetActive(true);
    }
    private float GetBeaconAlpha()
    {
        if (beaconRenderers == null ||
            beaconRenderers.Length == 0)
            return 0f;

        return beaconRenderers[0]
            .material.color.a;
    }
    private void SetBeaconAlpha(float alpha)
    {
        if (beaconRenderers == null)
            return;

        foreach (Renderer renderer in beaconRenderers)
        {
            if (renderer == null)
                continue;

            foreach (Material mat in renderer.materials)
            {
                Color color = mat.color;
                color.a = alpha;
                mat.color = color;
            }
        }
    }
    private IEnumerator FadeBeacon(float targetAlpha)
    {
        float startAlpha = GetBeaconAlpha();

        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    timer / fadeDuration);

            float alpha =
                Mathf.Lerp(
                    startAlpha,
                    targetAlpha,
                    t);

            SetBeaconAlpha(alpha);

            yield return null;
        }

        SetBeaconAlpha(targetAlpha);
    }
    private void UpdateBeacon(float distance)
    {
        if (beaconInstance == null)
            return;

        bool shouldShow =
            distance >= beaconShowDistance;

        if (shouldShow == beaconVisible)
            return;

        beaconVisible = shouldShow;

        if (beaconFadeCoroutine != null)
            StopCoroutine(beaconFadeCoroutine);

        beaconFadeCoroutine =
            StartCoroutine(
                FadeBeacon(
                    shouldShow ? 1f : 0f
                )
            );
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
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            transform.position,
            beaconShowDistance);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(
            transform.position,
            interactDistance);
    }
}