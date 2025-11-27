using UnityEngine;
using System.Collections;

/// <summary>
/// Fez-style camera controller that rotates around the player.
/// Works in sync with WorldRotationController to create perspective shifts.
/// 
/// Setup:
/// 1. Attach this to your Main Camera
/// 2. Assign Player transform reference
/// 3. Set camera distance and height
/// 4. Hook up to WorldRotationController events (or call RotateCamera directly)
/// 
/// How it works:
/// - Camera orbits around player on Y-axis (left/right)
/// - Maintains fixed distance and height
/// - Always looks at player
/// - Rotates in sync with world rotation
/// </summary>
public class FezCameraController : MonoBehaviour
{
    #region Variables

    // ==================== CAMERA SETTINGS ====================
    [Header("Camera Settings")]

    /// <summary>
    /// The player transform that camera orbits around.
    /// Must be assigned in Inspector.
    /// </summary>
    [Tooltip("Player transform to orbit around")]
    public Transform player;

    /// <summary>
    /// Distance from camera to player.
    /// Typical range: 8-15 units for 2.5D platformer
    /// </summary>
    [Tooltip("Distance from camera to player")]
    public float cameraDistance = 10f;

    /// <summary>
    /// Height offset above player.
    /// Positive = camera above player, 0 = at player height
    /// Typical range: 0-5 units
    /// </summary>
    [Tooltip("Height offset above player")]
    public float cameraHeight = 2f;

    /// <summary>
    /// Current camera angle around player (in 90° increments).
    /// 0 = front, 1 = right, 2 = back, 3 = left
    /// Syncs with world rotation index.
    /// </summary>
    [Tooltip("Current camera angle index (0-3)")]
    public int currentAngleIndex = 0;

    // ==================== ROTATION SETTINGS ====================
    [Header("Rotation Settings")]

    /// <summary>
    /// Should camera rotation match world rotation speed?
    /// If TRUE, uses WorldRotationController settings
    /// If FALSE, uses custom settings below
    /// </summary>
    [Tooltip("Sync rotation speed with WorldRotationController")]
    public bool syncWithWorldRotation = true;

    /// <summary>
    /// Custom rotation duration (only used if syncWithWorldRotation = false).
    /// Typical range: 0.5-2.0 seconds
    /// </summary>
    [Tooltip("Custom rotation duration (if not syncing with world)")]
    public float customRotationDuration = 1.5f;

    /// <summary>
    /// Animation curve for camera rotation.
    /// Recommended: Smooth ease in/out
    /// </summary>
    [Tooltip("Easing curve for rotation")]
    public AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // ==================== STATE ====================
    [Header("Debug Info (Read Only)")]

    /// <summary>
    /// Is camera currently rotating?
    /// </summary>
    [Tooltip("Is camera rotating?")]
    public bool isRotating = false;

    // Private reference to world controller
    private WorldRotationController worldController;

    #endregion

    // ==================== UNITY LIFECYCLE ====================
    #region UNITY LIFECYCLE

    /// <summary>
    /// Validate setup and find world controller.
    /// </summary>
    private void Awake()
    {
        // Validate player reference
        if (player == null)
        {
            Debug.LogError("[FezCamera] Player reference not assigned! Assign player Transform in Inspector.");
        }

        // Try to find WorldRotationController
        worldController = FindObjectOfType<WorldRotationController>();

        if (worldController == null)
        {
            Debug.LogWarning("[FezCamera] WorldRotationController not found. Camera won't auto-sync with world rotation.");
        }
    }

    /// <summary>
    /// Subscribe to world rotation events.
    /// </summary>
    private void Start()
    {
        // Subscribe to world rotation events if controller exists
        if (worldController != null)
        {
            worldController.OnRotationStart.AddListener(OnWorldRotationStart);
        }

        // Position camera at initial angle
        UpdateCameraPosition();
    }

    /// <summary>
    /// Update camera position every frame to follow player.
    /// </summary>
    private void LateUpdate()
    {
        if (player != null && !isRotating)
        {
            // Smoothly follow player position (but not rotation)
            UpdateCameraPosition();
        }
    }

    /// <summary>
    /// Unsubscribe from events on destroy.
    /// </summary>
    private void OnDestroy()
    {
        if (worldController != null)
        {
            worldController.OnRotationStart.RemoveListener(OnWorldRotationStart);
        }
    }

    #endregion

    // ==================== CAMERA CONTROL ====================
    #region CAMERA CONTROL

    /// <summary>
    /// Updates camera position based on current angle index.
    /// Positions camera at fixed distance/height, always looking at player.
    /// </summary>
    private void UpdateCameraPosition()
    {
        if (player == null) return;

        // Calculate angle in degrees (0°, 90°, 180°, 270°)
        float angle = currentAngleIndex * 90f;

        // Calculate position in orbit
        Vector3 offset = Quaternion.Euler(0, angle, 0) * (Vector3.back * cameraDistance);
        Vector3 targetPosition = player.position + offset + (Vector3.up * cameraHeight);

        // Set position
        transform.position = targetPosition;

        // Look at player
        transform.LookAt(player.position + Vector3.up * cameraHeight);
    }

    /// <summary>
    /// Rotates camera 90° to the right.
    /// Public method that can be called by triggers or other scripts.
    /// </summary>
    /// <returns>True if rotation started, false if already rotating</returns>
    public bool RotateRight()
    {
        if (isRotating)
        {
            Debug.Log("[FezCamera] Camera already rotating!");
            return false;
        }

        StartCoroutine(RotateCamera(90f));
        return true;
    }

    /// <summary>
    /// Rotates camera 90° to the left.
    /// Public method that can be called by triggers or other scripts.
    /// </summary>
    /// <returns>True if rotation started, false if already rotating</returns>
    public bool RotateLeft()
    {
        if (isRotating)
        {
            Debug.Log("[FezCamera] Camera already rotating!");
            return false;
        }

        StartCoroutine(RotateCamera(-90f));
        return true;
    }

    /// <summary>
    /// Immediately snaps camera to a specific angle index.
    /// Useful for initialization or teleporting.
    /// </summary>
    /// <param name="index">Angle index (0-3)</param>
    public void SnapToAngle(int index)
    {
        currentAngleIndex = index % 4;
        if (currentAngleIndex < 0) currentAngleIndex += 4;

        UpdateCameraPosition();

        Debug.Log($"[FezCamera] Snapped to angle {currentAngleIndex} ({currentAngleIndex * 90}°)");
    }

    #endregion

    // ==================== ROTATION LOGIC ====================
    #region ROTATION LOGIC

    /// <summary>
    /// Coroutine that smoothly rotates camera around player.
    /// 
    /// Process:
    /// 1. Lock rotation flag
    /// 2. Calculate start and end positions
    /// 3. Interpolate over time
    /// 4. Update angle index
    /// 5. Unlock rotation flag
    /// </summary>
    /// <param name="angle">Rotation angle in degrees (positive = right, negative = left)</param>
    private IEnumerator RotateCamera(float angle)
    {
        if (player == null)
        {
            Debug.LogError("[FezCamera] Cannot rotate: Player reference is null!");
            yield break;
        }

        isRotating = true;

        // Calculate start angle
        float startAngle = currentAngleIndex * 90f;
        float targetAngle = startAngle + angle;

        // Determine duration
        float duration = GetRotationDuration();

        Debug.Log($"[FezCamera] Rotating from {startAngle}° to {targetAngle}° over {duration}s");

        // Smooth rotation over time
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // Calculate interpolation progress (0 to 1)
            float t = elapsed / duration;

            // Apply easing curve
            float curvedT = rotationCurve.Evaluate(t);

            // Calculate current angle
            float currentAngle = Mathf.Lerp(startAngle, targetAngle, curvedT);

            // Calculate position in orbit
            Vector3 offset = Quaternion.Euler(0, currentAngle, 0) * (Vector3.back * cameraDistance);
            Vector3 targetPosition = player.position + offset + (Vector3.up * cameraHeight);

            // Update camera position and rotation
            transform.position = targetPosition;
            transform.LookAt(player.position + Vector3.up * cameraHeight);

            yield return null;
        }

        // Update angle index
        if (angle > 0) // Rotating right
        {
            currentAngleIndex = (currentAngleIndex + 1) % 4;
        }
        else // Rotating left
        {
            currentAngleIndex--;
            if (currentAngleIndex < 0) currentAngleIndex = 3;
        }

        // Snap to exact final position
        UpdateCameraPosition();

        isRotating = false;

        Debug.Log($"[FezCamera] Rotation complete. New angle index: {currentAngleIndex}");
    }

    /// <summary>
    /// Gets rotation duration based on settings.
    /// If syncing with world, tries to match WorldRotationController speed.
    /// </summary>
    private float GetRotationDuration()
    {
        if (syncWithWorldRotation && worldController != null)
        {
            // Match world rotation speed
            switch (worldController.speedMode)
            {
                case WorldRotationController.RotationSpeedMode.Fast:
                    return 0.5f;
                case WorldRotationController.RotationSpeedMode.Smooth:
                    return 1.5f;
                case WorldRotationController.RotationSpeedMode.Custom:
                    return worldController.customRotationDuration;
                default:
                    return 1.0f;
            }
        }
        else
        {
            // Use custom duration
            return customRotationDuration;
        }
    }

    #endregion

    // ==================== EVENT HANDLERS ====================
    #region EVENT HANDLERS

    /// <summary>
    /// Called when world rotation starts.
    /// Automatically rotates camera in same direction.
    /// </summary>
    private void OnWorldRotationStart()
    {
        if (worldController == null) return;

        // Determine rotation direction based on world's last rotation
        // Note: This assumes camera follows world rotation 1:1
        // We'll rotate in the same direction the world is rotating

        // For now, we'll rotate based on the world's currentRotationIndex
        // This ensures camera stays synced
        int targetIndex = worldController.currentRotationIndex;

        // Calculate direction
        int indexDiff = targetIndex - currentAngleIndex;

        if (indexDiff == 1 || indexDiff == -3)
        {
            // Rotate right
            RotateRight();
        }
        else if (indexDiff == -1 || indexDiff == 3)
        {
            // Rotate left
            RotateLeft();
        }
    }

    #endregion

    // ==================== DEBUG ====================
    #region DEBUG

    /// <summary>
    /// Draws debug visualization in Scene view.
    /// Shows camera orbit path and look direction.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (player == null) return;

        // Draw orbit circle
        Gizmos.color = Color.cyan;
        Vector3 playerPos = player.position + Vector3.up * cameraHeight;

        // Draw circle at camera height
        int segments = 32;
        Vector3 prevPoint = playerPos + (Vector3.back * cameraDistance);

        for (int i = 1; i <= segments; i++)
        {
            float angle = (i / (float)segments) * 360f;
            Vector3 offset = Quaternion.Euler(0, angle, 0) * (Vector3.back * cameraDistance);
            Vector3 point = playerPos + offset;

            Gizmos.DrawLine(prevPoint, point);
            prevPoint = point;
        }

        // Draw current camera position
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        // Draw look direction
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, playerPos);

        // Draw angle markers (0°, 90°, 180°, 270°)
        Gizmos.color = Color.red;
        for (int i = 0; i < 4; i++)
        {
            Vector3 offset = Quaternion.Euler(0, i * 90, 0) * (Vector3.back * cameraDistance);
            Gizmos.DrawWireSphere(playerPos + offset, 0.3f);
        }
    }

    #endregion
}