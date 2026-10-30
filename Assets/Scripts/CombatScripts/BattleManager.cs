using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BattleState
{
    Inactive,
    PlayerTurn,
    Resolving,
    Won,
    Lost
}

public enum BattleAction
{
    Attack,
    Heal,
    Ability
}

public class BattleManager : MonoBehaviour
{
    public List<Combatant> party = new List<Combatant>();
    public List<Combatant> enemies = new List<Combatant>();
    public float actionDelay = 0.8f;
    public int damageVariance = 1;
    public bool autoStart = true;
    public bool useDebugKeys = true;

    public BattleState State { get; private set; }
    public Combatant ActiveCombatant { get; private set; }
    public Combatant SelectedTarget { get; private set; }
    public Combatant SelectedAlly { get; private set; }

    public event Action<BattleState> StateChanged;
    public event Action<Combatant> TurnStarted;
    public event Action<Combatant> TargetChanged;
    public event Action<Combatant> AllyChanged;
    public event Action<Combatant, Combatant, BattleAction> ActionPerformed;

    bool actionSubmitted;
    bool extraTurn;
    BattleAction submittedAction;
    int submittedAbility;
    Combatant submittedTarget;
    Coroutine battleRoutine;
    List<Combatant> roundOrder = new List<Combatant>();
    int roundIndex;

    void Start()
    {
        if (autoStart && party.Count > 0 && enemies.Count > 0)
        {
            StartBattle(party, enemies);
        }
    }

    public void StartBattle(List<Combatant> newParty, List<Combatant> newEnemies)
    {
        party = newParty;
        enemies = newEnemies;

        foreach (Combatant c in party)
        {
            c.Initialize();
        }
        foreach (Combatant c in enemies)
        {
            c.Initialize();
        }

        BattleMode.Active = true;

        if (battleRoutine != null)
        {
            StopCoroutine(battleRoutine);
        }
        battleRoutine = StartCoroutine(BattleLoop());
    }

    IEnumerator BattleLoop()
    {
        while (true)
        {
            if (CheckEnd())
            {
                yield break;
            }

            roundOrder = BuildTurnOrder();

            for (int i = 0; i < roundOrder.Count; i++)
            {
                Combatant c = roundOrder[i];
                roundIndex = i;

                if (!c.IsAlive)
                {
                    continue;
                }

                if (CheckEnd())
                {
                    yield break;
                }

                ActiveCombatant = c;
                c.OnTurnStart();
                TurnStarted?.Invoke(c);

                if (c.isPlayerSide)
                {
                    yield return PlayerTurn(c);

                    while (extraTurn && c.IsAlive && !CheckEnd())
                    {
                        extraTurn = false;
                        TurnStarted?.Invoke(c);
                        yield return PlayerTurn(c);
                    }
                }
                else
                {
                    yield return EnemyTurn(c);
                }

                extraTurn = false;
                c.OnTurnEnd();
            }
        }
    }

    bool CheckEnd()
    {
        if (AllDead(enemies))
        {
            Finish(BattleState.Won);
            return true;
        }
        if (AllDead(party))
        {
            Finish(BattleState.Lost);
            return true;
        }
        return false;
    }

    void Finish(BattleState result)
    {
        SetState(result);
        BattleMode.Active = false;
    }

    List<Combatant> BuildTurnOrder()
    {
        List<Combatant> order = new List<Combatant>();

        AddToOrder(order, party);
        AddToOrder(order, enemies);

        return order;
    }

    void AddToOrder(List<Combatant> order, List<Combatant> source)
    {
        foreach (Combatant c in source)
        {
            if (c == null || !c.IsAlive)
            {
                continue;
            }

            int position = order.Count;
            while (position > 0 && GoesBefore(c, order[position - 1]))
            {
                position--;
            }
            order.Insert(position, c);
        }
    }

    bool GoesBefore(Combatant a, Combatant b)
    {
        if (a.Speed != b.Speed)
        {
            return a.Speed > b.Speed;
        }
        return a.isPlayerSide && !b.isPlayerSide;
    }

    public List<Combatant> GetUpcomingTurns(int count)
    {
        List<Combatant> result = new List<Combatant>();

        if (ActiveCombatant != null && ActiveCombatant.IsAlive)
        {
            result.Add(ActiveCombatant);
        }

        for (int i = roundIndex + 1; i < roundOrder.Count && result.Count < count; i++)
        {
            if (roundOrder[i] != null && roundOrder[i].IsAlive)
            {
                result.Add(roundOrder[i]);
            }
        }

        List<Combatant> next = BuildTurnOrder();
        int safety = 0;

        while (result.Count < count && next.Count > 0 && safety < 50)
        {
            foreach (Combatant c in next)
            {
                if (result.Count >= count)
                {
                    break;
                }
                result.Add(c);
            }
            safety++;
        }

        return result;
    }

    List<Combatant> GetAlive(List<Combatant> list)
    {
        List<Combatant> alive = new List<Combatant>();
        foreach (Combatant c in list)
        {
            if (c.IsAlive)
            {
                alive.Add(c);
            }
        }
        return alive;
    }

    Combatant FirstAlive(List<Combatant> list)
    {
        foreach (Combatant c in list)
        {
            if (c.IsAlive)
            {
                return c;
            }
        }
        return null;
    }

    bool AllDead(List<Combatant> list)
    {
        foreach (Combatant c in list)
        {
            if (c.IsAlive)
            {
                return false;
            }
        }
        return true;
    }

    IEnumerator PlayerTurn(Combatant user)
    {
        actionSubmitted = false;
        SelectedTarget = FirstAlive(enemies);
        SelectedAlly = user;
        TargetChanged?.Invoke(SelectedTarget);
        AllyChanged?.Invoke(SelectedAlly);
        SetState(BattleState.PlayerTurn);

        while (!actionSubmitted)
        {
            yield return null;
        }

        yield return Execute(user, submittedTarget, submittedAction, submittedAbility);
    }

    IEnumerator EnemyTurn(Combatant user)
    {
        SetState(BattleState.Resolving);
        yield return new WaitForSeconds(actionDelay);

        List<Combatant> alive = GetAlive(party);
        Combatant target = alive[UnityEngine.Random.Range(0, alive.Count)];

        yield return Execute(user, target, BattleAction.Attack, -1);
    }

    IEnumerator Execute(Combatant user, Combatant target, BattleAction action, int abilityIndex)
    {
        SetState(BattleState.Resolving);
        ActionPerformed?.Invoke(user, target, action);

        yield return new WaitForSeconds(actionDelay);

        if (action == BattleAction.Heal)
        {
            user.Heal();
            Debug.Log(user.data.displayName + " healed (HP " + user.currentHP + ")");
        }
        else if (action == BattleAction.Ability)
        {
            ApplyAbility(user, abilityIndex, target);
        }
        else
        {
            int damage = ComputeDamage(user, target, 1f);
            target.TakeDamage(damage);
            Debug.Log(user.data.displayName + " hit " + target.name + " for " + damage + " (HP " + target.currentHP + ")");
        }

        yield return new WaitForSeconds(actionDelay);
    }

    int ComputeDamage(Combatant user, Combatant target, float power)
    {
        float damage = user.data.attack * power * user.AttackMultiplier;
        damage += UnityEngine.Random.Range(-damageVariance, damageVariance + 1);
        damage *= 100f / (100f + target.data.defense);
        damage /= target.DefenseMultiplier;
        return Mathf.Max(1, Mathf.RoundToInt(damage));
    }

    void ApplyAbility(Combatant user, int abilityIndex, Combatant chosen)
    {
        AbilityData ability = user.data.abilities[abilityIndex];
        int stacks = user.StackCount(ability);
        AbilityTier tier = ability.GetTier(stacks);

        user.cooldowns[abilityIndex] = ability.cooldown;
        Debug.Log(user.data.displayName + " used " + ability.abilityName + " (stacks " + stacks + ")");

        if (tier.selfDamagePercent > 0f)
        {
            int selfDamage = Mathf.RoundToInt(user.data.maxHP * tier.selfDamagePercent);
            selfDamage = Mathf.Min(selfDamage, user.currentHP - 1);
            if (selfDamage > 0)
            {
                user.TakeDamage(selfDamage);
            }
        }

        List<Combatant> targets = GetTargets(user, ability, tier, chosen);
        foreach (Combatant t in targets)
        {
            ApplyTierToTarget(user, ability, tier, t);
        }

        if (tier.extraTurn)
        {
            extraTurn = true;
        }
    }

    List<Combatant> GetTargets(Combatant user, AbilityData ability, AbilityTier tier, Combatant chosen)
    {
        List<Combatant> list = new List<Combatant>();

        if (ability.target == AbilityTarget.AllEnemies)
        {
            list = GetAlive(enemies);
        }
        else if (ability.target == AbilityTarget.Self)
        {
            list.Add(user);
        }
        else if (chosen != null && chosen.IsAlive)
        {
            list.Add(chosen);
        }

        if (tier.hitAllAllies)
        {
            list = GetAlive(party);
        }

        return list;
    }

    void ApplyTierToTarget(Combatant user, AbilityData ability, AbilityTier tier, Combatant t)
    {
        bool onOwnTurn = t == user;

        if (tier.damagePower > 0f)
        {
            float damage = ComputeDamage(user, t, tier.damagePower);

            if (tier.missingHpBonus > 0f)
            {
                damage += (t.data.maxHP - t.currentHP) * tier.missingHpBonus;
            }
            if (UnityEngine.Random.value < tier.critChance)
            {
                damage *= tier.critMultiplier;
                Debug.Log("Critical hit!");
            }

            t.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(damage)));

            if (UnityEngine.Random.value < tier.defenseDropChance)
            {
                t.AddStatus(StatusType.DefenseDown, tier.defenseDropPercent, tier.defenseDropTurns, false);
            }
        }

        if (tier.healPercent > 0f)
        {
            t.HealBy(Mathf.RoundToInt(t.data.maxHP * tier.healPercent));
        }

        if (tier.regenPercent > 0f && tier.regenTurns > 0)
        {
            t.AddStatus(StatusType.Regen, tier.regenPercent, tier.regenTurns, onOwnTurn);
        }

        if (tier.buffTurns > 0)
        {
            if (tier.attackBoost > 0f)
            {
                t.AddStatus(StatusType.AttackUp, tier.attackBoost, tier.buffTurns, onOwnTurn);
            }
            if (tier.defenseBoost > 0f)
            {
                t.AddStatus(StatusType.DefenseUp, tier.defenseBoost, tier.buffTurns, onOwnTurn);
            }
            if (tier.speedBoost > 0f)
            {
                t.AddStatus(StatusType.SpeedUp, tier.speedBoost, tier.buffTurns, onOwnTurn);
            }
        }

        if (tier.cooldownReduction > 0)
        {
            t.ReduceCooldowns(tier.cooldownReduction, ability);
        }
        if (tier.resetShortestCooldown)
        {
            t.ResetShortestCooldown(ability);
        }
        if (tier.grantStack)
        {
            t.AddColorStack(tier.stackColor);
        }
    }

    public bool SubmitPlayerAction(BattleAction action)
    {
        if (State != BattleState.PlayerTurn || actionSubmitted)
        {
            return false;
        }

        if (action == BattleAction.Heal)
        {
            if (!ActiveCombatant.CanHeal)
            {
                return false;
            }
            submittedTarget = ActiveCombatant;
        }
        else if (action == BattleAction.Attack)
        {
            if (SelectedTarget == null || !SelectedTarget.IsAlive)
            {
                return false;
            }
            submittedTarget = SelectedTarget;
        }
        else
        {
            return false;
        }

        submittedAction = action;
        actionSubmitted = true;
        return true;
    }

    public bool SubmitAbility(int abilityIndex)
    {
        if (State != BattleState.PlayerTurn || actionSubmitted)
        {
            return false;
        }

        Combatant user = ActiveCombatant;

        if (!user.AbilityReady(abilityIndex))
        {
            Debug.Log("Ability " + abilityIndex + " is not ready");
            return false;
        }

        AbilityData ability = user.data.abilities[abilityIndex];
        Combatant target;

        if (ability.target == AbilityTarget.SingleEnemy)
        {
            target = SelectedTarget;
        }
        else if (ability.target == AbilityTarget.AllEnemies)
        {
            target = FirstAlive(enemies);
        }
        else if (ability.target == AbilityTarget.SingleAlly)
        {
            target = SelectedAlly;
        }
        else
        {
            target = user;
        }

        if (target == null || !target.IsAlive)
        {
            return false;
        }

        submittedAction = BattleAction.Ability;
        submittedAbility = abilityIndex;
        submittedTarget = target;
        actionSubmitted = true;
        return true;
    }

    public void SubmitAttack()
    {
        SubmitPlayerAction(BattleAction.Attack);
    }

    public void SubmitHeal()
    {
        SubmitPlayerAction(BattleAction.Heal);
    }

    public void SubmitAbilityOne()
    {
        SubmitAbility(0);
    }

    public void SubmitAbilityTwo()
    {
        SubmitAbility(1);
    }

    public void NextTarget()
    {
        CycleTarget(1);
    }

    public void PreviousTarget()
    {
        CycleTarget(-1);
    }

    public void CycleTarget(int direction)
    {
        if (State != BattleState.PlayerTurn)
        {
            return;
        }

        List<Combatant> alive = GetAlive(enemies);
        if (alive.Count == 0)
        {
            return;
        }

        int index = alive.IndexOf(SelectedTarget);
        index = (index + direction + alive.Count) % alive.Count;
        SelectedTarget = alive[index];
        TargetChanged?.Invoke(SelectedTarget);
    }

    public void CycleAlly(int direction)
    {
        if (State != BattleState.PlayerTurn)
        {
            return;
        }

        List<Combatant> alive = GetAlive(party);
        if (alive.Count == 0)
        {
            return;
        }

        int index = alive.IndexOf(SelectedAlly);
        index = (index + direction + alive.Count) % alive.Count;
        SelectedAlly = alive[index];
        AllyChanged?.Invoke(SelectedAlly);
        Debug.Log("Ally target: " + SelectedAlly.name);
    }

    void SetState(BattleState newState)
    {
        State = newState;
        StateChanged?.Invoke(newState);
    }

    void Update()
    {
        if (!useDebugKeys || State != BattleState.PlayerTurn)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            CycleTarget(1);
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            CycleTarget(-1);
        }
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            CycleAlly(1);
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            CycleAlly(-1);
        }
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SubmitAttack();
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SubmitHeal();
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SubmitAbility(0);
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            SubmitAbility(1);
        }
    }
}