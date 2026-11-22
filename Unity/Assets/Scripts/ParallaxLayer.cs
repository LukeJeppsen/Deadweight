using UnityEngine;

/// <summary>
/// Scrolls a background layer at a fraction of the camera's movement.
/// factor 1 = glued to the camera (sky), 0 = fixed in the world (foreground).
/// Set tileWidth to the layer's repeat width in world units (320 px / 8 PPU = 40) to wrap horizontally.
/// </summary>
[DefaultExecutionOrder(1000)]
public class ParallaxLayer : MonoBehaviour
{
    [Range(0f, 1f)] public float factorX = 0.5f;
    [Range(0f, 1f)] public float factorY = 0.5f;
    [Tooltip("World-space repeat width. 0 disables horizontal wrapping.")]
    public float tileWidth = 0f;
    [Tooltip("Offset from the camera position at start.")]
    public Vector2 offset;

    private Transform cam;
    private Vector3 startCam;
    private Vector3 startPos;

    private void Start()
    {
        Camera main = Camera.main;
        if (main == null)
        {
            enabled = false;
            return;
        }
        cam = main.transform;
        startCam = cam.position;
        startPos = new Vector3(startCam.x + offset.x, startCam.y + offset.y, transform.position.z);
    }

    private void LateUpdate()
    {
        Vector3 d = cam.position - startCam;
        float x = startPos.x + d.x * factorX;
        float y = startPos.y + d.y * factorY;
        if (tileWidth > 0f)
        {
            x += Mathf.Round((cam.position.x - x) / tileWidth) * tileWidth;
        }
        transform.position = new Vector3(x, y, startPos.z);
    }
}
