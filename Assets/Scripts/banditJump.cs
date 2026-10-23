using System.Runtime.Serialization;
using System.Threading;
using UnityEngine;

public class banditJump : MonoBehaviour
{

    float timer = 0f;
    float t = 0f;
    float movingRight = 1f;
    public float magnitude = 1f;
    public float distance = 1f;
    public float angleScaling = 1f;
    bool damaged = false;

    public string banditColor = "";
    public string gunColor = "";

    public int banditHP = 10;

    public GameObject gun;
    public GameObject explosion;

    Vector3 startingVector;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        startingVector = transform.position;
        
    }

    // Update is called once per frame
    void Update()
    {

        gun = GameObject.Find("PlayerMini");
        explosion = GameObject.Find("BloodExploder");
        gunColor = gun.GetComponent<playerController>().gunColor;
        timer += Time.deltaTime;
        t += Time.deltaTime * movingRight;

        float yPos = magnitude * t * (1 - t);

        if (!damaged)
        {
            transform.position = startingVector + new Vector3(t * distance, yPos, 0);

            float rotAngle = -(angleScaling / 2) + t * angleScaling;
            transform.rotation = new Quaternion(0, 0, rotAngle, 1);
            transform.localScale = new Vector3(1, 1, 1);
        }
        else
        {
            //transform.position = startingVector + new Vector3(0.5f * distance, yPos, 0);
        }
        if (Input.GetKeyDown(KeyCode.Space) && banditHP > 0 && banditColor == gunColor)
        {

            transform.rotation = new Quaternion(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360), 1);
            transform.localScale = new Vector3(Random.Range(0.5f, 1.5f), Random.Range(0.5f, 1.5f), 1);
            damaged = true;
            t = 0.5f;
            banditHP--;
            GetComponent<AudioSource>().Play();

        }
        else if (Input.GetKeyDown(KeyCode.Space) && banditColor == gunColor)
        {
            explosion.GetComponent<bloodExplosion>().doExplosion();
            
            Destroy(gameObject);
        }

        

            if (t >= 1)
        {
            movingRight = -1;
            damaged = false;
        }
        else if(t <= 0)
        {
            movingRight = 1;
            damaged = false;
        }




    }
}
