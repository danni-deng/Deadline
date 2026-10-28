using UnityEngine;
using UnityEngine.EventSystems;

public class PartyHighlight : MonoBehaviour
{
    public BattleManager battleManager;
    public SpriteRenderer activeRing;
    public SpriteRenderer allyArrow;
    public float ringPulseSpeed = 3f;
    public float ringPulseAmount = 0.08f;
    public float arrowOffset = 0.4f;
    public float bobHeight = 0.12f;
    public float bobSpeed = 4f;
    public bool clickToSelectAlly = true;

    Vector3 ringBaseScale = Vector3.one;

    void Awake()
    {
        if (activeRing != null)
        {
            ringBaseScale = activeRing.transform.localScale;
        }
    }

    void Update()
    {
        bool playerTurn = battleManager.State == BattleState.PlayerTurn;
        Combatant active = battleManager.ActiveCombatant;
        Combatant ally = battleManager.SelectedAlly;

        UpdateRing(playerTurn && active != null && active.IsAlive, active);
        UpdateArrow(playerTurn && ally != null && ally.IsAlive && ally != active, ally);

        if (clickToSelectAlly && playerTurn && Input.GetMouseButtonDown(0))
        {
            TrySelectByClick();
        }
    }

    bool GetVisual(Combatant c, out Bounds bounds, out int minOrder, out int maxOrder, out int layerId)
    {
        bounds = new Bounds(c.transform.position, Vector3.one);
        minOrder = 0;
        maxOrder = 0;
        layerId = 0;

        SpriteRenderer[] renderers = c.GetComponentsInChildren<SpriteRenderer>();
        if (renderers.Length == 0)
        {
            return false;
        }

        bounds = renderers[0].bounds;
        minOrder = renderers[0].sortingOrder;
        maxOrder = renderers[0].sortingOrder;
        layerId = renderers[0].sortingLayerID;

        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
            minOrder = Mathf.Min(minOrder, renderers[i].sortingOrder);
            maxOrder = Mathf.Max(maxOrder, renderers[i].sortingOrder);
        }

        return true;
    }

    void UpdateRing(bool show, Combatant c)
    {
        if (activeRing == null)
        {
            return;
        }

        if (activeRing.gameObject.activeSelf != show)
        {
            activeRing.gameObject.SetActive(show);
        }
        if (!show)
        {
            return;
        }

        Bounds bounds;
        int minOrder;
        int maxOrder;
        int layerId;

        if (!GetVisual(c, out bounds, out minOrder, out maxOrder, out layerId))
        {
            return;
        }

        activeRing.transform.position = new Vector3(bounds.center.x, bounds.min.y, c.transform.position.z);
        activeRing.sortingLayerID = layerId;
        activeRing.sortingOrder = minOrder - 1;

        float pulse = 1f + Mathf.Sin(Time.time * ringPulseSpeed) * ringPulseAmount;
        activeRing.transform.localScale = ringBaseScale * pulse;
    }

    void UpdateArrow(bool show, Combatant c)
    {
        if (allyArrow == null)
        {
            return;
        }

        if (allyArrow.gameObject.activeSelf != show)
        {
            allyArrow.gameObject.SetActive(show);
        }
        if (!show)
        {
            return;
        }

        Bounds bounds;
        int minOrder;
        int maxOrder;
        int layerId;

        if (!GetVisual(c, out bounds, out minOrder, out maxOrder, out layerId))
        {
            return;
        }

        float bob = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        allyArrow.transform.position = new Vector3(bounds.center.x, bounds.max.y + arrowOffset + bob, c.transform.position.z);
        allyArrow.sortingLayerID = layerId;
        allyArrow.sortingOrder = maxOrder + 1;
    }

    void TrySelectByClick()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            return;
        }

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D hit = Physics2D.GetRayIntersection(ray);

        if (hit.collider == null)
        {
            return;
        }

        Combatant clicked = hit.collider.GetComponentInParent<Combatant>();
        if (clicked == null || !clicked.isPlayerSide || !clicked.IsAlive)
        {
            return;
        }

        for (int i = 0; i < battleManager.party.Count; i++)
        {
            if (battleManager.SelectedAlly == clicked)
            {
                return;
            }
            battleManager.CycleAlly(1);
        }
    }
}
