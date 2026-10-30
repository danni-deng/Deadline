using UnityEngine;

public enum AbilityTarget
{
    SingleEnemy,
    AllEnemies,
    SingleAlly,
    Self
}

[System.Serializable]
public class AbilityTier
{
    public string note;
    public float damagePower;
    public float critChance;
    public float critMultiplier = 2.5f;
    public float missingHpBonus;
    public float selfDamagePercent;
    public float healPercent;
    public float regenPercent;
    public int regenTurns;
    public float attackBoost;
    public float defenseBoost;
    public float speedBoost;
    public int buffTurns;
    public float defenseDropChance;
    public float defenseDropPercent = 0.2f;
    public int defenseDropTurns = 2;
    public int cooldownReduction;
    public bool resetShortestCooldown;
    public bool grantStack;
    public BattleColor stackColor;
    public bool hitAllAllies;
    public bool extraTurn;
}

[CreateAssetMenu(fileName = "NewAbility", menuName = "Battle/Ability")]
public class AbilityData : ScriptableObject
{
    public string abilityName;
    public AbilityTarget target;
    public int cooldown = 2;
    public bool anyColor;
    public BattleColor[] stackColors = new BattleColor[0];
    public AbilityTier[] tiers = new AbilityTier[4];

    public AbilityTier GetTier(int stackCount)
    {
        if (tiers == null || tiers.Length == 0)
        {
            return new AbilityTier();
        }

        int index = Mathf.Clamp(stackCount, 0, tiers.Length - 1);
        if (tiers[index] == null)
        {
            return new AbilityTier();
        }
        return tiers[index];
    }
}