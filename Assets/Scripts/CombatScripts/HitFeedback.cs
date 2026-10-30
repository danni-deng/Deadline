using TMPro;
using UnityEngine;
using System.Collections;

public class HitFeedback : MonoBehaviour
{
    public float hitStopTime = 0.09f;
    public float killHitStopTime = 0.18f;
    public float flashTime = 0.12f;
    public float numberSize = 8f;
    public float numberLifetime = 1f;
    public float numberRiseSpeed = 1.2f;
    public Vector3 numberOffset = new Vector3(0f, 1.2f, 0f);
    public Color damageColor = Color.white;
    public Color healColor = new Color(0.5f, 1f, 0.6f);
    public string sortingLayerName = "Default";
    public int sortingOrder = 200;
    public Combatant testTarget;
    public KeyCode testKey = KeyCode.T;

    Camera cam;
    Material flashMaterial;
    Coroutine stopRoutine;

    void Awake()
    {
        cam = Camera.main;

        Shader shader = Shader.Find("GUI/Text Shader");
        if (shader != null)
        {
            flashMaterial = new Material(shader);
            flashMaterial.color = Color.white;
        }
        else
        {
            Debug.LogWarning("HitFeedback: flash shader not found, using fade instead");
        }

        if (TMP_Settings.defaultFontAsset == null)
        {
            Debug.LogWarning("HitFeedback: TextMeshPro resources are missing, import TMP Essentials");
        }
    }

    void OnEnable()
    {
        Combatant.AnyDamaged += OnDamaged;
        Combatant.AnyHealed += OnHealed;
    }

    void OnDisable()
    {
        Combatant.AnyDamaged -= OnDamaged;
        Combatant.AnyHealed -= OnHealed;
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (testTarget != null && Input.GetKeyDown(testKey))
        {
            OnDamaged(testTarget, 5);
        }
    }

    void OnDamaged(Combatant target, int amount)
    {
        Debug.Log("HitFeedback: hit on " + target.name + " for " + amount);

        float stop = target.IsAlive ? hitStopTime : killHitStopTime;

        if (stopRoutine != null)
        {
            StopCoroutine(stopRoutine);
        }
        stopRoutine = StartCoroutine(HitStop(stop));

        StartCoroutine(Flash(target));
        SpawnNumber(target.transform.position, amount.ToString(), damageColor);
    }

    void OnHealed(Combatant target, int amount)
    {
        Debug.Log("HitFeedback: heal on " + target.name + " for " + amount);
        SpawnNumber(target.transform.position, "+" + amount, healColor);
    }

    IEnumerator HitStop(float duration)
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }

    IEnumerator Flash(Combatant target)
    {
        SpriteRenderer[] renderers = target.GetComponentsInChildren<SpriteRenderer>();
        Material[] originalMaterials = new Material[renderers.Length];
        Color[] originalColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].sharedMaterial;
            originalColors[i] = renderers[i].color;

            if (flashMaterial != null)
            {
                renderers[i].sharedMaterial = flashMaterial;
            }
            else
            {
                Color faded = originalColors[i];
                faded.a = 0.3f;
                renderers[i].color = faded;
            }
        }

        yield return new WaitForSecondsRealtime(flashTime);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].sharedMaterial = originalMaterials[i];
                renderers[i].color = originalColors[i];
            }
        }
    }

    void SpawnNumber(Vector3 position, string message, Color color)
    {
        GameObject go = new GameObject("DamageNumber");
        TextMeshPro text = go.AddComponent<TextMeshPro>();
        go.transform.position = position + numberOffset;

        text.text = message;
        text.fontSize = numberSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.sortingLayerID = SortingLayer.NameToID(sortingLayerName);
        text.sortingOrder = sortingOrder;

        StartCoroutine(AnimateNumber(go.transform, text));
    }

    IEnumerator AnimateNumber(Transform t, TextMeshPro text)
    {
        Color startColor = text.color;
        float elapsed = 0f;

        while (elapsed < numberLifetime)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / numberLifetime;

            t.position += Vector3.up * numberRiseSpeed * Time.unscaledDeltaTime;

            if (cam != null)
            {
                t.rotation = cam.transform.rotation;
            }

            float pop = Mathf.Lerp(1.5f, 1f, Mathf.Clamp01(elapsed / 0.15f));
            t.localScale = Vector3.one * pop;

            Color c = startColor;
            c.a = 1f - Mathf.Clamp01((progress - 0.5f) * 2f);
            text.color = c;

            yield return null;
        }

        Destroy(t.gameObject);
    }
}
