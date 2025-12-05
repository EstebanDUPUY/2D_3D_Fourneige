using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    public PlayerSettings settings;
    public Transform spriteTransform;

    [Header("Form Switching")]
    public bool enableFormSwitch = true;
    public PlayerSettings iceSettings;
    public PlayerSettings fireSettings;
    public bool startAsIce = false;

    // Events (subscribe to these for visual feedback)
    public System.Action<bool> OnFormSwitch; // true = ice, false = fire

    [Header("Debug")]
    public bool showGizmos = true;

    // Form state
    public bool IsIceForm { get; private set; }

    // Components
    private Rigidbody rb;

    // Input
    private Vector2 moveInput;
    private bool jumpHeld;

    // State (public for external access)
    public bool IsGrounded { get; private set; }
    public bool IsTouchingWall { get; private set; }
    public bool IsWallSliding { get; private set; }
    public bool IsDashing { get; private set; }
    public int FacingDirection { get; private set; } = 1;

    public float bonusSlopeSpeed = 1;

    // Internal
    private int wallDirection;
    private bool hasDoubleJump;
    private bool hasAirDash;
    private float lastGroundedTime;
    private float lastWallTime;
    private int lastWallDirection;
    private float dashTimer;
    private float dashCooldownTimer;
    private bool isAirDash;
    private float flipAngle;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (spriteTransform == null)
            spriteTransform = transform;

        // Initialize form
        IsIceForm = startAsIce;
        if (enableFormSwitch && iceSettings != null && fireSettings != null)
            settings = IsIceForm ? iceSettings : fireSettings;
    }

    void Update()
    {
        CheckGrounded();
        CheckWalls();
        HandleFlip();
        UpdateTimers();
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
    // INPUT CALLBACKS (Connect these in PlayerInput component)
    // ══════════════════════════════════════════════════════════════

    public void OnMove(InputAction.CallbackContext ctx) => moveInput = ctx.ReadValue<Vector2>();

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (ctx.started)
        {
            jumpHeld = true;
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
        OnFormSwitch?.Invoke(IsIceForm);
    }

    public void SetIceForm()
    {
        if (iceSettings == null) return;
        IsIceForm = true;
        settings = iceSettings;
        OnFormSwitch?.Invoke(true);
    }

    public void SetFireForm()
    {
        if (fireSettings == null) return;
        IsIceForm = false;
        settings = fireSettings;
        OnFormSwitch?.Invoke(false);
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
            if (!wasGrounded)
            {
                hasDoubleJump = true;
                hasAirDash = true;
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

        float target = moveInput.x * settings.moveSpeed * bonusSlopeSpeed;
        
        if (Mathf.Abs(moveInput.x) > 0.1f)
        {
            // Accelerate toward target speed
            float newVelX = Mathf.MoveTowards(rb.linearVelocity.x, target, settings.acceleration * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector3(newVelX, rb.linearVelocity.y, 0);
        }
        else
        {
            // Apply friction when not pressing input
            float newVelX = Mathf.MoveTowards(rb.linearVelocity.x, 0, settings.friction * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector3(newVelX, rb.linearVelocity.y, 0);
        }
    }

    void ApplyGravity()
    {
        if (IsGrounded && rb.linearVelocity.y <= 0) return;

        rb.linearVelocity += new Vector3(0, Physics.gravity.y * settings.gravityScale * Time.fixedDeltaTime, 0);

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
            hasAirDash = true;
            lastWallTime = 0; // Consume wall coyote
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
        if (rb.linearVelocity.y > 0)
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f, 0);
    }

    // ══════════════════════════════════════════════════════════════
    // WALL SLIDE
    // ══════════════════════════════════════════════════════════════

    void HandleWallSlide()
    {
        if (!settings.enableWallSlide)
        {
            IsWallSliding = false;
            return;
        }

        IsWallSliding = IsTouchingWall && !IsGrounded && Mathf.Sign(moveInput.x) != -wallDirection;

        if (IsWallSliding)
        {
            float speed = -settings.wallSlideSpeed * (1f - settings.wallSlideFriction);
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, speed, 0);
        }
    }

    // ══════════════════════════════════════════════════════════════
    // DASH
    // ══════════════════════════════════════════════════════════════

    void TryDash()
    {
        if (dashCooldownTimer > 0) return;

        if (IsGrounded && settings.enableDash)
        {
            StartDash(false);
        }
        else if (!IsGrounded && settings.enableAirDash && hasAirDash)
        {
            StartDash(true);
            hasAirDash = false;
        }
    }

    void StartDash(bool air)
    {
        IsDashing = true;
        isAirDash = air;
        dashTimer = air ? settings.airDashDuration : settings.dashDuration;
        dashCooldownTimer = settings.dashCooldown;
    }

    void HandleDash()
    {
        float speed = isAirDash ? settings.airDashSpeed : settings.dashSpeed;
        Vector2 dir;
        if (moveInput.magnitude > 0.1f)
            dir = moveInput.normalized;
        else
            dir = new Vector2(FacingDirection, 0);

        rb.linearVelocity = new Vector3(dir.x * speed, dir.y * speed, 0);

        dashTimer -= Time.fixedDeltaTime;
        if (dashTimer <= 0)
        {
            IsDashing = false;
            if (isAirDash)
                rb.linearVelocity = new Vector3(rb.linearVelocity.x * settings.airDashDrag, rb.linearVelocity.y, 0);
        }
    }

    void UpdateTimers()
    {
        if (dashCooldownTimer > 0)
            dashCooldownTimer -= Time.deltaTime;
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
    // DEBUG
    // ══════════════════════════════════════════════════════════════

    void OnDrawGizmosSelected()
    {
        if (!showGizmos || settings == null) return;

        Gizmos.color = IsGrounded ? Color.green : Color.red;
        Vector3 gPos = transform.position + (Vector3)settings.groundCheckOffset;
        Gizmos.DrawWireCube(gPos, new Vector3(settings.groundCheckSize.x, settings.groundCheckSize.y, 0.2f));

        Gizmos.color = IsTouchingWall ? Color.blue : Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.right * settings.wallCheckDistance);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.left * settings.wallCheckDistance);
    }
}
