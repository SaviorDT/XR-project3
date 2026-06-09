using System.Collections.Generic;
using UnityEngine;

public class PlayerCollectionRecorder : MonoBehaviour
{
    public List<CollectedItemData> collectedItems = new List<CollectedItemData>();

    public void AddItem(CollectedItemData item)
    {
        if (collectedItems.Contains(item))
            return;

        collectedItems.Add(item);
    }

    public List<CollectedItemData> GetCollectedItems()
    {
        return collectedItems;
    }
}

[System.Serializable]
public class CollectedItemData
{
    public string itemID;

    public string itemNameEN;
    public string itemNameCN;

    [TextArea]
    public string descriptionEN;

    [TextArea]
    public string descriptionCN;
}