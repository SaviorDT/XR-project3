using UnityEngine;

public class ActiveAbilityInput : MonoBehaviour
{
    [Header("Reference")]
    public PlayerAbilityManager abilityManager;

    [Header("Enable Input")]
    public bool enableInput = true;

    void Awake()
    {
        if (abilityManager == null)
        {
            abilityManager = GetComponent<PlayerAbilityManager>();
        }

        if (abilityManager == null)
        {
            Debug.LogWarning("ActiveAbilityInput 找不到 PlayerAbilityManager");
        }
    }

    void Update()
    {
        if (!enableInput) return;
        if (abilityManager == null) return;

        HandleMetaQuestInput();
    }

    private void HandleMetaQuestInput()
    {
        // A：右手 Button.One
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
        {
            abilityManager.UseAbility(AbilitySlot.A);
        }

        // B：右手 Button.Two
        if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
        {
            abilityManager.UseAbility(AbilitySlot.B);
        }

        // X：左手 Button.One
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.LTouch))
        {
            abilityManager.UseAbility(AbilitySlot.X);
        }

        // Y：左手 Button.Two
        if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.LTouch))
        {
            abilityManager.UseAbility(AbilitySlot.Y);
        }
    }
}