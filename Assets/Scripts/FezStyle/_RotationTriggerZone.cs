using UnityEngine;

/// <summary>
/// Trigger zone that causes world rotation when player enters.
/// Attach to any GameObject with a trigger collider.
/// 
/// Modes:
/// - Directional: Rotates left or right by 90 degrees
/// - Target Face: Rotates to a specific face (0-3)
/// 
/// Features:
/// - One-shot or repeatable triggers
/// - Optional cooldown per trigger
/// - Visual debug in editor
/// - Works with _WorldRotationController's trigger mode
/// 
/// Usage:
/// 1. Add a Collider component and set "Is Trigger" to true
/// 2. Add this script
/// 3. Configure rotation mode and direction
/// 4. Set the player tag for detection
/// </summary>
[RequireComponent(typeof(Collider))]
public class _RotationTriggerZone : MonoBehaviour
{
    #region Variables

    // ==================== TRIGGER SETTINGS ====================
    #region TRIGGER SETTINGS

    [Header("Trigger Settings")]

    /// <summary>
    /// Tag used to identify the player.
    /// Only objects with this tag will trigger rotation.
    /// </summary>
    [Tooltip("Tag of the object that can trigger rotation (usually 'Player')")]
    public string playerTag = "Player";

    /// <summary>
    /// If TRUE, this trigger can only be used once.
    /// After triggering, it will be disabled until manually reset.
    /// </summary>
    [Tooltip("Trigger can only be used once")]
    public bool oneShot = false;

    /// <summary>
    /// Cooldown before this specific trigger can activate again.
    /// Independent of _WorldRotationController's global trigger cooldown.
    /// </summary>
    [Tooltip("Cooldown before this trigger can activate again (0 = no cooldown)")]
    public float triggerCooldown = 1f;

    #endregion

    // ==================== ROTATION MODE ====================
    #region ROTATION MODE

    //[Header("Rotation Mode")]

    /// <summary>
    /// Rotation mode for this trigger.
    /// Directional: Rotates by direction (left/right)
    /// TargetFace: Rotates to a specific face
    /// </summary>
    public enum RotationMode
    {
        Directional,    // Rotate left or right
        TargetFace      // Rotate to specific face
    }

    [Tooltip("How this trigger causes rotation")]
    public RotationMode rotationMode = RotationMode.Directional;

    #endregion

    // ==================== DIRECTIONAL MODE ====================
    #region DIRECTIONAL MODE

    //[Header("Directional Mode Settings")]

    /// <summary>
    /// Direction to rotate when using Directional mode.
    /// </summary>
    public enum RotationDirection
    {
        Left = -1,      // Counter-clockwise
        Right = 1       // Clockwise
    }

    [Tooltip("Direction to rotate (Directional mode only)")]
    public RotationDirection direction = RotationDirection.Right;

    #endregion

    // ==================== TARGET FACE MODE ====================
    #region TARGET FACE MODE

    [Header("Target Face Mode Settings")]

    /// <summary>
    /// The face to rotate to when using TargetFace mode.
    /// 0=North, 1=East, 2=South, 3=West
    /// </summary>
    [Tooltip("Target face index: 0=North, 1=East, 2=South, 3=West")]
    [Range(0, 3)]
    public int targetFaceIndex = 0;

    #endregion

    // ==================== DEBUG ====================
    #region DEBUG

    [Header("Debug")]

    [Tooltip("Show debug info in console")]
    public bool showDebugLogs = false;

    [Tooltip("Color for gizmo visualization")]
    public Color gizmoColor = new Color(0f, 1f, 1f, 0.3f);

    #endregion

    // ==================== PRIVATE VARIABLES ====================
    #region PRIVATE VARIABLES

    // Tracks if this trigger has been used (for oneShot mode)
    private bool hasTriggered = false;

    // Cooldown timer for this specific trigger
    private float cooldownTimer = 0f;

    // Cached reference to controller
    private _WorldRotationController rotationController;

    // Cached collider for gizmo drawing
    private Collider triggerCollider;

    #endregion

    #endregion

    // ==================== UNITY LIFECYCLE ====================
    #region UNITY LIFECYCLE

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();

        // Ensure collider is set as trigger
        if (triggerCollider != null && !triggerCollider.isTrigger)
        {
            Debug.LogWarning($"[_RotationTriggerZone] Collider on {gameObject.name} is not a trigger. Setting isTrigger = true.");
            triggerCollider.isTrigger = true;
        }
    }

    private void Start()
    {
        // Cache controller reference
        rotationController = _WorldRotationController.Instance;

        if (rotationController == null)
        {
            Debug.LogError($"[_RotationTriggerZone] No _WorldRotationController found in scene! Trigger on {gameObject.name} will not work.");
        }
    }

    private void Update()
    {
        // Update cooldown timer
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if it's the player
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        // Try to trigger rotation
        TryTriggerRotation();
    }

    #endregion

    // ==================== TRIGGER LOGIC ====================
    #region TRIGGER LOGIC

    /// <summary>
    /// Attempts to trigger rotation.
    /// Checks all conditions before requesting rotation from controller.
    /// </summary>
    private void TryTriggerRotation()
    {
        // Check if already used (oneShot mode)
        if (oneShot && hasTriggered)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[_RotationTriggerZone] {gameObject.name}: Already triggered (oneShot)");
            }
            return;
        }

        // Check cooldown
        if (cooldownTimer > 0f)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[_RotationTriggerZone] {gameObject.name}: On cooldown ({cooldownTimer:F1}s remaining)");
            }
            return;
        }

        // Check controller exists
        if (rotationController == null)
        {
            rotationController = _WorldRotationController.Instance;
            if (rotationController == null)
            {
                Debug.LogError($"[_RotationTriggerZone] {gameObject.name}: No controller found!");
                return;
            }
        }

        // Request rotation based on mode
        bool success = false;

        switch (rotationMode)
        {
            case RotationMode.Directional:
                rotationController.RotateFromTrigger((int)direction);
                success = true;
                break;

            case RotationMode.TargetFace:
                rotationController.RotateToFaceFromTrigger(targetFaceIndex);
                success = true;
                break;
        }

        // Handle successful trigger
        if (success)
        {
            hasTriggered = true;
            cooldownTimer = triggerCooldown;

            if (showDebugLogs)
            {
                string modeInfo = rotationMode == RotationMode.Directional
                    ? $"direction={direction}"
                    : $"targetFace={targetFaceIndex}";
                Debug.Log($"[_RotationTriggerZone] {gameObject.name}: Triggered rotation ({modeInfo})");
            }
        }
    }

    #endregion

    // ==================== PUBLIC METHODS ====================
    #region PUBLIC METHODS

    /// <summary>
    /// Resets the trigger state.
    /// Allows a oneShot trigger to be used again.
    /// </summary>
    public void ResetTrigger()
    {
        hasTriggered = false;
        cooldownTimer = 0f;

        if (showDebugLogs)
        {
            Debug.Log($"[_RotationTriggerZone] {gameObject.name}: Trigger reset");
        }
    }

    /// <summary>
    /// Manually activates this trigger.
    /// Useful for scripted events.
    /// </summary>
    public void ActivateTrigger()
    {
        TryTriggerRotation();
    }

    /// <summary>
    /// Checks if this trigger is ready to activate.
    /// </summary>
    public bool IsReady()
    {
        if (oneShot && hasTriggered) return false;
        if (cooldownTimer > 0f) return false;
        return true;
    }

    #endregion

    // ==================== EDITOR GIZMOS ====================
    #region EDITOR GIZMOS

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Draw trigger zone
        if (triggerCollider == null)
        {
            triggerCollider = GetComponent<Collider>();
        }

        if (triggerCollider == null)
        {
            return;
        }

        // Set color based on state
        Color drawColor = gizmoColor;
        if (hasTriggered && oneShot)
        {
            drawColor = Color.gray;
            drawColor.a = 0.2f;
        }
        else if (cooldownTimer > 0f)
        {
            drawColor = Color.yellow;
            drawColor.a = 0.3f;
        }

        Gizmos.color = drawColor;

        // Draw based on collider type
        if (triggerCollider is BoxCollider box)
        {
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.DrawWireCube(box.center, box.size);
            Gizmos.matrix = oldMatrix;
        }
        else if (triggerCollider is SphereCollider sphere)
        {
            Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius * transform.lossyScale.x);
            Gizmos.DrawWireSphere(transform.position + sphere.center, sphere.radius * transform.lossyScale.x);
        }
        else if (triggerCollider is CapsuleCollider capsule)
        {
            // Simplified capsule drawing
            Gizmos.DrawWireSphere(transform.position, capsule.radius);
        }

        // Draw direction arrow
        Gizmos.color = Color.white;
        Vector3 arrowStart = transform.position;
        Vector3 arrowDir = rotationMode == RotationMode.Directional
            ? (direction == RotationDirection.Right ? transform.right : -transform.right)
            : transform.forward;

        Gizmos.DrawRay(arrowStart, arrowDir * 2f);

        // Draw label
        string label = rotationMode == RotationMode.Directional
            ? $"Rotate {direction}"
            : $"To Face {targetFaceIndex}";
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, label);
    }
#endif

    #endregion
}