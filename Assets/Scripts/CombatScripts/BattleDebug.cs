using System.Collections.Generic;
using UnityEngine;

public class BattleDebug : MonoBehaviour
{
    public BattleManager battleManager;
    public BattleCamera battleCamera;

    void OnEnable()
    {
        if (battleManager != null)
        {
            battleManager.StateChanged += OnStateChanged;
        }
    }

    void OnDisable()
    {
        if (battleManager != null)
        {
            battleManager.StateChanged -= OnStateChanged;
        }
    }

    void Start()
    {
        if (battleManager == null)
        {
            Debug.LogError("BattleDebug: battleManager field is empty");
            return;
        }

        Debug.Log("BattleDebug: party " + battleManager.party.Count + ", enemies " + battleManager.enemies.Count + ", autoStart " + battleManager.autoStart);

        CheckList(battleManager.party, "party");
        CheckList(battleManager.enemies, "enemies");

        if (battleCamera == null)
        {
            Debug.LogError("BattleDebug: battleCamera field is empty");
            return;
        }

        CheckShot(battleCamera.wideShot, "Wide Shot");
        CheckShot(battleCamera.playerTurnShot, "Player Turn Shot");
        CheckShot(battleCamera.playerActionShot, "Player Action Shot");
        CheckShot(battleCamera.enemyActionShot, "Enemy Action Shot");
        CheckShot(battleCamera.victoryShot, "Victory Shot");
    }

    void CheckList(List<Combatant> list, string label)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == null)
            {
                Debug.LogError("BattleDebug: " + label + " slot " + i + " is empty");
            }
            else if (list[i].data == null)
            {
                Debug.LogError("BattleDebug: " + list[i].name + " has no Data asset");
            }
        }
    }

    void CheckShot(CameraShot shot, string label)
    {
        if (shot.pose == null)
        {
            Debug.LogWarning("BattleDebug: " + label + " has no pose assigned");
        }
    }

    void OnStateChanged(BattleState state)
    {
        Debug.Log("BattleDebug: state is now " + state);
    }
}
