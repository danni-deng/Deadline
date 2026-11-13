using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public static class SpriteInk
{
    public struct Info
    {
        public bool valid;
        public int minX;
        public int minY;
        public int maxX;
        public int maxY;
        public float feetX;
    }

    static Dictionary<Sprite, Info> cache = new Dictionary<Sprite, Info>();

    public static Info Get(Sprite sprite)
    {
        Info info = new Info();

        if (sprite == null)
        {
            return info;
        }

        if (cache.TryGetValue(sprite, out info))
        {
            return info;
        }

        info = Measure(sprite);
        cache[sprite] = info;
        return info;
    }

    static Info Measure(Sprite sprite)
    {
        Info info = new Info();

        Texture2D texture = sprite.texture;
        Rect rect = sprite.rect;
        int rx = Mathf.RoundToInt(rect.x);
        int ry = Mathf.RoundToInt(rect.y);
        int w = Mathf.RoundToInt(rect.width);
        int h = Mathf.RoundToInt(rect.height);

        if (texture == null || w <= 0 || h <= 0 || rx < 0 || ry < 0 || rx + w > texture.width || ry + h > texture.height)
        {
            return info;
        }

        RenderTexture rt = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        Graphics.Blit(texture, rt);

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D readable = new Texture2D(w, h, TextureFormat.RGBA32, false);
        readable.ReadPixels(new Rect(rx, ry, w, h), 0, 0);

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        Color32[] pixels = readable.GetPixels32();
        UnityEngine.Object.Destroy(readable);

        int minX = w;
        int minY = h;
        int maxX = -1;
        int maxY = -1;

        for (int y = 0; y < h; y++)
        {
            int row = y * w;
            for (int x = 0; x < w; x++)
            {
                if (pixels[row + x].a > 8)
                {
                    if (x < minX)
                    {
                        minX = x;
                    }
                    if (x > maxX)
                    {
                        maxX = x;
                    }
                    if (y < minY)
                    {
                        minY = y;
                    }
                    if (y > maxY)
                    {
                        maxY = y;
                    }
                }
            }
        }

        if (maxX < 0)
        {
            return info;
        }

        int band = Mathf.Max(2, Mathf.RoundToInt((maxY - minY) * 0.05f));
        int bandMinX = w;
        int bandMaxX = -1;

        for (int y = minY; y <= Mathf.Min(maxY, minY + band); y++)
        {
            int row = y * w;
            for (int x = 0; x < w; x++)
            {
                if (pixels[row + x].a > 8)
                {
                    if (x < bandMinX)
                    {
                        bandMinX = x;
                    }
                    if (x > bandMaxX)
                    {
                        bandMaxX = x;
                    }
                }
            }
        }

        info.valid = true;
        info.minX = minX;
        info.minY = minY;
        info.maxX = maxX;
        info.maxY = maxY;
        info.feetX = bandMaxX >= 0 ? (bandMinX + bandMaxX + 1) * 0.5f : (minX + maxX + 1) * 0.5f;
        return info;
    }

    static Vector3 ToWorld(SpriteRenderer r, float px, float py)
    {
        Sprite sprite = r.sprite;
        Vector2 pivot = sprite.pivot;
        float ppu = sprite.pixelsPerUnit;

        float lx = (px - pivot.x) / ppu;
        float ly = (py - pivot.y) / ppu;

        if (r.flipX)
        {
            lx = -lx;
        }
        if (r.flipY)
        {
            ly = -ly;
        }

        return r.transform.TransformPoint(new Vector3(lx, ly, 0f));
    }

    public static bool TryWorld(SpriteRenderer r, out Bounds bounds, out float feetX)
    {
        bounds = new Bounds();
        feetX = 0f;

        if (r == null || r.sprite == null)
        {
            return false;
        }

        Info info = Get(r.sprite);
        if (!info.valid)
        {
            return false;
        }

        Vector3 a = ToWorld(r, info.minX, info.minY);
        Vector3 b = ToWorld(r, info.maxX + 1, info.minY);
        Vector3 c = ToWorld(r, info.minX, info.maxY + 1);
        Vector3 d = ToWorld(r, info.maxX + 1, info.maxY + 1);

        bounds = new Bounds(a, Vector3.zero);
        bounds.Encapsulate(b);
        bounds.Encapsulate(c);
        bounds.Encapsulate(d);

        feetX = ToWorld(r, info.feetX, info.minY).x;
        return true;
    }
}

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
    Combatant lastHovered;
    bool wasTargeting;

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
        bool targeting = playerTurn && battleManager.IsTargetingAlly;
        Combatant active = battleManager.ActiveCombatant;
        Combatant ally = battleManager.SelectedAlly;

        UpdateRing(playerTurn && active != null && active.IsAlive, active);
        UpdateArrow(targeting && ally != null && ally.IsAlive, ally);

        if (!targeting)
        {
            wasTargeting = false;
            lastHovered = null;
            return;
        }

        if (clickToSelectAlly)
        {
            HandlePointer();
        }
    }

    bool GetVisual(Combatant c, out Bounds bounds, out float feetX, out int minOrder, out int maxOrder, out int layerId)
    {
        bounds = new Bounds(c.transform.position, Vector3.one);
        feetX = c.transform.position.x;
        minOrder = 0;
        maxOrder = 0;
        layerId = 0;

        SpriteRenderer[] renderers = c.GetComponentsInChildren<SpriteRenderer>();
        if (renderers.Length == 0)
        {
            return false;
        }

        bool first = true;

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer r = renderers[i];
            Bounds b;
            float fx;

            if (!SpriteInk.TryWorld(r, out b, out fx))
            {
                b = r.bounds;
                fx = b.center.x;
            }

            if (first)
            {
                bounds = b;
                feetX = fx;
                minOrder = r.sortingOrder;
                maxOrder = r.sortingOrder;
                layerId = r.sortingLayerID;
                first = false;
            }
            else
            {
                bounds.Encapsulate(b);
                minOrder = Mathf.Min(minOrder, r.sortingOrder);
                maxOrder = Mathf.Max(maxOrder, r.sortingOrder);
            }
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
        float feetX;
        int minOrder;
        int maxOrder;
        int layerId;

        if (!GetVisual(c, out bounds, out feetX, out minOrder, out maxOrder, out layerId))
        {
            return;
        }

        activeRing.transform.position = new Vector3(feetX, bounds.min.y, c.transform.position.z);
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
        float feetX;
        int minOrder;
        int maxOrder;
        int layerId;

        if (!GetVisual(c, out bounds, out feetX, out minOrder, out maxOrder, out layerId))
        {
            return;
        }

        float bob = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        allyArrow.transform.position = new Vector3(bounds.center.x, bounds.max.y + arrowOffset + bob, c.transform.position.z);
        allyArrow.sortingLayerID = layerId;
        allyArrow.sortingOrder = maxOrder + 1;
    }

    Combatant PickUnderPointer()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return null;
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            return null;
        }

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D hit = Physics2D.GetRayIntersection(ray);

        if (hit.collider == null)
        {
            return null;
        }

        Combatant found = hit.collider.GetComponentInParent<Combatant>();
        if (found == null || !found.isPlayerSide || !found.IsAlive)
        {
            return null;
        }

        return found;
    }

    void SelectAlly(Combatant c)
    {
        for (int i = 0; i < battleManager.party.Count; i++)
        {
            if (battleManager.SelectedAlly == c)
            {
                return;
            }
            battleManager.CycleAlly(1);
        }
    }

    void HandlePointer()
    {
        Combatant hovered = PickUnderPointer();

        if (!wasTargeting)
        {
            wasTargeting = true;
            lastHovered = hovered;
        }
        else if (hovered != lastHovered)
        {
            lastHovered = hovered;
            if (hovered != null)
            {
                SelectAlly(hovered);
            }
        }

        if (Input.GetMouseButtonDown(0) && hovered != null)
        {
            SelectAlly(hovered);
            if (battleManager.SelectedAlly == hovered)
            {
                battleManager.ConfirmAllyTarget();
            }
        }
    }
}