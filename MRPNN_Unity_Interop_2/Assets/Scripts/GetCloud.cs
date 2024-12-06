using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
using System.IO;
using Interop;


public class GetCloud : MonoBehaviour
{

    private int kernelHandle;
    //shaders
    public Shader blendShader;
    public ComputeShader AccumulateTransmittance_CS;
    private Material blendMaterial;


    //textures
    private RenderTexture accumRenderTexture;
    private Texture2D stronglyTypedTexture;
    private Texture2D infoTexture;


    //cloud properties
    public Transform cloud_trans;
    public Transform directional_light;
    [Header("Cloud Settings")]
    public FileNames cloud_to_render = FileNames.CLOUD0;
    public float alpha = 1;
    public float blendFactor = 0.8f;
    public int cloudRes = 1024;
    public int cloud_raymarch_step = 1024;

    private delegate void TextureUpdateCallback(System.IntPtr textureHandle);
    private int width = 512; 
    private int height = 512;

    public FPSTracker renderTimeTracker;

    float totalTime = 0;
    float maxTime = 1;

    int count = 0;
    bool render = false;

    Dictionary<string, Float3> transforms_dict = new Dictionary<string, Float3>();



    private HDRISaver hdrisaver;
    void CreateRenderTexture()
    {
        // Initialize the RenderTexture with the specified width, height, and depth.
        accumRenderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.RGFloat);
        // Optional: Set format, filtering, and other properties.
        accumRenderTexture.wrapMode = TextureWrapMode.Clamp;
        accumRenderTexture.enableRandomWrite = true;  // Allow GPU writing
        accumRenderTexture.filterMode = FilterMode.Bilinear;
        // Activate the render texture.
        accumRenderTexture.Create();
    }

    private void init_texture_sharing()
    {
        //INIT DIRECT3D

        stronglyTypedTexture = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
        stronglyTypedTexture.Apply();
        IntPtr texturePtr = stronglyTypedTexture.GetNativeTexturePtr();
        Cuda_Interop.RegisterTexture2D(texturePtr);


        infoTexture = new Texture2D(width, height, TextureFormat.RGFloat, false, true);
        infoTexture.Apply();
        IntPtr infoPointer = infoTexture.GetNativeTexturePtr();
        Cuda_Interop.RegisterTexture2D(infoPointer);


        CreateRenderTexture();
        RenderTexture.active = accumRenderTexture;
        Cuda_Interop.SetTextureFromUnity(Marshal.GetFunctionPointerForDelegate<TextureUpdateCallback>(OnTextureUpdated));
    }

    private void init_volume()
    {
        //INIT VOLUME RENDER
        Int2 size;
        size.x = width;
        size.y = height;
        string modelPath = Path.Combine(Application.streamingAssetsPath, cloud_to_render.ToString());
        Debug.Log($"Loading model from: {modelPath}.bin");
        if (!File.Exists(modelPath + ".bin"))
        {
            Debug.LogError($"Model file not found at: {modelPath}.bin");
            return;
        }

        Cuda_Interop.initialize_memory_path(size, modelPath, cloudRes);
        Cuda_Interop.set_volume_datas();
        Cuda_Interop.set_raymarch_step(cloud_raymarch_step);

        kernelHandle = AccumulateTransmittance_CS.FindKernel("CSMain");
    }

    void Start()
    {
        //camera render texture
        if (blendMaterial == null && blendShader != null)
        {
            blendMaterial = new Material(blendShader);
        }

        //initialise cuda and directx
        width = SetResolution.targetResolution;
        height = SetResolution.targetResolution;
        Debug.Log("Res" + width + ","+height);

/*        Material skyboxMaterial = RenderSettings.skybox;
        Texture2D skyboxTexture = skyboxMaterial.GetTexture("_MainTex") as Texture2D; hdrisaver = GetComponent<HDRISaver>();
        hdrisaver.SaveHDRI(skyboxTexture);
        string path = Path.Combine(Application.streamingAssetsPath, "hdr_texture.hdr");
        Cuda_Interop.setHDRIInterface(path);*/

        init_texture_sharing();
        init_volume();
    }

    private void Update()
    {
        
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {

        if (totalTime < maxTime)
        {
            totalTime += maxTime;
            return;
        }



        float time = Time.realtimeSinceStartup;
        width = SetResolution.targetResolution;
        height = SetResolution.targetResolution;

        //light

        Vector3 cudaLightForward = Vector3.Cross(-1*directional_light.right, directional_light.up);

        Float3 lightDir = new Float3(cudaLightForward.x, cudaLightForward.y, cudaLightForward.z);
        lightDir = lightDir.Normalized();
        Vector4 color = directional_light.GetComponent<Light>().color;
        Float3 lightColor = new Float3(color.x, color.y, color.z);

        Int2 size;
        size.x = width;
        size.y = height;



        Transform cam = Camera.main.transform;
        Float3 right = new Float3(
            -cam.right.x,
            -cam.right.y,
            -cam.right.z  
        );

        Float3 up = new Float3(
            cam.up.x,
            cam.up.y,
            cam.up.z     
        );

        Float3 position = new Float3(
            cam.position.x,
            cam.position.y,
            cam.position.z 
        );

        infoTexture.GetNativeTexturePtr();
        stronglyTypedTexture.GetNativeTexturePtr();


        Cuda_Interop.cuda_run_render(position, up, right, lightDir, lightColor, alpha, size);

        // Set the textures in the compute shader
        accumRenderTexture.DiscardContents();  // Clear the texture contents to black
        AccumulateTransmittance_CS.SetTexture(kernelHandle, "_AccumTex", accumRenderTexture);
        AccumulateTransmittance_CS.SetTexture(kernelHandle, "_Transmittance", infoTexture);

        int threadGroupsX = Mathf.CeilToInt(accumRenderTexture.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(accumRenderTexture.height / 8.0f);
        AccumulateTransmittance_CS.Dispatch(kernelHandle, threadGroupsX, threadGroupsY, 1);
        
        //blend results
        blendMaterial.SetTexture("_CloudTex", stronglyTypedTexture);
        blendMaterial.SetFloat("_BlendFactor", blendFactor);
        blendMaterial.SetTexture("_Transmittance", infoTexture);
        blendMaterial.SetTexture("_AccumTex", accumRenderTexture);
        Graphics.Blit(source, destination, blendMaterial);


        time = Time.realtimeSinceStartup - time;
    }


    [AOT.MonoPInvokeCallback(typeof(TextureUpdateCallback))]
    private static void OnTextureUpdated(IntPtr textureHandle)
    {
        Debug.Log("Texture updated by CUDA");
    }

    // Singleton instance for accessing non-static members in static context
    private static GetCloud Instance;

    void Awake()
    {
        Instance = this;
    }
    private void OnDestroy()
    {
        if (stronglyTypedTexture != null)
        {
            Destroy(stronglyTypedTexture);
            Destroy(infoTexture);
        }
    }
    void OnDisable()
    {
        Cuda_Interop.clean_memory();
    }
}


public enum FileNames
{
    CLOUD0,
    CLOUD1,
    MODEL1
}