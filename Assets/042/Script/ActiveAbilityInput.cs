using UnityEngine;
using UnityEngine.InputSystem;

public class ActiveAbilityInput : MonoBehaviour
{
    [Header("Reference")]
    public PlayerAbilityManager abilityManager;

    [Header("Enable Input")]
    public bool enableInput = true;
    public bool enableOVRInput = true;
    public bool enableKeyboardInput = true;

    void Awake()
    {
        if (abilityManager == null)
        {
            abilityManager = GetComponent<PlayerAbilityManager>();
        }

        if (abilityManager == null)
        {
            Debug.LogWarning("ActiveAbilityInput §ä¤£¨ì PlayerAbilityManager");
        }
    }

    void Update()
    {
        if (!enableInput) return;
        if (abilityManager == null) return;

        if (enableOVRInput)
        {
            HandleMetaQuestInput();
        }

        if (enableKeyboardInput)
        {
            HandleKeyboardInput();
        }
    }

    private void HandleMetaQuestInput()
    {
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
            abilityManager.UseAbility(AbilitySlot.A);

        if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
            abilityManager.UseAbility(AbilitySlot.B);

        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.LTouch))
            abilityManager.UseAbility(AbilitySlot.X);

        if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.LTouch))
            abilityManager.UseAbility(AbilitySlot.Y);
    }

    private void HandleKeyboardInput()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            abilityManager.UseAbility(AbilitySlot.A);

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
            abilityManager.UseAbility(AbilitySlot.B);

        if (Keyboard.current.digit3Key.wasPressedThisFrame)
            abilityManager.UseAbility(AbilitySlot.X);

        if (Keyboard.current.digit4Key.wasPressedThisFrame)
            abilityManager.UseAbility(AbilitySlot.Y);
    }
}