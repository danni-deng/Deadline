using UnityEngine;

[CreateAssetMenu(fileName = "StatusIcons", menuName = "Battle/Status Icons")]
public class StatusIcons : ScriptableObject
{
    public Sprite vulnerable;
    public Sprite weakened;
    public Sprite interrupted;
    public Sprite resistant;

    public Sprite ForColor(BattleColor color)
    {
        if (color == BattleColor.Red)
        {
            return vulnerable;
        }
        if (color == BattleColor.Yellow)
        {
            return interrupted;
        }
        return weakened;
    }
}
