using UnityEngine;
using System.Collections.Generic;

public class CloudSpawner : MonoBehaviour
{
    [Header("雲朵 Mesh（最多4種）")]
    public Mesh[] cloudMeshes = new Mesh[4];
    public Material cloudMat;

    [Header("生成位置設定")]
    [Tooltip("生成線的長度（沿生成軸方向）")]
    public float spawnLineLength = 600f;
    [Tooltip("生成線的中心偏移（相對於此物件位置）")]
    public Vector3 spawnOffset = new Vector3(300f, 0f, -300f);

    [Header("高度範圍")]
    public float minHeight = 80f;
    public float maxHeight = 130f;

    [Header("縮放範圍")]
    public float minScale = 5f;
    public float maxScale = 20f;

    [Header("飄移方向與速度")]
    public Vector3 moveDirection = new Vector3(-1f, 0f, 1f);
    public float minSpeed = 3f;
    public float maxSpeed = 8f;

    [Header("生成數量控制")]
    [Tooltip("場景中最多同時存在幾朵雲")]
    public int maxClouds = 40;
    [Tooltip("每幾秒生成一朵")]
    public float spawnInterval = 1.5f;
    [Tooltip("超過這個距離後銷毀雲朵")]
    public float destroyDistance = 700f;

    private float timer = 0f;
    private List<GameObject> activeClouds = new List<GameObject>();

    void Update()
    {
        // 清除已被銷毀的參考
        activeClouds.RemoveAll(c => c == null);

        if (activeClouds.Count >= maxClouds) return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnCloud();
        }
    }

    void SpawnCloud()
    {
        // 選有效的 Mesh
        List<int> validIdx = new List<int>();
        for (int i = 0; i < cloudMeshes.Length; i++)
            if (cloudMeshes[i] != null) validIdx.Add(i);
        if (validIdx.Count == 0) return;

        int idx = validIdx[Random.Range(0, validIdx.Count)];
        Mesh mesh = cloudMeshes[idx];

        // 在生成線上隨機位置
        Vector3 spawnCenter = transform.position + spawnOffset;
        // 生成線垂直於飄移方向，在XZ平面上
        Vector3 dir = moveDirection.normalized;
        Vector3 perp = new Vector3(-dir.z, 0f, dir.x); // 垂直方向
        float t = Random.Range(-spawnLineLength * 0.5f, spawnLineLength * 0.5f);
        float y = Random.Range(minHeight, maxHeight);
        Vector3 spawnPos = spawnCenter + perp * t;
        spawnPos.y = y;

        // 建立 GameObject
        GameObject cloud = new GameObject("Cloud_" + idx);
        cloud.transform.position = spawnPos;

        // 隨機旋轉 Y 軸
        cloud.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        // 隨機縮放
        float s = Random.Range(minScale, maxScale);
        cloud.transform.localScale = Vector3.one * s;

        // 加 MeshFilter 和 MeshRenderer
        MeshFilter mf = cloud.AddComponent<MeshFilter>();
        mf.mesh = mesh;
        MeshRenderer mr = cloud.AddComponent<MeshRenderer>();
        mr.material = cloudMat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        // 加 CloudMover
        CloudMover mover = cloud.AddComponent<CloudMover>();
        mover.moveDirection = moveDirection;
        mover.speed = Random.Range(minSpeed, maxSpeed);
        mover.destroyDistance = destroyDistance;

        activeClouds.Add(cloud);
    }

    // Scene 視窗顯示生成線
    void OnDrawGizmosSelected()
    {
        Vector3 dir = moveDirection.normalized;
        Vector3 perp = new Vector3(-dir.z, 0f, dir.x);
        Vector3 center = transform.position + spawnOffset;
        center.y = (minHeight + maxHeight) * 0.5f;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(center - perp * spawnLineLength * 0.5f, center + perp * spawnLineLength * 0.5f);
        Gizmos.DrawSphere(center, 5f);

        // 飄移方向箭頭
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(center, center + dir * 80f);
    }
}
