using UnityEngine;

[CreateAssetMenu(fileName = "PlayerSettingsModifier", menuName = "Player Settings Modifier")]
public class PlayerSettingsModifier : ScriptableObject
{
    [Header("Physics")]
    public float gravityScale = 1f;
    public float maxFallSpeed = 1f;

    [Header("Movement")]
    public bool enableMovement = true;
    public float moveSpeed = 1f;
    public float acceleration = 1f;

    [Header("Jump")]
    public bool enableJump = true;
    public float jumpForce = 1f;
    public float coyoteTime = 1f;

    [Header("Double Jump")]
    public bool enableDoubleJump = true;
    public float doubleJumpMultiplier = 1f;

    [Header("Wall Slide")]
    public bool enableWallSlide = true;
    public float wallSlideSpeed = 1f;
    public float wallSlideFriction = 1f;
    public float timeBetweenWallSlide = 1f;

    [Header("Wall Jump")]
    public bool enableWallJump = true;
    public float wallJumpForce = 1f;
    public float wallJumpPushForce = 1f;

    [Header("Dash")]
    public bool enableDash = true;
    public float dashSpeed = 1f;
    public float dashDuration = 1f;
    public float dashCooldown = 1f;

    [Header("Air Dash")]
    public bool enableAirDash = true;
    public float airDashSpeed = 1f;
    public float airDashDuration = 1f;
    public float airDashDrag = 1f;
}
