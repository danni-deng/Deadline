using UnityEngine;

public enum AbilityTarget
{
    SingleEnemy,
    AllEnemies,
    SingleAlly,
    Self
}

[CreateAssetMenu(fileName = "NewAbility", menuName = "Battle/Ability")]
public class AbilityData : ScriptableObject
{
    public string abilityName;
    [TextArea(2, 3)] public string description;
    public AbilityTarget target;
    public int cooldown = 2;

    public float damagePower;
    public float critChance;
    public float critMultiplier = 2.5f;
    public float selfDamagePercent;
    public float healPercent;
    public float regenPercent;
    public int regenTurns;
    public float attackBoost;
    public float defenseBoost;
    public float speedBoost;
    public int buffTurns;
    public int cooldownReduction;
    public int bonusShots;
}