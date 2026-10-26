using UnityEngine;

[CreateAssetMenu(fileName = "NewCombatant", menuName = "Battle/Combatant Data")]
public class CombatantData : ScriptableObject
{
    public string displayName;
    public int maxHP = 20;
    public int attack = 6;
    public int speed = 5;
    public int healAmount = 8;
    public int maxHeals = 3;
}
