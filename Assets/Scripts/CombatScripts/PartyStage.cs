using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PartyStage : MonoBehaviour
{
    class MemberVisual
    {
        public Combatant combatant;
        public Transform home;
        public SpriteRenderer[] renderers;
        public int[] baseOrders;
        public Color[] baseColors;
        public Vector3 baseScale;
        public Coroutine routine;
    }

    public BattleManager battleManager;
    public Transform frontSlot;
    public Transform[] backSlots;
    public float stepForward = 0.4f;
    public float stepSide = 0.5f;
    public float activeScale = 1.06f;
    public Color fadedTint = new Color(0.6f, 0.6f, 0.6f, 0.85f);
    public int orderStep = 1;
    public int activeOrderBoost = 10;
    public float moveTime = 0.3f;

    List<MemberVisual> members = new List<MemberVisual>();

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

    Transform HomeFor(int index)
    {
        if (index == 0 || backSlots == null || backSlots.Length == 0)
        {
            return frontSlot;
        }

        int slotIndex = Mathf.Min(index - 1, backSlots.Length - 1);
        return backSlots[slotIndex];
    }

    void Start()
    {
        if (battleManager == null || frontSlot == null)
        {
            Debug.LogError("PartyStage: assign the battle manager and the front slot");
            enabled = false;
            return;
        }

        for (int i = 0; i < battleManager.party.Count; i++)
        {
            Combatant c = battleManager.party[i];
            if (c == null)
            {
                continue;
            }

            MemberVisual m = new MemberVisual();
            m.combatant = c;
            m.home = HomeFor(members.Count);
            m.renderers = c.GetComponentsInChildren<SpriteRenderer>();
            m.baseOrders = new int[m.renderers.Length];
            m.baseColors = new Color[m.renderers.Length];
            m.baseScale = c.transform.localScale;

            for (int r = 0; r < m.renderers.Length; r++)
            {
                m.baseOrders[r] = m.renderers[r].sortingOrder;
                m.baseColors[r] = m.renderers[r].color;
            }

            members.Add(m);
        }

        if (members.Count > 0)
        {
            Combatant first = battleManager.ActiveCombatant;
            if (first == null || !first.isPlayerSide)
            {
                first = members[0].combatant;
            }
            Arrange(first, true);
        }
    }

    void OnTurnStarted(Combatant c)
    {
        if (c.isPlayerSide)
        {
            Arrange(c, false);
        }
        else
        {
            Arrange(null, false);
        }
    }

    void Arrange(Combatant active, bool snap)
    {
        int count = members.Count;

        for (int i = 0; i < count; i++)
        {
            MemberVisual m = members[i];
            bool isActive = active != null && m.combatant == active;

            Vector3 position = m.home.position;
            float scale = m.home.localScale.x;
            int order = (count - i) * orderStep;
            Color tint = Color.white;

            if (isActive)
            {
                position = new Vector3(position.x + stepSide, position.y, frontSlot.position.z - stepForward);
                scale *= activeScale;
                order += activeOrderBoost;
            }
            else if (active != null)
            {
                tint = fadedTint;
            }

            if (m.routine != null)
            {
                StopCoroutine(m.routine);
            }

            if (snap)
            {
                Place(m, position, scale, order, tint);
            }
            else
            {
                m.routine = StartCoroutine(MoveMember(m, position, scale, order, tint));
            }
        }
    }

    void Place(MemberVisual m, Vector3 position, float scale, int order, Color tint)
    {
        Transform t = m.combatant.transform;
        t.position = position;
        t.localScale = m.baseScale * scale;

        for (int i = 0; i < m.renderers.Length; i++)
        {
            m.renderers[i].sortingOrder = m.baseOrders[i] + order;
            m.renderers[i].color = m.baseColors[i] * tint;
        }
    }

    IEnumerator MoveMember(MemberVisual m, Vector3 position, float scale, int order, Color tint)
    {
        Transform t = m.combatant.transform;
        Vector3 startPosition = t.position;
        Vector3 startScale = t.localScale;
        Vector3 endScale = m.baseScale * scale;

        Color[] startColors = new Color[m.renderers.Length];
        Color[] endColors = new Color[m.renderers.Length];

        for (int i = 0; i < m.renderers.Length; i++)
        {
            startColors[i] = m.renderers[i].color;
            endColors[i] = m.baseColors[i] * tint;
            m.renderers[i].sortingOrder = m.baseOrders[i] + order;
        }

        float elapsed = 0f;
        while (elapsed < moveTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float p = Mathf.SmoothStep(0f, 1f, elapsed / moveTime);

            t.position = Vector3.Lerp(startPosition, position, p);
            t.localScale = Vector3.Lerp(startScale, endScale, p);

            for (int i = 0; i < m.renderers.Length; i++)
            {
                m.renderers[i].color = Color.Lerp(startColors[i], endColors[i], p);
            }

            yield return null;
        }

        Place(m, position, scale, order, tint);
    }
}
