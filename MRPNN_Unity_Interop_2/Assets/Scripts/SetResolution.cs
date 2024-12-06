using UnityEngine;

public class SetResolution : MonoBehaviour
{
    // Set your target width and height here
    public static int targetResolution = 1024; // Example for 1024x1024 resolution

    void Start()
    {
        // Set the resolution with no fullscreen and no resizable window
        Screen.SetResolution(targetResolution, targetResolution, false);
    }
}