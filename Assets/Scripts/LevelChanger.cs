using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelChanger : MonoBehaviour
{
    public Animator animator;
    public string fadeTrigger = "FadeOut";
    public float fadeDuration = 1f;

    int sceneIndexToLoad = -1;
    string sceneNameToLoad = "";
    bool fading;
    bool loaded;

    public void FadeToNextLevel()
    {
        int next = SceneManager.GetActiveScene().buildIndex + 1;
        if (next >= SceneManager.sceneCountInBuildSettings)
        {
            next = 0;
        }
        FadeToLevel(next);
    }

    public void FadeToLevel(int levelIndex)
    {
        if (fading)
        {
            return;
        }

        fading = true;
        sceneIndexToLoad = levelIndex;
        sceneNameToLoad = "";
        Begin();
    }

    public void FadeToScene(string sceneName)
    {
        if (fading)
        {
            return;
        }

        fading = true;
        sceneNameToLoad = sceneName;
        Begin();
    }

    void Begin()
    {
        if (animator != null)
        {
            animator.SetTrigger(fadeTrigger);
        }
        StartCoroutine(LoadAfterFade());
    }

    IEnumerator LoadAfterFade()
    {
        yield return new WaitForSecondsRealtime(fadeDuration);
        OnFadeComplete();
    }

    public void OnFadeComplete()
    {
        if (loaded)
        {
            return;
        }

        loaded = true;
        Time.timeScale = 1f;

        if (!string.IsNullOrEmpty(sceneNameToLoad))
        {
            SceneManager.LoadScene(sceneNameToLoad);
        }
        else
        {
            SceneManager.LoadScene(sceneIndexToLoad);
        }
    }
}
