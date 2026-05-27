using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public class RandomWind : EditorWindow
{
    [SerializeField]
    private GameObject windPrefab;
    [SerializeField]
    private GameObject[] Pillars;

    private int coverPillarCount = 10;
    private int pillarTopDeg90Count = 5;
    private int pillarTopDeg75Count = 8;
    private int pillarTopDeg60Count = 5;
    private int pillarTopDeg45Count = 5;
    private int pillarSideDeg90Count = 10;
    private int pillarSideDeg60Count = 20;
    private int pillarSideDeg45Count = 20;
    private int pillarSideDeg30Count = 20;
    private int pillarSideDeg15Count = 20;
    private int pillarSideDeg0Count = 20;
    public float minY = -100.0f; 

    [MenuItem("Tools/散佈風柱工具 (Scatter Tool)")]
    public static void ShowWindow()
    {
        GetWindow<RandomWind>("Scatter Tool");
    }

    void OnGUI()
    {
        GUILayout.Label("風柱生成設定", EditorStyles.boldLabel);

        // 使用 SerializedObject 來繪製欄位，讓美術可以拖曳
        ScriptableObject target = this;
        SerializedObject so = new(target);
        so.Update();

        SerializedProperty prefabProp = so.FindProperty("windPrefab");
        SerializedProperty pillarsProp = so.FindProperty("Pillars");

        EditorGUILayout.PropertyField(prefabProp, true);
        EditorGUILayout.PropertyField(pillarsProp, true);
        so.ApplyModifiedProperties();

        


        EditorGUILayout.Space();
        
        coverPillarCount = EditorGUILayout.IntField("覆蓋石柱數量", coverPillarCount);
        pillarTopDeg90Count = EditorGUILayout.IntField("石柱頂部 90 度數量", pillarTopDeg90Count);
        pillarTopDeg75Count = EditorGUILayout.IntField("石柱頂部 75 度數量", pillarTopDeg75Count);
        pillarTopDeg60Count = EditorGUILayout.IntField("石柱頂部 60 度數量", pillarTopDeg60Count);
        pillarTopDeg45Count = EditorGUILayout.IntField("石柱頂部 45 度數量", pillarTopDeg45Count);
        pillarSideDeg90Count = EditorGUILayout.IntField("石柱側面 90 度數量", pillarSideDeg90Count);
        pillarSideDeg60Count = EditorGUILayout.IntField("石柱側面 60 度數量", pillarSideDeg60Count);
        pillarSideDeg45Count = EditorGUILayout.IntField("石柱側面 45 度數量", pillarSideDeg45Count);
        pillarSideDeg30Count = EditorGUILayout.IntField("石柱側面 30 度數量", pillarSideDeg30Count);
        pillarSideDeg15Count = EditorGUILayout.IntField("石柱側面 15 度數量", pillarSideDeg15Count);
        pillarSideDeg0Count = EditorGUILayout.IntField("石柱側面 0 度數量", pillarSideDeg0Count);
        minY = EditorGUILayout.FloatField("最小 Y 座標", minY);

        EditorGUILayout.Space();

        EditorGUILayout.Space();

        if (GUILayout.Button("從場景選取設定為 Pillars (Use Selection)"))
        {
            // 將目前選取的場景物件設定為 Pillars
            GameObject[] selected = Selection.gameObjects;
            so.Update();
            pillarsProp = so.FindProperty("Pillars");
            pillarsProp.arraySize = selected.Length;
            for (int i = 0; i < selected.Length; i++)
            {
                pillarsProp.GetArrayElementAtIndex(i).objectReferenceValue = selected[i];
            }
            so.ApplyModifiedProperties();
            Repaint();
        }

        if (GUILayout.Button("生成風柱群!"))
        {
            GenerateWinds();
        }
    }

    private void GenerateWinds()
    {
        // 防呆檢查
        if (windPrefab == null)
        {
            Debug.LogError("請至少拖入一個風柱 Prefab！");
            return;
        }
        if (Pillars == null || Pillars.Length == 0)
        {
            Debug.LogError("請至少在 Pillars 填入一個石柱物件或使用 Use Selection 設定。");
            return;
        }

        var pillars = CollectPillars();
        if (pillars == null || pillars.Count == 0)
        {
            Debug.LogError("找不到任何可用的石柱資訊。");
            return;
        }

        // --- Cover ---
        int coverCount = Mathf.Clamp(coverPillarCount, 0, pillars.Count);
        // shuffle indices
        List<int> idx = new List<int>(pillars.Count);
        for (int i = 0; i < pillars.Count; i++) idx.Add(i);
        for (int i = 0; i < idx.Count; i++)
        {
            int j = Random.Range(i, idx.Count);
            int t = idx[i]; idx[i] = idx[j]; idx[j] = t;
        }
        for (int i = 0; i < coverCount; i++)
        {
            var pi = pillars[idx[i]];
            var inst = InstantiateWindPrefab();
            if (inst == null) continue;
            inst.transform.position = new Vector3(pi.center.x, 0f, pi.center.z);
            inst.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            inst.transform.localScale = new Vector3(pi.height + 20f, pi.radius + 10f, pi.radius + 10f);
        }

        // --- Top ---
        int[] topDegrees = new int[] { 90, 75, 60, 45 };
        int[] topCounts = new int[] { pillarTopDeg90Count, pillarTopDeg75Count, pillarTopDeg60Count, pillarTopDeg45Count };
        for (int d = 0; d < topDegrees.Length; d++)
        {
            int deg = topDegrees[d];
            int count = Mathf.Max(0, topCounts[d]);
            for (int i = 0; i < count; i++)
            {
                var pi = pillars[Random.Range(0, pillars.Count)];
                var inst = InstantiateWindPrefab();
                if (inst == null) continue;
                inst.transform.position = new Vector3(pi.center.x, pi.highestY, pi.center.z);
                float ry = Random.Range(0f, 360f);
                inst.transform.rotation = Quaternion.Euler(-deg, ry, 0f);
                float a = Random.Range(4f, 6f);
                float b = Random.Range(5f, 8f);
                inst.transform.localScale = new Vector3(a, a, b);
            }
        }

        // --- Side ---
        int[] sideDegrees = new int[] { 90, 60, 45, 30, 15, 0 };
        int[] sideCounts = new int[] { pillarSideDeg90Count, pillarSideDeg60Count, pillarSideDeg45Count, pillarSideDeg30Count, pillarSideDeg15Count, pillarSideDeg0Count };
        for (int d = 0; d < sideDegrees.Length; d++)
        {
            int deg = sideDegrees[d];
            int count = Mathf.Max(0, sideCounts[d]);
            for (int i = 0; i < count; i++)
            {
                var pi = pillars[Random.Range(0, pillars.Count)];
                float height = Random.Range(-50f, pi.height);
                float rotationY = Random.Range(0f, 360f);
                // place on circumference
                Vector3 dir = Quaternion.Euler(0f, rotationY, 0f) * Vector3.forward;
                Vector3 pos = new Vector3(pi.center.x, height, pi.center.z) + new Vector3(dir.x, 0f, dir.z) * pi.radius;

                var inst = InstantiateWindPrefab();
                if (inst == null) continue;
                inst.transform.position = pos;
                float ry = rotationY + Random.Range(-30f, 30f);
                inst.transform.rotation = Quaternion.Euler(-deg, ry, 0f);
                float a = Random.Range(4f, 6f);
                float b = Random.Range(5f, 8f);
                inst.transform.localScale = new Vector3(a, a, b);
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"GenerateWinds: 已生成 Cover={coverCount}, Top total={topCounts.Sum()}, Side total={sideCounts.Sum()}");
    }

    // Helper data for each pillar approximation
    private class PillarInfo
    {
        public GameObject go;
        public Vector3 center; // x,z center in world space
        public float highestY;
        public float height;
        public float radius;
    }

    private List<PillarInfo> CollectPillars()
    {
        var result = new List<PillarInfo>();
        if (Pillars == null || Pillars.Length == 0) return result;

        foreach (var p in Pillars)
        {
            if (p == null) continue;

            // Aggregate collider bounds to get extents and highest point
            var cols = p.GetComponentsInChildren<Collider>();
            if (cols == null || cols.Length == 0)
            {
                // fallback to transform bounds
                var b = new Bounds(p.transform.position, Vector3.zero);
                var pi = new PillarInfo { go = p, center = p.transform.position, highestY = p.transform.position.y, height = p.transform.position.y + 50f, radius = 1f };
                result.Add(pi);
                continue;
            }

            float minX = float.MaxValue, minZ = float.MaxValue;
            float maxX = float.MinValue, maxZ = float.MinValue;
            float maxY = float.MinValue;
            Bounds combined = cols[0].bounds;
            foreach (var c in cols)
            {
                combined.Encapsulate(c.bounds);
                var b = c.bounds;
                minX = Mathf.Min(minX, b.min.x);
                minZ = Mathf.Min(minZ, b.min.z);
                maxX = Mathf.Max(maxX, b.max.x);
                maxZ = Mathf.Max(maxZ, b.max.z);
                maxY = Mathf.Max(maxY, b.max.y);
            }

            Vector3 center = new Vector3((minX + maxX) * 0.5f, combined.center.y, (minZ + maxZ) * 0.5f);

            // Estimate radius by sampling mesh vertices (up to 500)
            List<float> dists = new List<float>(256);
            var mfilters = p.GetComponentsInChildren<MeshFilter>();
            foreach (var mf in mfilters)
            {
                var mesh = mf.sharedMesh;
                if (mesh == null) continue;
                var verts = mesh.vertices;
                for (int i = 0; i < verts.Length && dists.Count < 500; i++)
                {
                    Vector3 wv = mf.transform.TransformPoint(verts[i]);
                    float d = Vector2.Distance(new Vector2(wv.x, wv.z), new Vector2(center.x, center.z));
                    dists.Add(d);
                }
            }

            // also try mesh colliders if any
            var mcols = p.GetComponentsInChildren<MeshCollider>();
            foreach (var mc in mcols)
            {
                var mesh = mc.sharedMesh;
                if (mesh == null) continue;
                var verts = mesh.vertices;
                for (int i = 0; i < verts.Length && dists.Count < 500; i++)
                {
                    Vector3 wv = mc.transform.TransformPoint(verts[i]);
                    float d = Vector2.Distance(new Vector2(wv.x, wv.z), new Vector2(center.x, center.z));
                    dists.Add(d);
                }
            }

            float radius = 1f;
            if (dists.Count > 0)
            {
                float sum = 0f;
                foreach (var vv in dists) sum += vv;
                radius = Mathf.Max(0.1f, sum / dists.Count);
            }
            else
            {
                // fallback to bounds extents
                radius = Mathf.Max(combined.extents.x, combined.extents.z);
            }

            var pi2 = new PillarInfo
            {
                go = p,
                center = new Vector3(center.x, combined.center.y, center.z),
                highestY = maxY,
                height = maxY + 50f,
                radius = radius
            };

            result.Add(pi2);
        }

        return result;
    }

    private GameObject InstantiateWindPrefab()
    {
        GameObject inst = null;
        if (windPrefab == null) return null;
        if (PrefabUtility.IsPartOfPrefabAsset(windPrefab))
        {
            var obj = PrefabUtility.InstantiatePrefab(windPrefab);
            inst = obj as GameObject;
        }
        if (inst == null)
        {
            inst = GameObject.Instantiate(windPrefab);
        }
        Undo.RegisterCreatedObjectUndo(inst, "Generate Wind");
        return inst;
    }
}