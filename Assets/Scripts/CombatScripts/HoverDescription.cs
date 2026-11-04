using UnityEngine;
using UnityEngine.EventSystems;

public class HoverDescription : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string fixedText;
    public System.Func<string> getText;

    public void OnPointerEnter(PointerEventData eventData)
    {
        string message = getText != null ? getText() : fixedText;
        DescriptionBox.Show(message);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        DescriptionBox.Hide();
    }

    void OnDisable()
    {
        DescriptionBox.Hide();
    }
}
