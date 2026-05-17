using UnityEngine;

public class RandomPrefabSpawner : MonoBehaviour
{
    public GameObject prefab;

    public int spawnCount = 50;
    public Vector3 areaSize = new Vector3(100f, 30f, 100f);
    public int maxTryPerObject = 100;

    public LayerMask collisionMask = ~0;

    [Header("Rotation")]
    public bool randomRotationX = false;
    public bool randomRotationY = false;
    public bool randomRotationZ = false;
    public Vector3 fixRotation = new Vector3(0f, 0f, 0f);

    [Header("Scale")]
    public bool randomScale = true;
    public Vector3 fixedScale = new Vector3(1f, 1f, 1f);
    public Vector2 randomXRange = new Vector2(1f, 30f);
    public Vector2 randomYRange = new Vector2(1f, 5f);
    public Vector2 randomZRange = new Vector2(1f, 30f);
    public bool limitXZRatio = true;
    public float maxXZRatio = 4f;

    [Header("Spawn Area")]
    public bool useCustomCenter = false;
    public Vector3 customCenter = Vector3.zero;
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

                Quaternion rotation = GetRandomRotation();

                Vector3 halfExtents = scale * 0.5f;

                bool isOverlapping = Physics.CheckBox(
                    pos,
                    halfExtents,
                    rotation,
                    collisionMask
                );

                if (!isOverlapping)
                {
                    GameObject obj = Instantiate(prefab, pos, rotation);
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

    private Quaternion GetRandomRotation()
    {
        float x = randomRotationX ? Random.Range(0f, 360f) : fixRotation.x;
        float y = randomRotationY ? Random.Range(0f, 360f) : fixRotation.y;
        float z = randomRotationZ ? Random.Range(0f, 360f) : fixRotation.z;

        return Quaternion.Euler(x, y, z);
    }

    private Vector3 GetRandomPlatformScale()
    {
        if (!randomScale)
        {
            return fixedScale;
        }

        float x;
        float z;

        do
        {
            x = Random.Range(randomXRange.x, randomXRange.y);
            z = Random.Range(randomZRange.x, randomZRange.y);
        }
        while (
            limitXZRatio &&
            Mathf.Max(x, z) / Mathf.Min(x, z) > maxXZRatio
        );

        float y = Random.Range(randomYRange.x, randomYRange.y);

        return new Vector3(x, y, z);
    }

    private Vector3 GetRandomPositionInArea(Vector3 scale)
    {
        Vector3 center = useCustomCenter ? customCenter : transform.position;

        float x = Random.Range(
            -areaSize.x / 2f,
             areaSize.x / 2f
        );

        float y = Random.Range(
            -areaSize.y / 2f,
             areaSize.y / 2f
        );

        float z = Random.Range(
            -areaSize.z / 2f,
             areaSize.z / 2f
        );

        return center + new Vector3(x, y, z);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = useCustomCenter ? customCenter : transform.position;
        Gizmos.DrawWireCube(center, areaSize);
    }
}