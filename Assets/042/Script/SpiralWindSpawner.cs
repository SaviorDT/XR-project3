using UnityEngine;

public class SpiralWindSpawner : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject spiralCometPrefab;

    [Header("Spawn Timing")]
    public float minSpawnInterval = 0.1f;
    public float maxSpawnInterval = 0.4f;

    [Header("Spawn Area")]
    public float spawnRadius = 0.5f;

    [Header("Randomize Comet")]
    public Vector2 radiusRange = new Vector2(0.5f, 1.2f);
    public Vector2 maxDistanceRange = new Vector2(4f, 7f);
    public Vector2 forwardSpeedRange = new Vector2(3f, 6f);
    public Vector2 rotateSpeedRange = new Vector2(180f, 540f);
    public Vector2 noiseStrengthRange = new Vector2(0.05f, 0.2f);

    private float nextSpawnTime;

    void Start()
    {
        ScheduleNextSpawn();
    }

    void Update()
    {
        if (spiralCometPrefab == null) return;

        if (Time.time >= nextSpawnTime)
        {
            SpawnComet();
            ScheduleNextSpawn();
        }
    }

    private void SpawnComet()
    {
        Vector2 circle = Random.insideUnitCircle * spawnRadius;

        Vector3 localPos = new Vector3(
            circle.x,
            circle.y,
            0f
        );

        GameObject obj = Instantiate(
            spiralCometPrefab,
            transform
        );

        obj.transform.localPosition = localPos;
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;

        SpiralCometTrail comet = obj.GetComponent<SpiralCometTrail>();

        if (comet != null)
        {
            comet.radius = Random.Range(radiusRange.x, radiusRange.y);
            comet.maxDistance = Random.Range(maxDistanceRange.x, maxDistanceRange.y);
            comet.forwardSpeed = Random.Range(forwardSpeedRange.x, forwardSpeedRange.y);
            comet.rotateSpeed = Random.Range(rotateSpeedRange.x, rotateSpeedRange.y);
            comet.noiseStrength = Random.Range(noiseStrengthRange.x, noiseStrengthRange.y);
        }
    }

    private void ScheduleNextSpawn()
    {
        nextSpawnTime = Time.time + Random.Range(minSpawnInterval, maxSpawnInterval);
    }
}