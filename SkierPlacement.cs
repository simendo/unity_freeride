using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using System;


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
    public bool staticMode;

    private ParticleMovement particleMovement;
    //private List<GameObject> currentBoundingBoxes = new List<GameObject>();
    private Color currentColor;
    private bool videoStarted = false;
    private float videoStartDelay = 0f;
    private bool particleSystemsStarted = false;
    private float lastDetectionTime = 0f;


    [System.Serializable]
    public class BoundingBox
    {
        public float timestamp;
        public List<float> coordinates;
        public List<float> confidence;
        public List<float> class_id;
        public List<string> class_name;
        public List<string> colors;
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
        public float timestamp;
        public float accel_X;
        public float accel_Y;
        public float accel_Z;
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
        //Debug.Log($"Raw JSON motion data: {file.text}");
        string jsonToParse = "{\"motionData\":" + file.text + "}";
        return JsonUtility.FromJson<MotionDataList>(jsonToParse);
    }

    public PoseList ReadPoseFile(TextAsset file)
    {
        //Debug.Log($"Raw JSON data: {file.text}"); 

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

    // Dimensions of the source video
    private float sourceVideoWidth = 3840f;
    private float sourceVideoHeight = 2160f;


    bool FindBoundingBox(float currentTime)
    {
        bool foundRelevantBox = false;
        BoundingBox currentBbox = null;

        // Find the most recent bounding box up to the current time
        foreach (var bbox in bboxList.boundingBoxes)
        {
            if (bbox.timestamp <= currentTime && (bbox.class_name.Count > 0 && (bbox.class_name[0] == "person" || bbox.class_name[0] == "Skier")))
            {
                currentBbox = bbox;
                foundRelevantBox = true;
                
                //break; // Stop the loop once the relevant box is found
            }
            else if (bbox.timestamp > currentTime)
            {
                //Debug.Log($"Box not valid at video Time: {currentTime}, Bbox timestamp: {bbox.timestamp}, Bbox coordinates: {bbox.coordinates}");
                break;
            }
        }
        if (foundRelevantBox && currentBbox != null)
        {
            ExtractBoundingBoxData(currentBbox);
        }
        // Update position based on the found bounding box
        return foundRelevantBox;
    }


    void ExtractBoundingBoxData(BoundingBox bbox)
    {

        // Dimensions of the Canvas
        float canvasWidth = canvas.GetComponent<RectTransform>().rect.width;
        float canvasHeight = canvas.GetComponent<RectTransform>().rect.height;

        // Scale factors
        float scaleFactorX = canvasWidth / sourceVideoWidth;
        float scaleFactorY = canvasHeight / sourceVideoHeight;

        // Calculate the bounding box's canvas coordinates and size
        if (bbox.coordinates.Count > 0)
        {
            float xMin = bbox.coordinates[0] * scaleFactorX;
            float yMin = bbox.coordinates[1] * scaleFactorY;
            float xMax = bbox.coordinates[2] * scaleFactorX;
            float yMax = bbox.coordinates[3] * scaleFactorY;
            float width = xMax - xMin;
            float height = yMax - yMin;

            // Calculate the center of the bounding box in canvas coordinates
            float centerX = (xMin + xMax) / 2;
            float centerY = (yMin + yMax) / 2;



            //DRAW BOUNDING BOX
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

        if (bbox.colors.Count == 3) 
        {
            float r = float.Parse(bbox.colors[0]) / 255.0f; 
            float g = float.Parse(bbox.colors[1]) / 255.0f;
            float b = float.Parse(bbox.colors[2]) / 255.0f;
            Color color = new Color(r, g, b);

            if (r != currentColor.r && g != currentColor.g && b != currentColor.b)
            {
                OnColorChanged?.Invoke(color);
                currentColor = color;
            }   
        }

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


    private void FindMotionData(float currentTime)
    {
        MotionData closestData = null;
        //float minTimeDiff = float.MaxValue;
        //float closestTimestamp = float.NegativeInfinity;
        bool foundRelevantData = false;

  

        foreach (var data in motionDataList.motionData)
        {
            if (data.timestamp <= currentTime)
            {
                closestData = data;
                foundRelevantData = true;
            }
            else if (data.timestamp > currentTime)
            {
                break;
            }
        }

        /*
        // Clear previously displayed bounding boxes.
        foreach (var data in currentMotionData)
        {
            Destroy(data);
        }
        currentMotionData.Clear();
        */

        if (foundRelevantData && closestData != null)
        {
            //Debug.Log($"Video Time: {currentTime}, Motion timestamp: {closestData.timestamp}, AccelX: {closestData.accel_X}");
            EstimateDirectionFromMotion(closestData, currentTime);
        }
    }


    private void EstimateDirectionFromMotion(MotionData mdata, float time)
    {
        DirectionState previousDirection = currentSkierDirection;
        if (mdata != null)
        {
            
            const float softThreshold = 0.05f; 
            const float hardThreshold = 0.15f; 

           
            float accel_X_abs = Math.Abs(mdata.accel_X);
            /*
            if (accel_X_abs > softThreshold)
            {
                if (accel_X_abs > hardThreshold)
                {
                    currentSkierDirection = (mdata.accel_X > 0) ? DirectionState.HardRight : DirectionState.HardLeft;
                }
                else
                {
                    currentSkierDirection = (mdata.accel_X > 0) ? DirectionState.SoftRight : DirectionState.SoftLeft;
                }
            }
            else
            {
                currentSkierDirection = DirectionState.Straight;
            }
            */
            //currentSkierDirection = DirectionState.HardLeft;
        }
    }




    public DirectionState GetCurrentDirectionState()
    {
        if (staticMode)
        {
            return DirectionState.HardRight;
        }
        return currentSkierDirection;
    }

    public float GetAverageAngle()
    {
        return averageAngle;
    }

    string ListToString<T>(List<T> list)
    {
        return "[" + String.Join(", ", list) + "]";
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
        }
        else
        {
            Debug.LogError("JSON file with poses not assigned.");
        }

        if (motionJsonFile != null)
        {
            motionDataList = ReadMotionFile(motionJsonFile);
        }
        else
        {
            Debug.LogError("JSON file with motion data not assigned.");
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

        currentColor = new Color(0, 0, 0);

    }

    public delegate void ColorChanged(Color color);
    public static event ColorChanged OnColorChanged;
    

    // Update is called once per frame
    void Update()
    {
        if (videoPlayer.isPlaying)
        {
            float currentTime = (float)videoPlayer.time;
            // Check if the video has "started" by having a currentTime greater than a small threshold
            if (!videoStarted && videoPlayer.time > 4.7f)
            {
                videoStarted = true;
                videoStartDelay = currentTime; // Record the delay time
            }

            if (videoStarted)
            {
                float adjustedCurrentTime = currentTime - videoStartDelay; //adjust time based on initial delay

                bool foundDetection = FindBoundingBox(adjustedCurrentTime);

                if (foundDetection)
                {
                    lastDetectionTime = currentTime;
                    if (!particleSystemsStarted)
                    {
                        particleMovement.particleObject.Play();
                        particleMovement.particleCloud.Play();
                        particleMovement.particleBurst.Play();
                        particleSystemsStarted = true;
                        Debug.Log("Particle System is started");
                    }
                }

                // Stop particle systems if no detection in the last 1.0 s
                if (currentTime - lastDetectionTime > 1.0f && particleSystemsStarted)
                {
                    particleMovement.particleObject.Stop();
                    particleMovement.particleCloud.Stop();
                    particleMovement.particleBurst.Stop();
                    particleSystemsStarted = false;
                }

                //FindBoundingBox(adjustedCurrentTime);
                //FindKeypoints(adjustedCurrentTime);
                //FindMotionData(adjustedCurrentTime);

                
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
