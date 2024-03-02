using UnityEngine;
using System.Collections.Generic;
using System;



public class ParticleMovement : MonoBehaviour
{
    public SkierPlacementScript placementScript;
    public ParticleSystem particleObject;
    public ParticleSystem particleCloud;
    //public TextAsset jsonFile;

    private float baseEmissionRate = 1000f;
    private float cloudEmissionRate = 50f;

    private float cubeWidth;
    private float cubeHeight;

    Gradient targetColorGradient;
    float colorTransitionSpeed = 1.0f; 
    float colorTransitionProgress = 0.0f;

    private float averageAngle;
    private SkierPlacementScript.DirectionState currentDirectionState;


    void AdjustParticlePlacement()
    {
        Vector3 bottomCenterRel = new Vector3(0, -cubeHeight/10, -2);
        Vector3 bottomLeftRel = new Vector3(-cubeWidth/10, -cubeHeight/10, -2);
        Vector3 bottomRightRel = new Vector3(cubeWidth/10, -cubeHeight/10, -2);


        switch (currentDirectionState)
        {
            case SkierPlacementScript.DirectionState.Straight:
                particleObject.transform.localPosition = bottomCenterRel;
                particleCloud.transform.localPosition = bottomCenterRel + new Vector3(0, 0, 0);
                break;
            case SkierPlacementScript.DirectionState.SoftLeft:
            case SkierPlacementScript.DirectionState.HardLeft:
                particleObject.transform.localPosition = bottomRightRel;
                particleCloud.transform.localPosition = bottomRightRel + new Vector3(0, 0, 0);
                break;
            case SkierPlacementScript.DirectionState.SoftRight:
            case SkierPlacementScript.DirectionState.HardRight:
                particleObject.transform.localPosition = bottomLeftRel;
                particleCloud.transform.localPosition = bottomLeftRel + new Vector3(0, 0, 0);
                break;
        }
    }


    void AdjustParticleDirectionAndSize()
    {
        Vector2 flowDirection = Vector2.up;

        var cloudShape = particleCloud.shape;
        var shape = particleObject.shape;


        switch (currentDirectionState)
        {
            case SkierPlacementScript.DirectionState.Straight:
                break;
            case SkierPlacementScript.DirectionState.SoftLeft:
                flowDirection = (Vector2.up + Vector2.left).normalized;
                cloudShape.rotation = new Vector3(0, 40, 0);
                shape.rotation = new Vector3(0, 40, 0);
                break;
            case SkierPlacementScript.DirectionState.SoftRight:
                flowDirection = (Vector2.up + Vector2.right).normalized;
                cloudShape.rotation = new Vector3(0, -40, 0);
                shape.rotation = new Vector3(0, -40, 0);
                break;
            case SkierPlacementScript.DirectionState.HardLeft:
                flowDirection = Vector2.left;
                cloudShape.rotation = new Vector3(0, 40, 0);
                shape.rotation = new Vector3(0, 40, 0);
                break;
            case SkierPlacementScript.DirectionState.HardRight:
                flowDirection = Vector2.right;
                cloudShape.rotation = new Vector3(0, -40, 0);
                shape.rotation = new Vector3(0, -40, 0);
                break;
        }


        flowDirection.Normalize();

        var main = particleObject.main;
        main.startSize = cubeHeight/9;

        var cloudMain = particleCloud.main;
        //cloudMain.startSize = cubeHeight * 4f;
        


        //shape.scale = new Vector3(cubeWidth / 2, cubeHeight / 8, 0);
        if (cubeHeight < 1.8f)
        {
            shape.radius = cubeHeight / 2;
            cloudShape.radius = cubeHeight / 3;
            //cloudMain.startSize = cubeHeight * 5f;
            //cloudMain.startLifetime = cubeHeight * 10f;
        }
        else
        {
            shape.radius = 0.9f;
            cloudShape.radius = 1.1f;
            //cloudMain.startSize = 7;
            //cloudMain.startLifetime = 5f;
        }
        
        shape.length = 7 * shape.radius;
        //shape.angle = 30 * cubeHeight;
        //shape.rotation = new Vector3(0, averageAngle, 0);

        var emission = particleObject.emission;
        emission.rateOverTime = flowDirection.magnitude * baseEmissionRate;

        
       /*
        var cloudEmission = particleCloud.emission;

        if (Math.Abs(averageAngle) < 0)
        {
            cloudEmission.rateOverTime = 10f;
        }
        else
        {
            cloudEmission.rateOverTime = cloudEmissionRate;
        }
        */

        /*28.02

        //cloudMain.startSize = cubeHeight;
        cloudMain.startSize = new ParticleSystem.MinMaxCurve(cubeHeight * 10f, cubeHeight * 14f);
        cloudMain.startLifetime = new ParticleSystem.MinMaxCurve(1f, 2f);
        cloudMain.startColor = new Color(1f, 1f, 1f, 0.5f);
        */

        /*
        var cloudColor = particleCloud.colorOverLifetime;
        cloudColor.enabled = true;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.white, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.3f, 0.0f), new GradientAlphaKey(0f, 1.0f) } // Adjust alpha for transparency
        );

        cloudColor.color = new ParticleSystem.MinMaxGradient(gradient);

        */

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
            case SkierPlacementScript.DirectionState.Straight:
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
            case SkierPlacementScript.DirectionState.SoftLeft:
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
            case SkierPlacementScript.DirectionState.HardLeft:
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
            case SkierPlacementScript.DirectionState.SoftRight:
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
            case SkierPlacementScript.DirectionState.HardRight:
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


    }

    
    Gradient InterpolateGradients(Gradient from, Gradient to, float progress)
    {
        // Implement interpolation logic here. Use Gradient.Lerp
        //
        Gradient gradient = new Gradient();

        return gradient;
    }

    Gradient GetCurrentGradient(ParticleSystem.ColorOverLifetimeModule colorOverTime)
    {
        // Extract the current gradient from colorOverTime
        Gradient gradient = new Gradient();

        return gradient;
    }
    


    void Start()
    {
        

        var shape = particleObject.shape;
        shape.radius = 0.006f;
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

        currentDirectionState = placementScript.GetCurrentDirectionState();
        averageAngle = placementScript.GetAverageAngle();

        Debug.Log($"Current angle is: {averageAngle}");
        /*
        var colorOverTime = particleObject.colorOverLifetime;

        if (colorTransitionProgress < 1.0f)
        {
            Gradient currentGradient = GetCurrentGradient(colorOverTime);
            Gradient newGradient = InterpolateGradients(currentGradient, targetColorGradient, colorTransitionProgress);

            colorOverTime.color = new ParticleSystem.MinMaxGradient(newGradient);

            colorTransitionProgress += Time.deltaTime * colorTransitionSpeed;
        }
        */

        //ChangeParticleColor();
    }


}
