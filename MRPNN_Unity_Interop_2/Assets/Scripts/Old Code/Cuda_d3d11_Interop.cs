using System;
using UnityEngine;
using System.Runtime.InteropServices;

namespace D3D11_Cuda_NS
{
    public struct Float3
    {
        public float x;
        public float y;
        public float z;
        private float padding;
        // Constructor
        public Float3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.padding = 0f; // Initialize padding to 0
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Int2
    {
        public int x;
        public int y;
    }
    public class Cuda_d3d11_Interop
    {
        private const string DllName = @"C:/Tumi/Unity/MRPNN_Unity_Interop/Assets/Scripts/Old Code/VolumeRender_Flip.dll"; // Make sure this matches the actual DLL name

        [DllImport(DllName)]
        public static extern void SetTextureFromUnity(System.IntPtr updateCallback);

        [DllImport(DllName)]
        public static extern void InitCudaInterop(System.IntPtr texture);

        [DllImport(DllName)]
        public static extern void ProcessTextureWithCuda(int width, int height);

        [DllImport(DllName)]
        public static extern void initialize_memory(Int2 size);

        [DllImport(DllName)]
        public static extern void set_volume_datas();
        //cuda_run_render(float3 origin, float3 up, float3 right, float3 lightDir, float3 lightColor, float alpha, int2 size)
        [DllImport(DllName)]
        public static extern void cuda_run_render(Float3 origin, Float3 up, Float3 right, Float3 lightDir, Float3 lightColor, float alpha, Int2 size);

    }
}

