using UnityEngine;

public enum BattleColor
{
    Red,
    Yellow,
    Blue
}

public static class BattleColorUtil
{
    public static BattleColor FromString(string colorName)
    {
        switch (colorName.ToLower())
        {
            case "yellow":
                return BattleColor.Yellow;
            case "blue":
                return BattleColor.Blue;
            default:
                return BattleColor.Red;
        }
    }

    public static Color ToUnityColor(BattleColor color)
    {
        switch (color)
        {
            case BattleColor.Yellow:
                return new Color(0.95f, 0.76f, 0.19f);
            case BattleColor.Blue:
                return new Color(0.18f, 0.44f, 0.88f);
            default:
                return new Color(0.91f, 0.22f, 0.17f);
        }
    }
}
