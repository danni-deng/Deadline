using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EnemyColorView : MonoBehaviour
{
    public Combatant combatant;
    public SpriteRenderer[] renderers;
    public bool convertToGray = true;
    public bool tintSprite = true;
    public bool showHp = true;
    public Color baseTint = new Color(0.85f, 0.85f, 0.85f);
    public float fontSize = 4f;
    public float heightOffset = 0.25f;
    public string sortingLayerName = "Default";
    public int sortingOrder = 300;

    static Dictionary<Sprite, Sprite> cache = new Dictionary<Sprite, Sprite>();

    GameObject labelObject;
    TextMeshPro label;
    Camera cam;
    bool dirty = true;

    void Awake()
    {
        if (combatant == null)
        {
            combatant = GetComponent<Combatant>();
        }
        if (renderers == null || renderers.Length == 0)
        {
            renderers = GetComponentsInChildren<SpriteRenderer>();
        }

        if (convertToGray)
        {
            foreach (SpriteRenderer sr in renderers)
            {
                if (sr != null && sr.sprite != null)
                {
                    sr.sprite = GetGraySprite(sr.sprite);
                }
            }
        }

        labelObject = new GameObject("Status " + name);
        label = labelObject.AddComponent<TextMeshPro>();
        label.rectTransform.pivot = new Vector2(0.5f, 0f);
        label.rectTransform.sizeDelta = new Vector2(10f, 4f);
        label.alignment = TextAlignmentOptions.Bottom;
        label.fontSize = fontSize;
        label.fontStyle = FontStyles.Bold;
        label.richText = true;
        label.sortingLayerID = SortingLayer.NameToID(sortingLayerName);
        label.sortingOrder = sortingOrder;
        label.text = "";
    }

    void OnEnable()
    {
        if (labelObject != null)
        {
            labelObject.SetActive(true);
        }

        Combatant.AnyStatusChanged += OnStatus;
        Combatant.AnyDamaged += OnDamage;
        Combatant.AnyHealed += OnDamage;
        dirty = true;
    }

    void OnDisable()
    {
        Combatant.AnyStatusChanged -= OnStatus;
        Combatant.AnyDamaged -= OnDamage;
        Combatant.AnyHealed -= OnDamage;

        if (labelObject != null)
        {
            labelObject.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (labelObject != null)
        {
            Destroy(labelObject);
        }
    }

    void OnStatus(Combatant who)
    {
        if (who == combatant)
        {
            dirty = true;
        }
    }

    void OnDamage(Combatant who, int amount)
    {
        if (who == combatant)
        {
            dirty = true;
        }
    }

    float Luminance(Color32 c)
    {
        return (0.299f * c.r + 0.587f * c.g + 0.114f * c.b) / 255f;
    }

    Sprite GetGraySprite(Sprite original)
    {
        Sprite cached;
        if (cache.TryGetValue(original, out cached) && cached != null)
        {
            return cached;
        }

        Texture2D texture = original.texture;
        Rect rect = original.textureRect;
        int x = Mathf.RoundToInt(rect.x);
        int y = Mathf.RoundToInt(rect.y);
        int w = Mathf.RoundToInt(rect.width);
        int h = Mathf.RoundToInt(rect.height);

        RenderTexture rt = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(texture, rt);

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D readable = new Texture2D(w, h, TextureFormat.RGBA32, false);
        readable.ReadPixels(new Rect(x, y, w, h), 0, 0);

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        Color32[] pixels = readable.GetPixels32();

        float brightest = 0.01f;
        for (int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].a > 128)
            {
                float g = Luminance(pixels[i]);
                if (g > brightest)
                {
                    brightest = g;
                }
            }
        }

        float scale = 1f / brightest;
        for (int i = 0; i < pixels.Length; i++)
        {
            float g = Mathf.Clamp01(Luminance(pixels[i]) * scale);
            byte v = (byte)Mathf.RoundToInt(g * 255f);
            pixels[i] = new Color32(v, v, v, pixels[i].a);
        }

        readable.SetPixels32(pixels);
        readable.Apply();
        readable.filterMode = texture.filterMode;
        readable.wrapMode = TextureWrapMode.Clamp;

        Vector2 pivot = new Vector2(original.pivot.x / w, original.pivot.y / h);
        Sprite result = Sprite.Create(readable, new Rect(0, 0, w, h), pivot, original.pixelsPerUnit);

        cache[original] = result;
        return result;
    }

    void LateUpdate()
    {
        if (combatant == null || combatant.data == null)
        {
            return;
        }

        if (cam == null)
        {
            cam = Camera.main;
        }

        if (dirty)
        {
            Refresh();
        }

        Bounds bounds = new Bounds(transform.position, Vector3.zero);
        bool hasBounds = false;

        foreach (SpriteRenderer sr in renderers)
        {
            if (sr == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = sr.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(sr.bounds);
            }
        }

        labelObject.transform.position = new Vector3(bounds.center.x, bounds.max.y + heightOffset, transform.position.z);

        if (cam != null)
        {
            labelObject.transform.rotation = cam.transform.rotation;
        }
    }

    void Refresh()
    {
        dirty = false;

        string text = "";

        if (showHp)
        {
            text += "<color=#CCCCCC>" + combatant.currentHP + "/" + combatant.data.maxHP + "</color>\n";
        }

        if (combatant.bleedStacks > 0)
        {
            text += "<color=#FF5A4D>BLEED x" + combatant.bleedStacks + "  -" + combatant.BleedDamagePerTick + " HP</color>\n";
        }

        if (combatant.stunned)
        {
            text += "<color=#FFD84A>STUNNED</color>\n";
        }
        else if (combatant.stunImmuneTurns > 0)
        {
            text += "<color=#9A8A3A>STUN RESIST " + combatant.stunImmuneTurns + "</color>\n";
        }
        else if (combatant.stunBuildup > 0)
        {
            text += "<color=#FFD84A>STUN " + combatant.stunBuildup + "/" + combatant.StunThreshold + "</color>\n";
        }

        if (combatant.slowStacks > 0)
        {
            text += "<color=#5C97FF>SLOW -" + Mathf.RoundToInt(combatant.SlowPercent * 100f) + "%</color>\n";
        }

        label.text = text.TrimEnd('\n');

        if (tintSprite)
        {
            Color tint = baseTint;

            if (combatant.bleedStacks > 0)
            {
                tint = Color.Lerp(tint, BattleColorUtil.ToUnityColor(BattleColor.Red), Mathf.Min(0.6f, 0.12f * combatant.bleedStacks));
            }
            if (combatant.slowStacks > 0)
            {
                tint = Color.Lerp(tint, BattleColorUtil.ToUnityColor(BattleColor.Blue), Mathf.Min(0.6f, 0.15f * combatant.slowStacks));
            }
            if (combatant.stunned)
            {
                tint = Color.Lerp(tint, BattleColorUtil.ToUnityColor(BattleColor.Yellow), 0.6f);
            }

            foreach (SpriteRenderer sr in renderers)
            {
                if (sr != null)
                {
                    sr.color = tint;
                }
            }
        }
    }
}