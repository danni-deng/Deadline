using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BulletGauge : MonoBehaviour
{
    public BattleManager battleManager;
    public GunMode gun;
    public Image fillImage;
    public TMP_Text countText;
    public bool tintToColor = true;
    public float smoothSpeed = 8f;
    public int overrideMax = 0;

    Combatant shown;
    float display = -1f;

    void Awake()
    {
        if (fillImage != null)
        {
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = 0;
        }
    }

    void Update()
    {
        if (battleManager == null)
        {
            return;
        }

        Combatant active = battleManager.ActiveCombatant;
        if (active != null && active.isPlayerSide && active.data != null)
        {
            shown = active;
        }

        if (shown == null && battleManager.party.Count > 0)
        {
            shown = battleManager.party[0];
        }

        if (shown == null || shown.data == null)
        {
            return;
        }

        BattleColor color = shown.data.bulletColorA;
        if (gun != null && active == shown)
        {
            color = gun.CurrentColor;
        }

        int max = overrideMax > 0 ? overrideMax : Mathf.Max(1, shown.data.bulletsPerColor);
        int count = shown.BulletsOf(color);
        float target = Mathf.Clamp01((float)count / max);

        if (display < 0f)
        {
            display = target;
        }
        else
        {
            display = Mathf.MoveTowards(display, target, smoothSpeed * Time.unscaledDeltaTime);
        }

        Color tint = BattleColorUtil.ToUnityColor(color);

        if (fillImage != null)
        {
            fillImage.fillAmount = display;

            if (tintToColor)
            {
                fillImage.color = tint;
            }
        }

        if (countText != null)
        {
            countText.text = count.ToString();

            if (tintToColor)
            {
                countText.color = tint;
            }
        }
    }
}