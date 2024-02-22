using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class BoundingBoxScipt : MonoBehaviour
{
    public TextAsset jsonFile;
    public GameObject boundingBoxPrefab;
    public Canvas canvas;
    public VideoPlayer videoPlayer;
    public GameObject cube;

    private ParticleMovement particleMovement;

    private List<GameObject> currentBoundingBoxes = new List<GameObject>();
    //private float frameTimer = 0f;
    //private float frameDelay;
    //private int numberOfBboxes;

    [System.Serializable]
    public class BoundingBox
    {
        public float timestamp;
        public List<float> coordinates;
        public List<float> confidence;
        public List<float> class_id;
        public List<string> class_name;
    }

    [System.Serializable]
    public class BoundingBoxList
    {
        public List<BoundingBox> boundingBoxes;
    }

    private BoundingBoxList bboxList;

    public BoundingBoxList ReadJsonFile(TextAsset file)
    {
        string jsonToParse = "{\"boundingBoxes\":" + file.text + "}";
        return JsonUtility.FromJson<BoundingBoxList>(jsonToParse);
    }


    void DisplayBoundingBox(float currentTime)
    {
        bool foundRelevantBox = false;
        BoundingBox toDisplay = null;

        // Find the most recent bounding box up to the current time.
        foreach (var bbox in bboxList.boundingBoxes)
        {
            if (bbox.timestamp <= currentTime && (bbox.class_name.Count > 0 && bbox.class_name[0] == "person"))
            {
                toDisplay = bbox;
                foundRelevantBox = true; // Mark that we found a relevant box for this frame.
            }
            else if (bbox.timestamp > currentTime)
            {
                // Passed the relevant time period, stop checking further.
                break;
            }
        }

        // Clear previously displayed bounding boxes.
        foreach (var bbox in currentBoundingBoxes)
        {
            Destroy(bbox);
        }
        currentBoundingBoxes.Clear();

        if (foundRelevantBox && toDisplay != null)
        {
            //Debug.Log($"Video Time: {currentTime}, Bbox timestamp: {toDisplay.timestamp}");
            DisplaySingleBoundingBox(toDisplay);
        }
    }




    void DisplaySingleBoundingBox(BoundingBox bbox)
    {
        // Dimensions of the source video
        float sourceVideoWidth = 3840f;
        float sourceVideoHeight = 2160f;

        // Dimensions of the Canvas
        float canvasWidth = canvas.GetComponent<RectTransform>().rect.width;
        float canvasHeight = canvas.GetComponent<RectTransform>().rect.height;

        // Scale factors
        float scaleFactorX = canvasWidth / sourceVideoWidth;
        float scaleFactorY = canvasHeight / sourceVideoHeight;

        // Calculate the bounding box's canvas coordinates and size
        float xMin = bbox.coordinates[0] * scaleFactorX;
        float yMin = bbox.coordinates[1] * scaleFactorY;
        float xMax = bbox.coordinates[2] * scaleFactorX;
        float yMax = bbox.coordinates[3] * scaleFactorY;
        float width = xMax - xMin;
        float height = yMax - yMin;

        // Calculate the center of the bounding box in canvas coordinates
        float centerX = (xMin + xMax) / 2;
        float centerY = (yMin + yMax) / 2;



        //BOUNDING BOX
        /*
        GameObject bboxGameObject = Instantiate(boundingBoxPrefab, canvas.transform);
        
        float xMin = (bbox.coordinates[0] + 40) * scaleFactorX;
        float yMin = (bbox.coordinates[1] - 40) * scaleFactorY;
        float xMax = (bbox.coordinates[2] + 40) * scaleFactorX;
        float yMax = (bbox.coordinates[3] -40) * scaleFactorY;
        
        //convert yMin for Unity's UI system
        float convertedYMin = canvasHeight - (yMin + height); // Adjusted conversion

        RectTransform rectTransform = bboxGameObject.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = new Vector2(xMin, convertedYMin);
        rectTransform.sizeDelta = new Vector2(width, height);

        currentBoundingBoxes.Add(bboxGameObject);

        */

        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(centerX, canvasHeight - centerY, Camera.main.nearClipPlane + 100)); // Adjust the Z value as needed for visibility
        cube.transform.position = worldPosition;
        cube.transform.localScale = new Vector3(width / 100, height / 100, cube.transform.localScale.z); //må finne ut av hvorfor 100

        //particleSys.SetParticlePosition(centerX, centerY);

    }

    // Start is called before the first frame update
    void Start()
    {
        if (jsonFile != null)
        {
            bboxList = ReadJsonFile(jsonFile);
        }
        else
        {
            Debug.LogError("JSON file not assigned.");
        }

        if (videoPlayer != null)
        {
            videoPlayer.Play();
        }
        else
        {
            Debug.LogError("VideoPlayer not assigned.");
        }

        if (cube != null)
        {
            particleMovement = cube.GetComponent<ParticleMovement>();
        }
    }

    // Update is called once per frame
    private bool videoStarted = false;
    private float videoStartDelay = 0f; // This will hold the delay in video start

    void Update()
    {
        if (videoPlayer.isPlaying)
        {
            // Check if the video has "started" by having a currentTime greater than a small threshold
            if (!videoStarted && videoPlayer.time > 4.7f)
            {
                videoStarted = true;
                videoStartDelay = (float)videoPlayer.time; // Record the delay time
                particleMovement.particleObject.Play();
                particleMovement.particleCloud.Play();
            }

            if (videoStarted)
            {
                float adjustedCurrentTime = (float)videoPlayer.time - videoStartDelay; //adjust time based on initial delay

                // Clear existing bounding boxes
                foreach (var bbox in currentBoundingBoxes)
                {
                    Destroy(bbox);
                }
                currentBoundingBoxes.Clear();

                // Display the bounding box for the adjusted current time
                DisplayBoundingBox(adjustedCurrentTime);

                if (particleMovement != null)
                {
                    particleMovement.FindKeypoints(adjustedCurrentTime);
                }
                else
                {
                    Debug.Log("Error in bbox script");
                }
            }

        }

    }

}
