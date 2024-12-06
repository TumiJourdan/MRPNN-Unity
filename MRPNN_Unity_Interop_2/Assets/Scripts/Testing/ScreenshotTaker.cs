using UnityEngine;
using System;

public class ScreenshotTaker : MonoBehaviour
{
    public bool timed = false;
    public float timeInterval = 1.0f;
    private float elapsedTime = 0;
    public string res = "512";

    void Update()
    {
        elapsedTime += Time.deltaTime;
        if (timed == false && Input.GetKeyDown(KeyCode.Space))
        {
            TakeScreenshot();
        }
        if (timed && elapsedTime > timeInterval)
        {
            elapsedTime = 0;
            TakeScreenshot();
        }
    }

    void TakeScreenshot()
    {

        string screenshotFileName = res +"_"+ System.DateTime.Now.ToString("HH-mm-ss") + ".png";
        ScreenCapture.CaptureScreenshot(screenshotFileName);
        Debug.Log("Screenshot taken: " + screenshotFileName);
    }
}
