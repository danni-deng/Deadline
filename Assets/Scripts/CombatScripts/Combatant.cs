using System;
using UnityEngine;

public class Combatant : MonoBehaviour
{
    public static event Action<Combatant, int> AnyDamaged;
    public static event Action<Combatant, int> AnyHealed;
    public static event Action<Combatant> AnyDied;

    public CombatantData data;
    public bool isPlayerSide;
    public int currentHP;
    public int healsLeft;
    public float hideDelay = 0.4f;

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
    }

    public void TakeDamage(int amount)
    {
        currentHP = Mathf.Max(0, currentHP - amount);
        AnyDamaged?.Invoke(this, amount);

        if (currentHP == 0)
        {
            AnyDied?.Invoke(this);
            if (!isPlayerSide)
            {
                Invoke("Hide", hideDelay);
            }
        }
    }

    public void Heal()
    {
        int before = currentHP;
        currentHP = Mathf.Min(data.maxHP, currentHP + data.healAmount);
        healsLeft--;
        AnyHealed?.Invoke(this, currentHP - before);
    }

    void Hide()
    {
        gameObject.SetActive(false);
    }
}