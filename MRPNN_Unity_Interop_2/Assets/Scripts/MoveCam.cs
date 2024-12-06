using UnityEngine;

public class MoveCam : MonoBehaviour
{
    public float speed = 5f;       // Speed of orbiting
    public float adjustmentSpeed = 2f;
    public float distance = 10f;   // Distance from the origin
    public Vector3 target = Vector3.zero; // Orbit center (default is the origin)

    private float angleX = 0f;     // X-axis rotation angle
    private float angleY = 0f;     // Y-axis rotation angle

    public bool staticCam = false;

    void Start()
    {
        // Set initial camera position at the specified distance along the Z-axis
        transform.position = new Vector3(0, 0, -distance);
        transform.LookAt(target);

        if (staticCam)
        {
            angleX = 60;
            angleY -= 0;

            // Clamp vertical rotation to avoid flipping
            angleY = Mathf.Clamp(angleY, -89f, 89f);

            // Calculate new position and rotation
            Quaternion rotation = Quaternion.Euler(angleY, angleX, 0);
            Vector3 direction = rotation * Vector3.forward * distance;

            // Update the camera's position and look at the target
            transform.position = target - direction;
            transform.LookAt(target);
        }
    }

    void Update()
    {
        if (staticCam)
            return;

        if (Input.GetMouseButton(1))
        {
            // Accumulate rotation based on mouse input
            angleX += Input.GetAxis("Mouse X") * speed;
            angleY -= Input.GetAxis("Mouse Y") * speed;

            // Clamp vertical rotation to avoid flipping
            angleY = Mathf.Clamp(angleY, -89f, 89f);

            // Calculate new position and rotation
            Quaternion rotation = Quaternion.Euler(angleY, angleX, 0);
            Vector3 direction = rotation * Vector3.forward * distance;

            // Update the camera's position and look at the target
            transform.position = target - direction;
            transform.LookAt(target);
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0f)
        {
            // Adjust the value based on the scroll direction
            distance -= scroll * adjustmentSpeed;

            // Ensure the value doesn't go below 0
            distance = Mathf.Max(distance, 0.5f);
            Vector3 direction = transform.rotation * Vector3.forward * distance;

            // Update the camera's position and look at the target
            transform.position = target - direction;
            transform.LookAt(target);
        }

    }
}