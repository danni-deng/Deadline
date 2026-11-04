using System;
using System.Collections.Generic;
using UnityEngine;

public enum StatusType
{
    AttackUp,
    DefenseUp,
    Regen,
    SpeedUp
}

[Serializable]
public class StatusEffect
{
    public StatusType type;
    public float amount;
    public int turnsLeft;
}

public enum BulletResult
{
    Built,
    Activated,
    Resisted
}

public class Combatant : MonoBehaviour
{
    public const float TurnDistance = 100f;

    public static DebuffRules Rules = new DebuffRules();

    public static event Action<Combatant, int> AnyDamaged;
    public static event Action<Combatant, int> AnyHealed;
    public static event Action<Combatant> AnyDied;
    public static event Action<Combatant> AnyStatusChanged;
    public static event Action<Combatant> AnyBulletsChanged;
    public static event Action<Combatant, BattleColor> AnyDebuffActivated;

    public CombatantData data;
    public bool isPlayerSide;
    public int currentHP;
    public float hideDelay = 0.4f;
    public float charge;
    public int bonusShots;

    public int[] bullets = new int[3];
    public int[] buildup = new int[3];
    public int vulnerableTurns;
    public int weakenedTurns;
    public int interruptedTurns;
    public int resistTurns;

    public int[] cooldowns = new int[0];
    public List<StatusEffect> effects = new List<StatusEffect>();

    public static string DebuffName(BattleColor color)
    {
        if (color == BattleColor.Red)
        {
            return "Vulnerable";
        }
        if (color == BattleColor.Yellow)
        {
            return "Interrupted";
        }
        return "Weakened";
    }

    public static string DebuffDescription(BattleColor color)
    {
        if (color == BattleColor.Red)
        {
            return "Takes increased damage.";
        }
        if (color == BattleColor.Yellow)
        {
            return "Next attack is delayed.";
        }
        return "Deals reduced damage.";
    }

    public bool IsAlive
    {
        get { return currentHP > 0; }
    }

    public bool IsResistant
    {
        get { return resistTurns > 0; }
    }

    public bool stunned
    {
        get { return false; }
    }

    public int BuildupThreshold
    {
        get { return Mathf.Max(1, Rules.buildupThreshold + data.extraBuildup); }
    }

    public float EffectiveSpeed
    {
        get { return Mathf.Max(1f, data.speed * (1f + SumEffects(StatusType.SpeedUp))); }
    }

    public int Speed
    {
        get { return Mathf.RoundToInt(EffectiveSpeed); }
    }

    public float AttackMultiplier
    {
        get
        {
            float multiplier = 1f + SumEffects(StatusType.AttackUp);
            if (weakenedTurns > 0)
            {
                multiplier *= 1f - Rules.weakenedDamagePenalty;
            }
            return multiplier;
        }
    }

    public float DefenseMultiplier
    {
        get
        {
            float multiplier = Mathf.Max(0.25f, 1f + SumEffects(StatusType.DefenseUp));
            if (vulnerableTurns > 0)
            {
                multiplier /= 1f + Rules.vulnerableDamageBonus;
            }
            return multiplier;
        }
    }

    public int bulletsLeft
    {
        get
        {
            int total = 0;
            for (int i = 0; i < bullets.Length; i++)
            {
                total += bullets[i];
            }
            return total;
        }
    }

    void Awake()
    {
        if (data != null)
        {
            Initialize();
        }
    }

    public void Initialize()
    {
        currentHP = data.maxHP;
        bullets = new int[3];
        bullets[(int)data.bulletColorA] = data.bulletsPerColor;
        bullets[(int)data.bulletColorB] = data.bulletsPerColor;
        bonusShots = 0;
        charge = TurnDistance * Rules.startCharge;
        buildup = new int[3];
        vulnerableTurns = 0;
        weakenedTurns = 0;
        interruptedTurns = 0;
        resistTurns = 0;
        cooldowns = new int[data.abilities.Length];
        effects = new List<StatusEffect>();

        NotifyStatus();
        NotifyBullets();
    }

    void NotifyStatus()
    {
        if (AnyStatusChanged != null)
        {
            AnyStatusChanged(this);
        }
    }

    void NotifyBullets()
    {
        if (AnyBulletsChanged != null)
        {
            AnyBulletsChanged(this);
        }
    }

    float SumEffects(StatusType type)
    {
        float total = 0f;
        foreach (StatusEffect e in effects)
        {
            if (e.type == type)
            {
                total += e.amount;
            }
        }
        return total;
    }

    public int LeadingBuildup()
    {
        int best = -1;
        int bestCount = 0;

        for (int i = 0; i < buildup.Length; i++)
        {
            if (buildup[i] > bestCount)
            {
                bestCount = buildup[i];
                best = i;
            }
        }

        return best;
    }

    public int DebuffTurns(BattleColor color)
    {
        if (color == BattleColor.Red)
        {
            return vulnerableTurns;
        }
        if (color == BattleColor.Yellow)
        {
            return interruptedTurns;
        }
        return weakenedTurns;
    }

    public bool AbilityReady(int index)
    {
        if (index < 0 || index >= cooldowns.Length)
        {
            return false;
        }
        if (data.abilities[index] == null)
        {
            return false;
        }
        return cooldowns[index] <= 0;
    }

    public void AddStatus(StatusType type, float amount, int turns, bool appliedOnOwnTurn)
    {
        if (appliedOnOwnTurn)
        {
            turns++;
        }

        foreach (StatusEffect e in effects)
        {
            if (e.type == type && Mathf.Approximately(e.amount, amount))
            {
                e.turnsLeft = Mathf.Max(e.turnsLeft, turns);
                NotifyStatus();
                return;
            }
        }

        StatusEffect fresh = new StatusEffect();
        fresh.type = type;
        fresh.amount = amount;
        fresh.turnsLeft = turns;
        effects.Add(fresh);
        NotifyStatus();
    }

    public void ReduceCooldowns(int amount, AbilityData except)
    {
        for (int i = 0; i < cooldowns.Length; i++)
        {
            if (data.abilities[i] == except)
            {
                continue;
            }
            cooldowns[i] = Mathf.Max(0, cooldowns[i] - amount);
        }
    }

    public int BulletsOf(BattleColor color)
    {
        return bullets[(int)color];
    }

    public bool UseBullet(BattleColor color)
    {
        if (bullets[(int)color] <= 0)
        {
            return false;
        }

        bullets[(int)color]--;
        NotifyBullets();
        return true;
    }

    public BulletResult ApplyBullet(BattleColor color)
    {
        if (resistTurns > 0)
        {
            return BulletResult.Resisted;
        }

        int index = (int)color;
        buildup[index]++;

        if (buildup[index] >= BuildupThreshold)
        {
            ActivateDebuff(color);
            return BulletResult.Activated;
        }

        NotifyStatus();
        return BulletResult.Built;
    }

    void ActivateDebuff(BattleColor color)
    {
        for (int i = 0; i < buildup.Length; i++)
        {
            buildup[i] = 0;
        }

        resistTurns = Rules.resistanceTurns;

        if (color == BattleColor.Red)
        {
            vulnerableTurns = Rules.vulnerableTurns;
        }
        else if (color == BattleColor.Blue)
        {
            weakenedTurns = Rules.weakenedTurns;
        }
        else
        {
            interruptedTurns = Rules.interruptedTurns;
            charge = Mathf.Max(0f, charge - TurnDistance * Rules.interruptDelayPercent);
        }

        Debug.Log(name + " is now " + DebuffName(color));
        NotifyStatus();

        if (AnyDebuffActivated != null)
        {
            AnyDebuffActivated(this, color);
        }
    }

    public void ConsumeStun()
    {
    }

    public void OnTurnStart()
    {
        for (int i = 0; i < cooldowns.Length; i++)
        {
            if (cooldowns[i] > 0)
            {
                cooldowns[i]--;
            }
        }

        foreach (StatusEffect e in effects)
        {
            if (e.type == StatusType.Regen)
            {
                HealBy(Mathf.RoundToInt(data.maxHP * e.amount));
            }
        }
    }

    public void OnTurnEnd()
    {
        for (int i = effects.Count - 1; i >= 0; i--)
        {
            effects[i].turnsLeft--;
            if (effects[i].turnsLeft <= 0)
            {
                effects.RemoveAt(i);
            }
        }

        if (vulnerableTurns > 0)
        {
            vulnerableTurns--;
        }
        if (weakenedTurns > 0)
        {
            weakenedTurns--;
        }
        if (interruptedTurns > 0)
        {
            interruptedTurns--;
        }
        if (resistTurns > 0)
        {
            resistTurns--;
        }

        NotifyStatus();
    }

    public void TakeDamage(int amount)
    {
        currentHP = Mathf.Max(0, currentHP - amount);

        if (AnyDamaged != null)
        {
            AnyDamaged(this, amount);
        }

        if (currentHP == 0)
        {
            if (AnyDied != null)
            {
                AnyDied(this);
            }

            if (!isPlayerSide)
            {
                Invoke("Hide", hideDelay);
            }
        }
    }

    public void HealBy(int amount)
    {
        int before = currentHP;
        currentHP = Mathf.Min(data.maxHP, currentHP + amount);

        if (AnyHealed != null)
        {
            AnyHealed(this, currentHP - before);
        }
    }

    void Hide()
    {
        gameObject.SetActive(false);
    }
}