using UnityEngine;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// Complete Fez-style rotation system in ONE script.
/// Rotates player and camera together for perspective shift illusion.
/// 
/// Setup:
/// 1. Attach to Main Camera
/// 2. Assign Player transform
/// 3. Create trigger zones with FezRotationTrigger
/// 
/// How Fez rotation ACTUALLY works:
/// - World geometry: NEVER rotates (stays static)
/// - Player: Rotates 90° on Y-axis (faces new direction)
/// - Camera: Orbits 90° around player (perspective change)
/// - Result: Illusion that world rotated!
/// </summary>
public class FezRotationSystem : MonoBehaviour
{
    #region Variables

    // ==================== REFERENCES ====================
    [Header("References")]

    /// <summary>
    /// Player transform to orbit around and rotate.
    /// Must be the root player GameObject (not visual child).
    /// </summary>
    [Tooltip("Player root GameObject")]
    public Transform player;

    // ==================== CAMERA SETTINGS ====================
    [Header("Camera Settings")]

    /// <summary>
    /// Distance from camera to player.
    /// Typical: 10-15 units for 2.5D view
    /// </summary>
    [Tooltip("Camera distance from player")]
    public float cameraDistance = 10f;

    /// <summary>
    /// Height above player that camera looks at.
    /// Typical: 0-3 units (chest/head height)
    /// </summary>
    [Tooltip("Camera height offset")]
    public float cameraHeight = 1.5f;

    // ==================== ROTATION SETTINGS ====================
    //[Header("Rotation Settings")]

    /// <summary>
    /// Rotation speed mode.
    /// Fast = 0.5s (arcade), Smooth = 1.5s (Fez-like)
    /// </summary>
    public enum SpeedMode { Fast, Smooth, Custom }

    [Tooltip("Rotation speed preset")]
    public SpeedMode rotationSpeed = SpeedMode.Smooth;

    /// <summary>
    /// Custom duration (only if rotationSpeed = Custom).
    /// </summary>
    [Tooltip("Custom rotation duration")]
    public float customDuration = 1.0f;

    /// <summary>
    /// Easing curve for smooth rotation.
    /// </summary>
    [Tooltip("Rotation easing curve")]
    public AnimationCurve easingCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    /// <summary>
    /// Lock player movement during rotation?
    /// FALSE = player can move during rotation (smoother)
    /// TRUE = player frozen during rotation (more controlled)
    /// </summary>
    [Tooltip("Lock player movement during rotation")]
    public bool lockPlayerDuringRotation = false;

    // ==================== AUDIO ====================
    [Header("Audio")]

    [Tooltip("Sound effect for rotation")]
    public AudioClip rotationSound;

    [Range(0f, 1f)]
    [Tooltip("Sound volume")]
    public float soundVolume = 1f;

    // ==================== EVENTS ====================
    [Header("Events")]

    public UnityEvent OnRotationStart;
    public UnityEvent OnRotationComplete;

    // ==================== STATE ====================
    [Header("Debug Info (Read Only)")]

    [Tooltip("Currently rotating?")]
    public bool isRotating = false;

    [Tooltip("Current angle index (0-3 = 0°/90°/180°/270°)")]
    public int currentAngleIndex = 0;

    #endregion

    // ==================== UNITY LIFECYCLE ====================
    #region UNITY LIFECYCLE

    private void Awake()
    {
        if (player == null)
        {
            Debug.LogError("[FezRotation] Player reference not assigned!");
        }
    }

    private void Start()
    {
        // Position camera initially
        UpdateCameraPosition();
    }

    private void LateUpdate()
    {
        // Smoothly follow player (but not during rotation)
        if (!isRotating)
        {
            UpdateCameraPosition();
        }
    }

    #endregion

    // ==================== PUBLIC METHODS ====================
    #region PUBLIC METHODS

    /// <summary>
    /// Rotates player and camera 90° to the right.
    /// </summary>
    public bool RotateRight()
    {
        if (isRotating)
        {
            Debug.Log("[FezRotation] Already rotating!");
            return false;
        }

        StartCoroutine(PerformRotation(-90f)); // Negative = right in Unity
        return true;
    }

    /// <summary>
    /// Rotates player and camera 90° to the left.
    /// </summary>
    public bool RotateLeft()
    {
        if (isRotating)
        {
            Debug.Log("[FezRotation] Already rotating!");
            return false;
        }

        StartCoroutine(PerformRotation(90f)); // Positive = left in Unity
        return true;
    }

    /// <summary>
    /// Instantly snap to angle (no animation).
    /// </summary>
    public void SnapToAngle(int angleIndex)
    {
        currentAngleIndex = angleIndex % 4;
        if (currentAngleIndex < 0) currentAngleIndex += 4;

        // Snap player rotation
        float angle = currentAngleIndex * 90f;
        player.rotation = Quaternion.Euler(0, angle, 0);

        // Snap camera position
        UpdateCameraPosition();
    }

    #endregion

    // ==================== ROTATION LOGIC ====================
    #region ROTATION LOGIC

    /// <summary>
    /// Coroutine that rotates both player and camera simultaneously.
    /// </summary>
    private IEnumerator PerformRotation(float angle)
    {
        isRotating = true;

        // Get duration
        float duration = GetDuration();

        // Store starting rotations
        Quaternion playerStartRot = player.rotation;
        Quaternion playerTargetRot = playerStartRot * Quaternion.Euler(0, angle, 0);

        float cameraStartAngle = currentAngleIndex * 90f;
        float cameraTargetAngle = cameraStartAngle + angle;

        // Play sound
        if (rotationSound != null)
        {
            AudioSource.PlayClipAtPoint(rotationSound, player.position, soundVolume);
        }

        // Trigger event
        OnRotationStart?.Invoke();

        Debug.Log($"[FezRotation] Starting rotation: {angle}° over {duration}s");

        // Animate
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float curvedT = easingCurve.Evaluate(t);

            // Rotate player
            player.rotation = Quaternion.Slerp(playerStartRot, playerTargetRot, curvedT);

            // Rotate camera orbit
            float currentCameraAngle = Mathf.Lerp(cameraStartAngle, cameraTargetAngle, curvedT);
            UpdateCameraPosition(currentCameraAngle);

            yield return null;
        }

        // Snap to final
        player.rotation = playerTargetRot;

        // Update angle index
        if (angle > 0) // Left
        {
            currentAngleIndex = (currentAngleIndex + 1) % 4;
        }
        else // Right
        {
            currentAngleIndex--;
            if (currentAngleIndex < 0) currentAngleIndex = 3;
        }

        UpdateCameraPosition();

        // Trigger event
        OnRotationComplete?.Invoke();

        isRotating = false;

        Debug.Log($"[FezRotation] Rotation complete. New angle: {currentAngleIndex * 90}°");
    }

    /// <summary>
    /// Updates camera position based on angle.
    /// </summary>
    private void UpdateCameraPosition(float? customAngle = null)
    {
        if (player == null) return;

        float angle = customAngle ?? (currentAngleIndex * 90f);

        // Calculate orbit position
        Vector3 offset = Quaternion.Euler(0, angle, 0) * (Vector3.back * cameraDistance);
        Vector3 lookPoint = player.position + Vector3.up * cameraHeight;

        transform.position = lookPoint + offset;
        transform.LookAt(lookPoint);
    }

    /// <summary>
    /// Gets rotation duration based on speed mode.
    /// </summary>
    private float GetDuration()
    {
        switch (rotationSpeed)
        {
            case SpeedMode.Fast: return 0.5f;
            case SpeedMode.Smooth: return 1.5f;
            case SpeedMode.Custom: return customDuration;
            default: return 1.0f;
        }
    }

    #endregion

    // ==================== DEBUG ====================
    #region DEBUG

    private void OnDrawGizmosSelected()
    {
        if (player == null) return;

        // Draw orbit circle
        Gizmos.color = Color.cyan;
        Vector3 center = player.position + Vector3.up * cameraHeight;

        int segments = 32;
        Vector3 prevPoint = center + Vector3.back * cameraDistance;

        for (int i = 1; i <= segments; i++)
        {
            float angle = (i / (float)segments) * 360f;
            Vector3 offset = Quaternion.Euler(0, angle, 0) * (Vector3.back * cameraDistance);
            Gizmos.DrawLine(prevPoint, center + offset);
            prevPoint = center + offset;
        }

        // Draw 4 angle positions
        Gizmos.color = Color.yellow;
        for (int i = 0; i < 4; i++)
        {
            Vector3 offset = Quaternion.Euler(0, i * 90, 0) * (Vector3.back * cameraDistance);
            Gizmos.DrawWireSphere(center + offset, 0.5f);
        }

        // Draw current position
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }

    #endregion
}