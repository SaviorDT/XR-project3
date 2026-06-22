using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CollectionReviewPoint : MonoBehaviour
{
    [Header("語言設定")]
    public bool chinese = true;

    [Header("UI，自動抓 HUD_SourceCanvas 下的 Text / Text 中文")]
    public TMP_Text reviewTextEN;
    public TMP_Text reviewTextCN;

    [Header("設定")]
    public bool showOnlyOnce = false;
    public int requiredItemCount = 4;

    [Header("場景切換")]
    public string nextSceneName;

    private bool waitingForTrigger = false;
    private bool canChangeScene = false;
    private bool alreadyShown = false;

    private Rigidbody currentPlayerRb;

    private void Start()
    {
        AutoFindUIText();
    }

    private void Update()
    {
        if (!waitingForTrigger)
            return;

        if (!CheckTriggerPressed())
            return;

        waitingForTrigger = false;

        if (canChangeScene)
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            HideReviewText();

            if (currentPlayerRb != null)
                currentPlayerRb.isKinematic = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (alreadyShown && showOnlyOnce)
            return;

        if (!other.CompareTag("Player"))
            return;

        PlayerCollectionRecorder recorder =
            other.GetComponent<PlayerCollectionRecorder>();

        if (recorder == null)
            return;

        currentPlayerRb = other.GetComponent<Rigidbody>();

        if (currentPlayerRb != null)
            currentPlayerRb.isKinematic = true;

        ShowReview(recorder);

        alreadyShown = true;
        waitingForTrigger = true;
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

    private void AutoFindUIText()
    {
        GameObject canvasObj = GameObject.Find("HUD_SourceCanvas");

        if (canvasObj == null)
        {
            Debug.LogWarning("找不到 HUD_SourceCanvas");
            return;
        }

        Transform textEN = FindChildRecursive(canvasObj.transform, "Text");
        Transform textCN = FindChildRecursive(canvasObj.transform, "Text 中文");

        if (textEN != null)
            reviewTextEN = textEN.GetComponent<TMP_Text>();

        if (textCN != null)
            reviewTextCN = textCN.GetComponent<TMP_Text>();
    }

    private Transform FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;

            Transform result = FindChildRecursive(child, childName);

            if (result != null)
                return result;
        }

        return null;
    }

    private void ShowReview(PlayerCollectionRecorder recorder)
    {
        TMP_Text targetText = chinese ? reviewTextCN : reviewTextEN;

        if (targetText == null)
            return;

        int collectedCount = recorder.GetCollectedItems().Count;

        canChangeScene = collectedCount >= requiredItemCount;

        StringBuilder sb = new StringBuilder();

        if (chinese)
        {
            sb.AppendLine("收集物品回顧");
            sb.AppendLine();
            sb.AppendLine($"目前收集：{collectedCount} / {requiredItemCount}");
            sb.AppendLine();

            if (collectedCount == 0)
            {
                sb.AppendLine("你尚未收集任何物品。");
            }
            else
            {
                foreach (CollectedItemData item in recorder.GetCollectedItems())
                {
                    sb.AppendLine($"• {item.itemNameCN}");
                }
            }

            sb.AppendLine();

            if (canChangeScene)
                sb.AppendLine("已收集完成，按 Trigger 繼續。");
            else
                sb.AppendLine("尚未收集完成，按 Trigger 返回並繼續尋找。");
        }
        else
        {
            sb.AppendLine("Collection Review");
            sb.AppendLine();
            sb.AppendLine($"Collected: {collectedCount} / {requiredItemCount}");
            sb.AppendLine();

            if (collectedCount == 0)
            {
                sb.AppendLine("No items collected.");
            }
            else
            {
                foreach (CollectedItemData item in recorder.GetCollectedItems())
                {
                    sb.AppendLine($"• {item.itemNameEN}");
                }
            }

            sb.AppendLine();

            if (canChangeScene)
                sb.AppendLine("Collection complete. Press Trigger to continue.");
            else
                sb.AppendLine("Collection incomplete. Press Trigger to return.");
        }

        targetText.text = sb.ToString();
        targetText.gameObject.SetActive(true);
    }

    private void HideReviewText()
    {
        if (reviewTextEN != null)
        {
            reviewTextEN.text = "";
            reviewTextEN.gameObject.SetActive(false);
        }

        if (reviewTextCN != null)
        {
            reviewTextCN.text = "";
            reviewTextCN.gameObject.SetActive(false);
        }
    }
}