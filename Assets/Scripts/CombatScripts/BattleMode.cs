using UnityEngine;

public class BattleMode : MonoBehaviour
{
    public static bool Active;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState()
    {
        Active = false;
    }
}
