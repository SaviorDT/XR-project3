using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [Header("¤¬°Ê³]©w")]
    public float interactRange = 3f;
    public LayerMask interactLayer;

    private InteractableObject currentTarget;

    void Update()
    {
        CheckNearbyObject();

        if (currentTarget != null && IsAPressed())
        {
            currentTarget.Interact();
        }
    }

    private void CheckNearbyObject()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            interactRange,
            interactLayer
        );

        InteractableObject nearest = null;
        float nearestDistance = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            InteractableObject interactable =
                hit.GetComponentInParent<InteractableObject>();

            if (interactable == null)
                continue;

            float distance = Vector3.Distance(transform.position, hit.transform.position);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = interactable;
            }
        }

        if (nearest != currentTarget)
        {
            if (currentTarget != null)
                currentTarget.SetHighlight(false);

            currentTarget = nearest;

            if (currentTarget != null)
                currentTarget.SetHighlight(true);
        }
    }

    private bool IsAPressed()
    {
        return OVRInput.GetDown(OVRInput.Button.One);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}