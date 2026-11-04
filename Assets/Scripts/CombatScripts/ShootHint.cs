using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShootHint : MonoBehaviour
{
    public BattleManager battleManager;
    public GunMode gunMode;
    public RectTransform container;
    public Vector2 anchor = new Vector2(1f, 0f);
    public Vector2 position = new Vector2(-760f, 175f);
    public float pillWidth = 580f;
    public string keyLabel = "SPACE";
    public string actionLabel = "Shoot & Build Color";
    public string helpText = "Fill the enemy's color meter. Debuff activates when full.";
    public Color actionColor = new Color(1f, 0.86f, 0.5f);

    CanvasGroup group;
    TextMeshProUGUI actionText;
    string lastText = "";

    void Start()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null || battleManager == null)
        {
            Debug.LogError("ShootHint: put this on the Canvas and assign the battle manager");
            enabled = false;
            return;
        }

        Transform parent = container != null ? container : canvas.rootCanvas.transform;
        Vector2 center = new Vector2(0.5f, 0.5f);
        Vector2 topCenter = new Vector2(0.5f, 1f);
        Vector2 leftCenter = new Vector2(0f, 0.5f);

        RectTransform root = UiBuild.Box(parent, "Shoot Hint", anchor, center, position, new Vector2(pillWidth, 120f));
        group = root.gameObject.AddComponent<CanvasGroup>();

        Image pill = UiBuild.Panel(root, "Pill", topCenter, topCenter, Vector2.zero, new Vector2(pillWidth, 60f), new Color(0.05f, 0.05f, 0.06f, 0.95f));
        pill.raycastTarget = true;
        UiBuild.AddOutline(pill, Color.white, 2f);

        Button button = pill.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(OnClick);

        Image cap = UiBuild.Panel(pill.transform, "Key", leftCenter, leftCenter, new Vector2(12f, 0f), new Vector2(120f, 40f), new Color(0.05f, 0.05f, 0.06f, 1f));
        UiBuild.AddOutline(cap, Color.white, 2f);
        UiBuild.Text(cap.transform, "KeyText", center, center, Vector2.zero, new Vector2(120f, 40f), 26f, TextAlignmentOptions.Center, Color.white, FontStyles.Bold).text = keyLabel;

        actionText = UiBuild.Text(pill.transform, "Action", leftCenter, leftCenter, new Vector2(148f, 0f), new Vector2(pillWidth - 160f, 44f), 28f, TextAlignmentOptions.Left, actionColor, FontStyles.Bold);
        actionText.enableWordWrapping = false;

        Image helpBack = UiBuild.Panel(root, "HelpBack", topCenter, topCenter, new Vector2(0f, -64f), new Vector2(pillWidth, 40f), new Color(0.05f, 0.05f, 0.06f, 0.85f));
        TextMeshProUGUI help = UiBuild.Text(helpBack.transform, "Help", center, center, Vector2.zero, new Vector2(pillWidth - 20f, 40f), 21f, TextAlignmentOptions.Center, new Color(0.9f, 0.9f, 0.9f), FontStyles.Italic);
        help.enableWordWrapping = false;
        help.text = helpText;
    }

    void OnClick()
    {
        if (gunMode != null)
        {
            gunMode.FireButton();
        }
    }

    void Update()
    {
        bool visible = battleManager.State == BattleState.PlayerTurn && !battleManager.IsTargetingAlly;
        int shots = gunMode != null ? gunMode.ShotsLeft : 0;
        bool canShoot = visible && shots > 0;

        group.alpha = visible ? (shots > 0 ? 1f : 0.45f) : 0f;
        group.blocksRaycasts = canShoot;
        group.interactable = canShoot;

        string next = shots > 0 ? "— " + actionLabel : "— No shots left this turn";
        if (next != lastText)
        {
            lastText = next;
            actionText.text = next;
        }
    }
}