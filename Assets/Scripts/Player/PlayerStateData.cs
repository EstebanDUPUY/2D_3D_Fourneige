/*
using UnityEngine;

/// <summary>
/// ScriptableObject that stores all physics and movement parameters for a player state.
/// Create instances for different states (Fire/Ice) with different values to achieve
/// distinct gameplay feels without code duplication.
/// 
/// Usage: Create assets via Right-click > Create > Player > State Data
/// Then assign to PlayerController's fireStateData/iceStateData fields.
/// </summary>
[CreateAssetMenu(fileName = "New State Data", menuName = "Player/State Data")]
public class _PlayerStateData : ScriptableObject
{
    #region VARIABLES

    // ==================== BASIC MOVEMENT ====================
    [Header("Basic Movement")]

    /// <summary>
    /// Identifier for debugging purposes.
    /// Example: "Fire" or "Ice"
    /// </summary>
    public string stateName;

    /// <summary>
    /// Maximum horizontal movement speed in units per second.
    /// Higher values = faster character.
    /// Typical range: 5-15 for platformers
    /// </summary>
    public float moveSpeed;

    /// <summary>
    /// How quickly the player accelerates to target speed.
    /// Higher values = more responsive, snappier movement.
    /// Lower values = more sliding, momentum-based feel.
    /// Typical range: 50-200
    /// </summary>
    public float acceleration;

    /// <summary>
    /// Multiplier applied to the first frame of movement for instant responsiveness.
    /// Creates a "pop" when starting to move from standing still.
    /// Typical range: 1.5-3.0 (1.0 = no boost)
    /// </summary>
    public float startingSpeedBoost;

    /// <summary>
    /// How quickly the player slows down when no input is given.
    /// Higher values = stops faster (ice skating vs sneakers).
    /// Typical range: 100-300
    /// </summary>
    public float deceleration;

    // ==================== PHYSICS ====================
    [Header("Physics")]

    /// <summary>
    /// Additional gravity force applied to the character (on top of Unity's Physics gravity).
    /// Higher values = falls faster, feels heavier.
    /// Typical range: 10-50
    /// </summary>
    public float gravity;

    /// <summary>
    /// Rigidbody mass value. Affects how physics forces interact with the character.
    /// Higher = heavier, more momentum, harder to push.
    /// Lower = lighter, more floaty.
    /// Typical range: 1-5
    /// </summary>
    public float weight;

    /// <summary>
    /// How much friction is applied when on the ground and not moving.
    /// Higher values = stops faster when releasing movement input.
    /// Range: 0-1 (0 = no friction, 1 = instant stop)
    /// </summary>
    public float groundFriction;

    /// <summary>
    /// Friction applied when touching a wall (affects horizontal movement damping).
    /// Not directly used in current implementation but available for future features.
    /// Range: 0-1
    /// </summary>
    public float wallFriction;

    /// <summary>
    /// Maximum fall speed when sliding down a wall (negative value).
    /// Lower magnitude = slower slide (more friction).
    /// Example: -2 = slow slide, -8 = fast slide
    /// Typical range: -2 to -8
    /// </summary>
    public float wallSlideFriction;

    // ==================== JUMP ====================
    [Header("Jump")]

    /// <summary>
    /// Initial upward velocity applied when jumping (Impulse force).
    /// Higher = jumps higher.
    /// Typical range: 8-15
    /// </summary>
    public float jumpForce;

    /// <summary>
    /// Gravity multiplier while holding jump button and ascending (Celeste-style variable jump).
    /// Lower values = floatier, allows holding for higher jumps.
    /// Range: 0.3-0.7 (lower = more float)
    /// </summary>
    public float jumpHoldGravityMultiplier;

    /// <summary>
    /// Gravity multiplier when falling or after releasing jump button early.
    /// Higher values = snappier fall, more responsive jump canceling.
    /// Range: 1.5-3.0 (higher = faster fall)
    /// </summary>
    public float jumpReleaseGravityMultiplier;

    /// <summary>
    /// Maximum duration (in seconds) the player can hold jump to gain extra height.
    /// Longer = more control over jump height.
    /// Typical range: 0.2-0.5 seconds
    /// </summary>
    public float maxJumpHoldTime;

    // ==================== COYOTE TIME & BUFFERING ====================
    [Header("Coyote Time & Buffering")]

    /// <summary>
    /// Grace period (in seconds) after leaving a platform where jump is still allowed.
    /// Makes platforming more forgiving. Named after Wile E. Coyote running off cliffs.
    /// Higher = more forgiving (Ice state should have more).
    /// Typical range: 0.1-0.3 seconds
    /// </summary>
    public float coyoteTimeDuration;

    /// <summary>
    /// Time window (in seconds) before landing where jump input is remembered.
    /// Allows pressing jump slightly before hitting ground and still jumping.
    /// Makes gameplay feel more responsive.
    /// Typical range: 0.1-0.2 seconds
    /// </summary>
    public float jumpBufferTime;

    // ==================== FEATURE FLAGS ====================
    [Header("Feature Flags")]

    /// <summary>
    /// If true, this state uses buffered jump mechanics (hold for variable height).
    /// Ice state: TRUE (skill-based, hold for higher jumps)
    /// Fire state: FALSE (fast, instant jumps)
    /// </summary>
    public bool hasBufferedJump;

    /// <summary>
    /// If true, this state can perform a ground dash.
    /// Ice state: TRUE
    /// Fire state: FALSE
    /// </summary>
    public bool hasDash;

    /// <summary>
    /// If true, this state can perform a dash while airborne.
    /// Ice state: TRUE
    /// Fire state: FALSE
    /// </summary>
    public bool hasAirDash;

    #endregion
}
*/