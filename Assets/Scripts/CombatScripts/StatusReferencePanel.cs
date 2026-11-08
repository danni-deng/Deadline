using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StatusPanelHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public bool hovering;
    public System.Action clicked;

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (clicked != null)
        {
            clicked();
        }
    }
}

public class StatusReferencePanel : MonoBehaviour
{
    public StatusIcons icons;
    public Vector2 anchor = new Vector2(1f, 1f);
    public Vector2 offset = new Vector2(-30f, -190f);
    public KeyCode toggleKey = KeyCode.H;
    public bool startCollapsed = true;
    public bool closeOnOutsideClick = true;
    public bool closeOnEscape = true;
    public bool pixelSnap = true;
    public int sortingOrder = 50;
    public Color panelColor = new Color(0.04f, 0.04f, 0.05f, 0.94f);
    public Color outlineColor = new Color(1f, 0.55f, 0.7f);
    public Color attentionColor = new Color(1f, 0.85f, 0.3f);
    public bool showColorSquares = true;
    public float squareSize = 20f;
    public float squareGap = 8f;
    public float squareRightEdge = 74f;
    public float squareYOffset = 0f;
    public bool pulseUntilOpened = true;
    public float pulseAmount = 0.05f;
    public float pulseSpeed = 4f;
    public float hoverScale = 1.08f;
    public float scaleSpeed = 14f;

    static bool seen;

    RectTransform panel;
    GameObject rows;
    Image helpImage;
    Outline panelOutline;
    StatusPanelHover hoverHelper;
    Canvas rootCanvas;
    bool collapsed;

    void Start()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("StatusReferencePanel: put this on the Canvas");
            enabled = false;
            return;
        }

        rootCanvas = canvas.rootCanvas;
        RectTransform canvasRect = rootCanvas.GetComponent<RectTransform>();
        Vector2 topLeft = new Vector2(0f, 1f);
        Vector2 topRight = new Vector2(1f, 1f);
        Vector2 center = new Vector2(0.5f, 0.5f);

        Image background = UiBuild.Panel(canvasRect, "Status Reference", anchor, anchor, offset, new Vector2(400f, 270f), panelColor);
        UiBuild.AddOutline(background, outlineColor, 3f);
        background.raycastTarget = true;
        panel = background.rectTransform;
        panelOutline = background.GetComponent<Outline>();

        Canvas own = panel.gameObject.AddComponent<Canvas>();
        own.overrideSorting = true;
        own.sortingOrder = sortingOrder;
        own.pixelPerfect = pixelSnap;
        panel.gameObject.AddComponent<GraphicRaycaster>();

        hoverHelper = panel.gameObject.AddComponent<StatusPanelHover>();
        hoverHelper.clicked = OnPanelClicked;

        UiBuild.Text(panel, "Title", topLeft, topLeft, new Vector2(22f, -12f), new Vector2(290f, 40f), 30f, TextAlignmentOptions.Left, Color.white, FontStyles.Bold).text = "Status Effects";

        Image help = UiBuild.Panel(panel, "Help", topRight, topRight, new Vector2(-12f, -9f), new Vector2(48f, 48f), Color.white);
        help.raycastTarget = true;
        helpImage = help;
        UiBuild.AddOutline(help, outlineColor, 2f);
        Button button = help.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(Toggle);

        UiBuild.Text(help.transform, "Mark", center, center, Vector2.zero, new Vector2(48f, 48f), 34f, TextAlignmentOptions.Center, Color.black, FontStyles.Bold).text = "?";

        if (showColorSquares)
        {
            BattleColor[] squareColors = new BattleColor[] { BattleColor.Red, BattleColor.Blue, BattleColor.Yellow };
            float top = -(33f - squareSize * 0.5f) + squareYOffset;

            for (int i = 0; i < squareColors.Length; i++)
            {
                float x = -squareRightEdge - (squareColors.Length - 1 - i) * (squareSize + squareGap);
                Image square = UiBuild.Panel(panel, "Color " + i, topRight, topRight, new Vector2(x, top), new Vector2(squareSize, squareSize), BattleColorUtil.ToUnityColor(squareColors[i]));
                square.raycastTarget = false;
            }
        }

        UiBuild.Panel(panel, "Divider", topLeft, topLeft, new Vector2(22f, -62f), new Vector2(356f, 2f), outlineColor);

        rows = UiBuild.Box(panel, "Rows", topLeft, topLeft, Vector2.zero, new Vector2(400f, 210f)).gameObject;

        BattleColor[] order = new BattleColor[] { BattleColor.Red, BattleColor.Blue, BattleColor.Yellow };
        for (int i = 0; i < order.Length; i++)
        {
            float y = -(74f + i * 64f);

            TextMeshProUGUI letter;
            Image icon = UiBuild.Icon(rows.transform, "Icon " + i, topLeft, topLeft, new Vector2(22f, y), 44f, out letter);
            UiBuild.ApplyDebuffIcon(icon, letter, icons, order[i]);

            UiBuild.Text(rows.transform, "Name " + i, topLeft, topLeft, new Vector2(80f, y + 2f), new Vector2(300f, 30f), 26f, TextAlignmentOptions.Left, Color.white, FontStyles.Bold).text = Combatant.DebuffName(order[i]);
            UiBuild.Text(rows.transform, "Description " + i, topLeft, topLeft, new Vector2(80f, y - 28f), new Vector2(300f, 28f), 21f, TextAlignmentOptions.Left, new Color(0.85f, 0.85f, 0.85f), FontStyles.Normal).text = Combatant.DebuffDescription(order[i]);
        }

        collapsed = startCollapsed;
        Apply();
    }

    void Update()
    {
        if (panel == null)
        {
            return;
        }

        if (Input.GetKeyDown(toggleKey))
        {
            Toggle();
        }

        if (!collapsed)
        {
            if (closeOnEscape && Input.GetKeyDown(KeyCode.Escape))
            {
                Toggle();
            }
            else if (closeOnOutsideClick && Input.GetMouseButtonDown(0) && !PointerInsidePanel())
            {
                Toggle();
            }
        }

        bool hover = hoverHelper != null && hoverHelper.hovering && collapsed;
        bool attention = pulseUntilOpened && !seen && collapsed;
        float wave = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f;

        float target = 1f;
        if (hover)
        {
            target = hoverScale;
        }
        else if (attention)
        {
            target = 1f + pulseAmount * wave;
        }

        float t = 1f - Mathf.Exp(-scaleSpeed * Time.unscaledDeltaTime);
        panel.localScale = Vector3.Lerp(panel.localScale, Vector3.one * target, t);

        float mix = hover ? 1f : (attention ? wave : 0f);

        if (panelOutline != null)
        {
            panelOutline.effectColor = Color.Lerp(outlineColor, attentionColor, mix);
        }

        if (helpImage != null)
        {
            helpImage.color = Color.Lerp(Color.white, attentionColor, mix);
        }
    }

    bool PointerInsidePanel()
    {
        Camera cam = null;
        if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            cam = rootCanvas.worldCamera;
        }

        return RectTransformUtility.RectangleContainsScreenPoint(panel, Input.mousePosition, cam);
    }

    void OnPanelClicked()
    {
        Toggle();
    }

    void Toggle()
    {
        seen = true;
        collapsed = !collapsed;
        Apply();
    }

    void Apply()
    {
        rows.SetActive(!collapsed);
        panel.sizeDelta = new Vector2(400f, collapsed ? 66f : 270f);
    }
}