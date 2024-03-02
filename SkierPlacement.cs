using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;


//Script for extracting placement of skier from bounding box data and angle of skier from pose data

public class SkierPlacementScript : MonoBehaviour
{
    public TextAsset bboxJsonFile;
    public TextAsset poseJsonFile;
    public TextAsset motionJsonFile;
    public GameObject boundingBoxPrefab;
    public Canvas canvas;
    public VideoPlayer videoPlayer;
    public GameObject cube;

    private ParticleMovement particleMovement;

    private List<GameObject> currentBoundingBoxes = new List<GameObject>();

 
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

    [System.Serializable]
    public class Pose
    {
        public float timestamp;
        public List<float> keypoints;
    }

    [System.Serializable]
    public class PoseList
    {
        public List<Pose> poses;
    }

    [System.Serializable]
    public class MotionData
    {
        public float Time_s;
        public float Accel_X_g;
        public float Accel_Y_g;
        public float Accel_Z_g;
    }

    [System.Serializable]
    public class MotionDataList
    {
        public List<MotionData> motionData;
    }

    private BoundingBoxList bboxList;
    private PoseList poseList;
    private MotionDataList motionDataList;


    public BoundingBoxList ReadBboxFile(TextAsset file)
    {
        string jsonToParse = "{\"boundingBoxes\":" + file.text + "}";
        return JsonUtility.FromJson<BoundingBoxList>(jsonToParse);
    }

    public MotionDataList ReadMotionFile(TextAsset file)
    {
        string jsonToParse = "{\"motionData\":" + file.text + "}";
        return JsonUtility.FromJson<MotionDataList>(jsonToParse);
    }

    public PoseList ReadPoseFile(TextAsset file)
    {
        Debug.Log($"Raw JSON data: {file.text}"); //print json

        string jsonToParse = "{\"poses\":" + file.text + "}";
        return JsonUtility.FromJson<PoseList>(jsonToParse);
    }


    public enum DirectionState
    {
        Straight,
        HardLeft,
        SoftLeft,
        HardRight,
        SoftRight
    }

    private DirectionState currentSkierDirection = DirectionState.Straight;
    private float averageAngle;


    void FindBoundingBox(float currentTime)
    {
        bool foundRelevantBox = false;
        BoundingBox currentBbox = null;

        // Find the most recent bounding box up to the current time.
        foreach (var bbox in bboxList.boundingBoxes)
        {
            if (bbox.timestamp <= currentTime && (bbox.class_name.Count > 0 && bbox.class_name[0] == "person"))
            {
                currentBbox = bbox;
                foundRelevantBox = true; 
            }
            else if (bbox.timestamp > currentTime)
            {
                break;
            }
        }

        // Clear previously displayed bounding boxes.
        foreach (var bbox in currentBoundingBoxes)
        {
            Destroy(bbox);
        }
        currentBoundingBoxes.Clear();

        if (foundRelevantBox && currentBbox != null)
        {
            //Debug.Log($"Video Time: {currentTime}, Bbox timestamp: {toDisplay.timestamp}");
            ExtractBoundingBoxData(currentBbox);
        }
    }


    void ExtractBoundingBoxData(BoundingBox bbox)
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

    public void FindKeypoints(float currentTime)
    {
        Pose foundPose = null;
        float closestTimestamp = float.NegativeInfinity;

        foreach (var pose in poseList.poses)
        {
            if (pose.timestamp <= currentTime && pose.timestamp > closestTimestamp)
            {
                foundPose = pose;
                closestTimestamp = pose.timestamp;
            }
        }

        if (foundPose != null && foundPose.keypoints.Count >= 34)
        {
            Vector2 leftHip = new Vector2(foundPose.keypoints[11 * 2], foundPose.keypoints[11 * 2 + 1]);
            //Vector2 leftKnee = new Vector2(foundPose.keypoints[13 * 2], foundPose.keypoints[13 * 2 + 1]);
            Vector2 leftFoot = new Vector2(foundPose.keypoints[15 * 2], foundPose.keypoints[15 * 2 + 1]);

            Vector2 rightHip = new Vector2(foundPose.keypoints[12 * 2], foundPose.keypoints[12 * 2 + 1]);
            //Vector2 rightKnee = new Vector2(foundPose.keypoints[14 * 2], foundPose.keypoints[14 * 2 + 1]);
            Vector2 rightFoot = new Vector2(foundPose.keypoints[16 * 2], foundPose.keypoints[16 * 2 + 1]);

            float leftAngle = CalculateAngleWithRespectToVertical(leftHip, leftFoot);
            float rightAngle = CalculateAngleWithRespectToVertical(rightHip, rightFoot);
            averageAngle = (leftAngle + rightAngle) / 2;



            if (averageAngle >= -8 && averageAngle <= 8)
            {
                currentSkierDirection = DirectionState.Straight;
            }
            else if (averageAngle > 8 && averageAngle <= 20)
            {
                currentSkierDirection = DirectionState.SoftLeft;
            }
            else if (averageAngle < -8 && averageAngle >= -20)
            {
                currentSkierDirection = DirectionState.SoftRight;
            }
            else if (averageAngle > 20)
            {
                currentSkierDirection = DirectionState.HardLeft;
            }
            else if (averageAngle < -20)
            {
                currentSkierDirection = DirectionState.HardRight;
            }

        }
        else
        {
            Debug.LogWarning("Not enough keypoints to calculate angles.");
        }
    }


    public float CalculateAngleWithRespectToVertical(Vector2 hip, Vector2 foot)
    {
        Vector2 vectorHipToFoot = foot - hip;
        Vector2 verticalVector = new Vector2(0, 1); // y-axis

        vectorHipToFoot.Normalize();

        float dotProduct = Vector2.Dot(verticalVector, vectorHipToFoot);
        float angleRadians = Mathf.Acos(dotProduct);
        float angleDegrees = angleRadians * Mathf.Rad2Deg;

        if (angleDegrees > 80f)
        {
            angleDegrees = 80f;
        }

        if (hip.x > foot.x)
        {
            angleDegrees = -angleDegrees;
        }

        return angleDegrees;
    }

    private void EstimateDirectionFromMotion(float currentTime)
    {
        // Assuming motionDataList is already populated
        MotionData closestData = null;
        foreach (var data in motionDataList.motionData)
        {
            if (data.Time_s <= currentTime)
            {
                closestData = data; // This keeps updating until the last applicable timestamp
            }
            else
            {
                break; // Exit the loop once the current time has been exceeded
            }
        }

        if (closestData != null)
        {
            UpdateDirectionBasedOnMotion(closestData);
        }
    }

    private void UpdateDirectionBasedOnMotion(MotionData data)
    {
        // Example thresholds might need adjustment
        const float threshold = 0.1f; // Define a threshold for detecting a turn based on X acceleration

        if (data.Accel_X_g > threshold)
        {
            currentSkierDirection = DirectionState.SoftRight; // or HardRight depending on the magnitude
        }
        else if (data.Accel_X_g < -threshold)
        {
            currentSkierDirection = DirectionState.SoftLeft; // or HardLeft depending on the magnitude
        }
        else
        {
            currentSkierDirection = DirectionState.Straight;
        }
    }


    public DirectionState GetCurrentDirectionState()
    {
        return currentSkierDirection;
    }

    public float GetAverageAngle()
    {
        return averageAngle;
    }

    // Start is called before the first frame update
    void Start()
    {
        if (bboxJsonFile != null)
        {
            bboxList = ReadBboxFile(bboxJsonFile);
        }
        else
        {
            Debug.LogError("JSON file with bounding boxes not assigned.");
        }
        if (poseJsonFile != null)
        {
            poseList = ReadPoseFile(poseJsonFile);
            Debug.Log($"Loaded {poseList.poses.Count} poses from JSON.");
        }
        else
        {
            Debug.LogError("JSON file with poses not assigned.");
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

        averageAngle = 0f;

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

                FindBoundingBox(adjustedCurrentTime);
                FindKeypoints(adjustedCurrentTime);
                EstimateDirectionFromMotion(adjustedCurrentTime);

                /*
                if (particleMovement != null)
                {
                    particleMovement.FindKeypoints(adjustedCurrentTime);
                }
                else
                {
                    Debug.Log("ParticleMovement not assigned");
                }
                */
            }

        }

    }

}
