
using UnityEngine;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject tutorialPanel;
    public TMP_Text titleText;
    public TMP_Text bodyText;
    public TMP_Text continueText;

    private int currentPage = 0;

    private string[] titles =
    {
        "1. TURN-BASED COMBAT",
        "2. SHOOTING",
        "3. COLORS",
        "4. ATTACKS & ABILITIES",
        "5. WIN CONDITIONS"
    };

    private string[] descriptions =
    {
        "Combat is turn-based.\n\n" +
        "Characters take turns based on their Speed.\n\n" +
        "On your turn, you can shoot, attack, or use an ability.",

        "Shoot enemies to build up their color meters.\n\n" +
        "Attack — Shoot\n" +
        "LEFT SHIFT — Switch bullet color\n\n" +
        "You have 5 shots per turn.\n" +
        "Each shot deals 2 damage and does not end your turn.",

        "Matching shots apply debuffs once an enemy's color meter is filled.\n\n" +
        "RED — Vulnerable: Takes 25% more damage.\n\n" +
        "BLUE — Weakened: Deals 30% less damage.\n\n" +
        "YELLOW — Interrupted: Turn meter is halved.\n\n" ,

        "Choose an action to end your turn.\n\n" +
        "1 — Basic Attack\n" +
        "2 — Ability 1\n" +
        "3 — Ability 2\n\n" +
        "Abilities have cooldowns.\n" +
        "Some abilities require you to select an ally before confirming.\n\n" +
        "Shooting does not end your turn. Attacking or using an ability does.",

        "Defeat both waves of enemies!\n\n" +
        "VICTORY: Defeat all enemies.\n" +
        "DEFEAT: All heroes are eliminated.\n\n" +
        "HP, bullets, and cooldowns carry over between waves.\n\n" +
        "Manage your resources carefully!"
    };

    void Start()
{
    tutorialPanel.SetActive(false);
    StartTutorial();
}

    void Update()
    {
        if (tutorialPanel.activeSelf &&
            Input.GetKeyDown(KeyCode.Space))
        {
            NextPage();
        }
    }

    public void StartTutorial()
    {
        currentPage = 0;
        tutorialPanel.SetActive(true);
        ShowPage();
    }

    void ShowPage()
    {
        titleText.text = titles[currentPage];
        bodyText.text = descriptions[currentPage];

        if (currentPage == titles.Length - 1)
        {
            continueText.text = "PRESS SPACE TO FINISH";
        }
        else
        {
            continueText.text = "PRESS SPACE TO CONTINUE";
        }
    }

    void NextPage()
    {
        currentPage++;

        if (currentPage >= titles.Length)
        {
            tutorialPanel.SetActive(false);
            return;
        }

        ShowPage();
    }
}
