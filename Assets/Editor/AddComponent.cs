using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ComponentAttacherWindow : EditorWindow
{
    private Transform rootObject;

    // 定義目標代數 (0=根物件本身, 1=第一代子物件, 依此類推)
    [SerializeField] private List<int> targetGenerations = new List<int>() { 1 };
    
    // 定義要掛載的 Component 名稱 (需要是有效的類別名稱，例如 "BoxCollider", "Rigidbody", "YourCustomScript")
    [SerializeField] private List<string> componentsToAttach = new List<string>() { "BoxCollider" };

    private Vector2 scrollPos;
    private SerializedObject so;
    private SerializedProperty generationsProp;
    private SerializedProperty componentsProp;

    [MenuItem("Tools/Component Attacher")]
    public static void ShowWindow()
    {
        GetWindow<ComponentAttacherWindow>("Component Attacher");
    }

    private void OnEnable()
    {
        so = new SerializedObject(this);
        generationsProp = so.FindProperty("targetGenerations");
        componentsProp = so.FindProperty("componentsToAttach");
    }

    private void OnGUI()
    {
        so.Update();

        GUILayout.Label("基礎設定 (Base Settings)", EditorStyles.boldLabel);
        rootObject = (Transform)EditorGUILayout.ObjectField("根物件 (Root Object)", rootObject, typeof(Transform), true);

        GUILayout.Space(15);
        GUILayout.Label("目標代數設定 (Target Generations)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("0 表示根物件本身，1 表示第一代子物件，以此類推。", MessageType.Info);
        
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        EditorGUILayout.PropertyField(generationsProp, new GUIContent("指定代數 (x)"), true);

        GUILayout.Space(15);
        GUILayout.Label("掛載元件設定 (Components to Attach)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("請輸入 Component 的準確類別名稱，例如: Rigidbody, BoxCollider, 或自訂腳本名稱。", MessageType.Info);
        EditorGUILayout.PropertyField(componentsProp, new GUIContent("要掛載的元件 (y)"), true);
        
        EditorGUILayout.EndScrollView();

        so.ApplyModifiedProperties();

        GUILayout.Space(15);

        if (GUILayout.Button("開始掛載 (Attach Components)", GUILayout.Height(30)))
        {
            AttachComponents();
        }
    }

    private void AttachComponents()
    {
        if (rootObject == null)
        {
            Debug.LogWarning("請先選擇一個根物件 (Root Object)！");
            return;
        }

        if (targetGenerations.Count == 0)
        {
            Debug.LogWarning("請至少指定一個目標代數！");
            return;
        }

        if (componentsToAttach.Count == 0)
        {
            Debug.LogWarning("請至少指定一個要掛載的 Component！");
            return;
        }

        // 整理並過濾有效輸入
        HashSet<int> validGenerations = new HashSet<int>(targetGenerations);
        List<System.Type> validComponentTypes = new List<System.Type>();

        foreach (string compName in componentsToAttach)
        {
            if (string.IsNullOrWhiteSpace(compName)) continue;

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
            Debug.LogWarning("沒有找到任何有效的 Component 類型，掛載中止。");
            return;
        }

        int attachedCount = 0;
        int targetFoundCount = 0;

        // 開始尋找符合條件的子物件並掛載
        ProcessTransform(rootObject, 0, validGenerations, validComponentTypes, ref targetFoundCount, ref attachedCount);

        Debug.Log($"掛載完畢！找到 {targetFoundCount} 個符合代數的物件，共掛載了 {attachedCount} 個 Components。");
    }

    // 遞迴處理物件及其子物件
    private void ProcessTransform(Transform currentTransform, int currentGeneration, HashSet<int> validGenerations, List<System.Type> componentTypes, ref int targetFoundCount, ref int attachedCount)
    {
        // 檢查當前代數是否在使用者指定的列表中
        if (validGenerations.Contains(currentGeneration))
        {
            targetFoundCount++;
            foreach (System.Type compType in componentTypes)
            {
                // 檢查是否已經掛載了該類型的元件，避免重複掛載
                if (currentTransform.gameObject.GetComponent(compType) == null)
                {
                    Undo.AddComponent(currentTransform.gameObject, compType);
                    attachedCount++;
                }
            }
        }

        // 遞迴處理下一代
        foreach (Transform child in currentTransform)
        {
            ProcessTransform(child, currentGeneration + 1, validGenerations, componentTypes, ref targetFoundCount, ref attachedCount);
        }
    }

    // 輔助方法：透過字串名稱尋找 System.Type
    private System.Type GetTypeByName(string className)
    {
        // 1. 先嘗試從 UnityEngine 基礎命名空間找 (例如 BoxCollider, Rigidbody)
        System.Type type = System.Type.GetType($"UnityEngine.{className}, UnityEngine");
        if (type != null) return type;

        // 2. 如果找不到，嘗試在當前所有載入的組件 (Assemblies) 中尋找 (例如自訂的 MonoBehaviour 腳本)
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType(className);
            if (type != null) return type;
            
            // 如果還是找不到，可能在某個特定的 namespace 裡，這裡進行模糊搜尋 (較耗時，但對於未加 namespace 的類別有效)
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