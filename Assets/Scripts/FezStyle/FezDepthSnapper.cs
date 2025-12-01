// ============================================================================
// FEZ-STYLE DEPTH SNAPPER
// ============================================================================
// When the world rotates 90 degrees, platforms at different depths can
// suddenly align. This script snaps the player to valid positions after
// rotation to prevent falling through the world or floating in the air.
// ============================================================================

// Required Unity namespaces
using UnityEngine;                    // Core Unity functionality
using System.Collections;             // Required for coroutines
using System.Collections.Generic;     // Required for List<T>

/// <summary>
/// Handles depth snapping after world rotation completes.
/// 
/// HOW IT WORKS:
/// 1. Listens for rotation completion event from FezWorldRotation
/// 2. Casts rays in the new depth direction to find valid platforms
/// 3. Snaps the player to the nearest valid position
/// 
/// SETUP:
/// 1. Create an empty GameObject in your scene
/// 2. Add this component
/// 3. Assign the player reference (or let it auto-find)
/// 4. Configure the platform layer mask
/// </summary>
public class FezDepthSnapper : MonoBehaviour
{
    // ========================================================================
    // INSPECTOR REFERENCES
    // ========================================================================
    
    #region References
    
    [Header("=== REFERENCES ===")]
    
    [Tooltip("The player's transform. If not assigned, will auto-find _FezPlayerController.")]
    [SerializeField]
    private Transform playerTransform;            // The transform to snap
    
    [Tooltip("The player's Rigidbody. Used for physics-safe position updates.")]
    [SerializeField]
    private Rigidbody playerRigidbody;            // Used for MovePosition
    
    #endregion
    
    // ========================================================================
    // MASTER CONTROLS
    // ========================================================================
    
    #region Master Controls
    
    [Header("=== MASTER CONTROLS ===")]
    
    [Tooltip("Master toggle. If FALSE, depth snapping is completely disabled.")]
    [SerializeField]
    private bool snapEnabled = true;              // On/off switch for the system
    
    [Tooltip("If TRUE, snapping happens automatically after each rotation.")]
    [SerializeField]
    private bool autoSnapAfterRotation = true;    // Usually want this on
    
    #endregion
    
    // ========================================================================
    // SNAP SETTINGS
    // ========================================================================
    
    #region Snap Settings
    
    [Header("=== SNAP SETTINGS ===")]
    
    [Tooltip("Maximum distance to search for valid platforms in the depth direction.")]
    [SerializeField]
    private float maxSnapDistance = 10f;          // How far to look for platforms
    
    [Tooltip("Layers that count as valid platforms to snap to.")]
    [SerializeField]
    private LayerMask platformLayer = ~0;         // ~0 = all layers (default)
    
    [Tooltip("Small offset from surface when snapping to prevent clipping.")]
    [SerializeField]
    private float snapOffset = 0.1f;              // Keeps player slightly above surface
    
    [Tooltip("Delay after rotation before snapping occurs. Allows rotation to fully complete.")]
    [SerializeField]
    private float snapDelay = 0.1f;               // Brief pause before snap
    
    [Tooltip("If TRUE, smoothly interpolates to snap position. If FALSE, snaps instantly.")]
    [SerializeField]
    private bool smoothSnap = false;              // Smooth vs instant
    
    [Tooltip("Duration of smooth snap transition (only used if Smooth Snap is TRUE).")]
    [SerializeField]
    private float smoothSnapDuration = 0.2f;      // How long smooth snap takes
    
    #endregion
    
    // ========================================================================
    // ADVANCED SETTINGS
    // ========================================================================
    
    #region Advanced Settings
    
    [Header("=== ADVANCED SETTINGS ===")]
    
    [Tooltip("Maximum vertical difference allowed when snapping. Prevents snapping to platforms too far above/below.")]
    [SerializeField]
    private float maxVerticalDifference = 3f;     // Don't snap to platforms way above/below
    
    [Tooltip("If TRUE, prioritizes snapping to positions with ground beneath them.")]
    [SerializeField]
    private bool preferGroundSnap = true;         // Prefer landing on ground vs floating
    
    [Tooltip("Step size when searching for ground at different depths.")]
    [Range(0.1f, 2f)]
    [SerializeField]
    private float searchStepSize = 0.5f;          // Resolution of depth search
    
    #endregion
    
    // ========================================================================
    // DEBUG SETTINGS
    // ========================================================================
    
    #region Debug
    
    [Header("=== DEBUG ===")]
    
    [Tooltip("Log snap events to the Console.")]
    [SerializeField]
    private bool showDebugLogs = false;
    
    [Tooltip("Draw debug rays in Scene view when snapping.")]
    [SerializeField]
    private bool showDebugRays = true;
    
    [Tooltip("How long debug rays stay visible.")]
    [SerializeField]
    private float debugRayDuration = 2f;
    
    #endregion
    
    // ========================================================================
    // PRIVATE VARIABLES
    // ========================================================================
    
    #region Private Variables
    
    private Coroutine activeSnapCoroutine;        // Reference to active snap coroutine
    
    #endregion
    
    // ========================================================================
    // UNITY LIFECYCLE
    // ========================================================================
    
    #region Unity Lifecycle
    
    /// <summary>
    /// Called when the script is first enabled.
    /// Used for initialization.
    /// </summary>
    private void Start()
    {
        // Initialize references
        InitializeReferences();
        
        // Subscribe to rotation events
        SubscribeToRotationEvents();
    }
    
    /// <summary>
    /// Called when the script is destroyed.
    /// Used for cleanup.
    /// </summary>
    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        UnsubscribeFromRotationEvents();
    }
    
    #endregion
    
    // ========================================================================
    // INITIALIZATION
    // ========================================================================
    
    #region Initialization
    
    /// <summary>
    /// Initializes references, auto-finding them if not assigned.
    /// </summary>
    private void InitializeReferences()
    {
        // --- AUTO-FIND PLAYER ---
        
        // If player transform wasn't assigned, try to find it
        if (playerTransform == null)
        {
            // Look for the player controller
            FezPlayerController playerController = FindObjectOfType<FezPlayerController>();
            
            if (playerController != null)
            {
                // Found it - get references from it
                playerTransform = playerController.transform;
                playerRigidbody = playerController.GetComponent<Rigidbody>();
                
                if (showDebugLogs)
                {
                    Debug.Log("[FezDepthSnapper] Auto-found player controller.");
                }
            }
            else
            {
                // Couldn't find player
                Debug.LogWarning("[FezDepthSnapper] Could not find _FezPlayerController. Please assign player manually.");
            }
        }
        else if (playerRigidbody == null)
        {
            // Player assigned but no rigidbody - try to get it
            playerRigidbody = playerTransform.GetComponent<Rigidbody>();
        }
    }
    
    /// <summary>
    /// Subscribes to rotation controller events.
    /// </summary>
    private void SubscribeToRotationEvents()
    {
        // Get the rotation controller
        FezWorldRotation controller = FezWorldRotation.Instance;
        
        // Check if controller exists
        if (controller != null)
        {
            // Subscribe to the completion event
            controller.OnRotationCompleted += OnRotationCompleted;
            
            if (showDebugLogs)
            {
                Debug.Log("[FezDepthSnapper] Subscribed to FezWorldRotation events.");
            }
        }
        else
        {
            Debug.LogWarning("[FezDepthSnapper] No FezWorldRotation found. Will retry on first snap.");
        }
    }
    
    /// <summary>
    /// Unsubscribes from rotation controller events.
    /// Important for cleanup to prevent memory leaks.
    /// </summary>
    private void UnsubscribeFromRotationEvents()
    {
        // Get the rotation controller
        FezWorldRotation controller = FezWorldRotation.Instance;
        
        // If controller exists, unsubscribe
        if (controller != null)
        {
            controller.OnRotationCompleted -= OnRotationCompleted;
        }
    }
    
    #endregion
    
    // ========================================================================
    // EVENT CALLBACKS
    // ========================================================================
    
    #region Event Callbacks
    
    /// <summary>
    /// Called when rotation completes.
    /// Triggers the depth snap process.
    /// </summary>
    /// <param name="newFaceIndex">The face we rotated to (0-3)</param>
    private void OnRotationCompleted(int newFaceIndex)
    {
        // --- CHECK IF SNAPPING IS ENABLED ---
        
        if (!snapEnabled)
        {
            return;                               // Snapping disabled
        }
        
        if (!autoSnapAfterRotation)
        {
            return;                               // Auto-snap disabled
        }
        
        // --- START SNAP PROCESS ---
        
        // Stop any existing snap coroutine
        if (activeSnapCoroutine != null)
        {
            StopCoroutine(activeSnapCoroutine);
        }
        
        // Start new snap (with or without delay)
        if (snapDelay > 0f)
        {
            // Delayed snap
            activeSnapCoroutine = StartCoroutine(DelayedSnapCoroutine(newFaceIndex));
        }
        else
        {
            // Immediate snap
            SnapPlayerToValidPosition();
        }
    }
    
    #endregion
    
    // ========================================================================
    // SNAP LOGIC
    // ========================================================================
    
    #region Snap Logic
    
    /// <summary>
    /// Coroutine that waits before snapping.
    /// Allows rotation animation to fully complete.
    /// </summary>
    private IEnumerator DelayedSnapCoroutine(int faceIndex)
    {
        // Wait for the delay
        yield return new WaitForSeconds(snapDelay);
        
        // Perform snap
        SnapPlayerToValidPosition();
        
        // Clear reference
        activeSnapCoroutine = null;
    }
    
    /// <summary>
    /// Main snap method. Finds valid position and moves player there.
    /// </summary>
    public void SnapPlayerToValidPosition()
    {
        // --- VALIDATE ---
        
        // Check if snapping is enabled
        if (!snapEnabled)
        {
            if (showDebugLogs) Debug.Log("[FezDepthSnapper] Snap skipped: disabled.");
            return;
        }
        
        // Check if we have player reference
        if (playerTransform == null)
        {
            Debug.LogError("[FezDepthSnapper] Cannot snap: playerTransform is null!");
            return;
        }
        
        // --- GET DEPTH DIRECTION ---
        
        // Get rotation controller
        FezWorldRotation controller = FezWorldRotation.Instance;
        
        if (controller == null)
        {
            Debug.LogError("[FezDepthSnapper] Cannot snap: no FezWorldRotation found!");
            return;
        }
        
        // Get the current depth direction (forward = into screen)
        Vector3 depthDirection = controller.GetCurrentForward();
        
        // --- FIND SNAP POSITION ---
        
        // Current player position
        Vector3 currentPos = playerTransform.position;
        
        // Search for valid snap position
        Vector3? snapPosition = FindSnapPosition(currentPos, depthDirection);
        
        // --- APPLY SNAP ---
        
        if (snapPosition.HasValue)
        {
            // Found a valid position - snap to it
            ApplySnap(snapPosition.Value);
            
            if (showDebugLogs)
            {
                Debug.Log($"[FezDepthSnapper] Snapped player to {snapPosition.Value}");
            }
        }
        else
        {
            // No valid position found
            if (showDebugLogs)
            {
                Debug.Log("[FezDepthSnapper] No valid snap position found - player stays in place.");
            }
        }
    }
    
    /// <summary>
    /// Searches for a valid snap position by casting rays in depth direction.
    /// </summary>
    /// <param name="currentPos">Player's current position</param>
    /// <param name="depthDirection">The depth (forward) direction</param>
    /// <returns>Valid position or null if none found</returns>
    private Vector3? FindSnapPosition(Vector3 currentPos, Vector3 depthDirection)
    {
        // --- CHECK CURRENT POSITION ---
        
        // First, check if current position is already valid
        if (IsPositionValid(currentPos))
        {
            // Already on valid ground - no snap needed
            return null;
        }
        
        // --- SEARCH FOR PLATFORMS ---
        
        // List to store candidate positions
        List<SnapCandidate> candidates = new List<SnapCandidate>();
        
        // Cast rays in both depth directions (forward and backward)
        SearchForPlatforms(currentPos, depthDirection, candidates);   // Forward
        SearchForPlatforms(currentPos, -depthDirection, candidates);  // Backward
        
        // --- DEBUG VISUALIZATION ---
        
        if (showDebugRays)
        {
            // Green ray = forward search direction
            Debug.DrawRay(currentPos, depthDirection * maxSnapDistance, Color.green, debugRayDuration);
            
            // Red ray = backward search direction
            Debug.DrawRay(currentPos, -depthDirection * maxSnapDistance, Color.red, debugRayDuration);
        }
        
        // --- SEARCH FOR GROUND AT DIFFERENT DEPTHS ---
        
        // If no direct platform hits, search for ground
        if (candidates.Count == 0)
        {
            SearchForGround(currentPos, depthDirection, candidates);
        }
        
        // --- SELECT BEST CANDIDATE ---
        
        if (candidates.Count > 0)
        {
            // Sort candidates
            // Priority: ground positions first (if preferred), then by distance
            candidates.Sort((a, b) =>
            {
                // Compare ground status if preferGroundSnap is on
                if (preferGroundSnap)
                {
                    if (a.hasGround != b.hasGround)
                    {
                        // Ground positions come first
                        return b.hasGround.CompareTo(a.hasGround);
                    }
                }
                
                // Then sort by distance (closer is better)
                return a.distance.CompareTo(b.distance);
            });
            
            // Draw line to chosen position
            if (showDebugRays)
            {
                Debug.DrawLine(currentPos, candidates[0].position, Color.yellow, debugRayDuration);
            }
            
            // Return the best candidate
            return candidates[0].position;
        }
        
        // No valid position found
        return null;
    }
    
    /// <summary>
    /// Searches for platforms by casting a ray in the specified direction.
    /// </summary>
    private void SearchForPlatforms(Vector3 origin, Vector3 direction, List<SnapCandidate> candidates)
    {
        // Cast ray and get ALL hits (not just the first)
        RaycastHit[] hits = Physics.RaycastAll(
            origin,                               // Start position
            direction,                            // Direction to cast
            maxSnapDistance,                      // Maximum distance
            platformLayer                         // Which layers to hit
        );
        
        // Process each hit
        foreach (RaycastHit hit in hits)
        {
            // Calculate candidate position (slightly offset from surface)
            Vector3 candidate = hit.point - direction * snapOffset;
            
            // Check if this position is valid
            if (IsPositionValid(candidate))
            {
                // Calculate distance from original position
                float distance = Vector3.Distance(origin, candidate);
                
                // Add to candidates list
                candidates.Add(new SnapCandidate
                {
                    position = candidate,
                    distance = distance,
                    hasGround = true
                });
            }
        }
    }
    
    /// <summary>
    /// Searches for ground at various depths along the depth direction.
    /// Used when direct platform search doesn't find anything.
    /// </summary>
    private void SearchForGround(Vector3 currentPos, Vector3 depthDirection, List<SnapCandidate> candidates)
    {
        // Search at different depths from -maxSnapDistance to +maxSnapDistance
        for (float depth = -maxSnapDistance; depth <= maxSnapDistance; depth += searchStepSize)
        {
            // Calculate position at this depth
            Vector3 searchPos = currentPos + depthDirection * depth;
            
            // Cast ray downward from slightly above to find ground
            if (Physics.Raycast(
                searchPos + Vector3.up * 2f,      // Start 2 units above
                Vector3.down,                     // Cast downward
                out RaycastHit hit,
                10f,                              // Max distance down
                platformLayer                     // What to hit
            ))
            {
                // Found ground - calculate candidate position
                Vector3 candidate = hit.point + Vector3.up * snapOffset;
                
                // Check vertical difference isn't too extreme
                if (Mathf.Abs(candidate.y - currentPos.y) < maxVerticalDifference)
                {
                    // Calculate distance
                    float distance = Vector3.Distance(currentPos, candidate);
                    
                    // Add to candidates
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
    
    /// <summary>
    /// Checks if a position has valid ground beneath it.
    /// </summary>
    private bool IsPositionValid(Vector3 position)
    {
        // Cast a short ray downward to check for ground
        return Physics.Raycast(
            position + Vector3.up * 0.5f,         // Start slightly above
            Vector3.down,                         // Cast down
            1f,                                   // Short distance
            platformLayer                         // What counts as ground
        );
    }
    
    /// <summary>
    /// Applies the snap, either instantly or smoothly.
    /// </summary>
    private void ApplySnap(Vector3 targetPosition)
    {
        if (smoothSnap)
        {
            // Smooth snap - interpolate over time
            StartCoroutine(SmoothSnapCoroutine(targetPosition));
        }
        else
        {
            // Instant snap
            if (playerRigidbody != null)
            {
                // Use rigidbody for physics-safe movement
                playerRigidbody.position = targetPosition;
            }
            else
            {
                // Fallback to transform
                playerTransform.position = targetPosition;
            }
        }
    }
    
    /// <summary>
    /// Coroutine for smooth snapping.
    /// Interpolates player position over time.
    /// </summary>
    private IEnumerator SmoothSnapCoroutine(Vector3 targetPosition)
    {
        // Starting position
        Vector3 startPosition = playerTransform.position;
        
        // Elapsed time
        float elapsed = 0f;
        
        // Interpolate over duration
        while (elapsed < smoothSnapDuration)
        {
            // Update time
            elapsed += Time.deltaTime;
            
            // Calculate progress (0 to 1)
            float t = elapsed / smoothSnapDuration;
            
            // Apply smoothstep easing (slow at start and end, fast in middle)
            t = t * t * (3f - 2f * t);
            
            // Calculate new position
            Vector3 newPos = Vector3.Lerp(startPosition, targetPosition, t);
            
            // Apply position
            if (playerRigidbody != null)
            {
                playerRigidbody.MovePosition(newPos);
            }
            else
            {
                playerTransform.position = newPos;
            }
            
            // Wait for next frame
            yield return null;
        }
        
        // Ensure we end exactly at target
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
    
    // ========================================================================
    // HELPER STRUCTURES
    // ========================================================================
    
    #region Helper Structures
    
    /// <summary>
    /// Represents a candidate position for snapping.
    /// Stores position, distance from player, and whether it has ground.
    /// </summary>
    private struct SnapCandidate
    {
        public Vector3 position;                  // World position of candidate
        public float distance;                    // Distance from player
        public bool hasGround;                    // Whether ground is beneath
    }
    
    #endregion
    
    // ========================================================================
    // PUBLIC METHODS
    // ========================================================================
    
    #region Public Methods
    
    /// <summary>
    /// Forces a snap check immediately.
    /// Useful for testing or manual triggering.
    /// </summary>
    [ContextMenu("Force Snap Check")]
    public void ForceSnapCheck()
    {
        SnapPlayerToValidPosition();
    }
    
    /// <summary>
    /// Enables or disables snapping at runtime.
    /// </summary>
    public void SetSnapEnabled(bool enabled)
    {
        snapEnabled = enabled;
        
        if (showDebugLogs)
        {
            Debug.Log($"[FezDepthSnapper] Snap {(enabled ? "ENABLED" : "DISABLED")}");
        }
    }
    
    /// <summary>
    /// Returns whether snapping is currently enabled.
    /// </summary>
    public bool IsSnapEnabled()
    {
        return snapEnabled;
    }
    
    /// <summary>
    /// Sets the player reference at runtime.
    /// </summary>
    public void SetPlayer(Transform player, Rigidbody rb = null)
    {
        playerTransform = player;
        playerRigidbody = rb ?? player?.GetComponent<Rigidbody>();
    }
    
    #endregion
    
    // ========================================================================
    // EDITOR GIZMOS
    // ========================================================================
    
    #region Editor Gizmos
    
#if UNITY_EDITOR
    /// <summary>
    /// Draws debug gizmos in Scene view.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Need player reference
        if (playerTransform == null)
        {
            return;
        }
        
        // --- DRAW SEARCH RANGE ---
        
        // Semi-transparent cyan sphere showing search range
        Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
        Gizmos.DrawWireSphere(playerTransform.position, maxSnapDistance);
        
        // --- DRAW DEPTH DIRECTIONS ---
        
        // Get rotation controller
        FezWorldRotation controller = FezWorldRotation.Instance;
        
        if (controller != null || Application.isPlaying)
        {
            // Get forward direction
            Vector3 forward = controller != null 
                ? controller.GetCurrentForward() 
                : Vector3.forward;
            
            // Green = forward search direction
            Gizmos.color = Color.green;
            Gizmos.DrawRay(playerTransform.position, forward * maxSnapDistance);
            
            // Red = backward search direction
            Gizmos.color = Color.red;
            Gizmos.DrawRay(playerTransform.position, -forward * maxSnapDistance);
        }
    }
#endif
    
    #endregion
}
