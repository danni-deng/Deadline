using TMPro;
using UnityEngine;

public class DescriptionBox : MonoBehaviour
{
    public GameObject panel;
    public TMP_Text text;

    static DescriptionBox instance;

    void Awake()
    {
        instance = this;
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
    public static void Show(string message)
    {
        if (instance == null || instance.panel == null || instance.text == null || string.IsNullOrEmpty(message))
        {
            return;
        }

        instance.text.text = message;
        instance.panel.SetActive(true);
    }

    public static void Hide()
    {
        if (instance != null && instance.panel != null)
        {
            instance.panel.SetActive(false);
        }
    }
}