using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Main controller for the player character in a 2.5D platformer.
/// Handles movement, jumping, wall mechanics, dashing, and state switching (Fire/Ice).
///
/// Core Systems:
/// - Dual-state system using ScriptableObjects for data-driven design
/// - Hybrid ground detection (Raycast + OverlapBox) for reliability
/// - Variable-height jumping (Celeste-style)
/// - Wall slide and wall jump
/// - Directional dash with optional invincibility
/// - Smooth sprite flipping via Y-axis rotation
/// </summary>
public class PlayerController : MonoBehaviour
{
    #region Variables

    bool isDamaged;

    // ==================== REFERENCES ====================
    #region REFERENCES

    // Core Components
    private Rigidbody rb; // Physics body for movement and forces
    private SpriteRenderer spriteRenderer; // For visual feedback and sfixedprite flipping effects

    // State Data - ScriptableObjects containing all parameters for each state
    public PlayerStateData fireStateData; // Fast, light, low friction state
    public PlayerStateData iceStateData; // Slow, heavy, high friction state with dash
    private PlayerStateData currentStateData; // Currently active state data

    /// <summary>
    /// Enum defining the two available player states.
    /// Each state has drastically different physics and abilities.
    /// </summary>
    public enum States
    {
        Fire,
        Ice,
    }

    public States currentState; // Current active state

    public Action SwitchMode;

    #endregion

    // ==================== MOVEMENT VARIABLES ====================
    #region MOVEMENT VARIABLES

    private Vector2 moveInput; // Raw input from keyboard/controller (-1 to 1 on X axis)
    private float currentSpeed; // Current movement speed (not actively used but kept for future features)
    private bool isFacingRight = true; // Tracks which direction the sprite is facing
    private bool isFlipping = false; // Prevents multiple flip coroutines from running simultaneously
    #endregion

    // ==================== GROUND & WALL DETECTION ====================
    #region GROUND & WALL DETECTION

    /// <summary>
    /// Detection uses a hybrid approach:
    /// 1. OverlapBox: Confirms solid contact (reliable, no false negatives)
    /// 2. Raycast: Predicts upcoming contact (enables coyote time and buffering)
    ///
    /// This combination provides both reliability and predictive capabilities.
    /// </summary>
    [Header("Detection Settings")]
    public LayerMask groundLayer; // Layer(s) considered as ground
    public LayerMask wallLayer; // Layer(s) considered as walls

    // Size of detection boxes
    public Vector3 groundCheckSize = new Vector3(0.9f, 0.1f, 0.9f); // Flat box at feet
    public Vector3 wallCheckSize = new Vector3(0.1f, 0.9f, 0.9f); // Tall box at sides

    // Distance of predictive raycasts
    public float groundRaycastDistance = 0.2f; // How far ahead to check for ground
    public float wallRaycastDistance = 0.2f; // How far ahead to check for walls

    // Detection states
    private bool isGrounded; // TRUE when OverlapBox detects ground contact
    private bool isAboutToLand; // TRUE when Raycast predicts ground within distance
    private bool isTouchingWall; // TRUE when either side detects a wall
    private bool isWallAhead; // TRUE when Raycast detects wall in facing direction
    private int wallDirection; // -1 = wall on left, 1 = wall on right, 0 = no wall
    #endregion

    // ==================== JUMP VARIABLES ====================
    #region JUMP VARIABLES

    /// <summary>
    /// Jump system uses:
    /// - Coyote Time: Grace period after leaving ground
    /// - Jump Buffering: Remember input before landing
    /// - Variable Height: Hold jump for higher jumps (Celeste-style)
    /// </summary>
    private bool isJumping; // TRUE from jump initiation until landing
    private bool jumpBuffered; // TRUE when jump input is waiting to be executed
    private float jumpBufferTimer; // Countdown timer for jump buffer window
    private float coyoteTimeTimer; // Countdown timer for coyote time window
    private float jumpHoldTimer; // Tracks how long jump button has been held
    private bool isHoldingJump; // TRUE while jump button is held during ascent
    #endregion

    // ==================== DASH VARIABLES ====================
    #region DASH VARIABLES

    /// <summary>
    /// Dash system features:
    /// - Optional invincibility with visual feedback (sprite flicker)
    /// - Two modes: Fixed Distance OR Fixed Duration
    /// - Directional based on movement input or facing direction
    /// - Ice state exclusive ability
    /// </summary>
    [Header("Dash Settings")]
    public bool dashHasInvincibility = true; // Toggle invincibility during dash
    public float dashInvincibilityDuration = 0.3f; // How long invincibility lasts
    public bool dashIsFixedDistance = false; // FALSE = fixed duration, TRUE = fixed distance
    public float dashDistance = 5f; // Distance to travel (if fixed distance mode)
    public float dashDuration = 0.3f; // Time dash lasts (both modes)
    public float dashSpeed = 20f; // Speed during dash (if fixed duration mode)

    private bool isDashing; // TRUE during dash execution (disables normal movement)
    private bool isInvincible; // TRUE during invincibility frames
    private bool canDash = true; // FALSE after dash until landing (prevents air spam)
    private Vector3 dashDirection; // Normalized direction vector for current dash
    #endregion

    // ==================== WALL MECHANICS ====================
    #region WALL MECHANICS

    /// <summary>
    /// Wall mechanics include:
    /// - Wall Slide: Reduces fall speed when touching wall
    /// - Wall Jump: Jump away from wall while sliding
    /// - Optional input requirement for wall stick
    /// </summary>
    [Header("Wall Settings")]
    public bool wallStickRequiresInput = false; // If TRUE, must hold toward wall to stick

    private bool isWallSliding; // TRUE when sliding down a wall
    private bool canWallJump; // TRUE when wall jump is available
    #endregion

    #endregion

    // ==================== UNITY LIFECYCLE METHODS ====================
    #region START, UPDATE, ETC...

    /// <summary>
    /// Awake is called before Start. Used for getting component references.
    /// </summary>
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        PlayerDamageSystem.Die += () => isDamaged = true;
        LevelManager.OnLevelReset += () => isDamaged = false;
    }

    void OnDisable()
    {
        PlayerDamageSystem.Die -= () => isDamaged = true;
        LevelManager.OnLevelReset -= () => isDamaged = false;
    }

    /// <summary>
    /// Initialize the player state and apply starting configuration.
    /// </summary>
    private void Start()
    {
        currentState = States.Fire; // Always start in Fire state
        ApplyStateData(fireStateData); // Load Fire state parameters
    }

    /// <summary>
    /// Update runs every frame. Handles:
    /// - Detection updates (ground/wall checks)
    /// - Timer updates (coyote time, jump buffer)
    /// - Jump logic evaluation
    /// - Dash availability reset
    /// </summary>
    private void Update()
    {
        // Update detection states
        CheckGroundStatus();
        CheckWallStatus();

        // Update gameplay timers
        UpdateTimers();

        // Evaluate if jump should execute
        TryJump();

        // Reset dash when landing
        if (isGrounded && !canDash)
        {
            canDash = true;
        }
    }

    /// <summary>
    /// FixedUpdate runs at fixed intervals for physics calculations.
    /// Normal movement is disabled during dash.
    /// </summary>
    private void FixedUpdate()
    {
        if (!isDashing && !isDamaged)
        {
            ApplyMovement(); // Horizontal movement with acceleration
            ApplyWallSlide(); // Wall slide friction
            ApplyGravity(); // Custom gravity with variable jump multipliers
        }
    }

    #endregion

    // ==================== JUMP SYSTEM ====================
    #region JUMP

    /// <summary>
    /// Updates all timing systems for jump mechanics.
    /// Called every frame in Update().
    ///
    /// IMPROVED: Coyote time only resets on solid ground contact, not prediction
    /// </summary>
    private void UpdateTimers()
    {
        // Coyote Time: Grace period after leaving ground
        // FIXED: Only reset on actual ground contact, not prediction
        // This prevents coyote time from staying active too long
        if (isGrounded)
        {
            coyoteTimeTimer = currentStateData.coyoteTimeDuration; // Reset timer while grounded
        }
        else
        {
            coyoteTimeTimer -= Time.deltaTime; // Count down while airborne
        }

        // Jump Buffer: Remember jump input before landing
        if (jumpBufferTimer > 0)
        {
            jumpBufferTimer -= Time.deltaTime; // Count down active buffer
        }
        else
        {
            jumpBuffered = false; // Clear buffer when timer expires
        }

        // Jump Hold Timer: Track hold duration for variable height
        if (isHoldingJump)
        {
            jumpHoldTimer += Time.deltaTime;

            // Stop adding height after max hold time
            if (jumpHoldTimer >= currentStateData.maxJumpHoldTime)
            {
                isHoldingJump = false;
            }
        }
    }

    /// <summary>
    /// Evaluates conditions for both ground jump and wall jump.
    /// Ground jump uses coyote time for forgiveness.
    /// Wall jump has separate availability tracking.
    ///
    /// IMPROVED: Better priority handling and state checking
    /// </summary>
    private void TryJump()
    {
        // Can ground jump if: within coyote time AND not already jumping AND not dashing
        bool canGroundJump = coyoteTimeTimer > 0 && !isJumping && !isDashing;

        if (jumpBuffered)
        {
            // Priority 1: Wall jump (if available)
            if (canWallJump && !isGrounded)
            {
                WallJump();
            }
            // Priority 2: Ground jump
            else if (canGroundJump)
            {
                Jump();
            }
        }
    }

    /// <summary>
    /// Executes a standard ground jump.
    /// Resets vertical velocity to ensure consistent jump height.
    ///
    /// IMPROVED: Guarantees minimum upward velocity for consistent jumps
    /// </summary>
    private void Jump()
    {
        // FIXED: Reset vertical velocity completely for consistent jumps
        // Prevents velocity stacking from slopes or previous forces
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

        // Apply upward impulse force
        rb.AddForce(Vector3.up * currentStateData.jumpForce, ForceMode.Impulse);

        // ADDED: Guarantee minimum upward velocity
        // Ensures jump always reaches intended height even on slopes/moving platforms
        StartCoroutine(GuaranteeJumpVelocity());

        // Reset jump-related states
        isJumping = true;
        isHoldingJump = true; // Enable variable height control
        jumpHoldTimer = 0f;
        jumpBuffered = false; // Consume the buffered input
        coyoteTimeTimer = 0f; // Consume coyote time
    }

    /// <summary>
    /// Ensures jump maintains minimum velocity for one frame.
    /// Prevents external forces (slopes, moving platforms) from reducing jump height.
    /// </summary>
    private IEnumerator GuaranteeJumpVelocity()
    {
        // Wait for physics to apply forces
        yield return new WaitForFixedUpdate();

        // If upward velocity is less than expected, boost it
        float expectedMinVelocity = currentStateData.jumpForce * 0.9f; // 90% of jump force
        if (rb.linearVelocity.y < expectedMinVelocity)
        {
            rb.linearVelocity = new Vector3(
                rb.linearVelocity.x,
                expectedMinVelocity,
                rb.linearVelocity.z
            );
        }
    }

    /// <summary>
    /// Executes a wall jump, pushing the player away from the wall.
    /// Applies force both upward and horizontally (away from wall).
    /// </summary>
    private void WallJump()
    {
        // Reset all velocity for consistent wall jump
        rb.linearVelocity = new Vector3(0, 0, rb.linearVelocity.z);

        // Apply force: upward + away from wall (70% of jump force horizontally)
        Vector3 wallJumpForce = new Vector3(
            -wallDirection * currentStateData.jumpForce * 0.7f, // Horizontal away from wall
            currentStateData.jumpForce, // Vertical (same as ground jump)
            0
        );
        rb.AddForce(wallJumpForce, ForceMode.Impulse);

        // Reset states
        isJumping = true;
        isHoldingJump = true;
        jumpHoldTimer = 0f;
        jumpBuffered = false;
        canWallJump = false; // Prevent immediate re-wall-jump
        isWallSliding = false;
    }

    #endregion

    // ==================== APPLY METHODS ====================
    #region APPLY THINGS

    /// <summary>
    /// Switches to a new state by loading its ScriptableObject data.
    /// Updates Rigidbody mass and provides visual feedback via sprite color.
    /// </summary>
    private void ApplyStateData(PlayerStateData data)
    {
        currentStateData = data; // Set active data reference
        rb.mass = data.weight; // Update physics mass

        // Visual feedback: Red = Fire, Cyan = Ice
        spriteRenderer.color = currentState == States.Fire ? Color.red : Color.cyan;
    }

    /// <summary>
    /// Handles horizontal movement with acceleration/deceleration.
    /// Features:
    /// - Smooth acceleration to target speed
    /// - Starting speed boost for responsiveness
    /// - Ground friction when not moving
    /// - Automatic sprite flipping based on direction
    /// </summary>
    private void ApplyMovement()
    {
        // Calculate target speed from input
        float targetSpeed = moveInput.x * currentStateData.moveSpeed;
        float speedDifference = targetSpeed - rb.linearVelocity.x;

        // Choose between acceleration (moving) or deceleration (stopping)
        float accelRate =
            (Mathf.Abs(targetSpeed) > 0.01f)
                ? currentStateData.acceleration
                : currentStateData.deceleration;

        // Apply starting boost when beginning movement from standstill
        // Creates a more responsive "pop" feel
        if (Mathf.Abs(rb.linearVelocity.x) < 0.01f && Mathf.Abs(targetSpeed) > 0.01f)
        {
            speedDifference *= currentStateData.startingSpeedBoost;
        }

        // Calculate force to apply
        float movement = speedDifference * accelRate;
        rb.AddForce(Vector3.right * movement, ForceMode.Force);

        // Apply ground friction when grounded and not inputting movement
        if (isGrounded && Mathf.Abs(moveInput.x) < 0.01f)
        {
            rb.linearVelocity = new Vector3(
                rb.linearVelocity.x * (1 - currentStateData.groundFriction * Time.fixedDeltaTime),
                rb.linearVelocity.y,
                rb.linearVelocity.z
            );
        }

        // Handle sprite flipping based on movement direction
        if (moveInput.x > 0 && !isFacingRight)
        {
            StartFlip(true); // Flip to face right
        }
        else if (moveInput.x < 0 && isFacingRight)
        {
            StartFlip(false); // Flip to face left
        }
    }

    /// <summary>
    /// Applies custom gravity with multipliers for variable jump height (Celeste-style).
    ///
    /// Three gravity states:
    /// 1. Holding jump + ascending: Reduced gravity (floaty, allows holding for height)
    /// 2. Falling OR released jump: Increased gravity (snappy, responsive)
    /// 3. Default: Normal gravity
    /// </summary>
    private void ApplyGravity()
    {
        float gravityMultiplier = 1f;

        // Reduce gravity while holding jump and moving upward
        // This allows players to control jump height by hold duration
        if (isHoldingJump && rb.linearVelocity.y > 0)
        {
            gravityMultiplier = currentStateData.jumpHoldGravityMultiplier;
        }
        // Increase gravity when falling or when jump released early
        // Creates snappier, more responsive jump canceling
        else if (rb.linearVelocity.y < 0 || (!isHoldingJump && rb.linearVelocity.y > 0))
        {
            gravityMultiplier = currentStateData.jumpReleaseGravityMultiplier;
        }

        // Apply gravity force with multiplier
        rb.AddForce(
            Vector3.down * currentStateData.gravity * gravityMultiplier * rb.mass,
            ForceMode.Force
        );

        // Reset jumping flag when grounded
        if (isGrounded)
        {
            isJumping = false;
        }
    }

    /// <summary>
    /// Applies wall slide mechanics when conditions are met.
    ///
    /// Wall slide activates when:
    /// - Touching a wall
    /// - Not grounded
    /// - Falling (negative Y velocity)
    /// - (Optional) Pushing toward the wall with input
    ///
    /// While sliding, vertical velocity is capped to create controlled descent.
    /// </summary>
    private void ApplyWallSlide()
    {
        // Base conditions: touching wall, airborne, falling
        bool shouldWallSlide = isTouchingWall && !isGrounded && rb.linearVelocity.y < 0;

        // Optional: Require player to hold toward wall to stick
        if (wallStickRequiresInput)
        {
            bool pushingTowardWall =
                (wallDirection > 0 && moveInput.x > 0) || (wallDirection < 0 && moveInput.x < 0);
            shouldWallSlide = shouldWallSlide && pushingTowardWall;
        }

        if (shouldWallSlide)
        {
            isWallSliding = true;
            canWallJump = true; // Enable wall jump while sliding

            // Cap fall speed to wallSlideFriction value
            float slideSpeed = -currentStateData.wallSlideFriction;
            rb.linearVelocity = new Vector3(
                rb.linearVelocity.x,
                Mathf.Max(rb.linearVelocity.y, slideSpeed), // Clamp to max slide speed
                rb.linearVelocity.z
            );
        }
        else
        {
            isWallSliding = false;

            // Reset wall jump availability when landing
            if (isGrounded)
            {
                canWallJump = false;
            }
        }
    }

    #endregion

    // ==================== INPUT HANDLERS ====================
    #region INPUT

    /// <summary>
    /// Called by Unity's Input System when movement input changes.
    /// Receives Vector2 from keyboard/controller.
    /// </summary>
    public void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
    }

    /// <summary>
    /// Called when state switch input is pressed.
    /// Toggles between Fire and Ice states.
    /// </summary>
    public void OnSwitchState(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            if (currentState == States.Fire)
            {
                currentState = States.Ice;
                ApplyStateData(iceStateData);
            }
            else
            {
                currentState = States.Fire;
                ApplyStateData(fireStateData);
            }
            SwitchMode?.Invoke();
        }
    }

    /// <summary>
    /// Called by Unity's Input System for jump input.
    ///
    /// On press (performed): Buffer the jump input ONLY if grounded/about to land
    /// On release (canceled): Stop variable height control
    ///
    /// FIX: Prevents buffering jump while already airborne (no unintended double jump)
    /// </summary>
    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            // FIXED: Only buffer jump if we're grounded, about to land, or on a wall
            // This prevents the air double-jump bug
            bool canBufferJump = isGrounded || isAboutToLand || canWallJump;

            if (canBufferJump)
            {
                jumpBuffered = true;
                jumpBufferTimer = currentStateData.jumpBufferTime;
            }
        }

        if (ctx.canceled)
        {
            isHoldingJump = false; // Stop adding jump height
        }
    }

    /// <summary>
    /// Called when dash input is pressed.
    /// Only works if current state has dash enabled.
    /// Air dash requires hasAirDash flag (Ice state only).
    /// </summary>
    public void OnDash(InputAction.CallbackContext ctx)
    {
        // Check if air dash is allowed
        bool canAirDash = !isGrounded && currentStateData.hasAirDash;

        // Dash conditions: state has dash, not on cooldown, not already dashing
        if (ctx.performed && currentStateData.hasDash && canDash && !isDashing)
        {
            // Determine dash direction from movement input
            Vector3 dashDir = Vector3.zero;

            if (moveInput.magnitude > 0.1f)
            {
                // Dash in direction of input (normalized for consistent speed)
                dashDir = new Vector3(moveInput.x, moveInput.y, 0).normalized;
            }
            else
            {
                // No input: dash in facing direction
                dashDir = isFacingRight ? Vector3.right : Vector3.left;
            }

            StartCoroutine(PerformDash(dashDir));
        }
    }

    #endregion

    // ==================== DASH COROUTINES ====================
    #region DASH

    /// <summary>
    /// Executes the dash movement in the specified direction.
    ///
    /// Two modes:
    /// 1. Fixed Distance: Lerp from start to target position
    /// 2. Fixed Duration: Apply constant velocity for duration
    ///
    /// Dash locks out normal movement and can trigger invincibility.
    /// </summary>
    private IEnumerator PerformDash(Vector3 direction)
    {
        isDashing = true; // Lock out normal movement
        canDash = false; // Prevent dash spam (reset on landing)
        dashDirection = direction;

        // Start invincibility effect if enabled
        if (dashHasInvincibility)
        {
            StartCoroutine(InvincibilityFrames());
        }

        float elapsed = 0f;

        if (dashIsFixedDistance)
        {
            // MODE 1: Fixed Distance Dash
            // Smoothly interpolate from start to target position over duration
            Vector3 startPos = transform.position;
            Vector3 targetPos = startPos + dashDirection * dashDistance;

            while (elapsed < dashDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / dashDuration;
                transform.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }
        }
        else
        {
            // MODE 2: Fixed Duration Dash
            // Apply constant velocity in dash direction for duration
            while (elapsed < dashDuration)
            {
                rb.linearVelocity = dashDirection * dashSpeed;
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        isDashing = false;

        // Reset dash availability if grounded (prevents multi air-dash)
        if (isGrounded)
        {
            canDash = true;
        }
    }

    /// <summary>
    /// Provides invincibility frames during dash with visual feedback.
    /// Flickers the sprite on/off to indicate invulnerability state.
    /// </summary>
    private IEnumerator InvincibilityFrames()
    {
        isInvincible = true;

        // Visual feedback: Flicker sprite every 0.1 seconds
        float elapsed = 0f;
        while (elapsed < dashInvincibilityDuration)
        {
            spriteRenderer.enabled = !spriteRenderer.enabled; // Toggle visibility
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }

        // Ensure sprite is visible when invincibility ends
        spriteRenderer.enabled = true;
        isInvincible = false;
    }

    #endregion

    // ==================== SPRITE FLIPPING ====================
    #region FLIPPING

    /// <summary>
    /// Initiates sprite flip if not already flipping.
    /// Prevents overlapping flip animations.
    /// </summary>
    private void StartFlip(bool flipToRight)
    {
        if (!isFlipping)
        {
            StartCoroutine(FlipSprite(flipToRight));
        }
    }

    /// <summary>
    /// Smoothly rotates the sprite on Y-axis to create a flip effect.
    /// Uses Y-rotation instead of scale to avoid visual artifacts.
    ///
    /// Rotation values:
    /// - 0� = Facing right
    /// - 180� = Facing left
    ///
    /// Duration: 0.15 seconds (quick and snappy for fast gameplay)
    /// </summary>
    private IEnumerator FlipSprite(bool flipToRight)
    {
        isFlipping = true;

        float startRotation = transform.eulerAngles.y;
        float targetRotation = flipToRight ? 0f : 180f;
        float elapsed = 0f;
        float flipDuration = 0.15f;

        // Smoothly interpolate rotation over duration
        while (elapsed < flipDuration)
        {
            elapsed += Time.deltaTime;
            float yRotation = Mathf.Lerp(startRotation, targetRotation, elapsed / flipDuration);
            transform.rotation = Quaternion.Euler(0, yRotation, 0);
            yield return null;
        }

        // Ensure exact final rotation (prevents floating point drift)
        transform.rotation = Quaternion.Euler(0, targetRotation, 0);
        isFacingRight = flipToRight;
        isFlipping = false;
    }

    #endregion

    // ==================== GROUND DETECTION ====================
    #region GROUND DETECTION

    /// <summary>
    /// Hybrid ground detection system combining OverlapBox and Raycast.
    ///
    /// OverlapBox: Checks for immediate contact (isGrounded)
    /// - Reliable, no false negatives
    /// - Positioned at bottom of character collider
    ///
    /// Raycast: Predicts upcoming contact (isAboutToLand)
    /// - Enables coyote time and jump buffering
    /// - Slightly elevated start position to avoid starting inside ground
    ///
    /// This combination provides both current state and predictive capabilities.
    /// </summary>
    private void CheckGroundStatus()
    {
        // Calculate check position at bottom of collider
        Vector3 boxCenter =
            transform.position - new Vector3(0, GetComponent<Collider>().bounds.extents.y, 0);

        // OverlapBox: Check for solid ground contact
        isGrounded = Physics.CheckBox(
            boxCenter,
            groundCheckSize / 2,
            Quaternion.identity,
            groundLayer
        );

        // Raycast: Predict ground within distance
        RaycastHit hit;
        Vector3 rayStart = boxCenter + Vector3.up * 0.1f; // Slightly elevated to avoid self-collision
        isAboutToLand = Physics.Raycast(
            rayStart,
            Vector3.down,
            out hit,
            groundRaycastDistance,
            groundLayer
        );
    }

    #endregion

    // ==================== WALL DETECTION ====================
    #region WALL DETECTION

    /// <summary>
    /// Detects walls on both sides of the character using OverlapBox.
    /// Also uses Raycast to detect walls ahead in facing direction.
    ///
    /// Checks both left and right simultaneously to determine:
    /// - isTouchingWall: Is there a wall on either side?
    /// - wallDirection: Which side is the wall on?
    /// - isWallAhead: Is there a wall in front of the player?
    /// </summary>
    private void CheckWallStatus()
    {
        Vector3 boxCenter = transform.position;

        // Check right side with OverlapBox
        bool rightWall = Physics.CheckBox(
            boxCenter + Vector3.right * 0.5f,
            wallCheckSize / 2,
            Quaternion.identity,
            wallLayer
        );

        // Check left side with OverlapBox
        bool leftWall = Physics.CheckBox(
            boxCenter + Vector3.left * 0.5f,
            wallCheckSize / 2,
            Quaternion.identity,
            wallLayer
        );

        // Determine wall state
        isTouchingWall = rightWall || leftWall;
        wallDirection = rightWall ? 1 : (leftWall ? -1 : 0);

        // Raycast for wall ahead in facing direction
        Vector3 rayDirection = isFacingRight ? Vector3.right : Vector3.left;
        isWallAhead = Physics.Raycast(boxCenter, rayDirection, wallRaycastDistance, wallLayer);
    }

    #endregion

    // ==================== DEBUG GIZMOS ====================
    #region GIZMOS

    /// <summary>
    /// Draws visual debugging information in the Scene view.
    /// Only visible when the GameObject is selected.
    ///
    /// Color coding:
    /// - Green/Red boxes: Ground detection (green = grounded)
    /// - Yellow/Gray line down: Ground prediction (yellow = about to land)
    /// - Blue boxes: Wall detection zones
    /// - Cyan/Gray line forward: Wall ahead prediction (cyan = wall ahead)
    ///
    /// Use these visualizations to tune detection sizes and distances.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Get collider reference
        Collider col = GetComponent<Collider>();
        if (col == null)
            return;

        // Calculate ground check position at bottom of collider
        Vector3 groundCheckCenter = transform.position - new Vector3(0, col.bounds.extents.y, 0);

        // Ground OverlapBox (immediate contact detection)
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireCube(groundCheckCenter, groundCheckSize);

        // Ground Raycast (predictive detection)
        Gizmos.color = isAboutToLand ? Color.yellow : Color.gray;
        Vector3 rayStart = groundCheckCenter + Vector3.up * 0.1f;
        Gizmos.DrawLine(rayStart, rayStart + Vector3.down * groundRaycastDistance);

        // Wall detection - Right side
        Gizmos.color = Color.blue;
        Vector3 rightWallCheck = transform.position + Vector3.right * 0.5f;
        Gizmos.DrawWireCube(rightWallCheck, wallCheckSize);

        // Wall detection - Left side
        Vector3 leftWallCheck = transform.position + Vector3.left * 0.5f;
        Gizmos.DrawWireCube(leftWallCheck, wallCheckSize);

        // Wall ahead raycast
        Gizmos.color = isWallAhead ? Color.cyan : Color.gray;
        Vector3 wallRayDirection = isFacingRight ? Vector3.right : Vector3.left;
        Gizmos.DrawLine(
            transform.position,
            transform.position + wallRayDirection * wallRaycastDistance
        );
    }

    #endregion
}
