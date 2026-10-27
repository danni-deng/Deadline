using System;
using System.Collections.Generic;
using UnityEngine;

public enum StatusType
{
    AttackUp,
    DefenseUp,
    DefenseDown,
    Regen
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
    public static event Action<Combatant, int> AnyDamaged;
    public static event Action<Combatant, int> AnyHealed;
    public static event Action<Combatant> AnyDied;
    public static event Action<Combatant> AnyColorStateChanged;
    public static event Action<Combatant, Combatant, BattleColor> AnyColorGained;

    public CombatantData data;
    public bool isPlayerSide;
    public int currentHP;
    public int healsLeft;
    public int bulletsLeft;
    public int shotsToColor = 5;
    public float hideDelay = 0.4f;

    public int[] colorStacks = new int[3];
    public int[] colorProgress = new int[3];
    public List<BattleColor> colors = new List<BattleColor>();
    public int[] cooldowns = new int[0];
    public List<StatusEffect> effects = new List<StatusEffect>();

    public bool IsAlive
    {
        get { return currentHP > 0; }
    }

    public int Speed
    {
        get { return data.speed; }
    }

    public bool CanHeal
    {
        get { return healsLeft > 0 && currentHP < data.maxHP; }
    }

    public float AttackMultiplier
    {
        get { return 1f + SumEffects(StatusType.AttackUp); }
    }

    public float DefenseMultiplier
    {
        get { return Mathf.Max(0.25f, 1f + SumEffects(StatusType.DefenseUp) - SumEffects(StatusType.DefenseDown)); }
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
        healsLeft = data.maxHeals;
        bulletsLeft = data.startingBullets;
        colorStacks = new int[3];
        colorProgress = new int[3];
        colors = new List<BattleColor>(data.startingColors);
        cooldowns = new int[data.abilities.Length];
        effects = new List<StatusEffect>();

        if (AnyColorStateChanged != null)
        {
            AnyColorStateChanged(this);
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

    public int StackCount(AbilityData ability)
    {
        int total = 0;

        if (ability.anyColor)
        {
            for (int i = 0; i < colorStacks.Length; i++)
            {
                total += colorStacks[i];
            }
        }
        else
        {
            foreach (BattleColor c in ability.stackColors)
            {
                total += colorStacks[(int)c];
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
                return;
            }
        }

        StatusEffect fresh = new StatusEffect();
        fresh.type = type;
        fresh.amount = amount;
        fresh.turnsLeft = turns;
        effects.Add(fresh);
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

    public void ResetShortestCooldown(AbilityData except)
    {
        int bestIndex = -1;

        for (int i = 0; i < cooldowns.Length; i++)
        {
            if (data.abilities[i] == except)
            {
                continue;
            }
            if (cooldowns[i] > 0 && (bestIndex < 0 || cooldowns[i] < cooldowns[bestIndex]))
            {
                bestIndex = i;
            }
        }

        if (bestIndex >= 0)
        {
            cooldowns[bestIndex] = 0;
        }
    }

    public void AddColorStack(BattleColor color)
    {
        colorStacks[(int)color]++;
        Debug.Log(name + " gained a " + color + " stack (now " + colorStacks[(int)color] + ")");
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
    }

    public bool HasColor(BattleColor color)
    {
        return colors.Contains(color);
    }

    public bool UseBullet()
    {
        if (bulletsLeft <= 0)
        {
            return false;
        }

        bulletsLeft--;
        return true;
    }

    public bool ApplyShot(BattleColor color, Combatant shooter)
    {
        if (HasColor(color))
        {
            return true;
        }

        int index = (int)color;
        colorProgress[index]++;

        if (colorProgress[index] >= shotsToColor)
        {
            colorProgress[index] = 0;
            colors.Add(color);
            shooter.colorStacks[index]++;
            Debug.Log(name + " turned " + color + ", " + shooter.name + " now has " + shooter.colorStacks[index] + " " + color + " stack(s)");

            if (AnyColorGained != null)
            {
                AnyColorGained(this, shooter, color);
            }
        }

        if (AnyColorStateChanged != null)
        {
            AnyColorStateChanged(this);
        }

        return false;
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

    public void Heal()
    {
        healsLeft--;
        HealBy(data.healAmount);
    }

    void Hide()
    {
        gameObject.SetActive(false);
    }
}