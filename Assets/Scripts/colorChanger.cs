using UnityEngine;

public class colorChanger : MonoBehaviour
{

    public Sprite[] sprites = new Sprite[3];
    int colorSelected = 0;
    string[] colorName = new string[3];
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        colorName = new string[] { "red", "yellow", "blue" };
    }

    public string getColor()
    {
        return colorName[colorSelected];
    }

    // Update is called once per frame
    void Update()
    {
        GetComponent<SpriteRenderer>().sprite = sprites[colorSelected];

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            GetComponent<AudioSource>().Play();
            colorSelected++;
        }
        if(colorSelected > 2)
        {
            colorSelected = 0;
        }
    }
}
