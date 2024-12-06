using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Blit_Post_Render : MonoBehaviour
{
    RenderTexture outputTex;
    public Shader shader;
    Material depthCopyMaterial;
    void Start()
    {
        Camera.main.depthTextureMode = DepthTextureMode.Depth;  // Ensure the depth texture is available.

        // Create a RenderTexture for the output.
        outputTex = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
        outputTex.Create();
        depthCopyMaterial = new Material(shader);
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {

        // Use Graphics.Blit to copy the depth texture to your output texture.
        Texture tex = Shader.GetGlobalTexture("_CameraDepthTexture");
        depthCopyMaterial.SetTexture("_CameraDepthTexture", tex);
        Graphics.Blit(Shader.GetGlobalTexture("_CameraDepthTexture"), outputTex, depthCopyMaterial);
        // Optionally, draw the modified output onto the screen.
        Graphics.Blit(outputTex, (RenderTexture)null);  // This blits the output back to the screen.
        Camera.main.targetTexture = null;
    }
}
