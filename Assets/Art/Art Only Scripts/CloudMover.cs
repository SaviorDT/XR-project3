using UnityEngine;

public class CloudMover : MonoBehaviour
{
    public Vector3 moveDirection = new Vector3(-1f, 0f, 1f);
    public float speed = 5f;
    public float destroyDistance = 600f;
    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        transform.position += moveDirection.normalized * speed * Time.deltaTime;

        if (Vector3.Distance(transform.position, startPos) > destroyDistance)
            Destroy(gameObject);
    }
}
