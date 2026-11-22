using System;
using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Physics")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float jumpForce = 12f;

    [Header("Gravity Scaling")]
    public float jumpMultiplier = 2f;
    public float fallMultiplier = 3f;
    public float lowJumpMultiplier = 2.5f;

    [Header("Swing Launch (the sword throws you)")]
    [SerializeField] private float dashForce = 24f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;
    [Tooltip("Fraction of the launch speed you keep when the launch ends, so it flows into a jump arc instead of stopping dead.")]
    [SerializeField, Range(0f, 1f)] private float launchCarry = 0.5f;
    [Tooltip("How fast that carried horizontal speed fades while airborne (units/sec per second).")]
    [SerializeField] private float carryDecayAir = 14f;
    [Tooltip("...and while on the ground, so you don't skate.")]
    [SerializeField] private float carryDecayGround = 60f;
    [Tooltip("Swing launches available before you must touch the ground again (Twin Edge adds one).")]
    [SerializeField] private int maxLaunches = 1;

    [Header("Wall Grip (skill)")]
    [Tooltip("Fastest you can slide down a wall you are gripping.")]
    [SerializeField] private float wallSlideSpeed = 2.5f;
    [SerializeField] private float wallJumpHorizontal = 6.5f;
    [SerializeField] private float wallJumpVertical = 8.5f;
    [Tooltip("After a wall jump, steering is locked this long (seconds) so you actually leave the wall.")]
    [SerializeField] private float wallJumpLock = 0.18f;

    [Header("Ricochet (skill)")]
    [Tooltip("Speed you leave a surface with after bouncing off it mid-launch.")]
    [SerializeField] private float ricochetSpeed = 14f;
    [Tooltip("A launch can't bounce in its first moments (so launching while touching a wall doesn't instantly bounce).")]
    [SerializeField] private float ricochetMinTime = 0.04f;

    // set every frame by PlayerController from the skills the player owns
    [HideInInspector] public bool ricochetEnabled;
    [HideInInspector] public int bonusLaunches;

    private bool canDash = true;
    private float carryX;
    private int launchesLeft = 1;
    private float wallLockUntil;
    private float wallLockDir;
    private float dashStartTime;
    private bool pendingBounce;
    private Vector2 bounceNormal;

    public bool IsDashing { get; private set; }
    public Vector2 DashDirection { get; private set; }
    public Rigidbody2D Body { get { return rb; } }
    public float VelocityY { get { return rb.linearVelocity.y; } }
    public Vector2 Velocity { get { return rb.linearVelocity; } }
    public int LaunchesLeft { get { return launchesLeft; } }
    public int MaxLaunches { get { return maxLaunches + bonusLaunches; } }
    public float DashForce { get { return dashForce; } }

    /// <summary>Fired when a launch starts (direction).</summary>
    public event Action<Vector2> Launched;
    /// <summary>Fired when a launch bounces off a surface (surface normal).</summary>
    public event Action<Vector2> Ricocheted;
    /// <summary>Fired when the player leaps off a wall (direction away from the wall).</summary>
    public event Action<float> WallJumped;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        launchesLeft = maxLaunches;
    }

    public void ApplyMovement(float horizontalInput, bool grounded = false, bool wallSliding = false)
    {
        carryX = Mathf.MoveTowards(carryX, 0f, (grounded ? carryDecayGround : carryDecayAir) * Time.fixedDeltaTime);

        float vx;
        if (Time.time < wallLockUntil)
        {
            vx = wallLockDir * wallJumpHorizontal;      // leaving the wall: steering is locked for a moment
        }
        else
        {
            float limit = dashForce * launchCarry + moveSpeed;
            vx = Mathf.Clamp(horizontalInput * moveSpeed + carryX, -limit, limit);
        }

        float vy = rb.linearVelocity.y;
        if (wallSliding && vy <= 0f) vy = -wallSlideSpeed;      // gripping a wall: steady, controlled slide
        rb.linearVelocity = new Vector2(vx, vy);
    }

    public void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    /// <summary>Leap off a wall. awayDir is -1 (leap left) or +1 (leap right).</summary>
    public void WallJump(float awayDir)
    {
        carryX = 0f;
        wallLockDir = awayDir;
        wallLockUntil = Time.time + wallJumpLock;
        rb.linearVelocity = new Vector2(awayDir * wallJumpHorizontal, wallJumpVertical);
        if (WallJumped != null) WallJumped(awayDir);
    }

    public void UpdateGravity(bool isJumpHeld, bool wallSliding = false)
    {
        // Gripping a wall: no gravity, the slide speed is set directly in ApplyMovement
        if (wallSliding && rb.linearVelocity.y <= 0f)
        {
            rb.gravityScale = 0f;
        }
        // Case 1: Falling down
        else if (rb.linearVelocity.y < 0)
        {
            rb.gravityScale = fallMultiplier;
        }
        // Case 2: Jumping up, but player released Space early (Short Jump)
        else if (rb.linearVelocity.y > 0 && !isJumpHeld)
        {
            rb.gravityScale = lowJumpMultiplier;
        }
        // Case 3: Jumping up, and player is still holding Space (Full Jump)
        else if (rb.linearVelocity.y > 0 && isJumpHeld)
        {
            rb.gravityScale = jumpMultiplier;
        }
        // Case 4: Grounded or stationary
        else
        {
            rb.gravityScale = 1f;
        }
    }

    /// <summary>Called whenever the player is standing on something.</summary>
    public void RefillLaunches()
    {
        if (!IsDashing) launchesLeft = MaxLaunches;
    }

    /// <summary>Wind / updrafts: push the velocity toward a target without ever slowing a faster player.</summary>
    public void ApplyWind(Vector2 targetVelocity, float accel)
    {
        if (IsDashing) return;
        float step = accel * Time.fixedDeltaTime;
        Vector2 v = rb.linearVelocity;
        if (targetVelocity.y > 0f && v.y < targetVelocity.y) v.y = Mathf.Min(targetVelocity.y, v.y + step);
        else if (targetVelocity.y < 0f && v.y > targetVelocity.y) v.y = Mathf.Max(targetVelocity.y, v.y - step);
        rb.linearVelocity = v;
        if (targetVelocity.x > 0f && carryX < targetVelocity.x) carryX = Mathf.Min(targetVelocity.x, carryX + step);
        else if (targetVelocity.x < 0f && carryX > targetVelocity.x) carryX = Mathf.Max(targetVelocity.x, carryX - step);
    }

    /// <summary>Launch the player along dashDirection (already normalised). The sword's swing is what throws them.</summary>
    public IEnumerator DashRoutine(Vector2 dashDirection)
    {
        canDash = false;
        IsDashing = true;
        pendingBounce = false;
        DashDirection = dashDirection;
        dashStartTime = Time.time;
        launchesLeft = Mathf.Max(0, launchesLeft - 1);

        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;
        carryX = 0f;
        wallLockUntil = 0f;
        rb.linearVelocity = dashDirection * dashForce;
        if (Launched != null) Launched(dashDirection);

        float t = 0f;
        while (t < dashDuration && !pendingBounce)
        {
            yield return null;
            t += Time.deltaTime;
        }

        Vector2 exit;
        bool bounced = pendingBounce;
        if (bounced)
        {
            // Ricochet: the blade rings off the surface. You rebound and the blade is ready again.
            Vector2 reflected = Vector2.Reflect(dashDirection, bounceNormal).normalized;
            exit = reflected * ricochetSpeed;
            launchesLeft = Mathf.Min(MaxLaunches, launchesLeft + 1);
            pendingBounce = false;
            if (Ricocheted != null) Ricocheted(bounceNormal);
        }
        else
        {
            // Keep part of the speed: it bleeds off into a natural arc instead of a dead stop.
            exit = dashDirection * dashForce * launchCarry;
        }
        rb.gravityScale = originalGravity;
        rb.linearVelocity = exit;
        carryX = exit.x;
        IsDashing = false;

        if (!bounced) yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    public bool CanDashNow()
    {
        return canDash && !IsDashing && launchesLeft > 0;
    }

    private PlayerController owner;

    private void OnCollisionEnter2D(Collision2D c) { LaunchContact(c); TryRicochet(c); }
    private void OnCollisionStay2D(Collision2D c) { LaunchContact(c); TryRicochet(c); }

    /// <summary>Breaker: a launch that slams into cracked stone shatters it.</summary>
    private void LaunchContact(Collision2D c)
    {
        if (!IsDashing || c.contactCount == 0) return;
        if (owner == null) owner = GetComponent<PlayerController>();
        if (owner != null) owner.OnLaunchHit(c.collider, c.GetContact(0).point);
    }

    private void TryRicochet(Collision2D c)
    {
        if (!ricochetEnabled || !IsDashing || pendingBounce) return;
        if (Time.time - dashStartTime < ricochetMinTime) return;

        for (int i = 0; i < c.contactCount; i++)
        {
            ContactPoint2D cp = c.GetContact(i);
            // normal points away from the surface we hit; we must be flying INTO it
            if (Vector2.Dot(DashDirection, cp.normal) < -0.35f)
            {
                bounceNormal = cp.normal;
                pendingBounce = true;
                return;
            }
        }
    }

    /// <summary>Clears every launch / wall-jump state (used when respawning).</summary>
    public void ResetState()
    {
        IsDashing = false;
        canDash = true;
        pendingBounce = false;
        carryX = 0f;
        wallLockUntil = 0f;
        launchesLeft = MaxLaunches;
        if (rb != null)
        {
            rb.gravityScale = 1f;
            rb.linearVelocity = Vector2.zero;
        }
    }
}
