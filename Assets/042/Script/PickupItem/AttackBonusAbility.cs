using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/Passive/Attack Bonus")]
public class AttackBonusAbility : AbilityBase
{
    [Header("Attack Bonus")]
    public float attackBonusPerLevel = 10f;

    public override void OnGain(PlayerAbilityManager manager, int newLevel)
    {
        if (manager == null) return;
        if (manager.stats == null) return;

        manager.stats.attackBonus += attackBonusPerLevel;

        Debug.Log(
            $"{abilityName} 升到 Lv.{newLevel}，攻擊力增加 {attackBonusPerLevel}"
        );
    }

    public override void OnUse(PlayerAbilityManager manager, int level)
    {
        // 被動能力不用主動施放
    }
}