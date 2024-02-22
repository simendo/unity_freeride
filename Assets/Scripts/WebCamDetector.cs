using Assets.Scripts;
using NN;
using System;
using System.Collections.Generic;
using Unity.Barracuda;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;
using UnityEngine.Video;

[RequireComponent(typeof(OnGUICanvasRelativeDrawer))]
public class WebCamDetector : MonoBehaviour
{
    [Tooltip("File of YOLO model. If you want to use another than YOLOv2 tiny, it may be necessary to change some const values in YOLOHandler.cs")]
    public NNModel ModelFile;
    [Tooltip("Text file with classes names separated by coma ','")]
    public TextAsset ClassesTextFile;

    public VideoPlayer videoPlayer;

    [Tooltip("RawImage component which will be used to draw resuls.")]
    public RawImage ImageUI;

    [Range(0.0f, 1f)]
    [Tooltip("The minimum value of box confidence below which boxes won't be drawn.")]
    public float MinBoxConfidence = 0.3f;

    NNHandler nn;
    YOLOHandler yolo;

    //WebCamTextureProvider CamTextureProvider;
    Texture2D VideoTexture;

    string[] classesNames;
    OnGUICanvasRelativeDrawer relativeDrawer;

    Color[] colorArray = new Color[] { Color.red, Color.green, Color.blue, Color.cyan, Color.magenta, Color.yellow };

    int height;
    int width;

    void OnEnable()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Play();
        }

        nn = new NNHandler(ModelFile);
        yolo = new YOLOHandler(nn);

        var firstInput = nn.model.inputs[0];
        //int height = firstInput.shape[5];
        //int width = firstInput.shape[6];
        height = firstInput.shape[5];
        width = firstInput.shape[6];


        TextureFormat format = TextureFormat.RGB24;
        VideoTexture = new Texture2D(width, height, format, mipChain: false);
        RenderTexture rgbRenderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.Default);
        rgbRenderTexture.format = RenderTextureFormat.ARGB32; 
        videoPlayer.targetTexture = rgbRenderTexture;
        /*
        CamTextureProvider = new WebCamTextureProvider(width, height);
        CamTextureProvider.Start();
        */

        relativeDrawer = GetComponent<OnGUICanvasRelativeDrawer>();
        relativeDrawer.relativeObject = ImageUI.GetComponent<RectTransform>();

        classesNames = ClassesTextFile.text.Split(',');
        YOLOv2Postprocessor.DiscardThreshold = MinBoxConfidence;
    }

    void Update()
    {
        Texture2D texture = GetNextTexture();

        Debug.Log($"Texture for YOLO Model - Width: {texture.width}, Height: {texture.height}, Format: {texture.format}");

        var boxes = yolo.Run(texture);
        DrawResults(boxes, texture);
        ImageUI.texture = texture;
    }

    Texture2D GetNextTexture()
    {
        if (videoPlayer.isPlaying && videoPlayer.texture != null)
        {
            RenderTexture videoRT = videoPlayer.targetTexture;
            Texture2D rgbaFrame = new Texture2D(videoRT.width, videoRT.height, TextureFormat.RGBA32, false);

            RenderTexture.active = videoRT;
            rgbaFrame.ReadPixels(new Rect(0, 0, videoRT.width, videoRT.height), 0, 0);
            rgbaFrame.Apply();
            RenderTexture.active = null;

            // Convert RGBA to RGB by discarding the alpha channel
            Texture2D rgbFrame = new Texture2D(rgbaFrame.width, rgbaFrame.height, TextureFormat.RGB24, false);
            rgbFrame.SetPixels(rgbaFrame.GetPixels());
            rgbFrame.Apply();

            Destroy(rgbaFrame);

            // Optionally resize the texture if necessary
            Texture2D resizedFrame = ResizeTexture(rgbFrame, width, height);
            Destroy(rgbFrame);

            return resizedFrame;
        }
        return null;
    }



    private void OnDisable()
    {
        nn.Dispose();
        yolo.Dispose();
        //CamTextureProvider.Stop();
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }
    }

    private void DrawResults(IEnumerable<ResultBox> results, Texture2D texture)
    {
        relativeDrawer.Clear();
        foreach(ResultBox box in results)
            DrawBox(box, texture);
    }

    private void DrawBox(ResultBox box, Texture2D img)
    {
        if (box.score < MinBoxConfidence)
            return;

        Color boxColor = colorArray[box.bestClassIndex % colorArray.Length];
        int boxWidth = (int)(box.score / MinBoxConfidence);
        TextureTools.DrawRectOutline(img, box.rect, boxColor, boxWidth, rectIsNormalized: false, revertY: true);

        Vector2 textureSize = new(img.width, img.height);
        relativeDrawer.DrawLabel(classesNames[box.bestClassIndex], box.rect.position / textureSize);
    }


    Texture2D ResizeTexture(Texture2D sourceTexture, int width, int height)
    {
        // Create a RenderTexture with the desired dimensions
        RenderTexture rt = new RenderTexture(width, height, 24);
        RenderTexture currentActiveRT = RenderTexture.active;

        // Set the RenderTexture as the active RenderTexture
        RenderTexture.active = rt;

        // Copy the source texture to the RenderTexture, resizing it in the process
        Graphics.Blit(sourceTexture, rt);

        // Create a new Texture2D with the desired dimensions
        Texture2D result = new Texture2D(width, height);

        // Copy the pixels from the RenderTexture to the new Texture2D
        result.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        result.Apply();

        // Restore the previous active RenderTexture
        RenderTexture.active = currentActiveRT;
        rt.Release();

        return result;
    }

}
