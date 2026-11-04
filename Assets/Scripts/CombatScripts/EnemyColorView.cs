using System.Collections.Generic;
using UnityEngine;

public class EnemyColorView : MonoBehaviour
{
    public Combatant combatant;
    public SpriteRenderer[] renderers;
    public bool convertToGray = true;
    public Color baseTint = new Color(0.95f, 0.95f, 0.95f);
    public float buildupStrength = 0.9f;
    public float debuffStrength = 0.5f;
    public float changeSpeed = 6f;

    static Dictionary<Sprite, Sprite> cache = new Dictionary<Sprite, Sprite>();

    Color currentTint;
    Color targetTint;
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

        currentTint = baseTint;
        targetTint = baseTint;

        foreach (SpriteRenderer sr in renderers)
        {
            if (sr == null)
            {
                continue;
            }

            if (convertToGray && sr.sprite != null)
            {
                sr.sprite = GetGraySprite(sr.sprite);
            }
            sr.color = baseTint;
        }
    }

    void OnEnable()
    {
        Combatant.AnyStatusChanged += OnStatusChanged;
        dirty = true;
    }

    void OnDisable()
    {
        Combatant.AnyStatusChanged -= OnStatusChanged;
    }

    void OnStatusChanged(Combatant who)
    {
        if (who == combatant)
        {
            dirty = true;
        }
    }

    void LateUpdate()
    {
        if (combatant == null)
        {
            combatant = GetComponent<Combatant>();
            if (combatant == null)
            {
                return;
            }
            dirty = true;
        }

        if (combatant.data == null)
        {
            return;
        }

        if (dirty)
        {
            targetTint = ComputeTarget();
            dirty = false;
        }

        float blend = 1f - Mathf.Exp(-changeSpeed * Time.unscaledDeltaTime);
        currentTint = Color.Lerp(currentTint, targetTint, blend);

        foreach (SpriteRenderer sr in renderers)
        {
            if (sr != null)
            {
                sr.color = currentTint;
            }
        }
    }

    Color ComputeTarget()
    {
        Color tint = baseTint;
        float threshold = Mathf.Max(1, combatant.BuildupThreshold);

        for (int i = 0; i < 3; i++)
        {
            BattleColor color = (BattleColor)i;
            float progress = Mathf.Clamp01(combatant.buildup[i] / threshold);
            float amount = progress * buildupStrength;

            if (combatant.DebuffTurns(color) > 0)
            {
                amount = Mathf.Max(amount, debuffStrength);
            }

            if (amount > 0f)
            {
                tint = Color.Lerp(tint, BattleColorUtil.ToUnityColor(color), amount);
            }
        }

        return tint;
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
}