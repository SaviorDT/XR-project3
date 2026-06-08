using UnityEngine;

public class DragonSneeze : MonoBehaviour
{
    public float minInterval = 5f;   // 最短間隔
    public float maxInterval = 8f;   // 最長間隔
    public float delay = 0.8f;
    private bool shoot = false;
    private Animator anim;
    private float timer;
    private float nextTime;
    public ProjectileLauncher shooter;
    void Start()
    {
        anim = GetComponent<Animator>();
        if(shooter == null)
        {
            shooter = GetComponent<ProjectileLauncher>();
        }
        SetNextTime();
    }

    void Update()
    {
        timer += Time.deltaTime;
        if(timer >= delay && shoot == true)
        {
            shoot = false;
            shooter.Fire();
        }
        if (timer >= nextTime)
        {
            Sneeze();
            shoot = true;
            timer = 0f;
            SetNextTime();
        }
    }

    void SetNextTime()
    {
        nextTime = Random.Range(minInterval, maxInterval); // 5~8秒隨機
    }

    void Sneeze()
    {
        int type = Random.Range(0, 2);   // 0 或 1,各 50%
        anim.SetInteger("SneezeType", type);
        anim.SetTrigger("Sneeze");
    }
}