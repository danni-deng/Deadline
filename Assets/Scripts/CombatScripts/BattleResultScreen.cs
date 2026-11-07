using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BattleResultScreen : MonoBehaviour
{
    public BattleManager battleManager;
    public CanvasGroup winGroup;
    public CanvasGroup loseGroup;
    public Button winRetryButton;
    public Button loseRetryButton;
    public Button nextLevelButton;
    public string nextSceneName = "";
    public float showDelay = 1.2f;
    public float fadeTime = 0.5f;

    bool shown;

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

    void Start()
    {
        Hide(winGroup);
        Hide(loseGroup);

        if (winRetryButton != null)
        {
            winRetryButton.onClick.AddListener(Retry);
        }
        if (loseRetryButton != null)
        {
            loseRetryButton.onClick.AddListener(Retry);
        }
        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.AddListener(NextLevel);
            nextLevelButton.gameObject.SetActive(!string.IsNullOrEmpty(nextSceneName));
        }
    }

    void Hide(CanvasGroup group)
    {
        if (group == null)
        {
            return;
        }

        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    void OnStateChanged(BattleState state)
    {
        if (shown)
        {
            return;
        }

        if (state == BattleState.Won)
        {
            shown = true;
            StartCoroutine(Show(winGroup));
        }
        else if (state == BattleState.Lost)
        {
            shown = true;
            StartCoroutine(Show(loseGroup));
        }
    }

    IEnumerator Show(CanvasGroup group)
    {
        if (group == null)
        {
            yield break;
        }

        yield return new WaitForSecondsRealtime(showDelay);

        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Clamp01(elapsed / fadeTime);
            yield return null;
        }

        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;
    }

    void Retry()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void NextLevel()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
