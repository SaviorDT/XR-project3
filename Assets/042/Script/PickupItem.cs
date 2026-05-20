using UnityEngine;

public class PickupItem : MonoBehaviour
{
    [Header("Visual")]
    public Transform visualRoot;

    private AbilityBase selectedAbility;
    private GameObject currentVisual;

    public void Init(AbilityBase ability)
    {
        selectedAbility = ability;
        ApplyAppearance();
    }

    private void ApplyAppearance()
    {
        if (selectedAbility == null) return;
        if (selectedAbility.appearancePrefab == null) return;

        Transform parent = visualRoot != null ? visualRoot : transform;

        if (currentVisual != null)
        {
            Destroy(currentVisual);
        }

        currentVisual = Instantiate(
            selectedAbility.appearancePrefab,
            parent.position,
            parent.rotation,
            parent
        );

        currentVisual.transform.localPosition = Vector3.zero;
        currentVisual.transform.localRotation = Quaternion.identity;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerAbilityManager manager =
            other.GetComponentInParent<PlayerAbilityManager>();

        if (manager == null) return;
        if (selectedAbility == null) return;

        manager.GainAbility(selectedAbility);

        Destroy(gameObject);
    }
}