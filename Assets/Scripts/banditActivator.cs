using UnityEngine;
using UnityEngine.UIElements;

public class banditActivator : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(transform.position.y < -1.4f)
        {
            transform.position = new Vector3(transform.position.x, -1.4f, transform.position.z);
            Destroy(GetComponent<Rigidbody2D>());
            //GetComponent<BoxCollider2D>().enabled = false;
            GetComponent<banditJump>().enabled = true;
        }
    }
    /*

    private void OnTriggerEnter(Collision collision)
    {
        GetComponent<Rigidbody2D>().gravityScale = 0;
        GetComponent<BoxCollider2D>().enabled = false;
        GetComponent<banditJump>().enabled = true;
    }
    */
}
