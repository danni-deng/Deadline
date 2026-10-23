using UnityEngine;

public class railcarMovement : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    float timer = 0f;
    public float magnitude = 0f;
    public float speed = 1f;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        transform.position += Vector3.left * magnitude * Mathf.Sin(speed * timer);
    }
}
