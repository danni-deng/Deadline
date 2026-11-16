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

    public LevelChanger levelChanger;
    public bool autoAdvance = true;
    public float autoAdvanceDelay = 2.5f;

    bool shown;
    bool advancing;

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
            nextLevelButton.onClick.AddListener(AdvanceLevel);
            bool canAdvance = levelChanger != null || !string.IsNullOrEmpty(nextSceneName);
            nextLevelButton.gameObject.SetActive(canAdvance && !autoAdvance);
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
            StartCoroutine(Show(winGroup, true));
        }
        else if (state == BattleState.Lost)
        {
            shown = true;
            StartCoroutine(Show(loseGroup, false));
        }
    }

    IEnumerator Show(CanvasGroup group, bool won)
    {
        if (group != null)
        {
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
        else if (won)
        {
            yield return new WaitForSecondsRealtime(showDelay);
        }

        if (won && autoAdvance)
        {
            yield return new WaitForSecondsRealtime(autoAdvanceDelay);
            AdvanceLevel();
        }
    }

    void Retry()
    {
        if (advancing)
        {
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void AdvanceLevel()
    {
        if (advancing)
        {
            return;
        }

        advancing = true;

        if (levelChanger != null)
        {
            if (!string.IsNullOrEmpty(nextSceneName))
            {
                levelChanger.FadeToScene(nextSceneName);
            }
            else
            {
                levelChanger.FadeToNextLevel();
            }
            return;
        }

        Time.timeScale = 1f;

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
            return;
        }

        int next = SceneManager.GetActiveScene().buildIndex + 1;
        if (next >= SceneManager.sceneCountInBuildSettings)
        {
            next = 0;
        }
        SceneManager.LoadScene(next);
    }
}