using UnityEngine;
using Yarn.Unity;

public class dialogueFunctions : MonoBehaviour
{

    public Sprite[] images = new Sprite[12];
    public string[] imageNames = new string[12];

    public string currentImage = "blank";



    [YarnCommand("changeImage")]
    public void ImageController(string image)
    {
        for (int i = 0; i < images.Length; i++)
        {
            if (image == imageNames[i])
            {
                GetComponent<SpriteRenderer>().sprite = images[i];
                currentImage = image;
            }
        }


    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
