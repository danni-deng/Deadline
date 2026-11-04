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

public class Combatant : MonoBehaviour
{
    public const float TurnDistance = 100f;

    public static DebuffRules Rules = new DebuffRules();

    public static event Action<Combatant, int> AnyDamaged;
    public static event Action<Combatant, int> AnyHealed;
    public static event Action<Combatant> AnyDied;
    public static event Action<Combatant> AnyStatusChanged;
    public static event Action<Combatant> AnyBulletsChanged;

    public CombatantData data;
    public bool isPlayerSide;
    public int currentHP;
    public float hideDelay = 0.4f;
    public float charge;
    public int bonusShots;

    public int[] bullets = new int[3];
    public int bleedStacks;
    public int slowStacks;
    public int stunBuildup;
    public bool stunned;
    public int stunImmuneTurns;

    public int[] cooldowns = new int[0];
    public List<StatusEffect> effects = new List<StatusEffect>();

    public bool IsAlive
    {
        get { return currentHP > 0; }
    }

    public float SlowPercent
    {
        get { return Mathf.Clamp(slowStacks * Rules.slowPerStack * data.debuffEffectiveness, 0f, 0.9f); }
    }

    public float EffectiveSpeed
    {
        get
        {
            float speed = data.speed * (1f + SumEffects(StatusType.SpeedUp));
            speed *= 1f - SlowPercent;
            return Mathf.Max(1f, speed);
        }
    }

    public int Speed
    {
        get { return Mathf.RoundToInt(EffectiveSpeed); }
    }

    public int StunThreshold
    {
        get { return Rules.stunHitsRequired + data.stunExtraHits; }
    }

    public int BleedDamagePerTick
    {
        get
        {
            if (bleedStacks <= 0)
            {
                return 0;
            }

            float percent = Rules.bleedPercentPerStack * data.debuffEffectiveness;
            return Mathf.Max(1, Mathf.RoundToInt(data.maxHP * percent * bleedStacks));
        }
    }

    public float AttackMultiplier
    {
        get { return 1f + SumEffects(StatusType.AttackUp); }
    }

    public float DefenseMultiplier
    {
        get { return Mathf.Max(0.25f, 1f + SumEffects(StatusType.DefenseUp)); }
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
        bleedStacks = 0;
        slowStacks = 0;
        stunBuildup = 0;
        stunned = false;
        stunImmuneTurns = 0;
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

    public void ApplyBullet(BattleColor color)
    {
        if (color == BattleColor.Red)
        {
            bleedStacks = Mathf.Min(Rules.maxBleedStacks, bleedStacks + 1);
        }
        else if (color == BattleColor.Yellow)
        {
            if (!stunned && stunImmuneTurns <= 0)
            {
                stunBuildup++;
                if (stunBuildup >= StunThreshold)
                {
                    stunBuildup = 0;
                    stunned = true;
                }
            }
        }
        else
        {
            slowStacks = Mathf.Min(Rules.maxSlowStacks, slowStacks + 1);
        }

        NotifyStatus();
    }

    public void ConsumeStun()
    {
        stunned = false;
        stunBuildup = 0;
        stunImmuneTurns = Rules.stunImmuneTurns;
        NotifyStatus();
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

        if (stunImmuneTurns > 0)
        {
            stunImmuneTurns--;
            NotifyStatus();
        }

        if (bleedStacks > 0)
        {
            int damage = BleedDamagePerTick;
            bleedStacks = Mathf.Max(0, bleedStacks - Rules.bleedDecayPerTurn);
            NotifyStatus();
            TakeDamage(damage);
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

        if (slowStacks > 0)
        {
            slowStacks = Mathf.Max(0, slowStacks - Rules.slowDecayPerTurn);
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