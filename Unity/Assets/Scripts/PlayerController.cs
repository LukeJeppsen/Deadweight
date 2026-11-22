using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public enum SwingMode
    {
        [Tooltip("Aim where you want to GO. The sword swings that way and drags you with it. Easiest to learn.")]
        PullTowardAim,
        [Tooltip("Original behaviour: the swing throws you the OPPOSITE way (aim backwards to go forwards).")]
        RecoilAwayFromAim
    }

    [Header("Required Core Components")]
    public PlayerInputHandler inputHandler;
    public PlayerMovement movement;

    [Header("Separate Animators")]
    [SerializeField] private Animator bodyAnim;       // Drag your PlayerBody child here
    [SerializeField] private Animator swordAnim;      // Drag your SwordArm child here
    [SerializeField] private Animator particleAnim;      // Legacy slash particle (unused now that the sword layer has its own swoosh)
    [SerializeField] private PlayerAnimationDriver animDriver;   // Plays the body + sword layer clips. Auto-found if empty.

    [Header("Visual Layout Grouping")]
    [SerializeField] private Transform visualsGroup;  // Drag your master Visuals folder here

    [Header("Ground Check Settings")]
    public Transform groundCheck;                     // Drag your IsGrounded child GameObject here
    public float checkRadius = 0.2f;
    public LayerMask groundLayer;
    private bool isGrounded;

    private bool isFacingRight = true;

    [Header("Swing / Attack Settings")]
    [Tooltip("How the sword swing moves you.")]
    public SwingMode swingMode = SwingMode.PullTowardAim;
    public Transform attackPoint;       // Position this child exactly at the character's shoulder joint!
    public float attackOffsetDistance = 1.0f;
    public Vector2 attackSize = new Vector2(1.5f, 1f);
    public LayerMask enemyLayers;
    public float attackCooldown = 0.3f;
    [Tooltip("An attack pressed this early (seconds) before the cooldown ends still goes off.")]
    public float attackBuffer = 0.15f;
    private float nextAttackTime = 0f;
    private float attackBufferUntil = -1f;

    [Header("Jump feel")]
    [Tooltip("You can still jump this long (seconds) after walking off a ledge.")]
    public float coyoteTime = 0.1f;
    [Tooltip("A jump pressed this early (seconds) before landing still goes off.")]
    public float jumpBufferTime = 0.12f;
    private float coyoteTimer;
    private float jumpBufferTimer;

    [Header("Skills")]
    [Tooltip("You can still wall-jump this long (seconds) after your last touch of the wall.")]
    public float wallCoyoteTime = 0.1f;
    [Tooltip("How far in front of you a swing or launch reaches to shatter cracked stone (Breaker).")]
    public float breakReach = 0.6f;
    public Vector2 breakSize = new Vector2(1.6f, 1.1f);

    private PlayerAbilities abilities;
    private ShardPulse pulse;
    private BoxCollider2D body;
    private int wallDir;                 // -1 / +1 while gripping a wall, else 0
    private int lastWallDir = 1;
    private bool wallSliding;
    private float wallCoyoteTimer;

    private bool wasGroundedSfx = true;
    private float airMinVy;              // fastest fall since leaving the ground (for the landing thud)

    private float currentAttackAngle = 0f;
    private Vector2 currentAttackDir = Vector2.right;

    public bool IsGrounded { get { return isGrounded; } }
    public bool IsWallSliding { get { return wallSliding; } }
    public bool FacingRight { get { return isFacingRight; } }
    public PlayerAbilities Abilities { get { return abilities; } }

    /// <summary>Breaker: launching into cracked stone shatters it on contact (called by the movement on a collision).</summary>
    public void OnLaunchHit(Collider2D other, Vector2 point)
    {
        if (abilities == null || !abilities.Has(Skill.Breaker) || other == null) return;
        BreakableTilemap stone = other.GetComponentInParent<BreakableTilemap>();
        if (stone != null) stone.BreakInBox(point, new Vector2(1.2f, 1.2f));
    }

    void Awake()
    {
        if (animDriver == null) animDriver = GetComponent<PlayerAnimationDriver>();
        abilities = GetComponent<PlayerAbilities>();
        if (abilities == null) abilities = gameObject.AddComponent<PlayerAbilities>();
        pulse = GetComponent<ShardPulse>();
        if (pulse == null) pulse = gameObject.AddComponent<ShardPulse>();
        body = GetComponent<BoxCollider2D>();
        SetupBodyCollider();
    }

    /// <summary>
    /// Zero friction (so you never stick to walls or ceilings while pushing into them) and slightly rounded
    /// corners (so the box slides over the seams between tiles instead of catching on them).
    /// The outer size of the collider is unchanged.
    /// </summary>
    private void SetupBodyCollider()
    {
        if (body == null) return;
        PhysicsMaterial2D slick = new PhysicsMaterial2D("PlayerNoFriction");
        slick.friction = 0f;
        slick.bounciness = 0f;
        slick.frictionCombine = PhysicsMaterialCombine2D.Minimum;
        slick.bounceCombine = PhysicsMaterialCombine2D.Minimum;
        body.sharedMaterial = slick;
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.sharedMaterial = slick;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.freezeRotation = true;
        }
        if (body.edgeRadius < 0.01f)
        {
            const float r = 0.04f;
            Vector2 size = body.size;
            body.size = new Vector2(Mathf.Max(0.1f, size.x - 2f * r), Mathf.Max(0.1f, size.y - 2f * r));
            body.edgeRadius = r;
        }
    }

    /// <summary>
    /// Standing on something? A thin box under the whole width of the feet (the old single small circle missed
    /// ledges you were half hanging off, which left you unable to jump).
    /// </summary>
    private bool CheckGrounded()
    {
        if (body != null)
        {
            Bounds b = body.bounds;
            Vector2 size = new Vector2(b.size.x * 0.9f, 0.08f);
            Vector2 center = new Vector2(b.center.x, b.min.y - 0.03f);
            Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f, groundLayer);
            for (int i = 0; i < hits.Length; i++)
                if (hits[i] != null && !hits[i].isTrigger && hits[i].attachedRigidbody != movement.Body) return true;
            return false;
        }
        return Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
    }

    void Update()
    {
        // Guard clause: Safe check to prevent null crashes if components aren't linked yet
        if (movement == null || inputHandler == null) return;

        // Skills feed the movement component
        movement.ricochetEnabled = abilities.Has(Skill.Ricochet);
        movement.bonusLaunches = abilities.Has(Skill.TwinEdge) ? 1 : 0;

        if (movement.IsDashing) return;

        // Ground Check
        // Moving up fast (just jumped / launched) never counts as standing, so a jump can't fire twice
        isGrounded = movement.VelocityY <= 2f && CheckGrounded();
        if (isGrounded)
        {
            movement.RefillLaunches();
            coyoteTimer = coyoteTime;
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }

        UpdateWall();
        UpdateMovementSounds();

        // Idle / walk / run / jump / land for BOTH the body and the sword layer
        float absoluteHorizontalSpeed = Mathf.Abs(inputHandler.MoveInput.x);
        if (animDriver != null) animDriver.Tick(absoluteHorizontalSpeed, isGrounded, movement.VelocityY);

        // Jump with coyote time + input buffering (ground jump first, then wall jump)
        if (inputHandler.JumpTriggered) jumpBufferTimer = jumpBufferTime;
        else jumpBufferTimer -= Time.deltaTime;

        if (jumpBufferTimer > 0f)
        {
            if (coyoteTimer > 0f)
            {
                movement.Jump();
                Sfx.Play("jump");
                jumpBufferTimer = 0f;
                coyoteTimer = 0f;
            }
            else if (wallCoyoteTimer > 0f)
            {
                movement.WallJump(-lastWallDir);
                Sfx.Play("walljump");
                FaceX(-lastWallDir);
                jumpBufferTimer = 0f;
                wallCoyoteTimer = 0f;
            }
        }

        // Aim: 8 directions, snapped so diagonals always feel the same
        currentAttackDir = CalculateAttackDirection();
        currentAttackAngle = Mathf.Atan2(currentAttackDir.y, currentAttackDir.x) * Mathf.Rad2Deg;
        HandleWeaponVisuals();

        // Attack = swing the sword AND get thrown by it
        if (inputHandler.AttackTriggered)
        {
            inputHandler.ResetAttackTrigger();
            attackBufferUntil = Time.time + attackBuffer;
        }
        if (Time.time <= attackBufferUntil && Time.time >= nextAttackTime)
        {
            attackBufferUntil = -1f;
            nextAttackTime = Time.time + attackCooldown;
            SlashAttack();
            TryLaunch();
        }

        // Dedicated dash button: the same launch, without the swing
        if (inputHandler.DashTriggered)
        {
            inputHandler.ResetDashTrigger();
            TryLaunch();
        }

        // Shard Pulse (Interact button)
        if (inputHandler.InteractTriggered)
        {
            inputHandler.ResetInteractTrigger();
            if (abilities.Has(Skill.ShardPulse) && pulse != null) pulse.Trigger();
        }

        CheckSpriteDirection();
    }

    void FixedUpdate()
    {
        // Guard clause safe check
        if (movement == null || inputHandler == null) return;

        if (movement.IsDashing)
        {
            DashBreakCheck();
            return;
        }

        movement.ApplyMovement(inputHandler.MoveInput.x, isGrounded, wallSliding);
        movement.UpdateGravity(inputHandler.JumpHeld, wallSliding);
    }

    // ------------------------------------------------------------------ sounds

    private void UpdateMovementSounds()
    {
        if (!isGrounded) airMinVy = Mathf.Min(airMinVy, movement.VelocityY);
        else
        {
            if (!wasGroundedSfx && airMinVy < -3f)
                Sfx.Play("land", Mathf.Lerp(0.3f, 1f, Mathf.InverseLerp(3f, 22f, -airMinVy)));
            airMinVy = 0f;
        }
        wasGroundedSfx = isGrounded;
        bool scraping = wallSliding && movement.VelocityY < -0.5f && !inputHandler.Locked;
        Sfx.Loop("wallslide", scraping, 0.55f);
    }

    private void OnDisable()
    {
        Sfx.Loop("wallslide", false);
    }

    // ------------------------------------------------------------------ wall grip

    private void UpdateWall()
    {
        wallDir = 0;
        wallSliding = false;
        wallCoyoteTimer -= Time.deltaTime;

        if (!abilities.Has(Skill.WallGrip) || isGrounded || body == null) return;

        Bounds b = body.bounds;
        Vector2 size = new Vector2(0.08f, b.size.y * 0.6f);
        bool left = SolidAt(new Vector2(b.min.x - 0.04f, b.center.y), size);
        bool right = SolidAt(new Vector2(b.max.x + 0.04f, b.center.y), size);

        float ix = inputHandler.MoveInput.x;
        if (right && ix > 0.3f) wallDir = 1;
        else if (left && ix < -0.3f) wallDir = -1;

        if (wallDir != 0)
        {
            lastWallDir = wallDir;
            wallCoyoteTimer = wallCoyoteTime;
            wallSliding = movement.VelocityY <= 0.5f;
        }
    }

    private bool SolidAt(Vector2 center, Vector2 size)
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f, groundLayer);
        for (int i = 0; i < hits.Length; i++)
            if (hits[i] != null && !hits[i].isTrigger && hits[i].attachedRigidbody != movement.Body) return true;
        return false;
    }

    // ------------------------------------------------------------------ breaker

    private void DashBreakCheck()
    {
        if (abilities == null || !abilities.Has(Skill.Breaker)) return;
        Vector2 center = BodyCenter() + movement.DashDirection * 0.9f;
        BreakStoneInBox(center, new Vector2(1.5f, 1.6f), 0f);
    }

    /// <summary>Shatter every patch of cracked stone that the box touches (checked cell by cell, so a glancing hit counts).</summary>
    private void BreakStoneInBox(Vector2 center, Vector2 size, float angle)
    {
        // The box is turned with the swing; test against its upright bounds, which is a touch more generous.
        float rad = angle * Mathf.Deg2Rad;
        float c = Mathf.Abs(Mathf.Cos(rad)), s = Mathf.Abs(Mathf.Sin(rad));
        Vector2 aabb = new Vector2(size.x * c + size.y * s, size.x * s + size.y * c);
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, aabb, 0f, groundLayer);
        for (int i = 0; i < hits.Length; i++)
        {
            BreakableTilemap stone = hits[i] != null ? hits[i].GetComponentInParent<BreakableTilemap>() : null;
            if (stone != null) stone.BreakInBox(center, aabb);
        }
    }

    private Vector2 BodyCenter()
    {
        return body != null ? (Vector2)body.bounds.center : (Vector2)transform.position;
    }

    // ------------------------------------------------------------------ swing

    private void TryLaunch()
    {
        if (!movement.CanDashNow()) return;

        // Pull: go where you aim. Recoil (the old rule): go the opposite way.
        Vector2 dir = swingMode == SwingMode.PullTowardAim ? currentAttackDir : -currentAttackDir;

        // Nothing to launch into: aiming into the floor while standing on it
        if (isGrounded && dir.y < -0.1f) return;

        StartCoroutine(movement.DashRoutine(dir));
        Sfx.Play("launch", 0.8f);
    }

    private Vector2 CalculateAttackDirection()
    {
        Vector2 inputDir = inputHandler.MoveInput;

        // No input: swing the way you are facing
        if (inputDir.sqrMagnitude < 0.04f)
        {
            return isFacingRight ? Vector2.right : Vector2.left;
        }

        float angle = Mathf.Atan2(inputDir.y, inputDir.x) * Mathf.Rad2Deg;
        angle = Mathf.Round(angle / 45f) * 45f;
        float rad = angle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }

    private void HandleWeaponVisuals()
    {
        if (attackPoint == null) return;

        // 1. Let hierarchy parenting track the player position, zero out the local offset
        attackPoint.localPosition = Vector3.zero;

        // 2. Rotate the pivot point based on your 8-way aiming angle
        // Since your sword artwork naturally faces LEFT, we keep the + 180f offset
        attackPoint.localRotation = Quaternion.Euler(0f, 0f, currentAttackAngle + 180f);
    }

    private void SlashAttack()
    {
        // All animation frames face right, so turn to face the aim direction first,
        // then play the slash clip (forward / forward-up / up / forward-down / down) that matches the aim.
        FaceAimDirection();
        if (animDriver != null) animDriver.PlaySlash(currentAttackDir);
        Sfx.Play("swing", 0.9f, 0.1f);

        // Breaker: the swing shatters cracked stone in front of you
        if (abilities.Has(Skill.Breaker))
        {
            BreakStoneInBox(BodyCenter() + currentAttackDir * breakReach, breakSize, currentAttackAngle);
            // and anything you are pressed right up against in that direction
            BreakStoneInBox(BodyCenter() + currentAttackDir * 0.35f, new Vector2(1.3f, 1.3f), 0f);
        }

        if (attackPoint == null) return;

        // Pass the angle directly into the physics engine box overlay
        Collider2D[] hitEnemies = Physics2D.OverlapBoxAll(attackPoint.position, attackSize, currentAttackAngle, enemyLayers);

        foreach (Collider2D enemy in hitEnemies)
        {
            Debug.Log("Hit " + enemy.name);
        }
    }

    private void FaceAimDirection()
    {
        FaceX(currentAttackDir.x);
    }

    private void FaceX(float dirX)
    {
        if (Mathf.Abs(dirX) < 0.01f) return;
        bool right = dirX > 0f;
        if (right != isFacingRight) Flip();
    }

    private void CheckSpriteDirection()
    {
        float moveInputX = inputHandler.MoveInput.x;
        if (Mathf.Abs(moveInputX) > 0.01f)
        {
            if (moveInputX > 0 && !isFacingRight) Flip();
            else if (moveInputX < 0 && isFacingRight) Flip();
        }
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;

        // Flip ONLY the visual subfolder container.
        if (visualsGroup != null)
        {
            Vector3 currentScale = visualsGroup.localScale;
            currentScale.x *= -1;
            visualsGroup.localScale = currentScale;
        }
    }

    // ------------------------------------------------------------------ control from the level

    /// <summary>Freeze / unfreeze the player's input (cutscenes, pickups, pause, death).</summary>
    public void SetInputLocked(bool locked)
    {
        if (inputHandler == null) return;
        inputHandler.Locked = locked;
        if (locked) inputHandler.ClearAll();
    }

    /// <summary>Cancel anything in flight (launch, wall jump, buffered inputs) - used when respawning.</summary>
    public void ResetForRespawn()
    {
        StopAllCoroutines();
        if (movement != null) movement.ResetState();
        coyoteTimer = 0f;
        jumpBufferTimer = 0f;
        wallCoyoteTimer = 0f;
        attackBufferUntil = -1f;
        wallSliding = false;
        wallDir = 0;
        airMinVy = 0f;
        wasGroundedSfx = true;
        Sfx.Loop("wallslide", false);
        if (inputHandler != null) inputHandler.ClearAll();
        if (pulse != null) pulse.ResetPulse();
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.red;

        Matrix4x4 originalMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(attackPoint.position, Quaternion.Euler(0, 0, currentAttackAngle), Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, attackSize);
        Gizmos.matrix = originalMatrix;
    }
}
