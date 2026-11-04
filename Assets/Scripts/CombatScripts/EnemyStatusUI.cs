using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyStatusUI : MonoBehaviour
{
    class MiniWidget
    {
        public Combatant combatant;
        public RectTransform rect;
        public SpriteRenderer[] renderers;
        public Image[] images = new Image[4];
        public TextMeshProUGUI[] letters = new TextMeshProUGUI[4];
        public TextMeshProUGUI[] turns = new TextMeshProUGUI[4];
        public int shown;
    }

    public BattleManager battleManager;
    public StatusIcons icons;
    public float panelWidth = 600f;
    public float panelHeight = 200f;
    public float meterWidth = 300f;
    public float miniIconSize = 40f;
    public float panelGap = 112f;
    public int maxSegments = 12;
    public Color panelColor = new Color(0.04f, 0.04f, 0.05f, 0.94f);
    public Color panelOutline = Color.white;
    public Color emptySegment = new Color(0.22f, 0.22f, 0.24f);
    public Color resistColor = new Color(0.36f, 0.59f, 1f);

    static BattleColor[] order = new BattleColor[] { BattleColor.Red, BattleColor.Blue, BattleColor.Yellow };

    Canvas canvas;
    RectTransform canvasRect;
    Camera cam;
    bool dirty = true;
    List<MiniWidget> minis = new List<MiniWidget>();

    RectTransform panel;
    RectTransform pointer;
    TextMeshProUGUI nameText;
    TextMeshProUGUI hpText;
    TextMeshProUGUI labelText;
    TextMeshProUGUI countText;
    TextMeshProUGUI resistText;
    GameObject resistPill;
    Image[] segments;
    Image[] panelImages = new Image[3];
    TextMeshProUGUI[] panelLetters = new TextMeshProUGUI[3];
    TextMeshProUGUI[] panelTurns = new TextMeshProUGUI[3];

    void OnEnable()
    {
        if (battleManager != null)
        {
            battleManager.TargetChanged += OnTarget;
            battleManager.StateChanged += OnState;
            battleManager.TurnStarted += OnTarget;
        }
        Combatant.AnyStatusChanged += OnCombatant;
        Combatant.AnyDied += OnCombatant;
        Combatant.AnyDamaged += OnCombatantInt;
        Combatant.AnyHealed += OnCombatantInt;
    }

    void OnDisable()
    {
        if (battleManager != null)
        {
            battleManager.TargetChanged -= OnTarget;
            battleManager.StateChanged -= OnState;
            battleManager.TurnStarted -= OnTarget;
        }
        Combatant.AnyStatusChanged -= OnCombatant;
        Combatant.AnyDied -= OnCombatant;
        Combatant.AnyDamaged -= OnCombatantInt;
        Combatant.AnyHealed -= OnCombatantInt;
    }

    void OnTarget(Combatant c)
    {
        dirty = true;
    }

    void OnState(BattleState state)
    {
        dirty = true;
    }

    void OnCombatant(Combatant c)
    {
        dirty = true;
    }

    void OnCombatantInt(Combatant c, int amount)
    {
        dirty = true;
    }

    void Start()
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null || battleManager == null)
        {
            Debug.LogError("EnemyStatusUI: put this on the Canvas and assign the battle manager");
            enabled = false;
            return;
        }

        canvas = canvas.rootCanvas;
        canvasRect = canvas.GetComponent<RectTransform>();
        BuildPanel();
    }

    void LateUpdate()
    {
        if (cam == null)
        {
            cam = Camera.main;
        }

        SyncMinis();

        if (dirty)
        {
            RefreshAll();
        }

        PositionMinis();
        PositionPanel();
    }

    MiniWidget FindMini(Combatant c)
    {
        for (int i = 0; i < minis.Count; i++)
        {
            if (minis[i].combatant == c)
            {
                return minis[i];
            }
        }
        return null;
    }

    void SyncMinis()
    {
        foreach (Combatant c in battleManager.enemies)
        {
            if (c != null && FindMini(c) == null)
            {
                MiniWidget created = CreateMini(c);
                minis.Add(created);
                RefreshMini(created);
            }
        }

        for (int i = minis.Count - 1; i >= 0; i--)
        {
            if (minis[i].combatant == null)
            {
                Destroy(minis[i].rect.gameObject);
                minis.RemoveAt(i);
            }
        }
    }

    MiniWidget CreateMini(Combatant c)
    {
        MiniWidget m = new MiniWidget();
        m.combatant = c;
        m.renderers = c.GetComponentsInChildren<SpriteRenderer>();

        Vector2 center = new Vector2(0.5f, 0.5f);
        m.rect = UiBuild.Box(canvasRect, "Status " + c.name, center, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(240f, miniIconSize + 40f));

        for (int i = 0; i < 4; i++)
        {
            TextMeshProUGUI letter;
            m.images[i] = UiBuild.Icon(m.rect, "Icon " + i, new Vector2(0.5f, 0f), center, Vector2.zero, miniIconSize, out letter);
            m.letters[i] = letter;
            m.turns[i] = UiBuild.Text(m.images[i].transform, "Turns", new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(60f, 26f), 22f, TextAlignmentOptions.Center, Color.white, FontStyles.Bold);
        }

        return m;
    }

    void SetResistIcon(Image image, TextMeshProUGUI letter, TextMeshProUGUI turnsText, int turns)
    {
        if (icons != null && icons.resistant != null)
        {
            image.sprite = icons.resistant;
            image.color = Color.white;
            letter.text = "";
        }
        else
        {
            image.sprite = null;
            image.color = resistColor;
            letter.text = "R";
            letter.color = Color.white;
        }

        turnsText.text = turns.ToString();
    }

    void RefreshAll()
    {
        dirty = false;
        RefreshPanel();

        foreach (MiniWidget m in minis)
        {
            RefreshMini(m);
        }
    }

    void RefreshMini(MiniWidget m)
    {
        Combatant c = m.combatant;
        if (c == null)
        {
            return;
        }

        int count = 0;
        for (int i = 0; i < order.Length; i++)
        {
            if (c.DebuffTurns(order[i]) > 0)
            {
                count++;
            }
        }
        if (c.IsResistant)
        {
            count++;
        }

        float spacing = miniIconSize + 8f;
        float startX = -(count - 1) * spacing * 0.5f;
        float y = 28f + miniIconSize * 0.5f;
        int slot = 0;

        for (int i = 0; i < order.Length; i++)
        {
            int turns = c.DebuffTurns(order[i]);
            if (turns <= 0)
            {
                continue;
            }

            UiBuild.ApplyDebuffIcon(m.images[slot], m.letters[slot], icons, order[i]);
            m.turns[slot].text = turns.ToString();
            m.images[slot].rectTransform.anchoredPosition = new Vector2(startX + slot * spacing, y);
            slot++;
        }

        if (c.IsResistant)
        {
            SetResistIcon(m.images[slot], m.letters[slot], m.turns[slot], c.resistTurns);
            m.images[slot].rectTransform.anchoredPosition = new Vector2(startX + slot * spacing, y);
            slot++;
        }

        for (int i = 0; i < m.images.Length; i++)
        {
            m.images[i].gameObject.SetActive(i < slot);
        }

        m.shown = slot;
    }

    void BuildPanel()
    {
        Vector2 center = new Vector2(0.5f, 0.5f);
        Vector2 topLeft = new Vector2(0f, 1f);
        Vector2 topRight = new Vector2(1f, 1f);

        Image background = UiBuild.Panel(canvasRect, "Target Panel", center, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(panelWidth, panelHeight), panelColor);
        UiBuild.AddOutline(background, panelOutline, 3f);
        panel = background.rectTransform;

        nameText = UiBuild.Text(panel, "Name", topLeft, topLeft, new Vector2(24f, -14f), new Vector2(330f, 40f), 30f, TextAlignmentOptions.Left, Color.white, FontStyles.Bold);
        hpText = UiBuild.Text(panel, "Hp", topRight, topRight, new Vector2(-24f, -14f), new Vector2(240f, 40f), 28f, TextAlignmentOptions.Right, Color.white, FontStyles.Bold);

        UiBuild.Panel(panel, "Divider", topLeft, topLeft, new Vector2(24f, -62f), new Vector2(panelWidth - 48f, 2f), panelOutline);

        labelText = UiBuild.Text(panel, "Label", topLeft, topLeft, new Vector2(24f, -70f), new Vector2(330f, 32f), 26f, TextAlignmentOptions.Left, Color.white, FontStyles.Bold);

        RectTransform meter = UiBuild.Box(panel, "Meter", topLeft, topLeft, new Vector2(24f, -108f), new Vector2(meterWidth, 28f));
        segments = new Image[maxSegments];
        for (int i = 0; i < maxSegments; i++)
        {
            segments[i] = UiBuild.Panel(meter, "Segment " + i, topLeft, topLeft, Vector2.zero, new Vector2(20f, 28f), emptySegment);
        }

        countText = UiBuild.Text(panel, "Count", topLeft, topLeft, new Vector2(24f + meterWidth + 14f, -102f), new Vector2(90f, 40f), 30f, TextAlignmentOptions.Left, Color.white, FontStyles.Bold);

        for (int i = 0; i < 3; i++)
        {
            TextMeshProUGUI letter;
            panelImages[i] = UiBuild.Icon(panel, "Debuff " + i, topRight, topRight, Vector2.zero, 44f, out letter);
            panelLetters[i] = letter;
            panelTurns[i] = UiBuild.Text(panelImages[i].transform, "Turns", new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(60f, 26f), 22f, TextAlignmentOptions.Center, Color.white, FontStyles.Bold);
        }

        Image pill = UiBuild.Panel(panel, "Resist", topLeft, topLeft, new Vector2(24f, -150f), new Vector2(330f, 36f), new Color(0.15f, 0.15f, 0.17f));
        resistPill = pill.gameObject;
        Vector2 leftCenter = new Vector2(0f, 0.5f);
        UiBuild.Panel(pill.transform, "Shield", leftCenter, leftCenter, new Vector2(6f, 0f), new Vector2(26f, 26f), resistColor);
        resistText = UiBuild.Text(pill.transform, "Text", leftCenter, leftCenter, new Vector2(42f, 0f), new Vector2(280f, 32f), 24f, TextAlignmentOptions.Left, Color.white, FontStyles.Bold);

        pointer = UiBuild.Box(panel, "Pointer", new Vector2(0.5f, 0f), center, Vector2.zero, new Vector2(1f, 1f));
        Image border = UiBuild.Panel(pointer, "Border", center, center, Vector2.zero, new Vector2(34f, 34f), panelOutline);
        border.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        Image inner = UiBuild.Panel(pointer, "Inner", center, center, new Vector2(0f, 4f), new Vector2(28f, 28f), new Color(panelColor.r, panelColor.g, panelColor.b, 1f));
        inner.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);

        panel.gameObject.SetActive(false);
    }

    void RefreshPanel()
    {
        if (panel == null)
        {
            return;
        }

        Combatant t = battleManager.SelectedTarget;
        bool show = t != null && t.IsAlive && t.data != null && battleManager.State == BattleState.PlayerTurn;
        panel.gameObject.SetActive(show);

        if (!show)
        {
            return;
        }

        nameText.text = t.data.displayName;
        hpText.text = "HP   " + t.currentHP + " / " + t.data.maxHP;

        int lead = t.LeadingBuildup();
        int threshold = t.BuildupThreshold;
        int filled = lead >= 0 ? t.buildup[lead] : 0;
        Color fill = lead >= 0 ? BattleColorUtil.ToUnityColor((BattleColor)lead) : emptySegment;

        if (lead >= 0)
        {
            labelText.text = ((BattleColor)lead).ToString() + " Buildup";
            labelText.color = fill;
        }
        else if (t.IsResistant)
        {
            labelText.text = "Buildup Blocked";
            labelText.color = resistColor;
        }
        else
        {
            labelText.text = "No Buildup";
            labelText.color = new Color(0.7f, 0.7f, 0.7f);
        }

        countText.text = filled + " / " + threshold;

        int visible = Mathf.Clamp(threshold, 1, segments.Length);
        float gap = 4f;
        float width = (meterWidth - gap * (visible - 1)) / visible;

        for (int i = 0; i < segments.Length; i++)
        {
            bool used = i < visible;
            segments[i].gameObject.SetActive(used);

            if (!used)
            {
                continue;
            }

            segments[i].rectTransform.sizeDelta = new Vector2(width, 28f);
            segments[i].rectTransform.anchoredPosition = new Vector2(i * (width + gap), 0f);
            segments[i].color = i < filled ? fill : emptySegment;
        }

        int placed = 0;
        for (int i = 0; i < order.Length; i++)
        {
            int turns = t.DebuffTurns(order[i]);
            bool active = turns > 0;
            panelImages[i].gameObject.SetActive(active);

            if (!active)
            {
                continue;
            }

            UiBuild.ApplyDebuffIcon(panelImages[i], panelLetters[i], icons, order[i]);
            panelTurns[i].text = turns.ToString();
            panelImages[i].rectTransform.anchoredPosition = new Vector2(-24f - placed * 54f, -74f);
            placed++;
        }

        resistPill.SetActive(t.IsResistant);
        resistText.text = "Resistant for " + t.resistTurns + (t.resistTurns == 1 ? " turn" : " turns");
    }

    bool TryGetLocal(Vector3 world, out Vector2 local)
    {
        local = Vector2.zero;

        if (cam == null)
        {
            return false;
        }

        Vector3 screen = cam.WorldToScreenPoint(world);
        if (screen.z <= 0f)
        {
            return false;
        }

        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, uiCamera, out local);
    }

    Vector3 TopOf(MiniWidget m)
    {
        Bounds bounds = new Bounds(m.combatant.transform.position, Vector3.zero);
        bool has = false;

        foreach (SpriteRenderer sr in m.renderers)
        {
            if (sr == null)
            {
                continue;
            }

            if (!has)
            {
                bounds = sr.bounds;
                has = true;
            }
            else
            {
                bounds.Encapsulate(sr.bounds);
            }
        }

        return new Vector3(bounds.center.x, bounds.max.y, m.combatant.transform.position.z);
    }

    void PositionMinis()
    {
        foreach (MiniWidget m in minis)
        {
            bool show = m.combatant != null && m.combatant.IsAlive && m.combatant.gameObject.activeInHierarchy && m.shown > 0;
            Vector2 local;

            if (show && TryGetLocal(TopOf(m), out local))
            {
                m.rect.gameObject.SetActive(true);
                m.rect.anchoredPosition = local + new Vector2(0f, 6f);
            }
            else
            {
                m.rect.gameObject.SetActive(false);
            }
        }
    }

    void PositionPanel()
    {
        if (panel == null || !panel.gameObject.activeSelf)
        {
            return;
        }

        Combatant t = battleManager.SelectedTarget;
        MiniWidget m = t != null ? FindMini(t) : null;
        Vector2 local;

        if (m == null || !TryGetLocal(TopOf(m), out local))
        {
            return;
        }

        float limit = canvasRect.rect.width * 0.5f - panelWidth * 0.5f - 10f;
        float x = Mathf.Clamp(local.x, -limit, limit);
        float maxY = canvasRect.rect.height * 0.5f - panelHeight - 10f;
        float y = Mathf.Min(local.y + panelGap, maxY);

        panel.anchoredPosition = new Vector2(x, y);

        float pointerLimit = panelWidth * 0.5f - 40f;
        pointer.anchoredPosition = new Vector2(Mathf.Clamp(local.x - x, -pointerLimit, pointerLimit), 0f);
    }
}
