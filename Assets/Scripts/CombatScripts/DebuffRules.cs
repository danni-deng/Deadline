[System.Serializable]
public class DebuffRules
{
    public int buildupThreshold = 7;
    public int resistanceTurns = 1;
    public int vulnerableTurns = 2;
    public int weakenedTurns = 2;
    public int interruptedTurns = 1;
    public float vulnerableDamageBonus = 0.25f;
    public float weakenedDamagePenalty = 0.3f;
    public float interruptDelayPercent = 0.5f;
    public float startCharge = 0.35f;
}