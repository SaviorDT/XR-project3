using UnityEngine;

public class RandomPlatformSpawner : MonoBehaviour
{
    public GameObject prefab;

    public int spawnCount = 50;
    public Vector3 areaSize = new Vector3(100f, 30f, 100f);
    public int maxTryPerObject = 100;

    public LayerMask collisionMask = ~0;

    void Start()
    {
        SpawnObjects();
    }

    public void SpawnObjects()
    {
        int spawned = 0;

        for (int i = 0; i < spawnCount; i++)
        {
            bool success = false;

            for (int t = 0; t < maxTryPerObject; t++)
            {
                Vector3 scale = GetRandomPlatformScale();
                Vector3 pos = GetRandomPositionInArea(scale);

                Vector3 halfExtents = scale * 0.5f;

                bool isOverlapping = Physics.CheckBox(
                    pos,
                    halfExtents,
                    Quaternion.identity,
                    collisionMask
                );

                if (!isOverlapping)
                {
                    GameObject obj = Instantiate(prefab, pos, Quaternion.identity);
                    obj.transform.localScale = scale;
                    Renderer renderer = obj.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material.color = Random.ColorHSV(
                            0f, 1f,
                            0.5f, 1f,
                            0.6f, 1f
                        );
                    }
                    spawned++;
                    success = true;
                    break;
                }
            }

            if (!success)
            {
                Debug.LogWarning("找不到可生成位置，略過第 " + i + " 個平台");
            }
        }

        Debug.Log("成功生成平台數量：" + spawned);
    }

    private Vector3 GetRandomPlatformScale()
    {
        float x;
        float z;

        do
        {
            x = Random.Range(1f, 30f);
            z = Random.Range(1f, 30f);
        }
        while (Mathf.Max(x, z) / Mathf.Min(x, z) > 4f);

        float y = Random.Range(1f, 5f);

        return new Vector3(x, y, z);
    }

    private Vector3 GetRandomPositionInArea(Vector3 scale)
    {
        Vector3 center = transform.position;

        float x = Random.Range(
            -areaSize.x / 2f + scale.x / 2f,
             areaSize.x / 2f - scale.x / 2f
        );

        float y = Random.Range(
            -areaSize.y / 2f + scale.y / 2f,
             areaSize.y / 2f - scale.y / 2f
        );

        float z = Random.Range(
            -areaSize.z / 2f + scale.z / 2f,
             areaSize.z / 2f - scale.z / 2f
        );

        return center + new Vector3(x, y, z);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireCube(transform.position, areaSize);
    }
}