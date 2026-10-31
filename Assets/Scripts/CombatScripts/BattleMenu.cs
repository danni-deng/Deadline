using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleMenu : MonoBehaviour
{
    public BattleManager battleManager;
    public CanvasGroup menuGroup;
    public Button attackButton;
    public Button colorStrikeButton;
    public Button ability1Button;
    public Button ability2Button;
    public TMP_Text ability1Name;
    public TMP_Text ability2Name;
    public TMP_Text ability1Cooldown;
    public TMP_Text ability2Cooldown;
    public TMP_Text ability1Tier;
    public TMP_Text ability2Tier;
    public TMP_Text colorStrikeChipsText;
    public TMP_Text infoText;
    public TMP_Text promptText;
    public float popTime = 0.2f;
    public float popStagger = 0.06f;

    bool subscribed;
    List<Transform> popItems = new List<Transform>();
    List<Vector3> baseScales = new List<Vector3>();

    void OnEnable()
    {
        if (!FieldsAreSet())
        {
            return;
        }

        if (popItems.Count == 0)
        {
            AddPopItem(attackButton);
            AddPopItem(colorStrikeButton);
            AddPopItem(ability1Button);
            AddPopItem(ability2Button);
        }

        battleManager.StateChanged += OnStateChanged;
        battleManager.TargetChanged += OnTargetChanged;
        battleManager.AllyChanged += OnAllyChanged;
        battleManager.AllyTargetingChanged += OnTargetingChanged;
        battleManager.StrikeChipChanged += OnStrikeChipChanged;
        Combatant.AnyColorStateChanged += OnCombatantEvent;
        Combatant.AnyChipsChanged += OnCombatantEvent;
        Combatant.AnyBulletsChanged += OnCombatantEvent;

        attackButton.onClick.AddListener(battleManager.SubmitAttack);
        ability1Button.onClick.AddListener(battleManager.SubmitAbilityOne);
        ability2Button.onClick.AddListener(battleManager.SubmitAbilityTwo);
        if (colorStrikeButton != null)
        {
            colorStrikeButton.onClick.AddListener(battleManager.SubmitColorStrike);
        }

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
        battleManager.AllyTargetingChanged -= OnTargetingChanged;
        battleManager.StrikeChipChanged -= OnStrikeChipChanged;
        Combatant.AnyColorStateChanged -= OnCombatantEvent;
        Combatant.AnyChipsChanged -= OnCombatantEvent;
        Combatant.AnyBulletsChanged -= OnCombatantEvent;

        attackButton.onClick.RemoveListener(battleManager.SubmitAttack);
        ability1Button.onClick.RemoveListener(battleManager.SubmitAbilityOne);
        ability2Button.onClick.RemoveListener(battleManager.SubmitAbilityTwo);
        if (colorStrikeButton != null)
        {
            colorStrikeButton.onClick.RemoveListener(battleManager.SubmitColorStrike);
        }

        subscribed = false;
    }

    void Start()
    {
        if (subscribed && battleManager.State != BattleState.PlayerTurn)
        {
            SetVisible(false);
        }
    }

    void AddPopItem(Button button)
    {
        if (button == null)
        {
            return;
        }

        popItems.Add(button.transform);
        baseScales.Add(button.transform.localScale);
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

    void RestoreScales()
    {
        for (int i = 0; i < popItems.Count; i++)
        {
            popItems[i].localScale = baseScales[i];
        }
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
            RestoreScales();
        }
    }

    void RefreshIfPlayerTurn()
    {
        if (battleManager.State == BattleState.PlayerTurn)
        {
            Refresh();
        }
    }

    void OnTargetChanged(Combatant target)
    {
        RefreshIfPlayerTurn();
    }

    void OnAllyChanged(Combatant ally)
    {
        RefreshIfPlayerTurn();
    }

    void OnTargetingChanged(bool targeting)
    {
        RefreshIfPlayerTurn();
    }

    void OnStrikeChipChanged(BattleColor color)
    {
        RefreshIfPlayerTurn();
    }

    void OnCombatantEvent(Combatant who)
    {
        RefreshIfPlayerTurn();
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

        if (colorStrikeButton != null)
        {
            colorStrikeButton.interactable = user.TotalChips > 0;

            BattleColor chipColor = battleManager.StrikeChipColor;
            string chipLabel = "NO CHIPS";
            if (user.TotalChips > 0)
            {
                chipLabel = chipColor.ToString().ToUpper() + " x" + user.chips[(int)chipColor];
            }
            SetText(colorStrikeChipsText, chipLabel);
        }

        Combatant target = battleManager.SelectedTarget;
        string targetName = target != null ? target.name : "-";
        Combatant ally = battleManager.SelectedAlly;
        string allyName = ally != null ? ally.name : "-";

        SetText(infoText, user.data.displayName + "   HP " + user.currentHP + "/" + user.data.maxHP + "   BULLETS " + user.bulletsLeft + "   CHIPS R" + user.chips[0] + " Y" + user.chips[1] + " B" + user.chips[2] + "\nTARGET: " + targetName);

        if (battleManager.IsTargetingAlly)
        {
            string abilityName = "ABILITY";
            int pending = battleManager.PendingAbility;
            if (pending >= 0 && pending < user.data.abilities.Length && user.data.abilities[pending] != null)
            {
                abilityName = user.data.abilities[pending].abilityName.ToUpper();
            }

            SetText(promptText, abilityName + ": CHOOSE AN ALLY - " + allyName + "\nCLICK A PROFILE OR USE ARROWS   ENTER TO CONFIRM   ESC TO CANCEL");
        }
        else
        {
            SetText(promptText, "");
        }
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
        int tier = Mathf.Min(user.ChipCount(ability), 3);

        button.interactable = user.AbilityReady(index);
        SetText(nameText, ability.abilityName);
        SetText(cooldownText, cooldown > 0 ? cooldown.ToString() : "");
        SetText(tierText, "LV " + tier);
    }

    IEnumerator Pop()
    {
        for (int i = 0; i < popItems.Count; i++)
        {
            popItems[i].localScale = Vector3.zero;
        }

        float elapsed = 0f;
        float total = popTime + popStagger * (popItems.Count - 1);

        while (elapsed < total)
        {
            elapsed += Time.unscaledDeltaTime;

            for (int i = 0; i < popItems.Count; i++)
            {
                float p = Mathf.Clamp01((elapsed - popStagger * i) / popTime);
                float s = 1f + 2.70158f * Mathf.Pow(p - 1f, 3f) + 1.70158f * Mathf.Pow(p - 1f, 2f);
                popItems[i].localScale = baseScales[i] * s;
            }

            yield return null;
        }

        RestoreScales();
    }
}