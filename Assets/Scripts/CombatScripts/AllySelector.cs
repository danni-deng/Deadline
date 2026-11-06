using UnityEngine;

public class AllySelector : MonoBehaviour
{
    public BattleManager battleManager;
    public Transform marker;
    public Vector3 markerOffset = new Vector3(0f, 2.4f, 0f);
    public float bobHeight = 0.15f;
    public float bobSpeed = 4f;
    public bool clickToSelect = true;

    void Update()
    {
        bool targeting = battleManager.State == BattleState.PlayerTurn && battleManager.IsTargetingAlly;
        Combatant ally = battleManager.SelectedAlly;
        bool show = targeting && ally != null && ally.IsAlive;

        if (marker.gameObject.activeSelf != show)
        {
            marker.gameObject.SetActive(show);
        }

        if (!show)
        {
            return;
        }

        float bob = Mathf.Sin(Time.unscaledTime * bobSpeed) * bobHeight;
        marker.position = ally.transform.position + markerOffset + new Vector3(0f, bob, 0f);

        if (clickToSelect && Input.GetMouseButtonDown(0))
        {
            TryPickByClick();
        }
    }

    void TryPickByClick()
    {
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
                break;
            }
            battleManager.CycleAlly(1);
        }

        if (battleManager.SelectedAlly == clicked)
        {
            battleManager.ConfirmAllyTarget();
        }
    }
}