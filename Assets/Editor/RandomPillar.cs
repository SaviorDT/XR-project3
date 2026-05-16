using UnityEngine;
using UnityEditor;

public class ScatterToolWindow : EditorWindow
{
    [Header("石柱模型 (可拖曳多個)")]
    public GameObject[] pillarPrefabs = new GameObject[2]; 

    [Header("生成設定")]
    public int spawnCount = 100;
    public float scatterRadius = 500f; // 直徑 1000 = 半徑 500
    public float groundLevelY = -300f; 

    [MenuItem("Tools/散佈石柱工具 (Scatter Tool)")]
    public static void ShowWindow()
    {
        GetWindow<ScatterToolWindow>("Scatter Tool");
    }

    void OnGUI()
    {
        GUILayout.Label("石柱生成設定", EditorStyles.boldLabel);

        // 使用 SerializedObject 來繪製陣列欄位，讓美術可以拖曳
        ScriptableObject target = this;
        SerializedObject so = new SerializedObject(target);
        SerializedProperty prefabsProp = so.FindProperty("pillarPrefabs");
        
        EditorGUILayout.PropertyField(prefabsProp, true);
        so.ApplyModifiedProperties();

        EditorGUILayout.Space();
        
        spawnCount = EditorGUILayout.IntField("生成總數", spawnCount);
        scatterRadius = EditorGUILayout.FloatField("散佈半徑", scatterRadius);
        groundLevelY = EditorGUILayout.FloatField("地面 Y 座標", groundLevelY);

        EditorGUILayout.Space();

        if (GUILayout.Button("生成石柱群!"))
        {
            GeneratePillars();
        }
    }

    private void GeneratePillars()
    {
        // 防呆檢查
        if (pillarPrefabs == null || pillarPrefabs.Length == 0)
        {
            Debug.LogError("請至少拖入一個石柱 Prefab！");
            return;
        }

        foreach(GameObject prefab in pillarPrefabs)
        {
            if (prefab == null)
            {
                 Debug.LogWarning("注意：石柱列表中有空缺欄位，將跳過該欄位。");
            }
        }

        GameObject parentObj = new GameObject("Pillars_Group");
        Undo.RegisterCreatedObjectUndo(parentObj, "Scatter Pillars");

        for (int i = 0; i < spawnCount; i++)
        {
            // 1. 隨機選擇模型
            GameObject selectedPrefab = null;
            while (selectedPrefab == null)
            {
                 int randomIndex = Random.Range(0, pillarPrefabs.Length);
                 selectedPrefab = pillarPrefabs[randomIndex];
            }

            // 2. 在半徑內產生隨機 X, Z 座標
            Vector2 randomPos2D = Random.insideUnitCircle * scatterRadius;
            float distanceFromCenter = randomPos2D.magnitude;

            // 3. 計算高度 (非線性插值)
            float normalizedDistance = distanceFromCenter / scatterRadius;
            float heightWeight = 1f - Mathf.SmoothStep(0f, 1f, normalizedDistance); 
            float baseHeight = Mathf.Lerp(300f, 600f, heightWeight);
            float finalHeight = baseHeight + Random.Range(-20f, 0f);

            // 4. 計算寬度 (XZ 縮放)
            float widthScale = (15f + (finalHeight / 60f)) + Random.Range(0f, 10f);

            // 5. 生成並設定物件
            GameObject newPillar = (GameObject)PrefabUtility.InstantiatePrefab(selectedPrefab);
            Undo.RegisterCreatedObjectUndo(newPillar, "Scatter Pillars");
            newPillar.transform.parent = parentObj.transform;

            // 設定縮放
            newPillar.transform.localScale = new Vector3(widthScale, finalHeight, widthScale);

            // 6. 計算 Y 軸偏移並設定位置
            float yPos = groundLevelY + (finalHeight / 2f);
            newPillar.transform.position = new Vector3(randomPos2D.x, yPos, randomPos2D.y);

            // 隨機旋轉 (Y軸)
            newPillar.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
        }

        Debug.Log($"成功生成了 {spawnCount} 根石柱！");
    }
}