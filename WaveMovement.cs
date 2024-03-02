using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveMovement : MonoBehaviour
{
    public SkierPlacementScript placementScript;
    public GameObject waveModel;

    private float cubeWidth;
    private float cubeHeight;
    private SkierPlacementScript.DirectionState currentDirectionState;

    void AdjustWavePlacement()
    {
        Vector3 bottomCenterRel = new Vector3(0, -cubeHeight / 10, -5);
        Vector3 bottomLeftRel = new Vector3(-cubeWidth / 10, -cubeHeight / 10, -5);
        Vector3 bottomRightRel = new Vector3(cubeWidth / 10, -cubeHeight / 10, -5);

        waveModel.transform.localScale = new Vector3(cubeHeight/2, cubeHeight/2, cubeHeight/2);

        switch (currentDirectionState)
        {
            case SkierPlacementScript.DirectionState.Straight:
                break;
            case SkierPlacementScript.DirectionState.SoftLeft:
                waveModel.transform.localPosition = bottomRightRel;
                //waveModel.transform.rotation = Quaternion.Slerp
                break;
            case SkierPlacementScript.DirectionState.HardLeft:
                waveModel.transform.localPosition = bottomRightRel;
                break;
            case SkierPlacementScript.DirectionState.SoftRight:
                waveModel.transform.localPosition = bottomLeftRel;
                break;
            case SkierPlacementScript.DirectionState.HardRight:
                waveModel.transform.localPosition = bottomLeftRel;
                break;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        cubeWidth = transform.localScale.x;
        cubeHeight = transform.localScale.y;

        currentDirectionState = placementScript.GetCurrentDirectionState();

        AdjustWavePlacement();

    }
}
