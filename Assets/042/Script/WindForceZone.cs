using UnityEngine;

public class WindForceZone : MonoBehaviour
{
    [Header("Wind")]
    public Vector3 localWindDirection = Vector3.down;
    public float windStrength = 20f;

    private Flying currentPlayer;
    private Vector3 appliedForce;

    private void OnTriggerEnter(Collider other)
    {
        Flying player = other.GetComponentInParent<Flying>();
        if (player == null) return;

        currentPlayer = player;

        Vector3 worldDirection = transform.TransformDirection(localWindDirection).normalized;
        appliedForce = worldDirection * windStrength;

        currentPlayer.AddForce(appliedForce);
    }

    private void OnTriggerExit(Collider other)
    {
        Flying player = other.GetComponentInParent<Flying>();
        if (player == null) return;
        if (player != currentPlayer) return;

        currentPlayer.RemoveForce(appliedForce);

        currentPlayer = null;
        appliedForce = Vector3.zero;
    }
}