using UnityEngine;

/// <summary>
/// Fully modular ScriptableObject for player state configuration.
/// Every movement feature can be toggled on/off and fine-tuned independently.
/// 
/// Design Philosophy:
/// - No hardcoded "Fire" or "Ice" exclusive features
/// - Every state is defined purely by its parameter values and toggles
/// - Maximum flexibility for iteration and experimentation
/// 
/// Usage: Create assets via Right-click > Create > Player > State Data
/// Configure each state's capabilities by enabling/disabling features and tuning values.
/// </summary>
[CreateAssetMenu(fileName = "_New State Data", menuName = "_Player/State Data")]
public class _PlayerStateData : ScriptableObject
{
    #region VARIABLES

    // ==================== IDENTIFICATION ====================
    [Header("Identification")]

    /// <summary>
    /// State name for debugging and identification.
    /// Example: "Fire", "Ice", "Water", "Custom State 1"
    /// </summary>
    public string stateName;

    /// <summary>
    /// Visual feedback color for this state.
    /// Used to tint the sprite when this state is active.
    /// </summary>
    public Color stateColor = Color.white;

    // ==================== MASTER CONTROLS ====================
    [Header("Master Controls")]

    /// <summary>
    /// Master toggle - if FALSE, completely disables all player movement.
    /// Useful for cutscenes, dialogue, or debug purposes.
    /// Overrides all other movement settings.
    /// </summary>
    public bool canMove = true;

    /// <summary>
    /// If FALSE, state switching is disabled while this state is active.
    /// Useful for "locked" states or challenge modes.
    /// </summary>
    public bool canSwitchFromThisState = true;

    /// <summary>
    /// Cooldown duration (in seconds) after switching to this state before switching again.
    /// Prevents rapid state spam. Set to 0 for instant switching.
    /// Typical range: 0-2 seconds
    /// </summary>
    public float stateSwitchCooldown = 0.5f;

    // ==================== BASIC MOVEMENT ====================
    [Header("Basic Movement")]

    /// <summary>
    /// If FALSE, disables horizontal ground movement (but allows other actions).
    /// Useful for "frozen" or "stunned" states.
    /// </summary>
    public bool canMoveOnGround = true;

    /// <summary>
    /// Maximum horizontal movement speed in units per second.
    /// Higher values = faster character.
    /// Typical range: 5-25 for platformers
    /// </summary>
    public float moveSpeed = 15f;

    /// <summary>
    /// How quickly the player accelerates to target speed.
    /// Higher values = more responsive, snappier movement.
    /// Lower values = more sliding, momentum-based feel.
    /// Typical range: 50-200
    /// </summary>
    public float acceleration = 100f;

    /// <summary>
    /// Multiplier applied to the first frame of movement for instant responsiveness.
    /// Creates a "pop" when starting to move from standing still.
    /// Typical range: 1.5-3.0 (1.0 = no boost)
    /// </summary>
    public float startingSpeedBoost = 2f;

    /// <summary>
    /// How quickly the player slows down when no input is given.
    /// Higher values = stops faster (sneakers vs ice skating).
    /// Typical range: 50-300
    /// </summary>
    public float deceleration = 150f;

    // ==================== AIR CONTROL ====================
    [Header("Air Control")]

    /// <summary>
    /// If TRUE, player can move horizontally while airborne.
    /// If FALSE, player maintains horizontal velocity from takeoff.
    /// </summary>
    public bool hasAirControl = true;

    /// <summary>
    /// Percentage of ground control available in air (0-1).
    /// 1.0 = full control, 0.5 = half control, 0.0 = no control
    /// Only applies if hasAirControl is TRUE.
    /// Typical range: 0.5-1.0
    /// </summary>
    [Range(0f, 1f)]
    public float airControlMultiplier = 0.8f;

    // ==================== PHYSICS ====================
    [Header("Physics")]

    /// <summary>
    /// Additional gravity force applied to the character (on top of Unity's Physics gravity).
    /// Higher values = falls faster, feels heavier.
    /// Typical range: 10-50
    /// </summary>
    public float gravity = 30f;

    /// <summary>
    /// Rigidbody mass value. Affects how physics forces interact with the character.
    /// Higher = heavier, more momentum, harder to push.
    /// Lower = lighter, more floaty.
    /// Typical range: 1-5
    /// </summary>
    public float weight = 1f;

    /// <summary>
    /// How much friction is applied when on the ground and not moving.
    /// Higher values = stops faster when releasing movement input.
    /// Range: 0-1 (0 = no friction, 1 = instant stop)
    /// </summary>
    [Range(0f, 1f)]
    public float groundFriction = 0.5f;

    // ==================== JUMP SYSTEM ====================
    [Header("Jump System")]

    /// <summary>
    /// Master toggle for jumping. If FALSE, this state cannot jump at all.
    /// Useful for "heavy" or "grounded" states.
    /// </summary>
    public bool canJump = true;

    /// <summary>
    /// Initial upward velocity applied when jumping (Impulse force).
    /// Higher = jumps higher.
    /// Typical range: 8-20
    /// </summary>
    public float jumpForce = 15f;

    /// <summary>
    /// If TRUE, enables variable-height jumping (hold jump = higher).
    /// If FALSE, jump height is always the same regardless of hold duration.
    /// Celeste/Hollow Knight style variable jumping.
    /// </summary>
    public bool hasVariableJump = true;

    /// <summary>
    /// Gravity multiplier while holding jump button and ascending.
    /// Lower values = floatier, allows holding for higher jumps.
    /// Only applies if hasVariableJump is TRUE.
    /// Range: 0.3-0.8 (lower = more float)
    /// </summary>
    [Range(0.1f, 1f)]
    public float jumpHoldGravityMultiplier = 0.5f;

    /// <summary>
    /// Gravity multiplier when falling or after releasing jump button early.
    /// Higher values = snappier fall, more responsive jump canceling.
    /// Range: 1.5-3.0 (higher = faster fall)
    /// </summary>
    [Range(1f, 4f)]
    public float jumpReleaseGravityMultiplier = 2.0f;

    /// <summary>
    /// Maximum duration (in seconds) the player can hold jump to gain extra height.
    /// Longer = more control over jump height.
    /// Only applies if hasVariableJump is TRUE.
    /// Typical range: 0.1-0.5 seconds
    /// </summary>
    public float maxJumpHoldTime = 0.3f;

    // ==================== COYOTE TIME ====================
    [Header("Coyote Time")]

    /// <summary>
    /// If TRUE, enables coyote time (grace period after leaving ground).
    /// Named after Wile E. Coyote running off cliffs.
    /// Makes platforming more forgiving.
    /// </summary>
    public bool hasCoyoteTime = true;

    /// <summary>
    /// Grace period (in seconds) after leaving a platform where jump is still allowed.
    /// Higher = more forgiving.
    /// Only applies if hasCoyoteTime is TRUE.
    /// Typical range: 0.05-0.3 seconds
    /// </summary>
    public float coyoteTimeDuration = 0.15f;

    // ==================== JUMP BUFFERING ====================
    [Header("Jump Buffering")]

    /// <summary>
    /// If TRUE, enables jump buffering (remember input before landing).
    /// Allows pressing jump slightly before hitting ground and still jumping.
    /// Makes gameplay feel more responsive.
    /// </summary>
    public bool hasJumpBuffer = true;

    /// <summary>
    /// Time window (in seconds) before landing where jump input is remembered.
    /// Only applies if hasJumpBuffer is TRUE.
    /// Typical range: 0.1-0.3 seconds
    /// </summary>
    public float jumpBufferTime = 0.2f;

    // ==================== DOUBLE JUMP ====================
    [Header("Double Jump")]

    /// <summary>
    /// If TRUE, enables double jump (jump again while airborne).
    /// Classic platformer mechanic.
    /// </summary>
    public bool hasDoubleJump = false;

    /// <summary>
    /// Maximum number of times player can jump in the air.
    /// 1 = double jump, 2 = triple jump, etc.
    /// Only applies if hasDoubleJump is TRUE.
    /// Typical range: 1-3
    /// </summary>
    public int maxAirJumps = 1;

    /// <summary>
    /// Force multiplier for air jumps compared to ground jump.
    /// 1.0 = same height, 0.8 = 80% height, 1.2 = 120% height
    /// Only applies if hasDoubleJump is TRUE.
    /// Typical range: 0.7-1.0
    /// </summary>
    [Range(0.5f, 1.5f)]
    public float airJumpForceMultiplier = 0.9f;

    // ==================== WALL SLIDE ====================
    [Header("Wall Slide")]

    /// <summary>
    /// If TRUE, enables wall sliding (reduced fall speed when touching wall).
    /// Essential for wall jump mechanics.
    /// </summary>
    public bool hasWallSlide = true;

    /// <summary>
    /// If TRUE, player must hold toward wall to slide.
    /// If FALSE, sliding happens automatically when touching wall.
    /// Only applies if hasWallSlide is TRUE.
    /// </summary>
    public bool wallSlideRequiresInput = false;

    /// <summary>
    /// Maximum fall speed when sliding down a wall (negative value).
    /// Lower magnitude = slower slide (more friction).
    /// Example: -2 = slow slide, -8 = fast slide
    /// Only applies if hasWallSlide is TRUE.
    /// Typical range: -2 to -8
    /// </summary>
    public float wallSlideFriction = -3f;

    // ==================== WALL JUMP ====================
    [Header("Wall Jump")]

    /// <summary>
    /// If TRUE, enables wall jumping (jump away from wall while sliding).
    /// Typically paired with hasWallSlide = TRUE.
    /// </summary>
    public bool hasWallJump = true;

    /// <summary>
    /// Upward force applied during wall jump.
    /// If set to 0, uses regular jumpForce instead.
    /// Only applies if hasWallJump is TRUE.
    /// Typical range: 10-20 (0 = use jumpForce)
    /// </summary>
    public float wallJumpForce = 0f;

    /// <summary>
    /// Horizontal push multiplier when wall jumping.
    /// How far to push away from the wall (relative to jump force).
    /// 0.7 = 70% of jump force applied horizontally
    /// Only applies if hasWallJump is TRUE.
    /// Typical range: 0.5-1.0
    /// </summary>
    [Range(0.3f, 1.5f)]
    public float wallJumpHorizontalMultiplier = 0.7f;

    // ==================== DASH SYSTEM ====================
    [Header("Dash System")]

    /// <summary>
    /// If TRUE, enables ground dash ability.
    /// Fast directional movement with optional invincibility.
    /// </summary>
    public bool hasDash = false;

    /// <summary>
    /// If TRUE, enables dashing while airborne.
    /// Requires hasDash = TRUE.
    /// </summary>
    public bool hasAirDash = false;

    /// <summary>
    /// Cooldown (in seconds) after dashing before dashing again.
    /// Set to 0 for no cooldown (resets on landing).
    /// Only applies if hasDash is TRUE.
    /// Typical range: 0-2 seconds
    /// </summary>
    public float dashCooldown = 0f;

    /// <summary>
    /// If TRUE, dash travels fixed distance.
    /// If FALSE, dash travels for fixed duration at constant speed.
    /// Only applies if hasDash is TRUE.
    /// </summary>
    public bool dashIsFixedDistance = false;

    /// <summary>
    /// Distance traveled during dash (if dashIsFixedDistance = TRUE).
    /// Only applies if hasDash and dashIsFixedDistance are TRUE.
    /// Typical range: 3-8 units
    /// </summary>
    public float dashDistance = 5f;

    /// <summary>
    /// Duration of dash in seconds.
    /// If fixed distance: time to travel dashDistance
    /// If fixed duration: how long dash lasts
    /// Only applies if hasDash is TRUE.
    /// Typical range: 0.2-0.5 seconds
    /// </summary>
    public float dashDuration = 0.3f;

    /// <summary>
    /// Speed during dash (if dashIsFixedDistance = FALSE).
    /// Only applies if hasDash is TRUE and dashIsFixedDistance is FALSE.
    /// Typical range: 15-30 units/second
    /// </summary>
    public float dashSpeed = 20f;

    /// <summary>
    /// If TRUE, player is invincible during dash.
    /// Visual feedback provided via sprite flickering.
    /// Only applies if hasDash is TRUE.
    /// </summary>
    public bool dashHasInvincibility = false;

    /// <summary>
    /// Duration of invincibility frames during dash (in seconds).
    /// Only applies if hasDash and dashHasInvincibility are TRUE.
    /// Typical range: 0.2-0.5 seconds
    /// </summary>
    public float dashInvincibilityDuration = 0.3f;

    // ==================== BUNNY HOP ====================
    [Header("Bunny Hop")]

    /// <summary>
    /// If TRUE, enables bunny hopping (speed bonus for landing and immediately jumping).
    /// Rewards skilled timing and creates a "flow" feel.
    /// Popular in games like Quake, CS:GO, and Celeste.
    /// </summary>
    public bool hasBunnyHop = false;

    /// <summary>
    /// Speed bonus multiplier gained per successful bunny hop.
    /// Stacks up to bunnyHopMaxSpeed.
    /// Example: 0.15 = 15% speed increase per hop
    /// Only applies if hasBunnyHop is TRUE.
    /// Typical range: 0.1-0.3
    /// </summary>
    [Range(0f, 0.5f)]
    public float bunnyHopSpeedBonus = 0.15f;

    /// <summary>
    /// How quickly bunny hop bonus decays when not chaining hops (per second).
    /// Higher = faster decay, more punishing.
    /// Only applies if hasBunnyHop is TRUE.
    /// Typical range: 0.5-2.0
    /// </summary>
    [Range(0.1f, 3f)]
    public float bunnyHopDecayRate = 1f;

    /// <summary>
    /// Maximum speed achievable through bunny hopping (as multiplier of moveSpeed).
    /// Example: 1.5 = 150% of normal speed
    /// Only applies if hasBunnyHop is TRUE.
    /// Typical range: 1.3-2.0
    /// </summary>
    [Range(1f, 3f)]
    public float bunnyHopMaxSpeed = 1.5f;

    /// <summary>
    /// Time window (in seconds) after landing to jump and maintain/gain bunny hop bonus.
    /// Tighter timing = more skill required.
    /// Only applies if hasBunnyHop is TRUE.
    /// Typical range: 0.05-0.2 seconds
    /// </summary>
    public float bunnyHopTimingWindow = 0.1f;

    #endregion
}