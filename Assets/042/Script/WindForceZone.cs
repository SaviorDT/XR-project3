using UnityEngine;

public class WindForceZone : MonoBehaviour
{
    [Header("Wind")]
    public Vector3 localWindDirection = Vector3.down;
    public float windStrength = 20f;

    private Vector3 appliedForce;
    void Awake()
    {
        Vector3 worldDirection = transform.TransformDirection(localWindDirection).normalized;
        appliedForce = worldDirection * windStrength;
    }
    private void OnTriggerEnter(Collider other)
    {
        PlayerFlyController player = other.GetComponentInParent<PlayerFlyController>();
        Flying test = other.GetComponentInParent<Flying>();
        if (player == null)
            if (test == null)
                return;
            else
                test.AddForce(appliedForce);
        else
            player.SetWindVelocity(appliedForce);
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerFlyController player = other.GetComponentInParent<PlayerFlyController>();
        Flying test = other.GetComponentInParent<Flying>();
        if (player == null)
            if (test == null)
                return;
            else
                test.AddForce(-appliedForce);
        else
            player.SetWindVelocity(-appliedForce);

    }
}