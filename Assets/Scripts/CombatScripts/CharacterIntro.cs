using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterIntro : MonoBehaviour
{
    public BattleManager battleManager;
    public Combatant character;
    public bool showEveryTime = true;
    public bool pauseGame = true;

    public CanvasGroup group;
    public Image dim;
    [Range(0f, 1f)] public float dimAlpha = 0.7f;
    public Image cardImage;
    public Sprite cardSprite;

    public TMP_Text hintText;
    public string hint = "CLICK TO CONTINUE";
    public float blinkRate = 1.2f;
    [Range(0f, 1f)] public float blinkMinAlpha = 0.15f;

    public float fadeTime = 0.35f;
    public float minShowTime = 0.5f;

    bool showing;
    bool shown;
    float prevScale = 1f;
    bool prevDebugKeys;

    string Key
    {
        get { return "intro_" + (character != null ? character.name : "none"); }
    }

    void Awake()
    {
        if (group != null)
        {
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }
    }

    void OnEnable()
    {
        if (battleManager != null) battleManager.TurnStarted += OnTurnStarted;
    }

    void OnDisable()
    {
        if (battleManager != null) battleManager.TurnStarted -= OnTurnStarted;
        if (showing) Restore();
    }

    void OnTurnStarted(Combatant c)
    {
        if (showing || shown) return;
        if (character == null || c != character) return;
        if (!showEveryTime && PlayerPrefs.GetInt(Key, 0) == 1) return;
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        showing = true;
        shown = true;

        if (cardImage != null && cardSprite != null) cardImage.sprite = cardSprite;
        if (hintText != null)
        {
            hintText.text = hint;
            hintText.alpha = 1f;
        }
        if (dim != null)
        {
            Color c = dim.color;
            c.a = dimAlpha;
            dim.color = c;
        }

        if (battleManager != null)
        {
            prevDebugKeys = battleManager.useDebugKeys;
            battleManager.useDebugKeys = false;
        }
        prevScale = Time.timeScale;
        if (pauseGame) Time.timeScale = 0f;

        group.interactable = true;
        group.blocksRaycasts = true;

        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Clamp01(t / fadeTime);
            yield return null;
        }
        group.alpha = 1f;

        float opened = Time.unscaledTime;
        while (true)
        {
            if (hintText != null)
            {
                float wave = (Mathf.Sin(Time.unscaledTime * blinkRate * Mathf.PI * 2f) + 1f) * 0.5f;
                hintText.alpha = Mathf.Lerp(blinkMinAlpha, 1f, wave);
            }

            bool ready = Time.unscaledTime - opened >= minShowTime;
            bool input = Input.GetMouseButtonDown(0)
                || Input.GetKeyDown(KeyCode.Return)
                || Input.GetKeyDown(KeyCode.Space)
                || Input.GetKeyDown(KeyCode.Escape);

            if (ready && input) break;
            yield return null;
        }

        t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = 1f - Mathf.Clamp01(t / fadeTime);
            yield return null;
        }
        group.alpha = 0f;

        if (!showEveryTime)
        {
            PlayerPrefs.SetInt(Key, 1);
            PlayerPrefs.Save();
        }

        Restore();
    }

    void Restore()
    {
        if (group != null)
        {
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }
        if (battleManager != null) battleManager.useDebugKeys = prevDebugKeys;
        if (pauseGame) Time.timeScale = prevScale <= 0f ? 1f : prevScale;
        showing = false;
    }
}
