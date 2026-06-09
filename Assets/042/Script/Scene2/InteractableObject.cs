using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    public SimpleOutline outline;

    void Start()
    {
        SetHighlight(false);
    }

    public void SetHighlight(bool active)
    {
        if (outline != null)
            outline.SetOutline(active);
    }

    public void Interact()
    {
        Debug.Log("¤¬°Ê¦¨¥\¡G" + gameObject.name);
    }
}