using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// Fully modular player controller for 2.5D platformer with Fez-style world rotation.
/// All features are toggle-based through ScriptableObject states.
/// 
/// Core Systems:
/// - Data-driven state system (no hardcoded state exclusivity)
/// - Hybrid ground/wall detection with slope support
/// - Variable-height jumping with apex hang
/// - Wall slide, wall cling, and wall jump
/// - Dash system with charges and invincibility
/// - Bunny hop mechanic
/// - Double jump with configurable resets
/// - Air control with timing options
/// - Landing lag system
/// - Fez-style world rotation integration
/// - 4-directional wall detection (optional)
/// </summary>
public class FezPlayerController : MonoBehaviour
{
    #region Variables

    // ==================== REFERENCES ====================
    #region REFERENCES

    // Core Components
    private Rigidbody rb;
    private SpriteRenderer spriteRenderer;
    private Collider playerCollider;

    // Visual Transform - the child GameObject that rotates for sprite flipping
    [Header("Visual References")]
    [Tooltip("Drag the 'Visual' child GameObject here")]
    public Transform visualTransform;

    // State Data - ScriptableObjects containing all parameters for each state
    [Header("State Data")]
    public PlayerStateData fireStateData;
    public PlayerStateData iceStateData;
    private PlayerStateData currentStateData;

    /// <summary>
    /// Enum defining the available player states.
    /// </summary>
    public enum States { Fire, Ice }
    public States currentState;

    #endregion

    // ==================== MOVEMENT VARIABLES ====================
    #region MOVEMENT VARIABLES

    private float facingAngle = 0f;
    private Vector2 moveInput;
    private float currentSpeed;
    private bool isFacingRight = true;
    private bool isFlipping = false;

    #endregion

    // ==================== GROUND & WALL DETECTION ====================
    #region GROUND & WALL DETECTION

    [Header("Detection Settings")]
    public LayerMask groundLayer;
    public LayerMask wallLayer;

    [Header("Ground Detection")]
    public Vector3 groundCheckSize = new Vector3(0.9f, 0.1f, 0.9f);
    public float groundRaycastDistance = 0.2f;

    [Header("Wall Detection")]
    public Vector3 wallCheckSize = new Vector3(0.1f, 0.9f, 0.9f);
    public float wallRaycastDistance = 0.2f;

    // Detection states
    private bool isGrounded;
    private bool isAboutToLand;
    private bool isTouchingWall;
    private bool isWallAhead;
    private int wallDirection; // -1 = left, 1 = right, 0 = none

    // Slope detection (NEW)
    private bool isOnSlope;
    private float currentSlopeAngle;
    private Vector3 slopeNormal;
    private bool isOnSteepSlope;

    // 4-Direction wall detection cache (NEW)
    private bool[] wallStates = new bool[4]; // +X, -X, +Z, -Z
    private float fullWallCheckTimer = 0f;

    #endregion

    // ==================== JUMP VARIABLES ====================
    #region JUMP VARIABLES

    private bool isJumping;
    private bool jumpBuffered;
    private float jumpBufferTimer;
    private float coyoteTimeTimer;
    private float jumpHoldTimer;
    private bool isHoldingJump;
    private int airJumpsRemaining;

    // Air control timing (NEW)
    private float airControlDelayTimer;
    private float airControlRampProgress;
    private float timeInAir;

    #endregion

    // ==================== APEX HANG (NEW) ====================
    #region APEX HANG

    private bool isAtApex;

    #endregion

    // ==================== DASH VARIABLES ====================
    #region DASH VARIABLES

    [Header("Dash Settings (Inspector Overrides)")]
    [Tooltip("If TRUE, uses these inspector values instead of ScriptableObject values")]
    public bool overrideDashSettings = false;
    public bool dashHasInvincibility = true;
    public float dashInvincibilityDuration = 0.3f;
    public bool dashIsFixedDistance = false;
    public float dashDistance = 5f;
    public float dashDuration = 0.3f;
    public float dashSpeed = 20f;

    private bool isDashing;
    private bool isInvincible;
    private bool canDash = true;
    private float dashCooldownTimer;
    private Vector3 dashDirection;

    // Dash charges (NEW)
    private int currentDashCharges;
    private float dashRechargeTimer;
    private float dashRechargeDelayTimer;

    #endregion

    // ==================== WALL MECHANICS ====================
    #region WALL MECHANICS

    [Header("Wall Settings (Inspector Overrides)")]
    [Tooltip("If TRUE, uses this inspector value instead of ScriptableObject value")]
    public bool overrideWallStickRequiresInput = false;
    public bool wallStickRequiresInput = false;

    private bool isWallSliding;
    private bool canWallJump;
    private float wallSlideDelayTimer;

    // Wall cling (NEW)
    private bool isWallClinging;
    private float wallClingTimer;
    private float wallClingStamina = 100f;

    // Wall jump control lock (NEW)
    private bool isInWallJumpLock;
    private float wallJumpLockTimer;
    private int lastWallJumpDirection;

    #endregion

    // ==================== BUNNY HOP VARIABLES ====================
    #region BUNNY HOP VARIABLES

    private float bunnyHopBonus;
    private float timeSinceLanding;
    private bool canBunnyHop;
    private bool lastJumpWasBunnyHop;

    #endregion

    // ==================== LANDING LAG (NEW) ====================
    #region LANDING LAG

    private bool isInLandingLag;
    private float landingLagTimer;
    private float landingVelocity;

    #endregion

    // ==================== STATE SWITCHING ====================
    #region STATE SWITCHING

    private float stateSwitchCooldownTimer;
    private bool canSwitchState = true;

    #endregion

    // ============================================================================
    // WORLD ROTATION (FEZ-STYLE) - CINEMACHINE INTEGRATION
    // ============================================================================
    // This section handles integration with the FezWorldRotation controller.
    // The rotation system works by:
    // 1. Rotating a Cinemachine rig around the player (90° per step)
    // 2. Freezing the player during rotation animation
    // 3. Updating movement constraints after rotation
    // 4. Preserving momentum in the new movement direction
    // ============================================================================
    #region WORLD ROTATION VARIABLES

    [Header("World Rotation Settings")]
    // -------------------------------------------------------------------------
    // These settings control how the player interacts with the Fez rotation.
    // The key concept: after rotation, "right" on the joystick still moves
    // the player to the right side of the SCREEN, not world coordinates.
    // -------------------------------------------------------------------------

    [Tooltip("Enable Fez-style world rotation integration. If FALSE, all rotation features are disabled.")]
    public bool useWorldRotation = false;
    // Master toggle for the entire rotation system.
    // Set to TRUE when using FezWorldRotation in your scene.
    // When FALSE, all rotation-related code is bypassed for performance.

    [Tooltip("Reference to the FezWorldRotation controller. Will auto-find via FezWorldRotation.Instance if left empty.")]
    public FezWorldRotation worldRotationController;
    // Direct reference to the rotation controller.
    // Can be assigned in Inspector or left null for auto-detection.
    // Auto-detection uses the singleton pattern: FezWorldRotation.Instance

    [Tooltip("Freeze player movement and physics during rotation animation.")]
    public bool freezeDuringRotation = true;
    // When TRUE: Player stops completely during the 90° rotation animation.
    // When FALSE: Player can continue moving during rotation (can feel chaotic).
    // RECOMMENDED: Keep TRUE for authentic Fez feel.

    [Tooltip("Movement input is relative to current camera view. 'Right' means screen-right, not world +X.")]
    public bool useRotationRelativeMovement = true;
    // When TRUE: After rotating, pressing "right" still moves player right on screen.
    // When FALSE: Controls are always world-relative (confusing after rotation).
    // RECOMMENDED: Keep TRUE for intuitive controls.

    [Tooltip("Trigger depth snapping after rotation. FezDepthSnapper handles the actual snap.")]
    public bool snapAfterRotation = true;
    // When TRUE: Signals that a depth snap should occur after rotation.
    // The FezDepthSnapper component listens for rotation events and handles snapping.
    // When FALSE: Player stays at their current position (may float or clip).

    // -------------------------------------------------------------------------
    // Rotation Physics - How velocity is handled during/after rotation
    // -------------------------------------------------------------------------
    [Header("Rotation Physics")]

    [Tooltip("Zero out velocity in the depth direction (into/out of screen) after rotation.")]
    public bool clearDepthVelocityOnRotation = true;
    // The "depth" direction is perpendicular to the screen (forward/backward).
    // After rotation, you typically don't want the player drifting into the screen.
    // When TRUE: Depth velocity is set to 0 after each rotation.
    // When FALSE: Full 3D momentum is preserved (can cause player to drift off-screen).

    [Tooltip("Keep horizontal movement speed through rotation, just redirect it.")]
    public bool preserveHorizontalMomentum = true;
    // When TRUE: If player was running right at 5 units/sec, they continue at 5 units/sec
    //            in the new "right" direction after rotation.
    // When FALSE: All velocity is preserved as-is (movement may suddenly be into screen).
    // RECOMMENDED: TRUE for responsive, predictable movement.

    [Tooltip("Delay before wall detection resumes after rotation (prevents false positives).")]
    [Range(0f, 0.5f)]
    public float wallCheckDelayAfterRotation = 0.1f;
    // After rotation, there's a brief moment where the player hasn't fully settled.
    // Wall detection during this time can give incorrect results.
    // This delay pauses wall checks for the specified duration.
    // 0.1 seconds is usually enough for things to stabilize.

    // -------------------------------------------------------------------------
    // Wall Detection Mode - For advanced mechanics
    // -------------------------------------------------------------------------
    [Header("Wall Detection Mode")]

    [Tooltip("Check walls in all 4 world directions, not just camera-relative left/right.")]
    public bool use4DirectionalWallCheck = false;
    // Standard mode: Only checks walls to camera-left and camera-right.
    // 4-Direction mode: Also checks walls in front and behind (world +X, -X, +Z, -Z).
    // Use 4-direction for mechanics that need to know about walls in all directions.
    // Performance note: 4-direction mode does 4 physics casts instead of 2.

    [Tooltip("How often to update 4-direction wall cache (seconds). Lower = more responsive but more CPU.")]
    [Range(0.01f, 0.5f)]
    public float fullWallCheckInterval = 0.1f;
    // 4-direction checks run on a timer for performance.
    // 0.1 = 10 times per second, good balance of responsiveness and performance.
    // 0.01 = 100 times per second, very responsive but may impact performance.
    // 0.5 = 2 times per second, low CPU but walls may feel "sticky".

    // -------------------------------------------------------------------------
    // Private Rotation State - Internal tracking variables
    // -------------------------------------------------------------------------
    
    private bool isFrozenForRotation = false;
    // TRUE while player is frozen during rotation animation.
    // Prevents movement updates and physics simulation.
    // Set TRUE in FreezeForRotation(), set FALSE in UnfreezeFromRotation().

    private Vector3 frozenPosition;
    // The position where the player was frozen.
    // Used to hold player in place during rotation.
    // Stored when FreezeForRotation() is called.

    private Vector3 velocityBeforeFreeze;
    // The velocity the player had before being frozen.
    // Used to restore/transform momentum after rotation.
    // Stored when FreezeForRotation() is called.

    private float wallCheckDelayTimer;
    // Countdown timer for wall check delay after rotation.
    // Decremented each frame in Update().
    // Wall checks are skipped while this is > 0.

    #endregion

    // ============================================================================
    // DEBUG GIZMOS - Visual debugging in Scene view
    // ============================================================================
    // These settings control what debug visuals are drawn in the Scene view.
    // Gizmos help visualize hitboxes, detection areas, and system state.
    // Only visible in Unity Editor, not in builds.
    // ============================================================================
    #region DEBUG GIZMOS

    [Header("Debug Gizmos")]

    [Tooltip("Draw wall detection boxes in world-space coordinates.")]
    public bool showWorldSpaceGizmos = true;
    // Shows the actual physics cast boxes in world coordinates.
    // Useful for understanding where detection is happening.

    [Tooltip("Draw wall detection boxes relative to current camera view.")]
    public bool showCameraRelativeGizmos = true;
    // Shows boxes rotated to match current camera perspective.
    // Helps visualize how detection changes after rotation.

    [Tooltip("Highlight detected walls with different colors.")]
    public bool showDetectionResults = true;
    // Changes gizmo colors based on detection state.
    // Green = no wall, Red = wall detected.

    [Tooltip("Show slope detection normal and angle.")]
    public bool showSlopeGizmos = true;
    // Draws the ground normal vector when standing on slopes.
    // Also shows the calculated slope angle.

    #endregion

    // ============================================================================
    // FAST FALL - Quick descent mechanic
    // ============================================================================
    #region FAST FALL

    private bool isFastFalling;
    // TRUE while player is actively fast-falling.
    // Increases gravity multiplier for faster descent.

    private bool fastFallPressed;
    // Tracks if fast fall input is being held.
    // Used to trigger and maintain fast fall state.

    #endregion

    #endregion

    // ==================== UNITY LIFECYCLE METHODS ====================
    #region START, UPDATE, ETC...

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<Collider>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (visualTransform == null)
        {
            Debug.LogError("Visual Transform is not assigned!");
        }

        // Initialize visual rotation to default (facing +Z)
        if (visualTransform != null)
        {
            visualTransform.rotation = Quaternion.Euler(0, 0, 0);
        }
    }

    /// <summary>
    /// Called once when the game starts, after Awake.
    /// Initializes state, world rotation, and physics constraints.
    /// </summary>
    private void Start()
    {
        // --- SET INITIAL STATE ---
        // Start in Fire state by default
        currentState = States.Fire;
        ApplyStateData(fireStateData);

        // Set initial facing angle (0 = right, 180 = left)
        facingAngle = isFacingRight ? 0f : 180f;
        
        // --- INITIALIZE WORLD ROTATION ---
        // Set up event subscriptions with FezWorldRotation controller
        InitializeWorldRotation();

        // --- INITIALIZE DASH CHARGES ---
        // Set dash charges to max based on state data
        if (currentStateData != null)
        {
            currentDashCharges = currentStateData.maxDashCharges;
        }

        // --- SET INITIAL PHYSICS CONSTRAINTS ---
        // Constrain movement to the initial 2D plane
        if (worldRotationController != null)
        {
            // Use controller's current face to set constraints
            UpdatePhysicsConstraints(worldRotationController.GetCurrentFaceIndex());
        }
        else
        {
            // Default to face 0 (North) constraints
            UpdatePhysicsConstraints(0);
        }
    }

    /// <summary>
    /// Called every frame.
    /// Handles input, detection updates, and timer management.
    /// </summary>
    private void Update()
    {
        // --- CHECK FROZEN STATE ---
        // If frozen for rotation, skip all update logic
        if (isFrozenForRotation)
        {
            return;  // Player is frozen - do nothing
        }

        // Check if movement is allowed by current state
        if (!currentStateData.canMove)
        {
            return;  // Movement disabled
        }

        // --- UPDATE DETECTION STATES ---
        // Check ground and wall status each frame
        CheckGroundStatus();
        CheckWallStatus();

        // Update timers
        UpdateTimers();
        UpdateCooldowns();
        UpdateAirControlTiming();
        UpdateWallCling();
        UpdateWallJumpLock();
        UpdateLandingLag();
        UpdateDashCharges();

        // Track bunny hop timing
        UpdateBunnyHopTracking();

        // Evaluate jump
        TryJump();

        // Reset dash when landing (based on mode)
        HandleDashReset();

        // Update wall check delay
        if (wallCheckDelayTimer > 0f)
        {
            wallCheckDelayTimer -= Time.deltaTime;
        }
    }

    private void FixedUpdate()
    {
        // Handle frozen state
        if (isFrozenForRotation)
        {
            rb.linearVelocity = Vector3.zero;
            rb.position = frozenPosition;
            return;
        }

        if (!currentStateData.canMove)
        {
            return;
        }

        if (!isDashing)
        {
            ApplyMovement();
            ApplyWallSlide();
            ApplyWallCling();
            ApplyGravity();
            ApplySlopePhysics();
            ClampFallSpeed();
        }

        DecayBunnyHopBonus();
    }

    // =========================================================================
    // LateUpdate - Visual rotation and sprite facing
    // =========================================================================
    // LateUpdate runs after all Update() calls. We use it for visual updates
    // that should happen after movement calculations are done.
    // =========================================================================
    private void LateUpdate()
    {
        // Need visual transform reference
        if (visualTransform == null) return;

        // --- CALCULATE CAMERA ANGLE ---
        // We need to know which way the camera is facing so the sprite
        // always faces the camera correctly (billboard effect).
        
        float cameraAngle = 0f;  // Default to facing +Z
        
        // Try to get angle from rotation controller's rig
        if (worldRotationController != null)
        {
            // Get the current rotation angle from the controller
            cameraAngle = worldRotationController.GetCurrentAngle();
        }
        else if (Camera.main != null)
        {
            // Fallback: use main camera's Y rotation
            cameraAngle = Camera.main.transform.eulerAngles.y;
        }

        // --- APPLY VISUAL ROTATION ---
        // Combine camera angle with facing angle (0 or 180 for left/right)
        visualTransform.rotation = Quaternion.Euler(0, cameraAngle + facingAngle, 0);
    }

    #endregion

    // ============================================================================
    // WORLD ROTATION INTEGRATION
    // ============================================================================
    // This region handles all communication with the FezWorldRotation controller.
    // Key responsibilities:
    // 1. Subscribe to rotation events (start/complete)
    // 2. Freeze player during rotation
    // 3. Unfreeze and update constraints after rotation
    // 4. Provide direction queries for movement calculations
    // ============================================================================
    #region WORLD ROTATION

    /// <summary>
    /// Initializes world rotation integration.
    /// Called from Start() to set up event subscriptions.
    /// </summary>
    private void InitializeWorldRotation()
    {
        // --- CHECK IF ROTATION IS ENABLED ---
        // Skip initialization if rotation integration is disabled
        if (!useWorldRotation)
        {
            return;  // Not using rotation - nothing to initialize
        }

        // --- GET CONTROLLER REFERENCE ---
        // If no controller was assigned in Inspector, try to find it via singleton
        if (worldRotationController == null)
        {
            worldRotationController = FezWorldRotation.Instance;  // Singleton pattern
        }

        // --- VALIDATE CONTROLLER ---
        // Make sure we actually found a controller
        if (worldRotationController == null)
        {
            // No controller found - log warning and disable integration
            Debug.LogWarning("[_FezPlayerController] World rotation enabled but no FezWorldRotation found in scene!");
            useWorldRotation = false;  // Disable to prevent null reference errors
            return;
        }

        // --- SUBSCRIBE TO EVENTS ---
        // Connect our callbacks to the rotation controller's events
        
        // OnRotationStarted fires when rotation begins (freeze player here)
        worldRotationController.OnRotationStarted += OnWorldRotationStarted;
        
        // OnRotationCompleted fires when rotation ends (unfreeze player here)
        worldRotationController.OnRotationCompleted += OnWorldRotationCompleted;
    }

    /// <summary>
    /// Called when this component is disabled.
    /// IMPORTANT: Always unsubscribe from events to prevent memory leaks!
    /// </summary>
    private void OnDisable()
    {
        // Only unsubscribe if we have a controller reference
        if (worldRotationController != null)
        {
            // Remove our callbacks from the events
            worldRotationController.OnRotationStarted -= OnWorldRotationStarted;
            worldRotationController.OnRotationCompleted -= OnWorldRotationCompleted;
        }
    }

    /// <summary>
    /// Event callback: Called when world rotation STARTS.
    /// </summary>
    /// <param name="newFaceIndex">The face index we're rotating TO (0-3)</param>
    private void OnWorldRotationStarted(int newFaceIndex)
    {
        // --- CHECK IF WE SHOULD FREEZE ---
        // Only freeze if both rotation and freezing are enabled
        if (!useWorldRotation || !freezeDuringRotation)
        {
            return;  // Don't freeze
        }

        // --- FREEZE THE PLAYER ---
        FreezeForRotation();
    }

    /// <summary>
    /// Event callback: Called when world rotation COMPLETES.
    /// </summary>
    /// <param name="faceIndex">The face index we've arrived at (0-3)</param>
    private void OnWorldRotationCompleted(int faceIndex)
    {
        // Check if rotation integration is active
        if (!useWorldRotation)
        {
            return;
        }

        // --- UNFREEZE PLAYER ---
        // If we froze during rotation, now we unfreeze
        if (freezeDuringRotation)
        {
            UnfreezeFromRotation(faceIndex);
        }

        // --- TRIGGER DEPTH SNAP ---
        // Signal that a depth snap should occur (FezDepthSnapper handles this)
        if (snapAfterRotation)
        {
            SnapToCurrentPlane();
        }

        // --- UPDATE PHYSICS CONSTRAINTS ---
        // Adjust rigidbody constraints for new viewing angle
        UpdatePhysicsConstraints(faceIndex);

        // --- START WALL CHECK DELAY ---
        // Brief pause on wall detection to prevent false positives
        wallCheckDelayTimer = wallCheckDelayAfterRotation;
    }

    /// <summary>
    /// Freezes the player in place during rotation.
    /// Called when rotation starts.
    /// </summary>
    private void FreezeForRotation()
    {
        // Mark as frozen (checked in Update/FixedUpdate)
        isFrozenForRotation = true;
        
        // Store current position (we'll hold player here)
        frozenPosition = transform.position;
        
        // Store current velocity (may restore/transform after unfreeze)
        velocityBeforeFreeze = rb.linearVelocity;
        
        // Make rigidbody kinematic to stop all physics simulation
        // This prevents gravity, collisions, etc. during freeze
        rb.isKinematic = true;
    }

    /// <summary>
    /// Unfreezes the player after rotation completes.
    /// Handles momentum preservation/transformation.
    /// </summary>
    /// <param name="faceIndex">The new face index (0-3)</param>
    private void UnfreezeFromRotation(int faceIndex)
    {
        // --- RE-ENABLE PHYSICS ---
        rb.isKinematic = false;  // Allow physics simulation again
        
        // Mark as no longer frozen
        isFrozenForRotation = false;

        // --- HANDLE MOMENTUM PRESERVATION ---
        if (preserveHorizontalMomentum && worldRotationController != null)
        {
            // Calculate original horizontal speed (XZ plane only)
            // This ignores vertical velocity - we always preserve that separately
            float horizontalSpeed = new Vector2(
                velocityBeforeFreeze.x,
                velocityBeforeFreeze.z
            ).magnitude;
            
            // Get the new "right" direction from the rotation controller
            // After rotation, "right" points in a different world direction
            Vector3 newRight = worldRotationController.GetCurrentRight();
            
            // Determine if player was moving right (+1) or left (-1)
            // Dot product: positive = same direction, negative = opposite
            float direction = Mathf.Sign(
                Vector3.Dot(velocityBeforeFreeze.normalized, newRight)
            );
            
            // Handle edge case where player wasn't really moving
            // (Avoid NaN from normalizing zero vector)
            if (Mathf.Abs(direction) < 0.1f) direction = 1f;

            // Build new velocity vector:
            // - Horizontal: previous speed in new right direction
            // - Vertical: preserved from before (keep falling/rising)
            Vector3 newVelocity = new Vector3(
                newRight.x * horizontalSpeed * direction,  // X = right.x * speed
                velocityBeforeFreeze.y,                    // Y = preserved vertical
                newRight.z * horizontalSpeed * direction   // Z = right.z * speed
            );

            // --- CLEAR DEPTH VELOCITY ---
            // Optionally zero out velocity in the depth direction
            // This prevents drifting into/out of the screen
            if (clearDepthVelocityOnRotation)
            {
                // Determine which axis is "depth" based on current face
                // Face 0 (North) and 2 (South): Z is depth
                // Face 1 (East) and 3 (West): X is depth
                if (faceIndex % 2 == 0) // North/South - Z is into screen
                {
                    newVelocity.z = 0f;  // Clear Z velocity
                }
                else // East/West - X is into screen
                {
                    newVelocity.x = 0f;  // Clear X velocity
                }
            }

            // Apply the transformed velocity
            rb.linearVelocity = newVelocity;
        }
        else
        {
            // Not preserving momentum - just restore original velocity
            // Note: This can feel weird because movement direction may change
            rb.linearVelocity = velocityBeforeFreeze;
        }
    }

    /// <summary>
    /// Placeholder for depth snapping signal.
    /// Actual snapping is handled by FezDepthSnapper component.
    /// </summary>
    private void SnapToCurrentPlane()
    {
        // Validate we have controller reference
        if (worldRotationController == null)
        {
            return;
        }

        // FezDepthSnapper component handles the actual snapping logic.
        // This method exists as a signal/hook point if direct snapping is needed.
    }

    /// <summary>
    /// Gets the current "right" direction for movement.
    /// When rotation-relative movement is enabled, "right" changes with camera rotation.
    /// </summary>
    /// <returns>The right direction vector (normalized)</returns>
    private Vector3 GetMovementRight()
    {
        // Check if we should use camera-relative movement
        if (useWorldRotation && useRotationRelativeMovement && worldRotationController != null)
        {
            // Get "right" from the rotation controller
            // This changes based on current camera/face orientation
            return worldRotationController.GetCurrentRight();
        }

        // Fallback: world-space right (+X direction)
        // Used when rotation integration is disabled
        return Vector3.right;
    }

    /// <summary>
    /// Checks if the world is currently mid-rotation.
    /// Useful for preventing actions during rotation animation.
    /// </summary>
    /// <returns>TRUE if rotation animation is in progress</returns>
    public bool IsWorldRotating()
    {
        // Check if rotation system is active and controller exists
        if (!useWorldRotation || worldRotationController == null)
        {
            return false;  // Not using rotation, so never "rotating"
        }

        // Query the controller for rotation state
        return worldRotationController.IsRotating();
    }

    /// <summary>
    /// Checks if player is currently frozen for rotation.
    /// </summary>
    /// <returns>TRUE if player is frozen</returns>
    public bool IsFrozenForRotation()
    {
        return isFrozenForRotation;
    }

    /// <summary>
    /// Updates rigidbody constraints based on current camera face.
    /// Constrains movement to the 2D plane perpendicular to camera.
    /// </summary>
    /// <param name="faceIndex">Current face (0=North, 1=East, 2=South, 3=West)</param>
    private void UpdatePhysicsConstraints(int faceIndex)
    {
        // Need rigidbody reference
        if (rb == null) return;

        // Store old constraints to detect changes
        var oldConstraints = rb.constraints;

        // --- BUILD BASE CONSTRAINTS ---
        // Always freeze all rotation (player shouldn't tip over)
        RigidbodyConstraints constraints =
            RigidbodyConstraints.FreezeRotationX |    // No tipping forward/back
            RigidbodyConstraints.FreezeRotationZ |    // No tipping left/right
            RigidbodyConstraints.FreezeRotationY;     // No spinning

        // Get current velocity (may need to clear depth component)
        Vector3 velocity = rb.linearVelocity;

        // --- ADD DEPTH CONSTRAINT BASED ON FACE ---
        // The "depth" axis is the one going into the screen
        // We constrain movement on this axis to keep player in 2D plane
        
        if (faceIndex % 2 == 0) // Face 0 (North) or 2 (South)
        {
            // Camera facing +Z or -Z, so Z is the depth axis
            constraints |= RigidbodyConstraints.FreezePositionZ;
            
            // Clear Z velocity if constraints changed
            if (clearDepthVelocityOnRotation && oldConstraints != constraints)
            {
                velocity.z = 0f;
            }
        }
        else // Face 1 (East) or 3 (West)
        {
            // Camera facing +X or -X, so X is the depth axis
            constraints |= RigidbodyConstraints.FreezePositionX;
            
            // Clear X velocity if constraints changed
            if (clearDepthVelocityOnRotation && oldConstraints != constraints)
            {
                velocity.x = 0f;
            }
        }

        // --- APPLY CONSTRAINTS ---
        rb.constraints = constraints;

        // Apply modified velocity if constraints changed
        if (oldConstraints != constraints)
        {
            rb.linearVelocity = velocity;
        }
    }

    #endregion

    // ==================== GROUND DETECTION ====================
    #region GROUND DETECTION

    private void CheckGroundStatus()
    {
        if (playerCollider == null) return;

        Vector3 boxCenter = transform.position - new Vector3(0, playerCollider.bounds.extents.y, 0);
        bool wasGrounded = isGrounded;

        // OverlapBox check
        isGrounded = Physics.CheckBox(boxCenter, groundCheckSize / 2, Quaternion.identity, groundLayer);

        // Raycast for prediction
        RaycastHit hit;
        Vector3 rayStart = boxCenter + Vector3.up * 0.1f;
        isAboutToLand = Physics.Raycast(rayStart, Vector3.down, out hit, groundRaycastDistance, groundLayer);

        // Slope detection (NEW)
        CheckSlopeStatus(boxCenter);

        // Landing detection
        if (!wasGrounded && isGrounded)
        {
            OnLanded();
        }

        // Track time in air
        if (!isGrounded)
        {
            timeInAir += Time.deltaTime;
        }
        else
        {
            timeInAir = 0f;
        }
    }

    /// <summary>
    /// Checks slope angle and updates slope-related states.
    /// </summary>
    private void CheckSlopeStatus(Vector3 groundCheckCenter)
    {
        isOnSlope = false;
        isOnSteepSlope = false;
        currentSlopeAngle = 0f;
        slopeNormal = Vector3.up;

        if (!currentStateData.canWalkOnSlopes)
        {
            return;
        }

        // Raycast down to get slope normal
        RaycastHit hit;
        if (Physics.Raycast(groundCheckCenter + Vector3.up * 0.5f, Vector3.down, out hit, 1f, groundLayer))
        {
            slopeNormal = hit.normal;
            currentSlopeAngle = Vector3.Angle(Vector3.up, slopeNormal);

            if (currentSlopeAngle > 0.1f)
            {
                isOnSlope = true;

                if (currentSlopeAngle > currentStateData.maxSlopeAngle)
                {
                    isOnSteepSlope = true;
                }
            }
        }
    }

    /// <summary>
    /// Called when player lands on ground.
    /// </summary>
    private void OnLanded()
    {
        timeSinceLanding = 0f;
        isJumping = false;

        // Check for hard landing
        float fallSpeed = Mathf.Abs(landingVelocity);
        if (currentStateData.landingLagMode != LandingLagMode.Disabled &&
            fallSpeed >= currentStateData.hardLandingThreshold)
        {
            StartLandingLag(fallSpeed);
        }

        // Reset wall jump lock
        isInWallJumpLock = false;
        wallJumpLockTimer = 0f;

        // Reset air control timing
        airControlDelayTimer = 0f;
        airControlRampProgress = 1f;

        // Fast fall reset
        isFastFalling = false;
    }

    #endregion

    // ==================== WALL DETECTION ====================
    #region WALL DETECTION

    private void CheckWallStatus()
    {
        // Skip wall checks during delay after rotation
        if (wallCheckDelayTimer > 0f)
        {
            isTouchingWall = false;
            wallDirection = 0;
            return;
        }

        Vector3 boxCenter = transform.position;

        // Get check direction based on camera
        Vector3 checkDirection = GetWallCheckDirection();

        // Get wall check size based on face
        Vector3 checkSize = GetWallCheckSize();

        // Check Right Side
        bool rightWall = Physics.CheckBox(
            boxCenter + checkDirection * 0.5f,
            checkSize / 2,
            Quaternion.identity,
            wallLayer
        );

        // Check Left Side
        bool leftWall = Physics.CheckBox(
            boxCenter - checkDirection * 0.5f,
            checkSize / 2,
            Quaternion.identity,
            wallLayer
        );

        isTouchingWall = rightWall || leftWall;
        wallDirection = rightWall ? 1 : (leftWall ? -1 : 0);

        // Raycast for wall ahead
        Vector3 facingDir = isFacingRight ? checkDirection : -checkDirection;
        isWallAhead = Physics.Raycast(boxCenter, facingDir, wallRaycastDistance, wallLayer);

        // 4-Direction check (optional)
        if (use4DirectionalWallCheck)
        {
            fullWallCheckTimer -= Time.deltaTime;
            if (fullWallCheckTimer <= 0f)
            {
                CheckAllWallDirections(boxCenter);
                fullWallCheckTimer = fullWallCheckInterval;
            }
        }
    }

    /// <summary>
    /// Gets the wall check direction based on current camera face.
    /// Wall checks happen perpendicular to the screen (left/right of player).
    /// </summary>
    /// <returns>The axis to check for walls (X or Z direction)</returns>
    private Vector3 GetWallCheckDirection()
    {
        // Check if we're using rotation-aware wall detection
        if (useWorldRotation && worldRotationController != null)
        {
            // Get current "right" direction from rotation controller
            Vector3 worldRight = worldRotationController.GetCurrentRight();
            
            // Determine which world axis is more aligned with "right"
            // If Z is more significant, we're facing East/West (check on Z)
            // If X is more significant, we're facing North/South (check on X)
            if (Mathf.Abs(worldRight.z) > Mathf.Abs(worldRight.x))
            {
                // Right is mostly Z - check walls on Z axis
                return new Vector3(0, 0, 1);
            }
        }
        
        // Default: check walls on X axis (world right)
        return Vector3.right;
    }

    /// <summary>
    /// Gets the wall check box size, rotating dimensions based on current face.
    /// This ensures wall detection boxes are always oriented correctly.
    /// </summary>
    /// <returns>Wall check size with dimensions swapped if needed</returns>
    private Vector3 GetWallCheckSize()
    {
        // Check if we're using rotation-aware sizing
        if (useWorldRotation && worldRotationController != null)
        {
            // Get current face index to determine orientation
            int faceIndex = worldRotationController.GetCurrentFaceIndex();
            
            if (faceIndex % 2 == 0) // Face 0 (North) or 2 (South)
            {
                // Movement is on X axis - use standard size
                return new Vector3(wallCheckSize.x, wallCheckSize.y, wallCheckSize.z);
            }
            else // Face 1 (East) or 3 (West)
            {
                // Movement is on Z axis - swap X and Z dimensions
                // This rotates the check box to match the new orientation
                return new Vector3(wallCheckSize.z, wallCheckSize.y, wallCheckSize.x);
            }
        }
        
        // Default: use configured size as-is
        return wallCheckSize;
    }

    /// <summary>
    /// Checks all 4 cardinal directions for walls (optional mode).
    /// Results stored in wallStates array: [+X, -X, +Z, -Z]
    /// </summary>
    /// <param name="center">Center point to check from</param>
    private void CheckAllWallDirections(Vector3 center)
    {
        // --- CHECK +X DIRECTION (World Right) ---
        wallStates[0] = Physics.CheckBox(
            center + Vector3.right * 0.5f,                        // Offset to right
            new Vector3(0.1f, wallCheckSize.y, wallCheckSize.z) / 2,  // Thin box
            Quaternion.identity,                                  // No rotation
            wallLayer                                             // Layer mask
        );

        // --- CHECK -X DIRECTION (World Left) ---
        wallStates[1] = Physics.CheckBox(
            center + Vector3.left * 0.5f,                         // Offset to left
            new Vector3(0.1f, wallCheckSize.y, wallCheckSize.z) / 2,
            Quaternion.identity,
            wallLayer
        );

        // --- CHECK +Z DIRECTION (World Forward) ---
        wallStates[2] = Physics.CheckBox(
            center + Vector3.forward * 0.5f,                      // Offset forward
            new Vector3(wallCheckSize.x, wallCheckSize.y, 0.1f) / 2,  // Thin on Z
            Quaternion.identity,
            wallLayer
        );

        // -Z
        wallStates[3] = Physics.CheckBox(
            center + Vector3.back * 0.5f,
            new Vector3(wallCheckSize.x, wallCheckSize.y, 0.1f) / 2,
            Quaternion.identity,
            wallLayer
        );
    }

    #endregion

    // ==================== LANDING LAG (NEW) ====================
    #region LANDING LAG

    private void StartLandingLag(float fallSpeed)
    {
        float duration = currentStateData.landingLagDuration;

        // Reduce if jump is buffered
        if (currentStateData.reduceLandingLagOnJumpBuffer && jumpBuffered)
        {
            duration *= currentStateData.landingLagReductionMultiplier;
        }

        if (duration > 0f)
        {
            isInLandingLag = true;
            landingLagTimer = duration;
        }
    }

    private void UpdateLandingLag()
    {
        if (!isInLandingLag) return;

        landingLagTimer -= Time.deltaTime;
        if (landingLagTimer <= 0f)
        {
            isInLandingLag = false;
        }
    }

    /// <summary>
    /// Checks if an action is blocked by landing lag.
    /// </summary>
    private bool IsActionBlockedByLandingLag()
    {
        if (!isInLandingLag) return false;

        switch (currentStateData.landingLagMode)
        {
            case LandingLagMode.FreezeAll:
                return true;
            case LandingLagMode.PreventActionsOnly:
                return true; // Actions (jump/dash) blocked, movement allowed
            default:
                return false;
        }
    }

    /// <summary>
    /// Checks if movement is affected by landing lag.
    /// </summary>
    private float GetLandingLagMovementMultiplier()
    {
        if (!isInLandingLag) return 1f;

        switch (currentStateData.landingLagMode)
        {
            case LandingLagMode.FreezeAll:
                return 0f;
            case LandingLagMode.ReducedMovement:
                return currentStateData.landingLagMovementMultiplier;
            default:
                return 1f;
        }
    }

    #endregion

    // ==================== AIR CONTROL TIMING (NEW) ====================
    #region AIR CONTROL TIMING

    private void UpdateAirControlTiming()
    {
        if (isGrounded)
        {
            airControlDelayTimer = currentStateData.airControlDelay;
            airControlRampProgress = 0f;
            return;
        }

        // Delay countdown
        if (airControlDelayTimer > 0f)
        {
            airControlDelayTimer -= Time.deltaTime;
            return;
        }

        // Ramp up
        if (currentStateData.airControlRampUpTime > 0f && airControlRampProgress < 1f)
        {
            airControlRampProgress += Time.deltaTime / currentStateData.airControlRampUpTime;
            airControlRampProgress = Mathf.Clamp01(airControlRampProgress);
        }
        else
        {
            airControlRampProgress = 1f;
        }
    }

    /// <summary>
    /// Gets the effective air control multiplier including timing.
    /// </summary>
    private float GetEffectiveAirControl()
    {
        if (!currentStateData.hasAirControl) return 0f;

        float baseControl = currentStateData.airControlMultiplier;

        // Apply delay
        if (airControlDelayTimer > 0f) return 0f;

        // Apply ramp
        baseControl *= airControlRampProgress;

        // Apply apex bonus
        if (currentStateData.hasApexHang && isAtApex)
        {
            if (currentStateData.apexControlStacks)
            {
                baseControl *= currentStateData.apexAirControlMultiplier;
            }
            else
            {
                baseControl = Mathf.Max(baseControl, currentStateData.apexAirControlMultiplier * currentStateData.airControlMultiplier);
            }
        }

        // Apply wall jump lock
        if (isInWallJumpLock)
        {
            baseControl *= currentStateData.wallJumpControlLockMultiplier;
        }

        return baseControl;
    }

    #endregion

    // ==================== WALL CLING (NEW) ====================
    #region WALL CLING

    private void UpdateWallCling()
    {
        if (currentStateData.wallClingMode == WallClingMode.Disabled)
        {
            isWallClinging = false;
            return;
        }

        // Check if cling duration expired
        if (isWallClinging && currentStateData.maxWallClingDuration > 0f)
        {
            wallClingTimer += Time.deltaTime;
            if (wallClingTimer >= currentStateData.maxWallClingDuration)
            {
                // Force transition to slide
                isWallClinging = false;
            }
        }

        // Check stamina
        if (isWallClinging && currentStateData.wallClingStaminaCost > 0f)
        {
            wallClingStamina -= currentStateData.wallClingStaminaCost * Time.deltaTime;
            if (wallClingStamina <= 0f)
            {
                isWallClinging = false;
                wallClingStamina = 0f;
            }
        }

        // Restore stamina when grounded
        if (isGrounded)
        {
            wallClingStamina = 100f;
            wallClingTimer = 0f;
        }
    }

    private void ApplyWallCling()
    {
        if (!isWallClinging) return;

        // Apply cling gravity (can be 0 for perfect stick)
        if (currentStateData.wallClingGravity > 0f)
        {
            rb.linearVelocity = new Vector3(
                rb.linearVelocity.x,
                Mathf.Max(rb.linearVelocity.y, -currentStateData.wallClingGravity),
                rb.linearVelocity.z
            );
        }
        else
        {
            // Perfect stick - zero vertical velocity
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        }
    }

    /// <summary>
    /// Determines if player should enter wall cling state.
    /// </summary>
    private bool ShouldWallCling()
    {
        if (currentStateData.wallClingMode == WallClingMode.Disabled) return false;
        if (!isTouchingWall || isGrounded) return false;
        if (wallClingStamina <= 0f) return false;
        if (currentStateData.maxWallClingDuration > 0f && wallClingTimer >= currentStateData.maxWallClingDuration) return false;

        switch (currentStateData.wallClingMode)
        {
            case WallClingMode.ClingThenSlide:
                // Auto cling until duration expires
                return wallClingTimer < currentStateData.maxWallClingDuration;

            case WallClingMode.InputToggle:
                // Cling when pressing toward wall, slide otherwise
                bool pushingToWall = (wallDirection > 0 && moveInput.x > 0) || (wallDirection < 0 && moveInput.x < 0);
                return pushingToWall;

            case WallClingMode.HoldToStick:
                // Same as InputToggle but explicit
                bool holding = (wallDirection > 0 && moveInput.x > 0) || (wallDirection < 0 && moveInput.x < 0);
                return holding;

            default:
                return false;
        }
    }

    #endregion

    // ==================== WALL JUMP CONTROL LOCK (NEW) ====================
    #region WALL JUMP LOCK

    private void UpdateWallJumpLock()
    {
        if (!isInWallJumpLock) return;

        wallJumpLockTimer -= Time.deltaTime;
        if (wallJumpLockTimer <= 0f)
        {
            isInWallJumpLock = false;
        }

        // Check if lock only applies when returning to same wall
        if (currentStateData.wallJumpLockOnlyOnReturn)
        {
            // If moving away from the wall we jumped from, release lock early
            bool movingAway = (lastWallJumpDirection > 0 && moveInput.x < 0) ||
                              (lastWallJumpDirection < 0 && moveInput.x > 0);
            if (movingAway)
            {
                isInWallJumpLock = false;
            }
        }
    }

    private void StartWallJumpLock(int wallDir)
    {
        if (!currentStateData.hasWallJumpControlLock) return;

        isInWallJumpLock = true;
        wallJumpLockTimer = currentStateData.wallJumpControlLockDuration;
        lastWallJumpDirection = wallDir;
    }

    #endregion

    // ==================== DASH CHARGES (NEW) ====================
    #region DASH CHARGES

    private void UpdateDashCharges()
    {
        if (currentStateData.dashRechargeMode == DashRechargeMode.ResetOnLand)
        {
            // Handled in HandleDashReset
            return;
        }

        // Check recharge conditions
        if (currentStateData.dashRechargeOnlyGrounded && !isGrounded)
        {
            dashRechargeDelayTimer = currentStateData.dashRechargeDelay;
            return;
        }

        // Recharge delay
        if (dashRechargeDelayTimer > 0f)
        {
            dashRechargeDelayTimer -= Time.deltaTime;
            return;
        }

        // Recharge timer
        if (currentDashCharges < currentStateData.maxDashCharges && currentStateData.dashRechargeTime > 0f)
        {
            dashRechargeTimer += Time.deltaTime;

            if (dashRechargeTimer >= currentStateData.dashRechargeTime)
            {
                dashRechargeTimer = 0f;

                if (currentStateData.dashRechargeMode == DashRechargeMode.AllAtOnce)
                {
                    currentDashCharges = currentStateData.maxDashCharges;
                }
                else // OneAtATime
                {
                    currentDashCharges++;
                }
            }
        }
    }

    private void HandleDashReset()
    {
        if (!isGrounded) return;

        if (currentStateData.dashRechargeMode == DashRechargeMode.ResetOnLand)
        {
            currentDashCharges = currentStateData.maxDashCharges;
            dashRechargeTimer = 0f;
        }

        // Reset cooldown-based dash
        if (currentStateData.dashCooldown <= 0 && dashCooldownTimer <= 0)
        {
            canDash = true;
        }
    }

    #endregion
    // ==================== JUMP SYSTEM ====================
    #region JUMP

    private void UpdateTimers()
    {
        // Coyote Time
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
            coyoteTimeTimer = isGrounded ? 0.1f : 0f;
        }

        // Jump Buffer
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
            jumpBuffered = false;
        }

        // Jump Hold Timer
        if (currentStateData.hasVariableJump && isHoldingJump)
        {
            jumpHoldTimer += Time.deltaTime;

            if (jumpHoldTimer >= currentStateData.maxJumpHoldTime)
            {
                isHoldingJump = false;
            }
        }

        // Track landing velocity for landing lag
        if (!isGrounded)
        {
            landingVelocity = rb.linearVelocity.y;
        }
    }

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

    private void TryJump()
    {
        if (!currentStateData.canJump) return;
        if (IsActionBlockedByLandingLag()) return;

        // Check steep slope jump
        if (isOnSteepSlope && !currentStateData.canJumpOnSteepSlope)
        {
            return;
        }

        bool canGroundJump = coyoteTimeTimer > 0 && !isJumping && !isDashing;
        bool canAirJump = currentStateData.hasDoubleJump && airJumpsRemaining > 0 && !isGrounded && !isDashing;

        if (jumpBuffered)
        {
            // Priority 1: Wall jump (from cling or slide)
            if (currentStateData.hasWallJump && (canWallJump || (isWallClinging && currentStateData.canJumpFromWallCling)) && !isGrounded)
            {
                WallJump();
            }
            // Priority 2: Ground jump
            else if (canGroundJump)
            {
                Jump();
            }
            // Priority 3: Air jump
            else if (canAirJump)
            {
                AirJump();
            }
        }
    }

    private void Jump()
    {
        // Bunny hop check
        bool isBunnyHop = currentStateData.hasBunnyHop && canBunnyHop && bunnyHopBonus > 0;

        if (isBunnyHop)
        {
            bunnyHopBonus = Mathf.Min(bunnyHopBonus + currentStateData.bunnyHopSpeedBonus,
                                      currentStateData.bunnyHopMaxSpeed - 1f);
            lastJumpWasBunnyHop = true;
        }
        else if (currentStateData.hasBunnyHop && timeSinceLanding <= currentStateData.bunnyHopTimingWindow)
        {
            bunnyHopBonus = currentStateData.bunnyHopSpeedBonus;
            lastJumpWasBunnyHop = true;
        }
        else
        {
            lastJumpWasBunnyHop = false;
        }

        // Reset vertical velocity
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

        // Calculate jump force
        float force = currentStateData.jumpForce;

        // Moving jump bonus
        if (currentStateData.movingJumpBonus > 1f)
        {
            float horizontalSpeed = new Vector2(rb.linearVelocity.x, rb.linearVelocity.z).magnitude;
            if (horizontalSpeed >= currentStateData.movingJumpSpeedThreshold)
            {
                force *= currentStateData.movingJumpBonus;
            }
        }

        // Apply jump
        rb.AddForce(Vector3.up * force, ForceMode.Impulse);

        // Jump momentum boost
        if (currentStateData.jumpMomentumBoost && Mathf.Abs(moveInput.x) > 0.1f)
        {
            Vector3 movementRight = GetMovementRight();
            Vector3 horizontalBoost = movementRight * moveInput.x * currentStateData.jumpMomentumMultiplier;
            rb.AddForce(horizontalBoost, ForceMode.Impulse);
        }

        // Guarantee minimum velocity
        StartCoroutine(GuaranteeJumpVelocity());

        // Reset states
        isJumping = true;
        isHoldingJump = currentStateData.hasVariableJump;
        jumpHoldTimer = 0f;
        jumpBuffered = false;
        coyoteTimeTimer = 0f;
        canBunnyHop = false;

        // Reset air jumps
        if (currentStateData.hasDoubleJump)
        {
            airJumpsRemaining = currentStateData.maxAirJumps;
        }
        else
        {
            airJumpsRemaining = 0;
        }

        // Reset fast fall
        isFastFalling = false;
    }

    private void AirJump()
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

        float airJumpForce = currentStateData.jumpForce * currentStateData.airJumpForceMultiplier;
        rb.AddForce(Vector3.up * airJumpForce, ForceMode.Impulse);

        airJumpsRemaining--;

        isHoldingJump = currentStateData.hasVariableJump;
        jumpHoldTimer = 0f;
        jumpBuffered = false;

        // Reset fast fall
        isFastFalling = false;
    }

    private IEnumerator GuaranteeJumpVelocity()
    {
        yield return new WaitForFixedUpdate();

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

    private void WallJump()
    {
        float verticalForce = currentStateData.wallJumpForce > 0
            ? currentStateData.wallJumpForce
            : currentStateData.jumpForce;

        // Get push direction based on mode
        Vector3 pushDirection;
        if (currentStateData.wallJumpPushMode == WallJumpPushMode.CameraRelative)
        {
            Vector3 cameraRight = GetMovementRight();
            pushDirection = -wallDirection * cameraRight;
        }
        else // WallNormal
        {
            pushDirection = new Vector3(-wallDirection, 0, 0);
        }

        // Reset velocity
        rb.linearVelocity = new Vector3(0, 0, rb.linearVelocity.z);

        // Apply wall jump
        Vector3 wallJumpVelocity = new Vector3(
            pushDirection.x * verticalForce * currentStateData.wallJumpHorizontalMultiplier,
            verticalForce,
            pushDirection.z * verticalForce * currentStateData.wallJumpHorizontalMultiplier
        );
        rb.AddForce(wallJumpVelocity, ForceMode.Impulse);

        // Start wall jump lock
        StartWallJumpLock(wallDirection);

        // Reset states
        isJumping = true;
        isHoldingJump = currentStateData.hasVariableJump;
        jumpHoldTimer = 0f;
        jumpBuffered = false;
        canWallJump = false;
        isWallSliding = false;
        isWallClinging = false;
        wallClingTimer = 0f;

        // Reset air jumps if enabled
        if (currentStateData.resetAirJumpsOnWallJump)
        {
            airJumpsRemaining = currentStateData.maxAirJumps;
        }

        // Reset fast fall
        isFastFalling = false;
    }

    #endregion

    // ==================== BUNNY HOP SYSTEM ====================
    #region BUNNY HOP

    private void UpdateBunnyHopTracking()
    {
        if (!currentStateData.hasBunnyHop) return;

        if (isGrounded)
        {
            timeSinceLanding += Time.deltaTime;
            canBunnyHop = timeSinceLanding <= currentStateData.bunnyHopTimingWindow;
        }
        else
        {
            timeSinceLanding = 0f;
            canBunnyHop = false;
        }
    }

    private void DecayBunnyHopBonus()
    {
        if (!currentStateData.hasBunnyHop || bunnyHopBonus <= 0) return;

        bunnyHopBonus -= currentStateData.bunnyHopDecayRate * Time.fixedDeltaTime;
        bunnyHopBonus = Mathf.Max(bunnyHopBonus, 0f);
    }

    #endregion

    // ==================== APPLY METHODS ====================
    #region APPLY THINGS

    private void ApplyStateData(PlayerStateData data)
    {
        currentStateData = data;
        rb.mass = data.weight;
//        spriteRenderer.color = data.stateColor;

        if (isGrounded)
        {
            airJumpsRemaining = data.maxAirJumps;
        }
        else
        {
            airJumpsRemaining = Mathf.Min(airJumpsRemaining, data.maxAirJumps);
        }

        stateSwitchCooldownTimer = data.stateSwitchCooldown;
        canSwitchState = false;

        // Reset dash charges
        currentDashCharges = data.maxDashCharges;
    }

    private void ApplyMovement()
    {
        bool canMove = isGrounded ? currentStateData.canMoveOnGround : currentStateData.hasAirControl;
        if (!canMove) return;

        // Apply landing lag modifier
        float lagMultiplier = GetLandingLagMovementMultiplier();
        if (lagMultiplier <= 0f) return;

        Vector3 movementRight = GetMovementRight();
        Vector3 targetVelocity = movementRight * moveInput.x * currentStateData.moveSpeed;

        // Bunny hop bonus
        if (currentStateData.hasBunnyHop && bunnyHopBonus > 0)
        {
            targetVelocity *= (1f + bunnyHopBonus);
        }

        // Air control
        if (!isGrounded)
        {
            float airControl = GetEffectiveAirControl();
            targetVelocity *= airControl;

            // Air instant turn check
            if (!currentStateData.airInstantTurn)
            {
                Vector3 currentHorizontal = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
                float dot = Vector3.Dot(currentHorizontal.normalized, targetVelocity.normalized);
                if (dot < 0) // Trying to turn around
                {
                    targetVelocity *= 0.5f; // Reduced turning
                }
            }
        }

        // Steep slope handling
        if (isOnSteepSlope)
        {
            switch (currentStateData.steepSlopeBehavior)
            {
                case SteepSlopeBehavior.ForcedSlide:
                    targetVelocity = Vector3.zero;
                    break;
                case SteepSlopeBehavior.SlideReducedControl:
                    targetVelocity *= currentStateData.steepSlopeControlMultiplier;
                    break;
                // SlideWithControl - no modification
            }
        }

        // Wall slide movement prevention
        if (isWallSliding || isWallClinging)
        {
            bool tryingToMoveIntoWall = (wallDirection > 0 && moveInput.x > 0)
                                     || (wallDirection < 0 && moveInput.x < 0);
            if (tryingToMoveIntoWall)
            {
                targetVelocity = Vector3.zero;
            }
        }

        // Landing lag multiplier
        targetVelocity *= lagMultiplier;

        // Calculate velocity difference
        Vector3 currentHorizontalVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        Vector3 targetHorizontalVel = new Vector3(targetVelocity.x, 0, targetVelocity.z);
        Vector3 velocityDifference = targetHorizontalVel - currentHorizontalVel;

        // Acceleration/Deceleration
        float accelRate;
        if (targetHorizontalVel.magnitude > currentStateData.minimumMoveSpeed)
        {
            // Check for turn deceleration
            if (currentStateData.decelerateOnTurn && Vector3.Dot(currentHorizontalVel.normalized, targetHorizontalVel.normalized) < 0)
            {
                accelRate = currentStateData.deceleration * currentStateData.turnDecelerationMultiplier;
            }
            else
            {
                accelRate = currentStateData.acceleration;
            }
        }
        else
        {
            accelRate = isGrounded ? currentStateData.deceleration : currentStateData.airDeceleration;
        }

        // Starting boost
        if (isGrounded && currentHorizontalVel.magnitude < currentStateData.minimumMoveSpeed && targetHorizontalVel.magnitude > currentStateData.minimumMoveSpeed)
        {
            velocityDifference *= currentStateData.startingSpeedBoost;
        }

        // Apply force
        Vector3 movement = velocityDifference * accelRate;
        rb.AddForce(movement, ForceMode.Force);

        // Ground friction
        if (isGrounded && Mathf.Abs(moveInput.x) < 0.01f && !isOnSteepSlope)
        {
            rb.linearVelocity = new Vector3(
                rb.linearVelocity.x * (1 - currentStateData.groundFriction * Time.fixedDeltaTime),
                rb.linearVelocity.y,
                rb.linearVelocity.z * (1 - currentStateData.groundFriction * Time.fixedDeltaTime)
            );
        }

        // Sprite flipping
        if (moveInput.x > 0.1f && !isFacingRight)
        {
            StartFlip(true);
        }
        else if (moveInput.x < -0.1f && isFacingRight)
        {
            StartFlip(false);
        }
    }

    private void ApplyGravity()
    {
        // Skip during wall slide/cling
        if (isWallSliding || isWallClinging) return;

        float gravityMultiplier = 1f;

        // Check apex hang
        isAtApex = false;
        if (currentStateData.hasApexHang && !isGrounded)
        {
            float absYVel = Mathf.Abs(rb.linearVelocity.y);
            if (absYVel < currentStateData.apexVelocityThreshold && rb.linearVelocity.y > -1f)
            {
                isAtApex = true;
                gravityMultiplier = currentStateData.apexGravityMultiplier;
            }
        }

        // Variable jump gravity
        if (!isAtApex && currentStateData.hasVariableJump)
        {
            if (isHoldingJump && rb.linearVelocity.y > 0)
            {
                gravityMultiplier = currentStateData.jumpHoldGravityMultiplier;
            }
            else if (rb.linearVelocity.y < 0 || (!isHoldingJump && rb.linearVelocity.y > 0))
            {
                gravityMultiplier = currentStateData.jumpReleaseGravityMultiplier;
            }
        }

        // Fast fall
        if (isFastFalling && currentStateData.hasFastFall)
        {
            gravityMultiplier *= currentStateData.fastFallMultiplier;
        }

        rb.AddForce(Vector3.down * currentStateData.gravity * gravityMultiplier * rb.mass, ForceMode.Force);

        if (isGrounded)
        {
            isJumping = false;
        }
    }

    /// <summary>
    /// Applies slope physics when on steep slopes.
    /// </summary>
    private void ApplySlopePhysics()
    {
        if (!isOnSteepSlope) return;

        // Calculate slide direction (down the slope)
        Vector3 slideDirection = Vector3.Cross(Vector3.Cross(Vector3.up, slopeNormal), slopeNormal).normalized;

        // Apply slide force
        float slideForce = currentStateData.steepSlopeSlideSpeed * rb.mass;
        rb.AddForce(slideDirection * slideForce, ForceMode.Force);
    }

    /// <summary>
    /// Clamps fall speed to max.
    /// </summary>
    private void ClampFallSpeed()
    {
        if (rb.linearVelocity.y < -currentStateData.maxFallSpeed)
        {
            rb.linearVelocity = new Vector3(
                rb.linearVelocity.x,
                -currentStateData.maxFallSpeed,
                rb.linearVelocity.z
            );
        }
    }

    private void ApplyWallSlide()
    {
        if (!currentStateData.hasWallSlide)
        {
            isWallSliding = false;
            canWallJump = false;
            return;
        }

        // Check for wall cling first
        if (ShouldWallCling())
        {
            isWallClinging = true;
            isWallSliding = false;
            canWallJump = currentStateData.canJumpFromWallCling;
            return;
        }
        else
        {
            isWallClinging = false;
        }

        bool requiresInput = overrideWallStickRequiresInput
            ? wallStickRequiresInput
            : currentStateData.wallSlideRequiresInput;

        // Base conditions
        bool canSlide = isTouchingWall && !isGrounded;

        // Check rising vs falling
        if (!currentStateData.canWallSlideWhileRising && rb.linearVelocity.y > 0)
        {
            canSlide = false;
        }

        // Check wall slide delay
        if (canSlide && currentStateData.wallSlideDelay > 0f)
        {
            if (wallSlideDelayTimer < currentStateData.wallSlideDelay)
            {
                wallSlideDelayTimer += Time.deltaTime;
                canSlide = false;
            }
        }

        // Input requirement
        if (requiresInput && canSlide)
        {
            bool pushingTowardWall = (wallDirection > 0 && moveInput.x > 0)
                                  || (wallDirection < 0 && moveInput.x < 0);
            canSlide = canSlide && pushingTowardWall;
        }
        else if (!requiresInput && canSlide)
        {
            bool pushingAwayFromWall = (wallDirection > 0 && moveInput.x < -0.1f)
                                    || (wallDirection < 0 && moveInput.x > 0.1f);
            if (pushingAwayFromWall)
            {
                canSlide = false;
            }
        }

        if (canSlide && rb.linearVelocity.y < 0)
        {
            isWallSliding = true;
            canWallJump = currentStateData.hasWallJump;

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

            if (isGrounded)
            {
                canWallJump = false;
                wallSlideDelayTimer = 0f;
            }
        }

        // Reset delay timer when not touching wall
        if (!isTouchingWall)
        {
            wallSlideDelayTimer = 0f;
        }
    }

    #endregion

    // ==================== SPRITE FLIPPING ====================
    #region SPRITE FLIP

    private void StartFlip(bool flipToRight)
    {
        if (isFlipping) return;
        StartCoroutine(FlipCoroutine(flipToRight));
    }

    private IEnumerator FlipCoroutine(bool flipToRight)
    {
        isFlipping = true;

        if (visualTransform == null)
        {
            isFlipping = false;
            yield break;
        }

        float startAngle = facingAngle;
        float targetAngle = flipToRight ? 0f : 180f;

        float elapsed = 0f;
        float flipDuration = 0.15f;

        while (elapsed < flipDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flipDuration;
            facingAngle = Mathf.Lerp(startAngle, targetAngle, t);
            yield return null;
        }

        facingAngle = targetAngle;
        isFacingRight = flipToRight;
        isFlipping = false;
    }

    #endregion
    // ==================== INPUT HANDLERS ====================
    #region INPUT

    public void OnMove(InputAction.CallbackContext ctx)
    {
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

        // Fast fall input detection
        if (currentStateData.hasFastFall && !isGrounded)
        {
            if (currentStateData.fastFallRequiresPress)
            {
                if (ctx.performed && moveInput.y < -0.5f && !fastFallPressed)
                {
                    fastFallPressed = true;
                    isFastFalling = true;
                }
            }
            else
            {
                isFastFalling = moveInput.y < -0.5f;
            }
        }

        // Reset fast fall press tracking when grounded
        if (isGrounded)
        {
            fastFallPressed = false;
        }
    }

    public void OnSwitchState(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;

        if (isFrozenForRotation || IsWorldRotating()) return;

        if (!currentStateData.canSwitchFromThisState) return;

        if (!canSwitchState) return;

        if (isDashing)
        {
            Debug.Log("Cannot switch states while dashing!");
            return;
        }

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

    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (isFrozenForRotation) return;

        if (!currentStateData.canJump) return;

        // JUMP PRESSED
        if (ctx.performed)
        {
            if (currentStateData.hasJumpBuffer)
            {
                jumpBuffered = true;
                jumpBufferTimer = currentStateData.jumpBufferTime;
            }
            else
            {
                bool canJumpImmediately = isGrounded
                                       || (currentStateData.hasWallJump && (canWallJump || (isWallClinging && currentStateData.canJumpFromWallCling)))
                                       || (currentStateData.hasDoubleJump && airJumpsRemaining > 0);

                if (canJumpImmediately)
                {
                    jumpBuffered = true;
                    jumpBufferTimer = 0.01f;
                }
            }
        }

        // JUMP RELEASED
        if (ctx.canceled)
        {
            if (currentStateData.hasVariableJump)
            {
                isHoldingJump = false;
            }
        }
    }

    public void OnDash(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;

        if (isFrozenForRotation) return;

        if (!currentStateData.hasDash) return;

        if (!isGrounded && !currentStateData.hasAirDash) return;

        if (IsActionBlockedByLandingLag()) return;

        // Check charges
        if (currentDashCharges <= 0) return;

        if (dashCooldownTimer > 0) return;

        if (isDashing) return;

        // Determine dash direction based on mode
        Vector3 dashDir = CalculateDashDirection();

        StartCoroutine(PerformDash(dashDir));
    }

    /// <summary>
    /// Calculates dash direction based on current dash direction mode.
    /// </summary>
    private Vector3 CalculateDashDirection()
    {
        Vector3 dashDir = Vector3.zero;
        Vector3 movementRight = GetMovementRight();

        switch (currentStateData.dashDirectionMode)
        {
            case DashDirectionMode.FacingDirection:
                dashDir = isFacingRight ? movementRight : -movementRight;
                break;

            case DashDirectionMode.InputDirection:
                if (Mathf.Abs(moveInput.x) > 0.1f)
                {
                    dashDir = movementRight * Mathf.Sign(moveInput.x);
                }
                else
                {
                    dashDir = isFacingRight ? movementRight : -movementRight;
                }
                break;

            case DashDirectionMode.InputWithVertical:
                if (moveInput.magnitude > 0.1f)
                {
                    dashDir = movementRight * moveInput.x;
                    if (currentStateData.dashCanBeVertical)
                    {
                        dashDir += Vector3.up * moveInput.y;
                    }
                    dashDir = dashDir.normalized;
                }
                else
                {
                    dashDir = isFacingRight ? movementRight : -movementRight;
                }
                break;

            case DashDirectionMode.EightDirectional:
                if (moveInput.magnitude > 0.1f)
                {
                    dashDir = movementRight * moveInput.x;
                    if (currentStateData.dashCanBeVertical)
                    {
                        dashDir += Vector3.up * moveInput.y;
                    }

                    // Snap to 8 directions if cardinal only
                    if (currentStateData.dashCardinalOnly)
                    {
                        // Find dominant direction
                        if (Mathf.Abs(dashDir.x) > Mathf.Abs(dashDir.y) + Mathf.Abs(dashDir.z))
                        {
                            dashDir = new Vector3(Mathf.Sign(dashDir.x), 0, 0);
                        }
                        else if (Mathf.Abs(dashDir.y) > Mathf.Abs(dashDir.x) + Mathf.Abs(dashDir.z))
                        {
                            dashDir = new Vector3(0, Mathf.Sign(dashDir.y), 0);
                        }
                        else if (Mathf.Abs(dashDir.z) > Mathf.Abs(dashDir.x) + Mathf.Abs(dashDir.y))
                        {
                            dashDir = new Vector3(0, 0, Mathf.Sign(dashDir.z));
                        }
                        else
                        {
                            dashDir = dashDir.normalized;
                        }
                    }
                    else
                    {
                        dashDir = dashDir.normalized;
                    }
                }
                else
                {
                    dashDir = isFacingRight ? movementRight : -movementRight;
                }
                break;
        }

        // Ensure we have a valid direction
        if (dashDir.magnitude < 0.1f)
        {
            dashDir = isFacingRight ? movementRight : -movementRight;
        }

        return dashDir.normalized;
    }

    #endregion

    // ==================== DASH COROUTINES ====================
    #region DASH

    private IEnumerator PerformDash(Vector3 direction)
    {
        isDashing = true;
        currentDashCharges--;
        dashDirection = direction;

        // Get settings
        bool useInvincibility = overrideDashSettings ? dashHasInvincibility : currentStateData.dashHasInvincibility;
        float invincDuration = overrideDashSettings ? dashInvincibilityDuration : currentStateData.dashInvincibilityDuration;
        bool isFixedDistance = overrideDashSettings ? dashIsFixedDistance : currentStateData.dashIsFixedDistance;
        float distance = overrideDashSettings ? dashDistance : currentStateData.dashDistance;
        float duration = overrideDashSettings ? dashDuration : currentStateData.dashDuration;
        float speed = overrideDashSettings ? dashSpeed : currentStateData.dashSpeed;

        // Start invincibility
        if (useInvincibility)
        {
            StartCoroutine(InvincibilityFrames(invincDuration));
        }

        float elapsed = 0f;
        Vector3 startPos = transform.position;

        if (isFixedDistance)
        {
            // Fixed Distance Dash
            Vector3 targetPos = startPos + dashDirection * distance;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Check wall collision
                if (currentStateData.dashCancelOnWall && isWallAhead)
                {
                    break;
                }

                // Apply gravity during dash if enabled
                if (currentStateData.dashHasGravity)
                {
                    Vector3 currentTarget = Vector3.Lerp(startPos, targetPos, t);
                    currentTarget.y -= currentStateData.gravity * currentStateData.dashGravityMultiplier * elapsed * elapsed * 0.5f;
                    transform.position = currentTarget;
                }
                else
                {
                    transform.position = Vector3.Lerp(startPos, targetPos, t);
                }

                yield return null;
            }
        }
        else
        {
            // Fixed Duration Dash
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                // Check wall collision
                if (currentStateData.dashCancelOnWall && isWallAhead)
                {
                    break;
                }

                Vector3 dashVelocity = dashDirection * speed;

                // Apply gravity during dash if enabled
                if (currentStateData.dashHasGravity)
                {
                    dashVelocity.y -= currentStateData.gravity * currentStateData.dashGravityMultiplier;
                }

                rb.linearVelocity = dashVelocity;
                yield return null;
            }
        }

        // End dash
        isDashing = false;

        // Apply end velocity
        if (currentStateData.dashEndVelocityMultiplier > 0f)
        {
            rb.linearVelocity = dashDirection * speed * currentStateData.dashEndVelocityMultiplier;
        }
        else
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }

        // Start cooldown
        if (currentStateData.dashCooldown > 0)
        {
            dashCooldownTimer = currentStateData.dashCooldown;
            canDash = false;
        }

        // Start recharge delay
        dashRechargeDelayTimer = currentStateData.dashRechargeDelay;
        dashRechargeTimer = 0f;
    }

    private IEnumerator InvincibilityFrames(float duration)
    {
        isInvincible = true;
        float elapsed = 0f;

        Color originalColor = spriteRenderer.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // Visual feedback based on mode
            switch (currentStateData.dashInvincibilityVisual)
            {
                case DashInvincibilityVisual.Flicker:
                    bool visible = Mathf.Sin(elapsed * currentStateData.dashFlickerSpeed) > 0;
                    spriteRenderer.enabled = visible;
                    break;

                case DashInvincibilityVisual.Transparent:
                    Color transColor = originalColor;
                    transColor.a = currentStateData.dashTransparencyAlpha;
                    spriteRenderer.color = transColor;
                    break;

                case DashInvincibilityVisual.ColorShift:
                    spriteRenderer.color = currentStateData.dashInvincibilityColor;
                    break;

                case DashInvincibilityVisual.Trail:
                    // Trail would need a separate trail renderer component
                    // For now, use flicker as fallback
                    bool trailVisible = Mathf.Sin(elapsed * currentStateData.dashFlickerSpeed * 0.5f) > 0;
                    spriteRenderer.enabled = trailVisible;
                    break;

                case DashInvincibilityVisual.None:
                default:
                    break;
            }

            yield return null;
        }

        // Reset visuals
        spriteRenderer.enabled = true;
        spriteRenderer.color = currentStateData.stateColor;
        isInvincible = false;
    }

    #endregion

    // ==================== PUBLIC GETTERS ====================
    #region PUBLIC GETTERS

    public bool IsGrounded() => isGrounded;
    public bool IsDashing() => isDashing;
    public bool IsInvincible() => isInvincible;
    public bool IsWallSliding() => isWallSliding;
    public bool IsWallClinging() => isWallClinging;
    public bool IsOnSlope() => isOnSlope;
    public bool IsOnSteepSlope() => isOnSteepSlope;
    public float GetCurrentSlopeAngle() => currentSlopeAngle;
    public bool IsAtApex() => isAtApex;
    public bool IsInLandingLag() => isInLandingLag;
    public int GetCurrentDashCharges() => currentDashCharges;
    public int GetMaxDashCharges() => currentStateData?.maxDashCharges ?? 1;
    public float GetWallClingStamina() => wallClingStamina;
    public PlayerStateData GetCurrentStateData() => currentStateData;

    /// <summary>
    /// Gets the 4-direction wall states if enabled.
    /// Index: 0=+X, 1=-X, 2=+Z, 3=-Z
    /// </summary>
    public bool[] GetWallStates() => wallStates;

    #endregion

    // ==================== DEBUG GIZMOS ====================
    #region GIZMOS

    private void OnDrawGizmosSelected()
    {
        if (playerCollider == null)
        {
            playerCollider = GetComponent<Collider>();
        }
        if (playerCollider == null) return;

        Vector3 groundCheckCenter = transform.position - new Vector3(0, playerCollider.bounds.extents.y, 0);

        // Ground detection
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireCube(groundCheckCenter, groundCheckSize);

        // Ground raycast
        Gizmos.color = isAboutToLand ? Color.yellow : Color.gray;
        Vector3 rayStart = groundCheckCenter + Vector3.up * 0.1f;
        Gizmos.DrawLine(rayStart, rayStart + Vector3.down * groundRaycastDistance);

        // Slope visualization
        if (showSlopeGizmos && isOnSlope)
        {
            Gizmos.color = isOnSteepSlope ? Color.red : Color.cyan;
            Gizmos.DrawRay(groundCheckCenter, slopeNormal * 2f);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(groundCheckCenter + Vector3.up, $"Slope: {currentSlopeAngle:F1}°");
#endif
        }

        // World-space wall gizmos
        if (showWorldSpaceGizmos)
        {
            Gizmos.color = new Color(0f, 0f, 1f, 0.3f);
            Gizmos.DrawWireCube(transform.position + Vector3.right * 0.5f, wallCheckSize);
            Gizmos.DrawWireCube(transform.position + Vector3.left * 0.5f, wallCheckSize);
            Gizmos.DrawWireCube(transform.position + Vector3.forward * 0.5f, new Vector3(wallCheckSize.z, wallCheckSize.y, wallCheckSize.x));
            Gizmos.DrawWireCube(transform.position + Vector3.back * 0.5f, new Vector3(wallCheckSize.z, wallCheckSize.y, wallCheckSize.x));
        }

        // Camera-relative wall gizmos
        if (showCameraRelativeGizmos)
        {
            Vector3 checkDir = GetWallCheckDirection();
            Vector3 checkSize = GetWallCheckSize();

            Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
            Gizmos.DrawWireCube(transform.position + checkDir * 0.5f, checkSize);

            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
            Gizmos.DrawWireCube(transform.position - checkDir * 0.5f, checkSize);
        }

        // Detection results
        if (showDetectionResults)
        {
            if (isTouchingWall)
            {
                Vector3 checkDir = GetWallCheckDirection();
                Vector3 wallPos = transform.position + checkDir * wallDirection * 0.5f;

                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(wallPos, 0.3f);
            }

            // 4-direction results
            if (use4DirectionalWallCheck)
            {
                Color activeColor = new Color(1f, 0.5f, 0f, 0.8f);
                if (wallStates[0]) // +X
                {
                    Gizmos.color = activeColor;
                    Gizmos.DrawSphere(transform.position + Vector3.right * 0.7f, 0.15f);
                }
                if (wallStates[1]) // -X
                {
                    Gizmos.color = activeColor;
                    Gizmos.DrawSphere(transform.position + Vector3.left * 0.7f, 0.15f);
                }
                if (wallStates[2]) // +Z
                {
                    Gizmos.color = activeColor;
                    Gizmos.DrawSphere(transform.position + Vector3.forward * 0.7f, 0.15f);
                }
                if (wallStates[3]) // -Z
                {
                    Gizmos.color = activeColor;
                    Gizmos.DrawSphere(transform.position + Vector3.back * 0.7f, 0.15f);
                }
            }
        }

        // Wall ahead raycast
        Gizmos.color = isWallAhead ? Color.cyan : Color.gray;
        Vector3 facingDir = isFacingRight ? GetWallCheckDirection() : -GetWallCheckDirection();
        Gizmos.DrawLine(transform.position, transform.position + facingDir * wallRaycastDistance);

        // World rotation direction indicators
        if (useWorldRotation && worldRotationController != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(transform.position + Vector3.up, worldRotationController.GetCurrentRight() * 2f);
            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawRay(transform.position + Vector3.up, worldRotationController.GetCurrentForward() * 2f);
        }

        // State indicators
#if UNITY_EDITOR
        string stateText = "";
        if (isWallSliding) stateText += "SLIDE ";
        if (isWallClinging) stateText += "CLING ";
        if (isAtApex) stateText += "APEX ";
        if (isInLandingLag) stateText += "LAG ";
        if (isFastFalling) stateText += "FAST ";
        if (isInWallJumpLock) stateText += "LOCK ";

        if (!string.IsNullOrEmpty(stateText))
        {
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, stateText);
        }
#endif
    }

    #endregion
}
