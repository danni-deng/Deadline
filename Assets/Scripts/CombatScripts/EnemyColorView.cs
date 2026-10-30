using System.Collections.Generic;
using UnityEngine;

public class EnemyColorView : MonoBehaviour
{
    public Combatant combatant;
    public SpriteRenderer[] renderers;
    public bool convertToGray = true;
    public Color grayTint = new Color(0.75f, 0.75f, 0.75f);
    public Color orange = new Color(1f, 0.55f, 0.1f);
    public Color green = new Color(0.3f, 0.8f, 0.3f);
    public Color purple = new Color(0.6f, 0.25f, 0.8f);
    public Color allColors = new Color(0.55f, 0.4f, 0.3f);
    public float progressStrength = 0.15f;

    static Dictionary<Sprite, Sprite> cache = new Dictionary<Sprite, Sprite>();

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
    }

    void OnEnable()
    {
        Combatant.AnyColorStateChanged += OnColorStateChanged;
    }

    void OnDisable()
    {
        Combatant.AnyColorStateChanged -= OnColorStateChanged;
    }

    void Start()
    {
        Refresh();
    }

    void OnColorStateChanged(Combatant who)
    {
        if (who == combatant)
        {
            Refresh();
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

    void Refresh()
    {
        if (combatant == null)
        {
            return;
        }

        Color tint = BaseTint();

        int best = -1;
        int bestProgress = 0;
        for (int i = 0; i < 3; i++)
        {
            if (combatant.colorProgress[i] > bestProgress)
            {
                bestProgress = combatant.colorProgress[i];
                best = i;
            }
        }

        if (best >= 0)
        {
            Color next = BattleColorUtil.ToUnityColor((BattleColor)best);
            tint = Color.Lerp(tint, next, bestProgress * progressStrength);
        }

        foreach (SpriteRenderer sr in renderers)
        {
            if (sr != null)
            {
                sr.color = tint;
            }
        }
    }

    Color BaseTint()
    {
        bool red = combatant.HasColor(BattleColor.Red);
        bool yellow = combatant.HasColor(BattleColor.Yellow);
        bool blue = combatant.HasColor(BattleColor.Blue);

        int count = 0;
        if (red) { count++; }
        if (yellow) { count++; }
        if (blue) { count++; }

        if (count == 0)
        {
            return grayTint;
        }
        if (count == 3)
        {
            return allColors;
        }
        if (count == 1)
        {
            if (red)
            {
                return BattleColorUtil.ToUnityColor(BattleColor.Red);
            }
            if (yellow)
            {
                return BattleColorUtil.ToUnityColor(BattleColor.Yellow);
            }
            return BattleColorUtil.ToUnityColor(BattleColor.Blue);
        }

        if (red && yellow)
        {
            return orange;
        }
        if (red && blue)
        {
            return purple;
        }
        return green;
    }
}