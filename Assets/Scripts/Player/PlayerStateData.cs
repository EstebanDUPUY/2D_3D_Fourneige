using UnityEngine;

[CreateAssetMenu(fileName = "New State Data", menuName = "Player/State Data")]
public class PlayerStateData : ScriptableObject
{
    #region VARIABLES

    [Header("Basic Movement")]
    public string stateName;
    public float moveSpeed;
    public float acceleration;
    public float startingSpeedBoost;
    public float deceleration;

    [Header("Physics")]
    public float gravity;
    public float weight;
    public float groundFriction;
    public float wallFriction;
    public float wallSlideFriction;

    [Header("Jump")]
    public float jumpForce;
    public float jumpHoldGravityMultiplier;
    public float jumpReleaseGravityMultiplier;
    public float maxJumpHoldTime;

    [Header("Coyote Time & Buffering")]
    public float coyoteTimeDuration;
    public float jumpBufferTime;

    [Header("Coyote Time & Buffering")]
    public bool hasBufferedJump;
    public bool hasDash;
    public bool hasAirDash;
    #endregion
}
