using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Handles depth snapping after world rotation.
/// When the world rotates, platforms at different depths can become aligned,
/// and the player needs to snap to valid positions.
/// 
/// Core Systems:
/// - Raycasts in depth direction to find valid platforms
/// - Snaps player to nearest valid surface after rotation
/// - Configurable delay for visual effect timing
/// - Debug visualization for level design
/// </summary>
public class _FezDepthSnapper : MonoBehaviour
{
    #region Variables

    // ==================== REFERENCES ====================
    [Header("References")]

    [Tooltip("Use the rotation bridge (supports both original and Cinemachine controllers)")]
    public bool useRotationBridge = true;

    [Tooltip("Reference to the world rotation controller (only used if useRotationBridge is false)")]
    public _WorldRotationController worldRotation;

    [Tooltip("The player's transform. Auto-finds _FezPlayerController if null")]
    public Transform playerTransform;

    [Tooltip("The player's rigidbody for position updates")]
    public Rigidbody playerRigidbody;

    // ==================== MASTER CONTROLS ====================
    [Header("Master Controls")]

    /// <summary>
    /// Master toggle - if FALSE, disables all depth snapping.
    /// </summary>
    [Tooltip("Enable/disable depth snapping")]
    public bool snapEnabled = true;

    /// <summary>
    /// If TRUE, snapping happens automatically after rotation.
    /// </summary>
    [Tooltip("Automatically snap after rotation completes")]
    public bool autoSnapAfterRotation = true;

    // ==================== SNAP SETTINGS ====================
    [Header("Snap Settings")]

    /// <summary>
    /// Maximum distance to search for valid platforms.
    /// </summary>
    [Tooltip("Max distance to search for platforms")]
    public float maxSnapDistance = 10f;

    /// <summary>
    /// Layers to consider as valid platforms.
    /// </summary>
    [Tooltip("Layers considered as platforms")]
    public LayerMask platformLayer = ~0;

    /// <summary>
    /// Small offset to apply when snapping.
    /// </summary>
    [Tooltip("Offset from surface when snapping")]
    public float snapOffset = 0.1f;

    /// <summary>
    /// Delay after rotation before snapping occurs.
    /// </summary>
    [Tooltip("Delay before snapping (for visual timing)")]
    public float snapDelay = 0.1f;

    /// <summary>
    /// If TRUE, smoothly interpolates to snap position.
    /// </summary>
    [Tooltip("Smooth transition to snap position")]
    public bool smoothSnap = false;

    /// <summary>
    /// Duration of smooth snap transition.
    /// </summary>
    [Tooltip("Duration of smooth snap")]
    public float smoothSnapDuration = 0.2f;

    // ==================== ADVANCED SETTINGS ====================
    [Header("Advanced Settings")]

    /// <summary>
    /// Maximum vertical distance to consider valid.
    /// </summary>
    [Tooltip("Max vertical difference for valid snap")]
    public float maxVerticalDifference = 3f;

    /// <summary>
    /// Prefer snapping to ground vs in-air.
    /// </summary>
    [Tooltip("Prioritize ground snapping")]
    public bool preferGroundSnap = true;

    /// <summary>
    /// Search step size for ground finding.
    /// </summary>
    [Tooltip("Step size for depth search")]
    [Range(0.1f, 2f)]
    public float searchStepSize = 0.5f;

    // ==================== DEBUG ====================
    [Header("Debug")]

    [Tooltip("Show debug info in console")]
    public bool showDebugLogs = false;

    [Tooltip("Draw debug rays in scene view")]
    public bool showDebugRays = true;

    [Tooltip("Duration to show debug rays")]
    public float debugRayDuration = 2f;

    // ==================== PRIVATE VARIABLES ====================
    private Coroutine activeSnapCoroutine;

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

    private void InitializeReferences()
    {
        if (!useRotationBridge)
        {
            // Legacy mode: use direct controller reference
            if (worldRotation == null)
            {
                worldRotation = _WorldRotationController.Instance;

                if (worldRotation == null)
                {
                    Debug.LogError("[_FezDepthSnapper] No _WorldRotationController found!");
                }
            }
        }
        else
        {
            // Bridge mode: ensure bridge is initialized
            if (!FezRotationBridge.Instance.IsAvailable)
            {
                Debug.LogError("[_FezDepthSnapper] No rotation controller found via bridge!");
            }
        }

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

    private void SubscribeToEvents()
    {
        if (useRotationBridge)
        {
            FezRotationBridge.OnRotationCompleted += OnRotationCompleted;
        }
        else if (worldRotation != null)
        {
            worldRotation.OnRotationCompleted += OnRotationCompleted;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (useRotationBridge)
        {
            FezRotationBridge.OnRotationCompleted -= OnRotationCompleted;
        }
        else if (worldRotation != null)
        {
            worldRotation.OnRotationCompleted -= OnRotationCompleted;
        }
    }

    #endregion

    // ==================== EVENT CALLBACKS ====================
    #region EVENT CALLBACKS

    private void OnRotationCompleted(int newFaceIndex)
    {
        if (!snapEnabled || !autoSnapAfterRotation)
        {
            return;
        }

        if (activeSnapCoroutine != null)
        {
            StopCoroutine(activeSnapCoroutine);
        }

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

    private System.Collections.IEnumerator DelayedSnapCoroutine(int faceIndex)
    {
        yield return new WaitForSeconds(snapDelay);
        SnapPlayerToValidPosition(faceIndex);
        activeSnapCoroutine = null;
    }

    public void SnapPlayerToValidPosition(int faceIndex)
    {
        if (!snapEnabled || playerTransform == null)
        {
            return;
        }

        // Get depth direction from appropriate controller
        Vector3 depthDirection;
        if (useRotationBridge)
        {
            if (!FezRotationBridge.Instance.IsAvailable)
            {
                return;
            }
            depthDirection = FezRotationBridge.Instance.GetCurrentForward();
        }
        else
        {
            if (worldRotation == null)
            {
                return;
            }
            depthDirection = worldRotation.GetCurrentForward();
        }

        Vector3 currentPos = playerTransform.position;
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

    private Vector3? FindSnapPosition(Vector3 currentPos, Vector3 depthDirection)
    {
        // First check: is current position valid?
        if (IsPositionValid(currentPos))
        {
            return null; // No snap needed
        }

        List<SnapCandidate> candidates = new List<SnapCandidate>();

        // Cast rays in both depth directions
        SearchForPlatforms(currentPos, depthDirection, candidates);
        SearchForPlatforms(currentPos, -depthDirection, candidates);

        // Debug visualization
        if (showDebugRays)
        {
            Debug.DrawRay(currentPos, depthDirection * maxSnapDistance, Color.green, debugRayDuration);
            Debug.DrawRay(currentPos, -depthDirection * maxSnapDistance, Color.red, debugRayDuration);
        }

        // If no direct hits, search for ground at various depths
        if (candidates.Count == 0)
        {
            SearchForGround(currentPos, depthDirection, candidates);
        }

        // Sort and return best candidate
        if (candidates.Count > 0)
        {
            // Sort by priority (ground first if preferred) then by distance
            candidates.Sort((a, b) =>
            {
                if (preferGroundSnap)
                {
                    if (a.hasGround != b.hasGround)
                    {
                        return b.hasGround.CompareTo(a.hasGround);
                    }
                }
                return a.distance.CompareTo(b.distance);
            });

            if (showDebugRays)
            {
                Debug.DrawLine(currentPos, candidates[0].position, Color.yellow, debugRayDuration);
            }

            return candidates[0].position;
        }

        return null;
    }

    private void SearchForPlatforms(Vector3 origin, Vector3 direction, List<SnapCandidate> candidates)
    {
        RaycastHit[] hits = Physics.RaycastAll(origin, direction, maxSnapDistance, platformLayer);

        foreach (var hit in hits)
        {
            Vector3 candidate = hit.point - direction * snapOffset;

            if (IsPositionValid(candidate))
            {
                float distance = Vector3.Distance(origin, candidate);
                candidates.Add(new SnapCandidate
                {
                    position = candidate,
                    distance = distance,
                    hasGround = true
                });
            }
        }
    }

    private void SearchForGround(Vector3 currentPos, Vector3 depthDirection, List<SnapCandidate> candidates)
    {
        for (float depth = -maxSnapDistance; depth <= maxSnapDistance; depth += searchStepSize)
        {
            Vector3 searchPos = currentPos + depthDirection * depth;

            if (Physics.Raycast(searchPos + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 10f, platformLayer))
            {
                Vector3 candidate = hit.point + Vector3.up * snapOffset;

                if (Mathf.Abs(candidate.y - currentPos.y) < maxVerticalDifference)
                {
                    float distance = Vector3.Distance(currentPos, candidate);
                    candidates.Add(new SnapCandidate
                    {
                        position = candidate,
                        distance = distance,
                        hasGround = true
                    });
                }
            }
        }
    }

    private bool IsPositionValid(Vector3 position)
    {
        return Physics.Raycast(position + Vector3.up * 0.5f, Vector3.down, 1f, platformLayer);
    }

    private void ApplySnap(Vector3 targetPosition)
    {
        if (smoothSnap)
        {
            StartCoroutine(SmoothSnapCoroutine(targetPosition));
        }
        else
        {
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

    // ==================== HELPER STRUCTS ====================
    #region HELPER STRUCTS

    private struct SnapCandidate
    {
        public Vector3 position;
        public float distance;
        public bool hasGround;
    }

    #endregion

    // ==================== PUBLIC METHODS ====================
    #region PUBLIC METHODS

    [ContextMenu("Force Snap Check")]
    public void ForceSnapCheck()
    {
        int faceIndex;
        if (useRotationBridge)
        {
            faceIndex = FezRotationBridge.Instance.GetCurrentFaceIndex();
        }
        else if (worldRotation != null)
        {
            faceIndex = worldRotation.GetCurrentFaceIndex();
        }
        else
        {
            faceIndex = 0;
        }
        
        SnapPlayerToValidPosition(faceIndex);
    }

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
        Vector3 forward = Vector3.forward;
        bool hasRotation = false;
        
        if (Application.isPlaying)
        {
            if (useRotationBridge && FezRotationBridge.Instance.IsAvailable)
            {
                forward = FezRotationBridge.Instance.GetCurrentForward();
                hasRotation = true;
            }
            else if (worldRotation != null)
            {
                forward = worldRotation.GetCurrentForward();
                hasRotation = true;
            }
            else
            {
                var rotation = _WorldRotationController.Instance;
                if (rotation != null)
                {
                    forward = rotation.GetCurrentForward();
                    hasRotation = true;
                }
            }
        }
        else if (worldRotation != null)
        {
            forward = worldRotation.GetCurrentForward();
            hasRotation = true;
        }
        
        if (hasRotation)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(playerTransform.position, forward * maxSnapDistance);

            Gizmos.color = Color.red;
            Gizmos.DrawRay(playerTransform.position, -forward * maxSnapDistance);
        }
    }
#endif

    #endregion
}
