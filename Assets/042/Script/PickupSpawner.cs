using UnityEngine;

public class PickupSpawner : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject pickupPrefab;

    [Header("Abilities")]
    public AbilityBase[] possibleAbilities;

    [Header("Spawn")]
    public int spawnCount = 10;
    public Vector3 areaSize = new Vector3(50f, 10f, 50f);
    public Vector3 spawnCenter = new Vector3(0f, 0f, 0f);

    void Start()
    {
        SpawnPickups();
    }

    public void SpawnPickups()
    {
        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 pos = GetRandomPosition();
            GameObject obj = Instantiate(pickupPrefab, pos, Quaternion.identity);

            PickupItem pickup = obj.GetComponent<PickupItem>();
            if (pickup != null)
            {
                AbilityBase ability = GetRandomAbility();
                pickup.Init(ability);
            }
        }
    }

    private AbilityBase GetRandomAbility()
    {
        if (possibleAbilities == null || possibleAbilities.Length == 0)
            return null;

        int index = Random.Range(0, possibleAbilities.Length);
        return possibleAbilities[index];
    }

    private Vector3 GetRandomPosition()
    {
        Vector3 center = spawnCenter;

        float x = Random.Range(-areaSize.x / 2f, areaSize.x / 2f);
        float y = Random.Range(-areaSize.y / 2f, areaSize.y / 2f);
        float z = Random.Range(-areaSize.z / 2f, areaSize.z / 2f);

        return center + new Vector3(x, y, z);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireCube(transform.position, areaSize);
    }
}