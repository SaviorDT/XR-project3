using UnityEngine;
using UnityEditor;

public class GeneratePillars : EditorWindow
{
    [Header("素材設定")]
    public GameObject[] stonePrefabs;

    [Header("巨觀散佈與高度設定")]
    public int spawnCount = 100;
    public float scatterRadius = 500f; // 散佈半徑
    public float groundLevelY = -300f; // 地板高度
    
    [Tooltip("控制『每一根石柱』的整體基礎粗細，讓不同石柱之間有差異")]
    public Vector2 pillarBaseScaleRange = new Vector2(0.9f, 1.2f); // 新增：取代原本寫死的 15 倍

    [Header("微觀單根石柱 - 輪廓設定 (連續函數)")]
    [Tooltip("在基礎粗細上的倍率變化 (Perlin Noise)。")]
    public Vector2 scaleMultiplierRange = new Vector2(0.8f, 1.2f);
    public float scaleNoiseStep = 0.2f;

    [Header("微觀單根石柱 - 位移設定 (2D Noise)")]
    public Vector2 xzOffsetRange = new Vector2(-0.5f, 0.5f);
    public float offsetNoiseStep = 0.1f;

    [Header("變異與密合度設定")]
    public bool enableRandomFlip = true; // 50% 機率上下翻轉
    public float overlapAmount = 0.15f;


    [MenuItem("Tools/Generate Pillars")]
    public static void ShowWindow()
    {
        GetWindow<GeneratePillars>("Generate Pillars");
    }

    void OnGUI()
    {
        GUILayout.Label("沙漠石柱群生成設定", EditorStyles.boldLabel);

        ScriptableObject target = this;
        SerializedObject so = new SerializedObject(target);
        SerializedProperty prefabsProp = so.FindProperty("stonePrefabs");

        EditorGUILayout.PropertyField(prefabsProp, true);
        so.ApplyModifiedProperties();

        EditorGUILayout.Space();
        GUILayout.Label("巨觀散佈規則 (整體群集)", EditorStyles.label);
        spawnCount = EditorGUILayout.IntSlider("生成總柱數", spawnCount, 1, 500);
        scatterRadius = EditorGUILayout.FloatField("散佈半徑", scatterRadius);
        groundLevelY = EditorGUILayout.FloatField("地面 Y 座標 (起點)", groundLevelY);
        pillarBaseScaleRange = EditorGUILayout.Vector2Field("單根整體粗細亂數", pillarBaseScaleRange);

        EditorGUILayout.Space();
        GUILayout.Label("微觀堆疊規則 (單一石柱)", EditorStyles.label);
        scaleMultiplierRange = EditorGUILayout.Vector2Field("XZ 縮放倍率區間", scaleMultiplierRange);
        scaleNoiseStep = EditorGUILayout.Slider("縮放頻率 (Scale Step)", scaleNoiseStep, 0.01f, 2f);
        xzOffsetRange = EditorGUILayout.Vector2Field("XZ 偏移區間", xzOffsetRange);
        offsetNoiseStep = EditorGUILayout.Slider("偏移頻率 (Offset Step)", offsetNoiseStep, 0.01f, 2f);

        EditorGUILayout.Space();
        enableRandomFlip = EditorGUILayout.Toggle("50% 機率上下翻轉", enableRandomFlip);
        overlapAmount = EditorGUILayout.Slider("垂直嵌入深度 (Overlap)", overlapAmount, 0f, 1f);

        GUILayout.Space(20);

        if (GUILayout.Button("生成石柱群!", GUILayout.Height(40)))
        {
            GenerateScatteredPillars();
        }
    }

    private void GenerateScatteredPillars()
    {
        if (stonePrefabs == null || stonePrefabs.Length == 0)
        {
            Debug.LogError("請至少拖入一個石頭 Prefab！");
            return;
        }

        bool hasValidPrefab = false;
        foreach (var prefab in stonePrefabs)
        {
            if (prefab != null) hasValidPrefab = true;
        }
        if (!hasValidPrefab)
        {
            Debug.LogError("石頭列表中都是空缺欄位！");
            return;
        }

        GameObject parentObj = new GameObject("Desert_Pillars_Group");
        Undo.RegisterCreatedObjectUndo(parentObj, "Scatter Pillars");

        int totalStonesUsed = 0;

        try
        {
            for (int i = 0; i < spawnCount; i++)
            {
                EditorUtility.DisplayProgressBar("生成石柱中", $"正在生成第 {i + 1} / {spawnCount} 根石柱...", (float)i / spawnCount);

                // 1. 在半徑內產生隨機 X, Z 座標
                Vector2 randomPos2D = Random.insideUnitCircle * scatterRadius;
                float distanceFromCenter = randomPos2D.magnitude;

                // 2. 計算目標高度
                float normalizedDistance = distanceFromCenter / scatterRadius;
                float heightWeight = 1f - Mathf.SmoothStep(0f, 1f, normalizedDistance);
                float baseHeight = Mathf.Lerp(300f, 600f, heightWeight);
                float finalTargetHeight = baseHeight + Random.Range(-20f, 0f);

                // 3. 【修正點】計算這根石柱的基礎寬度倍率，取代原本的 (15 + ...)
                // 這裡稍微結合一點高度影響：越高的石柱有越高機率稍微粗一點，但嚴格限制在我們設定的範圍內
                float heightRatio = (finalTargetHeight - 300f) / 300f; // 0 ~ 1 之間
                float minBaseScale = Mathf.Lerp(pillarBaseScaleRange.x, pillarBaseScaleRange.y * 0.9f, heightRatio);
                float baseWidthScale = Random.Range(minBaseScale, pillarBaseScaleRange.y);

                // 4. 建立單根石柱的 Root 物件
                GameObject pillarRoot = new GameObject($"Pillar_{i:D3}");
                pillarRoot.transform.parent = parentObj.transform;
                Vector3 pillarStartPos = new Vector3(randomPos2D.x, groundLevelY, randomPos2D.y);
                pillarRoot.transform.position = pillarStartPos;

                // 5. 呼叫微觀生成邏輯
                totalStonesUsed += BuildSinglePillar(pillarRoot.transform, pillarStartPos, finalTargetHeight, baseWidthScale);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        Debug.Log($"成功生成了 {spawnCount} 根石柱！總計堆疊了 {totalStonesUsed} 塊石頭。");
    }

    private int BuildSinglePillar(Transform pillarRoot, Vector3 startPosition, float targetHeight, float baseWidthScale)
    {
        float currentY = startPosition.y;
        
        float scaleNoiseSeed = Random.Range(0f, 1000f);
        float offsetXSeed = Random.Range(0f, 1000f);
        float offsetZSeed = Random.Range(0f, 1000f);

        int stoneCount = 0;
        int maxStones = 500; 

        while ((currentY - startPosition.y) < targetHeight && stoneCount < maxStones)
        {
            GameObject prefab = null;
            while (prefab == null)
            {
                prefab = stonePrefabs[Random.Range(0, stonePrefabs.Length)];
            }

            GameObject stone = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(stone, "Scatter Pillars");
            stone.transform.SetParent(pillarRoot);
            stoneCount++;

            float rotX = (enableRandomFlip && Random.value > 0.5f) ? 180f : 0f;
            float rotY = Random.Range(0f, 360f);
            stone.transform.rotation = Quaternion.Euler(rotX, rotY, 0f);

            Renderer[] renderers = stone.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                DestroyImmediate(stone);
                continue;
            }

            Bounds bounds = GetCombinedBounds(renderers);
            float stoneHeight = bounds.size.y;

            if (stoneHeight <= 0.001f)
            {
                DestroyImmediate(stone);
                break;
            }

            float bottomToPivotOffset = bounds.min.y - stone.transform.position.y;
            float bottomToCenterOffset = bounds.center.y - bounds.min.y;
            float targetCenterY = currentY + bottomToCenterOffset;

            float noiseX = Mathf.PerlinNoise(targetCenterY * offsetNoiseStep, offsetXSeed);
            float noiseZ = Mathf.PerlinNoise(targetCenterY * offsetNoiseStep, offsetZSeed);
            float currentOffsetX = Mathf.Lerp(xzOffsetRange.x, xzOffsetRange.y, noiseX);
            float currentOffsetZ = Mathf.Lerp(xzOffsetRange.x, xzOffsetRange.y, noiseZ);

            float scaleNoise = Mathf.PerlinNoise(targetCenterY * scaleNoiseStep, scaleNoiseSeed);
            float perlinMultiplier = Mathf.Lerp(scaleMultiplierRange.x, scaleMultiplierRange.y, scaleNoise);

            // 套用縮放：原始大小 * 巨觀基礎倍率 * 微觀 Perlin 倍率
            Vector3 originalScale = stone.transform.localScale;
            float finalMultiplierXZ = perlinMultiplier * baseWidthScale;
            
            stone.transform.localScale = new Vector3(
                originalScale.x * finalMultiplierXZ, 
                originalScale.y, 
                originalScale.z * finalMultiplierXZ
            );

            stone.transform.position = new Vector3(
                startPosition.x + currentOffsetX,
                currentY - bottomToPivotOffset,
                startPosition.z + currentOffsetZ
            );

            currentY += (stoneHeight - overlapAmount);
        }

        return stoneCount;
    }

    private Bounds GetCombinedBounds(Renderer[] renderers)
    {
        Bounds bounds = renderers[0].bounds;
        for (int j = 1; j < renderers.Length; j++)
        {
            bounds.Encapsulate(renderers[j].bounds);
        }
        return bounds;
    }
}