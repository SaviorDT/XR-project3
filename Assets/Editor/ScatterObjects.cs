using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class PrefabScatterWindow : EditorWindow
{
    private Transform centerObject;
    
    // 加上 SerializeField，讓 SerializedObject 能抓到這個列表
    [SerializeField] private List<GameObject> prefabs = new List<GameObject>();
    
    private Vector3 ellipsoidRadii = new Vector3(5f, 5f, 5f);
    private int spawnCount = 10;
    private float minDistance = 1.5f; 
    
    private Vector2 scrollPos;
    
    // 用於原生繪製 List 的變數
    private SerializedObject so;
    private SerializedProperty prefabsProp;

    [MenuItem("Tools/Prefab Scatterer")]
    public static void ShowWindow()
    {
        GetWindow<PrefabScatterWindow>("Prefab Scatterer");
    }

    private void OnEnable()
    {
        // 綁定目前的視窗實例，並找到 prefabs 這個屬性
        so = new SerializedObject(this);
        prefabsProp = so.FindProperty("prefabs");
    }

    private void OnGUI()
    {
        // 更新 SerializedObject
        so.Update();

        GUILayout.Label("基礎設定 (Base Settings)", EditorStyles.boldLabel);

        centerObject = (Transform)EditorGUILayout.ObjectField("中心物件 (Center Object)", centerObject, typeof(Transform), true);
        ellipsoidRadii = EditorGUILayout.Vector3Field("生成半徑 (X, Y, Z)", ellipsoidRadii);
        spawnCount = EditorGUILayout.IntField("生成嘗試次數", spawnCount);
        minDistance = EditorGUILayout.FloatField("最小間距 (Min Distance)", minDistance);

        GUILayout.Space(15);
        GUILayout.Label("目標物件 (Prefabs)", EditorStyles.boldLabel);
        
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        
        // 使用原生 PropertyField 繪製，支援拖曳功能
        EditorGUILayout.PropertyField(prefabsProp, new GUIContent("可生成的 Prefabs"), true);
        
        EditorGUILayout.EndScrollView();
        
        // 儲存 SerializedObject 的變更
        so.ApplyModifiedProperties();

        GUILayout.Space(15);

        if (GUILayout.Button("開始生成 (Generate)", GUILayout.Height(30)))
        {
            GeneratePrefabs();
        }
    }

    private void GeneratePrefabs()
    {
        if (centerObject == null)
        {
            Debug.LogWarning("請先選擇一個中心物件 (Center Object)！");
            return;
        }

        List<GameObject> validPrefabs = prefabs.FindAll(p => p != null);
        if (validPrefabs.Count == 0)
        {
            Debug.LogWarning("請至少加入一個有效的 Prefab！");
            return;
        }

        int successCount = 0;
        List<Vector3> newlySpawnedPositions = new List<Vector3>();

        for (int i = 0; i < spawnCount; i++)
        {
            GameObject prefabToSpawn = validPrefabs[Random.Range(0, validPrefabs.Count)];

            Vector3 randomPoint = Random.insideUnitSphere;
            Vector3 spawnPos = centerObject.position + new Vector3(
                randomPoint.x * ellipsoidRadii.x,
                randomPoint.y * ellipsoidRadii.y,
                randomPoint.z * ellipsoidRadii.z
            );

            if (IsTooClose(spawnPos, newlySpawnedPositions))
            {
                continue; 
            }

            GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(prefabToSpawn);
            newObj.transform.position = spawnPos;
            newObj.transform.SetParent(centerObject);

            Undo.RegisterCreatedObjectUndo(newObj, "Scatter Prefab");
            
            newlySpawnedPositions.Add(spawnPos);
            successCount++;
        }

        Debug.Log($"生成完畢！嘗試次數：{spawnCount}，成功生成：{successCount} 個物件。");
    }

    private bool IsTooClose(Vector3 targetPos, List<Vector3> newPositions)
    {
        // 檢查本次生成的物件
        foreach (Vector3 pos in newPositions)
        {
            if (Vector3.Distance(targetPos, pos) < minDistance)
            {
                return true;
            }
        }

        // 檢查中心物件下既有的子物件（舊物件）
        if (centerObject != null)
        {
            foreach (Transform child in centerObject)
            {
                if (Vector3.Distance(targetPos, child.position) < minDistance)
                {
                    return true;
                }
            }
        }

        return false;
    }
}