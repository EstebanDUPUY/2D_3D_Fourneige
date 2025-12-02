// ============================================================================
// FEZ-STYLE ROTATION TRIGGER ZONE
// ============================================================================
// This script creates a trigger volume that causes world rotation when
// the player enters it. Attach to any GameObject with a trigger collider.
// ============================================================================

// Required Unity namespaces
using UnityEngine;                    // Core Unity functionality

/// <summary>
/// Trigger zone that causes world rotation when the player enters.
/// 
/// SETUP:
/// 1. Create a GameObject where you want the trigger
/// 2. Add a Collider component (BoxCollider, SphereCollider, etc.)
/// 3. Enable "Is Trigger" on the collider
/// 4. Add this component
/// 5. Configure rotation mode and direction/target
/// 
/// MODES:
/// - Directional: Rotates left or right by 90 degrees
/// - TargetFace: Rotates to a specific face (0-3)
/// </summary>
[RequireComponent(typeof(Collider))]  // Ensures a Collider exists on this GameObject
public class FusedRotationTrigger : MonoBehaviour
{
    // ========================================================================
    // ENUMS
    // ========================================================================
    // Enums define the available options for rotation behavior.
    // ========================================================================
    
    #region Enums
    
    /// <summary>
    /// Defines how this trigger causes rotation.
    /// </summary>
    public enum RotationMode
    {
        Directional,                              // Rotate 90° left or right
        TargetFace                                // Rotate to a specific face
    }
    
    /// <summary>
    /// Defines the direction for Directional mode.
    /// </summary>
    public enum RotationDirection
    {
        Left = -1,                                // Counter-clockwise rotation
        Right = 1                                 // Clockwise rotation
    }
    
    #endregion
    
    // ========================================================================
    // INSPECTOR SETTINGS
    // ========================================================================
    
    #region Trigger Settings
    
    [Header("=== TRIGGER SETTINGS ===")]
    
    [Tooltip("Tag that identifies the player. Only objects with this tag will trigger rotation.")]
    [SerializeField]
    private string playerTag = "Player";          // Make sure your player has this tag
    
    [Tooltip("If TRUE, this trigger can only be used once ever.")]
    [SerializeField]
    private bool oneShot = false;                 // One-time triggers for puzzles
    
    [Tooltip("Time in seconds before this trigger can activate again. 0 = no cooldown.")]
    [SerializeField]
    private float triggerCooldown = 1f;           // Prevents rapid re-triggering
    
    #endregion
    
    #region Rotation Settings
    
    [Header("=== ROTATION SETTINGS ===")]
    
    [Tooltip("How this trigger causes rotation.")]
    [SerializeField]
    private RotationMode rotationMode = RotationMode.Directional;
    
    [Tooltip("Direction to rotate (only used in Directional mode).")]
    [SerializeField]
    private RotationDirection direction = RotationDirection.Right;
    
    [Tooltip("Target face index (only used in TargetFace mode). 0=North, 1=East, 2=South, 3=West.")]
    [Range(0, 3)]
    [SerializeField]
    private int targetFaceIndex = 0;
    
    #endregion
    
    #region Debug Settings
    
    [Header("=== DEBUG ===")]
    
    [Tooltip("Show debug messages in Console.")]
    [SerializeField]
    private bool showDebugLogs = false;
    
    [Tooltip("Color for the trigger zone gizmo in Scene view.")]
    [SerializeField]
    private Color gizmoColor = new Color(0f, 1f, 1f, 0.3f);  // Cyan with transparency
    
    #endregion
    
    // ========================================================================
    // PRIVATE STATE
    // ========================================================================
    
    #region Private Variables
    
    private bool hasTriggered = false;            // Has this one-shot trigger been used?
    private float cooldownTimer = 0f;             // Current cooldown time remaining
    private Collider triggerCollider;             // Reference to our collider
    
    #endregion
    
    // ========================================================================
    // UNITY LIFECYCLE
    // ========================================================================
    
    #region Unity Lifecycle
    
    /// <summary>
    /// Called when script instance is loaded.
    /// </summary>
    private void Awake()
    {
        // --- GET COLLIDER REFERENCE ---
        
        // Get the collider on this GameObject
        triggerCollider = GetComponent<Collider>();
        
        // --- ENSURE IT'S A TRIGGER ---
        
        // A trigger collider doesn't physically block objects,
        // but fires OnTriggerEnter/Exit events instead
        if (triggerCollider != null && !triggerCollider.isTrigger)
        {
            // Collider exists but isn't set as trigger - fix it
            Debug.LogWarning($"[FezRotationTrigger] Collider on '{gameObject.name}' was not a trigger. Setting isTrigger = true.");
            triggerCollider.isTrigger = true;     // Make it a trigger
        }
    }
    
    /// <summary>
    /// Called every frame.
    /// Used to update the cooldown timer.
    /// </summary>
    private void Update()
    {
        // --- UPDATE COOLDOWN ---
        
        // Count down the cooldown timer if it's active
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;      // Subtract frame time
        }
    }
    
    /// <summary>
    /// Called when another collider enters this trigger.
    /// This is where we detect the player and trigger rotation.
    /// </summary>
    /// <param name="other">The collider that entered</param>
    private void OnTriggerEnter(Collider other)
    {
        // --- CHECK IF IT'S THE PLAYER ---
        
        // CompareTag is faster than checking the tag property directly
        if (!other.CompareTag(playerTag))
        {
            // Not the player - ignore this collision
            return;
        }
        
        // --- ATTEMPT ROTATION ---
        
        // It's the player - try to trigger rotation
        TryTriggerRotation();
    }
    
    #endregion
    
    // ========================================================================
    // TRIGGER LOGIC
    // ========================================================================
    
    #region Trigger Logic
    
    /// <summary>
    /// Attempts to trigger rotation if all conditions are met.
    /// Checks one-shot status, cooldown, and rotation controller availability.
    /// </summary>
    private void TryTriggerRotation()
    {
        // --- CHECK ONE-SHOT ---
        
        // If this is a one-shot trigger and it's already been used, exit
        if (oneShot && hasTriggered)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[FezRotationTrigger] '{gameObject.name}': Already triggered (one-shot).");
            }
            return;                               // Exit - can't trigger again
        }
        
        // --- CHECK COOLDOWN ---
        
        // If we're still in cooldown, exit
        if (cooldownTimer > 0f)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[FezRotationTrigger] '{gameObject.name}': On cooldown ({cooldownTimer:F1}s remaining).");
            }
            return;                               // Exit - wait for cooldown
        }

        // --- CHECK ROTATION CONTROLLER ---

        // Get reference to the rotation controller
        FusedWorldRotation controller = FusedWorldRotation.Instance;
        
        // Make sure controller exists
        if (controller == null)
        {
            Debug.LogError($"[FezRotationTrigger] '{gameObject.name}': No FezWorldRotation found in scene!");
            return;                               // Exit - no controller to use
        }
        
        // --- PERFORM ROTATION ---
        
        // Execute rotation based on mode
        switch (rotationMode)
        {
            case RotationMode.Directional:
                // Rotate left or right
                // Cast enum to int to get -1 or 1
                controller.RotateFromTrigger((int)direction);
                break;
                
            case RotationMode.TargetFace:
                // Rotate to specific face
                controller.RotateToFaceFromTrigger(targetFaceIndex);
                break;
        }
        
        // --- UPDATE STATE ---
        
        // Mark as triggered (for one-shot)
        hasTriggered = true;
        
        // Start cooldown
        cooldownTimer = triggerCooldown;
        
        // --- DEBUG LOG ---
        
        if (showDebugLogs)
        {
            // Build info string based on mode
            string modeInfo = rotationMode == RotationMode.Directional
                ? $"direction = {direction}"
                : $"targetFace = {targetFaceIndex}";
                
            Debug.Log($"[FezRotationTrigger] '{gameObject.name}': Triggered rotation ({modeInfo}).");
        }
    }
    
    #endregion
    
    // ========================================================================
    // PUBLIC METHODS
    // ========================================================================
    
    #region Public Methods
    
    /// <summary>
    /// Resets this trigger so it can be used again.
    /// Call this to re-enable a one-shot trigger.
    /// </summary>
    public void ResetTrigger()
    {
        // Clear triggered state
        hasTriggered = false;
        
        // Clear cooldown
        cooldownTimer = 0f;
        
        if (showDebugLogs)
        {
            Debug.Log($"[FezRotationTrigger] '{gameObject.name}': Trigger reset.");
        }
    }
    
    /// <summary>
    /// Manually activates this trigger from code.
    /// Useful for buttons, cutscenes, etc.
    /// </summary>
    public void ActivateTrigger()
    {
        TryTriggerRotation();                     // Use same logic as collision
    }
    
    /// <summary>
    /// Checks if this trigger is ready to activate.
    /// Returns FALSE if it's a used one-shot or on cooldown.
    /// </summary>
    /// <returns>TRUE if trigger can activate, FALSE otherwise</returns>
    public bool IsReady()
    {
        // Check one-shot
        if (oneShot && hasTriggered)
        {
            return false;                         // One-shot already used
        }
        
        // Check cooldown
        if (cooldownTimer > 0f)
        {
            return false;                         // Still on cooldown
        }
        
        // All checks passed
        return true;
    }
    
    /// <summary>
    /// Returns TRUE if this trigger has been used (for one-shot triggers).
    /// </summary>
    public bool HasBeenTriggered()
    {
        return hasTriggered;
    }
    
    #endregion
    
    // ========================================================================
    // EDITOR GIZMOS
    // ========================================================================
    
    #region Editor Gizmos
    
#if UNITY_EDITOR
    /// <summary>
    /// Draws the trigger zone in the Scene view.
    /// Helps visualize trigger areas during level design.
    /// </summary>
    private void OnDrawGizmos()
    {
        // --- GET COLLIDER ---
        
        // Get collider reference if we don't have it yet
        if (triggerCollider == null)
        {
            triggerCollider = GetComponent<Collider>();
        }
        
        // Exit if no collider
        if (triggerCollider == null)
        {
            return;
        }
        
        // --- DETERMINE COLOR ---
        
        // Base color from settings
        Color drawColor = gizmoColor;
        
        // Dim color if trigger is used (one-shot)
        if (hasTriggered && oneShot)
        {
            drawColor = Color.gray;               // Gray = used
            drawColor.a = 0.2f;                   // More transparent
        }
        // Yellow if on cooldown
        else if (cooldownTimer > 0f)
        {
            drawColor = Color.yellow;             // Yellow = cooling down
            drawColor.a = 0.3f;
        }
        
        // Set gizmo color
        Gizmos.color = drawColor;
        
        // --- DRAW COLLIDER SHAPE ---
        
        // Draw appropriate shape based on collider type
        if (triggerCollider is BoxCollider box)
        {
            // Box collider - draw a cube
            // Need to account for transform rotation and scale
            Matrix4x4 oldMatrix = Gizmos.matrix;  // Save current matrix
            Gizmos.matrix = Matrix4x4.TRS(        // Create transform matrix
                transform.position,               // Position
                transform.rotation,               // Rotation
                transform.lossyScale             // Scale (including parents)
            );
            
            // Draw filled and wireframe cube
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.DrawWireCube(box.center, box.size);
            
            Gizmos.matrix = oldMatrix;            // Restore matrix
        }
        else if (triggerCollider is SphereCollider sphere)
        {
            // Sphere collider - draw a sphere
            Vector3 center = transform.position + sphere.center;
            float radius = sphere.radius * transform.lossyScale.x;  // Apply scale
            
            Gizmos.DrawSphere(center, radius);
            Gizmos.DrawWireSphere(center, radius);
        }
        else if (triggerCollider is CapsuleCollider capsule)
        {
            // Capsule collider - just draw a wire sphere (simplified)
            Gizmos.DrawWireSphere(transform.position, capsule.radius);
        }
        
        // --- DRAW DIRECTION INDICATOR ---
        
        // White arrow showing rotation direction
        Gizmos.color = Color.white;
        
        // Determine arrow direction based on mode
        Vector3 arrowDir;
        
        if (rotationMode == RotationMode.Directional)
        {
            // Point left or right based on direction setting
            arrowDir = direction == RotationDirection.Right 
                ? transform.right     // Right arrow
                : -transform.right;   // Left arrow
        }
        else
        {
            // Point forward for target face mode
            arrowDir = transform.forward;
        }
        
        // Draw arrow
        Gizmos.DrawRay(transform.position, arrowDir * 2f);
        
        // --- DRAW LABEL ---
        
        // Create label text
        string label = rotationMode == RotationMode.Directional
            ? $"Rotate {direction}"
            : $"To Face {targetFaceIndex}";
        
        // Draw label above trigger
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, label);
    }
#endif
    
    #endregion
}
