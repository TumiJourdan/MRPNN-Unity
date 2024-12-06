
using UnityEngine;
using System;
using Cuda_CSharp_Interop;

public class MRPNN_Call : MonoBehaviour
{

    public Material material;  // Assign your HDRP shader material in the Inspector
    public int width = 512;
    public int height = 512;
    public float rate = 0.2f;

    Int2 size = new Int2 { x = 512, y = 512 };
    IntPtr outputTarget2;

    //cloud Position
    public Transform cloud_trans;
    //camera paramters
    public Vector3 target; // The object to rotate around
    public float baseDistance = 5.0f; // Base distance from the target
    public float rotationSpeed = 5.0f; // Speed of rotation

    private float phi; // Horizontal angle
    private float theta; // Vertical angle
    //Light params
    public Transform directional_light;
    //Depth
    Texture2D depthTexture;
    // Start is called before the first frame update
    void Start()
    {

        // Initialize memory
        cuda_interop2.initialize_memory(size, out outputTarget2);
        cuda_interop2.set_volume_datas();
        // depth texture holder

    }
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {



        // Get input for rotation
        if (Input.GetMouseButton(0)) // Left mouse button held down
        {
            phi += Input.GetAxis("Mouse X") * rotationSpeed;
            theta -= Input.GetAxis("Mouse Y") * rotationSpeed;
            theta = Mathf.Clamp(theta, 1.0f, 89.0f); // Prevent flipping
        }

        Transform cam = Camera.main.transform;


        //camera
        Float3 forward = new Float3(cam.forward.x, cam.forward.y, cam.forward.z);  // Z-axis (in world space)
        Float3 right = new Float3(cam.right.x, cam.right.y, cam.right.z);      // X-axis (in world space)
        Float3 up = new Float3(cam.up.x, cam.up.y, cam.up.z);            // Y-axis (in world space)

        //get transform which is centering cloud on origin
        Vector3 coord_convert = -1 * (cloud_trans.position);
        // move camera position to cuda co ord space
        Vector3 position_cuda = cam.position + coord_convert;
        Float3 origin = new Float3(position_cuda.x, position_cuda.y, position_cuda.z);
        //light
        Float3 lightDir = new Float3(directional_light.forward.x, directional_light.forward.y, directional_light.forward.z);
        Vector4 color = directional_light.GetComponent<Light>().color;
        Float3 lightColor = new Float3(color.x, color.y, color.z);
        float alpha = 1;


        // Run the CUDA render
        float time = Time.realtimeSinceStartup;
        //cuda_run_render(float3 origin,float3 up , float3 right,float3 lightDir, float3 lightColor, float alpha, int2 size) {
        cuda_interop2.cuda_run_render(origin, up, right, lightDir, lightColor, alpha, size);
        time = Time.realtimeSinceStartup - time;

        Debug.Log(time * 1000);

/*        Texture2D tex = image_creator.CreateTextureFromPointer(outputTarget2, size.x, size.y);
*/        // Assign the texture to the HDRP material (replace _BaseColorMap with your shader's texture property)
    }

}




