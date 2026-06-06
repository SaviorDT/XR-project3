using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ObjectSpawner : MonoBehaviour
{
    public enum SpawnMode { Sparse, Medium, Dense }

    [System.Serializable]
    public class PrefabEntry
    {
        public GameObject prefab;
        [Range(1, 10)]
        public int weight = 5;
    }

    [System.Serializable]
    public class SpawnCategory
    {
        public string categoryName;
        public PrefabEntry[] prefabs;

        [Header("Sparse / Medium / Dense 數量範圍")]
        public Vector2Int sparseRange  = new Vector2Int(1, 3);
        public Vector2Int mediumRange  = new Vector2Int(2, 5);
        public Vector2Int denseRange   = new Vector2Int(4, 8);
    }

    [Header("生成設定")]
    public SpawnMode spawnMode = SpawnMode.Medium;
    public float zoneX = 30f;
    public float zoneZ = 30f;

    [Header("Raycast 貼合地形")]
    public bool useRaycast = true;
    public float raycastHeight = 50f;        // 從多高往下打射線
    public LayerMask raycastLayer = ~0;      // 偵測哪些 Layer（預設全部）
    public float surfaceOffset = 0f;         // 貼合後的 Y 偏移（微調用）

    [Header("隨機旋轉 / 縮放")]
    public bool randomRotationY = true;
    [MinMaxRange(0.7f, 2f)]
    public Vector2 scaleRange = new Vector2(0.8f, 1.2f);

    [Header("防重疊")]
    public float minDistance = 1.2f;

    [Header("物件分類")]
    public SpawnCategory[] categories = new SpawnCategory[]
    {
        new SpawnCategory {
            categoryName = "岩石",
            prefabs = new PrefabEntry[0],
            sparseRange  = new Vector2Int(3,  5),
            mediumRange  = new Vector2Int(5,  8),
            denseRange   = new Vector2Int(6, 15)
        },
        new SpawnCategory {
            categoryName = "仙人掌",
            prefabs = new PrefabEntry[0],
            sparseRange  = new Vector2Int(1, 3),
            mediumRange  = new Vector2Int(2, 4),
            denseRange   = new Vector2Int(3, 8)
        },
        new SpawnCategory {
            categoryName = "樹",
            prefabs = new PrefabEntry[0],
            sparseRange  = new Vector2Int(0, 0),
            mediumRange  = new Vector2Int(0, 1),
            denseRange   = new Vector2Int(1, 3)
        },
        new SpawnCategory {
            categoryName = "死樹",
            prefabs = new PrefabEntry[0],
            sparseRange  = new Vector2Int(0, 1),
            mediumRange  = new Vector2Int(0, 2),
            denseRange   = new Vector2Int(1, 4)
        },
        new SpawnCategory {
            categoryName = "骨頭",
            prefabs = new PrefabEntry[0],
            sparseRange  = new Vector2Int(1, 3),
            mediumRange  = new Vector2Int(2, 4),
            denseRange   = new Vector2Int(3, 8)
        },
        new SpawnCategory {
            categoryName = "樹枝",
            prefabs = new PrefabEntry[0],
            sparseRange  = new Vector2Int(1, 3),
            mediumRange  = new Vector2Int(3, 5),
            denseRange   = new Vector2Int(5, 10)
        },
    };

    // ── 內部狀態 ──────────────────────────────
    private const string CONTAINER_NAME = "_SpawnedObjects";
    private List<Vector3> _placedPositions = new List<Vector3>();
    private GameObject _container;

    // ── 公開方法（Editor 按鈕用）─────────────
    public void Generate()
    {
        ClearGenerated();
        _placedPositions.Clear();

        _container = new GameObject(CONTAINER_NAME);
        GameObject container = _container;
        container.transform.SetParent(transform);
        container.transform.localPosition = Vector3.zero;

        foreach (var cat in categories)
        {
            if (cat.prefabs == null || cat.prefabs.Length == 0) continue;

            Vector2Int range = spawnMode == SpawnMode.Sparse  ? cat.sparseRange
                             : spawnMode == SpawnMode.Medium  ? cat.mediumRange
                                                              : cat.denseRange;

            int count = Random.Range(range.x, range.y + 1);

            for (int i = 0; i < count; i++)
            {
                GameObject prefab = PickWeighted(cat.prefabs);
                if (prefab == null) continue;

                Vector3 pos = TryGetPosition(50);
                if (pos == Vector3.positiveInfinity) continue;

                // Raycast 貼合地形
                if (useRaycast)
                {
                    Vector3 rayOrigin = new Vector3(pos.x, transform.position.y + raycastHeight, pos.z); // 每個點用自己的 XZ，Y 從 Spawner 上方往下
                    if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, raycastHeight + 5000f, raycastLayer))
                    {
                        pos = hit.point + Vector3.up * surfaceOffset;
                    }
                    else
                    {
                        // 射線打不到任何東西就跳過這個點
                        continue;
                    }
                }

                _placedPositions.Add(pos);

                float scale = Random.Range(scaleRange.x, scaleRange.y);
                float rotY  = randomRotationY ? Random.Range(0f, 360f) : 0f;

                GameObject go = Instantiate(prefab, pos, Quaternion.Euler(0, rotY, 0), container.transform);
                go.transform.localScale = Vector3.one * scale;
                go.name = prefab.name + "_spawned";

#if UNITY_EDITOR
                Undo.RegisterCreatedObjectUndo(go, "ObjectSpawner Generate");
#endif
            }
        }

        Debug.Log($"[ObjectSpawner] 生成完成，共 {container.transform.childCount} 個物件");

#if UNITY_EDITOR
        Undo.RegisterCreatedObjectUndo(container, "ObjectSpawner Generate");
#endif
    }

    public void ClearGenerated()
    {
        if (_container == null)
        {
            Transform found = transform.Find(CONTAINER_NAME);
            if (found != null) _container = found.gameObject;
        }
        if (_container != null)
        {
#if UNITY_EDITOR
            Undo.DestroyObjectImmediate(_container);
#else
            DestroyImmediate(_container);
#endif
            _container = null;
        }
        _placedPositions.Clear();
    }

    // ── 內部工具 ──────────────────────────────
    private Vector3 TryGetPosition(int maxAttempts)
    {
        float halfX = zoneX * 0.5f;
        float halfZ = zoneZ * 0.5f;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            float x = Random.Range(-halfX, halfX);
            float z = Random.Range(-halfZ, halfZ);
            Vector3 candidate = transform.position + new Vector3(x, 0f, z);

            bool tooClose = false;
            foreach (var p in _placedPositions)
            {
                if (Vector3.Distance(new Vector3(candidate.x, 0, candidate.z),
                                     new Vector3(p.x, 0, p.z)) < minDistance)
                {
                    tooClose = true;
                    break;
                }
            }
            if (!tooClose) return candidate;
        }
        return Vector3.positiveInfinity;
    }

    private GameObject PickWeighted(PrefabEntry[] entries)
    {
        int total = 0;
        foreach (var e in entries) total += e.weight;
        if (total == 0) return null;

        int roll = Random.Range(0, total);
        int cumulative = 0;
        foreach (var e in entries)
        {
            cumulative += e.weight;
            if (roll < cumulative) return e.prefab;
        }
        return entries[entries.Length - 1].prefab;
    }

    // ── Scene 視窗範圍框（Gizmo）─────────────
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.4f);
        Vector3 center = transform.position;
        Gizmos.DrawWireCube(center, new Vector3(zoneX, 0.1f, zoneZ));
    }
}

// ── Editor 腳本 ───────────────────────────
#if UNITY_EDITOR
[CustomEditor(typeof(ObjectSpawner))]
public class ObjectSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ObjectSpawner spawner = (ObjectSpawner)target;

        EditorGUILayout.Space(12);

        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("▶  生成", GUILayout.Height(36)))
        {
            spawner.Generate();
        }

        GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f);
        if (GUILayout.Button("✕  清除已生成", GUILayout.Height(28)))
        {
            spawner.ClearGenerated();
        }

        GUI.backgroundColor = Color.white;
    }
}
#endif
