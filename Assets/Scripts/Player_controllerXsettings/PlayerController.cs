using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    // ══════════════════════════════════════════════════════════════
    // REFERENCES
    // ══════════════════════════════════════════════════════════════

    [Header("References")]
    public PlayerSettings settings;
    public Transform spriteTransform;
    [Tooltip("Auto-detects child named 'Visuals' if not assigned")]
    public SpriteRenderer spriteRenderer;
    [Tooltip("Auto-detects from spriteRenderer's GameObject if not assigned")]
    public Animator animator;

    // ══════════════════════════════════════════════════════════════
    // FORM SWITCHING
    // ══════════════════════════════════════════════════════════════

    [Header("Form Switching")]
    public bool enableFormSwitch = true;
    public PlayerSettings fireSettings;
    public PlayerSettings iceSettings;
    public bool startAsIce = false;

    [Header("Form Sprites")]
    public Sprite fireSprite;
    public Sprite iceSprite;

    // Events (subscribe for visual feedback, particles, sounds, etc.)
    public System.Action<bool> OnFormSwitch; // true = ice, false = fire

    // ══════════════════════════════════════════════════════════════
    // DEBUG
    // ══════════════════════════════════════════════════════════════

    [Header("Debug")]
    public bool showGizmos = true;

    // ══════════════════════════════════════════════════════════════
    // PUBLIC STATE (for external script access)
    // ══════════════════════════════════════════════════════════════

    // Form state
    public bool IsIceForm { get; private set; }
    public bool IsFireForm => !IsIceForm;

    // Movement state
    public bool IsGrounded { get; private set; }
    public bool IsTouchingWall { get; private set; }
    public bool IsWallSliding { get; private set; }
    public bool IsWallSticking { get; private set; }
    public bool IsDashing { get; private set; }
    public int FacingDirection { get; private set; } = 1;

    // Advanced state
    public bool IsApexHanging { get; private set; }
    public bool IsWallJumpLocked { get; private set; }
    public bool IsFalling => rb.linearVelocity.y < 0 && !IsGrounded;
    public bool IsRising => rb.linearVelocity.y > 0 && !IsGrounded;
    public int AirDashesRemaining { get; private set; }

    public float bonusSlopeSpeed = 1;

    // ══════════════════════════════════════════════════════════════
    // PRIVATE - Components
    // ══════════════════════════════════════════════════════════════

    private Rigidbody rb;

    // ══════════════════════════════════════════════════════════════
    // PRIVATE - Input
    // ══════════════════════════════════════════════════════════════

    private Vector2 moveInput;
    private bool jumpHeld;
    private bool jumpPressedThisFrame;

    // ══════════════════════════════════════════════════════════════
    // PRIVATE - Internal State
    // ══════════════════════════════════════════════════════════════

    private int wallDirection;
    private bool hasDoubleJump;
    private float lastGroundedTime;
    private float lastWallTime;
    private int lastWallDirection;

    // Dash
    private float dashTimer;
    private float dashCooldownTimer;
    private bool isAirDash;
    private Vector2 dashDirection;

    // Wall mechanics
    private float wallStickTimer;
    private float wallJumpLockTimer;

    // Flip
    private float flipAngle;

    // ══════════════════════════════════════════════════════════════
    // UNITY LIFECYCLE
    // ══════════════════════════════════════════════════════════════

    void Awake()
    {
        // Rigidbody setup (3D physics)
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Auto-detect sprite transform
        if (spriteTransform == null)
            spriteTransform = transform;

        // Auto-detect SpriteRenderer on "Visuals" child
        if (spriteRenderer == null)
        {
            Transform visuals = transform.Find("Visuals");
            if (visuals != null)
                spriteRenderer = visuals.GetComponent<SpriteRenderer>();
        }

        // Auto-detect Animator from spriteRenderer's GameObject
        if (animator == null && spriteRenderer != null)
            animator = spriteRenderer.GetComponent<Animator>();

        // Initialize form
        IsIceForm = startAsIce;
        if (enableFormSwitch && iceSettings != null && fireSettings != null)
        {
            settings = IsIceForm ? iceSettings : fireSettings;
            UpdateFormSprite();
        }

        // Initialize air dashes
        AirDashesRemaining = settings != null ? settings.maxAirDashes : 1;
    }

    void Update()
    {
        CheckGrounded();
        CheckWalls();
        HandleFlip();
        UpdateTimers();

        // Check for dash cancel with jump
        if (jumpPressedThisFrame && IsDashing && settings.enableDashCancelWithJump)
        {
            CancelDash();
            TryJump();
        }

        jumpPressedThisFrame = false;
    }

    void FixedUpdate()
    {
        if (IsDashing)
            HandleDash();
        else
        {
            ApplyGravity();
            HandleMovement();
            HandleWallSlide();
        }
    }

    // ══════════════════════════════════════════════════════════════
    // INPUT CALLBACKS (Connect in PlayerInput component)
    // ══════════════════════════════════════════════════════════════

    public void OnMove(InputAction.CallbackContext ctx) => moveInput = ctx.ReadValue<Vector2>();

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            jumpHeld = true;
            jumpPressedThisFrame = true;

            // Don't process jump here if we might cancel a dash
            if (!(IsDashing && settings.enableDashCancelWithJump))
                TryJump();
        }
        else if (ctx.canceled)
        {
            jumpHeld = false;
            CutJump();
        }
    }

    public void OnDash(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
            TryDash();
    }

    public void OnSwitchForm(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
            SwitchForm();
    }

    // ══════════════════════════════════════════════════════════════
    // FORM SWITCHING
    // ══════════════════════════════════════════════════════════════

    public void SwitchForm()
    {
        if (!enableFormSwitch || iceSettings == null || fireSettings == null) return;

        IsIceForm = !IsIceForm;
        settings = IsIceForm ? iceSettings : fireSettings;
        UpdateFormSprite();
        OnFormSwitch?.Invoke(IsIceForm);
    }

    public void SetIceForm()
    {
        if (iceSettings == null) return;

        IsIceForm = true;
        settings = iceSettings;
        UpdateFormSprite();
        OnFormSwitch?.Invoke(true);
    }

    public void SetFireForm()
    {
        if (fireSettings == null) return;

        IsIceForm = false;
        settings = fireSettings;
        UpdateFormSprite();
        OnFormSwitch?.Invoke(false);
    }

    private void UpdateFormSprite()
    {
        if (spriteRenderer == null) return;

        Sprite targetSprite = IsIceForm ? iceSprite : fireSprite;
        if (targetSprite != null)
            spriteRenderer.sprite = targetSprite;
    }

    // ══════════════════════════════════════════════════════════════
    // DETECTION
    // ══════════════════════════════════════════════════════════════

    void CheckGrounded()
    {
        bool wasGrounded = IsGrounded;
        Vector3 pos = transform.position + (Vector3)settings.groundCheckOffset;
        IsGrounded = Physics.CheckBox(pos, new Vector3(settings.groundCheckSize.x / 2f, settings.groundCheckSize.y / 2f, 0.1f), Quaternion.identity, settings.groundLayer);

        if (IsGrounded)
        {
            lastGroundedTime = Time.time;

            // Reset abilities on landing
            if (!wasGrounded)
            {
                hasDoubleJump = true;

                // Refresh air dashes
                if (settings.dashRefreshOnGround)
                    AirDashesRemaining = settings.maxAirDashes;
            }
        }
    }

    void CheckWalls()
    {
        bool right = Physics.Raycast(transform.position, Vector3.right, settings.wallCheckDistance, settings.wallLayer);
        bool left = Physics.Raycast(transform.position, Vector3.left, settings.wallCheckDistance, settings.wallLayer);
        IsTouchingWall = right || left;
        wallDirection = right ? 1 : (left ? -1 : 0);

        // Track last wall touch for wall coyote time
        if (IsTouchingWall)
        {
            lastWallTime = Time.time;
            lastWallDirection = wallDirection;
        }
    }

    // ══════════════════════════════════════════════════════════════
    // MOVEMENT
    // ══════════════════════════════════════════════════════════════

    void HandleMovement()
    {
        if (!settings.enableMovement) return;

        float targetSpeed = moveInput.x * settings.moveSpeed;
        float currentVelX = rb.linearVelocity.x;
        float newVelX;

        if (IsGrounded)
        {
            // Ground movement
            newVelX = CalculateGroundMovement(targetSpeed, currentVelX);
        }
        else
        {
            // Air movement
            newVelX = CalculateAirMovement(targetSpeed, currentVelX);
        }

        rb.linearVelocity = new Vector3(newVelX, rb.linearVelocity.y, 0);
    }

    float CalculateGroundMovement(float target, float current)
    {
        // Instant movement if acceleration/deceleration disabled
        if (Mathf.Abs(moveInput.x) > 0.1f)
        {
            if (!settings.enableAcceleration)
                return target;

            float accel = Mathf.Abs(target) > Mathf.Abs(current) ? settings.acceleration : settings.deceleration;
            return Mathf.MoveTowards(current, target, accel * Time.fixedDeltaTime);
        }
        else
        {
            if (!settings.enableDeceleration)
                return 0f;

            return Mathf.MoveTowards(current, 0f, settings.deceleration * Time.fixedDeltaTime);
        }
    }

    float CalculateAirMovement(float target, float current)
    {
        if (!settings.enableAirControl)
            return current;

        // Apply wall jump lock
        float controlMultiplier = settings.airControlMultiplier;
        if (IsWallJumpLocked && settings.enableWallJumpLock)
            controlMultiplier *= settings.wallJumpLockControlMultiplier;

        float adjustedTarget = moveInput.x * settings.moveSpeed * controlMultiplier;

        if (Mathf.Abs(moveInput.x) > 0.1f)
        {
            if (!settings.enableAirAcceleration)
                return adjustedTarget;

            float accel = Mathf.Abs(adjustedTarget) > Mathf.Abs(current) ? settings.airAcceleration : settings.airDeceleration;
            return Mathf.MoveTowards(current, adjustedTarget, accel * Time.fixedDeltaTime);
        }
        else
        {
            if (!settings.enableAirDeceleration)
                return current;

            return Mathf.MoveTowards(current, 0f, settings.airDeceleration * Time.fixedDeltaTime);
        }
    }

    void ApplyGravity()
    {
        if (IsGrounded && rb.linearVelocity.y <= 0) return;

        float gravityMultiplier = 1f;

        // Check for apex state (near peak of jump)
        float verticalVel = rb.linearVelocity.y;
        IsApexHanging = settings.enableApexModifier && Mathf.Abs(verticalVel) < settings.apexThreshold && verticalVel > -0.1f;

        if (IsApexHanging)
        {
            gravityMultiplier = settings.apexGravityMultiplier;
        }
        else if (verticalVel < 0 && settings.enableFallGravityMultiplier)
        {
            // Falling - apply fall gravity multiplier
            gravityMultiplier = settings.fallGravityMultiplier;
        }

        rb.linearVelocity += new Vector3(0, Physics.gravity.y * settings.gravityScale * gravityMultiplier * Time.fixedDeltaTime, 0);

        // Cap fall speed
        if (rb.linearVelocity.y < -settings.maxFallSpeed)
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, -settings.maxFallSpeed, 0);
    }

    // ══════════════════════════════════════════════════════════════
    // JUMP
    // ══════════════════════════════════════════════════════════════

    void TryJump()
    {
        // Wall jump (with wall coyote time)
        bool canWallJump = IsTouchingWall || (Time.time - lastWallTime <= settings.wallCoyoteTime);
        if (settings.enableWallJump && canWallJump && !IsGrounded)
        {
            int wallDir = IsTouchingWall ? wallDirection : lastWallDirection;
            rb.linearVelocity = new Vector3(-wallDir * settings.wallJumpPushForce, settings.wallJumpForce, 0);
            FacingDirection = -wallDir;
            hasDoubleJump = true;

            // Refresh air dashes on wall jump
            if (settings.dashRefreshOnGround)
                AirDashesRemaining = settings.maxAirDashes;

            // Start wall jump lock
            if (settings.enableWallJumpLock)
            {
                wallJumpLockTimer = settings.wallJumpLockDuration;
                IsWallJumpLocked = true;
            }

            lastWallTime = 0; // Consume wall coyote
            wallStickTimer = 0; // Reset wall stick
            return;
        }

        // Ground jump (with coyote time)
        if (settings.enableJump && Time.time - lastGroundedTime <= settings.coyoteTime)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, settings.jumpForce, 0);
            lastGroundedTime = 0;
            return;
        }

        // Double jump
        if (settings.enableDoubleJump && hasDoubleJump && !IsGrounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, settings.jumpForce * settings.doubleJumpMultiplier, 0);
            hasDoubleJump = false;
        }
    }

    void CutJump()
    {
        if (!settings.enableVariableJump) return;

        if (rb.linearVelocity.y > 0)
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y * settings.jumpCutMultiplier, 0);
    }

    // ══════════════════════════════════════════════════════════════
    // WALL SLIDE
    // ══════════════════════════════════════════════════════════════

    void HandleWallSlide()
    {
        if (!settings.enableWallSlide)
        {
            IsWallSliding = false;
            IsWallSticking = false;
            return;
        }

        bool wantsToWallSlide = IsTouchingWall && !IsGrounded && rb.linearVelocity.y <= 0;

        // Check if pushing into wall or neutral (not pushing away)
        if (wantsToWallSlide)
        {
            bool pushingIntoWall = Mathf.Sign(moveInput.x) == wallDirection || Mathf.Abs(moveInput.x) < 0.1f;
            wantsToWallSlide = pushingIntoWall;
        }

        if (!wantsToWallSlide)
        {
            IsWallSliding = false;
            IsWallSticking = false;
            wallStickTimer = 0;
            return;
        }

        // Wall stick phase
        if (settings.enableWallStick && wallStickTimer < settings.wallStickDuration)
        {
            IsWallSticking = true;
            IsWallSliding = false;
            wallStickTimer += Time.fixedDeltaTime;

            // Freeze vertical velocity during stick
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, 0);
            return;
        }

        // Wall slide phase
        IsWallSticking = false;
        IsWallSliding = true;

        // Apply wall slide gravity
        float slideGravity = Physics.gravity.y * settings.wallSlideGravityScale * Time.fixedDeltaTime;
        float newVelY = rb.linearVelocity.y + slideGravity;

        // Clamp to wall slide speed
        if (newVelY < -settings.wallSlideSpeed)
            newVelY = -settings.wallSlideSpeed;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, newVelY, 0);
    }

    // ══════════════════════════════════════════════════════════════
    // DASH
    // ══════════════════════════════════════════════════════════════

    void TryDash()
    {
        if (!settings.enableDash) return;
        if (dashCooldownTimer > 0) return;

        if (IsGrounded)
        {
            if (settings.enableGroundDash)
                StartDash(false);
        }
        else
        {
            if (settings.enableAirDash && AirDashesRemaining > 0)
            {
                StartDash(true);
                AirDashesRemaining--;
            }
        }
    }

    void StartDash(bool air)
    {
        IsDashing = true;
        isAirDash = air;
        dashTimer = air ? settings.airDashDuration : settings.groundDashDuration;
        dashCooldownTimer = settings.dashCooldown;

        // Determine dash direction
        if (moveInput.magnitude > 0.1f)
            dashDirection = moveInput.normalized;
        else
            dashDirection = new Vector2(FacingDirection, 0);
    }

    void HandleDash()
    {
        float speed = isAirDash ? settings.airDashSpeed : settings.groundDashSpeed;
        Vector2 dir = dashDirection;

        // Apply dash air control (steering)
        if (settings.enableDashAirControl && moveInput.magnitude > 0.1f)
        {
            Vector2 steerInput = moveInput.normalized * settings.dashAirControlMultiplier;
            dir = (dashDirection + steerInput * Time.fixedDeltaTime * 10f).normalized;
            dashDirection = dir; // Update for continuous steering
        }

        // Calculate velocity
        Vector3 dashVel = new Vector3(dir.x * speed, dir.y * speed, 0);

        // Apply dash gravity if enabled
        if (settings.enableDashGravity)
        {
            dashVel.y += Physics.gravity.y * settings.dashGravityScale * Time.fixedDeltaTime;
        }

        rb.linearVelocity = dashVel;

        // Tick down dash timer
        dashTimer -= Time.fixedDeltaTime;

        if (dashTimer <= 0)
            EndDash();
    }

    void EndDash()
    {
        IsDashing = false;

        // Apply velocity retention
        float retainedVelX = rb.linearVelocity.x * settings.dashEndVelocityRetention;

        // Handle vertical velocity on dash end
        float endVelY;
        if (settings.dashEndVerticalVelocity < 0)
            endVelY = rb.linearVelocity.y; // Preserve
        else
            endVelY = settings.dashEndVerticalVelocity; // Set to specified value

        rb.linearVelocity = new Vector3(retainedVelX, endVelY, 0);
    }

    void CancelDash()
    {
        if (!IsDashing) return;

        IsDashing = false;
        dashTimer = 0;

        // Preserve some velocity for the jump
        rb.linearVelocity = new Vector3(rb.linearVelocity.x * 0.5f, 0, 0);
    }

    // ══════════════════════════════════════════════════════════════
    // TIMERS
    // ══════════════════════════════════════════════════════════════

    void UpdateTimers()
    {
        // Dash cooldown
        if (dashCooldownTimer > 0)
            dashCooldownTimer -= Time.deltaTime;

        // Wall jump lock
        if (wallJumpLockTimer > 0)
        {
            wallJumpLockTimer -= Time.deltaTime;
            if (wallJumpLockTimer <= 0)
                IsWallJumpLocked = false;
        }
    }

    // ══════════════════════════════════════════════════════════════
    // SPRITE FLIP
    // ══════════════════════════════════════════════════════════════

    void HandleFlip()
    {
        if (!settings.enableFlip || spriteTransform == null) return;

        if (Mathf.Abs(moveInput.x) > 0.1f)
            FacingDirection = moveInput.x > 0 ? 1 : -1;

        if (settings.useScaleFlip)
        {
            Vector3 s = spriteTransform.localScale;
            s.x = Mathf.Abs(s.x) * FacingDirection;
            spriteTransform.localScale = s;
        }
        else
        {
            float target = FacingDirection > 0 ? 0 : 180;
            flipAngle = Mathf.LerpAngle(flipAngle, target, settings.flipSpeed * Time.deltaTime);
            spriteTransform.rotation = Quaternion.Euler(0, flipAngle, 0);
        }
    }

    // ══════════════════════════════════════════════════════════════
    // PUBLIC UTILITY METHODS
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Force set velocity (useful for external knockback, etc.)
    /// </summary>
    public void SetVelocity(Vector3 velocity)
    {
        rb.linearVelocity = velocity;
    }

    /// <summary>
    /// Add force impulse
    /// </summary>
    public void AddImpulse(Vector3 force)
    {
        rb.linearVelocity += force;
    }

    /// <summary>
    /// Refresh all air abilities (double jump, air dash)
    /// </summary>
    public void RefreshAirAbilities()
    {
        hasDoubleJump = true;
        AirDashesRemaining = settings.maxAirDashes;
    }

    // ══════════════════════════════════════════════════════════════
    // DEBUG
    // ══════════════════════════════════════════════════════════════

    void OnDrawGizmosSelected()
    {
        if (!showGizmos || settings == null) return;

        // Ground check
        Gizmos.color = IsGrounded ? Color.green : Color.red;
        Vector3 gPos = transform.position + (Vector3)settings.groundCheckOffset;
        Gizmos.DrawWireCube(gPos, new Vector3(settings.groundCheckSize.x, settings.groundCheckSize.y, 0.2f));

        // Wall check
        Gizmos.color = IsTouchingWall ? Color.blue : Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.right * settings.wallCheckDistance);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.left * settings.wallCheckDistance);

        // Form indicator
        Gizmos.color = IsIceForm ? Color.cyan : new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 1.5f, 0.2f);
    }
}
