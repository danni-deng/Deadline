using UnityEngine;

public class building : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created



    public float moveSpeed = 5;
    public float PosToDespawn = -17f;

    //private Vector3 buildingPos = transform.position;
    //public GameObject explosion;
    //public GameObject rubble;




    void Start()
    {
        //explosion = GetComponent<Animator>();
    }

    public void DoDestroySequence()
    {
        //GetComponent<SpriteRenderer>().enabled = false;
        //Instantiate(explosion, transform.position, Quaternion.identity);
        //Instantiate(rubble, new Vector3(transform.position.x, transform.position.y, transform.position.z + 3), Quaternion.identity);
        //GameObject.Find("scoreController").GetComponent<scoreController>().addScore(100 * moveSpeed);
        Destroy(gameObject);
        //Time.timeScale = 1;
        //GameObject.Find("helicopterSpawner").SetActive(true);

    }

    // Update is called once per frame
    void Update()
    {
        
        transform.position += Vector3.left * moveSpeed * Time.deltaTime;
        if (transform.position.x < PosToDespawn)
        {
            /*if (GetComponent<BoxCollider2D>().enabled)
            {
                GameObject scoreController = GameObject.Find("scoreController");
                //scoreController.GetComponent<scoreController>().addHP(-5f);

            }
            */
            Destroy(gameObject);
            
        }

    }


}

