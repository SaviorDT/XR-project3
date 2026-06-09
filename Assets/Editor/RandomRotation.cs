using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class BatchRandomRotatorWindow : EditorWindow
{
    [Header("目標物件")]
    public GameObject[] targetObjects = new GameObject[0];

    [Header("Step 1: 機率性定角翻轉 (例如: 50% 機率轉 180 度)")]
    public bool enableFlip = true;
    [Range(0f, 1f)] public float flipProbability = 0.5f;
    public Axis flipAxis = Axis.Y;
    public float flipAngle = 180f;

    [Header("Step 2: 範圍隨機旋轉 (例如: 隨機 +-180 度)")]
    public bool enableRandomRotation = true;
    public Axis randomAxis = Axis.X;
    public float randomMin = -180f;
    public float randomMax = 180f;

    [Header("全域設定")]
    public Space rotationSpace = Space.Self; // 讓你可以選擇要依據自身座標還是世界座標旋轉

    public enum Axis { X, Y, Z }

    private SerializedObject so;
    private SerializedProperty targetsProp;
    private Vector2 scrollPos;

    [MenuItem("Tools/批次隨機旋轉工具 (Batch Random Rotator)")]
    public static void ShowWindow()
    {
        GetWindow<BatchRandomRotatorWindow>("批次隨機旋轉");
    }

    private void OnEnable()
    {
        so = new SerializedObject(this);
        targetsProp = so.FindProperty("targetObjects");
    }

    private void OnGUI()
    {
        so.Update();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        GUILayout.Space(10);
        GUILayout.Label("1. 載入目標物件", EditorStyles.boldLabel);
        
        // 畫出陣列，Unity 原生支援將多個 Hierarchy 中的物件直接拖曳到這個欄位的標題上來批次加入
        EditorGUILayout.PropertyField(targetsProp, new GUIContent("目標物件 (可整批拖曳到字上)"), true);
        
        GUILayout.Space(5);
        // 提供一個更方便的按鈕：直接抓取目前 Scene 中選取的所有東西
        if (GUILayout.Button("⏬ 自動載入目前 Scene 中選取的物件", GUILayout.Height(30)))
        {
            targetObjects = Selection.gameObjects;
        }

        GUILayout.Space(15);
        GUILayout.Label("2. 旋轉邏輯設定", EditorStyles.boldLabel);
        
        rotationSpace = (Space)EditorGUILayout.EnumPopup("旋轉座標系", rotationSpace);
        GUILayout.Space(10);

        // --- Step 1 UI ---
        EditorGUILayout.BeginVertical(GUI.skin.box);
        enableFlip = EditorGUILayout.ToggleLeft("啟用 Step 1: 機率性定角翻轉", enableFlip, EditorStyles.boldLabel);
        if (enableFlip)
        {
            EditorGUI.indentLevel++;
            flipProbability = EditorGUILayout.Slider("觸發機率", flipProbability, 0f, 1f);
            flipAxis = (Axis)EditorGUILayout.EnumPopup("翻轉軸向", flipAxis);
            flipAngle = EditorGUILayout.FloatField("翻轉角度", flipAngle);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();

        GUILayout.Space(5);

        // --- Step 2 UI ---
        EditorGUILayout.BeginVertical(GUI.skin.box);
        enableRandomRotation = EditorGUILayout.ToggleLeft("啟用 Step 2: 範圍隨機旋轉", enableRandomRotation, EditorStyles.boldLabel);
        if (enableRandomRotation)
        {
            EditorGUI.indentLevel++;
            randomAxis = (Axis)EditorGUILayout.EnumPopup("隨機軸向", randomAxis);
            randomMin = EditorGUILayout.FloatField("最小角度", randomMin);
            randomMax = EditorGUILayout.FloatField("最大角度", randomMax);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndVertical();

        so.ApplyModifiedProperties();

        GUILayout.Space(20);

        // --- 執行按鈕 ---
        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
        if (GUILayout.Button("執行旋轉 (Apply Rotation)", GUILayout.Height(40)))
        {
            ExecuteRotation();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndScrollView();
    }

    private void ExecuteRotation()
    {
        if (targetObjects == null || targetObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "請先指定至少一個目標物件。", "確定");
            return;
        }

        // 整理出所有有效的 Transform
        List<Transform> validTransforms = new List<Transform>();
        foreach (var obj in targetObjects)
        {
            if (obj != null) validTransforms.Add(obj.transform);
        }

        // 【關鍵】註冊 Undo 狀態：這會在記憶體中拍下這些 Transform 目前的 Snapshot
        Undo.RecordObjects(validTransforms.ToArray(), "Batch Random Rotation");

        // 依序處理每個物件的數學計算
        foreach (Transform t in validTransforms)
        {
            // Step 1: 機率性翻轉
            if (enableFlip && Random.value <= flipProbability)
            {
                t.Rotate(GetAxisVector(flipAxis) * flipAngle, rotationSpace);
            }

            // Step 2: 範圍隨機旋轉
            if (enableRandomRotation)
            {
                float randAngle = Random.Range(randomMin, randomMax);
                t.Rotate(GetAxisVector(randomAxis) * randAngle, rotationSpace);
            }
        }
        
        // 強制 Scene 視圖重繪以立即顯示結果
        SceneView.RepaintAll();
    }

    // 將 Enum 轉換為對應的 Vector3 向量
    private Vector3 GetAxisVector(Axis axis)
    {
        switch (axis)
        {
            case Axis.X: return Vector3.right;
            case Axis.Y: return Vector3.up;
            case Axis.Z: return Vector3.forward;
            default: return Vector3.up;
        }
    }
}