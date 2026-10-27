using UnityEngine;

public class EnemyColorView : MonoBehaviour
{
    public Combatant combatant;
    public SpriteRenderer[] renderers;
    public Color grayTint = new Color(0.75f, 0.75f, 0.75f);
    public Color orange = new Color(1f, 0.55f, 0.1f);
    public Color green = new Color(0.3f, 0.8f, 0.3f);
    public Color purple = new Color(0.6f, 0.25f, 0.8f);
    public Color allColors = new Color(0.55f, 0.4f, 0.3f);
    public float progressStrength = 0.15f;

    void Awake()
    {
        if (combatant == null)
        {
            combatant = GetComponent<Combatant>();
        }
        if (renderers == null || renderers.Length == 0)
        {
            renderers = GetComponentsInChildren<SpriteRenderer>();
        }
    }

    void OnEnable()
    {
        Combatant.AnyColorStateChanged += OnColorStateChanged;
    }

    void OnDisable()
    {
        Combatant.AnyColorStateChanged -= OnColorStateChanged;
    }

    void Start()
    {
        Refresh();
    }

    void OnColorStateChanged(Combatant who)
    {
        if (who == combatant)
        {
            Refresh();
        }
    }

    void Refresh()
    {
        Color tint = BaseTint();

        int best = -1;
        int bestProgress = 0;
        for (int i = 0; i < 3; i++)
        {
            if (combatant.colorProgress[i] > bestProgress)
            {
                bestProgress = combatant.colorProgress[i];
                best = i;
            }
        }

        if (best >= 0)
        {
            Color next = BattleColorUtil.ToUnityColor((BattleColor)best);
            tint = Color.Lerp(tint, next, bestProgress * progressStrength);
        }

        foreach (SpriteRenderer sr in renderers)
        {
            if (sr != null)
            {
                sr.color = tint;
            }
        }
    }

    Color BaseTint()
    {
        bool red = combatant.HasColor(BattleColor.Red);
        bool yellow = combatant.HasColor(BattleColor.Yellow);
        bool blue = combatant.HasColor(BattleColor.Blue);

        int count = 0;
        if (red) { count++; }
        if (yellow) { count++; }
        if (blue) { count++; }

        if (count == 0)
        {
            return grayTint;
        }
        if (count == 3)
        {
            return allColors;
        }
        if (count == 1)
        {
            if (red)
            {
                return BattleColorUtil.ToUnityColor(BattleColor.Red);
            }
            if (yellow)
            {
                return BattleColorUtil.ToUnityColor(BattleColor.Yellow);
            }
            return BattleColorUtil.ToUnityColor(BattleColor.Blue);
        }

        if (red && yellow)
        {
            return orange;
        }
        if (red && blue)
        {
            return purple;
        }
        return green;
    }
}
