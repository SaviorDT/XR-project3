using System.Collections;
using TMPro;
using UnityEngine;

public class CollectibleItem : MonoBehaviour
{
    [Header("物件資料")]
    public CollectedItemData itemData;

    [Header("玩家")]
    public Transform player;
    public PlayerFlyController playerFlyController;
    public LayerMask groundLayer;

    [Header("語言設定")]
    public bool chinese = true;

    [Header("距離設定")]
    public float beaconShowDistance = 8f;
    public float pickupDistance = 2f;

    [Header("玩家站地判定")]
    public float groundCheckDistance = 1.2f;
    public float groundCheckRadius = 0.3f;

    [Header("光柱")]
    public GameObject beaconPrefab;
    public float fadeDuration = 0.5f;

    [Header("UI")]
    public TMP_Text messageTextEN;
    public TMP_Text messageTextCN;
    public float messageDuration = 3f;

    public bool isGrounded;

    private GameObject beaconInstance;
    private Renderer[] beaconRenderers;
    private Coroutine beaconFadeCoroutine;
    private Coroutine messageCoroutine;

    private bool beaconVisible = false;
    private bool collected = false;

    private void Start()
    {
        AutoFindPlayer();
        AutoFindUIText();
        CreateBeacon();
    }

    private void AutoFindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
            player = playerObj.transform;
    }

    private void AutoFindUIText()
    {
        GameObject canvasObj = GameObject.Find("HUD_SourceCanvas");

        if (canvasObj == null)
        {
            Debug.LogWarning("找不到 HUD_SourceCanvas");
            return;
        }

        Transform textEN = canvasObj.transform.Find("Text");
        Transform textCN = canvasObj.transform.Find("Text 中文");

        if (textEN != null)
            messageTextEN = textEN.GetComponent<TMP_Text>();

        if (textCN != null)
            messageTextCN = textCN.GetComponent<TMP_Text>();

        if (messageTextEN == null)
            Debug.LogWarning("找不到英文 Text 或 TMP_Text");

        if (messageTextCN == null)
            Debug.LogWarning("找不到中文 Text 或 TMP_Text");
    }

    private void Update()
    {
        if (collected || player == null)
            return;

        float distance = Vector3.Distance(player.position, transform.position);

        UpdateBeacon(distance);
        CheckPickup(distance);
        isGrounded = IsPlayerGrounded();
    }

    private void FindPlayerIfNeeded()
    {
        if (player != null)
            return;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
            player = playerObj.transform;
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

        beaconRenderers = beaconInstance.GetComponentsInChildren<Renderer>();

        SetBeaconAlpha(0f);
        beaconInstance.SetActive(true);
    }

    private void UpdateBeacon(float distance)
    {
        if (beaconInstance == null)
            return;

        bool shouldShow = distance >= beaconShowDistance;

        if (shouldShow == beaconVisible)
            return;

        beaconVisible = shouldShow;

        if (beaconFadeCoroutine != null)
            StopCoroutine(beaconFadeCoroutine);

        beaconFadeCoroutine = StartCoroutine(
            FadeBeacon(shouldShow ? 1f : 0f)
        );
    }

    private void CheckPickup(float distance)
    {
        if (distance > pickupDistance)
            return;

        if (!IsPlayerGrounded())
            return;

        PickUp();
    }

    private bool IsPlayerGrounded()
    {
        return playerFlyController.IsGrounded();
    }

    private void PickUp()
    {
        if (collected)
            return;

        collected = true;

        PlayerCollectionRecorder recorder =
            player.GetComponent<PlayerCollectionRecorder>();

        if (recorder != null)
        {
            recorder.AddItem(itemData);
        }

        ShowPickupMessage();

        if (beaconInstance != null)
        {
            Destroy(beaconInstance);
        }

        gameObject.SetActive(false);
    }

    private void ShowPickupMessage()
    {
        TMP_Text targetText = chinese ? messageTextCN : messageTextEN;

        if (targetText == null || itemData == null)
            return;

        string message = chinese
            ? $"獲得：{itemData.itemNameCN}"
            : $"Collected: {itemData.itemNameEN}";

        if (messageCoroutine != null)
            StopCoroutine(messageCoroutine);

        messageCoroutine = StartCoroutine(
            ShowMessageRoutine(targetText, message)
        );
    }

    private IEnumerator ShowMessageRoutine(TMP_Text targetText, string message)
    {
        targetText.text = message;
        targetText.gameObject.SetActive(true);

        yield return new WaitForSeconds(messageDuration);

        targetText.gameObject.SetActive(false);
    }

    private IEnumerator FadeBeacon(float targetAlpha)
    {
        float startAlpha = GetBeaconAlpha();
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / fadeDuration);
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, t);

            SetBeaconAlpha(alpha);

            yield return null;
        }

        SetBeaconAlpha(targetAlpha);
    }

    private float GetBeaconAlpha()
    {
        if (beaconRenderers == null || beaconRenderers.Length == 0)
            return 0f;

        return beaconRenderers[0].material.color.a;
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, beaconShowDistance);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pickupDistance);
    }
}