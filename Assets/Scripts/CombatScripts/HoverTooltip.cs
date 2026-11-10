using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string title = "";
    public string fixedText = "";
    public Func<string> getTitle;
    public Func<string> getText;
    public Func<Color> getAccent;
    public Color accent = Color.white;
    public float gap = 10f;
    public float width = 380f;
    public int sortingOrder = 300;

    static RectTransform panel;
    static TextMeshProUGUI titleLabel;
    static TextMeshProUGUI bodyLabel;
    static Outline panelOutline;
    static CanvasGroup panelGroup;
    static HoverTooltip owner;

    void Build()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return;
        }

        GameObject root = new GameObject("Tooltip", typeof(RectTransform));
        panel = root.GetComponent<RectTransform>();
        panel.SetParent(canvas.rootCanvas.transform, false);
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0f);

        Canvas own = root.AddComponent<Canvas>();
        own.overrideSorting = true;
        own.sortingOrder = sortingOrder;

        panelGroup = root.AddComponent<CanvasGroup>();
        panelGroup.blocksRaycasts = false;
        panelGroup.interactable = false;

        Image back = root.AddComponent<Image>();
        back.color = new Color(0.04f, 0.04f, 0.05f, 0.96f);
        back.raycastTarget = false;

        panelOutline = root.AddComponent<Outline>();
        panelOutline.effectDistance = new Vector2(2f, -2f);

        titleLabel = NewLabel("Title", panel, 26f, FontStyles.Bold, Color.white);
        bodyLabel = NewLabel("Body", panel, 22f, FontStyles.Normal, new Color(0.85f, 0.85f, 0.85f));
        bodyLabel.enableWordWrapping = true;

        panelGroup.alpha = 0f;
    }

    static TextMeshProUGUI NewLabel(string name, RectTransform parent, float size, FontStyles style, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);

        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.fontSize = size;
        label.fontStyle = style;
        label.color = color;
        label.alignment = TextAlignmentOptions.TopLeft;
        label.raycastTarget = false;
        return label;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (panel == null)
        {
            Build();
        }

        if (panel == null)
        {
            return;
        }

        owner = this;
        Show();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (owner == this)
        {
            Hide();
            owner = null;
        }
    }

    void OnDisable()
    {
        if (owner == this)
        {
            Hide();
            owner = null;
        }
    }

    void Update()
    {
        if (owner == this && panel != null)
        {
            Show();
        }
    }

    void Hide()
    {
        if (panelGroup != null)
        {
            panelGroup.alpha = 0f;
        }
    }

    void Show()
    {
        string head = getTitle != null ? getTitle() : title;
        string body = getText != null ? getText() : fixedText;
        Color color = getAccent != null ? getAccent() : accent;

        float padding = 16f;
        float inner = width - padding * 2f;

        titleLabel.text = head;
        titleLabel.color = color;
        bodyLabel.text = body;
        panelOutline.effectColor = color;

        float titleHeight = string.IsNullOrEmpty(head) ? 0f : 34f;
        float titleGap = titleHeight > 0f ? 6f : 0f;
        float bodyHeight = bodyLabel.GetPreferredValues(body, inner, 0f).y;
        float height = padding * 2f + titleHeight + titleGap + bodyHeight;

        panel.sizeDelta = new Vector2(width, height);

        RectTransform titleRect = titleLabel.rectTransform;
        titleRect.sizeDelta = new Vector2(inner, titleHeight);
        titleRect.anchoredPosition = new Vector2(padding, -padding);
        titleLabel.gameObject.SetActive(titleHeight > 0f);

        RectTransform bodyRect = bodyLabel.rectTransform;
        bodyRect.sizeDelta = new Vector2(inner, bodyHeight);
        bodyRect.anchoredPosition = new Vector2(padding, -(padding + titleHeight + titleGap));

        RectTransform target = transform as RectTransform;
        Vector3 top = transform.position;
        Vector3 bottom = transform.position;

        if (target != null)
        {
            top = target.TransformPoint(new Vector3(target.rect.center.x, target.rect.yMax, 0f));
            bottom = target.TransformPoint(new Vector3(target.rect.center.x, target.rect.yMin, 0f));
        }

        Vector3[] corners = new Vector3[4];

        panel.pivot = new Vector2(0.5f, 0f);
        panel.position = top + new Vector3(0f, gap, 0f);
        panel.GetWorldCorners(corners);

        if (corners[2].y > Screen.height)
        {
            panel.pivot = new Vector2(0.5f, 1f);
            panel.position = bottom - new Vector3(0f, gap, 0f);
            panel.GetWorldCorners(corners);
        }

        float shiftX = 0f;
        if (corners[0].x < 0f)
        {
            shiftX = -corners[0].x;
        }
        else if (corners[2].x > Screen.width)
        {
            shiftX = Screen.width - corners[2].x;
        }

        panel.position += new Vector3(shiftX, 0f, 0f);
        panelGroup.alpha = 1f;
    }
}
