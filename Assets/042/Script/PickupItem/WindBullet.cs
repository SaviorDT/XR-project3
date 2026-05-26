using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WindBullet : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 25f;
    public float lifeTime = 5f;

    [Header("Damage")]
    public float damage = 20f;

    [Header("Hit")]
    public bool destroyOnHit = true;
    public LayerMask hitMask = ~0;

    private Vector3 moveDirection;
    private bool initialized = false;

    public void Init(
        Vector3 direction,
        float newSpeed,
        float newDamage,
        float newLifeTime
    )
    {
        moveDirection = direction.normalized;
        speed = newSpeed;
        damage = newDamage;
        lifeTime = newLifeTime;

        initialized = true;

        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        if (!initialized) return;

        transform.position += moveDirection * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & hitMask) == 0)
        {
            return;
        }

        EnemyPhysics enemy = other.GetComponentInParent<EnemyPhysics>();

        if (enemy != null)
        {
            enemy.DealDamage(damage + FindFirstObjectByType<PlayerStats>().attackBonus);
        }

        if (destroyOnHit)
        {
            Destroy(gameObject);
        }
    }
}