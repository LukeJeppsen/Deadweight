using UnityEngine;
using System.Collections;

public class RotatingPlatform : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 5f;  // Higher numbers = faster flip
    [SerializeField] private float waitDuration = 3f;   // Time spent sitting flat or vertical

    private Rigidbody2D rb;


    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // Start the endless cycle of flipping
        StartCoroutine(RotationCycleRoutine());
    }

    private IEnumerator RotationCycleRoutine()
    {
        while (true)
        {
            // Wait for the specified time before starting a rotation
            yield return new WaitForSeconds(waitDuration);

            // Calculate our next target orientation (Subtracting 90 degrees = Clockwise)
            float targetZRotation = transform.eulerAngles.z - 90f;
            Quaternion targetQuaternion = Quaternion.Euler(0f, 0f, targetZRotation);

            Quaternion startQuaternion = transform.rotation;
            float t = 0f;

            // Smoothly interpolate the rotation angle using Lerp
            while (t < 1f)
            {
                t += Time.deltaTime * rotationSpeed;

                // For Kinematic Rigidbodies, use MoveRotation for proper physics updates
                rb.MoveRotation(Quaternion.Lerp(startQuaternion, targetQuaternion, t));

                yield return null; // Wait for the next frame
            }

            // Ensure we snap perfectly to the exact target rotation angles
            rb.MoveRotation(targetQuaternion);
        }
    }
}