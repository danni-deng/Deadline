using UnityEngine;

public class colorChanger : MonoBehaviour
{
    public Sprite[] sprites = new Sprite[3];
    int colorSelected = 0;
    string[] colorName = new string[] { "red", "yellow", "blue" };

    public string getColor()
    {
        return colorName[colorSelected];
    }

    public void SetColor(int index)
    {
        colorSelected = Mathf.Clamp(index, 0, colorName.Length - 1);
        Apply();
    }

    public void PlaySwitchSound()
    {
        AudioSource sound = GetComponent<AudioSource>();
        if (sound != null)
        {
            sound.Play();
        }
    }

    void Start()
    {
        Apply();
    }

    void Apply()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && colorSelected < sprites.Length && sprites[colorSelected] != null)
        {
            sr.sprite = sprites[colorSelected];
        }
    }
}