using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Handles depth snapping after world rotation.
/// This is the "magic" that makes Fez's rotation work - when the world rotates,
/// platforms that were at different depths can become aligned, and the player
/// needs to snap to valid positions.
/// 
/// Core Systems:
/// - Raycasts in depth direction to find valid platforms
/// - Snaps player to nearest valid surface after rotation
/// - Configurable delay for visual effect timing
/// - Debug visualization for level design
/// 
/// Usage:
/// 1. Attach to a manager GameObject in your scene
/// 2. Assign player and rotation controller references
/// 3. Configure platform layer mask
/// 4. Adjust snap settings as needed
/// </summary>
public class _FezDepthSnapper : MonoBehaviour
{
    #region Variables

    // ==================== REFERENCES ====================
    #region REFERENCES

    [Header("References")]

    [Tooltip("Reference to the world rotation controller. Auto-finds if null")]
    public _WorldRotationController worldRotation;

    [Tooltip("The player's transform. Auto-finds _FezPlayerController if null")]
    public Transform playerTransform;

    [Tooltip("The player's rigidbody for position updates")]
    public Rigidbody playerRigidbody;

    #endregion

    // ==================== MASTER CONTROLS ====================
    #region MASTER CONTROLS

    [Header("Master Controls")]

    /// <summary>
    /// Master toggle - if FALSE, disables all depth snapping.
    /// Useful for debugging or specific gameplay sections.
    /// </summary>
    [Tooltip("Enable/disable depth snapping")]
    public bool snapEnabled = true;

    /// <summary>
    /// If TRUE, snapping happens automatically after rotation.
    /// If FALSE, snapping must be triggered manually via SnapPlayer().
    /// </summary>
    [Tooltip("Automatically snap after rotation completes")]
    public bool autoSnapAfterRotation = true;

    #endregion

    // ==================== SNAP SETTINGS ====================
    #region SNAP SETTINGS

    [Header("Snap Settings")]

    /// <summary>
    /// Maximum distance to search for valid platforms in depth direction.
    /// Larger values = more forgiving but potentially unexpected snaps.
    /// </summary>
    [Tooltip("Max distance to search for platforms")]
    public float maxSnapDistance = 10f;

    /// <summary>
    /// Layers to consider as valid platforms for snapping.
    /// Should match your ground/platform layers.
    /// </summary>
    [Tooltip("Layers considered as platforms")]
    public LayerMask platformLayer = ~0;

    /// <summary>
    /// Small offset to apply when snapping to prevent clipping.
    /// </summary>
    [Tooltip("Offset from surface when snapping")]
    public float snapOffset = 0.1f;

    /// <summary>
    /// Delay after rotation before snapping occurs.
    /// Allows rotation animation to complete first for better visuals.
    /// </summary>
    [Tooltip("Delay before snapping (for visual timing)")]
    public float snapDelay = 0.1f;

    /// <summary>
    /// If TRUE, smoothly interpolates to snap position.
    /// If FALSE, instantly teleports to snap position.
    /// </summary>
    [Tooltip("Smooth transition to snap position")]
    public bool smoothSnap = false;

    /// <summary>
    /// Duration of smooth snap transition.
    /// Only used when smoothSnap is TRUE.
    /// </summary>
    [Tooltip("Duration of smooth snap")]
    public float smoothSnapDuration = 0.2f;

    #endregion

    // ==================== DEBUG ====================
    #region DEBUG

    [Header("Debug")]

    [Tooltip("Show debug info in console")]
    public bool showDebugLogs = false;

    [Tooltip("Draw debug rays in scene view")]
    public bool showDebugRays = true;

    [Tooltip("Duration to show debug rays")]
    public float debugRayDuration = 2f;

    #endregion

    // ==================== PRIVATE VARIABLES ====================
    #region PRIVATE VARIABLES

    // Coroutine reference for delayed/smooth snap
    private Coroutine activeSnapCoroutine;

    #endregion

    #endregion

    // ==================== UNITY LIFECYCLE ====================
    #region UNITY LIFECYCLE

    private void Start()
    {
        InitializeReferences();
        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    #endregion

    // ==================== INITIALIZATION ====================
    #region INITIALIZATION

    /// <summary>
    /// Finds and assigns missing references.
    /// </summary>
    private void InitializeReferences()
    {
        // Find rotation controller
        if (worldRotation == null)
        {
            worldRotation = _WorldRotationController.Instance;

            if (worldRotation == null)
            {
                Debug.LogError("[_FezDepthSnapper] No _WorldRotationController found!");
            }
        }

        // Find player
        if (playerTransform == null)
        {
            var playerController = FindObjectOfType<_FezPlayerController>();
            if (playerController != null)
            {
                playerTransform = playerController.transform;
                playerRigidbody = playerController.GetComponent<Rigidbody>();

                if (showDebugLogs)
                {
                    Debug.Log("[_FezDepthSnapper] Auto-assigned player references");
                }
            }
            else
            {
                Debug.LogWarning("[_FezDepthSnapper] No player found. Assign manually.");
            }
        }
        else if (playerRigidbody == null)
        {
            playerRigidbody = playerTransform.GetComponent<Rigidbody>();
        }
    }

    /// <summary>
    /// Subscribes to rotation controller events.
    /// </summary>
    private void SubscribeToEvents()
    {
        if (worldRotation != null)
        {
            worldRotation.OnRotationCompleted += OnRotationCompleted;
        }
    }

    /// <summary>
    /// Unsubscribes from rotation controller events.
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        if (worldRotation != null)
        {
            worldRotation.OnRotationCompleted -= OnRotationCompleted;
        }
    }

    #endregion

    // ==================== EVENT CALLBACKS ====================
    #region EVENT CALLBACKS

    /// <summary>
    /// Called when world rotation completes.
    /// Initiates snap process if auto-snap is enabled.
    /// </summary>
    private void OnRotationCompleted(int newFaceIndex)
    {
        if (!snapEnabled || !autoSnapAfterRotation)
        {
            return;
        }

        // Stop any existing snap
        if (activeSnapCoroutine != null)
        {
            StopCoroutine(activeSnapCoroutine);
        }

        // Start snap with delay
        if (snapDelay > 0f)
        {
            activeSnapCoroutine = StartCoroutine(DelayedSnapCoroutine(newFaceIndex));
        }
        else
        {
            SnapPlayerToValidPosition(newFaceIndex);
        }
    }

    #endregion

    // ==================== SNAP LOGIC ====================
    #region SNAP LOGIC

    /// <summary>
    /// Coroutine for delayed snap.
    /// </summary>
    private System.Collections.IEnumerator DelayedSnapCoroutine(int faceIndex)
    {
        yield return new WaitForSeconds(snapDelay);
        SnapPlayerToValidPosition(faceIndex);
        activeSnapCoroutine = null;
    }

    /// <summary>
    /// Main snap logic. Finds valid position and moves player.
    /// </summary>
    public void SnapPlayerToValidPosition(int faceIndex)
    {
        if (!snapEnabled || playerTransform == null || worldRotation == null)
        {
            return;
        }

        Vector3 currentPos = playerTransform.position;
        Vector3 depthDirection = worldRotation.GetCurrentForward();

        // Find valid snap position
        Vector3? snapPosition = FindSnapPosition(currentPos, depthDirection);

        if (snapPosition.HasValue)
        {
            ApplySnap(snapPosition.Value);

            if (showDebugLogs)
            {
                Debug.Log($"[_FezDepthSnapper] Snapped player to {snapPosition.Value}");
            }
        }
        else
        {
            if (showDebugLogs)
            {
                Debug.Log("[_FezDepthSnapper] No valid snap position found");
            }
        }
    }

    /// <summary>
    /// Searches for valid snap position using raycasts.
    /// </summary>
    private Vector3? FindSnapPosition(Vector3 currentPos, Vector3 depthDirection)
    {
        // First check: is current position valid?
        if (IsPositionValid(currentPos))
        {
            return null; // No snap needed
        }

        List<Vector3> candidates = new List<Vector3>();

        // Cast rays in both depth directions
        SearchForPlatforms(currentPos, depthDirection, candidates);
        SearchForPlatforms(currentPos, -depthDirection, candidates);

        // Debug visualization
        if (showDebugRays)
        {
            Debug.DrawRay(currentPos, depthDirection * maxSnapDistance, Color.green, debugRayDuration);
            Debug.DrawRay(currentPos, -depthDirection * maxSnapDistance, Color.red, debugRayDuration);
        }

        // If no direct hits, search for ground below at various depths
        if (candidates.Count == 0)
        {
            SearchForGround(currentPos, depthDirection, candidates);
        }

        // Return nearest valid position
        if (candidates.Count > 0)
        {
            candidates.Sort((a, b) =>
                Vector3.Distance(currentPos, a).CompareTo(Vector3.Distance(currentPos, b)));

            if (showDebugRays)
            {
                Debug.DrawLine(currentPos, candidates[0], Color.yellow, debugRayDuration);
            }

            return candidates[0];
        }

        return null;
    }

    /// <summary>
    /// Searches for platforms in a given direction.
    /// </summary>
    private void SearchForPlatforms(Vector3 origin, Vector3 direction, List<Vector3> candidates)
    {
        RaycastHit[] hits = Physics.RaycastAll(origin, direction, maxSnapDistance, platformLayer);

        foreach (var hit in hits)
        {
            // Calculate candidate position (offset from surface)
            Vector3 candidate = hit.point - direction * snapOffset;

            // Verify this position is valid (has ground below)
            if (IsPositionValid(candidate))
            {
                candidates.Add(candidate);
            }
        }
    }

    /// <summary>
    /// Searches for ground at various depth positions.
    /// </summary>
    private void SearchForGround(Vector3 currentPos, Vector3 depthDirection, List<Vector3> candidates)
    {
        float searchStep = 0.5f;

        for (float depth = -maxSnapDistance; depth <= maxSnapDistance; depth += searchStep)
        {
            Vector3 searchPos = currentPos + depthDirection * depth;

            // Raycast down to find ground
            if (Physics.Raycast(searchPos + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 10f, platformLayer))
            {
                Vector3 candidate = hit.point + Vector3.up * snapOffset;

                // Check if this position is reasonable (not too far vertically)
                if (Mathf.Abs(candidate.y - currentPos.y) < 3f)
                {
                    candidates.Add(candidate);
                }
            }
        }
    }

    /// <summary>
    /// Checks if a position is valid (has ground below).
    /// </summary>
    private bool IsPositionValid(Vector3 position)
    {
        return Physics.Raycast(position + Vector3.up * 0.5f, Vector3.down, 1f, platformLayer);
    }

    /// <summary>
    /// Applies the snap to the target position.
    /// </summary>
    private void ApplySnap(Vector3 targetPosition)
    {
        if (smoothSnap)
        {
            StartCoroutine(SmoothSnapCoroutine(targetPosition));
        }
        else
        {
            // Instant snap
            if (playerRigidbody != null)
            {
                playerRigidbody.position = targetPosition;
            }
            else
            {
                playerTransform.position = targetPosition;
            }
        }
    }

    /// <summary>
    /// Coroutine for smooth snap transition.
    /// </summary>
    private System.Collections.IEnumerator SmoothSnapCoroutine(Vector3 targetPosition)
    {
        Vector3 startPosition = playerTransform.position;
        float elapsed = 0f;

        while (elapsed < smoothSnapDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / smoothSnapDuration;
            t = t * t * (3f - 2f * t); // Smoothstep

            Vector3 newPos = Vector3.Lerp(startPosition, targetPosition, t);

            if (playerRigidbody != null)
            {
                playerRigidbody.MovePosition(newPos);
            }
            else
            {
                playerTransform.position = newPos;
            }

            yield return null;
        }

        // Ensure final position is exact
        if (playerRigidbody != null)
        {
            playerRigidbody.position = targetPosition;
        }
        else
        {
            playerTransform.position = targetPosition;
        }
    }

    #endregion

    // ==================== PUBLIC METHODS ====================
    #region PUBLIC METHODS

    /// <summary>
    /// Manually triggers a snap check.
    /// Useful for debugging or scripted events.
    /// </summary>
    [ContextMenu("Force Snap Check")]
    public void ForceSnapCheck()
    {
        if (worldRotation != null)
        {
            SnapPlayerToValidPosition(worldRotation.GetCurrentFaceIndex());
        }
    }

    /// <summary>
    /// Enables or disables the snap system.
    /// </summary>
    public void SetSnapEnabled(bool enabled)
    {
        snapEnabled = enabled;

        if (showDebugLogs)
        {
            Debug.Log($"[_FezDepthSnapper] Snap {(enabled ? "ENABLED" : "DISABLED")}");
        }
    }

    #endregion

    // ==================== EDITOR GIZMOS ====================
    #region EDITOR GIZMOS

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (playerTransform == null)
        {
            return;
        }

        // Draw search range
        Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
        Gizmos.DrawWireSphere(playerTransform.position, maxSnapDistance);

        // Draw depth directions
        if (worldRotation != null || Application.isPlaying)
        {
            var rotation = worldRotation != null ? worldRotation : _WorldRotationController.Instance;
            if (rotation != null)
            {
                Vector3 forward = rotation.GetCurrentForward();

                Gizmos.color = Color.green;
                Gizmos.DrawRay(playerTransform.position, forward * maxSnapDistance);

                Gizmos.color = Color.red;
                Gizmos.DrawRay(playerTransform.position, -forward * maxSnapDistance);
            }
        }
    }
#endif

    #endregion
}