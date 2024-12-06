using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using D3D11_Cuda_NS;
using System;
public class TestD3D11_old : MonoBehaviour
{

    private RenderTexture renderTexture;
    private Texture2D stronglyTypedTexture;
    public Material debug_texture_mat;


    private delegate void TextureUpdateCallback(System.IntPtr textureHandle);
    [SerializeField]
    private int width = 512;  // Width of the texture
    public int height = 512; // Height of the texture
                             // Start is called before the first frame update
    float totalTime = 0;
    float maxTime = 2;

    int count = 0;
    bool render = false;
    void CreateRenderTexture()
    {
        // Initialize the RenderTexture with the specified width, height, and depth.
        renderTexture = new RenderTexture(width, height, 24);

        // Optional: Set format, filtering, and other properties.
        renderTexture.format = RenderTextureFormat.ARGB32;
        renderTexture.wrapMode = TextureWrapMode.Clamp;
        renderTexture.enableRandomWrite = true;  // Allow GPU writing
        renderTexture.filterMode = FilterMode.Bilinear;

        // Activate the render texture.
        renderTexture.Create();
    }

    void Start()
    {        // suspend execution for 5 seconds


        CreateRenderTexture();

        stronglyTypedTexture = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
        stronglyTypedTexture.Apply();
        RenderTexture.active = renderTexture;

        RenderBuffer cols = renderTexture.colorBuffer;
        Graphics.ConvertTexture(renderTexture, stronglyTypedTexture);

        IntPtr texturePtr = stronglyTypedTexture.GetNativeTexturePtr();

        Cuda_d3d11_Interop.InitCudaInterop(texturePtr);
        Cuda_d3d11_Interop.SetTextureFromUnity(Marshal.GetFunctionPointerForDelegate<TextureUpdateCallback>(OnTextureUpdated));
        debug_texture_mat.SetTexture("_MainTex", renderTexture);
/*        //edit texture
        Cuda_d3d11_Interop.ProcessTextureWithCuda(width, height);
        Graphics.Blit(stronglyTypedTexture, renderTexture);*/
    }

    private void Update()
    {

        IntPtr anotherptr = stronglyTypedTexture.GetNativeTexturePtr();
        Debug.Log("Pointer = " + anotherptr);
        count += 1;
        totalTime = 0;
        width = stronglyTypedTexture.width;
        height = stronglyTypedTexture.height;
        Cuda_d3d11_Interop.ProcessTextureWithCuda(width, height);
        Graphics.Blit(stronglyTypedTexture, renderTexture);
        Debug.Log("Rendering"+count);

    }

    IEnumerator WaitAndPrint()
    {
        Debug.Log("Running from coroutine");
        yield return new WaitForSeconds(2);
        render = true;
    }

    [AOT.MonoPInvokeCallback(typeof(TextureUpdateCallback))]
    private static void OnTextureUpdated(IntPtr textureHandle)
    {
        Debug.Log("Texture updated by CUDA");
    }

    // Singleton instance for accessing non-static members in static context
    private static TestD3D11_old Instance;

    void Awake()
    {
        Instance = this;
    }
    private void OnDestroy()
    {
        if (stronglyTypedTexture != null)
        {
            Destroy(stronglyTypedTexture);
        }
    }

}
