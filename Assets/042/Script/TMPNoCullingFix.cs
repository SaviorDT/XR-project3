using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class TMPNoCullingFix : MonoBehaviour
{
    private TMP_Text tmp;
    private Renderer[] renderers;

    private void Awake()
    {
        tmp = GetComponent<TMP_Text>();
        renderers = GetComponentsInChildren<Renderer>(true);

        tmp.ForceMeshUpdate(true, true);
        ExpandBounds();
    }

    private void LateUpdate()
    {
        tmp.ForceMeshUpdate(false, false);
        ExpandBounds();
    }

    private void ExpandBounds()
    {
        foreach (Renderer r in renderers)
        {
            if (r == null) continue;

            Bounds b = r.localBounds;
            b.extents = new Vector3(100f, 100f, 100f);
            r.localBounds = b;
        }
    }
}