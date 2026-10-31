using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PartyProfiles : MonoBehaviour
{
    class Card
    {
        
        public Combatant combatant;
        public RectTransform rect;
        public Image background;
        public Image portrait;
        public TMP_Text label;
        public RectTransform hpFill;
        public Outline outline;
        public bool picked;
        public RectTransform[] pipRects = new RectTransform[3];
        public Image[] pipImages = new Image[3];
        public TMP_Text[] pipTexts = new TMP_Text[3];
        public Outline[] pipOutlines = new Outline[3];
        public float[] popTimers = new float[3];
    }

    public Sprite chipSprite;
    public BattleManager battleManager;
    public RectTransform container;
    public float cardSize = 120f;
    public float pipSize = 30f;
    public float spacing = 16f;
    public Color cardColor = new Color(0.08f, 0.08f, 0.08f);
    public Color deadColor = new Color(0.3f, 0.3f, 0.3f);
    public Color activeOutline = Color.white;
    public Color pickOutline = new Color(0.6f, 0.9f, 1f);
    public Color hpColor = new Color(0.2f, 0.85f, 0.3f);
    public float pulseAmount = 0.05f;
    public float pulseSpeed = 6f;
    public float popDuration = 0.5f;

    List<Card> cards = new List<Card>();
    TMP_Text prompt;
    bool dirty = true;

    void OnEnable()
    {
        if (battleManager == null)
        {
            return;
        }

        battleManager.TurnStarted += OnTurnStarted;
        battleManager.StateChanged += OnStateChanged;
        battleManager.AllyChanged += OnAllyChanged;
        battleManager.AllyTargetingChanged += OnTargetingChanged;
        battleManager.StrikeChipChanged += OnStrikeChipChanged;
        Combatant.AnyDamaged += OnCombatantInt;
        Combatant.AnyHealed += OnCombatantInt;
        Combatant.AnyChipsChanged += OnCombatant;
        Combatant.AnyDied += OnCombatant;
        Combatant.AnyColorGained += OnColorGained;
    }

    void OnDisable()
    {
        if (battleManager == null)
        {
            return;
        }

        battleManager.TurnStarted -= OnTurnStarted;
        battleManager.StateChanged -= OnStateChanged;
        battleManager.AllyChanged -= OnAllyChanged;
        battleManager.AllyTargetingChanged -= OnTargetingChanged;
        battleManager.StrikeChipChanged -= OnStrikeChipChanged;
        Combatant.AnyDamaged -= OnCombatantInt;
        Combatant.AnyHealed -= OnCombatantInt;
        Combatant.AnyChipsChanged -= OnCombatant;
        Combatant.AnyDied -= OnCombatant;
        Combatant.AnyColorGained -= OnColorGained;
    }

    void Start()
    {
        if (battleManager == null || container == null)
        {
   
            enabled = false;
            return;
        }

        for (int i = 0; i < battleManager.party.Count; i++)
        {
            if (battleManager.party[i] != null)
            {
                cards.Add(CreateCard(cards.Count, battleManager.party[i]));
            }
        }

        prompt = CreatePrompt();
        Refresh();
    }

    void LateUpdate()
    {
        if (dirty)
        {
            Refresh();
        }

        float pulse = 1f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmount;

        foreach (Card card in cards)
        {
            card.rect.localScale = card.picked ? Vector3.one * pulse : Vector3.one;

            for (int i = 0; i < 3; i++)
            {
                float scale = 1f;
                if (card.popTimers[i] > 0f)
                {
                    card.popTimers[i] -= Time.unscaledDeltaTime;
                    float p = Mathf.Clamp01(card.popTimers[i] / popDuration);
                    scale = 1f + 0.6f * Mathf.Sin(p * Mathf.PI);
                }
                card.pipRects[i].localScale = Vector3.one * scale;
            }
        }
    }

    void OnTurnStarted(Combatant c)
    {
        dirty = true;
    }

    void OnStateChanged(BattleState state)
    {
        dirty = true;
    }

    void OnAllyChanged(Combatant ally)
    {
        dirty = true;
    }

    void OnTargetingChanged(bool targeting)
    {
        dirty = true;
    }

    void OnStrikeChipChanged(BattleColor color)
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

    void OnColorGained(Combatant enemy, Combatant shooter, BattleColor color)
    {
        foreach (Card card in cards)
        {
            if (card.combatant == shooter)
            {
                card.popTimers[(int)color] = popDuration;
            }
        }
        dirty = true;
    }

    void Stretch(RectTransform r, float left, float bottom, float right, float top)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(left, bottom);
        r.offsetMax = new Vector2(-right, -top);
    }

    string Initials(Combatant c)
    {
        string baseName = c.data != null ? c.data.displayName : c.name;
        string letters = baseName.Length > 3 ? baseName.Substring(0, 3) : baseName;
        return letters.ToUpper();
    }

    Card CreateCard(int index, Combatant c)
    {
        Card card = new Card();
        card.combatant = c;

        GameObject root = new GameObject("Profile " + c.name, typeof(RectTransform), typeof(Image), typeof(Button));
        root.transform.SetParent(container, false);

        card.rect = root.GetComponent<RectTransform>();
        card.rect.anchorMin = Vector2.zero;
        card.rect.anchorMax = Vector2.zero;
        card.rect.pivot = new Vector2(0.5f, 0.5f);
        card.rect.sizeDelta = new Vector2(cardSize, cardSize);
        card.rect.anchoredPosition = new Vector2(index * (cardSize + spacing) + cardSize * 0.5f, cardSize * 0.5f);

        card.background = root.GetComponent<Image>();
        card.background.color = cardColor;

        Button button = root.GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        Combatant captured = c;
        button.onClick.AddListener(delegate { Pick(captured); });

        card.outline = root.AddComponent<Outline>();
        card.outline.effectDistance = new Vector2(4f, -4f);
        card.outline.enabled = false;

        GameObject portraitObject = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
        portraitObject.transform.SetParent(root.transform, false);
        Stretch(portraitObject.GetComponent<RectTransform>(), 6f, 24f, 6f, pipSize + 12f);
        card.portrait = portraitObject.GetComponent<Image>();
        card.portrait.raycastTarget = false;
        card.portrait.preserveAspect = true;
        Sprite sprite = c.data != null ? c.data.portrait : null;
        card.portrait.sprite = sprite;
        card.portrait.enabled = sprite != null;

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(root.transform, false);
        Stretch(labelObject.GetComponent<RectTransform>(), 6f, 24f, 6f, pipSize + 12f);
        TextMeshProUGUI text = labelObject.GetComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 12f;
        text.fontSizeMax = 40f;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.raycastTarget = false;
        text.text = Initials(c);
        text.enabled = sprite == null;
        card.label = text;

        GameObject hpBack = new GameObject("HpBack", typeof(RectTransform), typeof(Image));
        hpBack.transform.SetParent(root.transform, false);
        RectTransform hpBackRect = hpBack.GetComponent<RectTransform>();
        hpBackRect.anchorMin = new Vector2(0f, 0f);
        hpBackRect.anchorMax = new Vector2(1f, 0f);
        hpBackRect.pivot = new Vector2(0.5f, 0f);
        hpBackRect.offsetMin = new Vector2(8f, 8f);
        hpBackRect.offsetMax = new Vector2(-8f, 20f);
        Image hpBackImage = hpBack.GetComponent<Image>();
        hpBackImage.color = new Color(0.25f, 0.25f, 0.25f);
        hpBackImage.raycastTarget = false;

        GameObject fill = new GameObject("HpFill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(hpBack.transform, false);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Image fillImage = fill.GetComponent<Image>();
        fillImage.color = hpColor;
        fillImage.raycastTarget = false;
        card.hpFill = fillRect;

        for (int i = 0; i < 3; i++)
        {
            GameObject pip = new GameObject("Chip " + i, typeof(RectTransform), typeof(Image), typeof(Button));
            pip.transform.SetParent(root.transform, false);

            RectTransform pipRect = pip.GetComponent<RectTransform>();
            pipRect.anchorMin = new Vector2(0f, 1f);
            pipRect.anchorMax = new Vector2(0f, 1f);
            pipRect.pivot = new Vector2(0.5f, 0.5f);
            pipRect.sizeDelta = new Vector2(pipSize, pipSize);
            pipRect.anchoredPosition = new Vector2(8f + pipSize * 0.5f + i * (pipSize + 4f), -(6f + pipSize * 0.5f));
            card.pipRects[i] = pipRect;

         card.pipImages[i] = pip.GetComponent<Image>();
         card.pipImages[i].sprite = chipSprite;
         card.pipImages[i].type = Image.Type.Simple;
         card.pipImages[i].preserveAspect = true;
          card.pipImages[i].color = BattleColorUtil.ToUnityColor((BattleColor)i);

            Button pipButton = pip.GetComponent<Button>();
            pipButton.transition = Selectable.Transition.None;
            int colorIndex = i;
            pipButton.onClick.AddListener(delegate { ClickPip(captured, colorIndex); });

            Outline pipOutline = pip.AddComponent<Outline>();
            pipOutline.effectColor = Color.white;
            pipOutline.effectDistance = new Vector2(3f, -3f);
            pipOutline.enabled = false;
            card.pipOutlines[i] = pipOutline;

            GameObject countObject = new GameObject("Count", typeof(RectTransform), typeof(TextMeshProUGUI));
            countObject.transform.SetParent(pip.transform, false);
            Stretch(countObject.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
            TextMeshProUGUI countText = countObject.GetComponent<TextMeshProUGUI>();
            countText.alignment = TextAlignmentOptions.Center;
            countText.fontSize = 20f;
            countText.fontStyle = FontStyles.Bold;
            countText.color = i == (int)BattleColor.Yellow ? Color.black : Color.white;
            countText.raycastTarget = false;
            countText.text = "";
            card.pipTexts[i] = countText;
        }

        return card;
    }

    TMP_Text CreatePrompt()
    {
        GameObject go = new GameObject("AllyPrompt", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(container, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0f, 0f);
        rect.sizeDelta = new Vector2(900f, 40f);
        rect.anchoredPosition = new Vector2(0f, cardSize + 12f);

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.BottomLeft;
        text.fontSize = 28f;
        text.fontStyle = FontStyles.Bold;
        text.color = pickOutline;
        text.raycastTarget = false;
        text.text = "";
        return text;
    }

    void Pick(Combatant c)
    {
        if (battleManager.State != BattleState.PlayerTurn || !battleManager.IsTargetingAlly)
        {
            return;
        }
        if (c == null || !c.IsAlive)
        {
            return;
        }

        for (int i = 0; i < battleManager.party.Count; i++)
        {
            if (battleManager.SelectedAlly == c)
            {
                break;
            }
            battleManager.CycleAlly(1);
        }

        battleManager.ConfirmAllyTarget();
    }

    void ClickPip(Combatant owner, int colorIndex)
    {
        if (battleManager.IsTargetingAlly)
        {
            Pick(owner);
            return;
        }

        if (owner == battleManager.ActiveCombatant)
        {
            battleManager.SelectStrikeChip((BattleColor)colorIndex);
        }
    }

    void Refresh()
    {
        dirty = false;

        BattleState state = battleManager.State;
        bool playerTurn = state == BattleState.PlayerTurn;
        bool acting = playerTurn || state == BattleState.Resolving;
        bool targeting = playerTurn && battleManager.IsTargetingAlly;
        Combatant active = battleManager.ActiveCombatant;
        Combatant selected = battleManager.SelectedAlly;

        foreach (Card card in cards)
        {
            Combatant c = card.combatant;
            bool alive = c.IsAlive;
            bool isActive = acting && c == active;
            card.picked = targeting && c == selected;

            float ratio = 0f;
            if (c.data != null && c.data.maxHP > 0)
            {
                ratio = Mathf.Clamp01((float)c.currentHP / c.data.maxHP);
            }
            card.hpFill.anchorMax = new Vector2(ratio, 1f);

            card.background.color = alive ? cardColor : deadColor;
            card.outline.enabled = isActive || card.picked;
            card.outline.effectColor = card.picked ? pickOutline : activeOutline;

            for (int i = 0; i < 3; i++)
            {
                int count = c.chips[i];

                Color pipColor = BattleColorUtil.ToUnityColor((BattleColor)i);
                pipColor.a = count > 0 ? 1f : 0.25f;
                card.pipImages[i].color = pipColor;
                card.pipTexts[i].text = count > 0 ? count.ToString() : "";

                bool chosen = isActive && playerTurn && count > 0 && (int)battleManager.StrikeChipColor == i;
                card.pipOutlines[i].enabled = chosen;
            }
        }

        if (targeting)
        {
            string abilityName = "ABILITY";
            int pending = battleManager.PendingAbility;
            if (active != null && pending >= 0 && pending < active.data.abilities.Length && active.data.abilities[pending] != null)
            {
                abilityName = active.data.abilities[pending].abilityName.ToUpper();
            }

            prompt.text = abilityName + ": CLICK WHO RECEIVES IT (ESC TO CANCEL)";
        }
        else
        {
            prompt.text = "";
        }
    }
}
