using UnityEngine;
using UnityEngine.SceneManagement;

public class playerController : MonoBehaviour
{

    //bool animationState = false;
    public float gunshotTime = 0.5f;
    public float gunshotAmount = 0.5f;
    float timer = 0;
    Animator animator;
    GameObject camera;

    public string gunColor = "red";

    public GameObject colorChanger;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        camera = GameObject.Find("Main Camera");
        gunColor = colorChanger.GetComponent<colorChanger>().getColor();
        if (Input.GetKeyDown(KeyCode.Space))
        {
            //animationState = true;
            animator.SetTrigger("triggerFUCK");
            camera.GetComponent<CameraShake>().shakeDuration = gunshotTime;
            camera.GetComponent<CameraShake>().shakeAmount = gunshotAmount;
            camera.GetComponent<CameraShake>().gunshotTime = true;
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene("SampleScene");
        }
    }

}
