using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// Fully modular world rotation controller for Fez-style 2.5D gameplay.
/// All features are toggle-based for maximum flexibility.
/// 
/// Core Systems:
/// - Input Mode: Rotation via keyboard (Q/E) or gamepad (LB/RB)
/// - Trigger Mode: Rotation when entering designated trigger zones
/// - Both modes can be enabled simultaneously or independently
/// - Smooth animated rotation with customizable easing
/// - Event system for other scripts to react to rotation
/// 
/// Input Setup:
/// - Uses Unity's New Input System with "Invoke Unity Events" behavior
/// - Add RotateLeft and RotateRight actions to your Input Actions asset
/// - Connect them to OnRotateLeft and OnRotateRight methods via PlayerInput component
/// 
/// Usage:
/// 1. Attach to an empty GameObject in your scene
/// 2. Assign camera and pivot references
/// 3. Add PlayerInput component and configure actions
/// 4. Enable desired modes (Input, Trigger, or both)
/// 5. For Trigger Mode, add _RotationTriggerZone to trigger colliders
/// </summary>
public class _WorldRotationController : MonoBehaviour
{
    #region Variables

    // ==================== SINGLETON ====================
    #region SINGLETON

    /// <summary>
    /// Singleton instance for easy access from trigger zones and other scripts.
    /// </summary>
    public static _WorldRotationController Instance { get; private set; }

    #endregion

    // ==================== REFERENCES ====================
    #region REFERENCES

    [Header("References")]

    [Tooltip("The camera that will rotate around the world. If null, will use Camera.main")]
    public Transform cameraTransform;

    [Tooltip("The pivot point the camera rotates around. If null, one will be created at world origin")]
    public Transform pivotPoint;

    [Tooltip("Optional: Target for the pivot to follow (e.g., player). Leave null for static pivot")]
    public Transform followTarget;

    #endregion

    // ==================== MASTER CONTROLS ====================
    #region MASTER CONTROLS

    [Header("Master Controls")]

    /// <summary>
    /// Master toggle - if FALSE, completely disables all rotation functionality.
    /// </summary>
    [Tooltip("Master toggle - disables ALL rotation when false")]
    public bool canRotate = true;

    /// <summary>
    /// If TRUE, player can rotate the world using keyboard/gamepad inputs.
    /// </summary>
    [Tooltip("Enable rotation via keyboard (Q/E) or gamepad (LB/RB)")]
    public bool inputModeEnabled = true;

    /// <summary>
    /// If TRUE, entering trigger zones can cause world rotation.
    /// </summary>
    [Tooltip("Enable rotation via trigger zones in the level")]
    public bool triggerModeEnabled = true;

    #endregion

    // ==================== ROTATION SETTINGS ====================
    #region ROTATION SETTINGS

    [Header("Rotation Settings")]

    /// <summary>
    /// Duration of the 90-degree rotation animation in seconds.
    /// </summary>
    [Tooltip("Duration of rotation animation in seconds")]
    [Range(0.1f, 2f)]
    public float rotationDuration = 0.5f;

    /// <summary>
    /// Distance from the pivot point to the camera.
    /// </summary>
    [Tooltip("Distance from pivot to camera")]
    public float cameraDistance = 15f;

    /// <summary>
    /// Height offset of the camera relative to the pivot point.
    /// </summary>
    [Tooltip("Camera height offset from pivot")]
    public float cameraHeightOffset = 2f;

    /// <summary>
    /// Animation curve for rotation easing.
    /// </summary>
    [Tooltip("Easing curve for rotation animation")]
    public AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    #endregion

    // ==================== FOLLOW SETTINGS ====================
    #region FOLLOW SETTINGS

    [Header("Follow Settings")]

    /// <summary>
    /// Speed at which the pivot follows the target.
    /// </summary>
    [Tooltip("How fast the pivot follows the target")]
    public float followSpeed = 5f;

    /// <summary>
    /// If TRUE, pivot continues following target during rotation animation.
    /// </summary>
    [Tooltip("Continue following target during rotation animation")]
    public bool followDuringRotation = false;

    #endregion

    // ==================== TRIGGER SETTINGS ====================
    #region TRIGGER SETTINGS

    [Header("Trigger Settings")]

    /// <summary>
    /// Cooldown between trigger-initiated rotations.
    /// </summary>
    [Tooltip("Cooldown between trigger rotations (prevents spam)")]
    public float triggerCooldown = 0.5f;

    /// <summary>
    /// If TRUE, trigger zones can interrupt an ongoing rotation.
    /// </summary>
    [Tooltip("Allow triggers to interrupt ongoing rotation")]
    public bool triggerCanInterrupt = false;

    #endregion

    // ==================== DEBUG ====================
    #region DEBUG

    [Header("Debug")]

    [Tooltip("Show debug messages in console")]
    public bool showDebugLogs = false;

    [Tooltip("Draw gizmos in Scene view")]
    public bool showDebugGizmos = true;

    [SerializeField] private int currentFaceIndex = 0; // 0=North, 1=East, 2=South, 3=West
    [SerializeField] private bool isRotating = false;

    #endregion

    // ==================== PRIVATE VARIABLES ====================
    #region PRIVATE VARIABLES

    // The four cardinal angles the camera can face
    private readonly float[] faceAngles = { 0f, 90f, 180f, 270f };

    // Current camera angle around the pivot
    private float currentAngle = 0f;

    // Timer for trigger cooldown
    private float triggerCooldownTimer = 0f;

    // Reference to current rotation coroutine (for interruption)
    private Coroutine activeRotationCoroutine = null;

    #endregion

    // ==================== EVENTS ====================
    #region EVENTS

    /// <summary>
    /// Fired when rotation begins. Parameter: new face index (0-3)
    /// </summary>
    public event System.Action<int> OnRotationStarted;

    /// <summary>
    /// Fired when rotation completes. Parameter: final face index (0-3)
    /// </summary>
    public event System.Action<int> OnRotationCompleted;

    /// <summary>
    /// Fired every frame during rotation. Parameter: progress (0-1)
    /// </summary>
    public event System.Action<float> OnRotationProgress;

    /// <summary>
    /// Fired when any mode toggle changes. Parameters: mode name, new state
    /// </summary>
    public event System.Action<string, bool> OnModeToggled;

    #endregion

    #endregion

    // ==================== UNITY LIFECYCLE ====================
    #region UNITY LIFECYCLE

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("[_WorldRotationController] Duplicate instance detected. Destroying this one.");
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        InitializeReferences();
        UpdateCameraPosition(currentAngle);
    }

    private void Update()
    {
        // Update trigger cooldown
        if (triggerCooldownTimer > 0f)
        {
            triggerCooldownTimer -= Time.deltaTime;
        }
    }

    private void LateUpdate()
    {
        // Follow target (if assigned and allowed)
        UpdatePivotFollow();

        // Update camera position when not rotating
        if (!isRotating)
        {
            UpdateCameraPosition(currentAngle);
        }
    }

    private void OnDestroy()
    {
        // Clean up singleton
        if (Instance == this)
        {
            Instance = null;
        }
    }

    #endregion

    // ==================== INITIALIZATION ====================
    #region INITIALIZATION

    /// <summary>
    /// Initializes missing references with sensible defaults.
    /// </summary>
    private void InitializeReferences()
    {
        // Auto-assign main camera if not set
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main?.transform;

            if (cameraTransform != null && showDebugLogs)
            {
                Debug.Log("[_WorldRotationController] Auto-assigned Camera.main");
            }
        }

        // Create pivot point if not set
        if (pivotPoint == null)
        {
            GameObject pivot = new GameObject("_WorldRotationPivot");
            pivotPoint = pivot.transform;

            if (showDebugLogs)
            {
                Debug.Log("[_WorldRotationController] Created pivot point at world origin");
            }
        }
    }

    #endregion

    // ==================== INPUT CALLBACKS (INVOKE UNITY EVENTS) ====================
    #region INPUT

    /// <summary>
    /// Called by Unity's Input System via PlayerInput component.
    /// Connect this to your "RotateLeft" action.
    /// </summary>
    public void OnRotateLeft(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed)
        {
            return;
        }

        if (!CanRotateViaInput())
        {
            return;
        }

        RotateWorld(-1);
    }

    /// <summary>
    /// Called by Unity's Input System via PlayerInput component.
    /// Connect this to your "RotateRight" action.
    /// </summary>
    public void OnRotateRight(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed)
        {
            return;
        }

        if (!CanRotateViaInput())
        {
            return;
        }

        RotateWorld(1);
    }

    #endregion

    // ==================== ROTATION LOGIC ====================
    #region ROTATION LOGIC

    /// <summary>
    /// Checks if rotation via input is currently allowed.
    /// </summary>
    private bool CanRotateViaInput()
    {
        if (!canRotate)
        {
            if (showDebugLogs) Debug.Log("[_WorldRotationController] Rotation blocked: canRotate is false");
            return false;
        }

        if (!inputModeEnabled)
        {
            if (showDebugLogs) Debug.Log("[_WorldRotationController] Rotation blocked: inputModeEnabled is false");
            return false;
        }

        if (isRotating)
        {
            if (showDebugLogs) Debug.Log("[_WorldRotationController] Rotation blocked: already rotating");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if rotation via trigger is currently allowed.
    /// </summary>
    private bool CanRotateViaTrigger()
    {
        if (!canRotate)
        {
            return false;
        }

        if (!triggerModeEnabled)
        {
            return false;
        }

        if (triggerCooldownTimer > 0f)
        {
            if (showDebugLogs) Debug.Log("[_WorldRotationController] Trigger rotation blocked: cooldown active");
            return false;
        }

        if (isRotating && !triggerCanInterrupt)
        {
            if (showDebugLogs) Debug.Log("[_WorldRotationController] Trigger rotation blocked: already rotating");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Initiates a 90-degree world rotation.
    /// </summary>
    /// <param name="direction">-1 for left (counter-clockwise), 1 for right (clockwise)</param>
    public void RotateWorld(int direction)
    {
        if (!canRotate)
        {
            return;
        }

        // Clamp direction to -1 or 1
        direction = direction < 0 ? -1 : 1;

        // Calculate new face index
        currentFaceIndex = (currentFaceIndex + direction + 4) % 4;

        // Stop any existing rotation if interrupting
        if (activeRotationCoroutine != null)
        {
            StopCoroutine(activeRotationCoroutine);
        }

        // Start rotation coroutine
        activeRotationCoroutine = StartCoroutine(RotateCoroutine(direction));
    }

    /// <summary>
    /// Called by trigger zones to request rotation.
    /// </summary>
    public void RotateFromTrigger(int direction)
    {
        if (!CanRotateViaTrigger())
        {
            return;
        }

        // Apply cooldown
        triggerCooldownTimer = triggerCooldown;

        // Perform rotation
        RotateWorld(direction);

        if (showDebugLogs)
        {
            Debug.Log($"[_WorldRotationController] Trigger rotation: direction={direction}");
        }
    }

    /// <summary>
    /// Rotates to a specific face index (0-3).
    /// </summary>
    public void RotateToFace(int targetFaceIndex)
    {
        if (!canRotate || isRotating)
        {
            return;
        }

        targetFaceIndex = Mathf.Clamp(targetFaceIndex, 0, 3);

        // Calculate shortest path
        int diff = targetFaceIndex - currentFaceIndex;

        // Normalize to -2 to +2 range for shortest path
        if (diff > 2) diff -= 4;
        if (diff < -2) diff += 4;

        if (diff != 0)
        {
            int direction = diff > 0 ? 1 : -1;
            RotateWorld(direction);
        }
    }

    /// <summary>
    /// Called by trigger zones to rotate to a specific face.
    /// </summary>
    public void RotateToFaceFromTrigger(int targetFaceIndex)
    {
        if (!CanRotateViaTrigger())
        {
            return;
        }

        triggerCooldownTimer = triggerCooldown;
        RotateToFace(targetFaceIndex);
    }

    /// <summary>
    /// Coroutine that performs the animated rotation.
    /// </summary>
    private IEnumerator RotateCoroutine(int direction)
    {
        isRotating = true;

        float startAngle = currentAngle;
        float targetAngle = startAngle + (direction * 90f);
        float elapsed = 0f;

        // Fire start event
        OnRotationStarted?.Invoke(currentFaceIndex);

        if (showDebugLogs)
        {
            Debug.Log($"[_WorldRotationController] Rotation started: face={currentFaceIndex}, direction={direction}");
        }

        // Animate rotation
        while (elapsed < rotationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / rotationDuration);
            float curvedT = rotationCurve.Evaluate(t);

            currentAngle = Mathf.Lerp(startAngle, targetAngle, curvedT);
            UpdateCameraPosition(currentAngle);

            // Follow target during rotation if enabled
            if (followDuringRotation)
            {
                UpdatePivotFollow();
            }

            // Fire progress event
            OnRotationProgress?.Invoke(t);

            yield return null;
        }

        // Finalize rotation
        currentAngle = targetAngle;
        NormalizeAngle();
        UpdateCameraPosition(currentAngle);

        isRotating = false;
        activeRotationCoroutine = null;

        // Fire completion event
        OnRotationCompleted?.Invoke(currentFaceIndex);

        if (showDebugLogs)
        {
            Debug.Log($"[_WorldRotationController] Rotation completed: face={currentFaceIndex}");
        }
    }

    /// <summary>
    /// Normalizes currentAngle to 0-360 range.
    /// </summary>
    private void NormalizeAngle()
    {
        while (currentAngle >= 360f) currentAngle -= 360f;
        while (currentAngle < 0f) currentAngle += 360f;
    }

    #endregion

    // ==================== CAMERA & PIVOT ====================
    #region CAMERA & PIVOT

    /// <summary>
    /// Updates camera position based on current angle.
    /// </summary>
    private void UpdateCameraPosition(float angle)
    {
        if (cameraTransform == null || pivotPoint == null)
        {
            return;
        }

        float radians = angle * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(
            Mathf.Sin(radians) * cameraDistance,
            cameraHeightOffset,
            Mathf.Cos(radians) * cameraDistance
        );

        cameraTransform.position = pivotPoint.position + offset;
        cameraTransform.LookAt(pivotPoint.position + Vector3.up * cameraHeightOffset);
    }

    /// <summary>
    /// Updates pivot position to follow target.
    /// </summary>
    private void UpdatePivotFollow()
    {
        if (followTarget == null || pivotPoint == null)
        {
            return;
        }

        // Don't follow during rotation unless explicitly allowed
        if (isRotating && !followDuringRotation)
        {
            return;
        }

        Vector3 targetPos = new Vector3(
            followTarget.position.x,
            pivotPoint.position.y,
            followTarget.position.z
        );

        pivotPoint.position = Vector3.Lerp(
            pivotPoint.position,
            targetPos,
            Time.deltaTime * followSpeed
        );
    }

    #endregion

    // ==================== PUBLIC TOGGLES ====================
    #region PUBLIC TOGGLES

    /// <summary>
    /// Enables or disables the entire rotation system.
    /// </summary>
    public void SetSystemEnabled(bool enabled)
    {
        canRotate = enabled;
        OnModeToggled?.Invoke("System", enabled);

        if (showDebugLogs)
        {
            Debug.Log($"[_WorldRotationController] System {(enabled ? "ENABLED" : "DISABLED")}");
        }
    }

    /// <summary>
    /// Enables or disables input-based rotation.
    /// </summary>
    public void SetInputModeEnabled(bool enabled)
    {
        inputModeEnabled = enabled;
        OnModeToggled?.Invoke("InputMode", enabled);

        if (showDebugLogs)
        {
            Debug.Log($"[_WorldRotationController] Input Mode {(enabled ? "ENABLED" : "DISABLED")}");
        }
    }

    /// <summary>
    /// Enables or disables trigger-based rotation.
    /// </summary>
    public void SetTriggerModeEnabled(bool enabled)
    {
        triggerModeEnabled = enabled;
        OnModeToggled?.Invoke("TriggerMode", enabled);

        if (showDebugLogs)
        {
            Debug.Log($"[_WorldRotationController] Trigger Mode {(enabled ? "ENABLED" : "DISABLED")}");
        }
    }

    #endregion

    // ==================== PUBLIC GETTERS ====================
    #region PUBLIC GETTERS

    /// <summary>
    /// Gets the current forward direction in world space based on camera rotation.
    /// </summary>
    public Vector3 GetCurrentForward()
    {
        float radians = currentAngle * Mathf.Deg2Rad;
        return new Vector3(-Mathf.Sin(radians), 0f, -Mathf.Cos(radians));
    }

    /// <summary>
    /// Gets the current right direction in world space based on camera rotation.
    /// </summary>
    public Vector3 GetCurrentRight()
    {
        float radians = currentAngle * Mathf.Deg2Rad;
        return new Vector3(-Mathf.Cos(radians), 0f, Mathf.Sin(radians));
    }

    /// <summary>
    /// Gets the current face index (0-3).
    /// </summary>
    public int GetCurrentFaceIndex()
    {
        return currentFaceIndex;
    }

    /// <summary>
    /// Gets the current rotation angle (0-360).
    /// </summary>
    public float GetCurrentAngle()
    {
        return currentAngle;
    }

    /// <summary>
    /// Returns true if world is currently rotating.
    /// </summary>
    public bool IsRotating()
    {
        return isRotating;
    }

    /// <summary>
    /// Returns true if input mode is enabled AND system is enabled.
    /// </summary>
    public bool IsInputModeActive()
    {
        return canRotate && inputModeEnabled;
    }

    /// <summary>
    /// Returns true if trigger mode is enabled AND system is enabled.
    /// </summary>
    public bool IsTriggerModeActive()
    {
        return canRotate && triggerModeEnabled;
    }

    #endregion

    // ==================== EDITOR GIZMOS ====================
    #region EDITOR GIZMOS

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos || pivotPoint == null)
        {
            return;
        }

        // Draw rotation circle
        Gizmos.color = Color.cyan;
        int segments = 36;
        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * (360f / segments) * Mathf.Deg2Rad;
            float angle2 = (i + 1) * (360f / segments) * Mathf.Deg2Rad;

            Vector3 p1 = pivotPoint.position + new Vector3(
                Mathf.Sin(angle1) * cameraDistance,
                cameraHeightOffset,
                Mathf.Cos(angle1) * cameraDistance
            );

            Vector3 p2 = pivotPoint.position + new Vector3(
                Mathf.Sin(angle2) * cameraDistance,
                cameraHeightOffset,
                Mathf.Cos(angle2) * cameraDistance
            );

            Gizmos.DrawLine(p1, p2);
        }

        // Draw the four cardinal positions
        Gizmos.color = Color.yellow;
        for (int i = 0; i < 4; i++)
        {
            float angle = faceAngles[i] * Mathf.Deg2Rad;
            Vector3 pos = pivotPoint.position + new Vector3(
                Mathf.Sin(angle) * cameraDistance,
                cameraHeightOffset,
                Mathf.Cos(angle) * cameraDistance
            );
            Gizmos.DrawWireSphere(pos, 0.5f);

            // Label
            UnityEditor.Handles.Label(pos + Vector3.up, $"Face {i}");
        }

        // Draw current camera position
        if (cameraTransform != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(cameraTransform.position, 0.3f);
            Gizmos.DrawLine(pivotPoint.position, cameraTransform.position);
        }

        // Draw pivot
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(pivotPoint.position, 0.5f);
    }
#endif

    #endregion
}
