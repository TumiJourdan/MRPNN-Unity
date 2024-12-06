using System;
using UnityEngine;
using System.Runtime.InteropServices;

namespace Cuda_CSharp_Interop
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct D3DInfo
    {
        [MarshalAs(UnmanagedType.I1)]
        public bool isSupported;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string vendorName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string adapterName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string cudaInfo;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 16)]
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
        public static Float3 Normalize(Float3 vector)
        {
            float length = (float)Math.Sqrt(vector.x * vector.x + vector.y * vector.y + vector.z * vector.z);
            return length > 0 ? new Float3(vector.x / length, vector.y / length, vector.z / length) : new Float3(0, 0, 0);
        }

        // Cross product of two vectors
        public static Float3 Cross(Float3 a, Float3 b)
        {
            return new Float3(
                a.y * b.z - a.z * b.y,
                a.z * b.x - a.x * b.z,
                a.x * b.y - a.y * b.x
            );
        }
        // Addition operator
        public static Float3 operator +(Float3 a, Float3 b)
        {
            return new Float3(a.x + b.x, a.y + b.y, a.z + b.z);
        }

        // Subtraction operator
        public static Float3 operator -(Float3 a, Float3 b)
        {
            return new Float3(a.x - b.x, a.y - b.y, a.z - b.z);
        }

        // Negation operator (Unary -)
        public static Float3 operator -(Float3 a)
        {
            return new Float3(-a.x, -a.y, -a.z);
        }

        // Scalar multiplication
        public static Float3 operator *(Float3 a, float scalar)
        {
            return new Float3(a.x * scalar, a.y * scalar, a.z * scalar);
        }

        // Scalar division
        public static Float3 operator /(Float3 a, float scalar)
        {
            return new Float3(a.x / scalar, a.y / scalar, a.z / scalar);
        }
    }
    public unsafe struct Histogram
    {
        public const int HISTO_SIZE = 10; // Set your HISTO_SIZE here

        // Fixed array to match float bin[HISTO_SIZE * 3];
        public fixed float bin[HISTO_SIZE * 3];

        // Matching the float fields
        public float totalSampleNum;
        public float x2;
        public float x;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Int2
    {
        public int x;
        public int y;
    }
    class cuda_interop2
    {
        private const string DllName = @"C:/Tumi/Unity/MRPNN_Unity_Interop/Assets/Plugins/VolumeRender.dll"; // Make sure this matches the actual DLL name
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void initialize_memory(Int2 size, out IntPtr output_target2);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void set_volume_datas();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void clean_memory();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        //cuda_run_render(float3 origin,float3 up , float3 right,float3 lightDir, float3 lightColor, float alpha, int2 size) {
        public static extern void cuda_run_render(Float3 origin, Float3 up, Float3 right, Float3 lightDir, Float3 lightColor, float alpha, Int2 size);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void ProcessDepthData(float[] depthData, int width, int height);
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool InitializeGLEW();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool CheckOpenGLSupport();
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern D3DInfo CheckDirect3DSupport();
    }
    
}
