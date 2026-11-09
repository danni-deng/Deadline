using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TurnOrderBar : MonoBehaviour
{
    public BattleManager battleManager;
    public Vector2 anchor = new Vector2(0.5f, 1f);
    public Vector2 offset = new Vector2(0f, -30f);
    public int count = 8;
    public float activeSize = 84f;
    public float cardSize = 58f;
    public float spacing = 8f;
    public float activeGap = 18f;
    public float frameThickness = 3f;
    public Color partyFrame = Color.white;
    public Color enemyFrame = new Color(0.9f, 0.3f, 0.25f);
    public Color cardBackground = new Color(0.05f, 0.05f, 0.06f, 0.95f);
    public int initialLength = 2;
    public float refreshInterval = 0.15f;
    public float moveSpeed = 14f;
    public int sortingOrder = 40;

    class Card
    {
        public RectTransform rect;
        public Image frame;
        public Image back;
        public Image portrait;
        public TextMeshProUGUI label;
        public Vector2 targetPos;
        public float targetSize;
        public bool placed;
    }

    List<Card> cards = new List<Card>();
    List<Combatant> shown = new List<Combatant>();
    RectTransform root;
    CanvasGroup group;
    float timer;

    void Start()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null || battleManager == null)
        {
            Debug.LogError("TurnOrderBar: put this on the Canvas and assign the battle manager");
            enabled = false;
            return;
        }

        root = NewRect("Turn Order", canvas.rootCanvas.transform);
        root.anchorMin = anchor;
        root.anchorMax = anchor;
        root.pivot = new Vector2(0.5f, 1f);
        root.anchoredPosition = offset;
        root.sizeDelta = new Vector2(10f, activeSize);

        Canvas own = root.gameObject.AddComponent<Canvas>();
        own.overrideSorting = true;
        own.sortingOrder = sortingOrder;
        group = root.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        for (int i = 0; i < count; i++)
        {
            cards.Add(BuildCard(i));
        }

        Refresh();
    }

    RectTransform NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    void Stretch(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    Card BuildCard(int index)
    {
        Card card = new Card();

        card.rect = NewRect("Card " + index, root);
        card.rect.anchorMin = new Vector2(0.5f, 1f);
        card.rect.anchorMax = new Vector2(0.5f, 1f);
        card.rect.pivot = new Vector2(0f, 1f);
        card.rect.sizeDelta = new Vector2(cardSize, cardSize);

        card.frame = card.rect.gameObject.AddComponent<Image>();
        card.frame.raycastTarget = false;

        RectTransform backRect = NewRect("Back", card.rect);
        Stretch(backRect, frameThickness);
        card.back = backRect.gameObject.AddComponent<Image>();
        card.back.color = cardBackground;
        card.back.raycastTarget = false;

        RectTransform portraitRect = NewRect("Portrait", backRect);
        Stretch(portraitRect, 0f);
        card.portrait = portraitRect.gameObject.AddComponent<Image>();
        card.portrait.preserveAspect = true;
        card.portrait.raycastTarget = false;

        RectTransform labelRect = NewRect("Label", backRect);
        Stretch(labelRect, 0f);
        card.label = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
        card.label.alignment = TextAlignmentOptions.Center;
        card.label.fontStyle = FontStyles.Bold;
        card.label.color = Color.white;
        card.label.enableWordWrapping = false;
        card.label.raycastTarget = false;

        card.rect.gameObject.SetActive(false);
        return card;
    }

    void Update()
    {
        if (root == null || battleManager == null)
        {
            return;
        }

        BattleState state = battleManager.State;
        bool show = state == BattleState.PlayerTurn || state == BattleState.Resolving;
        group.alpha = show ? 1f : 0f;

        timer += Time.unscaledDeltaTime;
        if (timer >= refreshInterval)
        {
            timer = 0f;
            Refresh();
        }

        float t = 1f - Mathf.Exp(-moveSpeed * Time.unscaledDeltaTime);

        foreach (Card card in cards)
        {
            if (!card.rect.gameObject.activeSelf)
            {
                continue;
            }

            if (!card.placed)
            {
                card.rect.anchoredPosition = card.targetPos;
                card.rect.sizeDelta = new Vector2(card.targetSize, card.targetSize);
                card.placed = true;
                continue;
            }

            card.rect.anchoredPosition = Vector2.Lerp(card.rect.anchoredPosition, card.targetPos, t);
            float size = Mathf.Lerp(card.rect.sizeDelta.x, card.targetSize, t);
            card.rect.sizeDelta = new Vector2(size, size);
        }
    }

    bool SameAsShown(List<Combatant> list)
    {
        if (list.Count != shown.Count)
        {
            return false;
        }

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != shown[i])
            {
                return false;
            }
        }

        return true;
    }

    string Initials(Combatant c)
    {
        string name = c.data != null && !string.IsNullOrEmpty(c.data.displayName) ? c.data.displayName : c.name;
        int length = Mathf.Min(initialLength, name.Length);
        return name.Substring(0, length).ToUpper();
    }

    void Refresh()
    {
        List<Combatant> list = battleManager.GetUpcomingTurns(count);

        if (SameAsShown(list))
        {
            return;
        }

        shown = list;

        float total = 0f;
        for (int i = 0; i < list.Count; i++)
        {
            total += i == 0 ? activeSize : cardSize;
            if (i > 0)
            {
                total += i == 1 ? activeGap : spacing;
            }
        }

        float x = -total * 0.5f;

        for (int i = 0; i < cards.Count; i++)
        {
            Card card = cards[i];

            if (i >= list.Count)
            {
                card.rect.gameObject.SetActive(false);
                card.placed = false;
                continue;
            }

            Combatant c = list[i];
            float size = i == 0 ? activeSize : cardSize;

            if (!card.rect.gameObject.activeSelf)
            {
                card.rect.gameObject.SetActive(true);
                card.placed = false;
            }

            card.targetSize = size;
            card.targetPos = new Vector2(x, -(activeSize - size) * 0.5f);
            x += size + (i == 0 ? activeGap : spacing);

            card.frame.color = c.isPlayerSide ? partyFrame : enemyFrame;

            Sprite sprite = c.data != null ? c.data.portrait : null;
            card.portrait.sprite = sprite;
            card.portrait.enabled = sprite != null;
            card.label.text = Initials(c);
            card.label.enabled = sprite == null;
            card.label.fontSize = size * 0.4f;
        }
    }
}

