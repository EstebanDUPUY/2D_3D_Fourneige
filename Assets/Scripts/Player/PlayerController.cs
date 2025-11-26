using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerController : MonoBehaviour
{

    //

    #region Variables
    #region REFERENCES
    // Components
    private Rigidbody rb;
    private SpriteRenderer spriteRenderer;

    // State Data
    public PlayerStateData fireStateData;
    public PlayerStateData iceStateData;
    private PlayerStateData currentStateData;

    // State enum
    public enum States { Fire, Ice }
    public States currentState;
    #endregion

    #region MOVEMENT VARIABLES
    private Vector2 moveInput;
    private float currentSpeed;
    private bool isFacingRight = true;
    private bool isFlipping = false;
    #endregion

    #region GROUND & WALL DETECTION
    [Header("Detection Settings")]
    public LayerMask groundLayer;
    public LayerMask wallLayer;
    public Vector3 groundCheckSize = new Vector3(0.9f, 0.1f, 0.9f);
    public Vector3 wallCheckSize = new Vector3(0.1f, 0.9f, 0.9f);
    public float groundRaycastDistance = 0.2f;
    public float wallRaycastDistance = 0.2f;

    private bool isGrounded;
    private bool isAboutToLand;
    private bool isTouchingWall;
    private bool isWallAhead;
    private int wallDirection; // -1 left, 1 right
    #endregion

    #region JUMP VARIABLES
    private bool isJumping;
    private bool jumpBuffered;
    private float jumpBufferTimer;
    private float coyoteTimeTimer;
    private float jumpHoldTimer;
    private bool isHoldingJump;
    #endregion

    #region DASH VARIABLES
    [Header("Dash Settings")]
    public bool dashHasInvincibility = true;
    public float dashInvincibilityDuration = 0.3f;
    public bool dashIsFixedDistance = false;
    public float dashDistance = 5f;
    public float dashDuration = 0.3f;
    public float dashSpeed = 20f;

    private bool isDashing;
    private bool isInvincible;
    private bool canDash = true;
    private Vector3 dashDirection;
    #endregion

    #region WALL MECHANICS
    [Header("Wall Settings")]
    public bool wallStickRequiresInput = false;

    private bool isWallSliding;
    private bool canWallJump;

    // More coming ...
    #endregion
    #endregion

    //

    #region START, UPDATE, ETC...
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        currentState = States.Fire;
        ApplyStateData(fireStateData);
    }

    private void Update()
    {
        CheckGroundStatus();
        CheckWallStatus();
        CheckGroundStatus();
        CheckWallStatus();
        UpdateTimers();
        TryJump();

        if (isGrounded && !canDash)
        {
            canDash = true;
        }
    }

    private void FixedUpdate()
    {
        if (!isDashing)
        {
            ApplyMovement();
            ApplyWallSlide();
            ApplyGravity();
        }
    }
    #endregion

    //

    #region JUMP
    private void UpdateTimers()
    {
        // Coyote time
        if (isGrounded || isAboutToLand)
        {
            coyoteTimeTimer = currentStateData.coyoteTimeDuration;
        }
        else
        {
            coyoteTimeTimer -= Time.deltaTime;
        }

        // Jump buffer
        if (jumpBufferTimer > 0)
        {
            jumpBufferTimer -= Time.deltaTime;
        }
        else
        {
            jumpBuffered = false;
        }

        // Jump hold timer
        if (isHoldingJump)
        {
            jumpHoldTimer += Time.deltaTime;

            if (jumpHoldTimer >= currentStateData.maxJumpHoldTime)
            {
                isHoldingJump = false;
            }
        }
    }

    private void TryJump()
    {
        bool canGroundJump = coyoteTimeTimer > 0 && !isJumping;

        if (jumpBuffered && canGroundJump)
        {
            Jump();
        }
        else if (jumpBuffered && canWallJump)
        {
            WallJump();
        }
    }

    private void Jump()
    {
        // Reset vertical velocity
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

        // Apply jump force
        rb.AddForce(Vector3.up * currentStateData.jumpForce, ForceMode.Impulse);

        // Reset states
        isJumping = true;
        isHoldingJump = true;
        jumpHoldTimer = 0f;
        jumpBuffered = false;
        coyoteTimeTimer = 0f;
    }
    private void WallJump()
    {
        // Reset velocity
        rb.linearVelocity = new Vector3(0, 0, rb.linearVelocity.z);

        // Apply jump force upward and away from wall
        Vector3 wallJumpForce = new Vector3(-wallDirection * currentStateData.jumpForce * 0.7f, currentStateData.jumpForce, 0);
        rb.AddForce(wallJumpForce, ForceMode.Impulse);

        // Reset states
        isJumping = true;
        isHoldingJump = true;
        jumpHoldTimer = 0f;
        jumpBuffered = false;
        canWallJump = false;
        isWallSliding = false;
    }
    #endregion

    //

    #region APPLY THINGS
    private void ApplyStateData(PlayerStateData data)
    {
        currentStateData = data;
        rb.mass = data.weight;

        // Visual feedback for state (optional - change sprite color?)
        spriteRenderer.color = currentState == States.Fire ? Color.red : Color.cyan;
    }
    private void ApplyMovement()
    {
        float targetSpeed = moveInput.x * currentStateData.moveSpeed;
        float speedDifference = targetSpeed - rb.linearVelocity.x;

        // Choose acceleration or deceleration
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? currentStateData.acceleration : currentStateData.deceleration;

        // Apply starting boost if just started moving
        if (Mathf.Abs(rb.linearVelocity.x) < 0.01f && Mathf.Abs(targetSpeed) > 0.01f)
        {
            speedDifference *= currentStateData.startingSpeedBoost;
        }

        float movement = speedDifference * accelRate;

        rb.AddForce(Vector3.right * movement, ForceMode.Force);

        // Apply ground friction
        if (isGrounded && Mathf.Abs(moveInput.x) < 0.01f)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x * (1 - currentStateData.groundFriction * Time.fixedDeltaTime), rb.linearVelocity.y, rb.linearVelocity.z);
        }

        // Handle sprite flipping
        if (moveInput.x > 0 && !isFacingRight)
        {
            StartFlip(true);
        }
        else if (moveInput.x < 0 && isFacingRight)
        {
            StartFlip(false);
        }
    }
    private void ApplyGravity()
    {
        float gravityMultiplier = 1f;

        // Reduce gravity while holding jump (Celeste-style)
        if (isHoldingJump && rb.linearVelocity.y > 0)
        {
            gravityMultiplier = currentStateData.jumpHoldGravityMultiplier;
        }
        // Increase gravity when falling or released jump early
        else if (rb.linearVelocity.y < 0 || (!isHoldingJump && rb.linearVelocity.y > 0))
        {
            gravityMultiplier = currentStateData.jumpReleaseGravityMultiplier;
        }

        rb.AddForce(Vector3.down * currentStateData.gravity * gravityMultiplier * rb.mass, ForceMode.Force);

        // Reset jumping flag when grounded
        if (isGrounded)
        {
            isJumping = false;
        }
    }
    private void ApplyWallSlide()
    {
        bool shouldWallSlide = isTouchingWall && !isGrounded && rb.linearVelocity.y < 0;

        // If wallStickRequiresInput is true, also check if pushing toward wall
        if (wallStickRequiresInput)
        {
            bool pushingTowardWall = (wallDirection > 0 && moveInput.x > 0) || (wallDirection < 0 && moveInput.x < 0);
            shouldWallSlide = shouldWallSlide && pushingTowardWall;
        }

        if (shouldWallSlide)
        {
            isWallSliding = true;
            canWallJump = true;

            // Apply wall slide friction
            float slideSpeed = -currentStateData.wallSlideFriction;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, slideSpeed), rb.linearVelocity.z);
        }
        else
        {
            isWallSliding = false;
            if (isGrounded)
            {
                canWallJump = false;
            }
        }
    }
    #endregion

    //

    #region INPUT
    public void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
    }
    public void OnSwitchState(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
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
    }
    public void OnJump(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            jumpBuffered = true;
            jumpBufferTimer = currentStateData.jumpBufferTime;
        }

        if (ctx.canceled)
        {
            isHoldingJump = false;
        }
    }
    public void OnDash(InputAction.CallbackContext ctx)
    {
        bool canAirDash = !isGrounded && currentStateData.hasAirDash;

        if (ctx.performed && currentStateData.hasDash && canDash && !isDashing)
        {
            // Determine dash direction from movement input
            Vector3 dashDir = Vector3.zero;

            if (moveInput.magnitude > 0.1f)
            {
                dashDir = new Vector3(moveInput.x, moveInput.y, 0).normalized;
            }
            else
            {
                // Default to facing direction if no input
                dashDir = isFacingRight ? Vector3.right : Vector3.left;
            }

            StartCoroutine(PerformDash(dashDir));
        }
    }
    #endregion

    //

    #region DASH
    private IEnumerator PerformDash(Vector3 direction)
    {
        isDashing = true;
        canDash = false;
        dashDirection = direction;

        // Apply invincibility
        if (dashHasInvincibility)
        {
            StartCoroutine(InvincibilityFrames());
        }

        float elapsed = 0f;

        if (dashIsFixedDistance)
        {
            // Fixed distance dash
            Vector3 startPos = transform.position;
            Vector3 targetPos = startPos + dashDirection * dashDistance;

            while (elapsed < dashDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / dashDuration;
                transform.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }
        }
        else
        {
            // Fixed duration dash
            while (elapsed < dashDuration)
            {
                rb.linearVelocity = dashDirection * dashSpeed;
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        isDashing = false;

        // Reset dash availability when grounded
        if (isGrounded)
        {
            canDash = true;
        }
    }
    private IEnumerator InvincibilityFrames()
    {
        isInvincible = true;

        // Visual feedback (optional: flicker sprite)
        float elapsed = 0f;
        while (elapsed < dashInvincibilityDuration)
        {
            spriteRenderer.enabled = !spriteRenderer.enabled;
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }

        spriteRenderer.enabled = true;
        isInvincible = false;
    }
    #endregion

    //

    #region FLIPPING
    private void StartFlip(bool flipToRight)
    {
        if (!isFlipping)
        {
            StartCoroutine(FlipSprite(flipToRight));
        }
    }

    private IEnumerator FlipSprite(bool flipToRight)
    {
        isFlipping = true;

        float startRotation = transform.eulerAngles.y;
        float targetRotation = flipToRight ? 0f : 180f;
        float elapsed = 0f;
        float flipDuration = 0.15f;

        while (elapsed < flipDuration)
        {
            elapsed += Time.deltaTime;
            float yRotation = Mathf.Lerp(startRotation, targetRotation, elapsed / flipDuration);
            transform.rotation = Quaternion.Euler(0, yRotation, 0);
            yield return null;
        }

        transform.rotation = Quaternion.Euler(0, targetRotation, 0);
        isFacingRight = flipToRight;
        isFlipping = false;
    }
    #endregion

    //

    #region GROUND DETECTION
    private void CheckGroundStatus()
    {
        Vector3 boxCenter = transform.position - new Vector3(0, GetComponent<Collider>().bounds.extents.y, 0);

        // OverlapBox for solid contact
        isGrounded = Physics.CheckBox(boxCenter, groundCheckSize / 2, Quaternion.identity, groundLayer);

        // Raycast for prediction
        RaycastHit hit;
        Vector3 rayStart = boxCenter + Vector3.up * 0.1f;
        isAboutToLand = Physics.Raycast(rayStart, Vector3.down, out hit, groundRaycastDistance, groundLayer);
    }
    #endregion

    //

    #region WALL DETECTION

    private void CheckWallStatus()
    {
        Vector3 boxCenter = transform.position;

        // Check right side
        bool rightWall = Physics.CheckBox(boxCenter + Vector3.right * 0.5f, wallCheckSize / 2, Quaternion.identity, wallLayer);

        // Check left side
        bool leftWall = Physics.CheckBox(boxCenter + Vector3.left * 0.5f, wallCheckSize / 2, Quaternion.identity, wallLayer);

        isTouchingWall = rightWall || leftWall;
        wallDirection = rightWall ? 1 : (leftWall ? -1 : 0);

        // Raycast for wall ahead
        Vector3 rayDirection = isFacingRight ? Vector3.right : Vector3.left;
        isWallAhead = Physics.Raycast(boxCenter, rayDirection, wallRaycastDistance, wallLayer);
    }

    #endregion

    //

    #region GIZMOS 
    /*
    private void OnDrawGizmosSelected()
    {
        if (rb == null) return;

        Vector3 boxCenter = transform.position - new Vector3(0, GetComponent<Collider>().bounds.extents.y, 0);

        // Ground detection
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireCube(boxCenter, groundCheckSize);
        Gizmos.DrawLine(boxCenter + Vector3.up * 0.1f, boxCenter + Vector3.up * 0.1f + Vector3.down * groundRaycastDistance);

        // Wall detection
        Gizmos.color = isTouchingWall ? Color.blue : Color.yellow;
        Gizmos.DrawWireCube(transform.position + Vector3.right * 0.5f, wallCheckSize);
        Gizmos.DrawWireCube(transform.position + Vector3.left * 0.5f, wallCheckSize);
    }
    */

    /*
    private void OnDrawGizmosSelected()
    {
        // Simple test - just draw a sphere at player position
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    } 
    */

    private void OnDrawGizmosSelected()
    {
        // Get the collider (cache it if you want)
        Collider col = GetComponent<Collider>();
        if (col == null) return;

        // Calculate ground check position (at the bottom of the collider)
        Vector3 groundCheckCenter = transform.position - new Vector3(0, col.bounds.extents.y, 0);

        // Ground OverlapBox
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireCube(groundCheckCenter, groundCheckSize);

        // Ground Raycast
        Gizmos.color = isAboutToLand ? Color.yellow : Color.gray;
        Vector3 rayStart = groundCheckCenter + Vector3.up * 0.1f;
        Gizmos.DrawLine(rayStart, rayStart + Vector3.down * groundRaycastDistance);

        // Wall detection - Right side
        Gizmos.color = Color.blue;
        Vector3 rightWallCheck = transform.position + Vector3.right * 0.5f;
        Gizmos.DrawWireCube(rightWallCheck, wallCheckSize);

        // Wall detection - Left side
        Vector3 leftWallCheck = transform.position + Vector3.left * 0.5f;
        Gizmos.DrawWireCube(leftWallCheck, wallCheckSize);

        // Wall ahead raycast
        Gizmos.color = isWallAhead ? Color.cyan : Color.gray;
        Vector3 wallRayDirection = isFacingRight ? Vector3.right : Vector3.left;
        Gizmos.DrawLine(transform.position, transform.position + wallRayDirection * wallRaycastDistance);
    }
    #endregion

    //

}
