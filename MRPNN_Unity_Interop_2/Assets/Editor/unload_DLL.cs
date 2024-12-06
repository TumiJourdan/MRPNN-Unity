using UnityEditor;
using UnityEngine;
using System.Runtime.InteropServices;
using System;
public static class DllUnloader
{
    // Import FreeLibrary and GetModuleHandle from kernel32.dll
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeLibrary(IntPtr hModule);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    // Function to unload the DLL
    public static void UnloadDll(string dllName)
    {
        IntPtr hModule = GetModuleHandle("VolumeRender.dll");
        if (hModule != IntPtr.Zero)
        {
            bool result = FreeLibrary(hModule);
            if (result)
            {
                Debug.Log("DLL successfully unloaded.");
            }
            else
            {
                Debug.LogError("Failed to unload the DLL.");
            }
        }
        else
        {
            Debug.LogError("DLL not found.");
        }
    }

    // Add a menu item in Unity to unload the DLL outside of play mode
    [MenuItem("Tools/Unload DLL")]
    public static void UnloadDllMenuItem()
    {
        UnloadDll("YourDllName.dll");  // Replace with the actual DLL name
    }
}