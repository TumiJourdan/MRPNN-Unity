using UnityEngine;
using UnityEngine.UI;

public class UIManagerScr : MonoBehaviour
{
    // References to the sliders
    public Slider slider_col;
    public Slider slider_alpha;
    public Slider slider_lightDir;

    public Light directionalLight;

    public GetCloud cloudScr;

    private float previousSliderValue;
    // Update is called once per frame
    private void Start()
    {
        slider_col.minValue = 0f;
        slider_col.maxValue = 1f;
        slider_col.onValueChanged.AddListener(UpdateLightColor); // Add listener for real-time updates'

        slider_lightDir.onValueChanged.AddListener(UpdateLightDirection);
        previousSliderValue = slider_lightDir.value;
    }
    void Update()
    {
        // Read the values of the sliders
        float alpha = slider_alpha.value;
        float light_direction = slider_lightDir.value;

        // Output the slider values to the console (or use them however you like)
        cloudScr.alpha = alpha;
    }

    void UpdateLightDirection(float sliderValue)
    {
        // Calculate the delta rotation from the slider
        float deltaValue = sliderValue - previousSliderValue;

        // Apply the delta rotation to the light's transform
        directionalLight.transform.Rotate(Vector3.right, deltaValue, Space.Self);

        // Update the previous slider value for the next frame
        previousSliderValue = sliderValue;
    }
    void UpdateLightColor(float value)
    {
        // Calculate the color based on the slider value
        Color color;

        if (value <= 0.9f)
        {
            // Convert slider value to a hue (0 to 1 range)
            float hue = value / 0.9f; // Normalize to stay in [0, 1) for colors
            color = Color.HSVToRGB(hue, 1f, 1f); // Full saturation and value for vibrant colors
        }
        else
        {
            // When slider value is near the end, blend towards white
            float whiteBlend = (value - 0.9f) / 0.1f; // Normalize the range [0.9, 1] to [0, 1]
            color = Color.Lerp(Color.red, Color.white, whiteBlend); // Blend red to white
        }

        // Update the Directional Light's color
        if (directionalLight != null)
        {
            directionalLight.color = color;
        }
    }
}
