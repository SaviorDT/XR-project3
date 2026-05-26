using UnityEngine;

public enum AbilityType
{
    Passive,
    Active,
    Item
}

public abstract class AbilityBase : ScriptableObject
{
    public string abilityName;
    public AbilityType abilityType;
    public GameObject appearancePrefab;

    [Header("Level")]
    public int maxLevel = 5;

    public abstract void OnGain(PlayerAbilityManager manager, int newLevel);
    public virtual void OnUse(PlayerAbilityManager manager, int level) { }
}