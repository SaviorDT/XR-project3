using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ComponentRemoverWindow : EditorWindow
{
    // 改為 List，用來接收多個根物件
    [SerializeField] private List<Transform> rootObjects = new List<Transform>();

    // 定義目標代數 (0=根物件本身, 1=第一代子物件, 依此類推)
    [SerializeField] private List<int> targetGenerations = new List<int>() { 1 };
    
    // 定義要移除的 Component 名稱
    [SerializeField] private List<string> componentsToRemove = new List<string>() { "BoxCollider" };

    private Vector2 scrollPos;
    private SerializedObject so;
    private SerializedProperty rootObjectsProp;
    private SerializedProperty generationsProp;
    private SerializedProperty componentsProp;

    [MenuItem("Tools/Component Remover")]
    public static void ShowWindow()
    {
        GetWindow<ComponentRemoverWindow>("Component Remover");
    }

    private void OnEnable()
    {
        so = new SerializedObject(this);
        rootObjectsProp = so.FindProperty("rootObjects");
        generationsProp = so.FindProperty("targetGenerations");
        componentsProp = so.FindProperty("componentsToRemove");
    }

    private void OnGUI()
    {
        so.Update();

        GUILayout.Label("基礎設定 (Base Settings)", EditorStyles.boldLabel);
        
        // 提示使用者如何操作
        EditorGUILayout.HelpBox("提示：在 Hierarchy 選取多個物件後，直接拖曳到下方的「根物件清單」標題上即可一次加入全部。也可以使用按鈕快速載入。", MessageType.Info);
        
        // 快速載入當前選取物件的按鈕
        if (GUILayout.Button("自動載入當前 Hierarchy 選取的物件", GUILayout.Height(25)))
        {
            LoadSelectedObjects();
        }

        // 多重物件清單介面
        EditorGUILayout.PropertyField(rootObjectsProp, new GUIContent("根物件清單 (Root Objects)"), true);

        GUILayout.Space(15);
        GUILayout.Label("目標代數設定 (Target Generations)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("0 表示根物件本身，1 表示第一代子物件，以此類推。", MessageType.Info);
        
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        EditorGUILayout.PropertyField(generationsProp, new GUIContent("指定代數 (x)"), true);

        GUILayout.Space(15);
        GUILayout.Label("移除元件設定 (Components to Remove)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("請輸入 Component 的準確類別名稱，例如: Rigidbody, BoxCollider, 或自訂腳本名稱。", MessageType.Info);
        EditorGUILayout.PropertyField(componentsProp, new GUIContent("要移除的元件 (y)"), true);
        
        EditorGUILayout.EndScrollView();

        so.ApplyModifiedProperties();

        GUILayout.Space(15);

        // 將按鈕改為紅色以作警示
        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
        if (GUILayout.Button("開始移除 (Remove Components)", GUILayout.Height(30)))
        {
            RemoveComponents();
        }
        GUI.backgroundColor = Color.white; // 還原顏色
    }

    // 將 Hierarchy 當前選取的物件加入清單
    private void LoadSelectedObjects()
    {
        rootObjects.Clear();
        foreach (GameObject obj in Selection.gameObjects)
        {
            rootObjects.Add(obj.transform);
        }
    }

    private void RemoveComponents()
    {
        if (rootObjects == null || rootObjects.Count == 0)
        {
            Debug.LogWarning("請先至少指定一個根物件 (Root Object)！");
            return;
        }

        if (targetGenerations.Count == 0)
        {
            Debug.LogWarning("請至少指定一個目標代數！");
            return;
        }

        if (componentsToRemove.Count == 0)
        {
            Debug.LogWarning("請至少指定一個要移除的 Component！");
            return;
        }

        // 整理並過濾有效輸入
        HashSet<int> validGenerations = new HashSet<int>(targetGenerations);
        List<System.Type> validComponentTypes = new List<System.Type>();

        foreach (string compName in componentsToRemove)
        {
            if (string.IsNullOrWhiteSpace(compName)) continue;

            // 防止意外移除 Transform 導致報錯
            if (compName.Equals("Transform", System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning("Transform 元件為核心元件，無法被移除。已自動略過。");
                continue;
            }

            System.Type type = GetTypeByName(compName);
            if (type != null && typeof(Component).IsAssignableFrom(type))
            {
                validComponentTypes.Add(type);
            }
            else
            {
                Debug.LogError($"找不到名為 '{compName}' 的 Component 類型，或者它不是一個有效的 Component。請檢查拼寫。");
            }
        }

        if (validComponentTypes.Count == 0)
        {
            Debug.LogWarning("沒有找到任何有效的 Component 類型，移除中止。");
            return;
        }

        int removedCount = 0;
        int targetFoundCount = 0;

        // 逐一處理清單中的每個根物件
        foreach (Transform root in rootObjects)
        {
            // 防呆：使用者可能手動新增陣列元素但留空
            if (root == null) continue; 
            
            ProcessTransform(root, 0, validGenerations, validComponentTypes, ref targetFoundCount, ref removedCount);
        }

        Debug.Log($"移除完畢！在所有根物件下，共找到 {targetFoundCount} 個符合代數的物件，總計移除了 {removedCount} 個 Components。");
    }

    // 遞迴處理物件及其子物件
    private void ProcessTransform(Transform currentTransform, int currentGeneration, HashSet<int> validGenerations, List<System.Type> componentTypes, ref int targetFoundCount, ref int removedCount)
    {
        // 檢查當前代數是否在使用者指定的列表中
        if (validGenerations.Contains(currentGeneration))
        {
            targetFoundCount++;
            foreach (System.Type compType in componentTypes)
            {
                // 使用 GetComponents 取得所有該類型的元件
                Component[] componentsOnObj = currentTransform.gameObject.GetComponents(compType);
                
                foreach (Component comp in componentsOnObj)
                {
                    if (comp is Transform) continue;

                    // 使用 Undo 進行刪除，支援 Ctrl + Z 復原
                    Undo.DestroyObjectImmediate(comp);
                    removedCount++;
                }
            }
        }

        // 遞迴處理下一代
        foreach (Transform child in currentTransform)
        {
            ProcessTransform(child, currentGeneration + 1, validGenerations, componentTypes, ref targetFoundCount, ref removedCount);
        }
    }

    // 輔助方法：透過字串名稱尋找 System.Type
    private System.Type GetTypeByName(string className)
    {
        System.Type type = System.Type.GetType($"UnityEngine.{className}, UnityEngine");
        if (type != null) return type;

        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType(className);
            if (type != null) return type;
            
            foreach(var t in assembly.GetTypes())
            {
                if(t.Name == className)
                {
                     return t;
                }
            }
        }

        return null;
    }
}