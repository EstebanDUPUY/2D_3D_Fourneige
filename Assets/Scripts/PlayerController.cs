using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using static Unity.Cinemachine.CinemachineFreeLookModifier;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    public PlayerSettings settings;
    public PlayerSettingsModifier modifier;
    public Transform spriteTransform;

    [Tooltip("Auto-detects child named 'Visuals' if not assigned")]
    public SpriteRenderer spriteRenderer;

    [Tooltip("Auto-detects from spriteRenderer's GameObject if not assigned")]
    public Animator animator;

    [Header("Form Sprites")]
    public Material fireSprite;
    public Material iceSprite;

    [Header("Form Switching")]
    public bool enableFormSwitch = true;
    public PlayerSettingsModifier iceSettings;
    public PlayerSettingsModifier fireSettings;
    public bool startAsIce = false;

    // Events (subscribe to these for visual feedback)
    public System.Action<bool> OnFormSwitch; // true = ice, false = fire

    [Header("Debug")]
    public bool showGizmos = true;

    [Header("Speed Modifier")]
    //[SerializeField] private float baseSpeedMultiplier = 1.0f;
    [SerializeField]
    private float speedMultiplier = 1f;

    // Form state
    public bool IsIceForm { get; private set; }

    // Components
    private Rigidbody rb;

    // Input
    private Vector2 moveInput;
    private bool jumpHeld;

    [HideInInspector]
    public PlayerDamageSystem damageSystem;

    // State (public for external access)
    public bool IsGrounded { get; private set; }
    public bool IsTouchingWall { get; private set; }
    public bool IsWallSliding { get; private set; }
    public bool IsDashing { get; private set; }
    public bool StopMoving { get; set; }
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
    private float wallSlideTimer;
    private bool isAirDash;
    private float flipAngle;

    [Header("Animation State")]
    public bool IsJumpingAnim;
    public bool IsFallingAnim;
    public bool IsWallJumping;
    public float wallJumpAnimTime = 0.15f;
    private float wallJumpAnimTimer;

    [Header("Audio")]
    [SerializeField] private float footstepInterval = 0.4f;
    private float footstepTimer;



    void Awake()
    {
        damageSystem = GetComponent<PlayerDamageSystem>();

        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

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
            //settings = IsIceForm ? iceSettings : fireSettings;
            modifier = IsIceForm ? iceSettings : fireSettings;
            UpdateFormSprite();
        }
    }

    void Update()
    {
        CheckGrounded();
        CheckWalls();
        HandleFlip();
        UpdateTimers();

        if (IsWallJumping)
        {
            wallJumpAnimTimer -= Time.deltaTime;
            if (wallJumpAnimTimer <= 0)
                IsWallJumping = false;
        }

        // Falling
        IsFallingAnim = !IsGrounded && rb.linearVelocity.y < -0.1f;

        // Reset au sol
        if (IsGrounded)
        {
            IsJumpingAnim = false;
            IsWallJumping = false;
        }
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
            AudioManager.Instance.PlaySound(AudioManager.Instance.jumpClip);

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
        if (!enableFormSwitch || iceSettings == null || fireSettings == null)
            return;
        IsIceForm = !IsIceForm;
        modifier = IsIceForm ? iceSettings : fireSettings;
        UpdateFormSprite();
        OnFormSwitch?.Invoke(IsIceForm);

        if (IsIceForm)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.iceSound);
        else
            AudioManager.Instance.PlaySFX(AudioManager.Instance.fireSound);
    }

    public void SetIceForm()
    {
        if (iceSettings == null)
            return;
        IsIceForm = true;
        modifier = iceSettings;
        UpdateFormSprite();
        OnFormSwitch?.Invoke(true);
    }

    public void SetFireForm()
    {
        if (fireSettings == null)
            return;
        IsIceForm = false;
        modifier = fireSettings;
        UpdateFormSprite();
        OnFormSwitch?.Invoke(false);
    }

    private void UpdateFormSprite()
    {
        if (spriteRenderer == null)
            return;

        Material targetSprite = IsIceForm ? iceSprite : fireSprite;
        if (targetSprite != null)
            spriteRenderer.material = targetSprite;
    }

    // ══════════════════════════════════════════════════════════════
    // DETECTION
    // ══════════════════════════════════════════════════════════════

    void CheckGrounded()
    {
        bool wasGrounded = IsGrounded;
        Vector3 pos = transform.position + (Vector3)settings.groundCheckOffset;
        IsGrounded = Physics.CheckBox(
            pos,
            new Vector3(settings.groundCheckSize.x / 2f, settings.groundCheckSize.y / 2f, 0.1f),
            Quaternion.identity,
            settings.groundLayer
        );

        if (IsGrounded)
        {
            lastGroundedTime = Time.time;
            if (!wasGrounded)
            {
                wallSlideTimer = 0;
                IsWallSliding = false;
                hasDoubleJump = true;
                hasAirDash = true;
            }
        }
    }

    void CheckWalls()
    {
        bool right = Physics.Raycast(
            transform.position,
            Vector3.right,
            settings.wallCheckDistance,
            settings.wallLayer
        );
        bool left = Physics.Raycast(
            transform.position,
            Vector3.left,
            settings.wallCheckDistance,
            settings.wallLayer
        );
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
    /*
    void HandleMovement()
    {
        if (!settings.enableMovement)
            return;
        if (StopMoving)
            return;

        float target = moveInput.x * settings.moveSpeed * speedMultiplier;
        float accel;

        if (Mathf.Abs(moveInput.x) > 0.1f)
            accel =
                Mathf.Abs(target) > Mathf.Abs(rb.linearVelocity.x)
                    ? settings.acceleration
                    : settings.deceleration;
        else
            accel = settings.deceleration;

        float newVelX = Mathf.MoveTowards(rb.linearVelocity.x, target, accel * speedMultiplier * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector3(newVelX, rb.linearVelocity.y, 0) * bonusSlopeSpeed;
    }
    */

    void HandleMovement()
    {
        if (!settings.enableMovement || StopMoving)
            return;

        if (Mathf.Abs(moveInput.x) < 0.1f)
        {
            footstepTimer = 0f;
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            return;
        }

        float target = moveInput.x * settings.moveSpeed * modifier.moveSpeed * speedMultiplier;
        float newVelX = Mathf.MoveTowards(
            rb.linearVelocity.x,
            target,
            settings.acceleration * modifier.acceleration * speedMultiplier * Time.fixedDeltaTime
        );
        rb.linearVelocity = new Vector3(newVelX, rb.linearVelocity.y, 0);

        // 🔊 FOOTSTEPS
        if (IsGrounded)
        {
            footstepTimer += Time.fixedDeltaTime;
            if (footstepTimer >= footstepInterval)
            {
                PlayFootstep();
                footstepTimer = 0f;
            }
        }
    }
    void PlayFootstep()
    {
        if (AudioManager.Instance == null)
            return;

        if (IsIceForm)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.footstepIce);
        else
            AudioManager.Instance.PlaySFX(AudioManager.Instance.footstepFire);
    }

    // Use this function to set the speed multiplier
    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = multiplier;
    }

    // Use this function to reset the speed multiplier
    public void ResetSpeedMultiplier()
    {
        speedMultiplier = 1;
    }

    // Use this function to get the speed multiplier
    public float GetSpeedMultiplier()
    {
        return speedMultiplier;
    }

    void ApplyGravity()
    {
        if (IsGrounded && rb.linearVelocity.y <= 0)
            return;

        rb.linearVelocity += new Vector3(
            0,
            Physics.gravity.y * settings.gravityScale * modifier.gravityScale * Time.fixedDeltaTime,
            0
        );

        if (rb.linearVelocity.y < -settings.maxFallSpeed * modifier.maxFallSpeed)
            rb.linearVelocity = new Vector3(
                rb.linearVelocity.x,
                -settings.maxFallSpeed * modifier.maxFallSpeed,
                0
            );
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
            wallSlideTimer = 0f;
            int wallDir = IsTouchingWall ? wallDirection : lastWallDirection;
            rb.linearVelocity = new Vector3(-wallDir * settings.wallJumpPushForce * modifier.wallJumpPushForce, settings.wallJumpForce * modifier.wallJumpForce, 0);
            AudioManager.Instance.PlaySound(AudioManager.Instance.jumpClip);
            FacingDirection = -wallDir;
            hasDoubleJump = true;
            hasAirDash = true;
            lastWallTime = 0; // Consume wall coyote

            //ANIMATION
            IsWallJumping = true;
            wallJumpAnimTimer = wallJumpAnimTime;
            IsJumpingAnim = true;
            IsWallSliding = false;

            return;
        }

        // Ground jump (with coyote time)
        if (settings.enableJump && Time.time - lastGroundedTime <= settings.coyoteTime)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, settings.jumpForce * modifier.jumpForce, 0);
            lastGroundedTime = 0;

            //ANIMATION
            IsJumpingAnim = true;

            return;
        }

        // Double jump
        if (settings.enableDoubleJump && hasDoubleJump && !IsGrounded)
        {
            rb.linearVelocity = new Vector3(
                rb.linearVelocity.x,
                settings.jumpForce
                    * modifier.jumpForce
                    * settings.doubleJumpMultiplier
                    * modifier.doubleJumpMultiplier,
                0
            );
            hasDoubleJump = false;

            //ANIMATION
            IsJumpingAnim = true;
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
        if (wallSlideTimer < settings.timeBetweenWallSlide)
            return;
        IsWallSliding = IsTouchingWall && !IsGrounded && Mathf.Sign(moveInput.x) != -wallDirection;

        if (IsWallSliding)
        {
            float speed =
                -settings.wallSlideSpeed
                * modifier.wallSlideSpeed
                * (1f - settings.wallSlideFriction * modifier.wallSlideFriction);
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, speed, 0);
        }
    }

    // ══════════════════════════════════════════════════════════════
    // DASH
    // ══════════════════════════════════════════════════════════════

    void TryDash()
    {
        if (dashCooldownTimer > 0)
            return;

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
        dashTimer = air
            ? settings.airDashDuration * modifier.airDashDuration
            : settings.dashDuration * modifier.dashDuration;
        dashCooldownTimer = settings.dashCooldown * modifier.dashCooldown;
        AudioManager.Instance.PlaySFX(AudioManager.Instance.dashClip);
    }

    void HandleDash()
    {
        float speed = isAirDash
            ? settings.airDashSpeed * modifier.airDashSpeed
            : settings.dashSpeed * modifier.dashSpeed;
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
                rb.linearVelocity = new Vector3(
                    rb.linearVelocity.x * settings.airDashDrag * modifier.airDashDrag,
                    rb.linearVelocity.y,
                    0
                );
        }
    }

    void UpdateTimers()
    {
        if (dashCooldownTimer > 0)
            dashCooldownTimer -= Time.deltaTime;
        if (!IsWallSliding)
        {
            wallSlideTimer += Time.deltaTime;
        }
    }

    // ══════════════════════════════════════════════════════════════
    // SPRITE FLIP
    // ══════════════════════════════════════════════════════════════

    void HandleFlip()
    {
        if (!settings.enableFlip || spriteTransform == null)
            return;

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
        if (!showGizmos || settings == null)
            return;

        Gizmos.color = IsGrounded ? Color.green : Color.red;
        Vector3 gPos = transform.position + (Vector3)settings.groundCheckOffset;
        Gizmos.DrawWireCube(
            gPos,
            new Vector3(settings.groundCheckSize.x, settings.groundCheckSize.y, 0.2f)
        );

        Gizmos.color = IsTouchingWall ? Color.blue : Color.yellow;
        Gizmos.DrawLine(
            transform.position,
            transform.position + Vector3.right * settings.wallCheckDistance
        );
        Gizmos.DrawLine(
            transform.position,
            transform.position + Vector3.left * settings.wallCheckDistance
        );
    }
}