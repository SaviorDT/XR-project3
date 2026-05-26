using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/Active/Wind Bullet")]
public class WindBulletAbility : AbilityBase
{
    [Header("Wind Bullet")]
    public GameObject windBulletPrefab;

    public float baseDamage = 20f;
    public float damagePerLevel = 10f;

    public float bulletSpeed = 25f;
    public float lifeTime = 5f;

    public float spawnForwardOffset = 1.5f;
    public float spawnUpOffset = 0.3f;

    public override void OnGain(PlayerAbilityManager manager, int newLevel)
    {
    }

    public override void OnUse(PlayerAbilityManager manager, int level)
    {
        if (manager == null) return;
        if (windBulletPrefab == null) return;

        Transform player = manager.transform;

        Camera cam = player.GetComponentInChildren<Camera>();
        Transform aimTransform = cam != null ? cam.transform : player;

        Vector3 shootDirection = aimTransform.forward.normalized;

        Vector3 spawnPos =
            aimTransform.position +
            shootDirection * spawnForwardOffset +
            aimTransform.up * spawnUpOffset;

        Quaternion spawnRot = Quaternion.LookRotation(shootDirection, Vector3.up);

        GameObject obj = Instantiate(
            windBulletPrefab,
            spawnPos,
            spawnRot
        );

        WindBullet bullet = obj.GetComponentInChildren<WindBullet>();

        if (bullet != null)
        {
            float damage = baseDamage + damagePerLevel * (level - 1);

            bullet.Init(
                shootDirection,
                bulletSpeed,
                damage,
                lifeTime
            );
        }
    }
}