using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleMenu : MonoBehaviour
{
    public BattleManager battleManager;
    public GameObject menuRoot;
    public Button attackButton;
    public Button healButton;
    public TMP_Text infoText;
    public float popTime = 0.2f;

    void OnEnable()
    {
        battleManager.StateChanged += OnStateChanged;
        battleManager.TargetChanged += OnTargetChanged;
        attackButton.onClick.AddListener(battleManager.SubmitAttack);
        healButton.onClick.AddListener(battleManager.SubmitHeal);
    }

    void OnDisable()
    {
        battleManager.StateChanged -= OnStateChanged;
        battleManager.TargetChanged -= OnTargetChanged;
        attackButton.onClick.RemoveListener(battleManager.SubmitAttack);
        healButton.onClick.RemoveListener(battleManager.SubmitHeal);
    }

    void Start()
    {
        if (battleManager.State != BattleState.PlayerTurn)
        {
            menuRoot.SetActive(false);
        }
    }

    void OnStateChanged(BattleState state)
    {
        StopAllCoroutines();

        if (state == BattleState.PlayerTurn)
        {
            Refresh();
            menuRoot.SetActive(true);
            StartCoroutine(Pop());
        }
        else
        {
            menuRoot.SetActive(false);
        }
    }

    void OnTargetChanged(Combatant target)
    {
        Refresh();
    }

    void Refresh()
    {
        Combatant user = battleManager.ActiveCombatant;
        Combatant target = battleManager.SelectedTarget;
        if (user == null)
        {
            return;
        }

        healButton.interactable = user.CanHeal;
        string targetName = target != null ? target.name : "-";
        infoText.text = user.data.displayName + "   HP " + user.currentHP + "/" + user.data.maxHP + "   HEALS " + user.healsLeft + "\nTARGET: " + targetName;
    }

    IEnumerator Pop()
    {
        Transform t = menuRoot.transform;
        float elapsed = 0f;

        while (elapsed < popTime)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / popTime);
            float s = 1f + 2.70158f * Mathf.Pow(p - 1f, 3f) + 1.70158f * Mathf.Pow(p - 1f, 2f);
            t.localScale = Vector3.one * s;
            yield return null;
        }

        t.localScale = Vector3.one;
    }
}
