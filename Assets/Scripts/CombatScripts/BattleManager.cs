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
    ColorStrike,
    Ability
}

public class BattleManager : MonoBehaviour
{
    public List<Combatant> party = new List<Combatant>();
    public List<Combatant> enemies = new List<Combatant>();
    public float actionDelay = 0.8f;
    public int damageVariance = 1;
    public float greyMultiplier = 0.5f;
    public float colorStrikeMultiplier = 1.25f;
    public float strikeMatchBonus = 1f;
    public bool autoStart = true;
    public bool useDebugKeys = true;

    public BattleState State { get; private set; }
    public Combatant ActiveCombatant { get; private set; }
    public Combatant SelectedTarget { get; private set; }
    public Combatant SelectedAlly { get; private set; }
    public bool IsTargetingAlly { get; private set; }
    public BattleColor StrikeChipColor { get; private set; }

    public int PendingAbility
    {
        get { return pendingAbility; }
    }

    public event Action<BattleState> StateChanged;
    public event Action<Combatant> TurnStarted;
    public event Action<Combatant> TargetChanged;
    public event Action<Combatant> AllyChanged;
    public event Action<bool> AllyTargetingChanged;
    public event Action<BattleColor> StrikeChipChanged;
    public event Action<Combatant, Combatant, BattleAction> ActionPerformed;

    bool actionSubmitted;
    bool extraTurn;
    BattleAction submittedAction;
    BattleColor submittedChip;
    int submittedAbility;
    int pendingAbility = -1;
    Combatant submittedTarget;
    Coroutine battleRoutine;
    List<Combatant> roundOrder = new List<Combatant>();
    int roundIndex;

    void OnEnable()
    {
        Combatant.AnyChipsChanged += OnChipsChanged;
    }

    void OnDisable()
    {
        Combatant.AnyChipsChanged -= OnChipsChanged;
    }

    void Start()
    {
        if (autoStart && party.Count > 0 && enemies.Count > 0)
        {
            StartBattle(party, enemies);
        }
    }

    void OnChipsChanged(Combatant who)
    {
        if (who == ActiveCombatant && who.isPlayerSide && who.chips[(int)StrikeChipColor] <= 0)
        {
            PickBestStrikeChip(who);
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
            if (c != null && c.IsAlive)
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
            if (c != null && c.IsAlive)
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
            if (c != null && c.IsAlive)
            {
                return false;
            }
        }
        return true;
    }

    void PickBestStrikeChip(Combatant user)
    {
        int best = -1;
        int bestCount = 0;

        for (int i = 0; i < user.chips.Length; i++)
        {
            if (user.chips[i] > bestCount)
            {
                bestCount = user.chips[i];
                best = i;
            }
        }

        if (best >= 0)
        {
            StrikeChipColor = (BattleColor)best;
        }

        StrikeChipChanged?.Invoke(StrikeChipColor);
    }

    public void SelectStrikeChip(BattleColor color)
    {
        if (State != BattleState.PlayerTurn || ActiveCombatant == null)
        {
            return;
        }
        if (ActiveCombatant.chips[(int)color] <= 0)
        {
            return;
        }

        StrikeChipColor = color;
        StrikeChipChanged?.Invoke(color);
    }

    public void CycleStrikeChip()
    {
        if (State != BattleState.PlayerTurn || ActiveCombatant == null)
        {
            return;
        }

        for (int step = 1; step <= 3; step++)
        {
            int index = ((int)StrikeChipColor + step) % 3;
            if (ActiveCombatant.chips[index] > 0)
            {
                SelectStrikeChip((BattleColor)index);
                return;
            }
        }
    }

    IEnumerator PlayerTurn(Combatant user)
    {
        actionSubmitted = false;
        EndAllyTargeting();
        PickBestStrikeChip(user);
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

        if (action == BattleAction.ColorStrike)
        {
            user.SpendChip(submittedChip);
        }

        ActionPerformed?.Invoke(user, target, action);

        yield return new WaitForSeconds(actionDelay);

        if (action == BattleAction.Ability)
        {
            ApplyAbility(user, abilityIndex, target);
        }
        else if (action == BattleAction.ColorStrike)
        {
            float multiplier = EnemyColorMultiplier(user, target, true);
            int damage = ComputeDamage(user, target, colorStrikeMultiplier, true);
            target.TakeDamage(damage);
            Debug.Log(user.data.displayName + " color strike (" + submittedChip + " chip) on " + target.name + " for " + damage + " (color x" + multiplier + ", HP " + target.currentHP + ")");
        }
        else
        {
            float multiplier = EnemyColorMultiplier(user, target, false);
            int damage = ComputeDamage(user, target, 1f, false);
            target.TakeDamage(damage);
            Debug.Log(user.data.displayName + " hit " + target.name + " for " + damage + " (color x" + multiplier + ", HP " + target.currentHP + ")");
        }

        yield return new WaitForSeconds(actionDelay);
    }

    float EnemyColorMultiplier(Combatant attacker, Combatant target, bool isStrike)
    {
        if (target.isPlayerSide)
        {
            return 1f;
        }
        if (target.colors.Count == 0)
        {
            return greyMultiplier;
        }
        if (isStrike && target.HasAnyColor(attacker.data.bulletColorA, attacker.data.bulletColorB))
        {
            return strikeMatchBonus;
        }
        return 1f;
    }

    int ComputeDamage(Combatant user, Combatant target, float power, bool isStrike)
    {
        float damage = user.data.attack * power * user.AttackMultiplier;
        damage += UnityEngine.Random.Range(-damageVariance, damageVariance + 1);
        damage *= 100f / (100f + target.data.defense);
        damage /= target.DefenseMultiplier;
        damage *= EnemyColorMultiplier(user, target, isStrike);
        return Mathf.Max(1, Mathf.RoundToInt(damage));
    }

    void ApplyAbility(Combatant user, int abilityIndex, Combatant chosen)
    {
        AbilityData ability = user.data.abilities[abilityIndex];
        int count = user.ChipCount(ability);
        AbilityTier tier = ability.GetTier(count);

        user.cooldowns[abilityIndex] = ability.cooldown;
        Debug.Log(user.data.displayName + " used " + ability.abilityName + " (matching chips " + count + ")");

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
            float damage = ComputeDamage(user, t, tier.damagePower, false);

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
            t.AddChip(tier.stackColor);
        }
    }

    public bool TrySubmitAttack()
    {
        if (State != BattleState.PlayerTurn || actionSubmitted)
        {
            return false;
        }
        if (SelectedTarget == null || !SelectedTarget.IsAlive)
        {
            return false;
        }

        EndAllyTargeting();
        submittedTarget = SelectedTarget;
        submittedAction = BattleAction.Attack;
        actionSubmitted = true;
        return true;
    }

    public bool TrySubmitColorStrike()
    {
        if (State != BattleState.PlayerTurn || actionSubmitted)
        {
            return false;
        }

        Combatant user = ActiveCombatant;
        if (user.TotalChips <= 0)
        {
            Debug.Log("No color chips for a color strike");
            return false;
        }
        if (user.chips[(int)StrikeChipColor] <= 0)
        {
            PickBestStrikeChip(user);
        }
        if (SelectedTarget == null || !SelectedTarget.IsAlive)
        {
            return false;
        }

        EndAllyTargeting();
        submittedTarget = SelectedTarget;
        submittedAction = BattleAction.ColorStrike;
        submittedChip = StrikeChipColor;
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
        bool needsPick = ability.target == AbilityTarget.SingleAlly && GetAlive(party).Count > 1;
        bool confirming = IsTargetingAlly && pendingAbility == abilityIndex;

        if (needsPick && !confirming)
        {
            BeginAllyTargeting(abilityIndex);
            return true;
        }

        Combatant target = PickTarget(user, ability);
        if (target == null || !target.IsAlive)
        {
            return false;
        }

        EndAllyTargeting();
        submittedAction = BattleAction.Ability;
        submittedAbility = abilityIndex;
        submittedTarget = target;
        actionSubmitted = true;
        return true;
    }

    Combatant PickTarget(Combatant user, AbilityData ability)
    {
        if (ability.target == AbilityTarget.SingleEnemy)
        {
            return SelectedTarget;
        }
        if (ability.target == AbilityTarget.AllEnemies)
        {
            return FirstAlive(enemies);
        }
        if (ability.target == AbilityTarget.SingleAlly)
        {
            return SelectedAlly;
        }
        return user;
    }

    void BeginAllyTargeting(int abilityIndex)
    {
        pendingAbility = abilityIndex;

        if (!IsTargetingAlly)
        {
            IsTargetingAlly = true;
            AllyTargetingChanged?.Invoke(true);
        }

        AllyChanged?.Invoke(SelectedAlly);
    }

    void EndAllyTargeting()
    {
        pendingAbility = -1;

        if (IsTargetingAlly)
        {
            IsTargetingAlly = false;
            AllyTargetingChanged?.Invoke(false);
        }
    }

    public void CancelAllyTargeting()
    {
        if (!IsTargetingAlly)
        {
            return;
        }

        EndAllyTargeting();
        SelectedAlly = ActiveCombatant;
        AllyChanged?.Invoke(SelectedAlly);
    }

    public void ConfirmAllyTarget()
    {
        if (IsTargetingAlly)
        {
            SubmitAbility(pendingAbility);
        }
    }

    public void SubmitAttack()
    {
        TrySubmitAttack();
    }

    public void SubmitColorStrike()
    {
        TrySubmitColorStrike();
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

        if (IsTargetingAlly)
        {
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                CycleAlly(1);
            }
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.DownArrow))
            {
                CycleAlly(-1);
            }
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                ConfirmAllyTarget();
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelAllyTargeting();
            }
        }
        else
        {
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
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            CycleStrikeChip();
        }
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            TrySubmitAttack();
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            TrySubmitColorStrike();
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