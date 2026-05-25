using UnityEngine;

public class FixRotationY : MonoBehaviour
{
    public void SetRotationY(float y)
    {
        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, -y, transform.rotation.eulerAngles.z);
        Debug.Log($"FixRotationY: Set Y to {-y}");
    }
}
