using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TurnOrderBar : MonoBehaviour
{
    class Entry
    {
        public RectTransform rect;
        public Image background;
        public Image portrait;
        public TMP_Text label;
        public Outline outline;
    }

    public BattleManager battleManager;
    public RectTransform container;
    public int visibleCount = 6;
    public float activeSize = 96f;
    public float cardSize = 66f;
    public float spacing = 10f;
    public float gapAfterActive = 18f;
    public Color playerColor = new Color(0.95f, 0.95f, 0.95f);
    public Color enemyColor = new Color(0.22f, 0.22f, 0.22f);
    public Color playerText = Color.black;
    public Color enemyText = Color.white;

    List<Entry> entries = new List<Entry>();
    bool dirty = true;

    void OnEnable()
    {
        if (battleManager != null)
        {
            battleManager.TurnStarted += OnTurnStarted;
            battleManager.StateChanged += OnStateChanged;
        }
        Combatant.AnyDied += OnDied;
    }

    void OnDisable()
    {
        if (battleManager != null)
        {
            battleManager.TurnStarted -= OnTurnStarted;
            battleManager.StateChanged -= OnStateChanged;
        }
        Combatant.AnyDied -= OnDied;
    }

    void Start()
    {
        if (container == null || battleManager == null)
        {
            Debug.LogError("TurnOrderBar: assign the battle manager and the container");
            enabled = false;
            return;
        }

        for (int i = 0; i < visibleCount; i++)
        {
            entries.Add(CreateEntry(i));
        }

        Refresh();
    }

    void LateUpdate()
    {
        if (dirty)
        {
            Refresh();
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

    void OnDied(Combatant c)
    {
        dirty = true;
    }

    void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }

    Entry CreateEntry(int index)
    {
        Entry e = new Entry();

        GameObject root = new GameObject("TurnCard " + index, typeof(RectTransform), typeof(Image));
        root.transform.SetParent(container, false);

        e.rect = root.GetComponent<RectTransform>();
        e.rect.anchorMin = new Vector2(0f, 1f);
        e.rect.anchorMax = new Vector2(0f, 1f);
        e.rect.pivot = new Vector2(0f, 1f);

        e.background = root.GetComponent<Image>();
        e.background.raycastTarget = false;

        e.outline = root.AddComponent<Outline>();
        e.outline.effectColor = Color.white;
        e.outline.effectDistance = new Vector2(3f, -3f);

        GameObject portraitObject = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
        portraitObject.transform.SetParent(root.transform, false);
        Stretch(portraitObject.GetComponent<RectTransform>());
        e.portrait = portraitObject.GetComponent<Image>();
        e.portrait.raycastTarget = false;
        e.portrait.preserveAspect = true;

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(root.transform, false);
        Stretch(labelObject.GetComponent<RectTransform>());
        TextMeshProUGUI text = labelObject.GetComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 10f;
        text.fontSizeMax = 32f;
        text.fontStyle = FontStyles.Bold;
        text.raycastTarget = false;
        e.label = text;

        return e;
    }

    string Initials(Combatant c)
    {
        string baseName = c.data != null ? c.data.displayName : c.name;
        string letters = baseName.Length > 3 ? baseName.Substring(0, 3) : baseName;
        letters = letters.ToUpper();

        if (!c.isPlayerSide)
        {
            string objectName = c.name;
            int space = objectName.LastIndexOf(' ');
            if (space >= 0 && space < objectName.Length - 1)
            {
                letters += objectName.Substring(space + 1);
            }
        }

        return letters;
    }

    void Refresh()
    {
        dirty = false;

        List<Combatant> upcoming = battleManager.GetUpcomingTurns(visibleCount);
        float y = 0f;

        for (int i = 0; i < entries.Count; i++)
        {
            Entry e = entries[i];

            if (i >= upcoming.Count)
            {
                e.rect.gameObject.SetActive(false);
                continue;
            }

            e.rect.gameObject.SetActive(true);

            Combatant c = upcoming[i];
            float size = i == 0 ? activeSize : cardSize;

            e.rect.sizeDelta = new Vector2(size, size);
            e.rect.anchoredPosition = new Vector2(0f, -y);
            y += size + (i == 0 ? gapAfterActive : spacing);

            bool player = c.isPlayerSide;
            e.background.color = player ? playerColor : enemyColor;
            e.outline.enabled = i == 0;

            Sprite portrait = c.data != null ? c.data.portrait : null;
            e.portrait.sprite = portrait;
            e.portrait.enabled = portrait != null;

            e.label.enabled = portrait == null;
            e.label.text = Initials(c);
            e.label.color = player ? playerText : enemyText;
        }
    }
}
