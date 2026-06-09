using System.Text;
using TMPro;
using UnityEngine;

public class CollectionReviewPoint : MonoBehaviour
{
    [Header("語言設定")]
    public bool chinese = true;

    [Header("UI，自動抓 HUD_SourceCanvas 下的 Text / Text 中文")]
    public TMP_Text reviewTextEN;
    public TMP_Text reviewTextCN;

    [Header("設定")]
    public bool showOnlyOnce = true;

    private bool alreadyShown = false;

    private void Start()
    {
        AutoFindUIText();
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

        ShowReview(recorder);
        alreadyShown = true;
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

        StringBuilder sb = new StringBuilder();

        if (chinese)
        {
            sb.AppendLine("本場景收集回顧");
            sb.AppendLine();

            if (recorder.GetCollectedItems().Count == 0)
            {
                sb.AppendLine("尚未收集任何物件。");
            }
            else
            {
                foreach (CollectedItemData item in recorder.GetCollectedItems())
                {
                    sb.AppendLine($"■ {item.itemNameCN}");
                    sb.AppendLine(item.descriptionCN);
                    sb.AppendLine();
                }
            }
        }
        else
        {
            sb.AppendLine("Collection Review");
            sb.AppendLine();

            if (recorder.GetCollectedItems().Count == 0)
            {
                sb.AppendLine("No items collected.");
            }
            else
            {
                foreach (CollectedItemData item in recorder.GetCollectedItems())
                {
                    sb.AppendLine($"■ {item.itemNameEN}");
                    sb.AppendLine(item.descriptionEN);
                    sb.AppendLine();
                }
            }
        }

        targetText.text = sb.ToString();
        targetText.gameObject.SetActive(true);
    }
}