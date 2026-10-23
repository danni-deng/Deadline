
using UnityEngine;

public class banditSpawner : MonoBehaviour
{

    public GameObject bandit;
    string[] colors = new string[3];
    public Sprite[] banditSprites = new Sprite[3];

   

    public GameObject bloodExploder;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        colors = new string[] { "red", "yellow", "blue" };
    }

    public void spawnNewBandit()
    {
        float randomBandit = Random.Range(0, 3);
        GameObject newBandit = Instantiate(bandit);
        newBandit.GetComponent<banditJump>().banditColor = colors[(int)randomBandit];
        newBandit.GetComponent<SpriteRenderer>().sprite = banditSprites[(int)randomBandit];
        bloodExploder.GetComponent<bloodExplosion>().followBandit = newBandit;
        Color[] colorTime = new Color[] { Color.red, Color.yellow, Color.blue }; 
        ParticleSystem ps = bloodExploder.GetComponent<ParticleSystem>();
        var mainMod = ps.main;
        mainMod.startColor = colorTime[(int)randomBandit];


    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {


            spawnNewBandit();
        }
    }
}
