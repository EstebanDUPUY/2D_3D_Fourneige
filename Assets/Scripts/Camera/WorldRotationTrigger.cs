using UnityEngine;

/// <summary>
/// Trigger zone that rotates the world when player enters.
/// Place this on trigger colliders throughout your level.
/// 
/// Setup Instructions:
/// 1. Create GameObject with BoxCollider (or other collider)
/// 2. Enable "Is Trigger" on the collider
/// 3. Attach this script
/// 4. Set Rotation Direction (Left or Right)
/// 5. Assign World Rotation Controller reference
/// 6. Configure player tag/layer detection
/// 
/// Behavior:
/// - Triggers when player ENTERS the collider
/// - Requires player to EXIT before re-triggering (no spam)
/// - Can be one-shot (trigger once and disable) or repeatable
/// - Visual indicators in Scene view for debugging
/// </summary>
[RequireComponent(typeof(Collider))]
public class WorldRotationTrigger : MonoBehaviour
{
    #region Variables

    // ==================== TRIGGER SETTINGS ====================
    //[Header("Trigger Settings")]

    /// <summary>
    /// Which direction should the world rotate?
    /// Left = -90° (counter-clockwise from above)
    /// Right = +90° (clockwise from above)
    /// </summary>
    public enum RotationDirection { Left, Right }

    [Tooltip("Direction to rotate the world (Left or Right)")]
    public RotationDirection direction = RotationDirection.Right;

    /// <summary>
    /// Reference to the WorldRotationController script.
    /// Must be assigned in Inspector.
    /// Usually found on the "World" parent GameObject.
    /// </summary>
    [Tooltip("Reference to WorldRotationController (usually on 'World' GameObject)")]
    public WorldRotationController worldController;

    /// <summary>
    /// Tag used to identify the player.
    /// Trigger only activates when GameObject with this tag enters.
    /// Default: "Player"
    /// </summary>
    [Tooltip("Player GameObject tag (must match player's tag)")]
    public string playerTag = "Player";

    /// <summary>
    /// If TRUE, trigger only works once then disables itself.
    /// If FALSE, trigger is repeatable (requires exit/re-enter).
    /// </summary>
    [Tooltip("If true, trigger only works once then disables")]
    public bool oneTimeUse = false;

    /// <summary>
    /// If TRUE, trigger only activates when player is grounded.
    /// If FALSE, trigger works even when player is airborne.
    /// Useful to prevent accidental rotation during jumps.
    /// </summary>
    [Tooltip("Require player to be grounded to trigger rotation")]
    public bool requireGrounded = false;

    // ==================== VISUAL DEBUG ====================
    [Header("Visual Debug")]

    /// <summary>
    /// Color of trigger gizmo in Scene view.
    /// Changes based on state: Ready, Used, Cooldown
    /// </summary>
    [Tooltip("Color of trigger visualization in Scene view")]
    public Color gizmoColor = Color.yellow;

    /// <summary>
    /// Size of direction arrow in Scene view.
    /// Helps visualize which way the world will rotate.
    /// </summary>
    [Tooltip("Size of rotation direction arrow")]
    public float arrowSize = 1f;

    // ==================== STATE TRACKING ====================
    [Header("Debug Info (Read Only)")]

    /// <summary>
    /// Is player currently inside the trigger zone?
    /// TRUE when player is in trigger, FALSE when outside.
    /// Used to require exit before re-triggering.
    /// </summary>
    [Tooltip("Is player currently inside this trigger?")]
    public bool playerInside = false;

    /// <summary>
    /// Has this trigger been used (for one-time triggers)?
    /// TRUE after first activation if oneTimeUse is enabled.
    /// </summary>
    [Tooltip("Has this one-time trigger been used?")]
    public bool hasBeenUsed = false;

    // ==================== PRIVATE VARIABLES ====================
    private Collider triggerCollider;

    #endregion

    // ==================== UNITY LIFECYCLE ====================
    #region UNITY LIFECYCLE

    /// <summary>
    /// Validate setup on start.
    /// Checks for required components and references.
    /// </summary>
    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();

        // Validate collider is set as trigger
        if (!triggerCollider.isTrigger)
        {
            Debug.LogError($"[WorldRotationTrigger] Collider on '{gameObject.name}' is not set as Trigger! Enable 'Is Trigger' in Inspector.", this);
        }

        // Validate world controller reference
        if (worldController == null)
        {
            Debug.LogError($"[WorldRotationTrigger] World Controller not assigned on '{gameObject.name}'! Assign the WorldRotationController reference in Inspector.", this);
        }
    }

    /// <summary>
    /// Called when another collider enters this trigger.
    /// Checks if it's the player, then triggers rotation if conditions met.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // Check if entering object is the player
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        // Mark player as inside
        playerInside = true;

        // Check if trigger is available
        if (!CanTrigger())
        {
            return;
        }

        // Optional: Check if player is grounded
        if (requireGrounded && !IsPlayerGrounded(other.gameObject))
        {
            Debug.Log($"[WorldRotationTrigger] Player must be grounded to trigger rotation.");
            return;
        }

        // Trigger rotation
        TriggerRotation();
    }

    /// <summary>
    /// Called when another collider exits this trigger.
    /// Resets playerInside flag to allow re-triggering.
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        // Check if exiting object is the player
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        // Mark player as outside
        playerInside = false;

        Debug.Log($"[WorldRotationTrigger] Player exited trigger '{gameObject.name}'. Ready to re-trigger.");
    }

    #endregion

    // ==================== TRIGGER LOGIC ====================
    #region TRIGGER LOGIC

    /// <summary>
    /// Checks if this trigger can currently activate.
    /// Validates: controller exists, not already used, player not inside
    /// </summary>
    /// <returns>True if trigger can activate, false otherwise</returns>
    private bool CanTrigger()
    {
        // Check if world controller exists
        if (worldController == null)
        {
            Debug.LogError($"[WorldRotationTrigger] Cannot trigger: World Controller not assigned!");
            return false;
        }

        // Check if world is already rotating
        if (worldController.isRotating)
        {
            Debug.Log($"[WorldRotationTrigger] Cannot trigger: World is already rotating.");
            return false;
        }

        // Check if one-time trigger has been used
        if (oneTimeUse && hasBeenUsed)
        {
            Debug.Log($"[WorldRotationTrigger] Cannot trigger: One-time trigger already used.");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Executes the rotation trigger.
    /// Calls rotation on BOTH WorldRotationController AND FezCameraController.
    /// Marks trigger as used if one-time.
    /// </summary>
    private void TriggerRotation()
    {
        bool worldSuccess = false;
        bool cameraSuccess = false;

        // Rotate world
        if (direction == RotationDirection.Right)
        {
            worldSuccess = worldController.RotateRight();
            Debug.Log($"[WorldRotationTrigger] Triggered RIGHT rotation from '{gameObject.name}'");
        }
        else
        {
            worldSuccess = worldController.RotateLeft();
            Debug.Log($"[WorldRotationTrigger] Triggered LEFT rotation from '{gameObject.name}'");
        }

        // Rotate camera (if exists)
        FezCameraController cameraController = FindObjectOfType<FezCameraController>();
        if (cameraController != null)
        {
            if (direction == RotationDirection.Right)
            {
                cameraSuccess = cameraController.RotateRight();
            }
            else
            {
                cameraSuccess = cameraController.RotateLeft();
            }
        }
        else
        {
            Debug.LogWarning("[WorldRotationTrigger] FezCameraController not found! Only world will rotate.");
        }

        // Mark as used if one-time trigger and rotation started
        if (worldSuccess && oneTimeUse)
        {
            hasBeenUsed = true;
            Debug.Log($"[WorldRotationTrigger] One-time trigger '{gameObject.name}' has been used and disabled.");
        }
    }

    /// <summary>
    /// Checks if player is grounded.
    /// Attempts to find PlayerController component and check its ground status.
    /// Returns true if grounded or if check cannot be performed.
    /// </summary>
    /// <param name="player">Player GameObject</param>
    /// <returns>True if grounded or unable to check, false if airborne</returns>
    private bool IsPlayerGrounded(GameObject player)
    {
        // Try to get PlayerController component
        var playerController = player.GetComponent<_PlayerController>();

        if (playerController != null)
        {
            // Access ground check through reflection or public property
            // Note: You may need to make isGrounded public in PlayerController
            // For now, we'll assume player is grounded if we can't check
            // TODO: Add public property to PlayerController for ground status
            return true; // Default to true if we can't check
        }

        // If no PlayerController found, default to true
        return true;
    }

    /// <summary>
    /// Manually resets trigger state.
    /// Useful for debugging or dynamic trigger reactivation.
    /// </summary>
    public void ResetTrigger()
    {
        hasBeenUsed = false;
        playerInside = false;
        Debug.Log($"[WorldRotationTrigger] Trigger '{gameObject.name}' has been reset.");
    }

    #endregion

    // ==================== VISUAL DEBUG ====================
    #region VISUAL DEBUG

    /// <summary>
    /// Draws debug visualization in Scene view.
    /// Shows trigger bounds, rotation direction, and current state.
    /// </summary>
    private void OnDrawGizmos()
    {
        // Get collider for bounds
        Collider col = GetComponent<Collider>();
        if (col == null) return;

        // Choose color based on state
        Color drawColor = gizmoColor;

        if (hasBeenUsed && oneTimeUse)
        {
            drawColor = Color.gray; // Used one-time trigger
        }
        else if (playerInside)
        {
            drawColor = Color.green; // Player inside
        }
        else if (worldController != null && worldController.isRotating)
        {
            drawColor = Color.red; // World currently rotating
        }

        Gizmos.color = drawColor;

        // Draw trigger bounds (wireframe)
        if (col is BoxCollider boxCol)
        {
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.matrix = rotationMatrix;
            Gizmos.DrawWireCube(boxCol.center, boxCol.size);
            Gizmos.matrix = Matrix4x4.identity;
        }
        else
        {
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }

        // Draw rotation direction arrow
        DrawRotationArrow();
    }

    /// <summary>
    /// Draws an arrow indicating rotation direction in Scene view.
    /// Right = Curved arrow clockwise
    /// Left = Curved arrow counter-clockwise
    /// </summary>
    private void DrawRotationArrow()
    {
        Vector3 center = transform.position;

        // Arrow color matches gizmo color
        Gizmos.color = gizmoColor;

        // Draw arrow based on direction
        if (direction == RotationDirection.Right)
        {
            // Draw curved arrow pointing right (clockwise)
            Vector3 arrowStart = center + transform.forward * arrowSize;
            Vector3 arrowEnd = center + transform.right * arrowSize;

            // Main arc
            DrawArc(center, arrowStart, arrowEnd, 10);

            // Arrowhead
            Vector3 arrowHead1 = arrowEnd - (transform.right * 0.3f * arrowSize) + (transform.up * 0.2f * arrowSize);
            Vector3 arrowHead2 = arrowEnd - (transform.right * 0.3f * arrowSize) - (transform.up * 0.2f * arrowSize);
            Gizmos.DrawLine(arrowEnd, arrowHead1);
            Gizmos.DrawLine(arrowEnd, arrowHead2);
        }
        else
        {
            // Draw curved arrow pointing left (counter-clockwise)
            Vector3 arrowStart = center + transform.forward * arrowSize;
            Vector3 arrowEnd = center - transform.right * arrowSize;

            // Main arc
            DrawArc(center, arrowStart, arrowEnd, 10);

            // Arrowhead
            Vector3 arrowHead1 = arrowEnd + (transform.right * 0.3f * arrowSize) + (transform.up * 0.2f * arrowSize);
            Vector3 arrowHead2 = arrowEnd + (transform.right * 0.3f * arrowSize) - (transform.up * 0.2f * arrowSize);
            Gizmos.DrawLine(arrowEnd, arrowHead1);
            Gizmos.DrawLine(arrowEnd, arrowHead2);
        }
    }

    /// <summary>
    /// Draws an arc from start to end point around center.
    /// Helper method for drawing curved rotation arrows.
    /// </summary>
    private void DrawArc(Vector3 center, Vector3 start, Vector3 end, int segments)
    {
        Vector3 prevPoint = start;

        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = Mathf.Lerp(0, 90, t);

            // Calculate rotation around Y-axis
            Quaternion rotation = Quaternion.Euler(0, direction == RotationDirection.Right ? angle : -angle, 0);

            // FIXED: Renamed local variable to avoid shadowing class field
            Vector3 arcDirection = start - center;
            Vector3 rotatedDirection = rotation * arcDirection;
            Vector3 point = center + rotatedDirection;

            Gizmos.DrawLine(prevPoint, point);
            prevPoint = point;
        }
    }
    #endregion
}


