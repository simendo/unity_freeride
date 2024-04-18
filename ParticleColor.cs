using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleColor : MonoBehaviour
{
    public SkierPlacementScript placementScript;
    public ParticleSystem particleObject;
    public ParticleSystem particleCloud;

    public bool useColoring;

    private Gradient currentGradient;
    private Gradient targetGradient;
    private float transitionProgress = 0f;
    private bool isTransitioning = false;
    private float transitionSpeed = 1f;
    
    private SkierPlacementScript.DirectionState currentDirectionState;
    private SkierPlacementScript.DirectionState previousDirectionState;


    void StartColorTransition()
    {
        targetGradient = GetTargetGradient();
        isTransitioning = true;
        transitionProgress = 0f;
        
        if (currentGradient == null)
        {
            currentGradient = targetGradient;
            isTransitioning = false;
        }
    }

    void UpdateColorTransition()
    {
        transitionProgress += Time.deltaTime * transitionSpeed;
        if (transitionProgress >= 1f)
        {
            transitionProgress = 1f;
            isTransitioning = false;
        }

        Gradient interpolatedGradient = InterpolateGradients(currentGradient, targetGradient, transitionProgress);
        var colorOverTime = particleObject.colorOverLifetime;
        var cloudColorOverTime = particleCloud.colorOverLifetime;
        colorOverTime.color = new ParticleSystem.MinMaxGradient(interpolatedGradient);
        var adjustedCloudGradient = AdjustAlpha(interpolatedGradient, 0.3f);
        cloudColorOverTime.color = new ParticleSystem.MinMaxGradient(adjustedCloudGradient);

        if (!isTransitioning)
        {
            currentGradient = targetGradient;
        }
    }

    Gradient GetTargetGradient()
    {
        Gradient gradient = new Gradient();

        if (useColoring) //FORTSETT HER -> BRUK FARGER I JSON!
        {
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(0.8f, 0.8f, 0.8f), 0.0f),
                    new GradientColorKey(new Color(0.9f, 0.9f, 0.9f), 0.5f),
                    new GradientColorKey(Color.green, 1.0f)
                },
                 new GradientAlphaKey[] {
                    new GradientAlphaKey(0.4f, 0.0f),
                    new GradientAlphaKey(0.9f, 0.5f),
                    new GradientAlphaKey(0.4f, 1.0f)
                 }
            );
        }
        else
        {
            switch (currentDirectionState)
            {
                case SkierPlacementScript.DirectionState.Straight:
                    gradient.SetKeys(
                    new GradientColorKey[] {
                    new GradientColorKey(new Color(0.8f, 0.8f, 0.8f), 0.0f),
                    new GradientColorKey(new Color(0.9f, 0.9f, 0.9f), 0.5f),
                    new GradientColorKey(Color.green, 1.0f)
                    },
                    new GradientAlphaKey[] {
                    new GradientAlphaKey(0.4f, 0.0f),
                    new GradientAlphaKey(0.9f, 0.5f),
                    new GradientAlphaKey(0.4f, 1.0f)
                    }
                );
                    break;
                case SkierPlacementScript.DirectionState.SoftLeft:
                    gradient.SetKeys(
                    new GradientColorKey[] {
                    new GradientColorKey(Color.cyan, 0.0f),
                    new GradientColorKey(Color.cyan, 0.5f),
                    new GradientColorKey(Color.cyan, 1.0f)
                    },
                    new GradientAlphaKey[] {
                    new GradientAlphaKey(0.4f, 0.0f),
                    new GradientAlphaKey(0.9f, 0.5f),
                    new GradientAlphaKey(0.4f, 1.0f)
                    }
                );
                    break;
                case SkierPlacementScript.DirectionState.HardLeft:
                    gradient.SetKeys(
                    new GradientColorKey[] {
                    new GradientColorKey(Color.blue, 0.0f),
                    new GradientColorKey(Color.blue, 0.5f),
                    new GradientColorKey(Color.blue, 1.0f)
                    },
                    new GradientAlphaKey[] {
                    new GradientAlphaKey(0.4f, 0.0f),
                    new GradientAlphaKey(0.9f, 0.5f),
                    new GradientAlphaKey(0.4f, 1.0f)
                    }
                );
                    break;
                case SkierPlacementScript.DirectionState.SoftRight:
                    gradient.SetKeys(
                    new GradientColorKey[] {
                    new GradientColorKey(Color.yellow, 0.0f),
                    new GradientColorKey(Color.yellow, 0.5f),
                    new GradientColorKey(Color.yellow, 1.0f)
                    },
                    new GradientAlphaKey[] {
                    new GradientAlphaKey(0.4f, 0.0f),
                    new GradientAlphaKey(0.9f, 0.5f),
                    new GradientAlphaKey(0.4f, 1.0f)
                    }
                );
                    break;
                case SkierPlacementScript.DirectionState.HardRight:
                    gradient.SetKeys(
                    new GradientColorKey[] {
                    new GradientColorKey(Color.red, 0.0f),
                    new GradientColorKey(Color.red, 0.5f),
                    new GradientColorKey(Color.red, 1.0f)
                    },
                    new GradientAlphaKey[] {
                    new GradientAlphaKey(0.4f, 0.0f),
                    new GradientAlphaKey(0.9f, 0.5f),
                    new GradientAlphaKey(0.4f, 1.0f)
                    }
                );
                    break;
            }
        }

        return gradient;
    }


    Gradient InterpolateGradients(Gradient gradientFrom, Gradient gradientTo, float progress)
    {
        Gradient resultGradient = new Gradient();

        List<GradientColorKey> colorKeys = new List<GradientColorKey>();
        List<GradientAlphaKey> alphaKeys = new List<GradientAlphaKey>();

        int maxColorKeyCount = Mathf.Max(gradientFrom.colorKeys.Length, gradientTo.colorKeys.Length);
        int maxAlphaKeyCount = Mathf.Max(gradientFrom.alphaKeys.Length, gradientTo.alphaKeys.Length);

        // Interpolate color
        for (int i = 0; i < maxColorKeyCount; i++)
        {
            float time = Mathf.Lerp(gradientFrom.colorKeys[i % gradientFrom.colorKeys.Length].time, gradientTo.colorKeys[i % gradientTo.colorKeys.Length].time, progress);
            Color color = Color.Lerp(gradientFrom.colorKeys[i % gradientFrom.colorKeys.Length].color, gradientTo.colorKeys[i % gradientTo.colorKeys.Length].color, progress);
            colorKeys.Add(new GradientColorKey(color, time));
        }

        // Interpolate alpha
        for (int i = 0; i < maxAlphaKeyCount; i++)
        {
            float time = Mathf.Lerp(gradientFrom.alphaKeys[i % gradientFrom.alphaKeys.Length].time, gradientTo.alphaKeys[i % gradientTo.alphaKeys.Length].time, progress);
            float alpha = Mathf.Lerp(gradientFrom.alphaKeys[i % gradientFrom.alphaKeys.Length].alpha, gradientTo.alphaKeys[i % gradientTo.alphaKeys.Length].alpha, progress);
            alphaKeys.Add(new GradientAlphaKey(alpha, time));
        }

        resultGradient.SetKeys(colorKeys.ToArray(), alphaKeys.ToArray());

        return resultGradient;
    }

    //used to assign different alpha value to the cloud
    Gradient AdjustAlpha(Gradient gradient, float newAlpha)
    {
        var colorKeys = gradient.colorKeys;
        var alphaKeys = new GradientAlphaKey[gradient.alphaKeys.Length];
        for (int i = 0; i < gradient.alphaKeys.Length; i++)
        {
            alphaKeys[i] = new GradientAlphaKey(newAlpha, gradient.alphaKeys[i].time);
        }

        Gradient newGradient = new Gradient();
        newGradient.SetKeys(colorKeys, alphaKeys);
        return newGradient;
    }

    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        SkierPlacementScript.OnColorChanged -= UpdateParticleColor;
    }

    void UpdateParticleColor(Color color)
    {
        var main = particleObject.main;
        main.startColor = color; // Set the particle system's start color

        // Optional: Adjust cloud particle system as well
        var cloudMain = particleCloud.main;
        cloudMain.startColor = color;
    }


    // Start is called before the first frame update
    void Start()
    {
        currentDirectionState = placementScript.GetCurrentDirectionState();
        currentGradient = GetTargetGradient();
        var colorOverTime = particleObject.colorOverLifetime;
        var cloudColorOverTime = particleCloud.colorOverLifetime;

        if (useColoring)
        {
            if (placementScript.staticMode)
            {
                SkierPlacementScript.OnColorChanged += UpdateParticleColor;
            }
            else
            {
                colorOverTime.enabled = true;
                cloudColorOverTime.enabled = true;
                colorOverTime.color = new ParticleSystem.MinMaxGradient(currentGradient);
                var adjustedCloudGradient = AdjustAlpha(currentGradient, 0.5f);
                cloudColorOverTime.color = new ParticleSystem.MinMaxGradient(adjustedCloudGradient);
            }
        }

        

    }

    // Update is called once per frame
    void Update()
    {
        if (useColoring)
        {
            if (!placementScript.staticMode)
            {
                currentDirectionState = placementScript.GetCurrentDirectionState();

                if (currentDirectionState != previousDirectionState)
                {
                    StartColorTransition();
                }

                if (isTransitioning)
                {
                    UpdateColorTransition();
                }

                previousDirectionState = currentDirectionState;
            }
            
        }
    }
}
