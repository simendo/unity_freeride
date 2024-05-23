using UnityEngine;
using System.Collections.Generic;
using System;


public class ParticleMovement : MonoBehaviour
{
    public SkierPlacementScript placementScript;
    public ParticleSystem particleObject;
    public ParticleSystem particleCloud;
    public ParticleSystem particleBurst;
    public ParticleSystem particleStatic;

    public bool burstMode;
   
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
        Vector3 bottomCenterRel = new Vector3(0, -cubeHeight/2, -2);
        Vector3 bottomLeftRel = new Vector3(-cubeWidth/4, -cubeHeight/6, -3);
        Vector3 bottomRightRel = new Vector3(cubeWidth/4, -cubeHeight/6, -3);

        //burst
        //Vector3 bottomLeftRel = new Vector3(-cubeWidth / 15, -cubeHeight / 8, -2);
        //Vector3 bottomRightRel = new Vector3(cubeWidth / 15, -cubeHeight / 8, -2);

        if (placementScript.staticMode)
        {
            particleStatic.transform.localPosition = bottomLeftRel + new Vector3(-0.3f,-0.4f,1);
            particleObject.transform.localPosition = bottomLeftRel + new Vector3(-0.3f, -0.4f, 1);
            var cloudShape = particleCloud.shape;
            cloudShape.rotation = new Vector3(-35, -70, 0);
            particleCloud.transform.localPosition = bottomLeftRel + new Vector3(-1.0f, -0.1f, 1);

        }
        else
        {
            switch (currentDirectionState)
            {
                case SkierPlacementScript.DirectionState.Straight:
                    particleObject.transform.localPosition = bottomCenterRel + new Vector3(0.5f, 0, 0);
                    particleCloud.transform.localPosition = bottomCenterRel + new Vector3(1, 0, 0);
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

    //Method used to place particles based on detection of skis
    /*
    public void SetSkiParticlePosition(Vector3 position)
    {
        Vector3 bottomCenterRel = new Vector3(0, -cubeHeight / 6, -2);
        Vector3 bottomLeftRel = new Vector3(-cubeWidth / 4, -cubeHeight / 6, -2);
        Vector3 bottomRightRel = new Vector3(cubeWidth / 4, -cubeHeight / 6, -2);
        switch (currentDirectionState)
        {
            case SkierPlacementScript.DirectionState.Straight:
                particleBurst.transform.localPosition = bottomCenterRel + new Vector3(0, 0, 0);
                break;
            case SkierPlacementScript.DirectionState.SoftLeft:
                particleBurst.transform.localPosition = bottomRightRel + new Vector3(0, 0, -2);
                break;
            case SkierPlacementScript.DirectionState.HardLeft:
                particleBurst.transform.localPosition = bottomRightRel + new Vector3(0, 0, -2);
                break;
            case SkierPlacementScript.DirectionState.SoftRight:
                particleBurst.transform.localPosition = bottomLeftRel + new Vector3(0, 0, 0);
                break;
            case SkierPlacementScript.DirectionState.HardRight:
                particleBurst.transform.localPosition = bottomLeftRel + new Vector3(0, 0, 0);
                break;
        }
        particleBurst.transform.position = position + new Vector3(0, -0.3f, 0); 
        particleBurst.Play();
    }
    */


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
                shape.rotation = new Vector3(0, 40, 0);
                burstShape.rotation = new Vector3(0, 40, 0);
                main.startSpeed = UnityEngine.Random.Range(7, 8);
                cloudMain.startSpeed = UnityEngine.Random.Range(2, 3);
                break;
            case SkierPlacementScript.DirectionState.SoftRight:
                flowDirection = (Vector2.up + Vector2.right).normalized;
                cloudShape.rotation = new Vector3(0, 40, 0);
                shape.rotation = new Vector3(0, -40, 0);
                burstShape.rotation = new Vector3(0, -40, 0);
                main.startSpeed = UnityEngine.Random.Range(7, 8);
                cloudMain.startSpeed = UnityEngine.Random.Range(2, 3);
                break;
            case SkierPlacementScript.DirectionState.HardLeft:
                flowDirection = Vector2.left;
                cloudShape.rotation = new Vector3(0, -60, 0);
                shape.rotation = new Vector3(0, 60, 0);
                burstShape.rotation = new Vector3(0, 60, 0);
                main.startSpeed = UnityEngine.Random.Range(10, 12);
                cloudMain.startSpeed = UnityEngine.Random.Range(4, 5);
                break;
            case SkierPlacementScript.DirectionState.HardRight:
                flowDirection = Vector2.right;
                cloudShape.rotation = new Vector3(0, 60, 0);
                shape.rotation = new Vector3(0, -60, 0);
                burstShape.rotation = new Vector3(0, -60, 0);
                main.startSpeed = UnityEngine.Random.Range(10, 12);
                cloudMain.startSpeed = UnityEngine.Random.Range(4, 5);
                break;
        }


        flowDirection.Normalize();

        var cloudEmission = particleCloud.emission;

        //shape.scale = new Vector3(cubeWidth / 2, cubeHeight / 8, 0);

        if (cubeHeight < 1.8f)
        {
            shape.radius = cubeHeight / 50;
            main.startSize = cubeHeight / 12;

            cloudShape.radius = cubeHeight / 6;
            cloudShape.scale = new Vector3(0.1f, 0.1f, 0.4f);
            cloudMain.startSize = cubeHeight / 12;
            //cloudMain.startSize = UnityEngine.Random.Range(0.5f, 1.2f);
            //cloudMain.startLifetime = UnityEngine.Random.Range(1f, 2f);
            burstShape.scale = new Vector3(0.1f, 0.1f, 0.4f);
            burstMain.startSize = UnityEngine.Random.Range(0.1f, 0.5f);
            //cloudEmission.rateOverTime = 0.75f * cloudEmissionRate;
        }
        else
        {
            shape.radius = 0.06f;
            main.startSize = UnityEngine.Random.Range(0.1f, 0.4f);

            cloudShape.radius = 0.7f;
            cloudShape.scale = new Vector3(0.5f, 0.5f, 0.8f);
            cloudMain.startSize = UnityEngine.Random.Range(0.8f, 2f);
            //cloudEmission.rateOverTime = cloudEmissionRate;
            //cloudMain.startLifetime = UnityEngine.Random.Range(6f, 8f);

            burstMain.startSize = UnityEngine.Random.Range(0.1f, 1f);
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

    //Method used to verify if cube placement is correct
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red; 
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

        if ((currentDirectionState != previousDirectionState) && burstMode && currentDirectionState == SkierPlacementScript.DirectionState.HardLeft)
            {
            Debug.Log("Burst!");
            ParticleBurst();
        }
        averageAngle = placementScript.GetAverageAngle();
    }


}
