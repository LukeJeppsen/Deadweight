using UnityEngine;

/// <summary>
/// What the Cinemachine camera actually follows. It trails the player with a little look-ahead (the way you are
/// moving), leads downward when you fall fast, stays inside the level (so you never see past the edge),
/// and adds screen shake. Cinemachine then smooths the motion.
/// </summary>
[DefaultExecutionOrder(-500)]
public class CameraTarget : MonoBehaviour
{
    public static CameraTarget Instance;

    public Transform follow;
    [Tooltip("The area of the world the camera may show (world units).")]
    public Rect bounds = new Rect(-1000f, -1000f, 2000f, 2000f);
    public Vector2 viewHalfSize = new Vector2(10f, 5.625f);
    public float lookAheadX = 1.8f;
    public float lookAheadSmooth = 5f;
    [Tooltip("Camera sits this far above the player.")]
    public float yOffset = 1.4f;
    public float fallLead = 0.12f;
    public float fallLeadMax = 3f;

    private Rigidbody2D body;
    private float lookX;
    private float shake;
    private bool snap = true;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static void Shake(float magnitude)
    {
        if (Instance != null && GameSettings.ScreenShake) Instance.shake = Mathf.Max(Instance.shake, magnitude);
    }

    /// <summary>Jump straight to the player (after a respawn or teleport).</summary>
    public void Snap() { snap = true; }

    private void LateUpdate()
    {
        if (follow == null) return;
        if (body == null) body = follow.GetComponent<Rigidbody2D>();

        float vx = body != null ? body.linearVelocity.x : 0f;
        float vy = body != null ? body.linearVelocity.y : 0f;

        float wantLook = Mathf.Clamp(vx / 8f, -1f, 1f) * lookAheadX;
        if (snap) lookX = wantLook;
        else lookX = Mathf.Lerp(lookX, wantLook, 1f - Mathf.Exp(-lookAheadSmooth * Time.deltaTime));

        float lead = Mathf.Clamp(vy * fallLead, -fallLeadMax, fallLeadMax * 0.4f);
        Vector2 p = new Vector2(follow.position.x + lookX, follow.position.y + yOffset + lead);

        p.x = ClampAxis(p.x, bounds.xMin + viewHalfSize.x, bounds.xMax - viewHalfSize.x, bounds.center.x);
        p.y = ClampAxis(p.y, bounds.yMin + viewHalfSize.y, bounds.yMax - viewHalfSize.y, bounds.center.y);

        if (shake > 0.001f)
        {
            Vector2 s = Random.insideUnitCircle * shake;
            p += new Vector2(Mathf.Round(s.x * 16f) / 16f, Mathf.Round(s.y * 16f) / 16f);
            shake = Mathf.Max(0f, shake - Time.deltaTime * 1.6f);
        }

        transform.position = new Vector3(p.x, p.y, 0f);
        snap = false;
    }

    private static float ClampAxis(float v, float min, float max, float center)
    {
        if (max < min) return center;       // level smaller than the screen on this axis
        return Mathf.Clamp(v, min, max);
    }
}
