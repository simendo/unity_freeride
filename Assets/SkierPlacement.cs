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
    public bool useKeypoints;
    public delegate void ColorChanged(Color color);
    public static event ColorChanged OnColorChanged;

    private ParticleMovement particleMovement;
    private Color currentColor;
    private bool videoStarted = false;
    private float videoStartDelay = 0f;
    private bool particleSystemsStarted = false;
    private float lastDetectionTime = 0f;

    //Classes for data from pose and detection models
    [System.Serializable]
    public class BoundingBox
    {
        public float timestamp;
        public List<float> coordinates;
        public List<float> confidence;
        public List<float> class_id;
        public List<string> class_name;
        public List<string> color;
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

    //Motion data is used in the test and development of the Unity program 
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
        string jsonToParse = "{\"motionData\":" + file.text + "}";
        return JsonUtility.FromJson<MotionDataList>(jsonToParse);
    }

    public PoseList ReadPoseFile(TextAsset file)
    {
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
    //private float sourceVideoWidth = 3840f; 
    //private float sourceVideoHeight = 2160f;
    private float sourceVideoWidth = 1920f;
    private float sourceVideoHeight = 1080f;


    bool FindBoundingBox(float currentTime)
    {
        bool foundRelevantBox = false;
        BoundingBox currentSkier = null;
        //BoundingBox currentSki = null;

        foreach (var bbox in bboxList.boundingBoxes)
        {
            if (bbox.timestamp > currentTime) break;

            if (bbox.class_name.Contains("Skier") || bbox.class_name.Contains("person"))
            {
                currentSkier = bbox;
                foundRelevantBox = true;
            }

            //The detection of "Ski" is not utilized
            /*
            else if (bbox.class_name.Contains("Ski"))
            {
                currentSki = bbox;
                foundRelevantBox = true;  
            }
            */
        }

        if (currentSkier != null)
        {
            ExtractBoundingBoxData(currentSkier);
        }


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

            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(centerX, canvasHeight - centerY, Camera.main.nearClipPlane + 100));
            cube.transform.position = worldPosition;
            cube.transform.localScale = new Vector3(width / 100, height / 100, cube.transform.localScale.z); //må finne ut av hvorfor 100

        }

        if (bbox.color.Count == 3) 
        {
            float r = float.Parse(bbox.color[0]) / 255.0f; 
            float g = float.Parse(bbox.color[1]) / 255.0f;
            float b = float.Parse(bbox.color[2]) / 255.0f;
            Color color = new Color(r, g, b);

            if (r != currentColor.r && g != currentColor.g && b != currentColor.b)
            {
                OnColorChanged?.Invoke(color);
                Debug.Log("Color change!");
                currentColor = color;
            }   
        }
    }

    //Method for utilizing the detection of skis
    /*
    void ExtractSkiData(BoundingBox skiBox)
    {
        float canvasWidth = canvas.GetComponent<RectTransform>().rect.width;
        float canvasHeight = canvas.GetComponent<RectTransform>().rect.height;

        // Scale factors
        float scaleFactorX = canvasWidth / sourceVideoWidth;
        float scaleFactorY = canvasHeight / sourceVideoHeight;

        // Calculate the bounding box's canvas coordinates and size
        if (skiBox.coordinates.Count > 0)
        {
            float xMin = skiBox.coordinates[0] * scaleFactorX;
            float yMin = skiBox.coordinates[1] * scaleFactorY;
            float xMax = skiBox.coordinates[2] * scaleFactorX;
            float yMax = skiBox.coordinates[3] * scaleFactorY;
            float width = xMax - xMin;
            float height = yMax - yMin;

            // Calculate the center of the bounding box in canvas coordinates
            float centerX = (xMin + xMax) / 2;
            float centerY = (yMin + yMax) / 2;

            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(centerX, canvasHeight - centerY, Camera.main.nearClipPlane + 100));
            particleMovement.SetSkiParticlePosition(worldPosition);

        }
    }
    */

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
            else if (averageAngle > 40)
            {
                currentSkierDirection = DirectionState.HardLeft;
            }
            else if (averageAngle < -20)
            {
                currentSkierDirection = DirectionState.HardRight;
            }
            Debug.Log($"Timestamp: {currentTime}, Current angle: {averageAngle}");

        }
        else
        {
            Debug.LogWarning($"Not enough keypoints to calculate angles. Number of keypoints {foundPose.keypoints.Count}");
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


        if (foundRelevantData && closestData != null)
        {
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

    // Start all relevant particle systems based on the detection state
    void StartParticleSystems()
    {
        if (staticMode)
        {
            particleMovement.particleStatic.Play();
            particleMovement.particleCloud.Play();
            particleMovement.particleObject.Play(); 
        }
        else
        {
            particleMovement.particleObject.Play();
            particleMovement.particleCloud.Play();
            //particleMovement.particleBurst.Play();
        }
    }

    // Stop all particle systems
    void StopParticleSystems()
    {
        particleMovement.particleObject.Stop();
        particleMovement.particleCloud.Stop();
        particleMovement.particleBurst.Stop();
        particleMovement.particleStatic.Stop();
    }



    public DirectionState GetCurrentDirectionState()
    {
        if (staticMode) //Only correct for current "static" video recording
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


    // Update is called once per frame
    void Update()
    {
        if (videoPlayer.isPlaying)
        {
            float currentTime = (float)videoPlayer.time;

            // Check if the video has "started" by having a currentTime greater than a small threshold
            if (!videoStarted && videoPlayer.time > 5.7f) //changed from 4.7f
            {
                videoStarted = true;
                videoStartDelay = currentTime; // Record the delay time
            }

            if (videoStarted)
            {
                float adjustedCurrentTime = currentTime - videoStartDelay; 

                // Detect bounding boxes and skis
                bool foundDetection = FindBoundingBox(adjustedCurrentTime);

                // Start particle systems upon first detection
                if (foundDetection && !particleSystemsStarted)
                {
                    lastDetectionTime = currentTime;
                    StartParticleSystems();
                    particleSystemsStarted = true;
                }

                // Stop particle systems if no detection in the last 1.0 s
                if (currentTime - lastDetectionTime > 1.0f && particleSystemsStarted)
                {
                    StopParticleSystems();
                    particleSystemsStarted = false;
                }

                //Optionally process keypoints if required and available
                if (useKeypoints)
                {
                    FindKeypoints(adjustedCurrentTime);
                }
            }
        }
    }

}
