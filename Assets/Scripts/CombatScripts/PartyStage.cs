using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PartyStage : MonoBehaviour
{
    class MemberVisual
    {
        public Combatant combatant;
        public SpriteRenderer[] renderers;
        public int[] baseOrders;
        public Color[] baseColors;
        public Vector3 baseScale;
        public Coroutine routine;
    }

    public BattleManager battleManager;
    public Transform frontSlot;
    public Transform[] backSlots;
    public Color fadedTint = new Color(0.55f, 0.55f, 0.55f, 0.7f);
    public int orderStep = 1;
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

    void Start()
    {
        foreach (Combatant c in battleManager.party)
        {
            MemberVisual m = new MemberVisual();
            m.combatant = c;
            m.renderers = c.GetComponentsInChildren<SpriteRenderer>();
            m.baseOrders = new int[m.renderers.Length];
            m.baseColors = new Color[m.renderers.Length];
            m.baseScale = c.transform.localScale;

            for (int i = 0; i < m.renderers.Length; i++)
            {
                m.baseOrders[i] = m.renderers[i].sortingOrder;
                m.baseColors[i] = m.renderers[i].color;
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
    }

    void Arrange(Combatant active, bool snap)
    {
        if (frontSlot == null || backSlots == null || backSlots.Length == 0)
        {
            Debug.LogError("PartyStage: assign the front slot and at least one back slot");
            return;
        }

        int backIndex = 0;

        for (int i = 0; i < members.Count; i++)
        {
            MemberVisual m = members[i];
            bool isActive = m.combatant == active;

            Transform slot;
            int order;

            if (isActive)
            {
                slot = frontSlot;
                order = orderStep * (backSlots.Length + 1);
            }
            else
            {
                int slotIndex = Mathf.Min(backIndex, backSlots.Length - 1);
                slot = backSlots[slotIndex];
                order = orderStep * (backSlots.Length - slotIndex);
                backIndex++;
            }

            Color tint = isActive ? Color.white : fadedTint;

            if (m.routine != null)
            {
                StopCoroutine(m.routine);
            }

            if (snap)
            {
                Place(m, slot, order, tint);
            }
            else
            {
                m.routine = StartCoroutine(MoveMember(m, slot, order, tint));
            }
        }
    }

    void Place(MemberVisual m, Transform slot, int order, Color tint)
    {
        Transform t = m.combatant.transform;
        t.position = slot.position;
        t.localScale = m.baseScale * slot.localScale.x;

        for (int i = 0; i < m.renderers.Length; i++)
        {
            m.renderers[i].sortingOrder = m.baseOrders[i] + order;
            m.renderers[i].color = m.baseColors[i] * tint;
        }
    }

    IEnumerator MoveMember(MemberVisual m, Transform slot, int order, Color tint)
    {
        Transform t = m.combatant.transform;
        Vector3 startPos = t.position;
        Vector3 startScale = t.localScale;
        Vector3 endScale = m.baseScale * slot.localScale.x;

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

            t.position = Vector3.Lerp(startPos, slot.position, p);
            t.localScale = Vector3.Lerp(startScale, endScale, p);

            for (int i = 0; i < m.renderers.Length; i++)
            {
                m.renderers[i].color = Color.Lerp(startColors[i], endColors[i], p);
            }

            yield return null;
        }

        Place(m, slot, order, tint);
    }
}
