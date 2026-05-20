using UnityEngine;

public abstract class PickupEffect : ScriptableObject
{
    public string effectName;
    public GameObject appearancePrefab;

    public abstract void Apply(PlayerStats playerStats);
}