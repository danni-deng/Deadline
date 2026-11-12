using UnityEngine;

public class CharacterGunSprite : MonoBehaviour
{
    public Combatant combatant;
    public GunMode gun;
    public SpriteRenderer target;
    public Sprite redSprite;
    public Sprite blueSprite;
    public Sprite yellowSprite;
    public bool anchorFeet = false;
    public bool reapplyEveryFrame = false;

    Sprite current;
    bool subscribed;

    void Awake()
    {
        if (combatant == null)
        {
            combatant = GetComponentInParent<Combatant>();
        }
        if (target == null)
        {
            target = GetComponentInChildren<SpriteRenderer>();
        }
        if (gun == null)
        {
            gun = FindObjectOfType<GunMode>();
        }
    }

    void OnEnable()
    {
        if (gun != null && !subscribed)
        {
            gun.SlotChanged += Refresh;
            subscribed = true;
        }
    }

    void OnDisable()
    {
        if (gun != null && subscribed)
        {
            gun.SlotChanged -= Refresh;
            subscribed = false;
        }
    }

    void Start()
    {
        Refresh();
    }

    void Refresh()
    {
        if (combatant == null || combatant.data == null || gun == null || target == null)
        {
            return;
        }

        BattleColor color = combatant.data.bulletColorA;

        if (gun.battleManager != null && gun.battleManager.ActiveCombatant == combatant)
        {
            color = gun.CurrentColor;
        }

        Sprite sprite = SpriteFor(color);

        if (sprite == null || sprite == target.sprite)
        {
            return;
        }

        Vector3 before = Feet();

        current = sprite;
        target.sprite = sprite;

        if (anchorFeet)
        {
            Vector3 after = Feet();
            target.transform.position += before - after;
        }
    }

    Vector3 Feet()
    {
        Bounds b = target.bounds;
        return new Vector3(b.center.x, b.min.y, 0f);
    }

    Sprite SpriteFor(BattleColor color)
    {
        switch (color.ToString())
        {
            case "Red":
                return redSprite;
            case "Blue":
                return blueSprite;
            case "Yellow":
                return yellowSprite;
        }
        return null;
    }

    void LateUpdate()
    {
        if (reapplyEveryFrame && target != null && current != null && target.sprite != current)
        {
            target.sprite = current;
        }
    }
}
