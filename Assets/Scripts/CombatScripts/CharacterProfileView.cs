using TMPro;
using UnityEngine;

public class CharacterProfileView : MonoBehaviour
{
    public CombatantData character;
    public GameObject root;
    public TMP_Text hpText;
    public TMP_Text[] bulletTexts = new TMP_Text[2];
    public string format = "{0}/{1}";
    public Color normalColor = Color.white;
    public Color deadColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    public CanvasGroup group;
    public float deadAlpha = 0.5f;

    Combatant combatant;
    float nextSearch;

    void Awake()
    {
        if (root == null)
        {
            root = gameObject;
        }
    }

    void Update()
    {
        if (combatant == null && Time.unscaledTime >= nextSearch)
        {
            nextSearch = Time.unscaledTime + 0.25f;
            Combatant[] all = FindObjectsOfType<Combatant>();

            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].data == character)
                {
                    combatant = all[i];
                    break;
                }
            }
        }

        bool present = combatant != null && combatant.data != null;

        if (root != gameObject)
        {
            if (root.activeSelf != present)
            {
                root.SetActive(present);
            }
        }
        else if (group != null)
        {
            group.alpha = present ? (combatant.IsAlive ? 1f : deadAlpha) : 0f;
            group.blocksRaycasts = present;
        }

        if (!present)
        {
            return;
        }

        if (hpText != null)
        {
            hpText.text = string.Format(format, Mathf.Max(0, combatant.currentHP), combatant.data.maxHP);
            hpText.color = combatant.IsAlive ? normalColor : deadColor;
        }

        if (group != null && root != gameObject)
        {
            group.alpha = combatant.IsAlive ? 1f : deadAlpha;
        }

        if (bulletTexts.Length > 0 && bulletTexts[0] != null)
        {
            bulletTexts[0].text = combatant.BulletsOf(combatant.data.bulletColorA).ToString();
        }

        if (bulletTexts.Length > 1 && bulletTexts[1] != null)
        {
            bulletTexts[1].text = combatant.BulletsOf(combatant.data.bulletColorB).ToString();
        }
    }
}
