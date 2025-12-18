using UnityEngine;

[CreateAssetMenu(fileName = "PlayerSettings", menuName = "Player Settings")]
public class PlayerSettings : ScriptableObject
{
    [Header("Physics")]
    public float gravityScale = 3f;
    public float maxFallSpeed = 20f;

    [Header("Movement")]
    public bool enableMovement = true;
    public float moveSpeed = 8f;
    public float acceleration = 80f;
    //public float deceleration = 60f;
    //public float friction = 60f;

    [Header("Jump")]
    public bool enableJump = true;
    public float jumpForce = 12f;
    public float coyoteTime = 0.1f;

    [Header("Double Jump")]
    public bool enableDoubleJump = true;
    public float doubleJumpMultiplier = 0.85f;

    [Header("Wall Slide")]
    public bool enableWallSlide = true;
    public float wallSlideSpeed = 2f;
    public float wallSlideFriction = 0.1f;
    public float timeBetweenWallSlide = 0.5f;

    [Header("Wall Jump")]
    public bool enableWallJump = true;
    public float wallJumpForce = 14f;
    public float wallJumpPushForce = 10f;
    public float wallCoyoteTime = 0.15f;

    [Header("Dash")]
    public bool enableDash = true;
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 0.5f;

    [Header("Air Dash")]
    public bool enableAirDash = true;
    public float airDashSpeed = 18f;
    public float airDashDuration = 0.2f;
    public float airDashDrag = 0.85f;

    [Header("Sprite Flip")]
    public bool enableFlip = true;
    public bool useScaleFlip = true;
    public float flipSpeed = 15f;

    [Header("Detection")]
    public LayerMask groundLayer;
    public LayerMask wallLayer;
    public Vector2 groundCheckSize = new Vector2(0.8f, 0.1f);
    public Vector2 groundCheckOffset = new Vector2(0f, -0.5f);
    public float wallCheckDistance = 0.6f;
}
