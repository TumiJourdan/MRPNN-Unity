using UnityEngine;
using System.IO;

public class HDRISaver : MonoBehaviour
{
    public void SaveHDRI(Texture2D texture)
    {
        // Ensure the texture is readable
        if (!texture.isReadable)
        {
            Debug.LogError("Texture is not readable. Please set it to readable in the importer.");
            return;
        }

        // Get pixel data as float
        Color[] pixels = texture.GetPixels();
        float[] hdrData = new float[pixels.Length * 4];

        for (int i = 0; i < pixels.Length; i++)
        {
            hdrData[i * 4 + 0] = pixels[i].r;
            hdrData[i * 4 + 1] = pixels[i].g;
            hdrData[i * 4 + 2] = pixels[i].b;
            hdrData[i * 4 + 3] = pixels[i].a; // Optional alpha
        }

        // Path to save HDRI
        string path = Path.Combine(Application.streamingAssetsPath, "hdr_texture.hdr");

        // Write HDR file (use your preferred method or a library like TinyEXR)
        File.WriteAllBytes(path, FloatArrayToByteArray(hdrData));

        Debug.Log("HDRI saved to: " + path);
    }

    private byte[] FloatArrayToByteArray(float[] floatArray)
    {
        byte[] byteArray = new byte[floatArray.Length * 4];
        System.Buffer.BlockCopy(floatArray, 0, byteArray, 0, byteArray.Length);
        return byteArray;
    }
}