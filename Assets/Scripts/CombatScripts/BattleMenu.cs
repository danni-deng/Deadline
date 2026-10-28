using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleMenu : MonoBehaviour
{
    public BattleManager battleManager;
    public CanvasGroup menuGroup;
    public Button attackButton;
    public Button ability1Button;
    public Button ability2Button;
    public TMP_Text ability1Name;
    public TMP_Text ability2Name;
    public TMP_Text ability1Cooldown;
    public TMP_Text ability2Cooldown;
    public TMP_Text ability1Tier;
    public TMP_Text ability2Tier;
    public TMP_Text infoText;
    public float popTime = 0.2f;
    public float popStagger = 0.06f;

    bool subscribed;

    void OnEnable()
    {
        if (!FieldsAreSet())
        {
            return;
        }

        battleManager.StateChanged += OnStateChanged;
        battleManager.TargetChanged += OnTargetChanged;
        battleManager.AllyChanged += OnAllyChanged;
        Combatant.AnyColorStateChanged += OnColorStateChanged;

        attackButton.onClick.AddListener(battleManager.SubmitAttack);
        ability1Button.onClick.AddListener(battleManager.SubmitAbilityOne);
        ability2Button.onClick.AddListener(battleManager.SubmitAbilityTwo);

        subscribed = true;
    }

    void OnDisable()
    {
        if (!subscribed)
        {
            return;
        }

        battleManager.StateChanged -= OnStateChanged;
        battleManager.TargetChanged -= OnTargetChanged;
        battleManager.AllyChanged -= OnAllyChanged;
        Combatant.AnyColorStateChanged -= OnColorStateChanged;

        attackButton.onClick.RemoveListener(battleManager.SubmitAttack);
        ability1Button.onClick.RemoveListener(battleManager.SubmitAbilityOne);
        ability2Button.onClick.RemoveListener(battleManager.SubmitAbilityTwo);

        subscribed = false;
    }

    void Start()
    {
        if (subscribed && battleManager.State != BattleState.PlayerTurn)
        {
            SetVisible(false);
        }
    }

    bool FieldsAreSet()
    {
        bool ok = true;

        if (battleManager == null)
        {
            Debug.LogError("BattleMenu: battleManager is empty");
            ok = false;
        }
        if (menuGroup == null)
        {
            Debug.LogError("BattleMenu: menuGroup is empty");
            ok = false;
        }
        if (attackButton == null)
        {
            Debug.LogError("BattleMenu: attackButton is empty");
            ok = false;
        }
        if (ability1Button == null)
        {
            Debug.LogError("BattleMenu: ability1Button is empty");
            ok = false;
        }
        if (ability2Button == null)
        {
            Debug.LogError("BattleMenu: ability2Button is empty");
            ok = false;
        }

        return ok;
    }

    void SetVisible(bool visible)
    {
        menuGroup.alpha = visible ? 1f : 0f;
        menuGroup.interactable = visible;
        menuGroup.blocksRaycasts = visible;
    }

    void OnStateChanged(BattleState state)
    {
        StopAllCoroutines();

        if (state == BattleState.PlayerTurn)
        {
            Refresh();
            SetVisible(true);
            StartCoroutine(Pop());
        }
        else
        {
            SetVisible(false);
        }
    }

    void OnTargetChanged(Combatant target)
    {
        if (battleManager.State == BattleState.PlayerTurn)
        {
            Refresh();
        }
    }

    void OnAllyChanged(Combatant ally)
    {
        if (battleManager.State == BattleState.PlayerTurn)
        {
            Refresh();
        }
    }

    void OnColorStateChanged(Combatant who)
    {
        if (battleManager.State == BattleState.PlayerTurn)
        {
            Refresh();
        }
    }

    void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }

    void Refresh()
    {
        Combatant user = battleManager.ActiveCombatant;
        if (user == null)
        {
            return;
        }

        RefreshAbility(user, 0, ability1Button, ability1Name, ability1Cooldown, ability1Tier);
        RefreshAbility(user, 1, ability2Button, ability2Name, ability2Cooldown, ability2Tier);

        Combatant target = battleManager.SelectedTarget;
        string targetName = target != null ? target.name : "-";
        Combatant ally = battleManager.SelectedAlly;
        string allyName = ally != null ? ally.name : "-";

        SetText(infoText, user.data.displayName + "   HP " + user.currentHP + "/" + user.data.maxHP + "   BULLETS " + user.bulletsLeft + "\nTARGET: " + targetName + "   ALLY: " + allyName);
    }

    void RefreshAbility(Combatant user, int index, Button button, TMP_Text nameText, TMP_Text cooldownText, TMP_Text tierText)
    {
        bool exists = index < user.data.abilities.Length && user.data.abilities[index] != null;
        button.gameObject.SetActive(exists);

        if (!exists)
        {
            return;
        }

        AbilityData ability = user.data.abilities[index];
        int cooldown = user.cooldowns[index];
        int tier = Mathf.Min(user.StackCount(ability), 3);

        button.interactable = user.AbilityReady(index);
        SetText(nameText, ability.abilityName);
        SetText(cooldownText, cooldown > 0 ? cooldown.ToString() : "");
        SetText(tierText, "LV " + tier);
    }

    IEnumerator Pop()
    {
        Transform[] items = new Transform[] { attackButton.transform, ability1Button.transform, ability2Button.transform };

        foreach (Transform t in items)
        {
            t.localScale = Vector3.zero;
        }

        float elapsed = 0f;
        float total = popTime + popStagger * (items.Length - 1);

        while (elapsed < total)
        {
            elapsed += Time.unscaledDeltaTime;

            for (int i = 0; i < items.Length; i++)
            {
                float p = Mathf.Clamp01((elapsed - popStagger * i) / popTime);
                float s = 1f + 2.70158f * Mathf.Pow(p - 1f, 3f) + 1.70158f * Mathf.Pow(p - 1f, 2f);
                items[i].localScale = Vector3.one * s;
            }

            yield return null;
        }

        foreach (Transform t in items)
        {
            t.localScale = Vector3.one;
        }
    }
}
