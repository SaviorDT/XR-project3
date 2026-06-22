using System.Collections;
using TMPro;
using UnityEngine;
using static OVRPlugin;
using static TutorialFlowController;

public class CollectibleItem : MonoBehaviour
{
    [Header("物件資料")]
    public CollectedItemData itemData;

    [Header("玩家")]
    public Transform player;
    public PlayerFlyController playerFlyController;
    public LayerMask groundLayer;

    [Header("距離設定")]
    public float beaconShowDistance = 8f;
    public float pickupDistance = 2f;

    [Header("玩家站地判定")]
    public float groundCheckDistance = 1.2f;
    public float groundCheckRadius = 0.3f;

    [Header("光柱")]
    public GameObject beaconPrefab;
    public float fadeDuration = 0.5f;

    [Header("UI，自動抓 HUD_SourceCanvas 下的 Title / Text 中文")]
    public TMP_Text titleText;
    public TMP_Text descriptionText;
    // public float messageDuration = 3f; // 不需要倒數計時了，可依需求刪除此行

    [Header("音效")]
    public AudioSource audioSource;

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
        {
            player = playerObj.transform;
            playerFlyController = playerObj.GetComponent<PlayerFlyController>();
        }
    }

    private void AutoFindUIText()
    {
        GameObject canvasObj = GameObject.Find("HUD_SourceCanvas");

        if (canvasObj == null)
        {
            Debug.LogWarning("找不到 HUD_SourceCanvas");
            return;
        }

        Transform title = canvasObj.transform.Find("Title");
        Transform textCN = canvasObj.transform.Find("Text 中文");

        if (title != null)
            titleText = title.GetComponent<TMP_Text>();

        if (textCN != null)
            descriptionText = textCN.GetComponent<TMP_Text>();

        if (titleText == null)
            Debug.LogWarning("找不到 Title 或 TMP_Text");

        if (descriptionText == null)
            Debug.LogWarning("找不到 Text 中文 或 TMP_Text");
    }

    private void Update()
    {
        if (collected || player == null)
            return;

        float distance = Vector3.Distance(player.position, transform.position);

        UpdateBeacon(distance);

        isGrounded = IsPlayerGrounded();

        bool canPickup = distance <= pickupDistance && isGrounded;

        if (canPickup)
            ShowPickupHint();
        else
            HidePickupHint();

        CheckPickup(distance);
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

        if (!CheckTriggerPressed())
            return;

        PickUp();
    }
    private void ShowPickupHint()
    {
        if (titleText == null || descriptionText == null)
            return;

        titleText.text = "???";
        descriptionText.text = "按 Trigger 收集";

        titleText.gameObject.SetActive(true);
        descriptionText.gameObject.SetActive(true);
    }
    private void HidePickupHint()
    {
        if (titleText != null)
            titleText.gameObject.SetActive(false);

        if (descriptionText != null)
            descriptionText.gameObject.SetActive(false);
    }
    private bool IsPlayerGrounded()
    {
        if (playerFlyController == null)
            return false;

        return playerFlyController.IsGrounded();
    }

    private void PickUp()
    {
        if (collected)
            return;

        if (audioSource != null)
        {
            audioSource.Play();
        }

        collected = true;

        PlayerCollectionRecorder recorder =
            player.GetComponent<PlayerCollectionRecorder>();

        if (recorder != null)
            recorder.AddItem(itemData);

        ShowPickupMessage();

        if (beaconInstance != null)
            Destroy(beaconInstance);

        // 隱藏外觀與碰撞體，讓物件看起來已被收集，但保持 GameObject 啟動以維持音效和 UI 協程
        HideItemVisuals();

        // 原本的 gameObject.SetActive(false); 移到協程最後面了
    }

    private void HideItemVisuals()
    {
        // 關閉所有渲染器 (不顯示模型)
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
            r.enabled = false;

        // 關閉所有碰撞體 (避免再度觸發任何物理判定)
        foreach (Collider c in GetComponentsInChildren<Collider>())
            c.enabled = false;
    }

    private void ShowPickupMessage()
    {
        if (titleText == null || descriptionText == null || itemData == null)
            return;

        titleText.text = itemData.itemNameCN;
        descriptionText.text = itemData.descriptionCN;

        titleText.gameObject.SetActive(true);
        descriptionText.gameObject.SetActive(true);

        if (messageCoroutine != null)
            StopCoroutine(messageCoroutine);

        messageCoroutine = StartCoroutine(HideMessageRoutine());
    }

    private bool CheckTriggerPressed()
    {
        return OVRInput.GetDown(
            OVRInput.Button.PrimaryIndexTrigger,
            OVRInput.Controller.RTouch
        )
        ||
        OVRInput.GetDown(
            OVRInput.Button.PrimaryIndexTrigger,
            OVRInput.Controller.LTouch
        );
    }

    private IEnumerator HideMessageRoutine()
    {
        // 先等待一幀，避免玩家可能剛好在按著 Trigger 的瞬間觸發收集，導致 UI 閃退
        yield return null;

        // 等待直到玩家按下 VR 控制器的 Trigger
        yield return new WaitUntil(() => CheckTriggerPressed());

        if (titleText != null)
            titleText.gameObject.SetActive(false);

        if (descriptionText != null)
            descriptionText.gameObject.SetActive(false);

        // UI 關閉且一切處理完畢後，才徹底關閉這個物件
        gameObject.SetActive(false);
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