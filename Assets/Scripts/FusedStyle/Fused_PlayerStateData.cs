// ============================================================================
// FUSED PLAYER STATE DATA - CELESTE FEEL + FEZ FEATURES
// ============================================================================
// This ScriptableObject merges the best of both worlds:
// - Celeste's instant, snappy movement feel (no floatiness)
// - All Fez functionalities (slopes, landing lag, bunny hop, wall cling, etc.)
// - Maximum Inspector editability (Range: -999999 to +999999)
// - Every feature is toggleable
//
// USAGE: Create via Right-click > Create > Fused > Player State Data
// ============================================================================

using UnityEngine;

// ============================================================================
// ENUMERATIONS - All modes from both systems
// ============================================================================

#region ENUMERATIONS

/// <summary>
/// Acceleration mode - Celeste uses INSTANT for snappy feel.
/// </summary>
public enum Fused_AccelerationMode
{
    /// <summary>Instant target speed - CELESTE STYLE (recommended).</summary>
    Instant,
    /// <summary>Linear acceleration over time.</summary>
    Linear,
    /// <summary>Ease-in curve acceleration.</summary>
    EaseIn,
    /// <summary>Custom AnimationCurve acceleration.</summary>
    Custom
}

/// <summary>
/// Deceleration mode - Celeste uses INSTANT for precise stopping.
/// </summary>
public enum Fused_DecelerationMode
{
    /// <summary>Instant stop - CELESTE STYLE (recommended).</summary>
    Instant,
    /// <summary>Slide before stopping (ice-like).</summary>
    Slide,
    /// <summary>Friction-based deceleration.</summary>
    Friction,
    /// <summary>Custom curve deceleration.</summary>
    Custom
}

/// <summary>
/// Gravity mode for nuanced jump feel.
/// </summary>
public enum Fused_GravityMode
{
    /// <summary>Constant gravity always.</summary>
    Constant,
    /// <summary>Variable gravity based on state - CELESTE STYLE.</summary>
    Variable,
    /// <summary>Custom curve gravity.</summary>
    Custom
}

/// <summary>
/// Wall jump push direction mode.
/// </summary>
public enum Fused_WallJumpMode
{
    /// <summary>Camera-relative push (consistent after rotation).</summary>
    CameraRelative,
    /// <summary>Based on wall collision normal.</summary>
    WallNormal,
    /// <summary>Always horizontal in screen space.</summary>
    HorizontalOnly
}

/// <summary>
/// Dash direction determination mode.
/// </summary>
public enum Fused_DashDirectionMode
{
    /// <summary>Dash in facing direction.</summary>
    FacingDirection,
    /// <summary>4-direction based on input.</summary>
    InputCardinal,
    /// <summary>8-direction including diagonals - CELESTE STYLE.</summary>
    InputEightWay,
    /// <summary>Always dash horizontally.</summary>
    HorizontalOnly
}

/// <summary>
/// Dash charge refill mode.
/// </summary>
public enum Fused_DashRefillMode
{
    /// <summary>Refill on touching ground - CELESTE STYLE.</summary>
    OnGroundTouch,
    /// <summary>Refill after delay on ground.</summary>
    OnGroundDelay,
    /// <summary>Refill over time.</summary>
    OverTime,
    /// <summary>Only from pickups/crystals.</summary>
    PickupOnly
}

/// <summary>
/// Visual effect during invincibility.
/// </summary>
public enum Fused_InvincibilityVisual
{
    None,
    Flicker,
    Transparent,
    ColorShift,
    Trail
}

/// <summary>
/// Wall slide behavior mode.
/// </summary>
public enum Fused_WallSlideMode
{
    /// <summary>Wall slide disabled.</summary>
    Disabled,
    /// <summary>Auto slide when touching wall and falling.</summary>
    Automatic,
    /// <summary>Must hold toward wall to slide.</summary>
    HoldToward,
    /// <summary>Must hold grab button to slide.</summary>
    GrabButton
}

/// <summary>
/// Wall cling behavior mode (Fez feature).
/// </summary>
public enum Fused_WallClingMode
{
    /// <summary>Wall cling disabled.</summary>
    Disabled,
    /// <summary>Cling first, then slide after duration.</summary>
    ClingThenSlide,
    /// <summary>Toggle between cling and slide with input.</summary>
    InputToggle,
    /// <summary>Cling while holding toward wall.</summary>
    HoldToStick
}

/// <summary>
/// Stamina behavior for wall climbing.
/// </summary>
public enum Fused_StaminaMode
{
    /// <summary>No stamina - infinite wall time.</summary>
    Disabled,
    /// <summary>Drain while on wall - CELESTE STYLE.</summary>
    DrainOnWall,
    /// <summary>Drain only while climbing.</summary>
    DrainOnClimb,
    /// <summary>Accelerated drain while climbing.</summary>
    DrainFasterOnClimb
}

/// <summary>
/// Steep slope behavior (Fez feature).
/// </summary>
public enum Fused_SteepSlopeBehavior
{
    /// <summary>Forced slide, no control.</summary>
    ForcedSlide,
    /// <summary>Slide but can still move and jump.</summary>
    SlideWithControl,
    /// <summary>Slide with reduced control.</summary>
    SlideReducedControl
}

/// <summary>
/// Landing lag mode (Fez feature).
/// </summary>
public enum Fused_LandingLagMode
{
    /// <summary>No landing lag - CELESTE STYLE.</summary>
    Disabled,
    /// <summary>Freeze all input during lag.</summary>
    FreezeAll,
    /// <summary>Only prevent actions, allow movement.</summary>
    PreventActionsOnly,
    /// <summary>Reduced movement speed during lag.</summary>
    ReducedMovement
}

#endregion

// ============================================================================
// SCRIPTABLE OBJECT DEFINITION
// ============================================================================

/// <summary>
/// Complete player state configuration merging Celeste and Fez features.
/// Create instances via: Right-click > Create > Fused > Player State Data
/// </summary>
[CreateAssetMenu(fileName = "New Fused State", menuName = "Fused/Player State Data", order = 1)]
public class FusedPlayerStateData : ScriptableObject
{
    // ========================================================================
    // SECTION: IDENTIFICATION
    // ========================================================================
    
    #region IDENTIFICATION
    
    [Header("═══════════════════════════════════════")]
    [Header("        IDENTIFICATION")]
    [Header("═══════════════════════════════════════")]
    
    [Tooltip("Name for this state configuration.")]
    public string stateName = "Default State";
    
    [Tooltip("Sprite tint color when this state is active.")]
    public Color stateColor = Color.white;
    
    [Tooltip("Optional description.")]
    [TextArea(2, 4)]
    public string description = "";
    
    #endregion
    
    // ========================================================================
    // SECTION: MASTER CONTROLS
    // ========================================================================
    
    #region MASTER CONTROLS
    
    [Header("═══════════════════════════════════════")]
    [Header("        MASTER CONTROLS")]
    [Header("═══════════════════════════════════════")]
    
    [Tooltip("Master toggle - FALSE = player is completely frozen.")]
    public bool canMove = true;
    
    [Tooltip("Can the player switch to a different state?")]
    public bool canSwitchState = true;
    
    [Tooltip("Cooldown after switching states.")]
    [Range(-999999f, 999999f)]
    public float stateSwitchCooldown = 0.1f;
    
    #endregion
    
    // ========================================================================
    // SECTION: HORIZONTAL MOVEMENT - CELESTE SNAPPY FEEL
    // ========================================================================
    
    #region HORIZONTAL MOVEMENT
    
    [Header("═══════════════════════════════════════")]
    [Header("        HORIZONTAL MOVEMENT")]
    [Header("═══════════════════════════════════════")]
    
    // --- SPEED SETTINGS ---
    
    [Tooltip("Maximum horizontal speed. Celeste ~= 9.0")]
    [Range(-999999f, 999999f)]
    public float maxMoveSpeed = 9f;
    
    [Tooltip("Speed threshold below which player stops (prevents micro-drift).")]
    [Range(-999999f, 999999f)]
    public float minimumSpeedThreshold = 0.1f;
    
    // --- ACCELERATION (CELESTE = INSTANT) ---
    
    [Tooltip("Acceleration mode. INSTANT = Celeste-style snappy.")]
    public Fused_AccelerationMode accelerationMode = Fused_AccelerationMode.Instant;
    
    [Tooltip("Acceleration rate (for non-Instant modes).")]
    [Range(-999999f, 999999f)]
    public float accelerationRate = 200f;
    
    [Tooltip("Custom acceleration curve.")]
    public AnimationCurve accelerationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    
    [Tooltip("Time to reach max speed (curve-based).")]
    [Range(-999999f, 999999f)]
    public float accelerationTime = 0.1f;
    
    [Tooltip("First-frame speed boost (Fez feature).")]
    [Range(-999999f, 999999f)]
    public float startingSpeedBoost = 1f;
    
    // --- DECELERATION (CELESTE = INSTANT) ---
    
    [Tooltip("Deceleration mode. INSTANT = Celeste-style precise stop.")]
    public Fused_DecelerationMode decelerationMode = Fused_DecelerationMode.Instant;
    
    [Tooltip("Deceleration rate (for Friction mode).")]
    [Range(-999999f, 999999f)]
    public float decelerationRate = 300f;
    
    [Tooltip("Custom deceleration curve.")]
    public AnimationCurve decelerationCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
    
    [Tooltip("Time to stop completely (curve-based).")]
    [Range(-999999f, 999999f)]
    public float decelerationTime = 0.05f;
    
    // --- TURNING ---
    
    [Tooltip("TRUE = instant direction change (Celeste-style).")]
    public bool instantTurning = true;
    
    [Tooltip("Speed multiplier during turn (if not instant).")]
    [Range(-999999f, 999999f)]
    public float turnSpeedMultiplier = 0.5f;
    
    [Tooltip("Apply extra deceleration when turning (Fez feature).")]
    public bool decelerateOnTurn = false;
    
    [Tooltip("Turn deceleration multiplier.")]
    [Range(-999999f, 999999f)]
    public float turnDecelerationMultiplier = 1.5f;
    
    #endregion
    
    // ========================================================================
    // SECTION: SLOPES (FEZ FEATURE)
    // ========================================================================
    
    #region SLOPES
    
    [Header("═══════════════════════════════════════")]
    [Header("        SLOPES")]
    [Header("═══════════════════════════════════════")]
    
    [Tooltip("Enable slope walking.")]
    public bool canWalkOnSlopes = true;
    
    [Tooltip("Maximum walkable slope angle.")]
    [Range(-999999f, 999999f)]
    public float maxSlopeAngle = 45f;
    
    [Tooltip("Behavior on steep slopes.")]
    public Fused_SteepSlopeBehavior steepSlopeBehavior = Fused_SteepSlopeBehavior.SlideWithControl;
    
    [Tooltip("Slide speed on steep slopes.")]
    [Range(-999999f, 999999f)]
    public float steepSlopeSlideSpeed = 10f;
    
    [Tooltip("Control multiplier on steep slopes.")]
    [Range(-999999f, 999999f)]
    public float steepSlopeControlMultiplier = 0.3f;
    
    [Tooltip("Allow jumping on steep slopes.")]
    public bool canJumpOnSteepSlope = true;
    
    #endregion
    
    // ========================================================================
    // SECTION: AIR CONTROL
    // ========================================================================
    
    #region AIR CONTROL
    
    [Header("═══════════════════════════════════════")]
    [Header("        AIR CONTROL")]
    [Header("═══════════════════════════════════════")]
    
    [Tooltip("Enable air control.")]
    public bool airControlEnabled = true;
    
    [Tooltip("Air control strength (1.0 = full, Celeste-style).")]
    [Range(-999999f, 999999f)]
    public float airControlMultiplier = 1f;
    
    [Tooltip("Delay before air control activates.")]
    [Range(-999999f, 999999f)]
    public float airControlDelay = 0f;
    
    [Tooltip("Time to ramp up to full air control.")]
    [Range(-999999f, 999999f)]
    public float airControlRampUpTime = 0f;
    
    [Tooltip("Allow instant direction change in air.")]
    public bool airInstantTurn = true;
    
    [Tooltip("Air deceleration when no input.")]
    [Range(-999999f, 999999f)]
    public float airDeceleration = 0f;
    
    #endregion
    
    // ========================================================================
    // SECTION: JUMPING
    // ========================================================================
    
    #region JUMPING
    
    [Header("═══════════════════════════════════════")]
    [Header("        JUMPING")]
    [Header("═══════════════════════════════════════")]
    
    // --- BASIC JUMP ---
    
    [Tooltip("Enable jumping.")]
    public bool canJump = true;
    
    [Tooltip("Initial jump velocity. Celeste ~= 10.5")]
    [Range(-999999f, 999999f)]
    public float jumpForce = 10.5f;
    
    [Tooltip("TRUE = impulse, FALSE = set velocity directly.")]
    public bool jumpAsImpulse = false;
    
    // --- VARIABLE JUMP HEIGHT (CELESTE CORE FEATURE) ---
    
    [Tooltip("Enable variable jump height (hold = higher).")]
    public bool variableJumpHeight = true;
    
    [Tooltip("Minimum jump height multiplier (tap jumps).")]
    [Range(-999999f, 999999f)]
    public float minJumpMultiplier = 0.4f;
    
    [Tooltip("Max seconds to hold jump for extra height.")]
    [Range(-999999f, 999999f)]
    public float maxJumpHoldTime = 0.2f;
    
    // --- JUMP FEEL (FEZ FEATURES) ---
    
    [Tooltip("Add horizontal momentum on jump.")]
    public bool jumpMomentumBoost = false;
    
    [Tooltip("Horizontal momentum multiplier on jump.")]
    [Range(-999999f, 999999f)]
    public float jumpMomentumMultiplier = 1.1f;
    
    [Tooltip("Extra jump height when moving fast.")]
    [Range(-999999f, 999999f)]
    public float movingJumpBonus = 1f;
    
    [Tooltip("Speed required for moving jump bonus.")]
    [Range(-999999f, 999999f)]
    public float movingJumpSpeedThreshold = 10f;
    
    // --- COYOTE TIME (ESSENTIAL) ---
    
    [Tooltip("Enable coyote time.")]
    public bool coyoteTimeEnabled = true;
    
    [Tooltip("Coyote time duration. Celeste ~= 0.1")]
    [Range(-999999f, 999999f)]
    public float coyoteTimeDuration = 0.1f;
    
    // --- JUMP BUFFERING (ESSENTIAL) ---
    
    [Tooltip("Enable jump buffering.")]
    public bool jumpBufferEnabled = true;
    
    [Tooltip("Jump buffer duration.")]
    [Range(-999999f, 999999f)]
    public float jumpBufferDuration = 0.1f;
    
    // --- AIR JUMPS / DOUBLE JUMP ---
    
    [Tooltip("Enable air jumps.")]
    public bool airJumpEnabled = false;
    
    [Tooltip("Number of air jumps allowed.")]
    [Range(-999999, 999999)]
    public int maxAirJumps = 1;
    
    [Tooltip("Air jump force multiplier.")]
    [Range(-999999f, 999999f)]
    public float airJumpForceMultiplier = 0.85f;
    
    [Tooltip("Restore air jumps when touching wall.")]
    public bool restoreAirJumpsOnWall = false;
    
    [Tooltip("Reset air jumps on wall jump.")]
    public bool resetAirJumpsOnWallJump = true;
    
    #endregion
    
    // ========================================================================
    // SECTION: GRAVITY AND FALLING
    // ========================================================================
    
    #region GRAVITY AND FALLING
    
    [Header("═══════════════════════════════════════")]
    [Header("        GRAVITY & FALLING")]
    [Header("═══════════════════════════════════════")]
    
    // --- GRAVITY MODE ---
    
    [Tooltip("Gravity calculation mode. Variable = Celeste-style.")]
    public Fused_GravityMode gravityMode = Fused_GravityMode.Variable;
    
    [Tooltip("Base gravity strength.")]
    [Range(-999999f, 999999f)]
    public float baseGravity = 40f;
    
    // --- VARIABLE GRAVITY MULTIPLIERS (CELESTE FEEL) ---
    
    [Tooltip("Gravity while rising and holding jump.")]
    [Range(-999999f, 999999f)]
    public float risingGravityMultiplier = 0.5f;
    
    [Tooltip("Gravity when jump released early.")]
    [Range(-999999f, 999999f)]
    public float jumpCutGravityMultiplier = 2.5f;
    
    [Tooltip("Gravity while falling.")]
    [Range(-999999f, 999999f)]
    public float fallingGravityMultiplier = 1.5f;
    
    // --- APEX HANG (CELESTE FEATURE) ---
    
    [Tooltip("Enable reduced gravity at apex.")]
    public bool apexHangEnabled = true;
    
    [Tooltip("Velocity threshold for apex detection.")]
    [Range(-999999f, 999999f)]
    public float apexVelocityThreshold = 2f;
    
    [Tooltip("Gravity multiplier at apex.")]
    [Range(-999999f, 999999f)]
    public float apexGravityMultiplier = 0.4f;
    
    [Tooltip("Air control bonus at apex.")]
    [Range(-999999f, 999999f)]
    public float apexAirControlBonus = 1.3f;
    
    // --- FALL SPEED ---
    
    [Tooltip("Maximum fall speed (terminal velocity).")]
    [Range(-999999f, 999999f)]
    public float maxFallSpeed = 25f;
    
    // --- FAST FALL (FEZ FEATURE) ---
    
    [Tooltip("Enable fast fall when holding down.")]
    public bool fastFallEnabled = false;
    
    [Tooltip("Fast fall gravity multiplier.")]
    [Range(-999999f, 999999f)]
    public float fastFallMultiplier = 1.5f;
    
    [Tooltip("Require pressing (vs holding) for fast fall.")]
    public bool fastFallRequiresPress = false;
    
    #endregion
    
    // ========================================================================
    // SECTION: LANDING LAG (FEZ FEATURE)
    // ========================================================================
    
    #region LANDING LAG
    
    [Header("═══════════════════════════════════════")]
    [Header("        LANDING LAG")]
    [Header("═══════════════════════════════════════")]
    
    [Tooltip("Landing lag mode. Disabled = Celeste-style.")]
    public Fused_LandingLagMode landingLagMode = Fused_LandingLagMode.Disabled;
    
    [Tooltip("Fall speed that triggers hard landing.")]
    [Range(-999999f, 999999f)]
    public float hardLandingThreshold = 20f;
    
    [Tooltip("Landing lag duration.")]
    [Range(-999999f, 999999f)]
    public float landingLagDuration = 0.1f;
    
    [Tooltip("Reduce lag if jump is buffered.")]
    public bool reduceLandingLagOnJumpBuffer = true;
    
    [Tooltip("Landing lag reduction multiplier.")]
    [Range(-999999f, 999999f)]
    public float landingLagReductionMultiplier = 0.5f;
    
    [Tooltip("Movement speed during landing lag.")]
    [Range(-999999f, 999999f)]
    public float landingLagMovementMultiplier = 0.3f;
    
    #endregion
    
    // ========================================================================
    // SECTION: WALL SLIDE
    // ========================================================================
    
    #region WALL SLIDE
    
    [Header("═══════════════════════════════════════")]
    [Header("        WALL SLIDE")]
    [Header("═══════════════════════════════════════")]
    
    [Tooltip("Wall slide mode.")]
    public Fused_WallSlideMode wallSlideMode = Fused_WallSlideMode.Automatic;
    
    [Tooltip("Wall slide fall speed (negative = slower).")]
    [Range(-999999f, 999999f)]
    public float wallSlideSpeed = -3f;
    
    [Tooltip("Delay before wall slide activates.")]
    [Range(-999999f, 999999f)]
    public float wallSlideStartDelay = 0f;
    
    [Tooltip("Can wall slide while rising.")]
    public bool canWallSlideWhileRising = false;
    
    #endregion
    
    // ========================================================================
    // SECTION: WALL CLIMB (CELESTE FEATURE)
    // ========================================================================
    
    #region WALL CLIMB
    
    [Header("═══════════════════════════════════════")]
    [Header("        WALL CLIMB")]
    [Header("═══════════════════════════════════════")]
    
    [Tooltip("Enable wall climbing.")]
    public bool wallClimbEnabled = true;
    
    [Tooltip("Wall climb speed.")]
    [Range(-999999f, 999999f)]
    public float wallClimbSpeed = 4f;
    
    #endregion
    
    // ========================================================================
    // SECTION: WALL CLING (FEZ FEATURE)
    // ========================================================================
    
    #region WALL CLING
    
    [Header("═══════════════════════════════════════")]
    [Header("        WALL CLING")]
    [Header("═══════════════════════════════════════")]
    
    [Tooltip("Wall cling behavior mode.")]
    public Fused_WallClingMode wallClingMode = Fused_WallClingMode.Disabled;
    
    [Tooltip("Max cling duration (0 = infinite).")]
    [Range(-999999f, 999999f)]
    public float maxWallClingDuration = 2f;
    
    [Tooltip("Gravity while wall clinging.")]
    [Range(-999999f, 999999f)]
    public float wallClingGravity = 0f;
    
    [Tooltip("Can jump from wall cling.")]
    public bool canJumpFromWallCling = true;
    
    #endregion
    
    // ========================================================================
    // SECTION: STAMINA (CELESTE FEATURE)
    // ========================================================================
    
    #region STAMINA
    
    [Header("═══════════════════════════════════════")]
    [Header("        STAMINA")]
    [Header("═══════════════════════════════════════")]
    
    [Tooltip("Stamina mode.")]
    public Fused_StaminaMode staminaMode = Fused_StaminaMode.DrainOnWall;
    
    [Tooltip("Maximum stamina.")]
    [Range(-999999f, 999999f)]
    public float maxStamina = 100f;
    
    [Tooltip("Stamina drain rate per second.")]
    [Range(-999999f, 999999f)]
    public float staminaDrainRate = 20f;
    
    [Tooltip("Extra stamina drain while climbing.")]
    [Range(-999999f, 999999f)]
    public float climbExtraDrain = 10f;
    
    [Tooltip("Stamina refill rate per second (on ground).")]
    [Range(-999999f, 999999f)]
    public float staminaRefillRate = 50f;
    
    [Tooltip("Delay before stamina refill starts.")]
    [Range(-999999f, 999999f)]
    public float staminaRefillDelay = 0.5f;
    
    #endregion
    
    // ========================================================================
    // SECTION: WALL JUMP
    // ========================================================================
    
    #region WALL JUMP
    
    [Header("═══════════════════════════════════════")]
    [Header("        WALL JUMP")]
    [Header("═══════════════════════════════════════")]
    
    [Tooltip("Enable wall jumping.")]
    public bool wallJumpEnabled = true;
    
    [Tooltip("Wall jump mode.")]
    public Fused_WallJumpMode wallJumpMode = Fused_WallJumpMode.CameraRelative;
    
    [Tooltip("Wall jump vertical force (0 = use jumpForce).")]
    [Range(-999999f, 999999f)]
    public float wallJumpVerticalForce = 0f;
    
    [Tooltip("Wall jump horizontal push force.")]
    [Range(-999999f, 999999f)]
    public float wallJumpHorizontalForce = 8f;
    
    // --- WALL JUMP CONTROL LOCK (CELESTE FEATURE) ---
    
    [Tooltip("Enable control lock after wall jump.")]
    public bool wallJumpControlLockEnabled = true;
    
    [Tooltip("Wall jump control lock duration.")]
    [Range(-999999f, 999999f)]
    public float wallJumpControlLockDuration = 0.15f;
    
    [Tooltip("Control multiplier during lock (0 = no control).")]
    [Range(-999999f, 999999f)]
    public float wallJumpLockControlMultiplier = 0.3f;
    
    [Tooltip("Only lock when returning to same wall.")]
    public bool wallJumpLockOnlyOnReturn = false;
    
    #endregion
    
    // ========================================================================
    // SECTION: DASH SYSTEM
    // ========================================================================
    
    #region DASH SYSTEM
    
    [Header("═══════════════════════════════════════")]
    [Header("        DASH SYSTEM")]
    [Header("═══════════════════════════════════════")]
    
    // --- BASIC DASH ---
    
    [Tooltip("Enable dash.")]
    public bool dashEnabled = true;
    
    [Tooltip("Enable air dash.")]
    public bool airDashEnabled = true;
    
    [Tooltip("Dash direction mode.")]
    public Fused_DashDirectionMode dashDirectionMode = Fused_DashDirectionMode.InputEightWay;
    
    // --- DASH CHARGES ---
    
    [Tooltip("Maximum dash charges.")]
    [Range(-999999, 999999)]
    public int maxDashCharges = 1;
    
    [Tooltip("Dash refill mode.")]
    public Fused_DashRefillMode dashRefillMode = Fused_DashRefillMode.OnGroundTouch;
    
    [Tooltip("Delay before dash refill starts.")]
    [Range(-999999f, 999999f)]
    public float dashRefillDelay = 0f;
    
    [Tooltip("Time to refill one dash charge.")]
    [Range(-999999f, 999999f)]
    public float dashRefillTime = 0.5f;
    
    // --- DASH PHYSICS ---
    
    [Tooltip("Dash speed.")]
    [Range(-999999f, 999999f)]
    public float dashSpeed = 24f;
    
    [Tooltip("Dash duration in seconds.")]
    [Range(-999999f, 999999f)]
    public float dashDuration = 0.15f;
    
    [Tooltip("Use fixed distance instead of duration.")]
    public bool dashIsFixedDistance = false;
    
    [Tooltip("Fixed dash distance.")]
    [Range(-999999f, 999999f)]
    public float dashDistance = 5f;
    
    [Tooltip("Freeze gravity during dash (Celeste-style).")]
    public bool dashFreezesGravity = true;
    
    [Tooltip("Velocity kept after dash ends (multiplier).")]
    [Range(-999999f, 999999f)]
    public float dashEndVelocityMultiplier = 0.4f;
    
    [Tooltip("Cancel dash on wall collision.")]
    public bool dashCancelOnWall = false;
    
    // --- UPWARD DASH BOOST (CELESTE FEATURE) ---
    
    [Tooltip("Upward dashes preserve more momentum.")]
    public bool upwardDashBoost = true;
    
    [Tooltip("Extra momentum for upward dashes.")]
    [Range(-999999f, 999999f)]
    public float upwardDashBoostMultiplier = 1.2f;
    
    // --- DASH COOLDOWN ---
    
    [Tooltip("Minimum time between dashes.")]
    [Range(-999999f, 999999f)]
    public float dashCooldown = 0.1f;
    
    // --- DASH INVINCIBILITY ---
    
    [Tooltip("Enable invincibility during dash.")]
    public bool dashInvincibility = false;
    
    [Tooltip("Dash invincibility duration.")]
    [Range(-999999f, 999999f)]
    public float dashInvincibilityDuration = 0.15f;
    
    [Tooltip("Visual effect during invincibility.")]
    public Fused_InvincibilityVisual invincibilityVisual = Fused_InvincibilityVisual.Flicker;
    
    [Tooltip("Flicker speed.")]
    [Range(-999999f, 999999f)]
    public float flickerSpeed = 30f;
    
    [Tooltip("Transparency alpha value.")]
    [Range(-999999f, 999999f)]
    public float transparencyAlpha = 0.5f;
    
    [Tooltip("Color during invincibility.")]
    public Color invincibilityColor = new Color(1f, 1f, 1f, 0.8f);
    
    // --- SUPER DASH (CELESTE WAVEDASH) ---
    
    [Tooltip("Enable super dash (wall jump + dash combo).")]
    public bool superDashEnabled = false;
    
    [Tooltip("Super dash speed bonus.")]
    [Range(-999999f, 999999f)]
    public float superDashSpeedMultiplier = 1.4f;
    
    [Tooltip("Super dash timing window.")]
    [Range(-999999f, 999999f)]
    public float superDashWindow = 0.1f;
    
    #endregion
    
    // ========================================================================
    // SECTION: BUNNY HOP (FEZ FEATURE)
    // ========================================================================
    
    #region BUNNY HOP
    
    [Header("═══════════════════════════════════════")]
    [Header("        BUNNY HOP")]
    [Header("═══════════════════════════════════════")]
    
    [Tooltip("Enable bunny hop speed bonus.")]
    public bool bunnyHopEnabled = false;
    
    [Tooltip("Speed bonus per successful hop.")]
    [Range(-999999f, 999999f)]
    public float bunnyHopSpeedBonus = 0.15f;
    
    [Tooltip("Bonus decay rate per second.")]
    [Range(-999999f, 999999f)]
    public float bunnyHopDecayRate = 1f;
    
    [Tooltip("Max speed multiplier from bunny hop.")]
    [Range(-999999f, 999999f)]
    public float bunnyHopMaxSpeed = 1.5f;
    
    [Tooltip("Timing window for bunny hop.")]
    [Range(-999999f, 999999f)]
    public float bunnyHopTimingWindow = 0.1f;
    
    #endregion
    
    // ========================================================================
    // SECTION: PHYSICS
    // ========================================================================
    
    #region PHYSICS
    
    [Header("═══════════════════════════════════════")]
    [Header("        PHYSICS")]
    [Header("═══════════════════════════════════════")]
    
    [Tooltip("Player mass.")]
    [Range(-999999f, 999999f)]
    public float mass = 1f;
    
    [Tooltip("Ground friction.")]
    [Range(-999999f, 999999f)]
    public float groundFriction = 0.9f;
    
    [Tooltip("Air drag.")]
    [Range(-999999f, 999999f)]
    public float airDrag = 0f;
    
    #endregion
    
    // ========================================================================
    // SECTION: WORLD ROTATION INTEGRATION (FEZ-STYLE)
    // ========================================================================
    
    #region WORLD ROTATION INTEGRATION
    
    [Header("═══════════════════════════════════════")]
    [Header("        WORLD ROTATION (FEZ-STYLE)")]
    [Header("═══════════════════════════════════════")]
    
    [Tooltip("Freeze player during world rotation.")]
    public bool freezeDuringRotation = true;
    
    [Tooltip("Clear depth velocity after rotation.")]
    public bool clearDepthVelocityAfterRotation = true;
    
    [Tooltip("Preserve horizontal speed through rotation.")]
    public bool preserveMomentumThroughRotation = true;
    
    [Tooltip("Wall detection delay after rotation.")]
    [Range(-999999f, 999999f)]
    public float wallDetectionDelayAfterRotation = 0.1f;
    
    [Tooltip("Refill dashes after world rotation.")]
    public bool refillDashOnRotation = false;
    
    [Tooltip("Refill stamina after world rotation.")]
    public bool refillStaminaOnRotation = false;
    
    #endregion
    
    // ========================================================================
    // SECTION: CONTROLLER INPUT SETTINGS
    // ========================================================================
    // This section contains all settings related to joystick/gamepad input
    // processing. These settings fix common issues with analog stick input
    // such as diagonal movement being harder than cardinal movement,
    // imprecise dash directions, and drift from stick deadzones.
    // ========================================================================
    
    #region CONTROLLER INPUT SETTINGS
    
    [Header("═══════════════════════════════════════")]
    [Header("        CONTROLLER INPUT SETTINGS")]
    [Header("═══════════════════════════════════════")]
    
    // -------------------------------------------------------------------------
    // DEADZONE SETTINGS
    // -------------------------------------------------------------------------
    // Deadzones prevent tiny stick movements (drift) from registering as input.
    // There are two main deadzone shapes: Square and Circular.
    // Square deadzones make diagonals harder because the stick must travel
    // further to exit the deadzone corner. Circular deadzones fix this.
    // -------------------------------------------------------------------------
    
    [Tooltip("Enable custom deadzone processing. If FALSE, uses raw input from Input System.")]
    public bool useCustomDeadzone = true;
    
    [Tooltip("Deadzone shape. CIRCULAR fixes diagonal input being harder than cardinal.\n" +
             "SQUARE is the traditional approach but diagonals require more stick travel.\n" +
             "SCALED_RADIAL removes deadzone then rescales so small movements still register.")]
    public Fused_DeadzoneType deadzoneType = Fused_DeadzoneType.ScaledRadial;
    
    [Tooltip("Inner deadzone radius. Input magnitude below this is treated as zero.\n" +
             "Typical values: 0.1 to 0.25. Higher = less drift but less precision.")]
    [Range(-999999f, 999999f)]
    public float innerDeadzone = 0.15f;
    
    [Tooltip("Outer deadzone. Input magnitude above this is treated as maximum (1.0).\n" +
             "Helps ensure full speed is reachable even if stick doesn't hit edge.")]
    [Range(-999999f, 999999f)]
    public float outerDeadzone = 0.95f;
    
    // -------------------------------------------------------------------------
    // INPUT NORMALIZATION
    // -------------------------------------------------------------------------
    // When moving diagonally, raw input can have magnitude ~1.41 (sqrt of 2).
    // This makes diagonal movement faster unless normalized.
    // -------------------------------------------------------------------------
    
    [Tooltip("Normalize diagonal input so magnitude never exceeds 1.0.\n" +
             "Prevents diagonal movement from being faster than cardinal.")]
    public bool normalizeDiagonalInput = true;
    
    [Tooltip("Clamp final input magnitude to 1.0 after all processing.\n" +
             "Safety net to ensure input never exceeds expected range.")]
    public bool clampInputMagnitude = true;
    
    // -------------------------------------------------------------------------
    // ANALOG VS DIGITAL MOVEMENT
    // -------------------------------------------------------------------------
    // Celeste uses digital (binary) input - any input = full speed.
    // Traditional games use analog - partial stick = partial speed.
    // These settings let you choose or blend between both approaches.
    // -------------------------------------------------------------------------
    
    [Tooltip("Movement input mode.\n" +
             "DIGITAL: Any input past deadzone = full speed (Celeste-style).\n" +
             "ANALOG: Partial stick = partial speed (traditional).\n" +
             "HYBRID: Analog up to threshold, then snaps to full.")]
    public Fused_InputMode movementInputMode = Fused_InputMode.Digital;
    
    [Tooltip("For HYBRID mode: input magnitude above this becomes full speed.\n" +
             "Example: 0.8 means 80%+ stick tilt = full speed.")]
    [Range(-999999f, 999999f)]
    public float digitalThreshold = 0.8f;
    
    [Tooltip("For ANALOG mode: curve to apply to input magnitude.\n" +
             "1.0 = linear, <1.0 = more sensitive at low values, >1.0 = less sensitive at low values.")]
    [Range(-999999f, 999999f)]
    public float analogSensitivityExponent = 1.0f;
    
    [Tooltip("Custom analog response curve. X = raw input, Y = processed output.\n" +
             "Only used when analogSensitivityExponent is not 1.0 or when using custom curve mode.")]
    public AnimationCurve analogResponseCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    
    [Tooltip("Use the custom AnimationCurve instead of the exponent for analog response.")]
    public bool useCustomAnalogCurve = false;
    
    // -------------------------------------------------------------------------
    // DIRECTION SNAPPING (FOR DASH AND WALL JUMP)
    // -------------------------------------------------------------------------
    // Analog sticks make it hard to hit exact directions (up, diagonal, etc).
    // Direction snapping quantizes input into discrete directions.
    // This is crucial for 8-way dashing and precise wall jumps.
    // -------------------------------------------------------------------------
    
    [Tooltip("Enable direction snapping for actions like dash.\n" +
             "Quantizes analog input into discrete directions.")]
    public bool enableDirectionSnapping = true;
    
    [Tooltip("Number of directions to snap to.\n" +
             "4 = cardinal only, 8 = cardinal + diagonal, 16 = finer control.")]
    public Fused_DirectionSnapCount directionSnapCount = Fused_DirectionSnapCount.EightWay;
    
    [Tooltip("Direction snap zone mode.\n" +
             "EQUAL: All directions get equal-sized zones.\n" +
             "CARDINAL_BIASED: Cardinals get larger zones (easier to hit).\n" +
             "DIAGONAL_BIASED: Diagonals get larger zones.")]
    public Fused_DirectionZoneMode directionZoneMode = Fused_DirectionZoneMode.Equal;
    
    [Tooltip("For CARDINAL_BIASED mode: extra degrees added to cardinal zones.\n" +
             "Example: 15 means cardinals get 45+15=60°, diagonals get 45-15=30°.")]
    [Range(-999999f, 999999f)]
    public float cardinalBiasAngle = 15f;
    
    [Tooltip("Minimum input magnitude required to register a direction for snapping.\n" +
             "Below this, direction is considered neutral/none.")]
    [Range(-999999f, 999999f)]
    public float directionSnapMinMagnitude = 0.3f;
    
    // -------------------------------------------------------------------------
    // WALL INTERACTION INPUT THRESHOLDS
    // -------------------------------------------------------------------------
    // How much stick deflection is needed to trigger wall-related actions.
    // Lower values = more sensitive, higher values = more deliberate.
    // -------------------------------------------------------------------------
    
    [Tooltip("Minimum horizontal input magnitude to trigger wall slide (when using HoldToward mode).\n" +
             "Higher values require more deliberate stick movement toward the wall.")]
    [Range(-999999f, 999999f)]
    public float wallSlideInputThreshold = 0.3f;
    
    [Tooltip("Minimum input magnitude to influence wall jump direction.\n" +
             "Below this, wall jump uses default away-from-wall direction.")]
    [Range(-999999f, 999999f)]
    public float wallJumpInputThreshold = 0.2f;
    
    [Tooltip("Enable directional wall jumps. If TRUE, input direction affects jump angle.\n" +
             "If FALSE, wall jumps always push directly away from wall.")]
    public bool enableDirectionalWallJump = true;
    
    [Tooltip("Enable neutral wall jump. If TRUE, jumping with no directional input\n" +
             "performs a straight-up wall jump instead of pushing away.")]
    public bool enableNeutralWallJump = false;
    
    [Tooltip("Vertical force multiplier for neutral wall jumps (relative to normal wall jump).")]
    [Range(-999999f, 999999f)]
    public float neutralWallJumpVerticalMultiplier = 1.2f;
    
    [Tooltip("Horizontal force multiplier for neutral wall jumps (usually lower than normal).")]
    [Range(-999999f, 999999f)]
    public float neutralWallJumpHorizontalMultiplier = 0.3f;
    
    [Tooltip("Maximum angle offset from straight-away for directional wall jumps.\n" +
             "45 = can wall jump at 45° upward, 0 = always straight away.")]
    [Range(-999999f, 999999f)]
    public float directionalWallJumpMaxAngle = 45f;
    
    // -------------------------------------------------------------------------
    // INPUT PROCESSING TIMING
    // -------------------------------------------------------------------------
    // Choose when input processing occurs - immediately on input event,
    // or each frame. Frame-based allows runtime setting changes.
    // -------------------------------------------------------------------------
    
    [Tooltip("When to apply input processing (deadzone, normalization, etc).\n" +
             "ON_INPUT_EVENT: Process immediately when input is received (lower latency).\n" +
             "PER_FRAME: Process each frame (allows runtime setting changes).\n" +
             "BOTH: Process on input AND validate each frame.")]
    public Fused_InputProcessingTime inputProcessingTime = Fused_InputProcessingTime.PerFrame;
    
    // -------------------------------------------------------------------------
    // INPUT SMOOTHING (OPTIONAL)
    // -------------------------------------------------------------------------
    // Smoothing can reduce jittery input but adds latency.
    // Generally not recommended for precise platformers.
    // -------------------------------------------------------------------------
    
    [Tooltip("Enable input smoothing. Reduces jitter but adds latency.\n" +
             "NOT recommended for Celeste-style precision platformers.")]
    public bool enableInputSmoothing = false;
    
    [Tooltip("Smoothing factor. 0 = no smoothing, 1 = infinite smoothing (stuck).\n" +
             "Typical values: 0.1 to 0.3 for subtle smoothing.")]
    [Range(-999999f, 999999f)]
    public float inputSmoothingFactor = 0.15f;
    
    [Tooltip("Smoothing mode.\n" +
             "LERP: Linear interpolation toward target.\n" +
             "EXPONENTIAL: Exponential smoothing (more responsive to large changes).\n" +
             "MOVING_AVERAGE: Average of last N frames.")]
    public Fused_InputSmoothingMode inputSmoothingMode = Fused_InputSmoothingMode.Exponential;
    
    [Tooltip("Number of frames to average for MOVING_AVERAGE mode.")]
    [Range(1, 30)]
    public int movingAverageFrames = 5;
    
    #endregion
}

// ============================================================================
// CONTROLLER INPUT ENUMERATIONS
// ============================================================================
// These enums define the various modes and options for controller input
// processing. They are placed at the end of the file with other enums.
// ============================================================================

#region CONTROLLER INPUT ENUMERATIONS

/// <summary>
/// Deadzone shape type for analog stick processing.
/// Affects how easily diagonal vs cardinal directions can be input.
/// </summary>
public enum Fused_DeadzoneType
{
    /// <summary>Square deadzone. Simple but diagonals are harder to reach.</summary>
    Square,
    
    /// <summary>Circular deadzone. Equal difficulty for all directions.</summary>
    Circular,
    
    /// <summary>
    /// Scaled radial deadzone. Circular deadzone that rescales output
    /// so the usable range starts at 0 instead of jumping from deadzone to partial.
    /// RECOMMENDED for best feel.
    /// </summary>
    ScaledRadial
}

/// <summary>
/// Input mode determining how analog stick magnitude affects movement.
/// </summary>
public enum Fused_InputMode
{
    /// <summary>Any input past deadzone = full speed. Celeste-style, most responsive.</summary>
    Digital,
    
    /// <summary>Partial stick = partial speed. Traditional analog control.</summary>
    Analog,
    
    /// <summary>Analog up to threshold, then snaps to full. Best of both worlds.</summary>
    Hybrid
}

/// <summary>
/// Number of directions for direction snapping.
/// </summary>
public enum Fused_DirectionSnapCount
{
    /// <summary>4 directions: Up, Down, Left, Right only.</summary>
    FourWay = 4,
    
    /// <summary>8 directions: Cardinals + Diagonals. RECOMMENDED for Celeste-style.</summary>
    EightWay = 8,
    
    /// <summary>16 directions: Finer control, less snapping.</summary>
    SixteenWay = 16
}

/// <summary>
/// How direction snap zones are sized.
/// </summary>
public enum Fused_DirectionZoneMode
{
    /// <summary>All directions get equal-sized zones (45° each for 8-way).</summary>
    Equal,
    
    /// <summary>Cardinal directions (Up/Down/Left/Right) get larger zones.</summary>
    CardinalBiased,
    
    /// <summary>Diagonal directions get larger zones.</summary>
    DiagonalBiased
}

/// <summary>
/// When input processing is applied.
/// </summary>
public enum Fused_InputProcessingTime
{
    /// <summary>Process immediately when input callback fires. Lowest latency.</summary>
    OnInputEvent,
    
    /// <summary>Process each frame in Update. Allows runtime setting changes.</summary>
    PerFrame,
    
    /// <summary>Process on both input event AND each frame. Most flexible but redundant.</summary>
    Both
}

/// <summary>
/// Input smoothing algorithm.
/// </summary>
public enum Fused_InputSmoothingMode
{
    /// <summary>Linear interpolation. Simple, consistent smoothing.</summary>
    Lerp,
    
    /// <summary>Exponential smoothing. More responsive to sudden changes.</summary>
    Exponential,
    
    /// <summary>Moving average of last N frames. Stable but adds latency.</summary>
    MovingAverage
}

#endregion
