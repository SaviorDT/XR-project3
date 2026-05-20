using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Base Stats")]
    public float moveSpeedBonus = 0f;
    public float attackBonus = 0f;
    public float maxSpeedBonus = 0f;
    public float boostForceBonus = 0f;

    public void AddMoveSpeed(float value)
    {
        moveSpeedBonus += value;
    }

    public void AddAttack(float value)
    {
        attackBonus += value;
    }

    public void AddMaxSpeed(float value)
    {
        maxSpeedBonus += value;
    }

    public void AddBoostForce(float value)
    {
        boostForceBonus += value;
    }
}