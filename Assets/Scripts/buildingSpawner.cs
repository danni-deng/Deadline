using UnityEngine;

public class buildingSpawner : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public GameObject building;

    public Sprite building1;
    public Sprite building2;
    public Sprite building3;
    public Sprite building4;
    public Sprite building5;

    public float buildingSpeed = 5f;

    public bool enabledCollider = true;
    public int renderLayerNumber = 0;

    public float shadeOfBuilding = 0f;
    public float depthOfBuilding = 0f;

    public float yPosOfBuilding = 0;

    public float scalingFactor = 1;



    public Sprite[] buildingList;

    private float timer = 0f;
    public float nextBuilding = 3f;
    private float nextBuildingSaved;// = nextBuilding;

    public float timeTillNext(float buildingSpeed)
    {
        float widthOfBuilding = 2.8f;
        float t; //what to set the new timer general spawn to

        t = scalingFactor * (widthOfBuilding / (1.2f * buildingSpeed)); //take the speed and width of building
                                                                        // distance to travel in 1 second of movement


        return t;
    }

    void Start()
    {
        buildingList = new Sprite[] { building1, building2, building3, building4, building5 };
        nextBuildingSaved = nextBuilding;
        timer = nextBuilding;
    }

    // Update is called once per frame
    void Update()
    {
        //nextBuildingSaved = GameObject.Find("speedController").GetComponent<speedController>().getSpawnRate();
        timer += Time.deltaTime;
        if (timer > nextBuilding)
        {
            Sprite spawnedBuilding = buildingList[Random.Range(0, buildingList.Length)];
            GameObject newBuilding = Instantiate(building, new Vector3(transform.position.x, transform.position.y + yPosOfBuilding, transform.position.z - depthOfBuilding), Quaternion.identity);
            SpriteRenderer spriterenderer = newBuilding.GetComponent<SpriteRenderer>();
            spriterenderer.sprite = spawnedBuilding;
            spriterenderer.color = new Color(spriterenderer.color.r * shadeOfBuilding, spriterenderer.color.g * shadeOfBuilding, spriterenderer.color.b * shadeOfBuilding, spriterenderer.color.a);
            newBuilding.GetComponent<BoxCollider2D>().enabled = enabledCollider;
            spriterenderer.sortingOrder = renderLayerNumber;
            newBuilding.GetComponent<building>().moveSpeed = buildingSpeed;
            //newBuilding.transform.Rotate(new Vector3(0, 1, 0));





            timer -= nextBuilding;
            nextBuildingSaved = timeTillNext(buildingSpeed);

            nextBuilding = Random.Range(nextBuildingSaved - 0.01f, nextBuildingSaved + 0.01f);
        }
    }
}

