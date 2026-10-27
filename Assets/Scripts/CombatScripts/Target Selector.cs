using UnityEngine;

public class TargetSelector : MonoBehaviour
{
    public BattleManager battleManager;
    public Transform marker;
    public Vector3 markerOffset = new Vector3(0f, 1.8f, 0f);
    public float bobHeight = 0.15f;
    public float bobSpeed = 4f;
    public bool clickToSelect = true;

    void Update()
    {
        Combatant target = battleManager.SelectedTarget;
        bool show = battleManager.State == BattleState.PlayerTurn && target != null && target.IsAlive;

        if (marker.gameObject.activeSelf != show)
        {
            marker.gameObject.SetActive(show);
        }

        if (!show)
        {
            return;
        }

        float bob = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        marker.position = target.transform.position + markerOffset + new Vector3(0f, bob, 0f);

        if (clickToSelect && Input.GetMouseButtonDown(0))
        {
            TrySelectByClick();
        }
    }

    void TrySelectByClick()
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
        if (clicked == null || clicked.isPlayerSide || !clicked.IsAlive)
        {
            return;
        }

        for (int i = 0; i < battleManager.enemies.Count; i++)
        {
            if (battleManager.SelectedTarget == clicked)
            {
                return;
            }
            battleManager.CycleTarget(1);
        }
    }
}
