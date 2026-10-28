using UnityEngine;

public class colorChanger : MonoBehaviour
{
    public BattleManager battleManager;
    public Sprite[] sprites = new Sprite[3];
    int colorSelected = 0;
    string[] colorName = new string[] { "red", "yellow", "blue" };

    void OnEnable()
    {
        if (battleManager != null)
        {
            battleManager.TurnStarted += OnTurnStarted;
        }
    }

    void OnDisable()
    {
        if (battleManager != null)
        {
            battleManager.TurnStarted -= OnTurnStarted;
        }
    }

    void OnTurnStarted(Combatant c)
    {
        if (c.isPlayerSide)
        {
            colorSelected = (int)c.data.bulletColorA;
        }
    }

    public string getColor()
    {
        return colorName[colorSelected];
    }

    public void SwitchBullet()
    {
        Combatant user = null;
        if (battleManager != null)
        {
            user = battleManager.ActiveCombatant;
        }

        if (user != null && user.isPlayerSide)
        {
            if (colorSelected == (int)user.data.bulletColorA)
            {
                colorSelected = (int)user.data.bulletColorB;
            }
            else
            {
                colorSelected = (int)user.data.bulletColorA;
            }
        }
        else
        {
            colorSelected = (colorSelected + 1) % 3;
        }

        AudioSource sound = GetComponent<AudioSource>();
        if (sound != null)
        {
            sound.Play();
        }
    }

    void Update()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && colorSelected < sprites.Length)
        {
            sr.sprite = sprites[colorSelected];
        }

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            SwitchBullet();
        }
    }
}
