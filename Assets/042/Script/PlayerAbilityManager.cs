using System.Collections.Generic;
using UnityEngine;

public enum AbilitySlot
{
    A,
    B,
    X,
    Y
}

public class PlayerAbilityManager : MonoBehaviour
{
    [Header("References")]
    public PlayerStats stats;

    private Dictionary<AbilityBase, int> abilityLevels = new Dictionary<AbilityBase, int>();

    private AbilityBase[] activeSlots = new AbilityBase[4];

    void Awake()
    {
        if (stats == null)
            stats = GetComponent<PlayerStats>();
    }

    public void GainAbility(AbilityBase ability)
    {
        if (ability == null) return;

        if (abilityLevels.ContainsKey(ability))
        {
            int newLevel = Mathf.Min(
                abilityLevels[ability] + 1,
                ability.maxLevel
            );

            abilityLevels[ability] = newLevel;
            ability.OnGain(this, newLevel);
            return;
        }

        abilityLevels.Add(ability, 1);
        ability.OnGain(this, 1);

        if (ability.abilityType == AbilityType.Active)
        {
            TryEquipActiveAbility(ability);
        }
    }

    private void TryEquipActiveAbility(AbilityBase ability)
    {
        for (int i = 0; i < activeSlots.Length; i++)
        {
            if (activeSlots[i] == null)
            {
                activeSlots[i] = ability;
                Debug.Log("裝備主動能力到槽位：" + ((AbilitySlot)i));
                return;
            }
        }

        Debug.Log("主動能力槽已滿，能力已獲得但未裝備：" + ability.abilityName);
    }

    public void UseAbility(AbilitySlot slot)
    {
        AbilityBase ability = activeSlots[(int)slot];

        if (ability == null) return;

        int level = abilityLevels[ability];
        ability.OnUse(this, level);
    }

    public int GetAbilityLevel(AbilityBase ability)
    {
        if (abilityLevels.TryGetValue(ability, out int level))
            return level;

        return 0;
    }

    public AbilityBase GetActiveAbility(AbilitySlot slot)
    {
        return activeSlots[(int)slot];
    }
}