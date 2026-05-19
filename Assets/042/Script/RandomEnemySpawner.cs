using UnityEngine;

public class RandomEnemySpawner : MonoBehaviour
{
    [Header("Enemy")]
    public GameObject enemyPrefab;
    public Transform player;

    [Header("Spawn Settings")]
    public int spawnCount = 10;
    public Vector3 areaSize = new Vector3(100f, 30f, 100f);
    public int maxTryPerEnemy = 100;

    [Header("Spawn Center")]
    public bool useCustomCenter = false;
    public Vector3 customCenter = Vector3.zero;

    [Header("Collision Check")]
    public Vector3 checkHalfExtents = new Vector3(1f, 1f, 1f);
    public LayerMask collisionMask = ~0;

    [Header("Rotation")]
    public bool randomYRotation = true;

    void Start()
    {
        SpawnEnemies();
    }

    public void SpawnEnemies()
    {
        int spawned = 0;

        for (int i = 0; i < spawnCount; i++)
        {
            bool success = false;

            for (int t = 0; t < maxTryPerEnemy; t++)
            {
                Vector3 pos = GetRandomPositionInArea();
                Quaternion rot = GetSpawnRotation();

                bool isOverlapping = Physics.CheckBox(
                    pos,
                    checkHalfExtents,
                    rot,
                    collisionMask,
                    QueryTriggerInteraction.Ignore
                );

                if (!isOverlapping)
                {
                    GameObject enemy = Instantiate(enemyPrefab, pos, rot);

                    EnemyAI ai = enemy.GetComponent<EnemyAI>();
                    if (ai != null)
                    {
                        ai.player = player;
                    }

                    spawned++;
                    success = true;
                    break;
                }
            }

            if (!success)
            {
                Debug.LogWarning("找不到可生成位置，略過第 " + i + " 個 Enemy");
            }
        }

        Debug.Log("成功生成 Enemy 數量：" + spawned);
    }

    private Vector3 GetRandomPositionInArea()
    {
        Vector3 center = useCustomCenter ? customCenter : transform.position;

        float x = Random.Range(-areaSize.x / 2f, areaSize.x / 2f);
        float y = Random.Range(-areaSize.y / 2f, areaSize.y / 2f);
        float z = Random.Range(-areaSize.z / 2f, areaSize.z / 2f);

        return center + new Vector3(x, y, z);
    }

    private Quaternion GetSpawnRotation()
    {
        if (!randomYRotation)
        {
            return Quaternion.identity;
        }

        return Quaternion.Euler(
            0f,
            Random.Range(0f, 360f),
            0f
        );
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = useCustomCenter ? customCenter : transform.position;
        Gizmos.DrawWireCube(center, areaSize);
    }
}