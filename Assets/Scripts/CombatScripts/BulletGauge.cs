using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BulletGauge : MonoBehaviour
{
    public BattleManager battleManager;
    public GunMode gun;
    public CanvasGroup group;
    public Image fill;
    public TMP_Text countText;
    public TMP_Text colorLabel;
    public TMP_Text keyHint;
    public Graphic[] tintWithColor;
    public int maxBullets = 40;
    public float emptyAlpha = 0.4f;
    public float fillSpeed = 10f;
    public float popAmount = 0.2f;
    public float popDecay = 8f;
    public bool showWhileChoosingAlly = true;

    Combatant lastUser;
    BattleColor lastColor;
    float pop;
    float shownFill;
    Vector3 baseScale = Vector3.one;

    void Awake()
    {
        baseScale = transform.localScale;

        if (fill != null)
        {
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
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
            && (showWhileChoosingAlly || !battleManager.IsTargetingAlly)
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

        BattleColor color = gun.CurrentColor;

        if (user != lastUser)
        {
            lastUser = user;
            lastColor = color;
            shownFill = -1f;
        }
        else if (color != lastColor)
        {
            lastColor = color;
            pop = 1f;
        }

        pop = Mathf.MoveTowards(pop, 0f, popDecay * Time.unscaledDeltaTime);

        int count = user.BulletsOf(color);
        float ratio = maxBullets > 0 ? Mathf.Clamp01((float)count / maxBullets) : 0f;

        if (shownFill < 0f)
        {
            shownFill = ratio;
        }
        else
        {
            shownFill = Mathf.Lerp(shownFill, ratio, 1f - Mathf.Exp(-fillSpeed * Time.unscaledDeltaTime));
        }

        Color tint = BattleColorUtil.ToUnityColor(color);
        tint.a = count > 0 ? 1f : emptyAlpha;

        if (fill != null)
        {
            fill.color = tint;
            fill.fillAmount = shownFill;
        }

        if (countText != null)
        {
            countText.text = count.ToString();
            countText.color = tint;
        }

        if (colorLabel != null)
        {
            colorLabel.text = color.ToString().ToUpper();
            colorLabel.color = tint;
        }

        if (tintWithColor != null)
        {
            foreach (Graphic g in tintWithColor)
            {
                if (g != null)
                {
                    Color c = BattleColorUtil.ToUnityColor(color);
                    c.a = g.color.a;
                    g.color = c;
                }
            }
        }

        if (keyHint != null)
        {
            string keyName = gun.swapKey == KeyCode.LeftShift ? "SHIFT" : gun.swapKey.ToString().ToUpper();
            keyHint.text = "[" + keyName + "] SWAP";
        }

        transform.localScale = baseScale * (1f + popAmount * pop);
    }
}