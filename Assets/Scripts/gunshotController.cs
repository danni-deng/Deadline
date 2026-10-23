using UnityEngine;

public class gunshotController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public AudioSource audioSource;
    public AudioClip gun1;
    public AudioClip gun2;
    public AudioClip gun3;
    public AudioClip gun4;
    AudioClip[] gunArray;
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        gunArray = new AudioClip[] { gun1, gun2, gun3, gun4 };
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            audioSource.clip = gunArray[Random.Range(0, 4)];
            audioSource.Play();
        }
    }
}
