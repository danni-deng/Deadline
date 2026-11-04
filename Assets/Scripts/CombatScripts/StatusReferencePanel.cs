using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatusReferencePanel : MonoBehaviour
{
    public StatusIcons icons;
    public Vector2 anchor = new Vector2(1f, 1f);
    public Vector2 offset = new Vector2(-30f, -120f);
    public bool startCollapsed = false;
    public Color panelColor = new Color(0.04f, 0.04f, 0.05f, 0.94f);
    public Color outlineColor = new Color(1f, 0.55f, 0.7f);

    RectTransform panel;
    GameObject rows;
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

        RectTransform canvasRect = canvas.rootCanvas.GetComponent<RectTransform>();
        Vector2 topLeft = new Vector2(0f, 1f);
        Vector2 topRight = new Vector2(1f, 1f);

        Image background = UiBuild.Panel(canvasRect, "Status Reference", anchor, anchor, offset, new Vector2(400f, 270f), panelColor);
        UiBuild.AddOutline(background, outlineColor, 3f);
        panel = background.rectTransform;

        UiBuild.Text(panel, "Title", topLeft, topLeft, new Vector2(22f, -12f), new Vector2(290f, 40f), 30f, TextAlignmentOptions.Left, Color.white, FontStyles.Bold).text = "Status Effects";

        Image help = UiBuild.Panel(panel, "Help", topRight, topRight, new Vector2(-14f, -10f), new Vector2(44f, 44f), new Color(0.1f, 0.1f, 0.12f));
        help.raycastTarget = true;
        UiBuild.AddOutline(help, outlineColor, 2f);
        Button button = help.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(Toggle);

        Vector2 center = new Vector2(0.5f, 0.5f);
        UiBuild.Text(help.transform, "Mark", center, center, Vector2.zero, new Vector2(44f, 44f), 30f, TextAlignmentOptions.Center, Color.white, FontStyles.Bold).text = "?";

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

    void Toggle()
    {
        collapsed = !collapsed;
        Apply();
    }

    void Apply()
    {
        rows.SetActive(!collapsed);
        panel.sizeDelta = new Vector2(400f, collapsed ? 66f : 270f);
    }
}
