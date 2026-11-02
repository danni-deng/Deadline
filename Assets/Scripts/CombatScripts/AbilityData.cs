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
    public bool inheritLowerTiers = true;
    public AbilityTier[] tiers = new AbilityTier[4];

    public AbilityTier GetTier(int stackCount)
    {
        if (tiers == null || tiers.Length == 0)
        {
            return new AbilityTier();
        }

        int index = Mathf.Clamp(stackCount, 0, tiers.Length - 1);

        if (!inheritLowerTiers)
        {
            return tiers[index] != null ? tiers[index] : new AbilityTier();
        }

        AbilityTier result = new AbilityTier();
        for (int i = 0; i <= index; i++)
        {
            if (tiers[i] != null)
            {
                ApplyLayer(result, tiers[i]);
            }
        }
        return result;
    }

    void ApplyLayer(AbilityTier result, AbilityTier layer)
    {
        if (layer.damagePower > 0f) { result.damagePower = layer.damagePower; }
        if (layer.critChance > 0f) { result.critChance = layer.critChance; }
        if (layer.missingHpBonus > 0f) { result.missingHpBonus = layer.missingHpBonus; }
        if (layer.selfDamagePercent > 0f) { result.selfDamagePercent = layer.selfDamagePercent; }
        if (layer.healPercent > 0f) { result.healPercent = layer.healPercent; }
        if (layer.regenPercent > 0f) { result.regenPercent = layer.regenPercent; }
        if (layer.regenTurns > 0) { result.regenTurns = layer.regenTurns; }
        if (layer.attackBoost > 0f) { result.attackBoost = layer.attackBoost; }
        if (layer.defenseBoost > 0f) { result.defenseBoost = layer.defenseBoost; }
        if (layer.speedBoost > 0f) { result.speedBoost = layer.speedBoost; }
        if (layer.buffTurns > 0) { result.buffTurns = layer.buffTurns; }
        if (layer.defenseDropChance > 0f) { result.defenseDropChance = layer.defenseDropChance; }
        if (layer.cooldownReduction > 0) { result.cooldownReduction = layer.cooldownReduction; }
        if (layer.resetShortestCooldown) { result.resetShortestCooldown = true; }
        if (layer.hitAllAllies) { result.hitAllAllies = true; }
        if (layer.extraTurn) { result.extraTurn = true; }

        if (layer.grantStack)
        {
            result.grantStack = true;
            result.stackColor = layer.stackColor;
        }

        result.critMultiplier = layer.critMultiplier;
        result.defenseDropPercent = layer.defenseDropPercent;
        result.defenseDropTurns = layer.defenseDropTurns;
    }
}