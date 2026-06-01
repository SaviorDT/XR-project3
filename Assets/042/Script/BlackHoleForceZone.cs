using System.Collections.Generic;
using UnityEngine;

public class BlackHoleForceZone : MonoBehaviour
{
    [Header("Black Hole")]
    public float gravityStrength = 30f;
    public float minDistance = 1f;
    public bool useDistanceFalloff = true;

    private Dictionary<PlayerFlyController, Vector3> playerForces = new();
    private Dictionary<Flying, Vector3> flyingForces = new();

    private void OnTriggerStay(Collider other)
    {
        PlayerFlyController player = other.GetComponentInParent<PlayerFlyController>();
        Flying flying = other.GetComponentInParent<Flying>();

        Vector3 force = CalculateBlackHoleForce(other.transform.position);

        if (player != null)
        {
            if (playerForces.TryGetValue(player, out Vector3 oldForce))
            {
                player.SetWindVelocity(-oldForce);
            }

            player.SetWindVelocity(force);
            playerForces[player] = force;
        }
        else if (flying != null)
        {
            if (flyingForces.TryGetValue(flying, out Vector3 oldForce))
            {
                flying.AddForce(-oldForce);
            }

            flying.AddForce(force);
            flyingForces[flying] = force;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerFlyController player = other.GetComponentInParent<PlayerFlyController>();
        Flying flying = other.GetComponentInParent<Flying>();

        if (player != null && playerForces.TryGetValue(player, out Vector3 oldPlayerForce))
        {
            player.SetWindVelocity(-oldPlayerForce);
            playerForces.Remove(player);
        }

        if (flying != null && flyingForces.TryGetValue(flying, out Vector3 oldFlyingForce))
        {
            flying.AddForce(-oldFlyingForce);
            flyingForces.Remove(flying);
        }
    }

    private Vector3 CalculateBlackHoleForce(Vector3 targetPosition)
    {
        Vector3 directionToCenter = transform.position - targetPosition;
        float distance = Mathf.Max(directionToCenter.magnitude, minDistance);

        float finalStrength = gravityStrength;

        if (useDistanceFalloff)
        {
            finalStrength = gravityStrength / (distance * distance);
        }

        return directionToCenter.normalized * finalStrength;
    }
}