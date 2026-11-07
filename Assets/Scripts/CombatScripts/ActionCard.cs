using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ActionCardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public System.Action<bool> onHover;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (onHover != null)
        {
            onHover(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (onHover != null)
        {
            onHover(false);
        }
    }
}

public class ActionCard : MonoBehaviour
{
    public BattleManager battleManager;
    public CanvasGroup group;
    public TMP_Text nameText;
    public TMP_Text bodyText;
    public TMP_Text cooldownText;
    public TMP_Text targetText;
    public TMP_Text keyText;
    public GameObject cooldownChip;
    public GameObject targetChip;
    public Button attackButton;
    public Button ability1Button;
    public Button ability2Button;
    public string attackBody = "Hit the selected enemy.";
    public string allyPrompt = "Choose who receives it. Click a character or use the arrows, then press Enter.";
    public float popAmount = 0.06f;
    public float popDecay = 10f;

    int hovered = -1;
    bool dirty = true;
    bool subscribed;
    float pop;
    string lastContent = "";

    void OnEnable()
    {
        if (battleManager == null)
        {
            return;
        }

        battleManager.StateChanged += OnState;
        battleManager.TurnStarted += OnTurn;
        battleManager.AllyTargetingChanged += OnTargeting;
        Combatant.AnyStatusChanged += OnCombatant;
        subscribed = true;
    }

    void OnDisable()
    {
        if (!subscribed)
        {
            return;
        }

        battleManager.StateChanged -= OnState;
        battleManager.TurnStarted -= OnTurn;
        battleManager.AllyTargetingChanged -= OnTargeting;
        Combatant.AnyStatusChanged -= OnCombatant;
        subscribed = false;
    }

    void Start()
    {
        Hook(attackButton, 0);
        Hook(ability1Button, 1);
        Hook(ability2Button, 2);
    }

    void Hook(Button button, int index)
    {
        if (button == null)
        {
            return;
        }

        ActionCardHover hover = button.gameObject.GetComponent<ActionCardHover>();
        if (hover == null)
        {
            hover = button.gameObject.AddComponent<ActionCardHover>();
        }

        hover.onHover = delegate (bool inside)
        {
            if (inside)
            {
                hovered = index;
            }
            else if (hovered == index)
            {
                hovered = -1;
            }
            dirty = true;
        };
    }

    void OnState(BattleState state)
    {
        dirty = true;
    }

    void OnTurn(Combatant c)
    {
        dirty = true;
    }

    void OnTargeting(bool targeting)
    {
        dirty = true;
    }

    void OnCombatant(Combatant c)
    {
        dirty = true;
    }

    void Update()
    {
        if (dirty)
        {
            Refresh();
        }

        pop = Mathf.MoveTowards(pop, 0f, popDecay * Time.unscaledDeltaTime);
        transform.localScale = Vector3.one * (1f + popAmount * pop);
    }

    void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }

    void SetChip(GameObject chip, TMP_Text text, string value)
    {
        SetText(text, value);

        if (chip != null)
        {
            chip.SetActive(!string.IsNullOrEmpty(value));
        }
    }

    string TargetLabel(string raw)
    {
        switch (raw)
        {
            case "SingleEnemy":
                return "ONE ENEMY";
            case "AllEnemies":
                return "ALL ENEMIES";
            case "SingleAlly":
                return "ONE ALLY";
            case "Self":
                return "SELF";
        }
        return raw.ToUpper();
    }

    void Refresh()
    {
        dirty = false;

        Combatant user = battleManager.ActiveCombatant;
        bool show = battleManager.State == BattleState.PlayerTurn
            && user != null
            && user.isPlayerSide
            && user.data != null;

        if (group != null)
        {
            group.alpha = show ? 1f : 0f;
        }

        if (!show)
        {
            return;
        }

        string title;
        string body;
        string cooldown = "";
        string target;
        string key;

        if (battleManager.IsTargetingAlly)
        {
            title = "CHOOSE AN ALLY";
            int pending = battleManager.PendingAbility;
            if (pending >= 0 && pending < user.data.abilities.Length && user.data.abilities[pending] != null)
            {
                title = user.data.abilities[pending].abilityName.ToUpper();
            }

            body = allyPrompt;
            target = "";
            key = "ENTER";
        }
        else
        {
            int index = hovered >= 0 ? hovered : 0;
            int abilityIndex = index - 1;

            bool validAbility = abilityIndex >= 0
                && abilityIndex < user.data.abilities.Length
                && user.data.abilities[abilityIndex] != null;

            if (index > 0 && validAbility)
            {
                AbilityData ability = user.data.abilities[abilityIndex];
                int left = user.cooldowns[abilityIndex];

                title = ability.abilityName.ToUpper();
                body = ability.description;
                cooldown = left > 0 ? "READY IN " + left : "COOLDOWN " + ability.cooldown;
                target = TargetLabel(ability.target.ToString());
                key = (index + 1).ToString();
            }
            else
            {
                title = "ATTACK";
                body = attackBody;
                cooldown = "NO COOLDOWN";
                target = "ONE ENEMY";
                key = "1";
            }
        }

        SetText(nameText, title);
        SetText(bodyText, body);
        SetChip(cooldownChip, cooldownText, cooldown);
        SetChip(targetChip, targetText, target);
        SetText(keyText, key);

        string content = title + body + key;
        if (content != lastContent)
        {
            lastContent = content;
            pop = 1f;
        }
    }
}
