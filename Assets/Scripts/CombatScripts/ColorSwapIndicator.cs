using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ColorSwapIndicator : MonoBehaviour
{
    public BattleManager battleManager;
    public GunMode gun;
    public CanvasGroup group;
    public Image[] chips = new Image[2];
    public GameObject[] activeMarkers = new GameObject[2];
    public TMP_Text[] letters = new TMP_Text[2];
    public TMP_Text[] counts = new TMP_Text[2];
    public TMP_Text keyHint;
    public Graphic[] tintWithActive;
    public float activeScale = 1.25f;
    public float inactiveScale = 1f;
    public float inactiveAlpha = 0.45f;
    public float scaleSpeed = 14f;
    public float popAmount = 0.25f;
    public float popDecay = 8f;

    int lastSlot = -1;
    Combatant lastUser;
    float pop;
    Vector3[] baseScales = new Vector3[2];

    void Awake()
    {
        for (int i = 0; i < 2; i++)
        {
            baseScales[i] = (i < chips.Length && chips[i] != null) ? chips[i].transform.localScale : Vector3.one;
        }
    }

    void Update()
    {
        if (battleManager == null || gun == null)
        {
            return;
        }

        Combatant user = battleManager.ActiveCombatant;
        bool show = battleManager.State == BattleState.PlayerTurn
            && !battleManager.IsTargetingAlly
            && user != null
            && user.isPlayerSide
            && user.data != null;

        if (group != null)
        {
            group.alpha = show ? 1f : 0f;
        }

        if (!show)
        {
            return;
        }

        int slot = gun.CurrentColor == user.data.bulletColorA ? 0 : 1;

        if (user != lastUser)
        {
            lastUser = user;
            lastSlot = slot;
        }
        else if (slot != lastSlot)
        {
            lastSlot = slot;
            pop = 1f;
        }

        pop = Mathf.MoveTowards(pop, 0f, popDecay * Time.unscaledDeltaTime);

        for (int i = 0; i < 2; i++)
        {
            BattleColor color = i == 0 ? user.data.bulletColorA : user.data.bulletColorB;
            bool active = i == slot;

            if (i < chips.Length && chips[i] != null)
            {
                Color tint = BattleColorUtil.ToUnityColor(color);
                tint.a = active ? 1f : inactiveAlpha;
                chips[i].color = tint;

                float targetScale = active ? activeScale + popAmount * pop : inactiveScale;
                Vector3 target = baseScales[i] * targetScale;
                float t = 1f - Mathf.Exp(-scaleSpeed * Time.unscaledDeltaTime);
                chips[i].transform.localScale = Vector3.Lerp(chips[i].transform.localScale, target, t);
            }

            if (i < activeMarkers.Length && activeMarkers[i] != null)
            {
                activeMarkers[i].SetActive(active);
            }

            if (i < letters.Length && letters[i] != null)
            {
                letters[i].text = color.ToString().Substring(0, 1).ToUpper();
            }

            if (i < counts.Length && counts[i] != null)
            {
                counts[i].text = user.BulletsOf(color).ToString();
            }
        }

        if (tintWithActive != null)
        {
            Color activeColor = BattleColorUtil.ToUnityColor(gun.CurrentColor);
            foreach (Graphic g in tintWithActive)
            {
                if (g != null)
                {
                    activeColor.a = g.color.a;
                    g.color = activeColor;
                }
            }
        }

        if (keyHint != null)
        {
            string keyName = gun.swapKey == KeyCode.LeftShift ? "SHIFT" : gun.swapKey.ToString().ToUpper();
            keyHint.text = "[" + keyName + "] SWAP";
        }
    }
}
