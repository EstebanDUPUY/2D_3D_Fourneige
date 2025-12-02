// ============================================================================
// FUSED PLAYER CONTROLLER - CELESTE FEEL + FEZ FEATURES
// ============================================================================
// This controller merges:
// - Celeste's INSTANT, SNAPPY movement (no floatiness)
// - All Fez functionalities (slopes, landing lag, bunny hop, wall cling, etc.)
// - Fez-style world rotation integration
// - Maximum editability and toggleability
//
// CORE PHILOSOPHY:
// 1. INSTANT RESPONSE - No sluggish acceleration
// 2. PREDICTABLE PHYSICS - Consistent jump heights and fall speeds
// 3. GENEROUS ASSISTS - Coyote time, jump buffering, apex hang
// 4. ALL FEATURES TOGGLEABLE - Via ScriptableObject
//
// SETUP:
// 1. Add to player GameObject with Rigidbody and Collider
// 2. Create Fused_PlayerStateData asset and assign
// 3. Configure layer masks for ground/wall detection
// 4. Connect Input System actions
// ============================================================================

using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class FusedPlayerController : MonoBehaviour
{
    // ========================================================================
    // SECTION: COMPONENT REFERENCES
    // ========================================================================
    
    #region COMPONENT REFERENCES
    
    private Rigidbody rb;
    private Collider playerCollider;
    private SpriteRenderer spriteRenderer;
    
    #endregion
    
    // ========================================================================
    // SECTION: INSPECTOR REFERENCES
    // ========================================================================
    
    #region INSPECTOR REFERENCES
    
    [Header("═══════════════════════════════════════")]
    [Header("        REQUIRED REFERENCES")]
    [Header("═══════════════════════════════════════")]
    
    [Tooltip("Child transform containing the sprite.")]
    [SerializeField]
    private Transform visualTransform;
    
    [Tooltip("Primary player state data asset.")]
    [SerializeField]
    private FusedPlayerStateData primaryStateData;
    
    [Tooltip("Optional secondary state for state-switching.")]
    [SerializeField]
    private FusedPlayerStateData secondaryStateData;
    
    private FusedPlayerStateData currentState;
    
    #endregion
    
    // ========================================================================
    // SECTION: LAYER MASKS
    // ========================================================================
    
    #region LAYER MASKS
    
    [Header("═══════════════════════════════════════")]
    [Header("        DETECTION LAYERS")]
    [Header("═══════════════════════════════════════")]
    
    [Tooltip("Layers that count as ground.")]
    [SerializeField]
    private LayerMask groundLayer = 1;
    
    [Tooltip("Layers that count as walls.")]
    [SerializeField]
    private LayerMask wallLayer = 1;
    
    #endregion
    
    // ========================================================================
    // SECTION: DETECTION SETTINGS
    // ========================================================================
    
    #region DETECTION SETTINGS
    
    [Header("═══════════════════════════════════════")]
    [Header("        DETECTION SETTINGS")]
    [Header("═══════════════════════════════════════")]
    
    [Tooltip("Ground detection box size.")]
    [SerializeField]
    private Vector3 groundCheckSize = new Vector3(0.9f, 0.1f, 0.9f);
    
    [Tooltip("Extra raycast distance for landing prediction.")]
    [SerializeField]
    private float groundRaycastDistance = 0.3f;
    
    [Tooltip("Wall detection box size.")]
    [SerializeField]
    private Vector3 wallCheckSize = new Vector3(0.1f, 0.8f, 0.8f);
    
    [Tooltip("Distance to check for walls.")]
    [SerializeField]
    private float wallCheckDistance = 0.5f;
    
    #endregion
    
    // ========================================================================
    // SECTION: WORLD ROTATION SETTINGS
    // ========================================================================
    
    #region WORLD ROTATION SETTINGS
    
    [Header("═══════════════════════════════════════")]
    [Header("        WORLD ROTATION (FEZ-STYLE)")]
    [Header("═══════════════════════════════════════")]
    
    [Tooltip("Enable Fez-style world rotation.")]
    [SerializeField]
    private bool useWorldRotation = true;
    
    [Tooltip("World rotation controller.")]
    [SerializeField]
    private FusedWorldRotation worldRotationController;
    
    [Tooltip("Check walls in all 4 directions.")]
    [SerializeField]
    private bool use4DirectionalWallCheck = false;
    
    [SerializeField]
    [Range(0.01f, 0.2f)]
    private float wallCheckInterval = 0.05f;
    
    #endregion
    
    // ========================================================================
    // SECTION: DEBUG SETTINGS
    // ========================================================================
    
    #region DEBUG SETTINGS
    
    [Header("═══════════════════════════════════════")]
    [Header("        DEBUG VISUALIZATION")]
    [Header("═══════════════════════════════════════")]
    
    [SerializeField] private bool showGroundGizmos = true;
    [SerializeField] private bool showWallGizmos = true;
    [SerializeField] private bool showDirectionGizmos = true;
    [SerializeField] private bool showSlopeGizmos = true;
    [SerializeField] private bool logStateChanges = false;
    
    #endregion
    
    // ========================================================================
    // SECTION: INPUT STATE VARIABLES
    // ========================================================================
    
    #region INPUT STATE
    
    // --- RAW INPUT (directly from Input System) ---
    // These store the unprocessed input values from the controller/keyboard.
    // They are set in the OnMove callback and may be processed before use.
    private Vector2 rawMoveInput;
    
    // --- PROCESSED INPUT (after deadzone, normalization, etc) ---
    // This is the input actually used for movement calculations.
    // It has been processed according to the current state's input settings.
    private Vector2 moveInput;
    
    // --- SMOOTHED INPUT (for optional input smoothing) ---
    // Used when input smoothing is enabled to reduce jitter.
    private Vector2 smoothedMoveInput;
    
    // --- DIRECTION SNAP CACHE ---
    // Stores the last snapped direction for dash/wall jump.
    // This ensures consistent direction during multi-frame actions.
    private Vector2 lastSnappedDirection;
    
    // --- INPUT HISTORY (for moving average smoothing) ---
    // Circular buffer storing recent input values for averaging.
    private Vector2[] inputHistory;
    private int inputHistoryIndex;
    private bool inputHistoryFilled;
    
    // --- BUTTON STATES ---
    // These track the state of action buttons (jump, dash, etc).
    private bool jumpHeld;
    private bool jumpPressedThisFrame;
    private bool dashPressedThisFrame;
    private bool fastFallPressedThisFrame;
    private bool grabHeld;
    
    #endregion
    
    // ========================================================================
    // SECTION: DETECTION STATE VARIABLES
    // ========================================================================
    
    #region DETECTION STATE
    
    private bool isGrounded;
    private bool wasGrounded;
    private bool isAboutToLand;
    private bool isTouchingWall;
    private int wallDirection;
    private bool[] wallStates = new bool[4];
    private float wallCheckTimer;
    private float wallDetectionDelayTimer;
    
    // Slope detection (Fez feature)
    private bool isOnSlope;
    private float currentSlopeAngle;
    private Vector3 slopeNormal;
    private bool isOnSteepSlope;
    
    #endregion
    
    // ========================================================================
    // SECTION: MOVEMENT STATE VARIABLES
    // ========================================================================
    
    #region MOVEMENT STATE
    
    private bool isFacingRight = true;
    private float facingAngle;
    private bool isFlipping;
    private float currentHorizontalSpeed;
    private float accelerationTimer;
    private float decelerationTimer;
    
    // Bunny hop (Fez feature)
    private float bunnyHopBonus;
    private float timeSinceLanding;
    private bool canBunnyHop;
    
    #endregion
    
    // ========================================================================
    // SECTION: JUMP STATE VARIABLES
    // ========================================================================
    
    #region JUMP STATE
    
    private bool isJumping;
    private bool jumpBuffered;
    private float jumpBufferTimer;
    private float coyoteTimer;
    private float jumpHoldTimer;
    private bool canExtendJump;
    private int airJumpsRemaining;
    
    // Air control timing (Fez feature)
    private float airControlDelayTimer;
    private float airControlRampProgress;
    private float timeInAir;
    
    #endregion
    
    // ========================================================================
    // SECTION: GRAVITY STATE VARIABLES
    // ========================================================================
    
    #region GRAVITY STATE
    
    private bool isAtApex;
    private bool isFastFalling;
    private float currentGravityMultiplier = 1f;
    
    #endregion
    
    // ========================================================================
    // SECTION: LANDING LAG (FEZ FEATURE)
    // ========================================================================
    
    #region LANDING LAG
    
    private bool isInLandingLag;
    private float landingLagTimer;
    private float landingVelocity;
    
    #endregion
    
    // ========================================================================
    // SECTION: WALL MECHANICS STATE VARIABLES
    // ========================================================================
    
    #region WALL STATE
    
    private bool isWallSliding;
    private bool isWallClimbing;
    private bool isWallClinging;
    private float wallSlideDelayTimer;
    private float wallClingTimer;
    private bool canWallJump;
    private bool isInWallJumpLock;
    private float wallJumpLockTimer;
    private int lastWallJumpDirection;
    private float currentStamina;
    private float staminaRefillDelayTimer;
    
    #endregion
    
    // ========================================================================
    // SECTION: DASH STATE VARIABLES
    // ========================================================================
    
    #region DASH STATE
    
    private bool isDashing;
    private int currentDashCharges;
    private float dashCooldownTimer;
    private float dashRefillDelayTimer;
    private float dashRefillTimer;
    private Vector3 dashDirection;
    private bool isInvincible;
    private float timeSinceWallJump;
    private Coroutine activeDashCoroutine;
    
    #endregion
    
    // ========================================================================
    // SECTION: WORLD ROTATION STATE VARIABLES
    // ========================================================================
    
    #region ROTATION STATE
    
    private bool isFrozenForRotation;
    private Vector3 frozenPosition;
    private Vector3 velocityBeforeFreeze;
    
    #endregion
    
    // ========================================================================
    // SECTION: STATE SWITCHING VARIABLES
    // ========================================================================
    
    #region STATE SWITCHING
    
    private bool stateSwitchOnCooldown;
    private float stateSwitchCooldownTimer;
    private int currentStateIndex;
    
    #endregion
    
    // ========================================================================
    // SECTION: EVENTS
    // ========================================================================
    
    #region EVENTS
    
    public event Action OnLanded;
    public event Action OnLeftGround;
    public event Action<int> OnJump; // 0=ground, 1=air, 2=wall
    public event Action<Vector3> OnDashStarted;
    public event Action OnDashEnded;
    public event Action OnWallGrab;
    public event Action OnWallRelease;
    public event Action OnStaminaDepleted;
    public event Action<int> OnStateChanged;
    public event Action OnBunnyHop;
    
    #endregion
    
    // ========================================================================
    // SECTION: UNITY LIFECYCLE - AWAKE
    // ========================================================================
    
    #region AWAKE
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<Collider>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        
        if (visualTransform == null)
        {
            Debug.LogError("[Fused_PlayerController] Visual Transform not assigned!", this);
        }
        
        if (primaryStateData == null)
        {
            Debug.LogError("[Fused_PlayerController] Primary State Data not assigned!", this);
        }
    }
    
    #endregion
    
    // ========================================================================
    // SECTION: UNITY LIFECYCLE - START
    // ========================================================================
    
    #region START
    
    private void Start()
    {
        // Initialize the current state to the primary state data asset.
        // This determines all movement parameters, abilities, and input settings.
        currentState = primaryStateData;
        currentStateIndex = 0;
        ApplyStateData();
        
        // Initialize resource values (dash charges, stamina, air jumps) from state data.
        // These may be modified during gameplay and reset on various conditions.
        if (currentState != null)
        {
            currentDashCharges = currentState.maxDashCharges;
            currentStamina = currentState.maxStamina;
            airJumpsRemaining = currentState.maxAirJumps;
        }
        
        // Initialize facing direction to right (0 degrees).
        // The visual sprite will be rotated based on this value.
        isFacingRight = true;
        facingAngle = 0f;
        
        // Initialize the world rotation system if enabled.
        // This handles Fez-style 90-degree world rotation.
        InitializeWorldRotation();
        
        // Configure the Rigidbody with appropriate constraints and settings.
        // Freezes rotation and sets collision detection mode.
        ConfigureRigidbody();
        
        // Initialize input processing systems.
        // This sets up the input history buffer for smoothing and resets input state.
        InitializeInputProcessing();
        
        // Log the initialization if debug logging is enabled.
        if (logStateChanges)
        {
            Debug.Log($"[Fused_PlayerController] Initialized with state: {currentState?.stateName ?? "NULL"}");
        }
    }
    
    /// <summary>
    /// Initializes the input processing system.
    /// Sets up the input history buffer for moving average smoothing
    /// and resets all input-related state variables to their defaults.
    /// </summary>
    private void InitializeInputProcessing()
    {
        // Initialize input history buffer for moving average smoothing.
        // Default size of 30 frames covers the maximum configurable window.
        inputHistory = new Vector2[30];
        inputHistoryIndex = 0;
        inputHistoryFilled = false;
        
        // Reset all input vectors to zero.
        rawMoveInput = Vector2.zero;
        moveInput = Vector2.zero;
        smoothedMoveInput = Vector2.zero;
        lastSnappedDirection = Vector2.zero;
        
        // Log initialization if debug logging is enabled.
        if (logStateChanges)
        {
            Debug.Log("[Fused_PlayerController] Input processing initialized.");
        }
    }
    
    private void ApplyStateData()
    {
        if (currentState == null) return;
        
        if (rb != null)
        {
            rb.mass = currentState.mass;
        }
        
        if (spriteRenderer != null)
        {
            spriteRenderer.color = currentState.stateColor;
        }
    }
    
    private void ConfigureRigidbody()
    {
        if (rb == null) return;
        
        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                        RigidbodyConstraints.FreezeRotationY |
                        RigidbodyConstraints.FreezeRotationZ;
        
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        
        if (!useWorldRotation)
        {
            rb.constraints |= RigidbodyConstraints.FreezePositionZ;
        }
        else if (worldRotationController != null)
        {
            UpdatePhysicsConstraints(worldRotationController.GetCurrentFaceIndex());
        }
    }
    
    #endregion
    
    // ========================================================================
    // SECTION: WORLD ROTATION INITIALIZATION
    // ========================================================================
    
    #region WORLD ROTATION INIT
    
    private void InitializeWorldRotation()
    {
        if (!useWorldRotation) return;
        
        if (worldRotationController == null)
        {
            worldRotationController = FusedWorldRotation.Instance;
        }
        
        if (worldRotationController == null)
        {
            Debug.LogWarning("[Fused_PlayerController] World rotation enabled but no FezWorldRotation found!");
            useWorldRotation = false;
            return;
        }
        
        worldRotationController.OnRotationStarted += OnWorldRotationStarted;
        worldRotationController.OnRotationCompleted += OnWorldRotationCompleted;
    }
    
    private void OnDisable()
    {
        if (worldRotationController != null)
        {
            worldRotationController.OnRotationStarted -= OnWorldRotationStarted;
            worldRotationController.OnRotationCompleted -= OnWorldRotationCompleted;
        }
    }
    
    private void OnWorldRotationStarted(int newFaceIndex)
    {
        if (!useWorldRotation) return;
        if (currentState != null && currentState.freezeDuringRotation)
        {
            FreezeForRotation();
        }
    }
    
    private void OnWorldRotationCompleted(int faceIndex)
    {
        if (!useWorldRotation) return;
        
        if (currentState != null && currentState.freezeDuringRotation)
        {
            UnfreezeFromRotation(faceIndex);
        }
        
        UpdatePhysicsConstraints(faceIndex);
        
        if (currentState != null)
        {
            wallDetectionDelayTimer = currentState.wallDetectionDelayAfterRotation;
            
            if (currentState.refillDashOnRotation)
            {
                currentDashCharges = currentState.maxDashCharges;
            }
            
            if (currentState.refillStaminaOnRotation)
            {
                currentStamina = currentState.maxStamina;
            }
        }
    }
    
    private void FreezeForRotation()
    {
        isFrozenForRotation = true;
        frozenPosition = transform.position;
        velocityBeforeFreeze = rb.linearVelocity;
        rb.isKinematic = true;
    }
    
    private void UnfreezeFromRotation(int faceIndex)
    {
        rb.isKinematic = false;
        isFrozenForRotation = false;
        
        if (currentState != null && currentState.preserveMomentumThroughRotation && worldRotationController != null)
        {
            float horizontalSpeed = new Vector2(velocityBeforeFreeze.x, velocityBeforeFreeze.z).magnitude;
            Vector3 newRight = worldRotationController.GetCurrentRight();
            float sign = Mathf.Sign(Vector3.Dot(velocityBeforeFreeze, newRight));
            
            Vector3 newVelocity = newRight * horizontalSpeed * sign;
            newVelocity.y = velocityBeforeFreeze.y;
            
            if (currentState.clearDepthVelocityAfterRotation)
            {
                Vector3 forward = worldRotationController.GetCurrentForward();
                newVelocity -= forward * Vector3.Dot(newVelocity, forward);
            }
            
            rb.linearVelocity = newVelocity;
        }
    }
    
    private void UpdatePhysicsConstraints(int faceIndex)
    {
        if (rb == null) return;
        
        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                        RigidbodyConstraints.FreezeRotationY |
                        RigidbodyConstraints.FreezeRotationZ;
        
        if (!useWorldRotation)
        {
            rb.constraints |= RigidbodyConstraints.FreezePositionZ;
        }
        else
        {
            // Constrain depth axis based on current face
            switch (faceIndex)
            {
                case 0: // North (camera looks -Z)
                case 2: // South (camera looks +Z)
                    rb.constraints |= RigidbodyConstraints.FreezePositionZ;
                    break;
                case 1: // East (camera looks -X)
                case 3: // West (camera looks +X)
                    rb.constraints |= RigidbodyConstraints.FreezePositionX;
                    break;
            }
        }
    }
    
    #endregion
    
    // ========================================================================
    // SECTION: UNITY LIFECYCLE - UPDATE
    // ========================================================================
    
    #region UPDATE
    
    private void Update()
    {
        // Skip all updates if player is frozen for world rotation.
        // The player remains completely still during rotation transitions.
        if (isFrozenForRotation) return;
        
        // Skip updates if no state data or movement is disabled.
        // This allows complete player freezing via the state data.
        if (currentState == null || !currentState.canMove) return;
        
        // =====================================================================
        // INPUT PROCESSING (must happen before any input-dependent logic)
        // =====================================================================
        // Process raw input through deadzone, normalization, and smoothing.
        // This converts raw stick input into clean, usable movement input.
        // Only processes per-frame if the setting is PerFrame or Both.
        if (currentState.inputProcessingTime == Fused_InputProcessingTime.PerFrame ||
            currentState.inputProcessingTime == Fused_InputProcessingTime.Both)
        {
            ProcessInputPerFrame();
        }
        
        // =====================================================================
        // DETECTION UPDATES
        // =====================================================================
        // Update all detection states (ground, slopes, walls).
        // These determine what actions the player can take.
        UpdateGroundDetection();
        UpdateSlopeDetection();
        UpdateWallDetection();
        
        // =====================================================================
        // TIMER UPDATES
        // =====================================================================
        // Update all gameplay timers (coyote time, buffers, cooldowns, etc).
        // These enable the generous assist systems like jump buffering.
        UpdateJumpTimers();
        UpdateDashTimers();
        UpdateWallTimers();
        UpdateStamina();
        UpdateStateSwitchCooldown();
        UpdateLandingLag();
        UpdateBunnyHop();
        UpdateAirControlTiming();
        
        // =====================================================================
        // INPUT PROCESSING (actions)
        // =====================================================================
        // Process action inputs like jump and dash.
        // These check buffers and conditions to execute abilities.
        ProcessJumpInput();
        ProcessDashInput();
        
        // =====================================================================
        // WALL MECHANICS UPDATE
        // =====================================================================
        // Update wall slide, climb, and cling states based on current conditions.
        UpdateWallMechanics();
        
        // =====================================================================
        // RESET FRAME FLAGS
        // =====================================================================
        // Reset single-frame input flags at the end of Update.
        // These flags are set in input callbacks and consumed once per frame.
        jumpPressedThisFrame = false;
        dashPressedThisFrame = false;
        fastFallPressedThisFrame = false;
    }
    
    #endregion
    
    // ========================================================================
    // SECTION: UNITY LIFECYCLE - FIXED UPDATE
    // ========================================================================
    
    #region FIXED UPDATE
    
    private void FixedUpdate()
    {
        if (isFrozenForRotation)
        {
            rb.linearVelocity = Vector3.zero;
            rb.position = frozenPosition;
            return;
        }
        
        if (currentState == null || !currentState.canMove) return;
        if (isDashing) return;
        
        // Check landing lag restrictions
        if (isInLandingLag && currentState.landingLagMode == Fused_LandingLagMode.FreezeAll)
        {
            return;
        }
        
        ApplyHorizontalMovement();
        ApplyGravity();
        ApplyWallPhysics();
        ApplySlopePhysics();
        ClampFallSpeed();
    }
    
    #endregion
    
    // ========================================================================
    // SECTION: UNITY LIFECYCLE - LATE UPDATE
    // ========================================================================
    
    #region LATE UPDATE
    
    private void LateUpdate()
    {
        if (visualTransform == null) return;
        
        float cameraAngle = 0f;
        
        if (worldRotationController != null && useWorldRotation)
        {
            cameraAngle = worldRotationController.GetCurrentAngle();
        }
        else if (Camera.main != null)
        {
            cameraAngle = Camera.main.transform.eulerAngles.y;
        }
        
        visualTransform.rotation = Quaternion.Euler(0f, cameraAngle + facingAngle, 0f);
    }
    
    #endregion
    
    // ========================================================================
    // SECTION: GROUND DETECTION
    // ========================================================================
    
    #region GROUND DETECTION
    
    private void UpdateGroundDetection()
    {
        wasGrounded = isGrounded;
        
        if (playerCollider == null) return;
        
        Vector3 groundCheckCenter = transform.position - 
            new Vector3(0f, playerCollider.bounds.extents.y, 0f);
        
        isGrounded = Physics.CheckBox(
            groundCheckCenter,
            groundCheckSize / 2f,
            Quaternion.identity,
            groundLayer
        );
        
        Vector3 rayStart = groundCheckCenter + Vector3.up * 0.1f;
        isAboutToLand = Physics.Raycast(
            rayStart,
            Vector3.down,
            groundRaycastDistance,
            groundLayer
        );
        
        if (!wasGrounded && isGrounded)
        {
            HandleLanding();
        }
        
        if (wasGrounded && !isGrounded && !isJumping)
        {
            HandleLeftGround();
        }
    }
    
    private void HandleLanding()
    {
        // Store landing velocity for landing lag calculation
        landingVelocity = Mathf.Abs(rb.linearVelocity.y);
        
        isJumping = false;
        canExtendJump = false;
        
        if (currentState != null)
        {
            airJumpsRemaining = currentState.maxAirJumps;
        }
        
        // Dash refill on landing
        if (currentState != null && 
            currentState.dashRefillMode == Fused_DashRefillMode.OnGroundTouch)
        {
            currentDashCharges = currentState.maxDashCharges;
        }
        
        isFastFalling = false;
        isInWallJumpLock = false;
        wallJumpLockTimer = 0f;
        
        // Reset air control timing
        timeInAir = 0f;
        airControlDelayTimer = 0f;
        airControlRampProgress = 0f;
        
        // Bunny hop check
        if (currentState != null && currentState.bunnyHopEnabled)
        {
            canBunnyHop = true;
            timeSinceLanding = 0f;
        }
        
        // Landing lag check (Fez feature)
        if (currentState != null && currentState.landingLagMode != Fused_LandingLagMode.Disabled)
        {
            if (landingVelocity >= currentState.hardLandingThreshold)
            {
                float lagDuration = currentState.landingLagDuration;
                
                // Reduce lag if jump is buffered
                if (jumpBuffered && currentState.reduceLandingLagOnJumpBuffer)
                {
                    lagDuration *= currentState.landingLagReductionMultiplier;
                }
                
                isInLandingLag = true;
                landingLagTimer = lagDuration;
            }
        }
        
        OnLanded?.Invoke();
        
        if (logStateChanges)
        {
            Debug.Log("[Fused_PlayerController] Landed");
        }
    }
    
    private void HandleLeftGround()
    {
        if (currentState != null && currentState.coyoteTimeEnabled)
        {
            coyoteTimer = currentState.coyoteTimeDuration;
        }
        
        // Start air control delay
        if (currentState != null && currentState.airControlDelay > 0)
        {
            airControlDelayTimer = currentState.airControlDelay;
        }
        
        OnLeftGround?.Invoke();
        
        if (logStateChanges)
        {
            Debug.Log("[Fused_PlayerController] Left ground");
        }
    }
    
    #endregion
    
    // ========================================================================
    // SECTION: SLOPE DETECTION (FEZ FEATURE)
    // ========================================================================
    
    #region SLOPE DETECTION
    
    private void UpdateSlopeDetection()
    {
        if (currentState == null || !currentState.canWalkOnSlopes)
        {
            isOnSlope = false;
            isOnSteepSlope = false;
            return;
        }
        
        if (!isGrounded)
        {
            isOnSlope = false;
            isOnSteepSlope = false;
            return;
        }
        
        Vector3 rayStart = transform.position;
        RaycastHit hit;
        
        if (Physics.Raycast(rayStart, Vector3.down, out hit, playerCollider.bounds.extents.y + 0.5f, groundLayer))
        {
            slopeNormal = hit.normal;
            currentSlopeAngle = Vector3.Angle(Vector3.up, slopeNormal);
            
            isOnSlope = currentSlopeAngle > 0.1f && currentSlopeAngle < 89f;
            isOnSteepSlope = currentSlopeAngle > currentState.maxSlopeAngle;
        }
        else
        {
            isOnSlope = false;
            isOnSteepSlope = false;
            currentSlopeAngle = 0f;
            slopeNormal = Vector3.up;
        }
    }
    
    private void ApplySlopePhysics()
    {
        if (currentState == null) return;
        if (!isOnSteepSlope) return;
        
        // Apply slope slide based on behavior mode
        switch (currentState.steepSlopeBehavior)
        {
            case Fused_SteepSlopeBehavior.ForcedSlide:
            case Fused_SteepSlopeBehavior.SlideWithControl:
            case Fused_SteepSlopeBehavior.SlideReducedControl:
                Vector3 slideDir = Vector3.Cross(Vector3.Cross(Vector3.up, slopeNormal), slopeNormal);
                slideDir.Normalize();
                
                Vector3 vel = rb.linearVelocity;
                float slideSpeed = currentState.steepSlopeSlideSpeed * Mathf.Sin(currentSlopeAngle * Mathf.Deg2Rad);
                vel += slideDir * slideSpeed * Time.fixedDeltaTime;
                rb.linearVelocity = vel;
                break;
        }
    }
    
    #endregion
    
    // ========================================================================
    // SECTION: WALL DETECTION
    // ========================================================================
    
    #region WALL DETECTION
    
    private void UpdateWallDetection()
    {
        if (wallDetectionDelayTimer > 0f)
        {
            wallDetectionDelayTimer -= Time.deltaTime;
            isTouchingWall = false;
            wallDirection = 0;
            return;
        }
        
        Vector3 checkDir = GetWallCheckDirection();
        Vector3 checkSize = GetWallCheckSize();
        Vector3 checkCenter = transform.position;
        
        bool rightWall = Physics.CheckBox(
            checkCenter + checkDir * wallCheckDistance,
            checkSize / 2f,
            Quaternion.identity,
            wallLayer
        );
        
        bool leftWall = Physics.CheckBox(
            checkCenter - checkDir * wallCheckDistance,
            checkSize / 2f,
            Quaternion.identity,
            wallLayer
        );
        
        isTouchingWall = rightWall || leftWall;
        wallDirection = rightWall ? 1 : (leftWall ? -1 : 0);
        
        if (isTouchingWall && currentState != null && currentState.restoreAirJumpsOnWall)
        {
            airJumpsRemaining = currentState.maxAirJumps;
        }
        
        if (use4DirectionalWallCheck)
        {
            wallCheckTimer -= Time.deltaTime;
            if (wallCheckTimer <= 0f)
            {
                Update4DirectionalWallCheck(checkCenter);
                wallCheckTimer = wallCheckInterval;
            }
        }
    }
    
    private Vector3 GetWallCheckDirection()
    {
        if (useWorldRotation && worldRotationController != null)
        {
            return worldRotationController.GetCurrentRight();
        }
        return Vector3.right;
    }
    
    private Vector3 GetWallCheckSize()
    {
        if (useWorldRotation && worldRotationController != null)
        {
            int faceIndex = worldRotationController.GetCurrentFaceIndex();
            if (faceIndex % 2 == 1)
            {
                return new Vector3(wallCheckSize.z, wallCheckSize.y, wallCheckSize.x);
            }
        }
        return wallCheckSize;
    }
    
    private void Update4DirectionalWallCheck(Vector3 center)
    {
        wallStates[0] = Physics.CheckBox(center + Vector3.right * wallCheckDistance,
            new Vector3(0.1f, wallCheckSize.y, wallCheckSize.z) / 2f, Quaternion.identity, wallLayer);
        wallStates[1] = Physics.CheckBox(center + Vector3.left * wallCheckDistance,
            new Vector3(0.1f, wallCheckSize.y, wallCheckSize.z) / 2f, Quaternion.identity, wallLayer);
        wallStates[2] = Physics.CheckBox(center + Vector3.forward * wallCheckDistance,
            new Vector3(wallCheckSize.x, wallCheckSize.y, 0.1f) / 2f, Quaternion.identity, wallLayer);
        wallStates[3] = Physics.CheckBox(center + Vector3.back * wallCheckDistance,
            new Vector3(wallCheckSize.x, wallCheckSize.y, 0.1f) / 2f, Quaternion.identity, wallLayer);
    }
    
    #endregion
    
    // ========================================================================
    // SECTION: TIMER UPDATES
    // ========================================================================
    
    #region TIMER UPDATES
    
    private void UpdateJumpTimers()
    {
        if (currentState == null) return;
        
        // Coyote time
        if (!isGrounded && coyoteTimer > 0f)
        {
            coyoteTimer -= Time.deltaTime;
        }
        else if (isGrounded)
        {
            coyoteTimer = currentState.coyoteTimeDuration;
        }
        
        // Jump buffer
        if (jumpBuffered && jumpBufferTimer > 0f)
        {
            jumpBufferTimer -= Time.deltaTime;
            if (jumpBufferTimer <= 0f)
            {
                jumpBuffered = false;
            }
        }
        
        // Jump hold timer
        if (canExtendJump && jumpHeld)
        {
            jumpHoldTimer += Time.deltaTime;
            if (jumpHoldTimer >= currentState.maxJumpHoldTime)
            {
                canExtendJump = false;
            }
        }
        
        // Wall jump lock timer
        if (isInWallJumpLock)
        {
            wallJumpLockTimer -= Time.deltaTime;
            if (wallJumpLockTimer <= 0f)
            {
                isInWallJumpLock = false;
            }
        }
        
        // Time since wall jump (for super dash)
        if (timeSinceWallJump < 1f)
        {
            timeSinceWallJump += Time.deltaTime;
        }
    }
    
    private void UpdateDashTimers()
    {
        if (currentState == null) return;
        
        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
        }
        
        // Time-based refill
        if (currentState.dashRefillMode == Fused_DashRefillMode.OnGroundDelay ||
            currentState.dashRefillMode == Fused_DashRefillMode.OverTime)
        {
            bool shouldRefill = isGrounded || 
                currentState.dashRefillMode == Fused_DashRefillMode.OverTime;
            
            if (shouldRefill && currentDashCharges < currentState.maxDashCharges)
            {
                if (dashRefillDelayTimer > 0f)
                {
                    dashRefillDelayTimer -= Time.deltaTime;
                }
                else
                {
                    dashRefillTimer += Time.deltaTime;
                    if (dashRefillTimer >= currentState.dashRefillTime)
                    {
                        currentDashCharges++;
                        dashRefillTimer = 0f;
                    }
                }
            }
        }
    }
    
    private void UpdateWallTimers()
    {
        if (currentState == null) return;
        
        if (isTouchingWall && !isGrounded)
        {
            if (wallSlideDelayTimer < currentState.wallSlideStartDelay)
            {
                wallSlideDelayTimer += Time.deltaTime;
            }
        }
        else
        {
            wallSlideDelayTimer = 0f;
        }
    }
    
    private void UpdateStamina()
    {
        if (currentState == null) return;
        
        if (currentState.staminaMode == Fused_StaminaMode.Disabled)
        {
            currentStamina = currentState.maxStamina;
            return;
        }
        
        if ((isWallSliding || isWallClimbing || isWallClinging) && !isGrounded)
        {
            float drainRate = currentState.staminaDrainRate;
            
            if (isWallClimbing)
            {
                drainRate += currentState.climbExtraDrain;
            }
            
            currentStamina -= drainRate * Time.deltaTime;
            
            if (currentStamina <= 0f)
            {
                currentStamina = 0f;
                HandleStaminaDepleted();
            }
        }
        else if (isGrounded)
        {
            if (staminaRefillDelayTimer > 0f)
            {
                staminaRefillDelayTimer -= Time.deltaTime;
            }
            else
            {
                currentStamina += currentState.staminaRefillRate * Time.deltaTime;
                currentStamina = Mathf.Min(currentStamina, currentState.maxStamina);
            }
        }
        else
        {
            staminaRefillDelayTimer = currentState.staminaRefillDelay;
        }
    }
    
    private void HandleStaminaDepleted()
    {
        isWallSliding = false;
        isWallClimbing = false;
        isWallClinging = false;
        canWallJump = false;
        
        OnStaminaDepleted?.Invoke();
        OnWallRelease?.Invoke();
    }
    
    private void UpdateStateSwitchCooldown()
    {
        if (stateSwitchOnCooldown)
        {
            stateSwitchCooldownTimer -= Time.deltaTime;
            if (stateSwitchCooldownTimer <= 0f)
            {
                stateSwitchOnCooldown = false;
            }
        }
    }
    
    private void UpdateLandingLag()
    {
        if (isInLandingLag)
        {
            landingLagTimer -= Time.deltaTime;
            if (landingLagTimer <= 0f)
            {
                isInLandingLag = false;
            }
        }
    }
    
    private void UpdateBunnyHop()
    {
        if (currentState == null || !currentState.bunnyHopEnabled) return;
        
        if (isGrounded && canBunnyHop)
        {
            timeSinceLanding += Time.deltaTime;
            if (timeSinceLanding > currentState.bunnyHopTimingWindow)
            {
                canBunnyHop = false;
            }
        }
        
        // Decay bunny hop bonus
        if (bunnyHopBonus > 0)
        {
            bunnyHopBonus -= currentState.bunnyHopDecayRate * Time.deltaTime;
            bunnyHopBonus = Mathf.Max(0, bunnyHopBonus);
        }
    }
    
    private void UpdateAirControlTiming()
    {
        if (currentState == null) return;
        
        if (!isGrounded)
        {
            timeInAir += Time.deltaTime;
            
            if (airControlDelayTimer > 0)
            {
                airControlDelayTimer -= Time.deltaTime;
            }
            
            if (airControlDelayTimer <= 0 && currentState.airControlRampUpTime > 0)
            {
                airControlRampProgress += Time.deltaTime / currentState.airControlRampUpTime;
                airControlRampProgress = Mathf.Clamp01(airControlRampProgress);
            }
            else if (currentState.airControlRampUpTime <= 0)
            {
                airControlRampProgress = 1f;
            }
        }
    }
    
    #endregion

    // ========================================================================
    // Continued in next section...
    // ========================================================================

    // ========================================================================
    
    #region HORIZONTAL MOVEMENT
    
    private void ApplyHorizontalMovement()
    {
        if (currentState == null) return;
        
        // Wall slide/climb blocks horizontal movement
        if (isWallSliding || isWallClimbing || isWallClinging) return;
        
        // Landing lag restrictions
        if (isInLandingLag)
        {
            switch (currentState.landingLagMode)
            {
                case Fused_LandingLagMode.FreezeAll:
                    return;
                case Fused_LandingLagMode.PreventActionsOnly:
                    // Allow full movement
                    break;
                case Fused_LandingLagMode.ReducedMovement:
                    // Handle below with multiplier
                    break;
            }
        }
        
        Vector3 movementRight = GetMovementRight();
        
        // Calculate target speed
        float targetSpeed = moveInput.x * currentState.maxMoveSpeed;
        
        // Apply bunny hop bonus
        if (currentState.bunnyHopEnabled && bunnyHopBonus > 0)
        {
            float bonusMultiplier = 1f + bunnyHopBonus;
            bonusMultiplier = Mathf.Min(bonusMultiplier, currentState.bunnyHopMaxSpeed);
            targetSpeed *= bonusMultiplier;
        }
        
        // Apply landing lag reduction
        if (isInLandingLag && currentState.landingLagMode == Fused_LandingLagMode.ReducedMovement)
        {
            targetSpeed *= currentState.landingLagMovementMultiplier;
        }
        
        // Apply wall jump lock
        if (isInWallJumpLock)
        {
            targetSpeed *= currentState.wallJumpLockControlMultiplier;
        }
        
        // Apply air control
        if (!isGrounded && currentState.airControlEnabled)
        {
            float airMod = currentState.airControlMultiplier;
            
            // Apply air control delay/ramp
            if (airControlDelayTimer > 0)
            {
                airMod = 0f;
            }
            else if (currentState.airControlRampUpTime > 0)
            {
                airMod *= airControlRampProgress;
            }
            
            // Apex bonus
            if (isAtApex && currentState.apexHangEnabled)
            {
                airMod *= currentState.apexAirControlBonus;
            }
            
            targetSpeed *= airMod;
        }
        
        // Steep slope control reduction
        if (isOnSteepSlope && currentState.steepSlopeBehavior == Fused_SteepSlopeBehavior.SlideReducedControl)
        {
            targetSpeed *= currentState.steepSlopeControlMultiplier;
        }
        
        // Calculate new velocity based on acceleration mode
        Vector3 newVelocity;
        float currentHorizontalVel = Vector3.Dot(rb.linearVelocity, movementRight);
        
        // === CELESTE-STYLE INSTANT ACCELERATION ===
        if (currentState.accelerationMode == Fused_AccelerationMode.Instant)
        {
            // INSTANT - Target speed reached immediately
            newVelocity = movementRight * targetSpeed;
            newVelocity.y = rb.linearVelocity.y;
            
            // Apply starting speed boost on first frame (Fez feature)
            if (Mathf.Abs(currentHorizontalVel) < 0.1f && Mathf.Abs(targetSpeed) > 0.1f)
            {
                newVelocity = movementRight * targetSpeed * currentState.startingSpeedBoost;
                newVelocity.y = rb.linearVelocity.y;
            }
        }
        else
        {
            // Non-instant acceleration
            newVelocity = CalculateAcceleratedVelocity(movementRight, targetSpeed, currentHorizontalVel);
        }
        
        // Handle deceleration when no input
        if (Mathf.Abs(moveInput.x) < 0.1f)
        {
            newVelocity = CalculateDeceleratedVelocity(movementRight);
        }
        
        rb.linearVelocity = newVelocity;
        UpdateFacing();
    }
    
    private Vector3 CalculateAcceleratedVelocity(Vector3 movementRight, float targetSpeed, float currentSpeed)
    {
        if (currentState == null) return rb.linearVelocity;
        
        float newSpeed;
        
        // Check for turn-around
        bool isTurning = (currentSpeed > 0.1f && targetSpeed < -0.1f) || 
                        (currentSpeed < -0.1f && targetSpeed > 0.1f);
        
        if (isTurning && currentState.instantTurning)
        {
            newSpeed = targetSpeed;
            accelerationTimer = 0f;
        }
        else if (isTurning)
        {
            if (currentState.decelerateOnTurn)
            {
                float decelStep = currentState.decelerationRate * currentState.turnDecelerationMultiplier * Time.fixedDeltaTime;
                newSpeed = Mathf.MoveTowards(currentSpeed, 0f, decelStep);
            }
            else
            {
                newSpeed = currentSpeed * currentState.turnSpeedMultiplier;
            }
        }
        else
        {
            switch (currentState.accelerationMode)
            {
                case Fused_AccelerationMode.Linear:
                    float accelStep = currentState.accelerationRate * Time.fixedDeltaTime;
                    newSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accelStep);
                    break;
                    
                case Fused_AccelerationMode.EaseIn:
                    accelerationTimer += Time.fixedDeltaTime;
                    float t = accelerationTimer / currentState.accelerationTime;
                    t = Mathf.Clamp01(t);
                    t = t * t; // Ease in
                    newSpeed = Mathf.Lerp(0, targetSpeed, t);
                    break;
                    
                case Fused_AccelerationMode.Custom:
                    accelerationTimer += Time.fixedDeltaTime;
                    float curveT = accelerationTimer / currentState.accelerationTime;
                    curveT = Mathf.Clamp01(curveT);
                    float curveValue = currentState.accelerationCurve.Evaluate(curveT);
                    newSpeed = targetSpeed * curveValue;
                    break;
                    
                default:
                    newSpeed = targetSpeed;
                    break;
            }
        }
        
        Vector3 result = movementRight * newSpeed;
        result.y = rb.linearVelocity.y;
        return result;
    }
    
    private Vector3 CalculateDeceleratedVelocity(Vector3 movementRight)
    {
        if (currentState == null) return rb.linearVelocity;
        
        accelerationTimer = 0f;
        float currentSpeed = Vector3.Dot(rb.linearVelocity, movementRight);
        
        if (Mathf.Abs(currentSpeed) < currentState.minimumSpeedThreshold)
        {
            Vector3 result = rb.linearVelocity;
            result -= movementRight * Vector3.Dot(result, movementRight);
            return result;
        }
        
        float newSpeed;
        
        // Apply air deceleration if in air
        if (!isGrounded && currentState.airDeceleration > 0)
        {
            float airDecelStep = currentState.airDeceleration * Time.fixedDeltaTime;
            newSpeed = Mathf.MoveTowards(currentSpeed, 0f, airDecelStep);
        }
        else
        {
            switch (currentState.decelerationMode)
            {
                case Fused_DecelerationMode.Instant:
                    // CELESTE-STYLE: Instant stop
                    newSpeed = 0f;
                    break;
                    
                case Fused_DecelerationMode.Slide:
                    newSpeed = currentSpeed * 0.98f;
                    break;
                    
                case Fused_DecelerationMode.Friction:
                    float frictionStep = currentState.decelerationRate * Time.fixedDeltaTime;
                    newSpeed = Mathf.MoveTowards(currentSpeed, 0f, frictionStep);
                    break;
                    
                case Fused_DecelerationMode.Custom:
                    decelerationTimer += Time.fixedDeltaTime;
                    float t = decelerationTimer / currentState.decelerationTime;
                    t = Mathf.Clamp01(t);
                    float curveValue = currentState.decelerationCurve.Evaluate(t);
                    newSpeed = currentSpeed * curveValue;
                    break;
                    
                default:
                    newSpeed = 0f;
                    break;
            }
        }
        
        Vector3 result2 = movementRight * newSpeed;
        result2.y = rb.linearVelocity.y;
        return result2;
    }
    
    private Vector3 GetMovementRight()
    {
        if (useWorldRotation && worldRotationController != null)
        {
            return worldRotationController.GetCurrentRight();
        }
        return Vector3.right;
    }
    
    private void UpdateFacing()
    {
        if (Mathf.Abs(moveInput.x) < 0.1f) return;
        
        bool shouldFaceRight = moveInput.x > 0;
        
        if (shouldFaceRight && !isFacingRight)
        {
            StartFlip(true);
        }
        else if (!shouldFaceRight && isFacingRight)
        {
            StartFlip(false);
        }
    }
    
    private void StartFlip(bool flipToRight)
    {
        if (isFlipping) return;
        StartCoroutine(FlipCoroutine(flipToRight));
    }
    
    private IEnumerator FlipCoroutine(bool flipToRight)
    {
        isFlipping = true;
        
        float startAngle = facingAngle;
        float targetAngle = flipToRight ? 0f : 180f;
        float flipDuration = 0.08f; // Fast flip for snappy feel
        float elapsed = 0f;
        
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
    
    // ========================================================================
    // SECTION: JUMPING
    // ========================================================================
    
    #region JUMPING
    
    private void ProcessJumpInput()
    {
        if (currentState == null || !currentState.canJump) return;
        
        // Landing lag restrictions
        if (isInLandingLag && currentState.landingLagMode != Fused_LandingLagMode.Disabled)
        {
            if (currentState.landingLagMode == Fused_LandingLagMode.FreezeAll ||
                currentState.landingLagMode == Fused_LandingLagMode.PreventActionsOnly)
            {
                return;
            }
        }
        
        // Steep slope jump restriction
        if (isOnSteepSlope && !currentState.canJumpOnSteepSlope)
        {
            return;
        }
        
        if (!jumpBuffered) return;
        
        // Priority 1: Wall Jump
        if (canWallJump && currentState.wallJumpEnabled && !isGrounded)
        {
            ExecuteWallJump();
            return;
        }
        
        // Priority 2: Ground Jump (includes coyote time)
        bool canGroundJump = isGrounded || coyoteTimer > 0f;
        if (canGroundJump && !isJumping)
        {
            ExecuteGroundJump();
            return;
        }
        
        // Priority 3: Air Jump
        if (currentState.airJumpEnabled && airJumpsRemaining > 0 && !isGrounded)
        {
            ExecuteAirJump();
            return;
        }
    }
    
    private void ExecuteGroundJump()
    {
        if (currentState == null) return;
        
        // Check for bunny hop
        bool isBunnyHop = false;
        if (currentState.bunnyHopEnabled && canBunnyHop && timeSinceLanding <= currentState.bunnyHopTimingWindow)
        {
            isBunnyHop = true;
            bunnyHopBonus += currentState.bunnyHopSpeedBonus;
            bunnyHopBonus = Mathf.Min(bunnyHopBonus, currentState.bunnyHopMaxSpeed - 1f);
            OnBunnyHop?.Invoke();
        }
        canBunnyHop = false;
        
        // Reset vertical velocity for consistent jump height
        Vector3 vel = rb.linearVelocity;
        vel.y = 0f;
        rb.linearVelocity = vel;
        
        // Calculate jump force
        float jumpForceToUse = currentState.jumpForce;
        
        // Moving jump bonus (Fez feature)
        if (currentState.movingJumpBonus > 1f)
        {
            float horizontalSpeed = new Vector2(rb.linearVelocity.x, rb.linearVelocity.z).magnitude;
            if (horizontalSpeed >= currentState.movingJumpSpeedThreshold)
            {
                jumpForceToUse *= currentState.movingJumpBonus;
            }
        }
        
        // Apply jump
        if (currentState.jumpAsImpulse)
        {
            rb.AddForce(Vector3.up * jumpForceToUse, ForceMode.Impulse);
        }
        else
        {
            vel.y = jumpForceToUse;
            rb.linearVelocity = vel;
        }
        
        // Horizontal momentum boost (Fez feature)
        if (currentState.jumpMomentumBoost)
        {
            Vector3 currentVel = rb.linearVelocity;
            currentVel.x *= currentState.jumpMomentumMultiplier;
            currentVel.z *= currentState.jumpMomentumMultiplier;
            rb.linearVelocity = currentVel;
        }
        
        // Set jump state
        isJumping = true;
        jumpBuffered = false;
        coyoteTimer = 0f;
        
        if (currentState.variableJumpHeight)
        {
            canExtendJump = true;
            jumpHoldTimer = 0f;
        }
        
        isFastFalling = false;
        
        OnJump?.Invoke(0); // 0 = ground jump
        
        if (logStateChanges)
        {
            Debug.Log($"[Fused_PlayerController] Ground Jump{(isBunnyHop ? " (Bunny Hop!)" : "")}");
        }
    }
    
    private void ExecuteAirJump()
    {
        if (currentState == null) return;
        
        airJumpsRemaining--;
        
        Vector3 vel = rb.linearVelocity;
        vel.y = 0f;
        rb.linearVelocity = vel;
        
        float airJumpForce = currentState.jumpForce * currentState.airJumpForceMultiplier;
        
        if (currentState.jumpAsImpulse)
        {
            rb.AddForce(Vector3.up * airJumpForce, ForceMode.Impulse);
        }
        else
        {
            vel.y = airJumpForce;
            rb.linearVelocity = vel;
        }
        
        jumpBuffered = false;
        isFastFalling = false;
        
        if (currentState.variableJumpHeight)
        {
            canExtendJump = true;
            jumpHoldTimer = 0f;
        }
        
        OnJump?.Invoke(1); // 1 = air jump
        
        if (logStateChanges)
        {
            Debug.Log("[Fused_PlayerController] Air Jump");
        }
    }
    
    private void ExecuteWallJump()
    {
        // Safety check for state data.
        if (currentState == null) return;
        
        // Clear wall interaction states - we're leaving the wall.
        isWallSliding = false;
        isWallClimbing = false;
        isWallClinging = false;
        
        // Get the wall jump direction from the input processing system.
        // This handles neutral vs directional wall jumps based on settings.
        Vector2 wallJumpDir2D = GetWallJumpDirection(wallDirection);
        
        // Calculate vertical force - use dedicated wall jump force if set, otherwise use normal jump force.
        float baseVerticalForce = currentState.wallJumpVerticalForce > 0 
            ? currentState.wallJumpVerticalForce 
            : currentState.jumpForce;
        
        // Calculate horizontal force.
        float horizontalForce = currentState.wallJumpHorizontalForce;
        
        // Get the movement right direction (accounts for world rotation).
        Vector3 movementRight = GetMovementRight();
        
        // Build the velocity vector based on wall jump direction.
        Vector3 vel = Vector3.zero;
        
        // Apply vertical component - direction's Y affects vertical force.
        // For neutral jumps, this will be higher due to the multiplier.
        vel.y = baseVerticalForce * Mathf.Max(wallJumpDir2D.y, 0.5f);
        
        // Apply horizontal component - direction's X affects push direction and strength.
        // The X component is already signed correctly (-1 for left wall jump, +1 for right).
        vel += movementRight * wallJumpDir2D.x * horizontalForce;
        
        // Set the final velocity.
        rb.linearVelocity = vel;
        
        // Set wall jump lock to prevent immediately returning to wall.
        if (currentState.wallJumpControlLockEnabled)
        {
            isInWallJumpLock = true;
            wallJumpLockTimer = currentState.wallJumpControlLockDuration;
            lastWallJumpDirection = -wallDirection;
        }
        
        // Reset air jumps on wall jump
        if (currentState.resetAirJumpsOnWallJump)
        {
            airJumpsRemaining = currentState.maxAirJumps;
        }
        
        jumpBuffered = false;
        canWallJump = false;
        isFastFalling = false;
        timeSinceWallJump = 0f;
        
        if (currentState.variableJumpHeight)
        {
            canExtendJump = true;
            jumpHoldTimer = 0f;
        }
        
        OnJump?.Invoke(2); // 2 = wall jump
        OnWallRelease?.Invoke();
        
        if (logStateChanges)
        {
            Debug.Log("[Fused_PlayerController] Wall Jump");
        }
    }
    
    #endregion
    
    // ========================================================================
    // SECTION: GRAVITY AND FALLING
    // ========================================================================
    
    #region GRAVITY
    
    private void ApplyGravity()
    {
        if (currentState == null) return;
        if (isGrounded) return;
        
        float gravityMultiplier = CalculateGravityMultiplier();
        currentGravityMultiplier = gravityMultiplier;
        
        float gravityForce = currentState.baseGravity * gravityMultiplier;
        rb.linearVelocity += Vector3.down * gravityForce * Time.fixedDeltaTime;
    }
    
    private float CalculateGravityMultiplier()
    {
        if (currentState == null) return 1f;
        
        float verticalVel = rb.linearVelocity.y;
        
        // Check apex state
        isAtApex = currentState.apexHangEnabled && 
                   Mathf.Abs(verticalVel) < currentState.apexVelocityThreshold &&
                   isJumping;
        
        // Fast fall check
        if (currentState.fastFallEnabled && isFastFalling)
        {
            return currentState.fastFallMultiplier;
        }
        
        // Variable gravity mode
        if (currentState.gravityMode == Fused_GravityMode.Variable)
        {
            // Apex hang
            if (isAtApex)
            {
                return currentState.apexGravityMultiplier;
            }
            
            // Rising while holding jump
            if (verticalVel > 0 && jumpHeld && canExtendJump)
            {
                return currentState.risingGravityMultiplier;
            }
            
            // Rising but jump released (cut jump)
            if (verticalVel > 0 && !jumpHeld)
            {
                canExtendJump = false;
                return currentState.jumpCutGravityMultiplier;
            }
            
            // Falling
            if (verticalVel < 0)
            {
                return currentState.fallingGravityMultiplier;
            }
        }
        
        return 1f;
    }
    
    private void ClampFallSpeed()
    {
        if (currentState == null) return;
        
        Vector3 vel = rb.linearVelocity;
        
        // Apply max fall speed
        if (vel.y < -currentState.maxFallSpeed)
        {
            vel.y = -currentState.maxFallSpeed;
            rb.linearVelocity = vel;
        }
    }
    
    #endregion
    
    // ========================================================================
    // SECTION: WALL MECHANICS
    // ========================================================================
    
    #region WALL MECHANICS
    
    private void UpdateWallMechanics()
    {
        if (currentState == null) return;
        
        bool wasWallSliding = isWallSliding;
        bool wasWallClinging = isWallClinging;
        
        // Reset states
        isWallSliding = false;
        isWallClimbing = false;
        canWallJump = false;
        
        // Check if we should be on a wall
        if (!isTouchingWall || isGrounded)
        {
            if (wasWallSliding || wasWallClinging)
            {
                OnWallRelease?.Invoke();
            }
            isWallClinging = false;
            wallClingTimer = 0f;
            return;
        }
        
        // Check stamina - if depleted, cannot use wall mechanics.
        if (currentState.staminaMode != Fused_StaminaMode.Disabled && currentStamina <= 0)
        {
            return;
        }
        
        // Wall slide conditions - uses input threshold for better controller support.
        // The threshold prevents accidental wall slides from slight stick drift.
        bool shouldWallSlide = false;
        
        switch (currentState.wallSlideMode)
        {
            case Fused_WallSlideMode.Automatic:
                // Automatic: Wall slide whenever touching wall and falling (or rising if enabled).
                shouldWallSlide = rb.linearVelocity.y <= 0 || currentState.canWallSlideWhileRising;
                break;
                
            case Fused_WallSlideMode.HoldToward:
                // HoldToward: Must press toward wall to slide.
                // Uses configurable threshold for controller support.
                shouldWallSlide = IsPressingTowardWall(wallDirection) && 
                    (rb.linearVelocity.y <= 0 || currentState.canWallSlideWhileRising);
                break;
                
            case Fused_WallSlideMode.GrabButton:
                // GrabButton: Must hold grab button to slide.
                shouldWallSlide = grabHeld && 
                    (rb.linearVelocity.y <= 0 || currentState.canWallSlideWhileRising);
                break;
        }
        
        // Wall cling mode (Fez feature) - allows sticking to walls before sliding.
        if (currentState.wallClingMode != Fused_WallClingMode.Disabled)
        {
            switch (currentState.wallClingMode)
            {
                case Fused_WallClingMode.ClingThenSlide:
                    if (wallClingTimer < currentState.maxWallClingDuration)
                    {
                        isWallClinging = true;
                        wallClingTimer += Time.deltaTime;
                    }
                    else
                    {
                        shouldWallSlide = true;
                    }
                    break;
                    
                case Fused_WallClingMode.HoldToStick:
                    // HoldToStick: Cling while pressing toward wall (uses threshold).
                    isWallClinging = IsPressingTowardWall(wallDirection);
                    if (!isWallClinging)
                    {
                        shouldWallSlide = true;
                    }
                    break;
                    
                case Fused_WallClingMode.InputToggle:
                    if (grabHeld)
                    {
                        isWallClinging = !isWallClinging;
                    }
                    if (!isWallClinging)
                    {
                        shouldWallSlide = true;
                    }
                    break;
            }
        }
        
        // Apply wall slide after delay
        if (shouldWallSlide && wallSlideDelayTimer >= currentState.wallSlideStartDelay)
        {
            isWallSliding = true;
        }
        
        // Wall climb check
        if (currentState.wallClimbEnabled && grabHeld && isTouchingWall)
        {
            if (moveInput.y > 0.1f)
            {
                isWallClimbing = true;
                isWallSliding = false;
            }
        }
        
        // Wall jump availability
        if (isTouchingWall && (isWallSliding || isWallClinging || isWallClimbing))
        {
            canWallJump = currentState.wallJumpEnabled;
        }
        
        // Fire events
        if (!wasWallSliding && !wasWallClinging && (isWallSliding || isWallClinging))
        {
            OnWallGrab?.Invoke();
        }
    }
    
    private void ApplyWallPhysics()
    {
        if (currentState == null) return;
        
        // Wall cling physics
        if (isWallClinging)
        {
            Vector3 vel = rb.linearVelocity;
            vel.y = Mathf.Max(vel.y, -currentState.wallClingGravity);
            rb.linearVelocity = vel;
            return;
        }
        
        // Wall slide physics
        if (isWallSliding)
        {
            Vector3 vel = rb.linearVelocity;
            vel.y = Mathf.Max(vel.y, currentState.wallSlideSpeed);
            rb.linearVelocity = vel;
            return;
        }
        
        // Wall climb physics
        if (isWallClimbing)
        {
            Vector3 vel = rb.linearVelocity;
            vel.y = currentState.wallClimbSpeed * moveInput.y;
            rb.linearVelocity = vel;
        }
    }
    
    #endregion
    
    // ========================================================================
    // SECTION: DASH SYSTEM
    // ========================================================================
    
    #region DASH
    
    private void ProcessDashInput()
    {
        if (currentState == null) return;
        if (!currentState.dashEnabled) return;
        
        // Landing lag restrictions
        if (isInLandingLag && currentState.landingLagMode != Fused_LandingLagMode.Disabled)
        {
            if (currentState.landingLagMode == Fused_LandingLagMode.FreezeAll ||
                currentState.landingLagMode == Fused_LandingLagMode.PreventActionsOnly)
            {
                return;
            }
        }
        
        if (!dashPressedThisFrame) return;
        if (isDashing) return;
        if (dashCooldownTimer > 0f) return;
        if (currentDashCharges <= 0) return;
        
        // Air dash check
        if (!isGrounded && !currentState.airDashEnabled) return;
        
        ExecuteDash();
    }
    
    private void ExecuteDash()
    {
        if (currentState == null) return;
        
        currentDashCharges--;
        
        // Calculate dash direction
        dashDirection = CalculateDashDirection();
        
        // Check for super dash
        float dashSpeedToUse = currentState.dashSpeed;
        if (currentState.superDashEnabled && timeSinceWallJump < currentState.superDashWindow)
        {
            dashSpeedToUse *= currentState.superDashSpeedMultiplier;
        }
        
        // Start dash coroutine
        if (activeDashCoroutine != null)
        {
            StopCoroutine(activeDashCoroutine);
        }
        activeDashCoroutine = StartCoroutine(DashCoroutine(dashSpeedToUse));
        
        OnDashStarted?.Invoke(dashDirection);
        
        if (logStateChanges)
        {
            Debug.Log($"[Fused_PlayerController] Dash: {dashDirection}");
        }
    }
    
    /// <summary>
    /// Calculates the dash direction based on input and settings.
    /// Uses the input processing system's direction snapping for precise control.
    /// </summary>
    /// <returns>The normalized dash direction in world space.</returns>
    private Vector3 CalculateDashDirection()
    {
        // Get the movement right direction (accounts for world rotation).
        Vector3 right = GetMovementRight();
        Vector3 dir = Vector3.zero;
        
        // Get the 2D dash direction from the input processing system.
        // This uses direction snapping for precise 4-way/8-way dashing.
        Vector2 dashDir2D = GetDashDirection();
        
        // Handle different dash direction modes.
        switch (currentState.dashDirectionMode)
        {
            case Fused_DashDirectionMode.FacingDirection:
                // Always dash in the direction the player is facing.
                dir = isFacingRight ? right : -right;
                break;
                
            case Fused_DashDirectionMode.InputCardinal:
                // 4-way dash: Use snapped direction (cardinals only).
                // The GetDashDirection method handles snapping based on settings.
                if (dashDir2D.magnitude > 0.1f)
                {
                    // Snap to cardinal direction (no diagonals).
                    if (Mathf.Abs(dashDir2D.x) > Mathf.Abs(dashDir2D.y))
                    {
                        // Horizontal dash.
                        dir = dashDir2D.x > 0 ? right : -right;
                    }
                    else
                    {
                        // Vertical dash.
                        dir = dashDir2D.y > 0 ? Vector3.up : Vector3.down;
                    }
                }
                else
                {
                    // No input: dash in facing direction.
                    dir = isFacingRight ? right : -right;
                }
                break;
                
            case Fused_DashDirectionMode.InputEightWay:
                // 8-way dash: Use snapped direction (cardinals + diagonals).
                if (dashDir2D.magnitude > 0.1f)
                {
                    // Convert 2D snapped direction to 3D world direction.
                    // X maps to the movement right axis, Y maps to world up.
                    dir = right * dashDir2D.x + Vector3.up * dashDir2D.y;
                    dir.Normalize();
                }
                else
                {
                    // No input: dash in facing direction.
                    dir = isFacingRight ? right : -right;
                }
                break;
                
            case Fused_DashDirectionMode.HorizontalOnly:
                // Horizontal only: Dash left or right based on input, no vertical.
                if (Mathf.Abs(dashDir2D.x) > 0.1f)
                {
                    dir = dashDir2D.x > 0 ? right : -right;
                }
                else
                {
                    dir = isFacingRight ? right : -right;
                }
                break;
        }
        
        return dir.normalized;
    }
    
    private IEnumerator DashCoroutine(float speed)
    {
        isDashing = true;
        
        float elapsed = 0f;
        float duration = currentState.dashIsFixedDistance 
            ? currentState.dashDistance / speed 
            : currentState.dashDuration;
        
        // Invincibility
        if (currentState.dashInvincibility)
        {
            StartCoroutine(InvincibilityCoroutine(currentState.dashInvincibilityDuration));
        }
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            
            // Apply dash velocity
            Vector3 dashVel = dashDirection * speed;
            
            // Apply gravity during dash if enabled
            if (!currentState.dashFreezesGravity)
            {
                dashVel.y = rb.linearVelocity.y - currentState.baseGravity * Time.deltaTime;
            }
            
            rb.linearVelocity = dashVel;
            
            // Wall collision cancel
            if (currentState.dashCancelOnWall && isTouchingWall)
            {
                break;
            }
            
            yield return null;
        }
        
        // End dash
        isDashing = false;
        
        // Apply end velocity
        Vector3 endVel = dashDirection * speed * currentState.dashEndVelocityMultiplier;
        
        // Upward dash boost
        if (currentState.upwardDashBoost && dashDirection.y > 0.5f)
        {
            endVel *= currentState.upwardDashBoostMultiplier;
        }
        
        endVel.y = Mathf.Max(endVel.y, rb.linearVelocity.y);
        rb.linearVelocity = endVel;
        
        // Start cooldown
        dashCooldownTimer = currentState.dashCooldown;
        dashRefillDelayTimer = currentState.dashRefillDelay;
        dashRefillTimer = 0f;
        
        OnDashEnded?.Invoke();
        activeDashCoroutine = null;
    }
    
    private IEnumerator InvincibilityCoroutine(float duration)
    {
        isInvincible = true;
        float elapsed = 0f;
        Color originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            
            if (spriteRenderer != null)
            {
                switch (currentState.invincibilityVisual)
                {
                    case Fused_InvincibilityVisual.Flicker:
                        bool visible = Mathf.Sin(elapsed * currentState.flickerSpeed) > 0;
                        spriteRenderer.enabled = visible;
                        break;
                        
                    case Fused_InvincibilityVisual.Transparent:
                        Color transColor = originalColor;
                        transColor.a = currentState.transparencyAlpha;
                        spriteRenderer.color = transColor;
                        break;
                        
                    case Fused_InvincibilityVisual.ColorShift:
                        spriteRenderer.color = currentState.invincibilityColor;
                        break;
                }
            }
            
            yield return null;
        }
        
        // Reset visuals
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color = currentState.stateColor;
        }
        
        isInvincible = false;
    }
    
    #endregion
    
    // ========================================================================
    // SECTION: INPUT CALLBACKS (New Input System)
    // ========================================================================
    // These methods are called by the Unity Input System when input events occur.
    // They are linked to the PlayerInput component via the Inspector.
    // Raw input is stored and optionally processed immediately based on settings.
    // ========================================================================
    
    #region INPUT CALLBACKS
    
    /// <summary>
    /// Called by the Input System when movement input changes.
    /// Stores the raw input value and optionally processes it immediately.
    /// The actual processing depends on the inputProcessingTime setting.
    /// </summary>
    /// <param name="context">The input callback context containing the input value.</param>
    public void OnMove(InputAction.CallbackContext context)
    {
        // Store the raw, unprocessed input value from the controller/keyboard.
        // This value ranges from -1 to 1 on each axis, with potential magnitude > 1 on diagonals.
        rawMoveInput = context.ReadValue<Vector2>();
        
        // Check if we should process input immediately on the input event.
        // OnInputEvent mode has lower latency but doesn't allow runtime setting changes.
        if (currentState != null)
        {
            if (currentState.inputProcessingTime == Fused_InputProcessingTime.OnInputEvent ||
                currentState.inputProcessingTime == Fused_InputProcessingTime.Both)
            {
                // Process the input immediately through deadzone, normalization, etc.
                moveInput = ProcessRawInput(rawMoveInput);
            }
            else
            {
                // For PerFrame mode, just copy raw input; it will be processed in Update.
                moveInput = rawMoveInput;
            }
        }
        else
        {
            // No state data available, use raw input directly.
            moveInput = rawMoveInput;
        }
    }
    
    /// <summary>
    /// Called by the Input System when the jump button is pressed or released.
    /// Sets the jump held state and triggers jump buffering if enabled.
    /// </summary>
    /// <param name="context">The input callback context.</param>
    public void OnJumpInput(InputAction.CallbackContext context)
    {
        // Check if the button was just pressed this frame.
        if (context.started)
        {
            // Mark that jump was pressed this frame for immediate actions.
            jumpPressedThisFrame = true;
            
            // Mark jump as held for variable jump height (hold = higher jump).
            jumpHeld = true;
            
            // If jump buffering is enabled, store the jump request.
            // This allows jumping even if pressed slightly before landing.
            if (currentState != null && currentState.jumpBufferEnabled)
            {
                jumpBuffered = true;
                jumpBufferTimer = currentState.jumpBufferDuration;
            }
        }
        // Check if the button was just released this frame.
        else if (context.canceled)
        {
            // Mark jump as no longer held.
            jumpHeld = false;
            
            // Disable jump extension (variable height) since button was released.
            canExtendJump = false;
        }
    }
    
    /// <summary>
    /// Called by the Input System when the dash button is pressed.
    /// Only triggers on press, not release.
    /// </summary>
    /// <param name="context">The input callback context.</param>
    public void OnDashInput(InputAction.CallbackContext context)
    {
        // Only respond to button press, not release.
        if (context.started)
        {
            // Mark that dash was pressed this frame for the dash system to consume.
            dashPressedThisFrame = true;
        }
    }
    
    /// <summary>
    /// Called by the Input System when the grab button state changes.
    /// Used for wall grab/climb mechanics.
    /// </summary>
    /// <param name="context">The input callback context.</param>
    public void OnGrabInput(InputAction.CallbackContext context)
    {
        // Read the button state as a boolean (pressed = true, released = false).
        grabHeld = context.ReadValueAsButton();
    }
    
    public void OnFastFallInput(InputAction.CallbackContext context)
    {
        if (currentState == null) return;
        
        if (currentState.fastFallEnabled)
        {
            if (currentState.fastFallRequiresPress)
            {
                if (context.started && !isGrounded && rb.linearVelocity.y <= 0)
                {
                    isFastFalling = true;
                }
            }
            else
            {
                if (context.ReadValueAsButton() && !isGrounded && rb.linearVelocity.y <= 0)
                {
                    isFastFalling = true;
                }
                else if (!context.ReadValueAsButton())
                {
                    isFastFalling = false;
                }
            }
        }
    }
    
    public void OnStateSwitchInput(InputAction.CallbackContext context)
    {
        if (context.started && !stateSwitchOnCooldown)
        {
            SwitchState();
        }
    }
    
    private void SwitchState()
    {
        if (currentState == null || !currentState.canSwitchState) return;
        if (secondaryStateData == null) return;
        
        if (currentStateIndex == 0)
        {
            currentState = secondaryStateData;
            currentStateIndex = 1;
        }
        else
        {
            currentState = primaryStateData;
            currentStateIndex = 0;
        }
        
        ApplyStateData();
        
        stateSwitchOnCooldown = true;
        stateSwitchCooldownTimer = currentState.stateSwitchCooldown;
        
        OnStateChanged?.Invoke(currentStateIndex);
        
        if (logStateChanges)
        {
            Debug.Log($"[Fused_PlayerController] Switched to: {currentState.stateName}");
        }
    }
    
    #endregion
    
    // ========================================================================
    // SECTION: INPUT PROCESSING SYSTEM
    // ========================================================================
    // This section contains all the logic for processing raw controller input
    // into clean, usable movement data. It handles:
    // - Deadzone processing (square, circular, scaled radial)
    // - Input normalization (prevents diagonal movement being faster)
    // - Analog/Digital/Hybrid input modes
    // - Direction snapping for precise dash and wall jump directions
    // - Input smoothing (optional, for reducing jitter)
    // ========================================================================
    
    #region INPUT PROCESSING
    
    /// <summary>
    /// Processes input every frame when using PerFrame or Both processing modes.
    /// This method applies all input processing steps and updates the moveInput variable.
    /// Called from Update() before any movement logic.
    /// </summary>
    private void ProcessInputPerFrame()
    {
        // Skip processing if no state data is available.
        if (currentState == null)
        {
            moveInput = rawMoveInput;
            return;
        }
        
        // Process the raw input through all enabled processing steps.
        Vector2 processedInput = ProcessRawInput(rawMoveInput);
        
        // Apply smoothing if enabled.
        // Smoothing reduces jitter but adds latency - not recommended for precision platformers.
        if (currentState.enableInputSmoothing)
        {
            processedInput = ApplyInputSmoothing(processedInput);
        }
        
        // Store the final processed input for use by movement systems.
        moveInput = processedInput;
    }
    
    /// <summary>
    /// Processes raw input through deadzone, normalization, and mode conversion.
    /// This is the main input processing pipeline.
    /// </summary>
    /// <param name="raw">The raw input vector from the Input System.</param>
    /// <returns>The processed input vector ready for use.</returns>
    private Vector2 ProcessRawInput(Vector2 raw)
    {
        // If no state or custom deadzone disabled, return raw input.
        if (currentState == null || !currentState.useCustomDeadzone)
        {
            return raw;
        }
        
        // Step 1: Apply deadzone processing.
        // This removes small stick movements (drift) and shapes the response.
        Vector2 processed = ApplyDeadzone(raw);
        
        // Step 2: Normalize diagonal input if enabled.
        // This prevents diagonal movement from being faster than cardinal.
        if (currentState.normalizeDiagonalInput)
        {
            processed = NormalizeDiagonalInput(processed);
        }
        
        // Step 3: Apply input mode (Digital/Analog/Hybrid).
        // This determines how stick magnitude affects movement speed.
        processed = ApplyInputMode(processed);
        
        // Step 4: Final magnitude clamping if enabled.
        // Safety net to ensure input never exceeds expected range.
        if (currentState.clampInputMagnitude && processed.magnitude > 1f)
        {
            processed = processed.normalized;
        }
        
        return processed;
    }
    
    /// <summary>
    /// Applies deadzone processing to raw input based on the configured deadzone type.
    /// Different deadzone shapes affect how easily different directions can be input.
    /// </summary>
    /// <param name="raw">The raw input vector.</param>
    /// <returns>The input with deadzone applied.</returns>
    private Vector2 ApplyDeadzone(Vector2 raw)
    {
        // Get deadzone values from current state.
        float innerDead = currentState.innerDeadzone;
        float outerDead = currentState.outerDeadzone;
        
        // Apply the appropriate deadzone algorithm based on settings.
        switch (currentState.deadzoneType)
        {
            case Fused_DeadzoneType.Square:
                // Square deadzone: Apply deadzone independently to each axis.
                // Simple but makes diagonals harder to reach (must exit corner of square).
                return ApplySquareDeadzone(raw, innerDead, outerDead);
                
            case Fused_DeadzoneType.Circular:
                // Circular deadzone: Apply deadzone based on magnitude.
                // Equal difficulty for all directions, but can feel jumpy at edge.
                return ApplyCircularDeadzone(raw, innerDead, outerDead);
                
            case Fused_DeadzoneType.ScaledRadial:
                // Scaled radial: Circular deadzone with rescaled output.
                // Best feel - equal difficulty AND smooth transition from deadzone.
                return ApplyScaledRadialDeadzone(raw, innerDead, outerDead);
                
            default:
                return raw;
        }
    }
    
    /// <summary>
    /// Applies a square deadzone by processing each axis independently.
    /// This is the traditional approach but makes diagonals harder to input.
    /// </summary>
    /// <param name="raw">The raw input vector.</param>
    /// <param name="inner">The inner deadzone threshold.</param>
    /// <param name="outer">The outer deadzone threshold.</param>
    /// <returns>The processed input vector.</returns>
    private Vector2 ApplySquareDeadzone(Vector2 raw, float inner, float outer)
    {
        // Process X axis independently.
        float x = ProcessAxisDeadzone(raw.x, inner, outer);
        
        // Process Y axis independently.
        float y = ProcessAxisDeadzone(raw.y, inner, outer);
        
        return new Vector2(x, y);
    }
    
    /// <summary>
    /// Processes a single axis value through deadzone.
    /// Used by square deadzone processing.
    /// </summary>
    /// <param name="value">The axis value (-1 to 1).</param>
    /// <param name="inner">The inner deadzone threshold.</param>
    /// <param name="outer">The outer deadzone threshold.</param>
    /// <returns>The processed axis value.</returns>
    private float ProcessAxisDeadzone(float value, float inner, float outer)
    {
        // Get the sign and absolute value for processing.
        float sign = Mathf.Sign(value);
        float abs = Mathf.Abs(value);
        
        // If below inner deadzone, return zero.
        if (abs < inner)
        {
            return 0f;
        }
        
        // If above outer deadzone, return full value.
        if (abs > outer)
        {
            return sign;
        }
        
        // Rescale the value to 0-1 range between inner and outer deadzones.
        float rescaled = (abs - inner) / (outer - inner);
        
        return sign * rescaled;
    }
    
    /// <summary>
    /// Applies a circular deadzone based on input magnitude.
    /// Equal difficulty for all directions but no rescaling.
    /// </summary>
    /// <param name="raw">The raw input vector.</param>
    /// <param name="inner">The inner deadzone radius.</param>
    /// <param name="outer">The outer deadzone radius.</param>
    /// <returns>The processed input vector.</returns>
    private Vector2 ApplyCircularDeadzone(Vector2 raw, float inner, float outer)
    {
        // Calculate the magnitude of the input.
        float magnitude = raw.magnitude;
        
        // If below inner deadzone, return zero.
        if (magnitude < inner)
        {
            return Vector2.zero;
        }
        
        // If above outer deadzone, clamp to unit circle.
        if (magnitude > outer)
        {
            return raw.normalized;
        }
        
        // Return original direction, magnitude unchanged.
        // Note: This doesn't rescale, so there's a "jump" at deadzone edge.
        return raw;
    }
    
    /// <summary>
    /// Applies a scaled radial deadzone - the recommended approach.
    /// Circular deadzone with smooth rescaling from 0 at inner edge.
    /// Provides equal difficulty for all directions AND smooth response.
    /// </summary>
    /// <param name="raw">The raw input vector.</param>
    /// <param name="inner">The inner deadzone radius.</param>
    /// <param name="outer">The outer deadzone radius.</param>
    /// <returns>The processed input vector.</returns>
    private Vector2 ApplyScaledRadialDeadzone(Vector2 raw, float inner, float outer)
    {
        // Calculate the magnitude of the input.
        float magnitude = raw.magnitude;
        
        // If below inner deadzone, return zero.
        if (magnitude < inner)
        {
            return Vector2.zero;
        }
        
        // Calculate the direction (normalized input).
        Vector2 direction = raw / magnitude;
        
        // Clamp magnitude to outer deadzone.
        float clampedMagnitude = Mathf.Min(magnitude, outer);
        
        // Rescale magnitude: 0 at inner edge, 1 at outer edge.
        // This provides smooth transition from deadzone instead of sudden jump.
        float rescaledMagnitude = (clampedMagnitude - inner) / (outer - inner);
        
        // Return direction scaled by rescaled magnitude.
        return direction * rescaledMagnitude;
    }
    
    /// <summary>
    /// Normalizes diagonal input to prevent faster diagonal movement.
    /// Raw diagonal input can have magnitude ~1.41 (sqrt of 2).
    /// This clamps it to 1.0 while preserving direction.
    /// </summary>
    /// <param name="input">The input vector to normalize.</param>
    /// <returns>The normalized input vector with magnitude <= 1.</returns>
    private Vector2 NormalizeDiagonalInput(Vector2 input)
    {
        // If magnitude exceeds 1, normalize it.
        // This ensures diagonal movement isn't faster than cardinal.
        if (input.magnitude > 1f)
        {
            return input.normalized;
        }
        
        return input;
    }
    
    /// <summary>
    /// Applies the configured input mode (Digital/Analog/Hybrid).
    /// This determines how stick magnitude affects movement speed.
    /// </summary>
    /// <param name="input">The input vector after deadzone processing.</param>
    /// <returns>The input with mode applied.</returns>
    private Vector2 ApplyInputMode(Vector2 input)
    {
        // Skip if no input.
        if (input.sqrMagnitude < 0.001f)
        {
            return Vector2.zero;
        }
        
        // Get magnitude and direction.
        float magnitude = input.magnitude;
        Vector2 direction = input / magnitude;
        
        // Apply the appropriate mode.
        switch (currentState.movementInputMode)
        {
            case Fused_InputMode.Digital:
                // Digital mode: Any input = full magnitude.
                // This is Celeste-style - most responsive, binary movement.
                return direction;
                
            case Fused_InputMode.Analog:
                // Analog mode: Magnitude affects speed.
                // Apply sensitivity curve for fine control.
                float curvedMagnitude = ApplyAnalogCurve(magnitude);
                return direction * curvedMagnitude;
                
            case Fused_InputMode.Hybrid:
                // Hybrid mode: Analog up to threshold, then snaps to full.
                // Best of both worlds - analog precision with full speed option.
                if (magnitude >= currentState.digitalThreshold)
                {
                    return direction;
                }
                else
                {
                    // Scale analog portion to fill 0 to threshold range.
                    float analogPortion = magnitude / currentState.digitalThreshold;
                    float curvedAnalog = ApplyAnalogCurve(analogPortion);
                    return direction * curvedAnalog;
                }
                
            default:
                return input;
        }
    }
    
    /// <summary>
    /// Applies the analog sensitivity curve to a magnitude value.
    /// Uses either an exponent or a custom AnimationCurve.
    /// </summary>
    /// <param name="magnitude">The input magnitude (0-1).</param>
    /// <returns>The curved magnitude (0-1).</returns>
    private float ApplyAnalogCurve(float magnitude)
    {
        // Use custom curve if enabled.
        if (currentState.useCustomAnalogCurve && currentState.analogResponseCurve != null)
        {
            return currentState.analogResponseCurve.Evaluate(magnitude);
        }
        
        // Otherwise use the exponent.
        // Exponent < 1 = more sensitive at low values (aggressive).
        // Exponent > 1 = less sensitive at low values (smooth).
        // Exponent = 1 = linear (no change).
        return Mathf.Pow(magnitude, currentState.analogSensitivityExponent);
    }
    
    /// <summary>
    /// Applies input smoothing to reduce jitter.
    /// NOT recommended for precision platformers as it adds latency.
    /// </summary>
    /// <param name="input">The input to smooth.</param>
    /// <returns>The smoothed input.</returns>
    private Vector2 ApplyInputSmoothing(Vector2 input)
    {
        switch (currentState.inputSmoothingMode)
        {
            case Fused_InputSmoothingMode.Lerp:
                // Linear interpolation: Move toward target by fixed factor.
                smoothedMoveInput = Vector2.Lerp(smoothedMoveInput, input, 
                    1f - currentState.inputSmoothingFactor);
                return smoothedMoveInput;
                
            case Fused_InputSmoothingMode.Exponential:
                // Exponential smoothing: More responsive to large changes.
                float factor = 1f - Mathf.Pow(currentState.inputSmoothingFactor, Time.deltaTime * 60f);
                smoothedMoveInput = Vector2.Lerp(smoothedMoveInput, input, factor);
                return smoothedMoveInput;
                
            case Fused_InputSmoothingMode.MovingAverage:
                // Moving average: Average of last N frames.
                return ApplyMovingAverageSmoothing(input);
                
            default:
                return input;
        }
    }
    
    /// <summary>
    /// Applies moving average smoothing using a circular buffer.
    /// Averages the last N frames of input for stable but delayed response.
    /// </summary>
    /// <param name="input">The current frame's input.</param>
    /// <returns>The averaged input.</returns>
    private Vector2 ApplyMovingAverageSmoothing(Vector2 input)
    {
        // Store current input in the circular buffer.
        inputHistory[inputHistoryIndex] = input;
        inputHistoryIndex = (inputHistoryIndex + 1) % currentState.movingAverageFrames;
        
        // Check if buffer is filled (for first N frames).
        if (inputHistoryIndex == 0)
        {
            inputHistoryFilled = true;
        }
        
        // Calculate the average of stored inputs.
        Vector2 sum = Vector2.zero;
        int count = inputHistoryFilled ? currentState.movingAverageFrames : inputHistoryIndex;
        
        for (int i = 0; i < count; i++)
        {
            sum += inputHistory[i];
        }
        
        return count > 0 ? sum / count : input;
    }
    
    /// <summary>
    /// Gets a snapped direction for precision actions like dash and wall jump.
    /// Quantizes analog input into discrete directions based on settings.
    /// </summary>
    /// <param name="input">The input direction to snap.</param>
    /// <returns>The snapped direction vector.</returns>
    public Vector2 GetSnappedDirection(Vector2 input)
    {
        // If snapping disabled or no state, return normalized input.
        if (currentState == null || !currentState.enableDirectionSnapping)
        {
            return input.magnitude > 0.001f ? input.normalized : Vector2.zero;
        }
        
        // Check minimum magnitude for direction registration.
        if (input.magnitude < currentState.directionSnapMinMagnitude)
        {
            return Vector2.zero;
        }
        
        // Calculate the angle of the input (0-360 degrees, 0 = right).
        float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;
        
        // Snap to the nearest direction based on snap count and zone mode.
        float snappedAngle = SnapAngleToDirection(angle);
        
        // Convert back to vector.
        float rad = snappedAngle * Mathf.Deg2Rad;
        Vector2 snapped = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        
        // Cache for multi-frame actions.
        lastSnappedDirection = snapped;
        
        return snapped;
    }
    
    /// <summary>
    /// Snaps an angle to the nearest discrete direction.
    /// Takes into account direction count and bias settings.
    /// </summary>
    /// <param name="angle">The input angle in degrees (0-360).</param>
    /// <returns>The snapped angle in degrees.</returns>
    private float SnapAngleToDirection(float angle)
    {
        // Get the number of directions and calculate zone size.
        int dirCount = (int)currentState.directionSnapCount;
        float baseZoneSize = 360f / dirCount;
        
        // Calculate which zone the angle falls into.
        // For 8-way: zones are centered at 0, 45, 90, 135, 180, 225, 270, 315.
        
        if (currentState.directionZoneMode == Fused_DirectionZoneMode.Equal)
        {
            // Equal zones: Simple quantization.
            // Offset by half zone to center zones on cardinal/diagonal directions.
            float offsetAngle = angle + (baseZoneSize / 2f);
            int zoneIndex = Mathf.FloorToInt(offsetAngle / baseZoneSize) % dirCount;
            return zoneIndex * baseZoneSize;
        }
        else
        {
            // Biased zones: Cardinals and diagonals have different sizes.
            return SnapAngleWithBias(angle, dirCount);
        }
    }
    
    /// <summary>
    /// Snaps an angle to direction with cardinal/diagonal bias.
    /// Cardinals are at 0, 90, 180, 270. Diagonals at 45, 135, 225, 315.
    /// </summary>
    /// <param name="angle">The input angle in degrees.</param>
    /// <param name="dirCount">The number of directions (4, 8, or 16).</param>
    /// <returns>The snapped angle in degrees.</returns>
    private float SnapAngleWithBias(float angle, int dirCount)
    {
        // Only apply bias for 8-way (4-way has no diagonals, 16-way is too fine).
        if (dirCount != 8)
        {
            float baseZoneSize = 360f / dirCount;
            float offsetAngle = angle + (baseZoneSize / 2f);
            int zoneIndex = Mathf.FloorToInt(offsetAngle / baseZoneSize) % dirCount;
            return zoneIndex * baseZoneSize;
        }
        
        // Calculate biased zone sizes.
        float bias = currentState.cardinalBiasAngle;
        bool isCardinalBiased = currentState.directionZoneMode == Fused_DirectionZoneMode.CardinalBiased;
        
        // Cardinal zone size and diagonal zone size.
        float cardinalZone = isCardinalBiased ? 45f + bias : 45f - bias;
        float diagonalZone = isCardinalBiased ? 45f - bias : 45f + bias;
        
        // Clamp zones to valid range.
        cardinalZone = Mathf.Clamp(cardinalZone, 5f, 85f);
        diagonalZone = Mathf.Clamp(diagonalZone, 5f, 85f);
        
        // Define the 8 directions and their zones.
        // Zones are centered on each direction.
        float[] directions = { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f };
        float[] zoneSizes = { cardinalZone, diagonalZone, cardinalZone, diagonalZone,
                              cardinalZone, diagonalZone, cardinalZone, diagonalZone };
        
        // Find which zone the angle falls into.
        float currentBoundary = 0f;
        for (int i = 0; i < 8; i++)
        {
            float halfZone = zoneSizes[i] / 2f;
            float zoneStart = directions[i] - halfZone;
            float zoneEnd = directions[i] + halfZone;
            
            // Handle wraparound at 0/360.
            if (zoneStart < 0)
            {
                if (angle >= (360f + zoneStart) || angle < zoneEnd)
                {
                    return directions[i];
                }
            }
            else if (zoneEnd > 360f)
            {
                if (angle >= zoneStart || angle < (zoneEnd - 360f))
                {
                    return directions[i];
                }
            }
            else
            {
                if (angle >= zoneStart && angle < zoneEnd)
                {
                    return directions[i];
                }
            }
        }
        
        // Fallback: snap to nearest direction.
        float nearestAngle = 0f;
        float nearestDist = 360f;
        foreach (float dir in directions)
        {
            float dist = Mathf.Abs(Mathf.DeltaAngle(angle, dir));
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearestAngle = dir;
            }
        }
        
        return nearestAngle;
    }
    
    /// <summary>
    /// Checks if the current input is pressing toward a wall.
    /// Uses the configured wall slide input threshold for controller support.
    /// </summary>
    /// <param name="wallDir">The direction of the wall (-1 = left, 1 = right).</param>
    /// <returns>True if pressing toward the wall past the threshold.</returns>
    public bool IsPressingTowardWall(int wallDir)
    {
        // Get the threshold from state, or use default if not available.
        float threshold = currentState != null ? currentState.wallSlideInputThreshold : 0.1f;
        
        // Check if horizontal input is toward the wall and past threshold.
        return moveInput.x * wallDir > threshold;
    }
    
    /// <summary>
    /// Gets the wall jump direction based on input and settings.
    /// Handles neutral wall jumps and directional wall jumps.
    /// </summary>
    /// <param name="wallDir">The direction of the wall (-1 = left, 1 = right).</param>
    /// <returns>The wall jump direction as a normalized vector.</returns>
    public Vector2 GetWallJumpDirection(int wallDir)
    {
        if (currentState == null)
        {
            // Default: jump away from wall at 45 degrees up.
            return new Vector2(-wallDir, 1f).normalized;
        }
        
        // Get input magnitude for determining neutral vs directional.
        float inputMagnitude = moveInput.magnitude;
        
        // Check for neutral wall jump (no directional input).
        if (currentState.enableNeutralWallJump && inputMagnitude < currentState.wallJumpInputThreshold)
        {
            // Neutral wall jump: Mostly vertical with slight push away.
            return new Vector2(
                -wallDir * currentState.neutralWallJumpHorizontalMultiplier,
                currentState.neutralWallJumpVerticalMultiplier
            ).normalized;
        }
        
        // Check for directional wall jump.
        if (currentState.enableDirectionalWallJump && inputMagnitude >= currentState.wallJumpInputThreshold)
        {
            // Get the snapped direction for precision.
            Vector2 inputDir = GetSnappedDirection(moveInput);
            
            // Calculate the base away direction (horizontal, away from wall).
            Vector2 awayDir = new Vector2(-wallDir, 0f);
            
            // Calculate the angle between input and away direction.
            float inputAngle = Mathf.Atan2(inputDir.y, inputDir.x) * Mathf.Rad2Deg;
            float awayAngle = Mathf.Atan2(awayDir.y, awayDir.x) * Mathf.Rad2Deg;
            float angleDiff = Mathf.DeltaAngle(awayAngle, inputAngle);
            
            // Clamp the angle difference to the maximum allowed.
            float maxAngle = currentState.directionalWallJumpMaxAngle;
            angleDiff = Mathf.Clamp(angleDiff, -maxAngle, maxAngle);
            
            // Calculate final angle and convert to direction.
            float finalAngle = (awayAngle + angleDiff) * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(finalAngle), Mathf.Sin(finalAngle));
        }
        
        // Default: Jump away from wall at standard angle.
        return new Vector2(-wallDir, 1f).normalized;
    }
    
    /// <summary>
    /// Gets the dash direction based on current input and settings.
    /// Uses direction snapping for precise 8-way or 4-way dashing.
    /// </summary>
    /// <returns>The dash direction as a normalized vector, or zero if no valid direction.</returns>
    public Vector2 GetDashDirection()
    {
        if (currentState == null)
        {
            // Default: dash in input direction or facing direction.
            if (moveInput.magnitude > 0.1f)
            {
                return moveInput.normalized;
            }
            return new Vector2(isFacingRight ? 1f : -1f, 0f);
        }
        
        // Handle different dash direction modes.
        switch (currentState.dashDirectionMode)
        {
            case Fused_DashDirectionMode.FacingDirection:
                // Always dash in facing direction regardless of input.
                return new Vector2(isFacingRight ? 1f : -1f, 0f);
                
            case Fused_DashDirectionMode.HorizontalOnly:
                // Dash horizontally based on input or facing.
                if (Mathf.Abs(moveInput.x) > currentState.directionSnapMinMagnitude)
                {
                    return new Vector2(Mathf.Sign(moveInput.x), 0f);
                }
                return new Vector2(isFacingRight ? 1f : -1f, 0f);
                
            case Fused_DashDirectionMode.InputCardinal:
            case Fused_DashDirectionMode.InputEightWay:
                // Use snapped direction for precise dashing.
                Vector2 snapped = GetSnappedDirection(moveInput);
                if (snapped.magnitude < 0.1f)
                {
                    // No input: dash in facing direction.
                    return new Vector2(isFacingRight ? 1f : -1f, 0f);
                }
                return snapped;
                
            default:
                return moveInput.magnitude > 0.1f ? moveInput.normalized : 
                    new Vector2(isFacingRight ? 1f : -1f, 0f);
        }
    }
    
    /// <summary>
    /// Gets the raw, unprocessed input value.
    /// Useful for debugging or systems that need the original input.
    /// </summary>
    /// <returns>The raw input vector from the Input System.</returns>
    public Vector2 GetRawInput()
    {
        return rawMoveInput;
    }
    
    /// <summary>
    /// Gets the processed movement input.
    /// This is the input used for actual movement calculations.
    /// </summary>
    /// <returns>The processed input vector.</returns>
    public Vector2 GetProcessedInput()
    {
        return moveInput;
    }
    
    /// <summary>
    /// Gets the last snapped direction that was calculated.
    /// Useful for consistent direction during multi-frame actions.
    /// </summary>
    /// <returns>The last snapped direction vector.</returns>
    public Vector2 GetLastSnappedDirection()
    {
        return lastSnappedDirection;
    }
    
    #endregion
    
    // ========================================================================
    // SECTION: PUBLIC GETTERS
    // ========================================================================
    
    #region PUBLIC GETTERS
    
    public bool IsGrounded() => isGrounded;
    public bool IsDashing() => isDashing;
    public bool IsInvincible() => isInvincible;
    public bool IsWallSliding() => isWallSliding;
    public bool IsWallClimbing() => isWallClimbing;
    public bool IsWallClinging() => isWallClinging;
    public bool IsOnSlope() => isOnSlope;
    public bool IsOnSteepSlope() => isOnSteepSlope;
    public float GetCurrentSlopeAngle() => currentSlopeAngle;
    public bool IsAtApex() => isAtApex;
    public bool IsFastFalling() => isFastFalling;
    public bool IsInLandingLag() => isInLandingLag;
    public bool IsInWallJumpLock() => isInWallJumpLock;
    public int GetCurrentDashCharges() => currentDashCharges;
    public int GetMaxDashCharges() => currentState?.maxDashCharges ?? 1;
    public float GetCurrentStamina() => currentStamina;
    public float GetMaxStamina() => currentState?.maxStamina ?? 100f;
    public float GetBunnyHopBonus() => bunnyHopBonus;
    public FusedPlayerStateData GetCurrentStateData() => currentState;
    public int GetCurrentStateIndex() => currentStateIndex;
    public bool IsFacingRight() => isFacingRight;
    public int GetWallDirection() => wallDirection;
    public bool[] Get4DirectionalWallStates() => wallStates;
    
    #endregion
    
    // ========================================================================
    // SECTION: PUBLIC CONTROLS
    // ========================================================================
    
    #region PUBLIC CONTROLS
    
    public void RefillDashes()
    {
        if (currentState != null)
        {
            currentDashCharges = currentState.maxDashCharges;
        }
    }
    
    public void RefillStamina()
    {
        if (currentState != null)
        {
            currentStamina = currentState.maxStamina;
        }
    }
    
    public void ForceStateSwitch(int stateIndex)
    {
        if (stateIndex == 0 && primaryStateData != null)
        {
            currentState = primaryStateData;
            currentStateIndex = 0;
        }
        else if (stateIndex == 1 && secondaryStateData != null)
        {
            currentState = secondaryStateData;
            currentStateIndex = 1;
        }
        
        ApplyStateData();
        OnStateChanged?.Invoke(currentStateIndex);
    }
    
    public void Teleport(Vector3 position)
    {
        transform.position = position;
        if (rb != null)
        {
            rb.position = position;
            rb.linearVelocity = Vector3.zero;
        }
    }
    
    public void ApplyExternalForce(Vector3 force, ForceMode mode = ForceMode.Impulse)
    {
        if (rb != null)
        {
            rb.AddForce(force, mode);
        }
        isJumping = false;
    }
    
    public void SetVelocity(Vector3 velocity)
    {
        if (rb != null)
        {
            rb.linearVelocity = velocity;
        }
    }
    
    #endregion
    
    // ========================================================================
    // SECTION: DEBUG GIZMOS
    // ========================================================================
    
    #region DEBUG GIZMOS
    
    private void OnDrawGizmosSelected()
    {
        if (playerCollider == null)
        {
            playerCollider = GetComponent<Collider>();
        }
        if (playerCollider == null) return;
        
        // Ground detection
        if (showGroundGizmos)
        {
            Vector3 groundCheckCenter = transform.position - 
                new Vector3(0f, playerCollider.bounds.extents.y, 0f);
            
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireCube(groundCheckCenter, groundCheckSize);
            
            Gizmos.color = isAboutToLand ? Color.yellow : Color.gray;
            Gizmos.DrawLine(groundCheckCenter, groundCheckCenter + Vector3.down * groundRaycastDistance);
        }
        
        // Slope detection
        if (showSlopeGizmos && isOnSlope)
        {
            Vector3 groundCheckCenter = transform.position - 
                new Vector3(0f, playerCollider.bounds.extents.y, 0f);
            
            Gizmos.color = isOnSteepSlope ? Color.red : Color.cyan;
            Gizmos.DrawRay(groundCheckCenter, slopeNormal * 2f);
            
#if UNITY_EDITOR
            UnityEditor.Handles.Label(groundCheckCenter + Vector3.up, $"Slope: {currentSlopeAngle:F1}°");
#endif
        }
        
        // Wall detection
        if (showWallGizmos)
        {
            Vector3 checkDir = GetWallCheckDirection();
            Vector3 checkSize = GetWallCheckSize();
            
            Gizmos.color = (wallDirection > 0) ? Color.yellow : new Color(0f, 1f, 0f, 0.5f);
            Gizmos.DrawWireCube(transform.position + checkDir * wallCheckDistance, checkSize);
            
            Gizmos.color = (wallDirection < 0) ? Color.yellow : new Color(1f, 0f, 0f, 0.5f);
            Gizmos.DrawWireCube(transform.position - checkDir * wallCheckDistance, checkSize);
        }
        
        // Direction indicators
        if (showDirectionGizmos)
        {
            Vector3 moveRight = GetMovementRight();
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(transform.position + Vector3.up, moveRight * 2f);
            
            Gizmos.color = Color.cyan;
            Vector3 facingDir = isFacingRight ? moveRight : -moveRight;
            Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, facingDir * 1.5f);
            
            if (rb != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(transform.position, rb.linearVelocity * 0.2f);
            }
        }
        
        // State labels
#if UNITY_EDITOR
        string stateText = "";
        if (isWallSliding) stateText += "SLIDE ";
        if (isWallClimbing) stateText += "CLIMB ";
        if (isWallClinging) stateText += "CLING ";
        if (isDashing) stateText += "DASH ";
        if (isAtApex) stateText += "APEX ";
        if (isFastFalling) stateText += "FAST ";
        if (isInWallJumpLock) stateText += "LOCK ";
        if (isInvincible) stateText += "INVULN ";
        if (isInLandingLag) stateText += "LAG ";
        if (bunnyHopBonus > 0) stateText += $"BHOP({bunnyHopBonus:F2}) ";
        
        if (!string.IsNullOrEmpty(stateText))
        {
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, stateText);
        }
        
        string resourceText = $"Dash: {currentDashCharges}/{(currentState?.maxDashCharges ?? 1)}";
        if (currentState != null && currentState.staminaMode != Fused_StaminaMode.Disabled)
        {
            resourceText += $"\nStamina: {currentStamina:F0}/{currentState.maxStamina}";
        }
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2.5f, resourceText);
#endif
    }
    
    #endregion
}
