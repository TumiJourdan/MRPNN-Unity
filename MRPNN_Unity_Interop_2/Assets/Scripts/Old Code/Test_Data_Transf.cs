using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cuda_CSharp_Interop;
using System;
public class Test_Data_Transf : MonoBehaviour
{
    private RenderTexture depthRenderTex; // RenderTexture to store depth
    private Texture depthTex = null; // The fetched _CameraDepthTexture
    private IntPtr texturePtr = IntPtr.Zero; // Native texture pointer (for CUDA)

    Int2 size = new Int2 { x = 512, y = 512 };
    IntPtr outputTarget2;
    void Start()
    {
        D3DInfo info = cuda_interop2.CheckDirect3DSupport();

        Debug.Log("Direct3D-CUDA Interop Supported: " + info.isSupported);
        Debug.Log("Vendor: " + info.vendorName);
        Debug.Log("Adapter: " + info.adapterName);
        Debug.Log("CUDA Info: " + info.cudaInfo);
    }

    // Update is called once per frame
/*    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
*//*        depthTex = Shader.GetGlobalTexture("_CameraDepthTexture");

        // Create a RenderTexture that matches the camera depth texture format
        depthRenderTex = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.Depth);
        depthRenderTex.Create();

        if (depthTex != null)
        {
            Graphics.Blit(depthTex, depthRenderTex);
            Int2 size = new Int2();
            size.x = Screen.width;
            size.y = Screen.height;
            IntPtr texturePtr = depthRenderTex.GetNativeTexturePtr();
            cuda_interop2.RegisterTextureWithCUDA(texturePtr, size);
        }
*/
        /*// Render the source texture to the destination normally
        Graphics.Blit(source, destination);

        if (depthTex != null)
        {
            Graphics.Blit((Texture)depthTex, depthRenderTex);
        }

        if (texturePtr != IntPtr.Zero)
        {
            cuda_interop2.MapAndProcessTexture();
        }*//*
    }*/
}
