using UnityEngine;

public class bloodExplosion : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public GameObject followBandit;
    public GameObject banditSpawner;
    float timer = 0f;
    bool doTimer = false;

    void Start()
    {
        
    }

    public void doExplosion()
    {
        GetComponent<ParticleSystem>().Play();
        
        
        doTimer = true;
    }

    // Update is called once per frame
    void Update()

    {

        if (doTimer)
        {
            timer += Time.deltaTime;
        }
        if(timer >= 1)
        {
            banditSpawner.GetComponent<banditSpawner>().spawnNewBandit();
            timer = 0;
            doTimer = false;
        }
        transform.position = followBandit.transform.position;
        if (Input.GetKeyDown(KeyCode.Space))
        {
            //GetComponent<ParticleSystem>().Play();
        }
    }
}
