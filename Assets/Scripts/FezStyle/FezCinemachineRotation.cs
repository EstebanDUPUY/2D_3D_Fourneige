using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
#if CINEMACHINE_3 || UNITY_6000_0_OR_NEWER
using Unity.Cinemachine;
#else
using Cinemachine;
#endif

/// <summary>
/// Cinemachine-compatible world rotation controller for Fez-style 2.5D gameplay.
/// 
/// SETUP OPTIONS:
/// 
/// 1. PIVOT RIG METHOD (Recommended):
///    - Create empty GameObject "CameraRig" at world origin
///    - Parent your CinemachineVirtualCamera to CameraRig
///    - Set rotationMethod = RotationMethod.RotatePivotRig
///    - Assign CameraRig to pivotRig field
///    - Cinemachine handles follow/look, this script rotates the rig
/// 
/// 2. ORBITAL TRANSPOSER METHOD:
///    - Use Cinemachine Virtual Camera with Orbital Transposer body
///    - Set rotationMethod = RotationMethod.OrbitalTransposer  
///    - Assign the virtual camera to virtualCamera field
///    - This script controls the orbital angle directly
/// 
/// 3. ROTATE WORLD METHOD:
///    - Instead of rotating camera, rotate all level geometry
///    - Set rotationMethod = RotationMethod.RotateWorld
///    - Assign your level root to worldRoot field
///    - Camera stays fixed, world rotates around player
/// 
/// All methods maintain the same API (events, face index, etc.) as the original controller.
/// </summary>
public class FezCinemachineRotation : MonoBehaviour
{
    #region Enums

    public enum RotationMethod
    {
        /// <summary>Rotate a parent rig that contains the virtual camera</summary>
        RotatePivotRig,
        /// <summary>Control Cinemachine's Orbital Transposer angle directly</summary>
        OrbitalTransposer,
        /// <summary>Rotate the world instead of the camera</summary>
        RotateWorld
    }

    #endregion

    #region Singleton

    public static FezCinemachineRotation Instance { get; private set; }

    #endregion

    #region Inspector Fields

    // ==================== METHOD SELECTION ====================
    [Header("Rotation Method")]
    
    [Tooltip("How to achieve the rotation effect")]
    public RotationMethod rotationMethod = RotationMethod.RotatePivotRig;

    // ==================== PIVOT RIG REFERENCES ====================
    [Header("Pivot Rig Method")]
    
    [Tooltip("The pivot rig that contains/parents the virtual camera")]
    public Transform pivotRig;
    
    [Tooltip("Target for the pivot to follow (usually the player)")]
    public Transform followTarget;
    
    [Tooltip("How fast the pivot follows the target")]
    public float followSpeed = 10f;
    
    [Tooltip("Follow target during rotation animation")]
    public bool followDuringRotation = true;

    // ==================== ORBITAL TRANSPOSER REFERENCES ====================
    [Header("Orbital Transposer Method")]
    
#if CINEMACHINE_3 || UNITY_6000_0_OR_NEWER
    [Tooltip("Virtual camera with OrbitalFollow component")]
    public CinemachineCamera virtualCamera;
#else
    [Tooltip("Virtual camera with Orbital Transposer body")]
    public CinemachineVirtualCamera virtualCamera;
#endif
    
    [Tooltip("Use instant angle change (true) or let Cinemachine dampen (false)")]
    public bool instantOrbitalRotation = true;

    // ==================== WORLD ROTATION REFERENCES ====================
    [Header("World Rotation Method")]
    
    [Tooltip("Root transform containing all rotatable level geometry")]
    public Transform worldRoot;
    
    [Tooltip("Point around which the world rotates (usually player position)")]
    public Transform worldRotationCenter;

    // ==================== MASTER CONTROLS ====================
    [Header("Master Controls")]
    
    [Tooltip("Master toggle - disables ALL rotation when false")]
    public bool canRotate = true;
    
    [Tooltip("Enable rotation via input (Q/E keys)")]
    public bool inputModeEnabled = true;
    
    [Tooltip("Enable rotation via trigger zones")]
    public bool triggerModeEnabled = true;

    // ==================== ROTATION SETTINGS ====================
    [Header("Rotation Settings")]
    
    [Tooltip("Duration of 90-degree rotation in seconds")]
    [Range(0.1f, 2f)]
    public float rotationDuration = 0.5f;
    
    [Tooltip("Easing curve for rotation animation")]
    public AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Tooltip("Starting face index (0=North, 1=East, 2=South, 3=West)")]
    [Range(0, 3)]
    public int startingFaceIndex = 0;

    // ==================== TRIGGER SETTINGS ====================
    [Header("Trigger Settings")]
    
    [Tooltip("Cooldown between trigger-initiated rotations")]
    public float triggerCooldown = 0.5f;
    
    [Tooltip("Allow triggers to interrupt ongoing rotation")]
    public bool triggerCanInterrupt = false;

    // ==================== DEBUG ====================
    [Header("Debug")]
    
    [Tooltip("Show debug messages in console")]
    public bool showDebugLogs = false;
    
    [Tooltip("Draw gizmos in Scene view")]
    public bool showDebugGizmos = true;

    #endregion

    #region Events

    /// <summary>Fired when rotation begins. Parameter: new face index (0-3)</summary>
    public event System.Action<int> OnRotationStarted;
    
    /// <summary>Fired when rotation completes. Parameter: final face index (0-3)</summary>
    public event System.Action<int> OnRotationCompleted;
    
    /// <summary>Fired every frame during rotation. Parameter: progress (0-1)</summary>
    public event System.Action<float> OnRotationProgress;
    
    /// <summary>Fired when any mode toggle changes. Parameters: mode name, new state</summary>
    public event System.Action<string, bool> OnModeToggled;

    #endregion

    #region Private Fields

    private readonly float[] faceAngles = { 0f, 90f, 180f, 270f };
    private int currentFaceIndex = 0;
    private float currentAngle = 0f;
    private bool isRotating = false;
    private float triggerCooldownTimer = 0f;
    private Coroutine activeRotationCoroutine = null;

    // Cached components for Orbital method
#if CINEMACHINE_3 || UNITY_6000_0_OR_NEWER
    private CinemachineOrbitalFollow orbitalFollow;
#else
    private CinemachineOrbitalTransposer orbitalTransposer;
#endif

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("[FezCinemachineRotation] Duplicate instance detected. Destroying this one.");
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        InitializeMethod();
        
        currentFaceIndex = startingFaceIndex;
        currentAngle = faceAngles[currentFaceIndex];
        
        ApplyRotationImmediate(currentAngle);
    }

    private void Update()
    {
        if (triggerCooldownTimer > 0f)
        {
            triggerCooldownTimer -= Time.deltaTime;
        }
    }

    private void LateUpdate()
    {
        if (rotationMethod == RotationMethod.RotatePivotRig)
        {
            UpdatePivotFollow();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    #endregion

    #region Initialization

    private void InitializeMethod()
    {
        switch (rotationMethod)
        {
            case RotationMethod.RotatePivotRig:
                if (pivotRig == null)
                {
                    Debug.LogError("[FezCinemachineRotation] Pivot Rig method selected but pivotRig is not assigned!");
                }
                break;

            case RotationMethod.OrbitalTransposer:
                if (virtualCamera == null)
                {
                    Debug.LogError("[FezCinemachineRotation] Orbital Transposer method selected but virtualCamera is not assigned!");
                }
                else
                {
                    CacheOrbitalComponent();
                }
                break;

            case RotationMethod.RotateWorld:
                if (worldRoot == null)
                {
                    Debug.LogError("[FezCinemachineRotation] World Rotation method selected but worldRoot is not assigned!");
                }
                break;
        }

        if (showDebugLogs)
        {
            Debug.Log($"[FezCinemachineRotation] Initialized with method: {rotationMethod}");
        }
    }

    private void CacheOrbitalComponent()
    {
#if CINEMACHINE_3 || UNITY_6000_0_OR_NEWER
        orbitalFollow = virtualCamera.GetComponent<CinemachineOrbitalFollow>();
        if (orbitalFollow == null)
        {
            Debug.LogError("[FezCinemachineRotation] Virtual camera does not have CinemachineOrbitalFollow component!");
        }
#else
        var composer = virtualCamera.GetCinemachineComponent<CinemachineOrbitalTransposer>();
        if (composer != null)
        {
            orbitalTransposer = composer;
        }
        else
        {
            Debug.LogError("[FezCinemachineRotation] Virtual camera does not have Orbital Transposer body!");
        }
#endif
    }

    #endregion

    #region Input Handlers

    /// <summary>
    /// Connect to Input System's RotateLeft action.
    /// </summary>
    public void OnRotateLeft(InputAction.CallbackContext context)
    {
        if (context.performed && inputModeEnabled && canRotate)
        {
            RotateWorld(-1);
        }
    }

    /// <summary>
    /// Connect to Input System's RotateRight action.
    /// </summary>
    public void OnRotateRight(InputAction.CallbackContext context)
    {
        if (context.performed && inputModeEnabled && canRotate)
        {
            RotateWorld(1);
        }
    }

    #endregion

    #region Public Rotation Methods

    /// <summary>
    /// Rotates the world/camera by 90 degrees.
    /// </summary>
    /// <param name="direction">-1 for left, 1 for right</param>
    public void RotateWorld(int direction)
    {
        if (!canRotate) return;
        if (isRotating) return;

        direction = Mathf.Clamp(direction, -1, 1);
        if (direction == 0) return;

        int newFaceIndex = (currentFaceIndex + direction + 4) % 4;
        StartRotation(newFaceIndex, direction);
    }

    /// <summary>
    /// Rotates to a specific face index.
    /// </summary>
    public void RotateToFace(int targetFaceIndex)
    {
        if (!canRotate) return;
        if (isRotating) return;

        targetFaceIndex = Mathf.Clamp(targetFaceIndex, 0, 3);
        if (targetFaceIndex == currentFaceIndex) return;

        // Calculate shortest rotation direction
        int diff = targetFaceIndex - currentFaceIndex;
        int direction = 1;

        if (diff == -1 || diff == 3) direction = -1;
        else if (diff == 1 || diff == -3) direction = 1;
        else if (diff == 2 || diff == -2) direction = 1;

        StartRotation(targetFaceIndex, direction);
    }

    /// <summary>
    /// Called by trigger zones to initiate rotation.
    /// </summary>
    public void RotateFromTrigger(int direction)
    {
        if (!triggerModeEnabled || !canRotate) return;
        if (triggerCooldownTimer > 0f) return;
        if (isRotating && !triggerCanInterrupt) return;

        if (isRotating && triggerCanInterrupt)
        {
            StopRotation();
        }

        RotateWorld(direction);
        triggerCooldownTimer = triggerCooldown;
    }

    /// <summary>
    /// Called by trigger zones to rotate to a specific face.
    /// </summary>
    public void RotateToFaceFromTrigger(int targetFaceIndex)
    {
        if (!triggerModeEnabled || !canRotate) return;
        if (triggerCooldownTimer > 0f) return;
        if (isRotating && !triggerCanInterrupt) return;

        if (isRotating && triggerCanInterrupt)
        {
            StopRotation();
        }

        RotateToFace(targetFaceIndex);
        triggerCooldownTimer = triggerCooldown;
    }

    /// <summary>
    /// Immediately sets rotation without animation.
    /// </summary>
    public void SetFaceImmediate(int faceIndex)
    {
        faceIndex = Mathf.Clamp(faceIndex, 0, 3);
        currentFaceIndex = faceIndex;
        currentAngle = faceAngles[faceIndex];
        ApplyRotationImmediate(currentAngle);
    }

    #endregion

    #region Rotation Logic

    private void StartRotation(int targetFaceIndex, int direction)
    {
        if (activeRotationCoroutine != null)
        {
            StopCoroutine(activeRotationCoroutine);
        }

        activeRotationCoroutine = StartCoroutine(RotationCoroutine(targetFaceIndex, direction));
    }

    private void StopRotation()
    {
        if (activeRotationCoroutine != null)
        {
            StopCoroutine(activeRotationCoroutine);
            activeRotationCoroutine = null;
        }
        isRotating = false;
    }

    private IEnumerator RotationCoroutine(int targetFaceIndex, int direction)
    {
        isRotating = true;
        currentFaceIndex = targetFaceIndex;

        float startAngle = currentAngle;
        float targetAngle = faceAngles[targetFaceIndex];

        // Handle wrapping (e.g., 270 -> 0 should go through 360, not back through 180)
        if (direction > 0 && targetAngle < startAngle)
        {
            targetAngle += 360f;
        }
        else if (direction < 0 && targetAngle > startAngle)
        {
            targetAngle -= 360f;
        }

        OnRotationStarted?.Invoke(targetFaceIndex);

        if (showDebugLogs)
        {
            Debug.Log($"[FezCinemachineRotation] Starting rotation: {startAngle}° -> {targetAngle}°");
        }

        float elapsed = 0f;
        while (elapsed < rotationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / rotationDuration);
            float curvedT = rotationCurve.Evaluate(t);

            currentAngle = Mathf.Lerp(startAngle, targetAngle, curvedT);
            ApplyRotation(currentAngle);

            OnRotationProgress?.Invoke(t);

            yield return null;
        }

        // Finalize
        currentAngle = targetAngle;
        NormalizeAngle();
        ApplyRotationImmediate(currentAngle);

        isRotating = false;
        activeRotationCoroutine = null;

        OnRotationCompleted?.Invoke(currentFaceIndex);

        if (showDebugLogs)
        {
            Debug.Log($"[FezCinemachineRotation] Rotation completed: face={currentFaceIndex}");
        }
    }

    private void NormalizeAngle()
    {
        while (currentAngle >= 360f) currentAngle -= 360f;
        while (currentAngle < 0f) currentAngle += 360f;
    }

    #endregion

    #region Apply Rotation Methods

    private void ApplyRotation(float angle)
    {
        switch (rotationMethod)
        {
            case RotationMethod.RotatePivotRig:
                ApplyPivotRigRotation(angle);
                break;
            case RotationMethod.OrbitalTransposer:
                ApplyOrbitalRotation(angle);
                break;
            case RotationMethod.RotateWorld:
                ApplyWorldRotation(angle);
                break;
        }
    }

    private void ApplyRotationImmediate(float angle)
    {
        ApplyRotation(angle);
    }

    private void ApplyPivotRigRotation(float angle)
    {
        if (pivotRig == null) return;
        pivotRig.rotation = Quaternion.Euler(0f, angle, 0f);
    }

    private void ApplyOrbitalRotation(float angle)
    {
#if CINEMACHINE_3 || UNITY_6000_0_OR_NEWER
        if (orbitalFollow == null) return;
        
        // In CM3, HorizontalAxis is a struct - we need to get it, modify it, and set it back
        var axis = orbitalFollow.HorizontalAxis;
        axis.Value = angle;
        orbitalFollow.HorizontalAxis = axis;
#else
        if (orbitalTransposer == null) return;
        
        orbitalTransposer.m_XAxis.Value = angle;
#endif
    }

    private void ApplyWorldRotation(float angle)
    {
        if (worldRoot == null) return;

        Vector3 center = worldRotationCenter != null 
            ? worldRotationCenter.position 
            : Vector3.zero;

        // Rotate world in opposite direction so it appears camera is rotating
        worldRoot.rotation = Quaternion.Euler(0f, -angle, 0f);
        
        // If we have a center point that's not origin, we need to adjust position
        if (worldRotationCenter != null && center != Vector3.zero)
        {
            // This keeps the center point stable while world rotates around it
            worldRoot.position = center - worldRoot.rotation * center;
        }
    }

    private void UpdatePivotFollow()
    {
        if (followTarget == null || pivotRig == null) return;
        if (isRotating && !followDuringRotation) return;

        Vector3 targetPos = new Vector3(
            followTarget.position.x,
            pivotRig.position.y,
            followTarget.position.z
        );

        pivotRig.position = Vector3.Lerp(
            pivotRig.position,
            targetPos,
            Time.deltaTime * followSpeed
        );
    }

    #endregion

    #region Public Toggles

    public void SetSystemEnabled(bool enabled)
    {
        canRotate = enabled;
        OnModeToggled?.Invoke("System", enabled);
        if (showDebugLogs) Debug.Log($"[FezCinemachineRotation] System {(enabled ? "ENABLED" : "DISABLED")}");
    }

    public void SetInputModeEnabled(bool enabled)
    {
        inputModeEnabled = enabled;
        OnModeToggled?.Invoke("InputMode", enabled);
        if (showDebugLogs) Debug.Log($"[FezCinemachineRotation] Input Mode {(enabled ? "ENABLED" : "DISABLED")}");
    }

    public void SetTriggerModeEnabled(bool enabled)
    {
        triggerModeEnabled = enabled;
        OnModeToggled?.Invoke("TriggerMode", enabled);
        if (showDebugLogs) Debug.Log($"[FezCinemachineRotation] Trigger Mode {(enabled ? "ENABLED" : "DISABLED")}");
    }

    #endregion

    #region Public Getters

    /// <summary>
    /// Gets the current forward direction (depth direction) based on rotation.
    /// </summary>
    public Vector3 GetCurrentForward()
    {
        float radians = currentAngle * Mathf.Deg2Rad;
        return new Vector3(-Mathf.Sin(radians), 0f, -Mathf.Cos(radians));
    }

    /// <summary>
    /// Gets the current right direction (movement direction) based on rotation.
    /// </summary>
    public Vector3 GetCurrentRight()
    {
        float radians = currentAngle * Mathf.Deg2Rad;
        return new Vector3(-Mathf.Cos(radians), 0f, Mathf.Sin(radians));
    }

    public int GetCurrentFaceIndex() => currentFaceIndex;
    public float GetCurrentAngle() => currentAngle;
    public bool IsRotating() => isRotating;
    public bool IsInputModeActive() => canRotate && inputModeEnabled;
    public bool IsTriggerModeActive() => canRotate && triggerModeEnabled;

    #endregion

    #region Editor Gizmos

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;

        Vector3 center = Vector3.zero;
        
        if (rotationMethod == RotationMethod.RotatePivotRig && pivotRig != null)
        {
            center = pivotRig.position;
        }
        else if (rotationMethod == RotationMethod.RotateWorld && worldRotationCenter != null)
        {
            center = worldRotationCenter.position;
        }
        else if (followTarget != null)
        {
            center = followTarget.position;
        }

        // Draw face indicators
        float radius = 3f;
        string[] faceNames = { "N (0)", "E (1)", "S (2)", "W (3)" };
        
        for (int i = 0; i < 4; i++)
        {
            float angle = faceAngles[i] * Mathf.Deg2Rad;
            Vector3 pos = center + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * radius;
            
            Gizmos.color = (i == currentFaceIndex) ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(pos, 0.3f);
            UnityEditor.Handles.Label(pos + Vector3.up * 0.5f, faceNames[i]);
        }

        // Draw current direction
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(center, GetCurrentForward() * radius);
        
        Gizmos.color = Color.red;
        Gizmos.DrawRay(center, GetCurrentRight() * radius);
    }
#endif

    #endregion
}
