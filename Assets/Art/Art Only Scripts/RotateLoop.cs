using UnityEngine;

public class RotateLoop : MonoBehaviour
{
    public float speed = 90f;
    public bool rotateX = false;
    public bool rotateY = true;
    public bool rotateZ = false;

    void Update()
    {
        float x = rotateX ? speed * Time.deltaTime : 0f;
        float y = rotateY ? speed * Time.deltaTime : 0f;
        float z = rotateZ ? speed * Time.deltaTime : 0f;
        transform.Rotate(x, y, z);
    }
}
