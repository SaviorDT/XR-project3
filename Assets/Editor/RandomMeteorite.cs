using UnityEngine;
using UnityEditor;

public class HollowPipeSpawnerWindow : EditorWindow
{
    // 使用 SerializedObject 讓陣列能在 Editor Window 中正常顯示 Unity 內建的 UI
    public GameObject[] prefabsToSpawn;
    public Transform parentTransform;
    public int spawnCount = 20;

    public float innerRadius = 2f;
    public float outerRadius = 5f;
    public float pipeLength = 10f;
    public PipeAxis axis = PipeAxis.Z;
    public bool showPreview = true;

    public enum PipeAxis { X, Y, Z }

    private SerializedObject so;
    private SerializedProperty prefabsProp;

    [MenuItem("Tools/中空水管生成器 (Hollow Pipe Spawner)")]
    public static void ShowWindow()
    {
        // 打開或聚焦視窗
        GetWindow<HollowPipeSpawnerWindow>("水管生成器");
    }

    private void OnEnable()
    {
        // 初始化 SerializedObject 以便繪製陣列
        so = new SerializedObject(this);
        prefabsProp = so.FindProperty("prefabsToSpawn");

        // 註冊 Scene 視圖的事件，用來畫預覽線框
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        // 視窗關閉時取消註冊，避免報錯
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnGUI()
    {
        so.Update();

        GUILayout.Space(10);
        GUILayout.Label("生成設定", EditorStyles.boldLabel);
        
        // 繪製 Prefab 陣列
        EditorGUILayout.PropertyField(prefabsProp, new GUIContent("生成物件清單 (Prefabs)"), true);
        
        parentTransform = (Transform)EditorGUILayout.ObjectField("父物件 (定位中心)", parentTransform, typeof(Transform), true);
        spawnCount = EditorGUILayout.IntField("生成數量", spawnCount);

        GUILayout.Space(15);
        GUILayout.Label("水管尺寸與方向", EditorStyles.boldLabel);
        innerRadius = EditorGUILayout.FloatField("內徑", innerRadius);
        outerRadius = EditorGUILayout.FloatField("外徑", outerRadius);
        pipeLength = EditorGUILayout.FloatField("長度", pipeLength);
        axis = (PipeAxis)EditorGUILayout.EnumPopup("軸向", axis);

        GUILayout.Space(15);
        showPreview = EditorGUILayout.Toggle("顯示範圍預覽 (Scene 視圖)", showPreview);

        so.ApplyModifiedProperties();

        GUILayout.Space(20);
        
        // 建立一個醒目的生成按鈕
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("生成物件 (Spawn)", GUILayout.Height(40)))
        {
            SpawnObjects();
        }
        GUI.backgroundColor = Color.white; // 恢復預設顏色
    }

    private void SpawnObjects()
    {
        if (prefabsToSpawn == null || prefabsToSpawn.Length == 0)
        {
            EditorUtility.DisplayDialog("錯誤", "請先在清單中加入至少一個 Prefab！", "確定");
            return;
        }

        if (innerRadius > outerRadius)
        {
            float temp = innerRadius;
            innerRadius = outerRadius;
            outerRadius = temp;
        }

        // 決定基準位置與旋轉
        Vector3 centerPos = parentTransform != null ? parentTransform.position : Vector3.zero;
        Quaternion rotation = parentTransform != null ? parentTransform.rotation : Quaternion.identity;

        for (int i = 0; i < spawnCount; i++)
        {
            GameObject selectedPrefab = prefabsToSpawn[Random.Range(0, prefabsToSpawn.Length)];
            if (selectedPrefab == null) continue;

            // 取得局部隨機點
            Vector3 localPos = GetRandomPointInHollowCylinder();
            
            // 轉換為世界座標 (考慮父物件的旋轉與位置)
            Vector3 worldPos = centerPos + rotation * localPos;

            // 實例化物件
            GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(selectedPrefab);
            newObj.transform.position = worldPos;
            newObj.transform.rotation = Random.rotation;

            // 如果有指定父物件，就將其設為子物件
            if (parentTransform != null)
            {
                newObj.transform.SetParent(parentTransform);
            }

            // 註冊 Undo，讓開發者可以 Ctrl + Z 復原生成
            Undo.RegisterCreatedObjectUndo(newObj, "Spawn Hollow Pipe Objects");
        }
    }

    private Vector3 GetRandomPointInHollowCylinder()
    {
        float r = Mathf.Sqrt(Random.Range(innerRadius * innerRadius, outerRadius * outerRadius));
        float theta = Random.Range(0f, Mathf.PI * 2f);
        float lengthPos = Random.Range(-pipeLength / 2f, pipeLength / 2f);

        float circleX = r * Mathf.Cos(theta);
        float circleY = r * Mathf.Sin(theta);

        switch (axis)
        {
            case PipeAxis.X: return new Vector3(lengthPos, circleX, circleY);
            case PipeAxis.Y: return new Vector3(circleX, lengthPos, circleY);
            case PipeAxis.Z: default: return new Vector3(circleX, circleY, lengthPos);
        }
    }

    // 使用 Handles 在 Scene 視圖中畫出預覽
    private void OnSceneGUI(SceneView sceneView)
    {
        if (!showPreview) return;

        Vector3 centerPos = parentTransform != null ? parentTransform.position : Vector3.zero;
        Quaternion rotation = parentTransform != null ? parentTransform.rotation : Quaternion.identity;

        Vector3 normal;
        Vector3 offset1, offset2;

        switch (axis)
        {
            case PipeAxis.X:
                normal = rotation * Vector3.right;
                offset1 = rotation * new Vector3(-pipeLength / 2f, 0, 0);
                offset2 = rotation * new Vector3(pipeLength / 2f, 0, 0);
                break;
            case PipeAxis.Y:
                normal = rotation * Vector3.up;
                offset1 = rotation * new Vector3(0, -pipeLength / 2f, 0);
                offset2 = rotation * new Vector3(0, pipeLength / 2f, 0);
                break;
            case PipeAxis.Z:
            default:
                normal = rotation * Vector3.forward;
                offset1 = rotation * new Vector3(0, 0, -pipeLength / 2f);
                offset2 = rotation * new Vector3(0, 0, pipeLength / 2f);
                break;
        }

        Vector3 end1 = centerPos + offset1;
        Vector3 end2 = centerPos + offset2;

        // 畫外圈 (Cyan)
        Handles.color = Color.cyan;
        Handles.DrawWireDisc(end1, normal, outerRadius);
        Handles.DrawWireDisc(end2, normal, outerRadius);
        Handles.DrawLine(end1 + rotation * GetPerpendicular(normal) * outerRadius, end2 + rotation * GetPerpendicular(normal) * outerRadius);
        Handles.DrawLine(end1 - rotation * GetPerpendicular(normal) * outerRadius, end2 - rotation * GetPerpendicular(normal) * outerRadius);

        // 畫內圈 (Red)
        Handles.color = Color.red;
        Handles.DrawWireDisc(end1, normal, innerRadius);
        Handles.DrawWireDisc(end2, normal, innerRadius);
    }

    // 取得垂直於法線的向量，用來畫圓柱體的側邊連線
    private Vector3 GetPerpendicular(Vector3 normal)
    {
        if (normal == Vector3.up || normal == Vector3.down)
            return Vector3.right;
        return Vector3.up;
    }
}