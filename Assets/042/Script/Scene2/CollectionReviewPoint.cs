using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
public class CollectionReviewPoint : MonoBehaviour
{
    [Header("�y���]�w")]
    public bool chinese = true;

    [Header("UI�A�۰ʧ� HUD_SourceCanvas �U�� Text / Text ����")]
    public TMP_Text reviewTextEN;
    public TMP_Text reviewTextCN;

    [Header("�]�w")]
    public bool showOnlyOnce = true;

    [Header("��������")]
    public string nextSceneName;

    private bool waitingForTrigger = false;

    private bool alreadyShown = false;

    private void Start()
    {
        AutoFindUIText();
    }
    private void Update()
    {
        if (!waitingForTrigger)
            return;

        bool triggerPressed =
            OVRInput.GetDown(
                OVRInput.Button.PrimaryIndexTrigger,
                OVRInput.Controller.RTouch)
            ||
            OVRInput.GetDown(
                OVRInput.Button.PrimaryIndexTrigger,
                OVRInput.Controller.LTouch);

        if (triggerPressed)
        {
            SceneManager.LoadScene(nextSceneName);
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

        other.GetComponent<Rigidbody>().isKinematic = true;

        if (recorder == null)
            return;

        ShowReview(recorder);
        alreadyShown = true;
        waitingForTrigger = true;
    }

    private void AutoFindUIText()
    {
        GameObject canvasObj = GameObject.Find("HUD_SourceCanvas");

        if (canvasObj == null)
        {
            Debug.LogWarning("�䤣�� HUD_SourceCanvas");
            return;
        }

        Transform textEN = FindChildRecursive(canvasObj.transform, "Text");
        Transform textCN = FindChildRecursive(canvasObj.transform, "Text ����");

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
            sb.AppendLine("�����������^�U");
            sb.AppendLine();

            if (recorder.GetCollectedItems().Count == 0)
            {
                sb.AppendLine("�|���������󪫥�C");
            }
            else
            {
                foreach (CollectedItemData item in recorder.GetCollectedItems())
                {
                    sb.AppendLine($"�� {item.itemNameCN}");
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
                    sb.AppendLine($"�� {item.itemNameEN}");
                    sb.AppendLine(item.descriptionEN);
                    sb.AppendLine();
                }
            }
        }

        targetText.text = sb.ToString();
        targetText.gameObject.SetActive(true);

        waitingForTrigger = true;
    }
}