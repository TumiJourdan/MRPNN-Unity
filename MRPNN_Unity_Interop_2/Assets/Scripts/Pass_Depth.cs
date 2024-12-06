using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cuda_CSharp_Interop;
public class Pass_Depth : MonoBehaviour
{
    public Shader depthCopyShader;
    private Material depthCopyMaterial;
    public Material depth_debugMaterial;
    public Material depthMaterial;
    RenderTexture outputTex;


    public int textureWidth = 1920;
    public int textureHeight = 1080;
    private RenderTexture depthRT;
    private Texture2D depthTexture;


    /*    [DllImport("YourCudaDLL")]
        private static extern void ProcessDepthData(float[] depthData, int width, int height);*/

    void Start()
    {

        textureWidth = Screen.width;
        textureHeight = Screen.height;
        depthRT = new RenderTexture(textureWidth, textureHeight, 0, RenderTextureFormat.RFloat);
        depthTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RFloat, false);

        RenderTexture outputTex;
        outputTex = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.RFloat);
        outputTex.Create();
        depthCopyMaterial = new Material(depthCopyShader);

    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {


        // Use Graphics.Blit to copy the depth texture to your output texture.
        Texture tex = Shader.GetGlobalTexture("_CameraDepthTexture");
        depthCopyMaterial.SetTexture("_CameraDepthTexture", tex);
        Graphics.Blit((RenderTexture)null, outputTex, depthCopyMaterial);
        // Optionally, draw the modified output onto the screen.
        Graphics.Blit(outputTex, (RenderTexture)null);  // This blits the output back to the screen.
        Camera.main.targetTexture = null;




        textureWidth = Screen.width;
        textureHeight = Screen.height;
        // Render depth to RenderTexture

        // Read RenderTexture into Texture2D
/*        RenderTexture.active = depthRT;
        
        depthTexture.ReadPixels(new Rect(0, 0, textureWidth, textureHeight), 0, 0);
        depthTexture.Apply();
        RenderTexture.active = null;
        depthMaterial.SetTexture("_MainTex", depthTexture);
*/
        

        /*        // Get depth data as float array
                float[] depthData = new float[textureWidth * textureHeight];
                Color[] pixels = depthTexture.GetPixels();
                for (int i = 0; i < pixels.Length; i++)
                {
                    depthData[i] = pixels[i].r; // Depth is stored in the red channel
                }
                cuda_interop2.ProcessDepthData(depthData, textureWidth, textureHeight);*/
    }
}
