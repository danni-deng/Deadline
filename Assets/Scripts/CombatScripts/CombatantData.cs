using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCombatant", menuName = "Battle/Combatant Data")]
public class CombatantData : ScriptableObject
{
    public string displayName;
    public Sprite portrait;
    public int maxHP = 20;
    public int attack = 6;
    public int defense = 0;
    public int speed = 5;
    public int healAmount = 8;
    public int maxHeals = 3;

    public BattleColor bulletColorA = BattleColor.Red;
    public BattleColor bulletColorB = BattleColor.Yellow;
    public int startingBullets = 40;

    public List<BattleColor> startingColors = new List<BattleColor>();
    public AbilityData[] abilities = new AbilityData[0];
}
