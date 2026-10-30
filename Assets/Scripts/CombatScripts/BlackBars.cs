using UnityEngine;
using System.Collections;

public class BlackBars : MonoBehaviour
{
    public BattleManager battleManager;
    public RectTransform topBar;
    public RectTransform bottomBar;
    public float barHeight = 80f;
    public float slideTime = 0.5f;
    public float hideAfterEnd = 2f;

    Coroutine slideRoutine;
    float current;
    float targetHeight;

    void Awake()
    {
        SetHeight(0f);
        targetHeight = 0f;
    }

    void OnEnable()
    {
        if (battleManager != null)
        {
            battleManager.StateChanged += OnStateChanged;
        }
    }

    void OnDisable()
    {
        if (battleManager != null)
        {
            battleManager.StateChanged -= OnStateChanged;
        }
    }

    void OnStateChanged(BattleState state)
    {
        if (state == BattleState.Won || state == BattleState.Lost)
        {
            Slide(0f, hideAfterEnd);
        }
        else if (state != BattleState.Inactive)
        {
            Slide(barHeight, 0f);
        }
    }

    void Slide(float target, float delay)
    {
        if (Mathf.Approximately(target, targetHeight))
        {
            return;
        }

        targetHeight = target;

        if (slideRoutine != null)
        {
            StopCoroutine(slideRoutine);
        }
        slideRoutine = StartCoroutine(SlideTo(target, delay));
    }

    IEnumerator SlideTo(float target, float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        float start = current;
        float elapsed = 0f;

        while (elapsed < slideTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / slideTime);
            SetHeight(Mathf.Lerp(start, target, t));
            yield return null;
        }

        SetHeight(target);
    }

    void SetHeight(float height)
    {
        current = height;

        if (topBar != null)
        {
            topBar.sizeDelta = new Vector2(topBar.sizeDelta.x, height);
        }
        if (bottomBar != null)
        {
            bottomBar.sizeDelta = new Vector2(bottomBar.sizeDelta.x, height);
        }
    }
}
