using System.Collections;
using UnityEngine;

public class ProjectileLauncher : MonoBehaviour
{
    public enum FireMode
    {
        MeteorA_Star_Spit,
        MeteorB_Star_Spit,
        MeteorC_Star_Spit
    }

    [Header("發射模式")]
    public FireMode fireMode = FireMode.MeteorA_Star_Spit;

    [Header("Prefab")]
    public MeteorAProjectile meteorAPrefab;
    public MeteorBProjectile meteorBPrefab;
    public MeteorCProjectile meteorCPrefab;
    public StarProjectile starPrefab;
    public SpitProjectile spitPrefab;

    [Header("發射位置")]
    public Transform firePoint;
    public Transform player;

    [Header("隨機數量")]
    public Vector2Int meteorCountRange = new Vector2Int(20, 30);
    public Vector2Int starCountRange = new Vector2Int(15, 20);
    public Vector2Int spitCountRange = new Vector2Int(30, 40);

    [Header("隕石A")]
    public float meteorASpeed = 120f;
    public float meteorASpreadAngle = 70f;

    [Header("隕石B")]
    public Vector2 meteorBSpeedRange = new Vector2(80f, 160f);
    public float meteorBSpreadAngle = 80f;

    [Header("隕石C")]
    public Vector2 meteorCSpeedRange = new Vector2(80f, 140f);
    public float meteorCSpreadAngle = 100f;
    public Vector2 meteorCLockDelayRange = new Vector2(1f, 4f);
    public float meteorCInterval = 0.25f;

    [Header("星星")]
    public Vector2 starSpeedRange = new Vector2(80f, 130f);
    public Vector2 starUpwardSpeedRange = new Vector2(30f, 70f);
    public float starSpreadAngle = 80f;
    public Vector2 starAngularSpeedRange = new Vector2(180f, 540f);

    [Header("口水")]
    public Vector2 spitSpeedRange = new Vector2(80f, 130f);
    public Vector2 spitUpwardSpeedRange = new Vector2(20f, 50f);
    public float spitSpreadAngle = 80f;

    public void Start()
    {
        if(player == null)
        {
            player = FindFirstObjectByType<PlayerFlyController>().transform;
        }
    }
    public void Fire()
    {
        StopAllCoroutines();

        FireMode randomMode = (FireMode)Random.Range(
            0,
            System.Enum.GetValues(typeof(FireMode)).Length
        );

        switch (randomMode)
        {
            case FireMode.MeteorA_Star_Spit:
                FireMeteorAGroup();
                FireStarGroup();
                FireSpitGroup();
                break;

            case FireMode.MeteorB_Star_Spit:
                FireMeteorBGroup();
                FireStarGroup();
                FireSpitGroup();
                break;

            case FireMode.MeteorC_Star_Spit:
                StartCoroutine(FireMeteorCSequence());
                FireStarGroup();
                FireSpitGroup();
                break;
        }
    }

    private void FireMeteorAGroup()
    {
        int count = Random.Range(meteorCountRange.x, meteorCountRange.y + 1);

        for (int i = 0; i < count; i++)
        {
            Vector3 dir = GetRandomDirectionAroundForward(meteorASpreadAngle);

            MeteorAProjectile meteor = Instantiate(
                meteorAPrefab,
                GetFirePosition(),
                Quaternion.LookRotation(dir)
            );

            meteor.Init(dir, meteorASpeed);
        }
    }

    private void FireMeteorBGroup()
    {
        int count = Random.Range(meteorCountRange.x, meteorCountRange.y + 1);

        for (int i = 0; i < count; i++)
        {
            Vector3 startDir = GetRandomDirectionAroundForward(meteorBSpreadAngle);
            float speed = Random.Range(meteorBSpeedRange.x, meteorBSpeedRange.y);

            MeteorBProjectile meteor = Instantiate(
                meteorBPrefab,
                GetFirePosition(),
                Quaternion.LookRotation(startDir)
            );

            meteor.Init(startDir, transform.forward, speed);
        }
    }

    private IEnumerator FireMeteorCSequence()
    {
        int count = Random.Range(meteorCountRange.x, meteorCountRange.y + 1);

        for (int i = 0; i < count; i++)
        {
            Vector3 startDir = GetRandomDirectionAroundForward(meteorCSpreadAngle);
            float speed = Random.Range(meteorCSpeedRange.x, meteorCSpeedRange.y);
            float delay = Random.Range(meteorCLockDelayRange.x, meteorCLockDelayRange.y);

            MeteorCProjectile meteor = Instantiate(
                meteorCPrefab,
                GetFirePosition(),
                Quaternion.LookRotation(startDir)
            );

            meteor.Init(startDir, speed, player, delay);

            yield return new WaitForSeconds(meteorCInterval);
        }
    }

    private void FireStarGroup()
    {
        int count = Random.Range(starCountRange.x, starCountRange.y + 1);

        for (int i = 0; i < count; i++)
        {
            Vector3 dir = GetRandomDirectionAroundForward(starSpreadAngle);
            float speed = Random.Range(starSpeedRange.x, starSpeedRange.y);
            float upwardSpeed = Random.Range(starUpwardSpeedRange.x, starUpwardSpeedRange.y);

            Vector3 angularVelocity = Random.onUnitSphere *
                                      Random.Range(starAngularSpeedRange.x, starAngularSpeedRange.y);

            StarProjectile star = Instantiate(
                starPrefab,
                GetFirePosition(),
                Random.rotation
            );

            star.Init(dir, speed, upwardSpeed, angularVelocity);
        }
    }

    private void FireSpitGroup()
    {
        int count = Random.Range(spitCountRange.x, spitCountRange.y + 1);

        for (int i = 0; i < count; i++)
        {
            Vector3 dir = GetRandomDirectionAroundForward(spitSpreadAngle);
            float speed = Random.Range(spitSpeedRange.x, spitSpeedRange.y);
            float upwardSpeed = Random.Range(spitUpwardSpeedRange.x, spitUpwardSpeedRange.y);

            SpitProjectile spit = Instantiate(
                spitPrefab,
                GetFirePosition(),
                Quaternion.LookRotation(dir)
            );

            spit.Init(dir, speed, upwardSpeed);
        }
    }

    private Vector3 GetFirePosition()
    {
        if (firePoint != null)
        {
            return firePoint.position;
        }

        return transform.position;
    }

    private Vector3 GetRandomDirectionAroundForward(float angle)
    {
        float yaw = Random.Range(-angle, angle);       // 左右
        float pitch = Random.Range(-angle, angle);     // 上下

        Quaternion yawRot = Quaternion.AngleAxis(yaw, transform.up);
        Quaternion pitchRot = Quaternion.AngleAxis(pitch, transform.right);

        Vector3 dir = yawRot * pitchRot * transform.forward;

        return dir.normalized;
    }
}