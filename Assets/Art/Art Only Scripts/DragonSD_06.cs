using UnityEngine;

public class DragonSneeze : MonoBehaviour
{
    public float minInterval = 5f;   // 最短間隔
    public float maxInterval = 8f;   // 最長間隔
    private Animator anim;
    private float timer;
    private float nextTime;

    void Start()
    {
        anim = GetComponent<Animator>();
        SetNextTime();
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= nextTime)
        {
            Sneeze();
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