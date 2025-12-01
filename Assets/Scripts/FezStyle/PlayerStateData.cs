using UnityEngine;

// ==================== ENUMS ====================
#region ENUMS

/// <summary>
/// Determines how wall jump pushes the player away from the wall.
/// </summary>
public enum WallJumpPushMode
{
    /// <summary>Push toward screen space (camera-relative). Feels consistent regardless of world orientation.</summary>
    CameraRelative,
    /// <summary>Push based on wall's actual collision normal. Physically accurate.</summary>
    WallNormal
}

/// <summary>
/// Determines the direction of dash based on input and facing.
/// </summary>
public enum DashDirectionMode
{
    /// <summary>Always dash in the direction the player is facing.</summary>
    FacingDirection,
    /// <summary>Dash in horizontal input direction (left/right only).</summary>
    InputDirection,
    /// <summary>Dash in input direction including vertical (up/down).</summary>
    InputWithVertical,
    /// <summary>Full 8-way dash including diagonals.</summary>
    EightDirectional
}

/// <summary>
/// Visual feedback style during dash invincibility.
/// </summary>
public enum DashInvincibilityVisual
{
    /// <summary>No visual feedback during invincibility.</summary>
    None,
    /// <summary>Sprite flickers on/off rapidly.</summary>
    Flicker,
    /// <summary>Sprite becomes semi-transparent.</summary>
    Transparent,
    /// <summary>Sprite shifts to a specified invincibility color.</summary>
    ColorShift,
    /// <summary>Leave afterimage trail during dash.</summary>
    Trail
}

/// <summary>
/// Determines how dash charges are recharged.
/// </summary>
public enum DashRechargeMode
{
    /// <summary>All charges recharge simultaneously.</summary>
    AllAtOnce,
    /// <summary>Charges recharge one at a time sequentially.</summary>
    OneAtATime,
    /// <summary>All charges reset to max on landing.</summary>
    ResetOnLand
}

/// <summary>
/// Wall cling behavior mode.
/// </summary>
public enum WallClingMode
{
    /// <summary>Wall cling is completely disabled.</summary>
    Disabled,
    /// <summary>Player clings first, then slides after duration expires.</summary>
    ClingThenSlide,
    /// <summary>Player can toggle between cling and slide with input.</summary>
    InputToggle,
    /// <summary>Player clings while holding input toward wall, slides otherwise.</summary>
    HoldToStick
}

/// <summary>
/// How landing lag affects player control.
/// </summary>
public enum LandingLagMode
{
    /// <summary>No landing lag.</summary>
    Disabled,
    /// <summary>Freeze all input during landing lag.</summary>
    FreezeAll,
    /// <summary>Only prevent jumping/dashing, allow movement.</summary>
    PreventActionsOnly,
    /// <summary>Reduce movement speed during landing lag.</summary>
    ReducedMovement
}

/// <summary>
/// How the player behaves on steep slopes.
/// </summary>
public enum SteepSlopeBehavior
{
    /// <summary>Player cannot stand on steep slopes at all (forced slide).</summary>
    ForcedSlide,
    /// <summary>Player slides but can still move and jump.</summary>
    SlideWithControl,
    /// <summary>Player slides with reduced control.</summary>
    SlideReducedControl
}

#endregion

/// <summary>
/// Fully modular ScriptableObject for player state configuration.
/// Every movement feature can be toggled on/off and fine-tuned independently.
/// 
/// Design Philosophy:
/// - No hardcoded exclusive features
/// - Every state is defined purely by its parameter values and toggles
/// - Maximum flexibility for iteration and experimentation
/// - All values editable in Inspector
/// 
/// Usage: Create assets via Right-click > Create > Player > State Data
/// Configure each state's capabilities by enabling/disabling features and tuning values.
/// </summary>
[CreateAssetMenu(fileName = "_New State Data", menuName = "_Player/State Data")]
public class PlayerStateData : ScriptableObject
{
    #region VARIABLES

    // ==================== IDENTIFICATION ====================
    [Header("Identification")]

    /// <summary>State name for debugging and identification.</summary>
    [Tooltip("State name for debugging and identification")]
    public string stateName = "New State";

    /// <summary>Visual feedback color for this state.</summary>
    [Tooltip("Visual feedback color - tints sprite when active")]
    public Color stateColor = Color.white;

    // ==================== MASTER CONTROLS ====================
    [Header("Master Controls")]

    /// <summary>Master toggle - if FALSE, completely disables all player movement.</summary>
    [Tooltip("Master toggle - disables ALL movement when false")]
    public bool canMove = true;

    /// <summary>If FALSE, state switching is disabled while this state is active.</summary>
    [Tooltip("Allow switching away from this state")]
    public bool canSwitchFromThisState = true;

    /// <summary>Cooldown after switching to this state before switching again.</summary>
    [Tooltip("Cooldown duration after switching to this state")]
    [Range(0f, 5f)]
    public float stateSwitchCooldown = 0.5f;

    // ==================== BASIC MOVEMENT ====================
    [Header("Basic Movement")]

    /// <summary>If FALSE, disables horizontal ground movement.</summary>
    [Tooltip("Enable horizontal ground movement")]
    public bool canMoveOnGround = true;

    /// <summary>Maximum horizontal movement speed.</summary>
    [Tooltip("Maximum horizontal movement speed")]
    [Range(1f, 50f)]
    public float moveSpeed = 15f;

    /// <summary>How quickly the player accelerates to target speed.</summary>
    [Tooltip("Acceleration rate - higher = snappier")]
    [Range(10f, 500f)]
    public float acceleration = 100f;

    /// <summary>Multiplier applied to first frame of movement for instant responsiveness.</summary>
    [Tooltip("Speed boost on first frame of movement")]
    [Range(1f, 5f)]
    public float startingSpeedBoost = 2f;

    /// <summary>How quickly the player slows down when no input is given.</summary>
    [Tooltip("Deceleration rate - higher = stops faster")]
    [Range(10f, 500f)]
    public float deceleration = 150f;

    // ==================== MOVEMENT FEEL (NEW) ====================
    [Header("Movement Feel")]

    /// <summary>Minimum speed to consider 'moving' (prevents micro-drift).</summary>
    [Tooltip("Minimum speed threshold - prevents micro-drift")]
    [Range(0f, 1f)]
    public float minimumMoveSpeed = 0.1f;

    /// <summary>Apply deceleration when changing direction.</summary>
    [Tooltip("Apply extra deceleration when turning around")]
    public bool decelerateOnTurn = true;

    /// <summary>Turn deceleration multiplier.</summary>
    [Tooltip("Deceleration multiplier when turning")]
    [Range(1f, 5f)]
    public float turnDecelerationMultiplier = 1.5f;

    // ==================== SLOPES (NEW) ====================
    [Header("Slopes")]

    /// <summary>Can walk on slopes.</summary>
    [Tooltip("Enable slope walking")]
    public bool canWalkOnSlopes = true;

    /// <summary>Maximum walkable slope angle in degrees.</summary>
    [Tooltip("Maximum angle player can walk on")]
    [Range(0f, 89f)]
    public float maxSlopeAngle = 45f;

    /// <summary>Behavior when on slopes steeper than max.</summary>
    [Tooltip("How player behaves on steep slopes")]
    public SteepSlopeBehavior steepSlopeBehavior = SteepSlopeBehavior.SlideWithControl;

    /// <summary>Speed when sliding down steep slopes.</summary>
    [Tooltip("Slide speed on steep slopes")]
    [Range(1f, 50f)]
    public float steepSlopeSlideSpeed = 10f;

    /// <summary>Control multiplier when sliding on steep slopes (for SlideReducedControl mode).</summary>
    [Tooltip("Movement control multiplier during steep slope slide")]
    [Range(0f, 1f)]
    public float steepSlopeControlMultiplier = 0.3f;

    /// <summary>Can the player jump while on a steep slope.</summary>
    [Tooltip("Allow jumping while on steep slopes")]
    public bool canJumpOnSteepSlope = true;

    // ==================== AIR CONTROL ====================
    [Header("Air Control")]

    /// <summary>If TRUE, player can move horizontally while airborne.</summary>
    [Tooltip("Enable horizontal air control")]
    public bool hasAirControl = true;

    /// <summary>Percentage of ground control available in air.</summary>
    [Tooltip("Air control strength (0 = none, 1 = full)")]
    [Range(0f, 1f)]
    public float airControlMultiplier = 0.8f;

    // ==================== AIR CONTROL TIMING (NEW) ====================
    [Header("Air Control Timing")]

    /// <summary>Delay before air control activates after leaving ground.</summary>
    [Tooltip("Delay before air control kicks in")]
    [Range(0f, 1f)]
    public float airControlDelay = 0f;

    /// <summary>Air control ramp-up time (0 = instant full control).</summary>
    [Tooltip("Time to ramp up to full air control")]
    [Range(0f, 1f)]
    public float airControlRampUpTime = 0f;

    /// <summary>Can change direction instantly in air.</summary>
    [Tooltip("Allow instant direction change in air")]
    public bool airInstantTurn = true;

    /// <summary>Air deceleration when no input (0 = maintain momentum).</summary>
    [Tooltip("Air deceleration rate when no input")]
    [Range(0f, 100f)]
    public float airDeceleration = 0f;

    // ==================== PHYSICS ====================
    [Header("Physics")]

    /// <summary>Additional gravity force applied to the character.</summary>
    [Tooltip("Custom gravity strength")]
    [Range(0f, 100f)]
    public float gravity = 30f;

    /// <summary>Rigidbody mass value.</summary>
    [Tooltip("Character mass (affects physics interactions)")]
    [Range(0.1f, 10f)]
    public float weight = 1f;

    /// <summary>Ground friction when not moving.</summary>
    [Tooltip("Ground friction (0 = ice, 1 = instant stop)")]
    [Range(0f, 1f)]
    public float groundFriction = 0.5f;

    // ==================== FALL SPEED (NEW) ====================
    [Header("Fall Speed")]

    /// <summary>Maximum fall speed (terminal velocity).</summary>
    [Tooltip("Maximum falling speed")]
    [Range(10f, 100f)]
    public float maxFallSpeed = 50f;

    /// <summary>Enable fast fall when holding down.</summary>
    [Tooltip("Enable fast fall when holding down")]
    public bool hasFastFall = false;

    /// <summary>Fast fall speed multiplier.</summary>
    [Tooltip("Fast fall gravity multiplier")]
    [Range(1f, 5f)]
    public float fastFallMultiplier = 1.5f;

    /// <summary>Fast fall requires pressing down (vs just holding).</summary>
    [Tooltip("Require pressing down (vs holding) for fast fall")]
    public bool fastFallRequiresPress = false;

    // ==================== LANDING (NEW) ====================
    [Header("Landing")]

    /// <summary>Landing lag mode.</summary>
    [Tooltip("Landing lag behavior")]
    public LandingLagMode landingLagMode = LandingLagMode.Disabled;

    /// <summary>Fall speed threshold for hard landing.</summary>
    [Tooltip("Fall speed that triggers hard landing")]
    [Range(5f, 50f)]
    public float hardLandingThreshold = 20f;

    /// <summary>Landing lag duration.</summary>
    [Tooltip("Duration of landing lag")]
    [Range(0f, 1f)]
    public float landingLagDuration = 0.1f;

    /// <summary>Reduce landing lag if holding jump (for bunny hop feel).</summary>
    [Tooltip("Reduce lag if jump is buffered")]
    public bool reduceLandingLagOnJumpBuffer = true;

    /// <summary>Landing lag reduction multiplier when jump is buffered.</summary>
    [Tooltip("Landing lag multiplier when jump buffered")]
    [Range(0f, 1f)]
    public float landingLagReductionMultiplier = 0.5f;

    /// <summary>Movement speed multiplier during landing lag (for ReducedMovement mode).</summary>
    [Tooltip("Movement speed during landing lag")]
    [Range(0f, 1f)]
    public float landingLagMovementMultiplier = 0.3f;

    // ==================== JUMP SYSTEM ====================
    [Header("Jump System")]

    /// <summary>Master toggle for jumping.</summary>
    [Tooltip("Enable jumping")]
    public bool canJump = true;

    /// <summary>Initial upward velocity applied when jumping.</summary>
    [Tooltip("Jump force (impulse)")]
    [Range(1f, 50f)]
    public float jumpForce = 15f;

    // ==================== JUMP FEEL (NEW) ====================
    [Header("Jump Feel")]

    /// <summary>Apply horizontal boost when jumping while moving.</summary>
    [Tooltip("Add horizontal momentum on jump")]
    public bool jumpMomentumBoost = false;

    /// <summary>Horizontal momentum multiplier on jump.</summary>
    [Tooltip("Horizontal momentum multiplier")]
    [Range(1f, 2f)]
    public float jumpMomentumMultiplier = 1.1f;

    /// <summary>Jump force multiplier when jumping while moving fast.</summary>
    [Tooltip("Extra jump height when moving fast")]
    [Range(1f, 1.5f)]
    public float movingJumpBonus = 1f;

    /// <summary>Speed threshold for moving jump bonus.</summary>
    [Tooltip("Speed required for moving jump bonus")]
    [Range(0f, 20f)]
    public float movingJumpSpeedThreshold = 10f;

    // ==================== VARIABLE JUMP ====================
    [Header("Variable Jump")]

    /// <summary>If TRUE, enables variable-height jumping.</summary>
    [Tooltip("Enable variable height jumps (hold = higher)")]
    public bool hasVariableJump = true;

    /// <summary>Gravity multiplier while holding jump and ascending.</summary>
    [Tooltip("Gravity while holding jump")]
    [Range(0.1f, 1f)]
    public float jumpHoldGravityMultiplier = 0.5f;

    /// <summary>Gravity multiplier when falling or after releasing jump early.</summary>
    [Tooltip("Gravity when falling or jump released")]
    [Range(1f, 5f)]
    public float jumpReleaseGravityMultiplier = 2.0f;

    /// <summary>Maximum duration the player can hold jump for extra height.</summary>
    [Tooltip("Max hold time for variable jump")]
    [Range(0.05f, 1f)]
    public float maxJumpHoldTime = 0.3f;

    // ==================== APEX HANG (NEW) ====================
    [Header("Apex Hang")]

    /// <summary>Reduce gravity at jump apex for floaty peak.</summary>
    [Tooltip("Enable floaty apex")]
    public bool hasApexHang = false;

    /// <summary>Velocity threshold to consider 'at apex'.</summary>
    [Tooltip("Velocity threshold for apex detection")]
    [Range(0.5f, 10f)]
    public float apexVelocityThreshold = 2f;

    /// <summary>Gravity multiplier at apex.</summary>
    [Tooltip("Gravity at apex (lower = floatier)")]
    [Range(0.1f, 1f)]
    public float apexGravityMultiplier = 0.5f;

    /// <summary>Air control multiplier at apex.</summary>
    [Tooltip("Extra air control at apex")]
    [Range(1f, 3f)]
    public float apexAirControlMultiplier = 1.3f;

    /// <summary>Should apex air control stack with regular air control.</summary>
    [Tooltip("Stack apex control with regular air control")]
    public bool apexControlStacks = true;

    // ==================== COYOTE TIME ====================
    [Header("Coyote Time")]

    /// <summary>If TRUE, enables coyote time.</summary>
    [Tooltip("Enable coyote time grace period")]
    public bool hasCoyoteTime = true;

    /// <summary>Grace period after leaving platform where jump is allowed.</summary>
    [Tooltip("Coyote time duration")]
    [Range(0.01f, 0.5f)]
    public float coyoteTimeDuration = 0.15f;

    // ==================== JUMP BUFFERING ====================
    [Header("Jump Buffering")]

    /// <summary>If TRUE, enables jump buffering.</summary>
    [Tooltip("Enable jump input buffering")]
    public bool hasJumpBuffer = true;

    /// <summary>Time window before landing where jump input is remembered.</summary>
    [Tooltip("Jump buffer time window")]
    [Range(0.01f, 0.5f)]
    public float jumpBufferTime = 0.2f;

    // ==================== DOUBLE JUMP ====================
    [Header("Double Jump")]

    /// <summary>If TRUE, enables double jump.</summary>
    [Tooltip("Enable air jumping")]
    public bool hasDoubleJump = false;

    /// <summary>Maximum number of air jumps.</summary>
    [Tooltip("Number of air jumps allowed")]
    [Range(1, 10)]
    public int maxAirJumps = 1;

    /// <summary>Force multiplier for air jumps.</summary>
    [Tooltip("Air jump force multiplier")]
    [Range(0.3f, 2f)]
    public float airJumpForceMultiplier = 0.9f;

    /// <summary>Reset air jumps when wall jumping.</summary>
    [Tooltip("Reset air jumps on wall jump")]
    public bool resetAirJumpsOnWallJump = true;

    // ==================== WALL SLIDE ====================
    [Header("Wall Slide")]

    /// <summary>If TRUE, enables wall sliding.</summary>
    [Tooltip("Enable wall sliding")]
    public bool hasWallSlide = true;

    /// <summary>If TRUE, player must hold toward wall to slide.</summary>
    [Tooltip("Require input toward wall to slide")]
    public bool wallSlideRequiresInput = false;

    /// <summary>Maximum fall speed when sliding (negative value).</summary>
    [Tooltip("Wall slide fall speed (negative = slower)")]
    [Range(-20f, -0.5f)]
    public float wallSlideFriction = -3f;

    /// <summary>Delay before wall slide activates after touching wall.</summary>
    [Tooltip("Delay before wall slide starts")]
    [Range(0f, 0.5f)]
    public float wallSlideDelay = 0f;

    /// <summary>Can slide on walls while moving upward.</summary>
    [Tooltip("Allow wall slide while rising")]
    public bool canWallSlideWhileRising = false;

    // ==================== WALL CLING (NEW) ====================
    [Header("Wall Cling")]

    /// <summary>Wall cling behavior mode.</summary>
    [Tooltip("Wall cling behavior")]
    public WallClingMode wallClingMode = WallClingMode.Disabled;

    /// <summary>Max duration player can cling before forced slide.</summary>
    [Tooltip("Max cling duration (0 = infinite)")]
    [Range(0f, 10f)]
    public float maxWallClingDuration = 2f;

    /// <summary>Stamina cost per second while clinging (0 = no cost).</summary>
    [Tooltip("Stamina drain while clinging")]
    [Range(0f, 50f)]
    public float wallClingStaminaCost = 0f;

    /// <summary>Can the player jump from wall cling.</summary>
    [Tooltip("Allow jumping while clinging")]
    public bool canJumpFromWallCling = true;

    /// <summary>Wall cling gravity (0 = stick perfectly).</summary>
    [Tooltip("Gravity while wall clinging")]
    [Range(0f, 10f)]
    public float wallClingGravity = 0f;

    // ==================== WALL JUMP ====================
    [Header("Wall Jump")]

    /// <summary>If TRUE, enables wall jumping.</summary>
    [Tooltip("Enable wall jumping")]
    public bool hasWallJump = true;

    /// <summary>Upward force during wall jump (0 = use jumpForce).</summary>
    [Tooltip("Wall jump vertical force (0 = use jump force)")]
    [Range(0f, 50f)]
    public float wallJumpForce = 0f;

    /// <summary>Horizontal push multiplier when wall jumping.</summary>
    [Tooltip("Horizontal push strength")]
    [Range(0.1f, 2f)]
    public float wallJumpHorizontalMultiplier = 0.7f;

    /// <summary>Direction mode for wall jump push.</summary>
    [Tooltip("Wall jump push direction mode")]
    public WallJumpPushMode wallJumpPushMode = WallJumpPushMode.CameraRelative;

    // ==================== WALL JUMP CONTROL LOCK (NEW) ====================
    [Header("Wall Jump Control Lock")]

    /// <summary>Enable air control lock after wall jump.</summary>
    [Tooltip("Lock air control after wall jump")]
    public bool hasWallJumpControlLock = false;

    /// <summary>Time after wall jump where air control is modified.</summary>
    [Tooltip("Control lock duration")]
    [Range(0f, 1f)]
    public float wallJumpControlLockDuration = 0.2f;

    /// <summary>Air control multiplier during wall jump lock.</summary>
    [Tooltip("Air control during lock (0 = none)")]
    [Range(0f, 1f)]
    public float wallJumpControlLockMultiplier = 0.3f;

    /// <summary>Only apply lock when trying to return to same wall.</summary>
    [Tooltip("Only lock when returning to same wall")]
    public bool wallJumpLockOnlyOnReturn = false;

    // ==================== DASH SYSTEM ====================
    [Header("Dash System")]

    /// <summary>If TRUE, enables ground dash ability.</summary>
    [Tooltip("Enable dash ability")]
    public bool hasDash = false;

    /// <summary>If TRUE, enables dashing while airborne.</summary>
    [Tooltip("Enable air dashing")]
    public bool hasAirDash = false;

    /// <summary>Cooldown after dashing before dashing again.</summary>
    [Tooltip("Dash cooldown time")]
    [Range(0f, 5f)]
    public float dashCooldown = 0f;

    // ==================== DASH DIRECTION (NEW) ====================
    [Header("Dash Direction")]

    /// <summary>Dash direction mode.</summary>
    [Tooltip("How dash direction is determined")]
    public DashDirectionMode dashDirectionMode = DashDirectionMode.FacingDirection;

    /// <summary>Can dash vertically (up/down).</summary>
    [Tooltip("Allow vertical dashing")]
    public bool dashCanBeVertical = false;

    /// <summary>Restrict dash to pure cardinal directions.</summary>
    [Tooltip("Only allow cardinal direction dashes")]
    public bool dashCardinalOnly = true;

    // ==================== DASH PHYSICS ====================
    [Header("Dash Physics")]

    /// <summary>If TRUE, dash travels fixed distance.</summary>
    [Tooltip("Dash by distance (vs by duration)")]
    public bool dashIsFixedDistance = false;

    /// <summary>Distance traveled during dash.</summary>
    [Tooltip("Dash distance (if fixed distance)")]
    [Range(1f, 20f)]
    public float dashDistance = 5f;

    /// <summary>Duration of dash.</summary>
    [Tooltip("Dash duration in seconds")]
    [Range(0.05f, 1f)]
    public float dashDuration = 0.3f;

    /// <summary>Speed during dash.</summary>
    [Tooltip("Dash speed (if fixed duration)")]
    [Range(5f, 100f)]
    public float dashSpeed = 20f;

    /// <summary>Velocity multiplier applied after dash ends.</summary>
    [Tooltip("Velocity kept after dash (0 = stop)")]
    [Range(0f, 1f)]
    public float dashEndVelocityMultiplier = 0f;

    /// <summary>Cancel dash on wall collision.</summary>
    [Tooltip("Stop dash when hitting wall")]
    public bool dashCancelOnWall = true;

    /// <summary>Cancel dash on taking damage.</summary>
    [Tooltip("Stop dash when damaged")]
    public bool dashCancelOnDamage = false;

    /// <summary>Apply gravity during dash.</summary>
    [Tooltip("Apply gravity while dashing")]
    public bool dashHasGravity = false;

    /// <summary>Gravity multiplier during dash (if dashHasGravity).</summary>
    [Tooltip("Gravity strength during dash")]
    [Range(0f, 1f)]
    public float dashGravityMultiplier = 0.5f;

    // ==================== DASH INVINCIBILITY ====================
    [Header("Dash Invincibility")]

    /// <summary>If TRUE, player is invincible during dash.</summary>
    [Tooltip("Enable invincibility during dash")]
    public bool dashHasInvincibility = false;

    /// <summary>Duration of invincibility during dash.</summary>
    [Tooltip("Invincibility duration")]
    [Range(0f, 1f)]
    public float dashInvincibilityDuration = 0.3f;

    /// <summary>Visual feedback style during invincibility.</summary>
    [Tooltip("Visual effect during invincibility")]
    public DashInvincibilityVisual dashInvincibilityVisual = DashInvincibilityVisual.Flicker;

    /// <summary>Flicker speed for Flicker visual mode.</summary>
    [Tooltip("Flicker frequency")]
    [Range(5f, 60f)]
    public float dashFlickerSpeed = 30f;

    /// <summary>Alpha value for Transparent visual mode.</summary>
    [Tooltip("Transparency during invincibility")]
    [Range(0f, 1f)]
    public float dashTransparencyAlpha = 0.5f;

    /// <summary>Color for ColorShift visual mode.</summary>
    [Tooltip("Color during invincibility")]
    public Color dashInvincibilityColor = Color.white;

    // ==================== DASH CHARGES (NEW) ====================
    [Header("Dash Charges")]

    /// <summary>Maximum dash charges.</summary>
    [Tooltip("Max number of dash charges")]
    [Range(1, 10)]
    public int maxDashCharges = 1;

    /// <summary>Time to recharge one dash charge.</summary>
    [Tooltip("Recharge time per charge")]
    [Range(0f, 10f)]
    public float dashRechargeTime = 0f;

    /// <summary>Dash recharge mode.</summary>
    [Tooltip("How dash charges recharge")]
    public DashRechargeMode dashRechargeMode = DashRechargeMode.ResetOnLand;

    /// <summary>Recharge dashes only when grounded.</summary>
    [Tooltip("Only recharge while grounded")]
    public bool dashRechargeOnlyGrounded = true;

    /// <summary>Delay before dash recharge starts.</summary>
    [Tooltip("Delay before recharge begins")]
    [Range(0f, 5f)]
    public float dashRechargeDelay = 0f;

    // ==================== BUNNY HOP ====================
    [Header("Bunny Hop")]

    /// <summary>If TRUE, enables bunny hopping.</summary>
    [Tooltip("Enable bunny hop speed bonus")]
    public bool hasBunnyHop = false;

    /// <summary>Speed bonus per successful bunny hop.</summary>
    [Tooltip("Speed bonus per hop")]
    [Range(0f, 0.5f)]
    public float bunnyHopSpeedBonus = 0.15f;

    /// <summary>How quickly bunny hop bonus decays.</summary>
    [Tooltip("Bonus decay rate")]
    [Range(0.1f, 5f)]
    public float bunnyHopDecayRate = 1f;

    /// <summary>Maximum speed from bunny hopping.</summary>
    [Tooltip("Max speed multiplier from bunny hop")]
    [Range(1f, 3f)]
    public float bunnyHopMaxSpeed = 1.5f;

    /// <summary>Time window after landing to chain bunny hop.</summary>
    [Tooltip("Timing window for bunny hop")]
    [Range(0.01f, 0.5f)]
    public float bunnyHopTimingWindow = 0.1f;

    #endregion
}
