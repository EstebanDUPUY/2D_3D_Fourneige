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
    
    private Vector2 moveInput;
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
        currentState = primaryStateData;
        currentStateIndex = 0;
        ApplyStateData();
        
        if (currentState != null)
        {
            currentDashCharges = currentState.maxDashCharges;
            currentStamina = currentState.maxStamina;
            airJumpsRemaining = currentState.maxAirJumps;
        }
        
        isFacingRight = true;
        facingAngle = 0f;
        
        InitializeWorldRotation();
        ConfigureRigidbody();
        
        if (logStateChanges)
        {
            Debug.Log($"[Fused_PlayerController] Initialized with state: {currentState?.stateName ?? "NULL"}");
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
        if (isFrozenForRotation) return;
        if (currentState == null || !currentState.canMove) return;
        
        // Update detection states
        UpdateGroundDetection();
        UpdateSlopeDetection();
        UpdateWallDetection();
        
        // Update timers
        UpdateJumpTimers();
        UpdateDashTimers();
        UpdateWallTimers();
        UpdateStamina();
        UpdateStateSwitchCooldown();
        UpdateLandingLag();
        UpdateBunnyHop();
        UpdateAirControlTiming();
        
        // Process input
        ProcessJumpInput();
        ProcessDashInput();
        
        // Update wall mechanics
        UpdateWallMechanics();
        
        // Reset frame flags
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
        if (currentState == null) return;
        
        isWallSliding = false;
        isWallClimbing = false;
        isWallClinging = false;
        
        Vector3 vel = rb.linearVelocity;
        vel.y = 0f;
        
        // Vertical force
        float verticalForce = currentState.wallJumpVerticalForce > 0 
            ? currentState.wallJumpVerticalForce 
            : currentState.jumpForce;
        
        vel.y = verticalForce;
        
        // Horizontal push
        Vector3 pushDir = GetMovementRight() * (-wallDirection);
        vel += pushDir * currentState.wallJumpHorizontalForce;
        
        rb.linearVelocity = vel;
        
        // Set wall jump lock
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
        
        // Check stamina
        if (currentState.staminaMode != Fused_StaminaMode.Disabled && currentStamina <= 0)
        {
            return;
        }
        
        // Wall slide conditions
        bool shouldWallSlide = false;
        
        switch (currentState.wallSlideMode)
        {
            case Fused_WallSlideMode.Automatic:
                shouldWallSlide = rb.linearVelocity.y <= 0 || currentState.canWallSlideWhileRising;
                break;
                
            case Fused_WallSlideMode.HoldToward:
                shouldWallSlide = (moveInput.x * wallDirection > 0) && 
                    (rb.linearVelocity.y <= 0 || currentState.canWallSlideWhileRising);
                break;
                
            case Fused_WallSlideMode.GrabButton:
                shouldWallSlide = grabHeld && 
                    (rb.linearVelocity.y <= 0 || currentState.canWallSlideWhileRising);
                break;
        }
        
        // Wall cling mode (Fez feature)
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
                    isWallClinging = moveInput.x * wallDirection > 0;
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
    
    private Vector3 CalculateDashDirection()
    {
        Vector3 dir = Vector3.zero;
        Vector3 right = GetMovementRight();
        
        switch (currentState.dashDirectionMode)
        {
            case Fused_DashDirectionMode.FacingDirection:
                dir = isFacingRight ? right : -right;
                break;
                
            case Fused_DashDirectionMode.InputCardinal:
                if (Mathf.Abs(moveInput.x) > 0.1f)
                {
                    dir = moveInput.x > 0 ? right : -right;
                }
                else if (Mathf.Abs(moveInput.y) > 0.1f)
                {
                    dir = moveInput.y > 0 ? Vector3.up : Vector3.down;
                }
                else
                {
                    dir = isFacingRight ? right : -right;
                }
                break;
                
            case Fused_DashDirectionMode.InputEightWay:
                if (Mathf.Abs(moveInput.x) > 0.1f || Mathf.Abs(moveInput.y) > 0.1f)
                {
                    dir = right * moveInput.x + Vector3.up * moveInput.y;
                    dir.Normalize();
                }
                else
                {
                    dir = isFacingRight ? right : -right;
                }
                break;
                
            case Fused_DashDirectionMode.HorizontalOnly:
                dir = isFacingRight ? right : -right;
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
    
    #region INPUT CALLBACKS
    
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
    
    public void OnJumpInput(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            jumpPressedThisFrame = true;
            jumpHeld = true;
            
            if (currentState != null && currentState.jumpBufferEnabled)
            {
                jumpBuffered = true;
                jumpBufferTimer = currentState.jumpBufferDuration;
            }
        }
        else if (context.canceled)
        {
            jumpHeld = false;
            canExtendJump = false;
        }
    }
    
    public void OnDashInput(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            dashPressedThisFrame = true;
        }
    }
    
    public void OnGrabInput(InputAction.CallbackContext context)
    {
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
