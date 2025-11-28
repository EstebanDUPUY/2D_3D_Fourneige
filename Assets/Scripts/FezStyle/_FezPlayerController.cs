using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// Fully modular player controller for 2.5D platformer.
/// All features are toggle-based through ScriptableObject states.
/// 
/// Core Systems:
/// - Data-driven state system (no hardcoded state exclusivity)
/// - Hybrid ground/wall detection (Raycast + OverlapBox)
/// - Variable-height jumping (optional per state)
/// - Wall slide and wall jump (optional per state)
/// - Dash system with invincibility (optional per state)
/// - Bunny hop mechanic (optional per state)
/// - Double jump (optional per state)
/// - Air control (optional per state)
/// - State switching with cooldown
/// - Master movement lock
/// - Fez-style world rotation integration (optional)
/// </summary>
public class _FezPlayerController : MonoBehaviour
{
    #region Variables

    // ==================== REFERENCES ====================
    #region REFERENCES

    // Core Components
    private Rigidbody rb; // Physics body for movement and forces
    private SpriteRenderer spriteRenderer; // For visual feedback and sprite flipping effects

    // Visual Transform - the child GameObject that rotates for sprite flipping
    [Header("Visual References")]
    [Tooltip("Drag the 'Visual' child GameObject here")]
    public Transform visualTransform;

    // State Data - ScriptableObjects containing all parameters for each state
    public _PlayerStateData fireStateData; // First state option
    public _PlayerStateData iceStateData; // Second state option
    private _PlayerStateData currentStateData; // Currently active state data

    /// <summary>
    /// Enum defining the two available player states.
    /// States are NOT hardcoded with exclusive features - all features are defined in ScriptableObjects.
    /// </summary>
    public enum States { Fire, Ice }
    public States currentState; // Current active state

    #endregion

    // ==================== MOVEMENT VARIABLES ====================
    #region MOVEMENT VARIABLES

    private float facingAngle = 0f; // Tracks local facing (0=Right, 180=Left)
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
    /// Jump system supports:
    /// - Coyote Time: Grace period after leaving ground (optional)
    /// - Jump Buffering: Remember input before landing (optional)
    /// - Variable Height: Hold jump for higher jumps (optional)
    /// - Double Jump: Jump again while airborne (optional)
    /// </summary>
    private bool isJumping; // TRUE from jump initiation until landing
    private bool jumpBuffered; // TRUE when jump input is waiting to be executed
    private float jumpBufferTimer; // Countdown timer for jump buffer window
    private float coyoteTimeTimer; // Countdown timer for coyote time window
    private float jumpHoldTimer; // Tracks how long jump button has been held
    private bool isHoldingJump; // TRUE while jump button is held during ascent
    private int airJumpsRemaining; // Number of air jumps left (for double jump)

    #endregion

    // ==================== DASH VARIABLES ====================
    #region DASH VARIABLES

    /// <summary>
    /// Dash system features:
    /// - Optional invincibility with visual feedback (sprite flicker)
    /// - Two modes: Fixed Distance OR Fixed Duration
    /// - Directional based on movement input or facing direction
    /// - Optional cooldown system
    /// </summary>
    [Header("Dash Settings (Inspector Overrides)")]
    [Tooltip("If TRUE, uses these inspector values instead of ScriptableObject values")]
    public bool overrideDashSettings = false;
    public bool dashHasInvincibility = true;
    public float dashInvincibilityDuration = 0.3f;
    public bool dashIsFixedDistance = false;
    public float dashDistance = 5f;
    public float dashDuration = 0.3f;
    public float dashSpeed = 20f;

    private bool isDashing; // TRUE during dash execution (disables normal movement)
    private bool isInvincible; // TRUE during invincibility frames
    private bool canDash = true; // FALSE after dash until cooldown expires
    private float dashCooldownTimer; // Countdown for dash cooldown
    private Vector3 dashDirection; // Normalized direction vector for current dash

    #endregion

    // ==================== WALL MECHANICS ====================
    #region WALL MECHANICS

    /// <summary>
    /// Wall mechanics include:
    /// - Wall Slide: Reduces fall speed when touching wall (optional)
    /// - Wall Jump: Jump away from wall while sliding (optional)
    /// - Optional input requirement for wall stick
    /// </summary>
    [Header("Wall Settings (Inspector Overrides)")]
    [Tooltip("If TRUE, uses this inspector value instead of ScriptableObject value")]
    public bool overrideWallStickRequiresInput = false;
    public bool wallStickRequiresInput = false;

    private bool isWallSliding; // TRUE when sliding down a wall
    private bool canWallJump; // TRUE when wall jump is available

    #endregion

    // ==================== BUNNY HOP VARIABLES ====================
    #region BUNNY HOP VARIABLES

    /// <summary>
    /// Bunny hop system:
    /// - Rewards landing and immediately jumping with speed bonus
    /// - Bonus stacks with successful chains
    /// - Decays over time if not chained
    /// - Creates "flow" gameplay feel
    /// </summary>
    private float bunnyHopBonus; // Current speed multiplier from bunny hopping (0 to bunnyHopMaxSpeed-1)
    private float timeSinceLanding; // Tracks time since last landing for timing window
    private bool canBunnyHop; // TRUE when within timing window after landing
    private bool lastJumpWasBunnyHop; // Tracks if last jump maintained the chain

    #endregion

    // ==================== STATE SWITCHING ====================
    #region STATE SWITCHING

    /// <summary>
    /// State switching with cooldown system.
    /// Prevents rapid state spam and allows for strategic state management.
    /// </summary>
    private float stateSwitchCooldownTimer; // Countdown until next switch allowed
    private bool canSwitchState = true; // FALSE during cooldown

    #endregion

    // ==================== WORLD ROTATION (FEZ-STYLE) ====================
    #region WORLD ROTATION VARIABLES

    /// <summary>
    /// Fez-style world rotation integration.
    /// Features:
    /// - Freeze player during rotation (optional)
    /// - Adapt movement to current camera view (optional)
    /// - Snap to 2D plane after rotation (optional)
    /// 
    /// Requires _WorldRotationController in scene.
    /// </summary>
    [Header("World Rotation Settings")]

    [Tooltip("Enable Fez-style world rotation integration")]
    public bool useWorldRotation = false;

    [Tooltip("Reference to the world rotation controller. Auto-finds if null")]
    public _WorldRotationController worldRotationController;

    [Tooltip("Freeze player during world rotation (like Fez)")]
    public bool freezeDuringRotation = true;

    [Tooltip("Adapt movement direction to current camera view")]
    public bool useRotationRelativeMovement = true;

    [Tooltip("Snap to 2D plane after rotation completes")]
    public bool snapAfterRotation = true;

    // Private rotation state
    private bool isFrozenForRotation = false; // TRUE during world rotation freeze
    private Vector3 frozenPosition; // Position when frozen
    private Vector3 velocityBeforeFreeze; // Velocity to restore after unfreeze

    #endregion

    #endregion

    // ==================== UNITY LIFECYCLE METHODS ====================
    #region START, UPDATE, ETC...

    /// <summary>
    /// Awake is called before Start. Used for getting component references.
    /// IMPORTANT: SpriteRenderer must be on a child GameObject (Visual/Sprite).
    /// </summary>
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Get SpriteRenderer from child (it's no longer on this GameObject)
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // Validate that visualTransform is assigned
        if (visualTransform == null)
        {
            Debug.LogError("Visual Transform is not assigned! Please drag the 'Visual' child GameObject to the PlayerController's visualTransform field in the Inspector.");
        }

        // Ensure visual starts at correct rotation
        if (visualTransform != null)
        {
            visualTransform.rotation = Quaternion.Euler(0, 0, 0); // Start facing right
        }
    }

    /// <summary>
    /// Initialize the player state and apply starting configuration.
    /// </summary>
    private void Start()
    {
        currentState = States.Fire; // Always start in Fire state
        ApplyStateData(fireStateData); // Load Fire state parameters

        facingAngle = isFacingRight ? 0f : 180f;
        // Initialize world rotation if enabled
        InitializeWorldRotation();

        if (worldRotationController != null)
        {
            UpdatePhysicsConstraints(worldRotationController.GetCurrentFaceIndex());
        }
        else
        {
            // Default to Standard 2D (Lock Z) if no rotation system found
            UpdatePhysicsConstraints(0);
        }
    }

    /// <summary>
    /// Update runs every frame. Handles:
    /// - Detection updates (ground/wall checks)
    /// - Timer updates (coyote time, jump buffer, cooldowns)
    /// - Jump logic evaluation
    /// - Bunny hop tracking
    /// - Dash availability reset
    /// </summary>
    private void Update()
    {
        // Early exit if frozen for world rotation
        if (isFrozenForRotation)
        {
            return;
        }

        // Early exit if movement is completely disabled
        if (!currentStateData.canMove)
        {
            return;
        }

        // Update detection states
        CheckGroundStatus();
        CheckWallStatus();

        // Update gameplay timers
        UpdateTimers();
        UpdateCooldowns();

        // Track bunny hop timing
        UpdateBunnyHopTracking();

        // Evaluate if jump should execute
        TryJump();

        // Reset dash when landing (if no cooldown system AND dash was actually used)
        // FIXED: Only reset if dash was used, not if cooldown is active
        if (isGrounded && !canDash && currentStateData.dashCooldown <= 0 && dashCooldownTimer <= 0)
        {
            canDash = true;
        }
    }

    /// <summary>
    /// FixedUpdate runs at fixed intervals for physics calculations.
    /// Normal movement is disabled during dash or when canMove is FALSE.
    /// </summary>
    private void FixedUpdate()
    {
        // Handle frozen state for world rotation
        if (isFrozenForRotation)
        {
            rb.linearVelocity = Vector3.zero;
            rb.position = frozenPosition;
            return;
        }

        // Early exit if movement is completely disabled
        if (!currentStateData.canMove)
        {
            return;
        }

        if (!isDashing)
        {
            ApplyMovement(); // Horizontal movement with acceleration
            ApplyWallSlide(); // Wall slide friction
            ApplyGravity(); // Custom gravity with variable jump multipliers
        }

        // Decay bunny hop bonus over time
        DecayBunnyHopBonus();
    }

    private void LateUpdate()
    {
        if (visualTransform == null) return;

        // 1. Get the current Camera Y Rotation
        float cameraAngle = 0f;
        if (worldRotationController != null && worldRotationController.cameraTransform != null)
        {
            cameraAngle = worldRotationController.cameraTransform.eulerAngles.y;
        }
        else if (Camera.main != null)
        {
            cameraAngle = Camera.main.transform.eulerAngles.y;
        }

        // 2. Apply Rotation: Camera Angle + Your Facing Angle
        // This effectively "parents" the sprite rotation to the camera view
        visualTransform.rotation = Quaternion.Euler(0, cameraAngle + facingAngle, 0);
    }

    #endregion

    // ==================== WORLD ROTATION INTEGRATION ====================
    #region WORLD ROTATION

    /// <summary>
    /// Initializes world rotation integration if enabled.
    /// Subscribes to rotation events from _WorldRotationController.
    /// </summary>
    private void InitializeWorldRotation()
    {
        if (!useWorldRotation)
        {
            return;
        }

        // Find controller if not assigned
        if (worldRotationController == null)
        {
            worldRotationController = _WorldRotationController.Instance;
        }

        if (worldRotationController == null)
        {
            Debug.LogWarning("[_PlayerController] World rotation enabled but no _WorldRotationController found in scene!");
            useWorldRotation = false;
            return;
        }

        // Subscribe to rotation events
        worldRotationController.OnRotationStarted += OnWorldRotationStarted;
        worldRotationController.OnRotationCompleted += OnWorldRotationCompleted;
    }

    /// <summary>
    /// Unsubscribes from rotation events on disable.
    /// </summary>
    private void OnDisable()
    {
        if (worldRotationController != null)
        {
            worldRotationController.OnRotationStarted -= OnWorldRotationStarted;
            worldRotationController.OnRotationCompleted -= OnWorldRotationCompleted;
        }
    }

    /// <summary>
    /// Called when world rotation begins.
    /// Freezes player if freezeDuringRotation is enabled.
    /// </summary>
    private void OnWorldRotationStarted(int newFaceIndex)
    {
        if (!useWorldRotation || !freezeDuringRotation)
        {
            return;
        }

        FreezeForRotation();
    }

    /// <summary>
    /// Called when world rotation completes.
    /// Unfreezes player and optionally snaps to 2D plane.
    /// </summary>
    private void OnWorldRotationCompleted(int faceIndex)
    {
        if (!useWorldRotation)
        {
            return;
        }

        if (freezeDuringRotation)
        {
            UnfreezeFromRotation();
        }

        if (snapAfterRotation)
        {
            SnapToCurrentPlane();
        }

        UpdatePhysicsConstraints(faceIndex);
    }

    /// <summary>
    /// Freezes player in place during world rotation.
    /// Stores current position and velocity for later restoration.
    /// </summary>
    private void FreezeForRotation()
    {
        isFrozenForRotation = true;
        frozenPosition = transform.position;
        velocityBeforeFreeze = rb.linearVelocity;
        rb.isKinematic = true;
    }

    /// <summary>
    /// Unfreezes player after world rotation.
    /// Restores velocity in the new camera-relative direction.
    /// </summary>
    private void UnfreezeFromRotation()
    {
        rb.isKinematic = false;
        isFrozenForRotation = false;

        // Restore velocity adapted to new direction
        if (useRotationRelativeMovement && worldRotationController != null)
        {
            // Calculate horizontal speed from old velocity
            float horizontalSpeed = new Vector2(velocityBeforeFreeze.x, velocityBeforeFreeze.z).magnitude;

            // Get new right direction
            Vector3 newRight = worldRotationController.GetCurrentRight();

            // Maintain direction (left/right) of movement
            float direction = Mathf.Sign(Vector3.Dot(velocityBeforeFreeze.normalized, newRight));
            if (Mathf.Abs(direction) < 0.1f) direction = 1f;

            rb.linearVelocity = new Vector3(
                newRight.x * horizontalSpeed * direction,
                velocityBeforeFreeze.y,
                newRight.z * horizontalSpeed * direction
            );
        }
        else
        {
            rb.linearVelocity = velocityBeforeFreeze;
        }
    }

    /// <summary>
    /// Snaps player to the 2D plane based on current camera view.
    /// Called after rotation completes to prevent depth-related issues.
    /// </summary>
    private void SnapToCurrentPlane()
    {
        if (worldRotationController == null)
        {
            return;
        }

        // Get current face for determining snap behavior
        int faceIndex = worldRotationController.GetCurrentFaceIndex();

        // In a full implementation, you would raycast to find valid platforms
        // For now, this is a placeholder for the depth snapper system
        // The _FezDepthSnapper component handles more complex snapping logic
    }

    /// <summary>
    /// Gets the movement direction based on world rotation state.
    /// Returns world right if rotation disabled, or camera-relative right if enabled.
    /// </summary>
    private Vector3 GetMovementRight()
    {
        if (useWorldRotation && useRotationRelativeMovement && worldRotationController != null)
        {
            return worldRotationController.GetCurrentRight();
        }

        return Vector3.right;
    }

    /// <summary>
    /// Returns TRUE if world is currently rotating.
    /// Used to prevent actions during rotation.
    /// </summary>
    public bool IsWorldRotating()
    {
        if (!useWorldRotation || worldRotationController == null)
        {
            return false;
        }

        return worldRotationController.IsRotating();
    }

    /// <summary>
    /// Returns TRUE if player is frozen for world rotation.
    /// </summary>
    public bool IsFrozenForRotation()
    {
        return isFrozenForRotation;
    }

    /// <summary>
    /// Dynamically locks the physics axis perpendicular to the camera.
    /// This ensures true 2D movement on the current plane.
    /// </summary>
    private void UpdatePhysicsConstraints(int faceIndex)
    {
        if (rb == null) return;

        // Standard constraints: Always freeze rotation X and Z (so player doesn't tip over)
        RigidbodyConstraints constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // Even Faces (0: North, 2: South) = View is along Z-axis.
        // We move on X, so we must FREEZE Z (Depth).
        if (faceIndex % 2 == 0)
        {
            constraints |= RigidbodyConstraints.FreezePositionZ;
        }
        // Odd Faces (1: East, 3: West) = View is along X-axis.
        // We move on Z, so we must FREEZE X (Depth).
        else
        {
            constraints |= RigidbodyConstraints.FreezePositionX;
        }

        rb.constraints = constraints;
    }

    #endregion

    // ==================== JUMP SYSTEM ====================
    #region JUMP

    /// <summary>
    /// Updates all timing systems for jump mechanics.
    /// Called every frame in Update().
    /// Respects state toggles for coyote time and jump buffer.
    /// </summary>
    private void UpdateTimers()
    {
        // Coyote Time: Grace period after leaving ground (if enabled)
        if (currentStateData.hasCoyoteTime)
        {
            if (isGrounded)
            {
                coyoteTimeTimer = currentStateData.coyoteTimeDuration;
            }
            else
            {
                coyoteTimeTimer -= Time.deltaTime;
            }
        }
        else
        {
            // If coyote time disabled, only allow jump while grounded
            coyoteTimeTimer = isGrounded ? 0.1f : 0f;
        }

        // Jump Buffer: Remember jump input before landing (if enabled)
        if (currentStateData.hasJumpBuffer)
        {
            if (jumpBufferTimer > 0)
            {
                jumpBufferTimer -= Time.deltaTime;
            }
            else
            {
                jumpBuffered = false;
            }
        }
        else
        {
            // If jump buffer disabled, clear buffer immediately
            jumpBuffered = false;
        }

        // Jump Hold Timer: Track hold duration for variable height (if enabled)
        if (currentStateData.hasVariableJump && isHoldingJump)
        {
            jumpHoldTimer += Time.deltaTime;

            if (jumpHoldTimer >= currentStateData.maxJumpHoldTime)
            {
                isHoldingJump = false;
            }
        }
    }

    /// <summary>
    /// Updates cooldown timers for dash and state switching.
    /// </summary>
    private void UpdateCooldowns()
    {
        // Dash cooldown
        if (dashCooldownTimer > 0)
        {
            dashCooldownTimer -= Time.deltaTime;
            if (dashCooldownTimer <= 0)
            {
                canDash = true;
            }
        }

        // State switch cooldown
        if (stateSwitchCooldownTimer > 0)
        {
            stateSwitchCooldownTimer -= Time.deltaTime;
            if (stateSwitchCooldownTimer <= 0)
            {
                canSwitchState = true;
            }
        }
    }

    /// <summary>
    /// Evaluates conditions for ground jump, air jump, and wall jump.
    /// Respects all state toggles and cooldowns.
    /// </summary>
    private void TryJump()
    {
        // Early exit if jumping is disabled for this state
        if (!currentStateData.canJump)
        {
            return;
        }

        // Can ground jump if: within coyote time AND not already jumping AND not dashing
        bool canGroundJump = coyoteTimeTimer > 0 && !isJumping && !isDashing;

        // Can air jump if: double jump enabled AND jumps remaining AND not dashing
        bool canAirJump = currentStateData.hasDoubleJump && airJumpsRemaining > 0 && !isGrounded && !isDashing;

        if (jumpBuffered)
        {
            // Priority 1: Wall jump (if available and enabled)
            if (currentStateData.hasWallJump && canWallJump && !isGrounded)
            {
                WallJump();
            }
            // Priority 2: Ground jump
            else if (canGroundJump)
            {
                Jump();
            }
            // Priority 3: Air jump (double jump)
            else if (canAirJump)
            {
                AirJump();
            }
        }
    }

    /// <summary>
    /// Executes a standard ground jump.
    /// Resets vertical velocity to ensure consistent jump height.
    /// Checks for bunny hop and applies bonus if within timing window.
    /// </summary>
    private void Jump()
    {
        // Check bunny hop timing
        bool isBunnyHop = currentStateData.hasBunnyHop && canBunnyHop && bunnyHopBonus > 0;

        if (isBunnyHop)
        {
            // Successful bunny hop - increase bonus
            bunnyHopBonus = Mathf.Min(bunnyHopBonus + currentStateData.bunnyHopSpeedBonus,
                                      currentStateData.bunnyHopMaxSpeed - 1f);
            lastJumpWasBunnyHop = true;
        }
        else if (currentStateData.hasBunnyHop && timeSinceLanding <= currentStateData.bunnyHopTimingWindow)
        {
            // First bunny hop - start the bonus
            bunnyHopBonus = currentStateData.bunnyHopSpeedBonus;
            lastJumpWasBunnyHop = true;
        }
        else
        {
            lastJumpWasBunnyHop = false;
        }

        // Reset vertical velocity completely for consistent jumps
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

        // Apply upward impulse force
        rb.AddForce(Vector3.up * currentStateData.jumpForce, ForceMode.Impulse);

        // Guarantee minimum upward velocity
        StartCoroutine(GuaranteeJumpVelocity());

        // Reset jump-related states
        isJumping = true;
        isHoldingJump = currentStateData.hasVariableJump;
        jumpHoldTimer = 0f;
        jumpBuffered = false;
        coyoteTimeTimer = 0f;
        canBunnyHop = false;

        // Reset air jumps (only if state allows double jump)
        if (currentStateData.hasDoubleJump)
        {
            airJumpsRemaining = currentStateData.maxAirJumps;
        }
        else
        {
            airJumpsRemaining = 0; // No air jumps if double jump disabled
        }
    }

    /// <summary>
    /// Executes an air jump (double jump).
    /// Uses airJumpForceMultiplier for potentially different jump height.
    /// </summary>
    private void AirJump()
    {
        // Reset vertical velocity
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

        // Apply upward impulse force with multiplier
        float airJumpForce = currentStateData.jumpForce * currentStateData.airJumpForceMultiplier;
        rb.AddForce(Vector3.up * airJumpForce, ForceMode.Impulse);

        // Consume one air jump
        airJumpsRemaining--;

        // Reset states
        isHoldingJump = currentStateData.hasVariableJump;
        jumpHoldTimer = 0f;
        jumpBuffered = false;
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
        float expectedMinVelocity = currentStateData.jumpForce * 0.9f;
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
    /// Uses wallJumpForce and wallJumpHorizontalMultiplier from state data.
    /// </summary>
    private void WallJump()
    {
        // Determine jump force (use wallJumpForce if set, otherwise use regular jumpForce)
        float verticalForce = currentStateData.wallJumpForce > 0
            ? currentStateData.wallJumpForce
            : currentStateData.jumpForce;

        // Reset all velocity for consistent wall jump
        rb.linearVelocity = new Vector3(0, 0, rb.linearVelocity.z);

        // Apply force: upward + away from wall
        Vector3 wallJumpForce = new Vector3(
            -wallDirection * verticalForce * currentStateData.wallJumpHorizontalMultiplier,
            verticalForce,
            0
        );
        rb.AddForce(wallJumpForce, ForceMode.Impulse);

        // Reset states
        isJumping = true;
        isHoldingJump = currentStateData.hasVariableJump;
        jumpHoldTimer = 0f;
        jumpBuffered = false;
        canWallJump = false;
        isWallSliding = false;

        // Reset air jumps
        airJumpsRemaining = currentStateData.maxAirJumps;
    }

    #endregion

    // ==================== BUNNY HOP SYSTEM ====================
    #region BUNNY HOP

    /// <summary>
    /// Tracks timing window for bunny hop after landing.
    /// Updates timeSinceLanding and canBunnyHop flag.
    /// </summary>
    private void UpdateBunnyHopTracking()
    {
        if (!currentStateData.hasBunnyHop)
        {
            return;
        }

        if (isGrounded)
        {
            timeSinceLanding += Time.deltaTime;

            // Check if within timing window
            if (timeSinceLanding <= currentStateData.bunnyHopTimingWindow)
            {
                canBunnyHop = true;
            }
            else
            {
                canBunnyHop = false;
            }
        }
        else
        {
            // Reset timer when airborne
            timeSinceLanding = 0f;
            canBunnyHop = false;
        }
    }

    /// <summary>
    /// Gradually decays bunny hop bonus when not chaining hops.
    /// Called in FixedUpdate for consistent decay rate.
    /// </summary>
    private void DecayBunnyHopBonus()
    {
        if (!currentStateData.hasBunnyHop || bunnyHopBonus <= 0)
        {
            return;
        }

        // Decay bonus over time
        bunnyHopBonus -= currentStateData.bunnyHopDecayRate * Time.fixedDeltaTime;
        bunnyHopBonus = Mathf.Max(bunnyHopBonus, 0f);
    }

    #endregion

    // ==================== APPLY METHODS ====================
    #region APPLY THINGS

    /// <summary>
    /// Switches to a new state by loading its ScriptableObject data.
    /// Updates Rigidbody mass and provides visual feedback via sprite color.
    /// 
    /// FIXED: Only resets air jumps when grounded to prevent infinite flight exploit.
    /// FIXED: Preserves dash cooldown across state switches.
    /// </summary>
    private void ApplyStateData(_PlayerStateData data)
    {
        currentStateData = data;
        rb.mass = data.weight;

        // Visual feedback using state color
        spriteRenderer.color = data.stateColor;

        // FIXED: Only reset air jumps when grounded
        // This prevents exploit: jump -> switch state -> get new air jumps -> fly infinitely
        if (isGrounded)
        {
            airJumpsRemaining = data.maxAirJumps;
        }
        else
        {
            // When switching states mid-air, recalculate remaining jumps
            // Use the LOWER value between current remaining and new state's max
            airJumpsRemaining = Mathf.Min(airJumpsRemaining, data.maxAirJumps);
        }

        // Start state switch cooldown
        stateSwitchCooldownTimer = data.stateSwitchCooldown;
        canSwitchState = false;

        // FIXED: Preserve dash availability across state switches
        // Don't reset canDash here - let cooldown system handle it
    }

    /// <summary>
    /// Handles horizontal movement with acceleration/deceleration.
    /// Features:
    /// - Respects canMoveOnGround toggle
    /// - Air control with multiplier (if enabled)
    /// - Bunny hop speed bonus
    /// - Starting speed boost for responsiveness
    /// - Ground friction when not moving
    /// - Prevents pushing into wall during wall slide
    /// - Automatic sprite flipping
    /// - Optional rotation-relative movement for Fez-style gameplay
    /// </summary>
    private void ApplyMovement()
    {
        // Check if ground movement is enabled
        bool canMove = isGrounded ? currentStateData.canMoveOnGround : currentStateData.hasAirControl;
        if (!canMove)
        {
            return;
        }

        // Get movement direction (world or camera-relative)
        Vector3 movementRight = GetMovementRight();

        // Calculate target velocity from input
        Vector3 targetVelocity = movementRight * moveInput.x * currentStateData.moveSpeed;

        // Apply bunny hop bonus if active
        if (currentStateData.hasBunnyHop && bunnyHopBonus > 0)
        {
            targetVelocity *= (1f + bunnyHopBonus);
        }

        // Apply air control multiplier if in air
        if (!isGrounded && currentStateData.hasAirControl)
        {
            targetVelocity *= currentStateData.airControlMultiplier;
        }

        // If wall sliding, prevent movement toward the wall
        if (isWallSliding)
        {
            bool tryingToMoveIntoWall = (wallDirection > 0 && moveInput.x > 0)
                                     || (wallDirection < 0 && moveInput.x < 0);

            if (tryingToMoveIntoWall)
            {
                targetVelocity = Vector3.zero;
            }
        }

        // Calculate current horizontal velocity
        Vector3 currentHorizontalVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        Vector3 targetHorizontalVel = new Vector3(targetVelocity.x, 0, targetVelocity.z);

        Vector3 velocityDifference = targetHorizontalVel - currentHorizontalVel;

        // Choose between acceleration (moving) or deceleration (stopping)
        float accelRate = (targetHorizontalVel.magnitude > 0.01f)
            ? currentStateData.acceleration
            : currentStateData.deceleration;

        // Apply starting boost when beginning movement from standstill (ground only)
        if (isGrounded && currentHorizontalVel.magnitude < 0.01f && targetHorizontalVel.magnitude > 0.01f)
        {
            velocityDifference *= currentStateData.startingSpeedBoost;
        }

        // Calculate and apply force
        Vector3 movement = velocityDifference * accelRate;
        rb.AddForce(movement, ForceMode.Force);

        // Apply ground friction when grounded and not inputting movement
        if (isGrounded && Mathf.Abs(moveInput.x) < 0.01f)
        {
            rb.linearVelocity = new Vector3(
                rb.linearVelocity.x * (1 - currentStateData.groundFriction * Time.fixedDeltaTime),
                rb.linearVelocity.y,
                rb.linearVelocity.z * (1 - currentStateData.groundFriction * Time.fixedDeltaTime)
            );
        }

        if (!isGrounded && isTouchingWall && !isWallSliding)
        {
            bool pushingIntoWall = (wallDirection > 0 && moveInput.x > 0)
                                || (wallDirection < 0 && moveInput.x < 0);

            if (pushingIntoWall)
            {
                // Zero out horizontal velocity component toward the wall
                Vector3 getMovementRight = GetMovementRight();
                float horizontalSpeed = Vector3.Dot(rb.linearVelocity, movementRight);

                // If moving toward wall, cancel that velocity
                if ((wallDirection > 0 && horizontalSpeed > 0) ||
                    (wallDirection < 0 && horizontalSpeed < 0))
                {
                    rb.linearVelocity = new Vector3(
                        0, // Cancel X velocity into wall
                        rb.linearVelocity.y,
                        rb.linearVelocity.z
                    );
                }
            }
        }

        // Handle sprite flipping based on movement direction
        if (moveInput.x > 0 && !isFacingRight)
        {
            StartFlip(true);
        }
        else if (moveInput.x < 0 && isFacingRight)
        {
            StartFlip(false);
        }
    }

    /// <summary>
    /// Applies custom gravity with multipliers for variable jump height.
    /// Respects hasVariableJump toggle - if disabled, always uses base gravity.
    /// Skips gravity when wall sliding to prevent overpowering wall friction.
    /// </summary>
    private void ApplyGravity()
    {
        // Don't apply gravity when wall sliding
        if (isWallSliding)
        {
            return;
        }

        float gravityMultiplier = 1f;

        // Only apply variable gravity if enabled for this state
        if (currentStateData.hasVariableJump)
        {
            // Reduce gravity while holding jump and moving upward
            if (isHoldingJump && rb.linearVelocity.y > 0)
            {
                gravityMultiplier = currentStateData.jumpHoldGravityMultiplier;
            }
            // Increase gravity when falling or when jump released early
            else if (rb.linearVelocity.y < 0 || (!isHoldingJump && rb.linearVelocity.y > 0))
            {
                gravityMultiplier = currentStateData.jumpReleaseGravityMultiplier;
            }
        }

        // Apply gravity force with multiplier
        rb.AddForce(Vector3.down * currentStateData.gravity * gravityMultiplier * rb.mass, ForceMode.Force);

        // Reset jumping flag when grounded
        if (isGrounded)
        {
            isJumping = false;
        }
    }

    /// <summary>
    /// Applies wall slide mechanics when conditions are met.
    /// Only active if hasWallSlide is TRUE in current state.
    /// Respects wallSlideRequiresInput toggle.
    /// </summary>
    private void ApplyWallSlide()
    {
        // Early exit if wall slide is disabled for this state
        if (!currentStateData.hasWallSlide)
        {
            isWallSliding = false;
            canWallJump = false;
            return;
        }

        // Use inspector override if enabled, otherwise use SO value
        bool requiresInput = overrideWallStickRequiresInput
            ? wallStickRequiresInput
            : currentStateData.wallSlideRequiresInput;

        // Base conditions: touching wall, airborne, falling
        bool shouldWallSlide = isTouchingWall && !isGrounded && rb.linearVelocity.y < 0;

        // Check input requirement
        if (requiresInput)
        {
            bool pushingTowardWall = (wallDirection > 0 && moveInput.x > 0)
                                  || (wallDirection < 0 && moveInput.x < 0);
            shouldWallSlide = shouldWallSlide && pushingTowardWall;
        }
        else
        {
            // If no input required, allow pushing away to drop off
            bool pushingAwayFromWall = (wallDirection > 0 && moveInput.x < -0.1f)
                                    || (wallDirection < 0 && moveInput.x > 0.1f);

            if (pushingAwayFromWall)
            {
                shouldWallSlide = false;
            }
        }

        if (shouldWallSlide)
        {
            isWallSliding = true;
            canWallJump = currentStateData.hasWallJump; // Only enable wall jump if state allows it

            // Apply wall slide friction (clamp fall speed)
            float slideSpeed = currentStateData.wallSlideFriction;
            rb.linearVelocity = new Vector3(
                rb.linearVelocity.x,
                Mathf.Max(rb.linearVelocity.y, slideSpeed),
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
        /*
        // Anti-stick: Cancel velocity into wall when touching but not sliding
        if (isTouchingWall && !isGrounded && !isWallSliding)
        {
            bool pushingIntoWall = (wallDirection > 0 && moveInput.x > 0)
                                || (wallDirection < 0 && moveInput.x < 0);

            if (pushingIntoWall)
            {
                rb.linearVelocity = new Vector3(
                    0,
                    rb.linearVelocity.y,
                    rb.linearVelocity.z
                );
            }
        }
        */
    }

    #endregion

    // ==================== INPUT HANDLERS ====================
    #region INPUT

    /// <summary>
    /// Called by Unity's Input System when movement input changes.
    /// Receives Vector2 from keyboard/controller.
    /// Respects canMove master toggle and world rotation freeze.
    /// </summary>
    public void OnMove(InputAction.CallbackContext ctx)
    {
        // Block input during rotation freeze
        if (isFrozenForRotation)
        {
            moveInput = Vector2.zero;
            return;
        }

        if (!currentStateData.canMove)
        {
            moveInput = Vector2.zero;
            return;
        }

        moveInput = ctx.ReadValue<Vector2>();
    }

    /// <summary>
    /// Called when state switch input is pressed.
    /// Toggles between Fire and Ice states.
    /// Respects canSwitchFromThisState toggle and cooldown system.
    /// 
    /// FIXED: Prevents state switching abuse for infinite flight.
    /// FIXED: Blocks state switching during world rotation.
    /// </summary>
    public void OnSwitchState(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed)
        {
            return;
        }

        // Block during world rotation
        if (isFrozenForRotation || IsWorldRotating())
        {
            return;
        }

        // Check if switching is allowed from current state
        if (!currentStateData.canSwitchFromThisState)
        {
            return;
        }

        // Check cooldown
        if (!canSwitchState)
        {
            return;
        }

        // FIXED: Track if we're mid-dash to prevent dash spam via state switch
        if (isDashing)
        {
            Debug.Log("Cannot switch states while dashing!");
            return;
        }

        // Perform state switch
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
    }

    /// <summary>
    /// Called by Unity's Input System for jump input.
    /// 
    /// On press (performed): Buffer the jump input if jump buffering is enabled
    /// On release (canceled): Stop variable height control
    /// 
    /// FIXED: Jump buffer now activates ANYTIME, not just when close to ground.
    /// </summary>
    public void OnJump(InputAction.CallbackContext ctx)
    {
        // Block during rotation freeze
        if (isFrozenForRotation)
        {
            return;
        }

        // Early exit if jumping disabled
        if (!currentStateData.canJump)
        {
            return;
        }

        // JUMP PRESSED
        if (ctx.performed)
        {
            // If jump buffering is ENABLED, always buffer the input
            if (currentStateData.hasJumpBuffer)
            {
                jumpBuffered = true;
                jumpBufferTimer = currentStateData.jumpBufferTime;
            }
            // If jump buffering is DISABLED, only allow immediate execution
            else
            {
                // Check if we can jump RIGHT NOW (no buffering)
                bool canJumpImmediately = isGrounded
                                       || (currentStateData.hasWallJump && canWallJump)
                                       || (currentStateData.hasDoubleJump && airJumpsRemaining > 0);

                if (canJumpImmediately)
                {
                    jumpBuffered = true;
                    jumpBufferTimer = 0.01f; // Tiny buffer for TryJump to catch it
                }
            }
        }

        // JUMP RELEASED
        if (ctx.canceled)
        {
            // Only stop holding if variable jump is enabled
            if (currentStateData.hasVariableJump)
            {
                isHoldingJump = false;
            }
        }
    }

    /// <summary>
    /// Called when dash input is pressed.
    /// Only works if hasDash is TRUE in current state.
    /// Respects hasAirDash toggle and cooldown system.
    /// </summary>
    public void OnDash(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed)
        {
            return;
        }

        // Block during rotation freeze
        if (isFrozenForRotation)
        {
            return;
        }

        // Check if dash is enabled for this state
        if (!currentStateData.hasDash)
        {
            return;
        }

        // Check air dash permission
        if (!isGrounded && !currentStateData.hasAirDash)
        {
            return;
        }

        // FIXED: Prevent dash spam via rapid state switching
        if (dashCooldownTimer > 0)
        {
            return;
        }

        // Check if dash is available (cooldown)
        if (!canDash || isDashing)
        {
            return;
        }

        // Determine dash direction from movement input
        Vector3 dashDir = Vector3.zero;

        if (moveInput.magnitude > 0.1f)
        {
            // Use rotation-relative direction if enabled
            if (useWorldRotation && useRotationRelativeMovement && worldRotationController != null)
            {
                Vector3 right = worldRotationController.GetCurrentRight();
                dashDir = (right * moveInput.x + Vector3.up * moveInput.y).normalized;
            }
            else
            {
                dashDir = new Vector3(moveInput.x, moveInput.y, 0).normalized;
            }
        }
        else
        {
            // Use rotation-relative facing direction if enabled
            if (useWorldRotation && useRotationRelativeMovement && worldRotationController != null)
            {
                Vector3 right = worldRotationController.GetCurrentRight();
                dashDir = isFacingRight ? right : -right;
            }
            else
            {
                dashDir = isFacingRight ? Vector3.right : Vector3.left;
            }
        }

        StartCoroutine(PerformDash(dashDir));
    }

    #endregion

    // ==================== DASH COROUTINES ====================
    #region DASH

    /// <summary>
    /// Executes the dash movement in the specified direction.
    /// Uses ScriptableObject values unless overrideDashSettings is TRUE.
    /// Implements cooldown system if dashCooldown > 0.
    /// </summary>
    private IEnumerator PerformDash(Vector3 direction)
    {
        isDashing = true;
        canDash = false;
        dashDirection = direction;

        // Determine which settings to use
        bool useInvincibility = overrideDashSettings ? dashHasInvincibility : currentStateData.dashHasInvincibility;
        float invincDuration = overrideDashSettings ? dashInvincibilityDuration : currentStateData.dashInvincibilityDuration;
        bool isFixedDistance = overrideDashSettings ? dashIsFixedDistance : currentStateData.dashIsFixedDistance;
        float distance = overrideDashSettings ? dashDistance : currentStateData.dashDistance;
        float duration = overrideDashSettings ? dashDuration : currentStateData.dashDuration;
        float speed = overrideDashSettings ? dashSpeed : currentStateData.dashSpeed;

        // Start invincibility effect if enabled
        if (useInvincibility)
        {
            StartCoroutine(InvincibilityFrames(invincDuration));
        }

        float elapsed = 0f;

        if (isFixedDistance)
        {
            // MODE 1: Fixed Distance Dash
            Vector3 startPos = transform.position;
            Vector3 targetPos = startPos + dashDirection * distance;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }
        }
        else
        {
            // MODE 2: Fixed Duration Dash
            while (elapsed < duration)
            {
                rb.linearVelocity = dashDirection * speed;
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        isDashing = false;

        // Start cooldown if enabled
        if (currentStateData.dashCooldown > 0)
        {
            dashCooldownTimer = currentStateData.dashCooldown;
        }
        else if (isGrounded)
        {
            // If no cooldown, reset on landing
            canDash = true;
        }
    }

    /// <summary>
    /// Provides invincibility frames with visual feedback.
    /// Flickers the sprite on/off to indicate invulnerability state.
    /// </summary>
    private IEnumerator InvincibilityFrames(float duration)
    {
        isInvincible = true;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            spriteRenderer.enabled = !spriteRenderer.enabled;
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }

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
    /// Smoothly rotates the VISUAL CHILD on Y-axis to create a flip effect.
    /// Uses a separate Transform so the main player GameObject stays axis-aligned.
    /// 
    /// How it works:
    /// - Only rotates visualTransform (the Visual child GameObject)
    /// - Player GameObject and all physics components remain unrotated
    /// - Camera (if child of Player) is NOT affected
    /// - Colliders and detection boxes remain axis-aligned
    /// 
    /// Rotation values:
    /// - 0° = Facing right (default Unity forward)
    /// - 180° = Facing left (rotated around Y-axis)
    /// 
    /// Duration: 0.15 seconds (quick and snappy for fast gameplay)
    /// </summary>
    /// <param name="flipToRight">True = face right (0°), False = face left (180°)</param>
    private IEnumerator FlipSprite(bool flipToRight)
    {
        isFlipping = true;

        if (visualTransform == null)
        {
            isFlipping = false;
            yield break;
        }

        // We animate the variable 'facingAngle' instead of the Transform directly
        float startAngle = facingAngle;
        float targetAngle = flipToRight ? 0f : 180f;

        float elapsed = 0f;
        float flipDuration = 0.15f;

        while (elapsed < flipDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flipDuration;

            // Lerp the angle value
            // LateUpdate will automatically apply (CameraAngle + facingAngle) this frame
            facingAngle = Mathf.Lerp(startAngle, targetAngle, t);

            yield return null;
        }

        // Ensure we land exactly on the target
        facingAngle = targetAngle;
        isFacingRight = flipToRight;
        isFlipping = false;
    }

    #endregion

    // ==================== GROUND DETECTION ====================
    #region GROUND DETECTION

    /// <summary>
    /// Hybrid ground detection system combining OverlapBox and Raycast.
    /// Resets bunny hop timer when landing detected.
    /// </summary>
    private void CheckGroundStatus()
    {
        Vector3 boxCenter = transform.position - new Vector3(0, GetComponent<Collider>().bounds.extents.y, 0);

        bool wasGrounded = isGrounded;

        // OverlapBox: Check for solid ground contact
        isGrounded = Physics.CheckBox(boxCenter, groundCheckSize / 2, Quaternion.identity, groundLayer);

        // Raycast: Predict ground within distance
        RaycastHit hit;
        Vector3 rayStart = boxCenter + Vector3.up * 0.1f;
        isAboutToLand = Physics.Raycast(rayStart, Vector3.down, out hit, groundRaycastDistance, groundLayer);

        // Reset bunny hop timer on landing
        if (!wasGrounded && isGrounded)
        {
            timeSinceLanding = 0f;
        }
    }

    #endregion

    // ==================== WALL DETECTION ====================
    #region WALL DETECTION

    /// <summary>
    /// Detects walls on both sides of the character using OverlapBox.
    /// Also uses Raycast to detect walls ahead in facing direction.
    /// </summary>
    private void CheckWallStatus()
    {
        Vector3 boxCenter = transform.position;

        // 1. Get the correct "Right" direction based on Camera Rotation
        Vector3 checkDirection = Vector3.right; // Default
        if (useWorldRotation && worldRotationController != null)
        {
            // Use the ABSOLUTE value to just get the axis (X or Z)
            // We handle Left/Right checking manually below
            Vector3 worldRight = worldRotationController.GetCurrentRight();

            // Snap to nearest axis to be safe (1,0,0) or (0,0,1)
            if (Mathf.Abs(worldRight.z) > Mathf.Abs(worldRight.x))
                checkDirection = new Vector3(0, 0, Mathf.Sign(worldRight.z));
            else
                checkDirection = new Vector3(Mathf.Sign(worldRight.x), 0, 0);
        }

        // 2. Check Right Side (Relative to Camera)
        bool rightWall = Physics.CheckBox(
            boxCenter + checkDirection * 0.5f,
            wallCheckSize / 2,
            Quaternion.identity,
            wallLayer
        );

        // 3. Check Left Side (Relative to Camera)
        bool leftWall = Physics.CheckBox(
            boxCenter - checkDirection * 0.5f,
            wallCheckSize / 2,
            Quaternion.identity,
            wallLayer
        );

        isTouchingWall = rightWall || leftWall;

        // 4. Determine Wall Direction (1 = Right, -1 = Left relative to Camera)
        wallDirection = rightWall ? 1 : (leftWall ? -1 : 0);

        // 5. Raycast for "Wall Ahead"
        // We calculate this based on where the player is actually facing visually
        Vector3 facingDir = isFacingRight ? checkDirection : -checkDirection;
        isWallAhead = Physics.Raycast(boxCenter, facingDir, wallRaycastDistance, wallLayer);
    }

    #endregion

    // ==================== PUBLIC GETTERS ====================
    #region PUBLIC GETTERS

    /// <summary>
    /// Returns TRUE if player is currently grounded.
    /// </summary>
    public bool IsGrounded()
    {
        return isGrounded;
    }

    /// <summary>
    /// Returns TRUE if player is currently dashing.
    /// </summary>
    public bool IsDashing()
    {
        return isDashing;
    }

    /// <summary>
    /// Returns TRUE if player is currently invincible.
    /// </summary>
    public bool IsInvincible()
    {
        return isInvincible;
    }

    /// <summary>
    /// Returns TRUE if player is wall sliding.
    /// </summary>
    public bool IsWallSliding()
    {
        return isWallSliding;
    }

    /// <summary>
    /// Returns the current state data.
    /// </summary>
    public _PlayerStateData GetCurrentStateData()
    {
        return currentStateData;
    }

    #endregion

    // ==================== DEBUG GIZMOS ====================
    #region GIZMOS

    /// <summary>
    /// Draws visual debugging information in the Scene view.
    /// Color coding helps identify detection states at a glance.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) return;

        Vector3 groundCheckCenter = transform.position - new Vector3(0, col.bounds.extents.y, 0);

        // Ground OverlapBox
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireCube(groundCheckCenter, groundCheckSize);

        // Ground Raycast
        Gizmos.color = isAboutToLand ? Color.yellow : Color.gray;
        Vector3 rayStart = groundCheckCenter + Vector3.up * 0.1f;
        Gizmos.DrawLine(rayStart, rayStart + Vector3.down * groundRaycastDistance);

        // Wall detection boxes
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(transform.position + Vector3.right * 0.5f, wallCheckSize);
        Gizmos.DrawWireCube(transform.position + Vector3.left * 0.5f, wallCheckSize);

        // Wall ahead raycast
        Gizmos.color = isWallAhead ? Color.cyan : Color.gray;
        Vector3 wallRayDirection = isFacingRight ? Vector3.right : Vector3.left;
        Gizmos.DrawLine(transform.position, transform.position + wallRayDirection * wallRaycastDistance);

        // World rotation direction indicators
        if (useWorldRotation && worldRotationController != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(transform.position + Vector3.up, worldRotationController.GetCurrentRight() * 2f);
            Gizmos.color = new Color(1f, 0.5f, 0f); // Orange
            Gizmos.DrawRay(transform.position + Vector3.up, worldRotationController.GetCurrentForward() * 2f);
        }
    }

    #endregion
}