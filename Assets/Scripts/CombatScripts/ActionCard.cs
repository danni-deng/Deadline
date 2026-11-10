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
    public GunMode gun;
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
    public Button swapButton;
    public string attackBodyFormat = "Hit the enemy. Adds {0} {1} buildup.";
    public string swapBody = "Switch to your other bullet color.";
    public string allyPromptFormat = "Choose a character to {0}. Click one or use the arrows, then press Enter.";
    public bool showOnlyOnHover = true;
    public float fadeSpeed = 12f;
    public float popAmount = 0.06f;
    public float popDecay = 10f;

    int hovered = -1;
    bool dirty = true;
    bool subscribed;
    bool gunSubscribed;
    float pop;
    float targetAlpha;
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

        if (gun != null)
        {
            gun.SlotChanged += OnSlot;
            gunSubscribed = true;
        }
    }

    void OnDisable()
    {
        if (subscribed)
        {
            battleManager.StateChanged -= OnState;
            battleManager.TurnStarted -= OnTurn;
            battleManager.AllyTargetingChanged -= OnTargeting;
            Combatant.AnyStatusChanged -= OnCombatant;
            subscribed = false;
        }

        if (gunSubscribed && gun != null)
        {
            gun.SlotChanged -= OnSlot;
            gunSubscribed = false;
        }
    }

    void Start()
    {
        Hook(attackButton, 0);
        Hook(ability1Button, 1);
        Hook(ability2Button, 2);
        Hook(swapButton, 3);

        if (group != null)
        {
            group.blocksRaycasts = false;
            group.interactable = false;
            group.alpha = 0f;
        }
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

    void OnSlot()
    {
        dirty = true;
    }

    void Update()
    {
        if (dirty)
        {
            Refresh();
        }

        if (group != null)
        {
            group.alpha = Mathf.MoveTowards(group.alpha, targetAlpha, fadeSpeed * Time.unscaledDeltaTime);
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

    string KeyLabel(KeyCode key)
    {
        if (key == KeyCode.LeftShift || key == KeyCode.RightShift)
        {
            return "SHIFT";
        }
        return key.ToString().ToUpper();
    }

    void Refresh()
    {
        dirty = false;

        Combatant user = battleManager.ActiveCombatant;
        bool show = battleManager.State == BattleState.PlayerTurn
            && user != null
            && user.isPlayerSide
            && user.data != null;

        if (!show)
        {
            hovered = -1;
            targetAlpha = 0f;
            lastContent = "";
            return;
        }

        bool wanted = !showOnlyOnHover || hovered >= 0 || battleManager.IsTargetingAlly;
        targetAlpha = wanted ? 1f : 0f;

        if (!wanted)
        {
            lastContent = "";
            return;
        }

        string title;
        string body;
        string cooldown = "";
        string target;
        string key;

        if (battleManager.IsTargetingAlly)
        {
            title = "CHOOSE A CHARACTER";
            string verb = "buff";
            int pending = battleManager.PendingAbility;
            if (pending >= 0 && pending < user.data.abilities.Length && user.data.abilities[pending] != null)
            {
                AbilityData pendingAbility = user.data.abilities[pending];
                title = pendingAbility.abilityName.ToUpper();
                if (pendingAbility.healPercent > 0f)
                {
                    verb = "heal";
                }
            }

            body = string.Format(allyPromptFormat, verb);
            target = "";
            key = "ENTER";
        }
        else if (hovered == 3)
        {
            BattleColor current = gun != null ? gun.CurrentColor : BattleColor.Red;
            title = "SWAP COLOR";
            body = swapBody;
            cooldown = "NOW: " + current.ToString().ToUpper();
            target = Combatant.DebuffName(current).ToUpper();
            key = gun != null ? KeyLabel(gun.swapKey) : "SHIFT";
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
                BattleColor current = gun != null ? gun.CurrentColor : BattleColor.Red;
                int amount = gun != null ? gun.PendingBuildup(user) : 0;

                title = "ATTACK";
                body = string.Format(attackBodyFormat, amount, current.ToString().ToUpper());
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