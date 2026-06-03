using UnityEngine;
using UnityEditor;

public class DivideScaleBy3
{
    [MenuItem("Tools/Divide Scale by 3")]
    static void DivideScale()
    {
        GameObject[] selected = Selection.gameObjects;

        if (selected.Length == 0)
        {
            Debug.LogWarning("沒有選取任何物件！");
            return;
        }

        foreach (GameObject obj in selected)
        {
            Undo.RegisterCompleteObjectUndo(obj.transform, "Divide Scale by 3");
            Vector3 s = obj.transform.localScale;
            obj.transform.localScale = new Vector3(s.x / 3f, s.y / 3f, s.z / 3f);
        }

        Debug.Log($"已將 {selected.Length} 個物件的 Scale 除以 3。");
    }
}
