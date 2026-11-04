[System.Serializable]
public class DebuffRules
{
    public int maxBleedStacks = 5;
    public float bleedPercentPerStack = 0.02f;
    public int bleedDecayPerTurn = 2;
    public int stunHitsRequired = 3;
    public int stunImmuneTurns = 2;
    public float slowPerStack = 0.08f;
    public int maxSlowStacks = 4;
    public int slowDecayPerTurn = 1;
    public float startCharge = 0.35f;
}
