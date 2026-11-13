using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

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
    Ability
}

public class BattleManager : MonoBehaviour
{
    public List<Combatant> party = new List<Combatant>();
    public List<Combatant> enemies = new List<Combatant>();
    public DebuffRules debuffRules = new DebuffRules();
    public float actionDelay = 0.8f;
    public int damageVariance = 1;
    public bool autoStart = true;
    public bool useDebugKeys = true;
    public bool requireEnemyPick = true;

    public BattleState State { get; private set; }
    public Combatant ActiveCombatant { get; private set; }
    public Combatant SelectedTarget { get; private set; }
    public Combatant SelectedAlly { get; private set; }
    public bool IsTargetingAlly { get; private set; }
    public bool IsTargetingEnemy { get; private set; }

    public int PendingAbility
    {
        get { return pendingAbility; }
    }

    public int PendingEnemyAbility
    {
        get { return enemyPendingAbility; }
    }

    public event Action<BattleState> StateChanged;
    public event Action<Combatant> TurnStarted;
    public event Action<Combatant> TargetChanged;
    public event Action<Combatant> AllyChanged;
    public event Action<bool> AllyTargetingChanged;
    public event Action<bool> EnemyTargetingChanged;
    public event Action<Combatant, Combatant, BattleAction> ActionPerformed;
    public event Action<Combatant, Combatant> AttackResolved;

    bool actionSubmitted;
    BattleAction submittedAction;
    int submittedAbility;
    int pendingAbility = -1;
    int enemyPendingAbility = -1;
    Combatant submittedTarget;
    Coroutine battleRoutine;

    void Awake()
    {
        Combatant.Rules = debuffRules;
    }

    void Start()
    {
        if (autoStart && party.Count > 0 && enemies.Count > 0)
        {
            StartBattle(party, enemies);
        }
    }

    public void StartBattle(List<Combatant> newParty, List<Combatant> newEnemies)
    {
        Combatant.Rules = debuffRules;
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

            Combatant c = NextActor();
            if (c == null)
            {
                yield break;
            }

            ActiveCombatant = c;
            c.OnTurnStart();

            if (!c.IsAlive)
            {
                continue;
            }

            TurnStarted?.Invoke(c);

            if (c.stunned)
            {
                Debug.Log(c.name + " is stunned and loses its turn");
                c.ConsumeStun();
                SetState(BattleState.Resolving);
                yield return new WaitForSeconds(actionDelay);
            }
            else if (c.isPlayerSide)
            {
                yield return PlayerTurn(c);
            }
            else
            {
                yield return EnemyTurn(c);
            }

            c.OnTurnEnd();
        }
    }

    Combatant NextActor()
    {
        List<Combatant> alive = new List<Combatant>();
        AddAlive(alive, party);
        AddAlive(alive, enemies);

        if (alive.Count == 0)
        {
            return null;
        }

        Combatant best = null;
        float bestTime = float.MaxValue;

        foreach (Combatant c in alive)
        {
            float time = (Combatant.TurnDistance - c.charge) / c.EffectiveSpeed;
            if (best == null || time < bestTime - 0.0001f)
            {
                best = c;
                bestTime = time;
            }
        }

        bestTime = Mathf.Max(0f, bestTime);

        foreach (Combatant c in alive)
        {
            c.charge += c.EffectiveSpeed * bestTime;
        }

        best.charge = 0f;
        return best;
    }

    public List<Combatant> GetUpcomingTurns(int count)
    {
        List<Combatant> result = new List<Combatant>();

        if (ActiveCombatant != null && ActiveCombatant.IsAlive)
        {
            result.Add(ActiveCombatant);
        }

        List<Combatant> alive = new List<Combatant>();
        AddAlive(alive, party);
        AddAlive(alive, enemies);

        if (alive.Count == 0)
        {
            return result;
        }

        float[] charges = new float[alive.Count];
        for (int i = 0; i < alive.Count; i++)
        {
            charges[i] = alive[i].charge;
        }

        int safety = 0;
        while (result.Count < count && safety < 300)
        {
            safety++;

            int best = 0;
            float bestTime = float.MaxValue;

            for (int i = 0; i < alive.Count; i++)
            {
                float time = (Combatant.TurnDistance - charges[i]) / alive[i].EffectiveSpeed;
                if (time < bestTime - 0.0001f)
                {
                    best = i;
                    bestTime = time;
                }
            }

            bestTime = Mathf.Max(0f, bestTime);

            for (int i = 0; i < alive.Count; i++)
            {
                charges[i] += alive[i].EffectiveSpeed * bestTime;
            }

            charges[best] = 0f;
            result.Add(alive[best]);
        }

        return result;
    }

    void AddAlive(List<Combatant> target, List<Combatant> source)
    {
        foreach (Combatant c in source)
        {
            if (c != null && c.IsAlive)
            {
                target.Add(c);
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

    List<Combatant> GetAlive(List<Combatant> list)
    {
        List<Combatant> alive = new List<Combatant>();
        AddAlive(alive, list);
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

    IEnumerator PlayerTurn(Combatant user)
    {
        actionSubmitted = false;
        EndAllyTargeting();
        EndEnemyTargeting();
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
        if (alive.Count == 0)
        {
            yield break;
        }

        Combatant target = alive[UnityEngine.Random.Range(0, alive.Count)];
        yield return Execute(user, target, BattleAction.Attack, -1);
    }

    IEnumerator Execute(Combatant user, Combatant target, BattleAction action, int abilityIndex)
    {
        SetState(BattleState.Resolving);
        ActionPerformed?.Invoke(user, target, action);

        yield return new WaitForSeconds(actionDelay);

        if (action == BattleAction.Ability)
        {
            ApplyAbility(user, abilityIndex, target);
        }
        else
        {
            int damage = ComputeDamage(user, target, 1f);
            target.TakeDamage(damage);
            Debug.Log(user.data.displayName + " hit " + target.name + " for " + damage + " (HP " + target.currentHP + ")");

            if (user.isPlayerSide)
            {
                AttackResolved?.Invoke(user, target);
            }
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

        user.cooldowns[abilityIndex] = ability.cooldown;
        Debug.Log(user.data.displayName + " used " + ability.abilityName);

        if (ability.selfDamagePercent > 0f)
        {
            int selfDamage = Mathf.RoundToInt(user.data.maxHP * ability.selfDamagePercent);
            selfDamage = Mathf.Min(selfDamage, user.currentHP - 1);
            if (selfDamage > 0)
            {
                user.TakeDamage(selfDamage);
            }
        }

        List<Combatant> targets = GetTargets(user, ability, chosen);
        foreach (Combatant t in targets)
        {
            ApplyToTarget(user, ability, t);
        }
    }

    List<Combatant> GetTargets(Combatant user, AbilityData ability, Combatant chosen)
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

        return list;
    }

    void ApplyToTarget(Combatant user, AbilityData ability, Combatant t)
    {
        bool onOwnTurn = t == user;

        if (ability.damagePower > 0f)
        {
            float damage = ComputeDamage(user, t, ability.damagePower);

            if (UnityEngine.Random.value < ability.critChance)
            {
                damage *= ability.critMultiplier;
                Debug.Log("Critical hit!");
            }

            t.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(damage)));
        }

        if (ability.healPercent > 0f)
        {
            t.HealBy(Mathf.RoundToInt(t.data.maxHP * ability.healPercent));
        }

        if (ability.regenPercent > 0f && ability.regenTurns > 0)
        {
            t.AddStatus(StatusType.Regen, ability.regenPercent, ability.regenTurns, onOwnTurn);
        }

        if (ability.buffTurns > 0)
        {
            if (ability.attackBoost > 0f)
            {
                t.AddStatus(StatusType.AttackUp, ability.attackBoost, ability.buffTurns, onOwnTurn);
            }
            if (ability.defenseBoost > 0f)
            {
                t.AddStatus(StatusType.DefenseUp, ability.defenseBoost, ability.buffTurns, onOwnTurn);
            }
            if (ability.speedBoost > 0f)
            {
                t.AddStatus(StatusType.SpeedUp, ability.speedBoost, ability.buffTurns, onOwnTurn);
            }
        }

        if (ability.cooldownReduction > 0)
        {
            t.ReduceCooldowns(ability.cooldownReduction, ability);
        }

        if (ability.bonusShots > 0)
        {
            t.bonusShots += ability.bonusShots;
        }
    }

    public bool TrySubmitAttack()
    {
        if (State != BattleState.PlayerTurn || actionSubmitted)
        {
            return false;
        }
        if (IsTargetingAlly)
        {
            return false;
        }
        if (SelectedTarget == null || !SelectedTarget.IsAlive)
        {
            SelectedTarget = FirstAlive(enemies);
            TargetChanged?.Invoke(SelectedTarget);

            if (SelectedTarget == null)
            {
                return false;
            }
        }

        bool needsPick = requireEnemyPick && GetAlive(enemies).Count > 1;
        bool confirming = IsTargetingEnemy && enemyPendingAbility == -1;

        if (needsPick && !confirming)
        {
            BeginEnemyTargeting(-1);
            return true;
        }

        EndAllyTargeting();
        EndEnemyTargeting();
        submittedTarget = SelectedTarget;
        submittedAction = BattleAction.Attack;
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

        bool enemyPick = ability.target == AbilityTarget.SingleEnemy
            && requireEnemyPick
            && GetAlive(enemies).Count > 1;
        bool confirmingEnemy = IsTargetingEnemy && enemyPendingAbility == abilityIndex;

        if (enemyPick && !confirmingEnemy)
        {
            if (SelectedTarget == null || !SelectedTarget.IsAlive)
            {
                SelectedTarget = FirstAlive(enemies);
                TargetChanged?.Invoke(SelectedTarget);
            }

            BeginEnemyTargeting(abilityIndex);
            return true;
        }

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
        EndEnemyTargeting();
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
        EndEnemyTargeting();
        pendingAbility = abilityIndex;

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

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

    void BeginEnemyTargeting(int abilityIndex)
    {
        EndAllyTargeting();
        enemyPendingAbility = abilityIndex;

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        IsTargetingEnemy = true;
        EnemyTargetingChanged?.Invoke(true);
        TargetChanged?.Invoke(SelectedTarget);
    }

    void EndEnemyTargeting()
    {
        enemyPendingAbility = -1;

        if (IsTargetingEnemy)
        {
            IsTargetingEnemy = false;
            EnemyTargetingChanged?.Invoke(false);
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

    public void CancelEnemyTargeting()
    {
        if (!IsTargetingEnemy)
        {
            return;
        }

        EndEnemyTargeting();
    }

    public void ConfirmAllyTarget()
    {
        if (IsTargetingAlly)
        {
            SubmitAbility(pendingAbility);
        }
    }

    public void ConfirmEnemyTarget()
    {
        if (!IsTargetingEnemy)
        {
            return;
        }

        if (enemyPendingAbility < 0)
        {
            TrySubmitAttack();
        }
        else
        {
            SubmitAbility(enemyPendingAbility);
        }
    }

    public void SubmitAttack()
    {
        TrySubmitAttack();
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

        if (Input.GetMouseButtonDown(1))
        {
            CancelAllyTargeting();
            CancelEnemyTargeting();
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
        else if (IsTargetingEnemy)
        {
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                CycleTarget(1);
            }
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.DownArrow))
            {
                CycleTarget(-1);
            }
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                ConfirmEnemyTarget();
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelEnemyTargeting();
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

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            TrySubmitAttack();
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SubmitAbility(0);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SubmitAbility(1);
        }
    }
}