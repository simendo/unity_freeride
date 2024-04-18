using UnityEngine;
using System.Collections.Generic;
using System;


public class ParticleMovement : MonoBehaviour
{
    public SkierPlacementScript placementScript;
    public ParticleSystem particleObject;
    public ParticleSystem particleCloud;
    public ParticleSystem particleBurst;

    public bool burstMode;
    
    //public TextAsset jsonFile;

    private float baseEmissionRate = 1000f;
    private float cloudEmissionRate = 50f;

    private float cubeWidth;
    private float cubeHeight;
    private float originalCloudHeight = 0.2f;

    private float averageAngle;
    private SkierPlacementScript.DirectionState currentDirectionState;
    private SkierPlacementScript.DirectionState previousDirectionState;


    void AdjustParticlePlacement()
    {
        Vector3 bottomCenterRel = new Vector3(0, -cubeHeight/6, -2);
        Vector3 bottomLeftRel = new Vector3(-cubeWidth/4, -cubeHeight/6, -2);
        Vector3 bottomRightRel = new Vector3(cubeWidth/4, -cubeHeight/6, -2);

        if (placementScript.staticMode)
        {
            particleObject.transform.localPosition = bottomLeftRel;
        }
        else
        {
            switch (currentDirectionState)
            {
                case SkierPlacementScript.DirectionState.Straight:
                    particleObject.transform.localPosition = bottomCenterRel;
                    particleCloud.transform.localPosition = bottomCenterRel + new Vector3(0, 0, 0);
                    particleBurst.transform.localPosition = bottomCenterRel + new Vector3(0, 0, 0);
                    break;
                case SkierPlacementScript.DirectionState.SoftLeft:
                case SkierPlacementScript.DirectionState.HardLeft:
                    particleObject.transform.localPosition = bottomRightRel; 
                    particleCloud.transform.localPosition = bottomRightRel + new Vector3(0, 0, 0);
                    particleBurst.transform.localPosition = bottomRightRel + new Vector3(0, 0, 0);
                    break;
                case SkierPlacementScript.DirectionState.SoftRight:
                case SkierPlacementScript.DirectionState.HardRight:
                    particleObject.transform.localPosition = bottomLeftRel;
                    particleCloud.transform.localPosition = bottomLeftRel + new Vector3(0, 0, 0);
                    particleBurst.transform.localPosition = bottomLeftRel + new Vector3(0, 0, 0);
                    break;
            }
        }
    }


    void AdjustParticleDirectionAndSize()
    {
        Vector2 flowDirection = Vector2.up;

        var cloudShape = particleCloud.shape;
        var shape = particleObject.shape;
        var burstShape = particleBurst.shape;
        var cloudMain = particleCloud.main;
        var main = particleObject.main;
        var burstMain = particleBurst.main;


        switch (currentDirectionState)
        {
            case SkierPlacementScript.DirectionState.Straight:
                main.startSpeed = UnityEngine.Random.Range(2, 3);
                cloudMain.startSpeed = UnityEngine.Random.Range(0, 1);
                burstMain.startSize = 0.01f;
                break;
            case SkierPlacementScript.DirectionState.SoftLeft:
                flowDirection = (Vector2.up + Vector2.left).normalized;
                cloudShape.rotation = new Vector3(0, -40, 0);
                shape.rotation = new Vector3(0, -40, 0);
                burstShape.rotation = new Vector3(0, -40, 0);
                main.startSpeed = UnityEngine.Random.Range(7, 8);
                cloudMain.startSpeed = UnityEngine.Random.Range(2, 3);
                break;
            case SkierPlacementScript.DirectionState.SoftRight:
                flowDirection = (Vector2.up + Vector2.right).normalized;
                cloudShape.rotation = new Vector3(0, 40, 0);
                shape.rotation = new Vector3(0, 40, 0);
                burstShape.rotation = new Vector3(0, 40, 0);
                main.startSpeed = UnityEngine.Random.Range(7, 8);
                cloudMain.startSpeed = UnityEngine.Random.Range(2, 3);
                break;
            case SkierPlacementScript.DirectionState.HardLeft:
                flowDirection = Vector2.left;
                cloudShape.rotation = new Vector3(0, -60, 0);
                shape.rotation = new Vector3(0, -60, 0);
                burstShape.rotation = new Vector3(0, -60, 0);
                main.startSpeed = UnityEngine.Random.Range(10, 12);
                cloudMain.startSpeed = UnityEngine.Random.Range(4, 5);
                break;
            case SkierPlacementScript.DirectionState.HardRight:
                flowDirection = Vector2.right;
                cloudShape.rotation = new Vector3(0, 60, 0);
                shape.rotation = new Vector3(0, 60, 0);
                burstShape.rotation = new Vector3(0, 60, 0);
                main.startSpeed = UnityEngine.Random.Range(10, 12);
                cloudMain.startSpeed = UnityEngine.Random.Range(4, 5);
                break;
        }


        flowDirection.Normalize();

        var cloudEmission = particleCloud.emission;

        //shape.scale = new Vector3(cubeWidth / 2, cubeHeight / 8, 0);
        if (cubeHeight == 1.00f)
        {
            cloudMain.startSize = 0.1f;
            cloudShape.radius = 0.01f;
            //cloudShape.scale = new Vector3(0.01f, 0.01f, 0.01f);
        }
        else if (cubeHeight < 1.8f)
        {
            shape.radius = cubeHeight / 6;
            //cloudShape.radius = cubeHeight / 6;
            cloudShape.scale = new Vector3(0.1f, 0.1f, 0.4f);
            main.startSize = cubeHeight / 12;
            //cloudMain.startSize = cubeHeight / 12;
            cloudMain.startSize = UnityEngine.Random.Range(0.5f, 1.2f);
            cloudMain.startLifetime = UnityEngine.Random.Range(1f, 2f);
            burstShape.scale = new Vector3(0.1f, 0.1f, 0.4f);
            burstMain.startSize = UnityEngine.Random.Range(0.1f, 0.5f);

            cloudEmission.rateOverTime = 0.75f * cloudEmissionRate;
        }
        else
        {
            shape.radius = 0.7f;
            //cloudShape.radius = 0.7f;
            cloudShape.scale = new Vector3(0.5f, 0.5f, 0.8f);
            cloudMain.startSize = UnityEngine.Random.Range(0.8f, 2f); 
            main.startSize = UnityEngine.Random.Range(0.1f, 0.4f);
            burstMain.startSize = UnityEngine.Random.Range(0.1f, 1f);
            cloudEmission.rateOverTime = cloudEmissionRate;
            cloudMain.startLifetime = UnityEngine.Random.Range(6f, 8f);
            burstShape.scale = new Vector3(0.5f, 0.5f, 0.8f);
        }
        
        shape.length = 8 * shape.radius;
        //cloudShape.length = 8 * cloudShape.length;

    }


    void ParticleBurst()
    {
        particleBurst.Play();
        var burst = particleBurst.emission;
        burst.enabled = true;
        var burstCount = new ParticleSystem.Burst(0.0f, 500); 
        particleBurst.emission.SetBursts(new[] { burstCount });


        particleObject.Play();
        var objectBurst = particleObject.emission;
        objectBurst.enabled = true;
        var objectBurstCount = new ParticleSystem.Burst(0.0f, 1000);
        particleObject.emission.SetBursts(new[] { objectBurstCount });

        //particleObject er ny (tidligere rateovertime: 2000, rateoverdistance: 600)
    }


    void OnDrawGizmos()
    {
        Gizmos.color = Color.red; // Set the Gizmo color to red
        Gizmos.DrawWireSphere(transform.position, cubeHeight); // Draw a red wireframe sphere around the skier/cube at its position
    }

    void Start()
    {

    }

    void Update()
    {
        cubeWidth = transform.localScale.x;
        cubeHeight = transform.localScale.y;
        currentDirectionState = placementScript.GetCurrentDirectionState();
        AdjustParticlePlacement();
        AdjustParticleDirectionAndSize();

        if ((currentDirectionState != previousDirectionState) && burstMode )
        {
            //Debug.Log("Burst!");
            ParticleBurst();
        }
        //else
        //{
        //    particleObject.Play();
        //}

        averageAngle = placementScript.GetAverageAngle();

    }


}
