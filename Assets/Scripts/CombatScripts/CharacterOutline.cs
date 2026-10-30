using System.Collections.Generic;
using UnityEngine;

public class CharacterOutline : MonoBehaviour
{
    public BattleManager battleManager;
    public Combatant combatant;
    public SpriteRenderer source;
    public float thickness = 0.06f;
    public float pulseAmount = 0.25f;
    public float pulseSpeed = 4f;
    public float fadeSpeed = 10f;
    public bool highlightWhenActive = true;
    public bool highlightWhenAllySelected = true;
    public bool highlightWhenTargeted = false;
    public Color activeColor = Color.white;
    public Color allyColor = new Color(0.6f, 0.9f, 1f);
    public Color targetColor = new Color(1f, 0.35f, 0.3f);

    static Dictionary<Sprite, Sprite> cache = new Dictionary<Sprite, Sprite>();

    static Vector2[] directions = new Vector2[]
    {
        new Vector2(1f, 0f),
        new Vector2(-1f, 0f),
        new Vector2(0f, 1f),
        new Vector2(0f, -1f),
        new Vector2(0.7071f, 0.7071f),
        new Vector2(-0.7071f, 0.7071f),
        new Vector2(0.7071f, -0.7071f),
        new Vector2(-0.7071f, -0.7071f)
    };

    GameObject root;
    List<SpriteRenderer> copies = new List<SpriteRenderer>();
    Material baseMaterial;
    Sprite lastSprite;
    Color currentColor = Color.white;
    float strength;

    void Awake()
    {
        if (source == null)
        {
            source = GetComponentInChildren<SpriteRenderer>();
        }
        if (source != null)
        {
            baseMaterial = source.sharedMaterial;
        }

        root = new GameObject("Outline " + name);

        for (int i = 0; i < directions.Length; i++)
        {
            GameObject go = new GameObject("Copy " + i);
            go.transform.SetParent(root.transform, false);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            if (baseMaterial != null)
            {
                sr.sharedMaterial = baseMaterial;
            }
            copies.Add(sr);
        }

        root.SetActive(false);
    }

    void OnDisable()
    {
        strength = 0f;
        if (root != null)
        {
            root.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (root != null)
        {
            Destroy(root);
        }
    }

    bool GetMode(out Color color)
    {
        color = currentColor;

        if (battleManager == null || combatant == null || !combatant.IsAlive)
        {
            return false;
        }

        BattleState state = battleManager.State;
        bool playerTurn = state == BattleState.PlayerTurn;
        bool acting = playerTurn || state == BattleState.Resolving;

        if (highlightWhenActive && acting && battleManager.ActiveCombatant == combatant)
        {
            color = activeColor;
            return true;
        }
        if (highlightWhenAllySelected && playerTurn && battleManager.SelectedAlly == combatant)
        {
            color = allyColor;
            return true;
        }
        if (highlightWhenTargeted && playerTurn && battleManager.SelectedTarget == combatant)
        {
            color = targetColor;
            return true;
        }

        return false;
    }

    void LateUpdate()
    {
        if (combatant == null)
        {
            combatant = GetComponent<Combatant>();
        }
        if (battleManager == null)
        {
            battleManager = FindFirstObjectByType<BattleManager>();
        }
        if (source == null)
        {
            return;
        }

        Color mode;
        bool wanted = GetMode(out mode);
        if (wanted)
        {
            currentColor = mode;
        }

        float goal = wanted ? 1f : 0f;
        strength = Mathf.MoveTowards(strength, goal, fadeSpeed * Time.unscaledDeltaTime);

        if (strength <= 0f)
        {
            if (root.activeSelf)
            {
                root.SetActive(false);
            }
            return;
        }

        if (!root.activeSelf)
        {
            root.SetActive(true);
        }

        Transform st = source.transform;
        root.transform.position = st.position;
        root.transform.rotation = st.rotation;
        root.transform.localScale = st.lossyScale;

        if (source.sprite != lastSprite)
        {
            lastSprite = source.sprite;
            Sprite silhouette = GetSilhouette(lastSprite);

            for (int i = 0; i < copies.Count; i++)
            {
                copies[i].sprite = silhouette;
            }
        }

        Vector3 scale = st.lossyScale;
        float sx = Mathf.Max(0.0001f, Mathf.Abs(scale.x));
        float sy = Mathf.Max(0.0001f, Mathf.Abs(scale.y));
        float pulse = 1f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmount;

        Color tint = currentColor;
        tint.a = currentColor.a * strength;

        for (int i = 0; i < copies.Count; i++)
        {
            SpriteRenderer sr = copies[i];
            float offsetX = directions[i].x * thickness * pulse / sx;
            float offsetY = directions[i].y * thickness * pulse / sy;

            sr.transform.localPosition = new Vector3(offsetX, offsetY, 0f);
            sr.color = tint;
            sr.flipX = source.flipX;
            sr.flipY = source.flipY;
            sr.sortingLayerID = source.sortingLayerID;
            sr.sortingOrder = source.sortingOrder - 1;
        }
    }

    Sprite GetSilhouette(Sprite original)
    {
        if (original == null)
        {
            return null;
        }

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

        RenderTexture rt = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        Graphics.Blit(texture, rt);

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D readable = new Texture2D(w, h, TextureFormat.RGBA32, false);
        readable.ReadPixels(new Rect(x, y, w, h), 0, 0);

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        Color32[] pixels = readable.GetPixels32();
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color32(255, 255, 255, pixels[i].a);
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
