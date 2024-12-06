using System;
using UnityEngine;
using System.Runtime.InteropServices;

namespace Interop
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

        // Returns the magnitude (length) of the vector
        public float Magnitude()
        {
            return (float)Math.Sqrt(x * x + y * y + z * z);
        }

        // Normalizes this vector (modifies the current instance)
        public void Normalize()
        {
            float magnitude = Magnitude();
            if (magnitude > 1e-6f) // Check for non-zero magnitude to avoid division by zero
            {
                x /= magnitude;
                y /= magnitude;
                z /= magnitude;
            }
        }

        // Returns a normalized copy of this vector (doesn't modify the current instance)
        public Float3 Normalized()
        {
            float magnitude = Magnitude();
            if (magnitude > 1e-6f) // Check for non-zero magnitude to avoid division by zero
            {
                return new Float3(
                    x / magnitude,
                    y / magnitude,
                    z / magnitude
                );
            }
            return this; // Return the original vector if magnitude is too close to zero
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Int2
    {
        public int x;
        public int y;
    }
    public class Cuda_Interop
    {
        private const string DllName = "VolumeRender.dll";

        [DllImport(DllName)]
        public static extern void SetTextureFromUnity(System.IntPtr updateCallback);

        [DllImport(DllName)]
        public static extern void RegisterTexture2D(System.IntPtr texture);

        [DllImport(DllName)]
        public static extern void initialize_memory(Int2 size, int resolution = 512);
        [DllImport(DllName)]
        public static extern void initialize_memory_path(
                                    Int2 size,
                                    [MarshalAs(UnmanagedType.LPStr)] string path,
                                    int targetResolution);

        [DllImport(DllName)]
        public static extern void set_volume_datas();
        //cuda_run_render(float3 origin, float3 up, float3 right, float3 lightDir, float3 lightColor, float alpha, int2 size)
        [DllImport(DllName)]
        public static extern void cuda_run_render(Float3 origin, Float3 up, Float3 right, Float3 lightDir, Float3 lightColor, float alpha, Int2 size);

        [DllImport(DllName)]
        public static extern void clean_memory();

        [DllImport(DllName)]
        public static extern void set_raymarch_step(int stepCount);

        [DllImport(DllName)]
        public static extern void setHDRIInterface(string path);
    }


}
