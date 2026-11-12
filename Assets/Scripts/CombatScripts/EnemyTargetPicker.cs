using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class EnemyHoverRelay : MonoBehaviour
{
    public System.Action<bool> onHover;
    public System.Action onClick;

    void OnMouseEnter()
    {
        if (onHover != null)
        {
            onHover(true);
        }
    }

    void OnMouseExit()
    {
        if (onHover != null)
        {
            onHover(false);
        }
    }

    void OnMouseDown()
    {
        if (onClick != null)
        {
            onClick();
        }
    }
}

public class EnemyTargetPicker : MonoBehaviour
{
    class Entry
    {
        public Combatant combatant;
        public Transform visual;
        public Vector3 baseScale;
        public bool hover;
    }

    public BattleManager battleManager;
    public float hoverScale = 1.06f;
    public float selectedScale = 1.03f;
    public float scaleSpeed = 12f;
    public bool selectOnHover = false;
    public bool clickAgainToAttack = false;
    public bool respectUi = true;
    public bool autoAddColliders = true;

    readonly Dictionary<Combatant, Entry> entries = new Dictionary<Combatant, Entry>();
    int knownCount = -1;

    void Start()
    {
        Rescan();
    }

    void Rescan()
    {
        knownCount = battleManager.enemies.Count;

        foreach (Combatant c in battleManager.enemies)
        {
            if (c == null || entries.ContainsKey(c))
            {
                continue;
            }

            Register(c);
        }
    }

    void Register(Combatant c)
    {
        SpriteRenderer best = null;
        float bestSize = 0f;

        foreach (SpriteRenderer sr in c.GetComponentsInChildren<SpriteRenderer>())
        {
            float size = sr.bounds.size.x * sr.bounds.size.y;
            if (best == null || size > bestSize)
            {
                best = sr;
                bestSize = size;
            }
        }

        if (best == null)
        {
            return;
        }

        if (best.GetComponent<Collider2D>() == null)
        {
            if (!autoAddColliders)
            {
                return;
            }

            BoxCollider2D box = best.gameObject.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
        }

        Entry entry = new Entry();
        entry.combatant = c;
        entry.visual = best.transform;
        entry.baseScale = best.transform.localScale;
        entries[c] = entry;

        EnemyHoverRelay relay = best.GetComponent<EnemyHoverRelay>();
        if (relay == null)
        {
            relay = best.gameObject.AddComponent<EnemyHoverRelay>();
        }

        relay.onHover = delegate (bool inside)
        {
            entry.hover = inside;

            if (inside && selectOnHover && CanPick(c) && !PointerBlocked())
            {
                SelectEnemy(c);
            }
        };

        relay.onClick = delegate ()
        {
            if (!CanPick(c) || PointerBlocked())
            {
                return;
            }

            if (battleManager.SelectedTarget != c)
            {
                SelectEnemy(c);
            }
            else if (clickAgainToAttack)
            {
                battleManager.TrySubmitAttack();
            }
        };
    }

    bool PointerBlocked()
    {
        return respectUi && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    bool CanPick(Combatant c)
    {
        return c != null
            && c.IsAlive
            && battleManager.State == BattleState.PlayerTurn
            && !battleManager.IsTargetingAlly;
    }

    void SelectEnemy(Combatant c)
    {
        int guard = battleManager.enemies.Count + 1;

        while (battleManager.SelectedTarget != c && guard > 0)
        {
            battleManager.CycleTarget(1);
            guard--;
        }
    }

    void Update()
    {
        if (battleManager.enemies.Count != knownCount)
        {
            Rescan();
        }

        bool blocked = PointerBlocked();

        foreach (KeyValuePair<Combatant, Entry> pair in entries)
        {
            Entry e = pair.Value;

            if (e.visual == null)
            {
                continue;
            }

            float factor = 1f;

            if (CanPick(e.combatant))
            {
                if (e.hover && !blocked)
                {
                    factor = hoverScale;
                }
                else if (battleManager.SelectedTarget == e.combatant)
                {
                    factor = selectedScale;
                }
            }

            Vector3 goal = e.baseScale * factor;
            e.visual.localScale = Vector3.Lerp(e.visual.localScale, goal, scaleSpeed * Time.unscaledDeltaTime);
        }
    }
}
