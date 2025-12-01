using UnityEngine;
using System;

/// <summary>
/// Bridge class that provides unified access to whichever rotation controller is active.
/// This allows all other scripts (_FezPlayerController, _FezDepthSnapper, etc.) to work
/// seamlessly with either _WorldRotationController or FezCinemachineRotation.
/// 
/// Usage:
/// - Call FezRotationBridge.Initialize() at game start (optional - auto-initializes on first access)
/// - Use FezRotationBridge.Instance for queries and methods
/// - Subscribe to FezRotationBridge.OnRotationStarted, etc. for events
/// 
/// The bridge automatically prefers FezCinemachineRotation if both controllers exist.
/// </summary>
public class FezRotationBridge : MonoBehaviour
{
    #region Singleton

    private static FezRotationBridge _instance;
    public static FezRotationBridge Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to find existing
                _instance = FindObjectOfType<FezRotationBridge>();
                
                if (_instance == null)
                {
                    // Auto-create
                    GameObject bridgeObj = new GameObject("FezRotationBridge");
                    _instance = bridgeObj.AddComponent<FezRotationBridge>();
                    DontDestroyOnLoad(bridgeObj);
                }
            }
            return _instance;
        }
    }

    #endregion

    #region Events

    /// <summary>Fired when rotation begins. Parameter: new face index (0-3)</summary>
    public static event Action<int> OnRotationStarted;
    
    /// <summary>Fired when rotation completes. Parameter: final face index (0-3)</summary>
    public static event Action<int> OnRotationCompleted;
    
    /// <summary>Fired every frame during rotation. Parameter: progress (0-1)</summary>
    public static event Action<float> OnRotationProgress;

    #endregion

    #region Controller References

    private _WorldRotationController originalController;
    private FezCinemachineRotation cinemachineController;
    
    /// <summary>Which controller type is currently active</summary>
    public enum ActiveControllerType { None, Original, Cinemachine }
    
    [SerializeField] private ActiveControllerType activeType = ActiveControllerType.None;
    
    /// <summary>Returns which controller type is being used</summary>
    public ActiveControllerType GetActiveControllerType() => activeType;
    
    /// <summary>Returns true if any rotation controller is available</summary>
    public bool IsAvailable => activeType != ActiveControllerType.None;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DetectControllers();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
        
        if (_instance == this)
        {
            _instance = null;
        }
    }

    #endregion

    #region Controller Detection

    /// <summary>
    /// Manually triggers controller detection. Call this if controllers are added at runtime.
    /// </summary>
    public void DetectControllers()
    {
        UnsubscribeFromEvents();
        
        // Find controllers
        cinemachineController = FezCinemachineRotation.Instance;
        if (cinemachineController == null)
        {
            cinemachineController = FindObjectOfType<FezCinemachineRotation>();
        }
        
        originalController = _WorldRotationController.Instance;
        if (originalController == null)
        {
            originalController = FindObjectOfType<_WorldRotationController>();
        }
        
        // Determine which to use (prefer Cinemachine if both exist)
        if (cinemachineController != null)
        {
            activeType = ActiveControllerType.Cinemachine;
            SubscribeToCinemachine();
            Debug.Log("[FezRotationBridge] Using FezCinemachineRotation");
        }
        else if (originalController != null)
        {
            activeType = ActiveControllerType.Original;
            SubscribeToOriginal();
            Debug.Log("[FezRotationBridge] Using _WorldRotationController");
        }
        else
        {
            activeType = ActiveControllerType.None;
            Debug.LogWarning("[FezRotationBridge] No rotation controller found in scene!");
        }
    }

    /// <summary>
    /// Forces the bridge to use a specific controller type.
    /// </summary>
    public void ForceControllerType(ActiveControllerType type)
    {
        UnsubscribeFromEvents();
        
        switch (type)
        {
            case ActiveControllerType.Cinemachine:
                if (cinemachineController != null)
                {
                    activeType = ActiveControllerType.Cinemachine;
                    SubscribeToCinemachine();
                }
                break;
                
            case ActiveControllerType.Original:
                if (originalController != null)
                {
                    activeType = ActiveControllerType.Original;
                    SubscribeToOriginal();
                }
                break;
                
            case ActiveControllerType.None:
                activeType = ActiveControllerType.None;
                break;
        }
    }

    private void SubscribeToCinemachine()
    {
        if (cinemachineController == null) return;
        
        cinemachineController.OnRotationStarted += HandleRotationStarted;
        cinemachineController.OnRotationCompleted += HandleRotationCompleted;
        cinemachineController.OnRotationProgress += HandleRotationProgress;
    }

    private void SubscribeToOriginal()
    {
        if (originalController == null) return;
        
        originalController.OnRotationStarted += HandleRotationStarted;
        originalController.OnRotationCompleted += HandleRotationCompleted;
        originalController.OnRotationProgress += HandleRotationProgress;
    }

    private void UnsubscribeFromEvents()
    {
        if (cinemachineController != null)
        {
            cinemachineController.OnRotationStarted -= HandleRotationStarted;
            cinemachineController.OnRotationCompleted -= HandleRotationCompleted;
            cinemachineController.OnRotationProgress -= HandleRotationProgress;
        }
        
        if (originalController != null)
        {
            originalController.OnRotationStarted -= HandleRotationStarted;
            originalController.OnRotationCompleted -= HandleRotationCompleted;
            originalController.OnRotationProgress -= HandleRotationProgress;
        }
    }

    #endregion

    #region Event Handlers

    private void HandleRotationStarted(int faceIndex)
    {
        OnRotationStarted?.Invoke(faceIndex);
    }

    private void HandleRotationCompleted(int faceIndex)
    {
        OnRotationCompleted?.Invoke(faceIndex);
    }

    private void HandleRotationProgress(float progress)
    {
        OnRotationProgress?.Invoke(progress);
    }

    #endregion

    #region Rotation Commands

    /// <summary>
    /// Rotates the world by 90 degrees.
    /// </summary>
    /// <param name="direction">-1 for left, 1 for right</param>
    public void RotateWorld(int direction)
    {
        switch (activeType)
        {
            case ActiveControllerType.Cinemachine:
                cinemachineController?.RotateWorld(direction);
                break;
            case ActiveControllerType.Original:
                originalController?.RotateWorld(direction);
                break;
        }
    }

    /// <summary>
    /// Rotates to a specific face index.
    /// </summary>
    public void RotateToFace(int faceIndex)
    {
        switch (activeType)
        {
            case ActiveControllerType.Cinemachine:
                cinemachineController?.RotateToFace(faceIndex);
                break;
            case ActiveControllerType.Original:
                originalController?.RotateToFace(faceIndex);
                break;
        }
    }

    /// <summary>
    /// Called by trigger zones.
    /// </summary>
    public void RotateFromTrigger(int direction)
    {
        switch (activeType)
        {
            case ActiveControllerType.Cinemachine:
                cinemachineController?.RotateFromTrigger(direction);
                break;
            case ActiveControllerType.Original:
                originalController?.RotateFromTrigger(direction);
                break;
        }
    }

    /// <summary>
    /// Called by trigger zones to rotate to specific face.
    /// </summary>
    public void RotateToFaceFromTrigger(int faceIndex)
    {
        switch (activeType)
        {
            case ActiveControllerType.Cinemachine:
                cinemachineController?.RotateToFaceFromTrigger(faceIndex);
                break;
            case ActiveControllerType.Original:
                originalController?.RotateToFaceFromTrigger(faceIndex);
                break;
        }
    }

    #endregion

    #region Queries

    /// <summary>
    /// Returns true if world is currently rotating.
    /// </summary>
    public bool IsRotating()
    {
        switch (activeType)
        {
            case ActiveControllerType.Cinemachine:
                return cinemachineController?.IsRotating() ?? false;
            case ActiveControllerType.Original:
                return originalController?.IsRotating() ?? false;
            default:
                return false;
        }
    }

    /// <summary>
    /// Gets the current face index (0-3).
    /// </summary>
    public int GetCurrentFaceIndex()
    {
        switch (activeType)
        {
            case ActiveControllerType.Cinemachine:
                return cinemachineController?.GetCurrentFaceIndex() ?? 0;
            case ActiveControllerType.Original:
                return originalController?.GetCurrentFaceIndex() ?? 0;
            default:
                return 0;
        }
    }

    /// <summary>
    /// Gets the current rotation angle (0-360).
    /// </summary>
    public float GetCurrentAngle()
    {
        switch (activeType)
        {
            case ActiveControllerType.Cinemachine:
                return cinemachineController?.GetCurrentAngle() ?? 0f;
            case ActiveControllerType.Original:
                return originalController?.GetCurrentAngle() ?? 0f;
            default:
                return 0f;
        }
    }

    /// <summary>
    /// Gets the current forward (depth) direction based on camera rotation.
    /// </summary>
    public Vector3 GetCurrentForward()
    {
        switch (activeType)
        {
            case ActiveControllerType.Cinemachine:
                return cinemachineController?.GetCurrentForward() ?? Vector3.forward;
            case ActiveControllerType.Original:
                return originalController?.GetCurrentForward() ?? Vector3.forward;
            default:
                return Vector3.forward;
        }
    }

    /// <summary>
    /// Gets the current right (movement) direction based on camera rotation.
    /// </summary>
    public Vector3 GetCurrentRight()
    {
        switch (activeType)
        {
            case ActiveControllerType.Cinemachine:
                return cinemachineController?.GetCurrentRight() ?? Vector3.right;
            case ActiveControllerType.Original:
                return originalController?.GetCurrentRight() ?? Vector3.right;
            default:
                return Vector3.right;
        }
    }

    /// <summary>
    /// Returns true if input mode is active.
    /// </summary>
    public bool IsInputModeActive()
    {
        switch (activeType)
        {
            case ActiveControllerType.Cinemachine:
                return cinemachineController?.IsInputModeActive() ?? false;
            case ActiveControllerType.Original:
                return originalController?.IsInputModeActive() ?? false;
            default:
                return false;
        }
    }

    /// <summary>
    /// Returns true if trigger mode is active.
    /// </summary>
    public bool IsTriggerModeActive()
    {
        switch (activeType)
        {
            case ActiveControllerType.Cinemachine:
                return cinemachineController?.IsTriggerModeActive() ?? false;
            case ActiveControllerType.Original:
                return originalController?.IsTriggerModeActive() ?? false;
            default:
                return false;
        }
    }

    #endregion

    #region System Controls

    /// <summary>
    /// Enables or disables the entire rotation system.
    /// </summary>
    public void SetSystemEnabled(bool enabled)
    {
        switch (activeType)
        {
            case ActiveControllerType.Cinemachine:
                cinemachineController?.SetSystemEnabled(enabled);
                break;
            case ActiveControllerType.Original:
                originalController?.SetSystemEnabled(enabled);
                break;
        }
    }

    /// <summary>
    /// Enables or disables input-based rotation.
    /// </summary>
    public void SetInputModeEnabled(bool enabled)
    {
        switch (activeType)
        {
            case ActiveControllerType.Cinemachine:
                cinemachineController?.SetInputModeEnabled(enabled);
                break;
            case ActiveControllerType.Original:
                originalController?.SetInputModeEnabled(enabled);
                break;
        }
    }

    /// <summary>
    /// Enables or disables trigger-based rotation.
    /// </summary>
    public void SetTriggerModeEnabled(bool enabled)
    {
        switch (activeType)
        {
            case ActiveControllerType.Cinemachine:
                cinemachineController?.SetTriggerModeEnabled(enabled);
                break;
            case ActiveControllerType.Original:
                originalController?.SetTriggerModeEnabled(enabled);
                break;
        }
    }

    #endregion

    #region Direct Controller Access

    /// <summary>
    /// Gets direct reference to the original controller (may be null).
    /// </summary>
    public _WorldRotationController GetOriginalController() => originalController;

    /// <summary>
    /// Gets direct reference to the Cinemachine controller (may be null).
    /// </summary>
    public FezCinemachineRotation GetCinemachineController() => cinemachineController;

    #endregion

    #region Static Helpers

    /// <summary>
    /// Static helper to check if rotating (creates instance if needed).
    /// </summary>
    public static bool IsWorldRotating()
    {
        return Instance.IsRotating();
    }

    /// <summary>
    /// Static helper to get current forward direction.
    /// </summary>
    public static Vector3 GetWorldForward()
    {
        return Instance.GetCurrentForward();
    }

    /// <summary>
    /// Static helper to get current right direction.
    /// </summary>
    public static Vector3 GetWorldRight()
    {
        return Instance.GetCurrentRight();
    }

    /// <summary>
    /// Static helper to get current face index.
    /// </summary>
    public static int GetFaceIndex()
    {
        return Instance.GetCurrentFaceIndex();
    }

    #endregion
}
