using UnityEngine;

[CreateAssetMenu(fileName = "NewCombatant", menuName = "Battle/Combatant Data")]
public class CombatantData : ScriptableObject
{
    public string displayName;
    public Sprite portrait;
    public int maxHP = 20;
    public int attack = 6;
    public int defense = 0;
    public int speed = 100;

    public BattleColor bulletColorA = BattleColor.Red;
    public BattleColor bulletColorB = BattleColor.Yellow;
    public int bulletsPerColor = 40;
    public int shotsPerTurn = 5;

    public int stunExtraHits = 0;
    public float debuffEffectiveness = 1f;

    public AbilityData[] abilities = new AbilityData[0];
}
