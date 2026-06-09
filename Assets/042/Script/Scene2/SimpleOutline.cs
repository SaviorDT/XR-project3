using UnityEngine;

public class SimpleOutline : MonoBehaviour
{
    public Material outlineMaterial;
    public float outlineScale = 1.05f;

    private GameObject outlineObject;

    void Awake()
    {
        CreateOutline();
        SetOutline(false);
    }

    private void CreateOutline()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        MeshRenderer mr = GetComponent<MeshRenderer>();

        if (mf == null || mr == null) return;

        outlineObject = new GameObject("White Outline");
        outlineObject.transform.SetParent(transform);
        outlineObject.transform.localPosition = Vector3.zero;
        outlineObject.transform.localRotation = Quaternion.identity;
        outlineObject.transform.localScale = Vector3.one * outlineScale;

        MeshFilter outlineMF = outlineObject.AddComponent<MeshFilter>();
        outlineMF.sharedMesh = mf.sharedMesh;

        MeshRenderer outlineMR = outlineObject.AddComponent<MeshRenderer>();
        outlineMR.material = outlineMaterial;
    }

    public void SetOutline(bool active)
    {
        if (outlineObject != null)
            outlineObject.SetActive(active);
    }
}