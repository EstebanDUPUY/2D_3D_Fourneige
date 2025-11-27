using UnityEngine;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// Fez-style world rotation controller.
/// Rotates the entire world 90° left or right when triggered.
/// 
/// Setup Instructions:
/// 1. Create empty GameObject called "World" as parent of all rotating objects
/// 2. Place all level geometry, platforms, obstacles as children of "World"
/// 3. Keep Player and Camera OUTSIDE of "World" hierarchy (they stay fixed)
/// 4. Attach this script to "World" GameObject
/// 5. Set up trigger zones with CameraRotationTrigger script
/// 
/// Important:
/// - Player must NOT be child of World (or they'll rotate with it)
/// - Camera must NOT be child of World
/// - Ground/Wall detection will work because we rotate World, not Player
/// </summary>
public class WorldRotationController : MonoBehaviour
{
    #region Variables

    // ==================== ROTATION SETTINGS ====================
    //[Header("Rotation Settings")]

    /// <summary>
    /// Rotation speed mode.
    /// Fast: 0.5 seconds (snappy, arcade feel)
    /// Smooth: 1.5 seconds (cinematic, Fez-like)
    /// Custom: Use customRotationDuration
    /// </summary>
    public enum RotationSpeedMode { Fast, Smooth, Custom }

    [Tooltip("Choose rotation speed: Fast (0.5s), Smooth (1.5s), or Custom")]
    public RotationSpeedMode speedMode = RotationSpeedMode.Smooth;

    /// <summary>
    /// Custom rotation duration in seconds.
    /// Only used if speedMode is set to Custom.
    /// Typical range: 0.3-3.0 seconds
    /// </summary>
    [Tooltip("Custom rotation duration (only used if Speed Mode = Custom)")]
    public float customRotationDuration = 1.0f;

    /// <summary>
    /// Easing curve for rotation animation.
    /// Recommended: EaseInOutQuad for smooth acceleration/deceleration
    /// Linear for constant speed
    /// </summary>
    [Tooltip("Animation curve for rotation smoothness")]
    public AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // ==================== VISUAL EFFECTS ====================
    [Header("Visual Effects")]

    /// <summary>
    /// Particle effect prefab to spawn during rotation.
    /// Optional. Leave empty for no particles.
    /// Example: Dust clouds, sparkles, dimensional shift effect
    /// </summary>
    [Tooltip("Optional particle effect spawned at rotation start")]
    public GameObject rotationParticlePrefab;

    /// <summary>
    /// Where to spawn particles relative to world center.
    /// If null, spawns at this GameObject's position.
    /// </summary>
    [Tooltip("Optional transform for particle spawn location")]
    public Transform particleSpawnPoint;

    /// <summary>
    /// How long particles live before auto-destroying (in seconds).
    /// Set to 0 for no auto-destroy.
    /// Typical range: 1-3 seconds
    /// </summary>
    [Tooltip("Particle lifetime in seconds (0 = no auto-destroy)")]
    public float particleLifetime = 2f;

    // ==================== AUDIO ====================
    [Header("Audio")]

    /// <summary>
    /// Audio clip to play when rotation starts.
    /// Optional. Leave empty for no sound.
    /// Tip: Use a mechanical "chunk" sound like Fez's rotation SFX
    /// </summary>
    [Tooltip("Sound effect played when rotation starts")]
    public AudioClip rotationSound;

    /// <summary>
    /// Volume of rotation sound effect (0-1).
    /// </summary>
    [Range(0f, 1f)]
    [Tooltip("Volume of rotation sound")]
    public float soundVolume = 1f;

    // ==================== EVENTS ====================
    [Header("Events")]

    /// <summary>
    /// Unity Event triggered when rotation starts.
    /// Use this to hook up custom behavior (UI updates, gameplay changes, etc.)
    /// </summary>
    [Tooltip("Event fired when rotation begins")]
    public UnityEvent OnRotationStart;

    /// <summary>
    /// Unity Event triggered when rotation completes.
    /// Use this to resume gameplay, update UI, etc.
    /// </summary>
    [Tooltip("Event fired when rotation completes")]
    public UnityEvent OnRotationComplete;

    // ==================== STATE TRACKING ====================
    [Header("Debug Info (Read Only)")]

    /// <summary>
    /// Is world currently rotating?
    /// TRUE during rotation animation, FALSE when idle.
    /// Public so triggers can check before activating.
    /// </summary>
    [Tooltip("Is world currently rotating?")]
    public bool isRotating = false;

    /// <summary>
    /// Current world rotation in 90° increments.
    /// 0 = default, 1 = 90°, 2 = 180°, 3 = 270°, then wraps to 0
    /// Used to track which "side" of the world is facing forward.
    /// </summary>
    [Tooltip("Current rotation index (0-3, representing 0°/90°/180°/270°)")]
    public int currentRotationIndex = 0;

    #endregion

    // ==================== PUBLIC METHODS ====================
    #region PUBLIC METHODS

    /// <summary>
    /// Rotates the world 90° to the right (clockwise from above).
    /// Called by CameraRotationTrigger or other scripts.
    /// 
    /// Returns false if rotation is already in progress.
    /// </summary>
    /// <returns>True if rotation started, false if already rotating</returns>
    public bool RotateRight()
    {
        if (isRotating)
        {
            Debug.Log("World is already rotating!");
            return false;
        }

        StartCoroutine(RotateWorld(90f));
        return true;
    }

    /// <summary>
    /// Rotates the world 90° to the left (counter-clockwise from above).
    /// Called by CameraRotationTrigger or other scripts.
    /// 
    /// Returns false if rotation is already in progress.
    /// </summary>
    /// <returns>True if rotation started, false if already rotating</returns>
    public bool RotateLeft()
    {
        if (isRotating)
        {
            Debug.Log("World is already rotating!");
            return false;
        }

        StartCoroutine(RotateWorld(-90f));
        return true;
    }

    /// <summary>
    /// Immediately snaps world to a specific rotation index (0-3).
    /// Useful for setting initial world orientation or teleporting.
    /// Does NOT play animation or effects.
    /// </summary>
    /// <param name="index">Rotation index (0=0°, 1=90°, 2=180°, 3=270°)</param>
    public void SnapToRotation(int index)
    {
        currentRotationIndex = index % 4;
        if (currentRotationIndex < 0) currentRotationIndex += 4;

        float targetAngle = currentRotationIndex * 90f;
        transform.rotation = Quaternion.Euler(0, targetAngle, 0);

        Debug.Log($"World snapped to rotation index {currentRotationIndex} ({targetAngle}°)");
    }

    #endregion

    // ==================== ROTATION LOGIC ====================
    #region ROTATION LOGIC

    /// <summary>
    /// Coroutine that performs smooth world rotation over time.
    /// 
    /// Process:
    /// 1. Lock rotation flag
    /// 2. Trigger start events/effects
    /// 3. Smoothly interpolate from current to target rotation
    /// 4. Snap to exact target (prevent floating point drift)
    /// 5. Update rotation index
    /// 6. Trigger completion events
    /// 7. Unlock rotation flag
    /// </summary>
    /// <param name="angle">Rotation angle in degrees (positive = right, negative = left)</param>
    private IEnumerator RotateWorld(float angle)
    {
        // Lock rotation
        isRotating = true;

        // Calculate target rotation
        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = startRotation * Quaternion.Euler(0, angle, 0);

        // Determine duration based on speed mode
        float duration = GetRotationDuration();

        // Trigger effects and events
        TriggerRotationEffects();
        OnRotationStart?.Invoke();

        // Smooth rotation over time
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // Calculate interpolation progress (0 to 1)
            float t = elapsed / duration;

            // Apply easing curve
            float curvedT = rotationCurve.Evaluate(t);

            // Interpolate rotation
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, curvedT);

            yield return null;
        }

        // Snap to exact target rotation (prevent drift)
        transform.rotation = targetRotation;

        // Update rotation index
        if (angle > 0) // Rotating right
        {
            currentRotationIndex = (currentRotationIndex + 1) % 4;
        }
        else // Rotating left
        {
            currentRotationIndex--;
            if (currentRotationIndex < 0) currentRotationIndex = 3;
        }

        // Trigger completion events
        OnRotationComplete?.Invoke();

        // Unlock rotation
        isRotating = false;

        Debug.Log($"World rotation complete. New index: {currentRotationIndex} ({currentRotationIndex * 90}°)");
    }

    /// <summary>
    /// Gets rotation duration based on current speed mode.
    /// Fast = 0.5s, Smooth = 1.5s, Custom = user-defined
    /// </summary>
    private float GetRotationDuration()
    {
        switch (speedMode)
        {
            case RotationSpeedMode.Fast:
                return 0.5f;
            case RotationSpeedMode.Smooth:
                return 1.5f;
            case RotationSpeedMode.Custom:
                return Mathf.Max(0.1f, customRotationDuration); // Minimum 0.1s
            default:
                return 1.0f;
        }
    }

    /// <summary>
    /// Triggers all visual and audio effects for rotation.
    /// Spawns particles and plays sound if configured.
    /// </summary>
    private void TriggerRotationEffects()
    {
        // Spawn particle effect
        if (rotationParticlePrefab != null)
        {
            Vector3 spawnPosition = particleSpawnPoint != null
                ? particleSpawnPoint.position
                : transform.position;

            GameObject particles = Instantiate(rotationParticlePrefab, spawnPosition, Quaternion.identity);

            // Auto-destroy particles after lifetime
            if (particleLifetime > 0)
            {
                Destroy(particles, particleLifetime);
            }
        }

        // Play rotation sound
        if (rotationSound != null)
        {
            // Use AudioSource.PlayClipAtPoint for one-shot sound
            AudioSource.PlayClipAtPoint(rotationSound, transform.position, soundVolume);
        }
    }

    #endregion

    // ==================== DEBUG ====================
    #region DEBUG

    /// <summary>
    /// Draws debug gizmos in Scene view.
    /// Shows world center and rotation axis.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Draw Y-axis (rotation axis)
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 5f);

        // Draw current forward direction
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 3f);

        // Draw rotation direction indicators
        Gizmos.color = Color.yellow;

        // Right arrow (90° rotation indicator)
        Vector3 rightDir = Quaternion.Euler(0, 90, 0) * transform.forward;
        Gizmos.DrawLine(transform.position, transform.position + rightDir * 2f);

        // Left arrow (-90° rotation indicator)
        Vector3 leftDir = Quaternion.Euler(0, -90, 0) * transform.forward;
        Gizmos.DrawLine(transform.position, transform.position + leftDir * 2f);
    }

    #endregion
}