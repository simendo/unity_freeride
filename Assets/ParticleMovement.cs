using UnityEngine;
using System.Collections.Generic;

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


public class ParticleMovement : MonoBehaviour
{
    public ParticleSystem particleObject;
    public ParticleSystem particleCloud;
    public TextAsset jsonFile;

    private float baseEmissionRate = 650f;
    private float cloudEmissionRate = 100f;

    private float cubeWidth;
    private float cubeHeight;

    Gradient targetColorGradient;
    float colorTransitionSpeed = 1.0f; 
    float colorTransitionProgress = 0.0f;


    enum DirectionState 
    {
        Straight,
        HardLeft,
        SoftLeft,
        HardRight,
        SoftRight
    }
   
    private DirectionState currentDirectionState = DirectionState.Straight;

    
    /*
    private bool straight;
    private bool softLeft;
    private bool softRight;
    private bool hardLeft;
    private bool hardRight;
    private bool turningLeft;
    private bool turningRight;
    */

    float averageAngle;

    private PoseList poseList;


    public PoseList ReadJsonFile(TextAsset file)
    {
        Debug.Log($"Raw JSON data: {file.text}"); //print json

        string jsonToParse = "{\"poses\":" + file.text + "}";
        return JsonUtility.FromJson<PoseList>(jsonToParse);
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

            /*
            straight = softLeft = softRight = hardLeft = hardRight = false;

            if (averageAngle >= -8 && averageAngle <= 8)
            {
                straight = true;
            }
            else if (averageAngle > 8 && averageAngle <= 20)
            {
                softLeft = true;
            }
            else if (averageAngle < -8 && averageAngle >= -20)
            {
                softRight = true;
            }
            else if (averageAngle > 20)
            {
                hardLeft = true;
            }
            else if (averageAngle < -20)
            {
                hardRight = true;
            }
            */


            if (averageAngle >= -8 && averageAngle <= 8)
            {
                currentDirectionState = DirectionState.Straight;
            }
            else if (averageAngle > 8 && averageAngle <= 20)
            {
                currentDirectionState = DirectionState.SoftLeft;
            }
            else if (averageAngle < -8 && averageAngle >= -20)
            {
                currentDirectionState = DirectionState.SoftRight;
            }
            else if (averageAngle > 20)
            {
                currentDirectionState = DirectionState.HardLeft;
            }
            else if (averageAngle < -20)
            {
                currentDirectionState = DirectionState.HardRight;
            }

        }
        else
        {
            Debug.LogWarning("Not enough keypoints to calculate angles.");
        }

        
    }


    float CalculateAngleWithRespectToVertical(Vector2 hip, Vector2 foot)
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


    void AdjustParticlePlacement()
    {
        Vector3 bottomCenterRel = new Vector3(0, -cubeHeight/10, -15);
        Vector3 bottomLeftRel = new Vector3(-cubeWidth/10, -cubeHeight/10, -15);
        Vector3 bottomRightRel = new Vector3(cubeWidth/10, -cubeHeight/10, -15);

        /*
        
        if (turningLeft)
        {
            particleObject.transform.localPosition = bottomRightRel;
            particleCloud.transform.localPosition = bottomRightRel + new Vector3(0, 0, 10);
        }
        else if (turningRight)
        {
            particleObject.transform.localPosition = bottomLeftRel;
            particleCloud.transform.localPosition = bottomLeftRel + new Vector3(0,0,10);
        }
        else
        {
            particleObject.transform.localPosition = bottomCenterRel;
            particleCloud.transform.localPosition = bottomCenterRel + new Vector3(0, 0, 10);
        }
        */

        switch (currentDirectionState)
        {
            case DirectionState.Straight:
                particleObject.transform.localPosition = bottomCenterRel;
                particleCloud.transform.localPosition = bottomCenterRel + new Vector3(0, 0, 10);
                break;
            case DirectionState.SoftLeft:
            case DirectionState.HardLeft:
                particleObject.transform.localPosition = bottomRightRel;
                particleCloud.transform.localPosition = bottomRightRel + new Vector3(0, 0, 10);
                break;
            case DirectionState.SoftRight:
            case DirectionState.HardRight:
                particleObject.transform.localPosition = bottomLeftRel;
                particleCloud.transform.localPosition = bottomLeftRel + new Vector3(0, 0, 10);
                break;
        }
    }


    void AdjustParticleDirectionAndSize()
    {
        Vector2 flowDirection = Vector2.up; 

        /*
        if (hardLeft)
        {
            flowDirection = Vector2.left; 
        }
        else if (softLeft)
        {
            flowDirection = (Vector2.up + Vector2.left).normalized; 
        }
        else if (hardRight)
        {
            flowDirection = Vector2.right; 
        }
        else if (softRight)
        {
            flowDirection = (Vector2.up + Vector2.right).normalized; 
        }
        */

        switch (currentDirectionState)
        {
            case DirectionState.Straight:
                break;
            case DirectionState.SoftLeft:
                flowDirection = (Vector2.up + Vector2.left).normalized;
                break;
            case DirectionState.SoftRight:
                flowDirection = (Vector2.up + Vector2.right).normalized;
                break;
            case DirectionState.HardLeft:
                flowDirection = Vector2.left;
                break;
            case DirectionState.HardRight:
                flowDirection = Vector2.right;
                break;
        }


        flowDirection.Normalize();

        var main = particleObject.main;
        main.startSize = cubeHeight/5;

        var cloudMain = particleCloud.main;
        cloudMain.startSize = cubeHeight/2;

        var shape = particleObject.shape;
        shape.scale = new Vector3(cubeWidth / 2, cubeHeight / 8, 0);
        shape.rotation = new Vector3(0, averageAngle, 0);

        var cloudShape = particleCloud.shape;
        //cloudShape.scale = new Vector3(cubeWidth / 4, cubeHeight / 4, 0);
        cloudShape.radius = cubeHeight/25;
        //cloudShape.length = 2;
        cloudShape.angle = 2*cubeHeight;
        cloudShape.rotation = new Vector3(0, averageAngle, 0);

        var emission = particleObject.emission;
        emission.rateOverTime = flowDirection.magnitude * baseEmissionRate;
       
        var cloudEmission = particleCloud.emission;
        cloudEmission.rateOverTime = flowDirection.magnitude * cloudEmissionRate;
    }


    void ChangeParticleColor()
    {
        var colorOverTime = particleObject.colorOverLifetime;
        colorOverTime.enabled = true;

        var main = particleObject.main;
        var currentColor = main.startColor; 

        Gradient gradient = new Gradient();

        switch (currentDirectionState)
        {
            case DirectionState.Straight:
                gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.white, 0.0f),
                    new GradientColorKey(Color.white, 0.5f), 
                    new GradientColorKey(Color.green, 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1.0f, 0.0f),
                    new GradientAlphaKey(1.0f, 1.0f)
                }
            );
                break;
            case DirectionState.SoftLeft:
                gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.white, 0.0f),
                    new GradientColorKey(Color.white, 0.5f), 
                    new GradientColorKey(Color.green, 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1.0f, 0.0f),
                    new GradientAlphaKey(1.0f, 1.0f)
                }
            );
                break;
            case DirectionState.HardLeft:
                gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.white, 0.0f),
                    new GradientColorKey(Color.white, 0.5f), 
                    new GradientColorKey(Color.green, 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1.0f, 0.0f),
                    new GradientAlphaKey(1.0f, 1.0f)
                }
            );
                break;
            case DirectionState.SoftRight:
                gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.white, 0.0f),
                    new GradientColorKey(Color.white, 0.5f), 
                    new GradientColorKey(Color.green, 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1.0f, 0.0f),
                    new GradientAlphaKey(1.0f, 1.0f)
                }
            );
                break;
            case DirectionState.HardRight:
                gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.white, 0.0f),
                    new GradientColorKey(Color.white, 0.5f), 
                    new GradientColorKey(Color.green, 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1.0f, 0.0f),
                    new GradientAlphaKey(1.0f, 1.0f)
                }
            );
                break;
        }

        colorOverTime.color = gradient;

        /*
        var main = particleObject.main;
        if (turningLeft)
        {
            main.startColor = new Color(0, 1, 0, 1);
        }
        else
        {
            main.startColor = new Color(1, 0, 0, 1);
        }
        */
    }


    Gradient InterpolateGradients(Gradient from, Gradient to, float progress)
    {
        // Implement interpolation logic here. This is a complex task requiring
        // interpolation of each key in the gradients. You might use Gradient.Lerp if available
        // or manually interpolate the color and alpha keys.
        Gradient gradient = new Gradient();

        return gradient;
    }

    Gradient GetCurrentGradient(ParticleSystem.ColorOverLifetimeModule colorOverTime)
    {
        // Extract the current gradient from colorOverTime. This might be straightforward if
        // the color is already a gradient, or require creating a new gradient from a solid color.
        Gradient gradient = new Gradient();

        return gradient;
    }



    void Start()
    {
        if (jsonFile != null)
        {
            poseList = ReadJsonFile(jsonFile);
            Debug.Log($"Loaded {poseList.poses.Count} poses from JSON.");
        }
        else
        {
            Debug.LogError("JSON file not assigned.");
        }

        var shape = particleObject.shape;
        shape.radius = 0.06f;
        //shape.length = 2;

        var cloudShape = particleCloud.shape;
        cloudShape.radius = 0.06f;
        //cloudShape.length = 2;
        
    }



    void Update()
    {
        cubeWidth = transform.localScale.x;
        cubeHeight = transform.localScale.y;

        AdjustParticlePlacement();
        AdjustParticleDirectionAndSize();


        var colorOverTime = particleObject.colorOverLifetime;

        if (colorTransitionProgress < 1.0f)
        {
            Gradient currentGradient = GetCurrentGradient(colorOverTime);
            Gradient newGradient = InterpolateGradients(currentGradient, targetColorGradient, colorTransitionProgress);

            colorOverTime.color = new ParticleSystem.MinMaxGradient(newGradient);

            colorTransitionProgress += Time.deltaTime * colorTransitionSpeed;
        }


        ChangeParticleColor();
    }


}
