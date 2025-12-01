using UnityEngine;

/// <summary>
/// Universal rotation trigger zone that works with both:
/// - _WorldRotationController (original)
/// - FezCinemachineRotation (Cinemachine version)
/// 
/// Automatically detects which controller is present in the scene.
/// </summary>
[RequireComponent(typeof(Collider))]
public class FezRotationTriggerZone : MonoBehaviour
{
    #region Enums

    public enum RotationMode
    {
        Directional,    // Rotate left or right by 90 degrees
        TargetFace      // Rotate to a specific face
    }

    public enum RotationDirection
    {
        Left = -1,
        Right = 1
    }

    #endregion

    #region Inspector Fields

    [Header("Trigger Settings")]
    [Tooltip("Tag of the object that can trigger rotation (usually 'Player')")]
    public string playerTag = "Player";

    [Tooltip("Trigger can only be used once")]
    public bool oneShot = false;

    [Tooltip("Cooldown before this trigger can activate again (0 = no cooldown)")]
    public float triggerCooldown = 1f;

    [Header("Rotation Settings")]
    [Tooltip("How this trigger causes rotation")]
    public RotationMode rotationMode = RotationMode.Directional;

    [Tooltip("Direction to rotate (Directional mode only)")]
    public RotationDirection direction = RotationDirection.Right;

    [Tooltip("Target face index: 0=North, 1=East, 2=South, 3=West (TargetFace mode only)")]
    [Range(0, 3)]
    public int targetFaceIndex = 0;

    [Header("Debug")]
    [Tooltip("Show debug info in console")]
    public bool showDebugLogs = false;

    [Tooltip("Color for gizmo visualization")]
    public Color gizmoColor = new Color(0f, 1f, 1f, 0.3f);

    #endregion

    #region Private Fields

    private bool hasTriggered = false;
    private float cooldownTimer = 0f;
    private Collider triggerCollider;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null && !triggerCollider.isTrigger)
        {
            Debug.LogWarning($"[FezRotationTriggerZone] Collider on {gameObject.name} is not a trigger. Setting isTrigger = true.");
            triggerCollider.isTrigger = true;
        }
    }

    private void Start()
    {
        DetectControllers();
    }

    private void Update()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        TryTriggerRotation();
    }

    #endregion

    #region Controller Detection

    private void DetectControllers()
    {
        // Simply use the bridge - it handles all controller detection
        if (!FezRotationBridge.Instance.IsAvailable)
        {
            Debug.LogError($"[FezRotationTriggerZone] {gameObject.name}: No rotation controller found in scene!");
        }
        else if (showDebugLogs)
        {
            Debug.Log($"[FezRotationTriggerZone] {gameObject.name}: Using FezRotationBridge ({FezRotationBridge.Instance.GetActiveControllerType()})");
        }
    }

    #endregion

    #region Trigger Logic

    private void TryTriggerRotation()
    {
        // Check one-shot
        if (oneShot && hasTriggered)
        {
            if (showDebugLogs) Debug.Log($"[FezRotationTriggerZone] {gameObject.name}: Already triggered (oneShot)");
            return;
        }

        // Check cooldown
        if (cooldownTimer > 0f)
        {
            if (showDebugLogs) Debug.Log($"[FezRotationTriggerZone] {gameObject.name}: On cooldown ({cooldownTimer:F1}s remaining)");
            return;
        }

        // Check if bridge is available
        if (!FezRotationBridge.Instance.IsAvailable)
        {
            DetectControllers();
            if (!FezRotationBridge.Instance.IsAvailable) return;
        }

        bool success = false;

        // Execute rotation via bridge
        switch (rotationMode)
        {
            case RotationMode.Directional:
                FezRotationBridge.Instance.RotateFromTrigger((int)direction);
                success = true;
                break;

            case RotationMode.TargetFace:
                FezRotationBridge.Instance.RotateToFaceFromTrigger(targetFaceIndex);
                success = true;
                break;
        }

        if (success)
        {
            hasTriggered = true;
            cooldownTimer = triggerCooldown;

            if (showDebugLogs)
            {
                string modeInfo = rotationMode == RotationMode.Directional
                    ? $"direction={direction}"
                    : $"targetFace={targetFaceIndex}";
                Debug.Log($"[FezRotationTriggerZone] {gameObject.name}: Triggered rotation ({modeInfo})");
            }
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Resets the trigger state.
    /// </summary>
    public void ResetTrigger()
    {
        hasTriggered = false;
        cooldownTimer = 0f;
        if (showDebugLogs) Debug.Log($"[FezRotationTriggerZone] {gameObject.name}: Trigger reset");
    }

    /// <summary>
    /// Manually activates this trigger.
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

    #region Editor Gizmos

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (triggerCollider == null)
        {
            triggerCollider = GetComponent<Collider>();
        }
        if (triggerCollider == null) return;

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
        }
        else if (triggerCollider is CapsuleCollider capsule)
        {
            Gizmos.DrawWireSphere(transform.position, capsule.radius);
        }

        // Direction indicator
        Gizmos.color = Color.white;
        Vector3 arrowDir = rotationMode == RotationMode.Directional
            ? (direction == RotationDirection.Right ? transform.right : -transform.right)
            : transform.forward;
        Gizmos.DrawRay(transform.position, arrowDir * 2f);

        // Label
        string label = rotationMode == RotationMode.Directional
            ? $"Rotate {direction}"
            : $"To Face {targetFaceIndex}";
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, label);
    }
#endif

    #endregion
}
