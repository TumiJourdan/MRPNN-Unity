using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

public class FPSTracker : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Custom path where the FPS stats file will be saved. Leave empty to use default path.")]
    [SerializeField] private string customFilePath = "";

    private List<float> fpsValues = new List<float>();
    private float startTime;
    private bool isRecording = true;
    private string outputPath;
    public float TEST_DURATION = 10f;

    private void Start()
    {
        startTime = Time.time;
        string fileName = "performance_tests.csv";

        if (string.IsNullOrEmpty(customFilePath))
        {
            outputPath = Path.Combine(Application.persistentDataPath, fileName);
        }
        else
        {
            Directory.CreateDirectory(customFilePath);
            outputPath = Path.Combine(customFilePath, fileName);
        }

        if (!File.Exists(outputPath))
        {
            File.WriteAllText(outputPath, "Timestamp,Test Setup,Average FPS\n");
        }

        Debug.Log($"Test started. Recording for {TEST_DURATION} seconds...");
    }

    public void UpdateTime(float renderTime,string testName)
    {
        if (!isRecording) return;

        float currentTime = Time.time;
        float timeElapsed = currentTime - startTime;

        if (timeElapsed <= TEST_DURATION)
        {
            float currentFps = renderTime;
            fpsValues.Add(currentFps);
        }
        else if (isRecording)
        {
            float averageFps = fpsValues.Average();
            string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // Use invariant culture to ensure period as decimal separator
            string formattedFps = averageFps.ToString("F2", CultureInfo.InvariantCulture);
            string result = $"{timestamp},{testName},{formattedFps}\n";

            try
            {
                File.AppendAllText(outputPath, result);
                Debug.Log($"Test '{testName}' completed:\nAverage FPS: {formattedFps}\nResults saved to: {outputPath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error writing results: {e.Message}");
            }

            isRecording = false;
        }
    }
}