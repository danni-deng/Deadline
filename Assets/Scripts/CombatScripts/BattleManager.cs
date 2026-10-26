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
    Heal
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

    public event Action<BattleState> StateChanged;
    public event Action<Combatant> TurnStarted;
    public event Action<Combatant> TargetChanged;
    public event Action<Combatant, Combatant, BattleAction> ActionPerformed;

    bool actionSubmitted;
    BattleAction submittedAction;
    Combatant submittedTarget;
    Coroutine battleRoutine;

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

            List<Combatant> order = BuildTurnOrder();

            foreach (Combatant c in order)
            {
                if (!c.IsAlive)
                {
                    continue;
                }

                if (CheckEnd())
                {
                    yield break;
                }

                ActiveCombatant = c;
                TurnStarted?.Invoke(c);

                if (c.isPlayerSide)
                {
                    yield return PlayerTurn(c);
                }
                else
                {
                    yield return EnemyTurn(c);
                }
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

        foreach (Combatant c in party)
        {
            if (c.IsAlive)
            {
                order.Add(c);
            }
        }
        foreach (Combatant c in enemies)
        {
            if (c.IsAlive)
            {
                order.Add(c);
            }
        }

        order.Sort(CompareTurnOrder);
        return order;
    }

    int CompareTurnOrder(Combatant a, Combatant b)
    {
        if (a.Speed != b.Speed)
        {
            return b.Speed.CompareTo(a.Speed);
        }
        if (a.isPlayerSide != b.isPlayerSide)
        {
            return a.isPlayerSide ? -1 : 1;
        }
        return 0;
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
        TargetChanged?.Invoke(SelectedTarget);
        SetState(BattleState.PlayerTurn);

        while (!actionSubmitted)
        {
            yield return null;
        }

        yield return Execute(user, submittedTarget, submittedAction);
    }

    IEnumerator EnemyTurn(Combatant user)
    {
        SetState(BattleState.Resolving);
        yield return new WaitForSeconds(actionDelay);

        List<Combatant> alive = GetAlive(party);
        Combatant target = alive[UnityEngine.Random.Range(0, alive.Count)];

        yield return Execute(user, target, BattleAction.Attack);
    }

    IEnumerator Execute(Combatant user, Combatant target, BattleAction action)
    {
        SetState(BattleState.Resolving);
        ActionPerformed?.Invoke(user, target, action);

        yield return new WaitForSeconds(actionDelay);

        if (action == BattleAction.Heal)
        {
            user.Heal();
            Debug.Log(user.data.displayName + " healed (HP " + user.currentHP + ")");
        }
        else
        {
            int damage = Mathf.Max(1, user.data.attack + UnityEngine.Random.Range(-damageVariance, damageVariance + 1));
            target.TakeDamage(damage);
            Debug.Log(user.data.displayName + " hit " + target.name + " for " + damage + " (HP " + target.currentHP + ")");
        }

        yield return new WaitForSeconds(actionDelay);
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
        else
        {
            if (SelectedTarget == null || !SelectedTarget.IsAlive)
            {
                return false;
            }
            submittedTarget = SelectedTarget;
        }

        submittedAction = action;
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
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SubmitAttack();
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SubmitHeal();
        }
    }
}