using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class UiBuild
{
    public static RectTransform Box(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    public static Image Panel(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size, Color color)
    {
        RectTransform rect = Box(parent, name, anchor, pivot, position, size);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    public static TextMeshProUGUI Text(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment, Color color, FontStyles style)
    {
        RectTransform rect = Box(parent, name, anchor, pivot, position, size);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.fontStyle = style;
        text.raycastTarget = false;
        text.text = "";
        return text;
    }

    public static Outline AddOutline(Graphic graphic, Color color, float distance)
    {
        Outline outline = graphic.gameObject.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(distance, -distance);
        return outline;
    }

    public static Image Icon(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 position, float size, out TextMeshProUGUI letter)
    {
        Image image = Panel(parent, name, anchor, pivot, position, new Vector2(size, size), Color.gray);
        image.preserveAspect = true;
        AddOutline(image, Color.white, 2f);

        Vector2 center = new Vector2(0.5f, 0.5f);
        letter = Text(image.transform, "Letter", center, center, Vector2.zero, new Vector2(size, size), size * 0.6f, TextAlignmentOptions.Center, Color.white, FontStyles.Bold);
        return image;
    }

    public static void SetOutline(Image image, bool on)
    {
        Outline outline = image.GetComponent<Outline>();
        if (outline != null)
        {
            outline.enabled = on;
        }
    }

    public static void ApplyDebuffIcon(Image image, TMP_Text letter, StatusIcons icons, BattleColor color)
    {
        Sprite sprite = icons != null ? icons.ForColor(color) : null;

        if (sprite != null)
        {
            image.sprite = sprite;
            image.color = Color.white;
            letter.text = "";
            SetOutline(image, false);
        }
        else
        {
            image.sprite = null;
            image.color = BattleColorUtil.ToUnityColor(color);
            letter.text = Combatant.DebuffName(color).Substring(0, 1);
            letter.color = color == BattleColor.Yellow ? Color.black : Color.white;
            SetOutline(image, true);
        }
    }
}