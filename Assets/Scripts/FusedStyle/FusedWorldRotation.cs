// ============================================================================
// FEZ-STYLE WORLD ROTATION CONTROLLER FOR CINEMACHINE
// ============================================================================
// This script handles Fez-style 90-degree world rotation using Cinemachine.
// It rotates a "rig" (parent GameObject) that contains the virtual camera.
// The camera orbits around the player while Cinemachine handles smooth following.
// ============================================================================

// Required Unity namespaces
using UnityEngine;                    // Core Unity functionality (MonoBehaviour, Transform, etc.)
using UnityEngine.InputSystem;        // New Input System for player input handling
using System.Collections;             // Required for IEnumerator (coroutines)
using System;                         // Required for Action delegates (events)

// Cinemachine namespace - Unity 6+ uses Unity.Cinemachine, older versions use Cinemachine
#if UNITY_6000_0_OR_NEWER
using Unity.Cinemachine;              // Cinemachine 3.x namespace for Unity 6+
#else
using Cinemachine;                    // Cinemachine 2.x namespace for older Unity versions
#endif

/// <summary>
/// Controls Fez-style 90-degree world rotation integrated with Cinemachine.
/// 
/// HOW IT WORKS:
/// - A "pivot rig" GameObject contains the Cinemachine Virtual Camera as a child
/// - When rotation is triggered, the rig rotates 90° around the Y-axis
/// - The virtual camera (being a child) orbits with the rig
/// - Cinemachine still handles smooth follow/look-at behavior
/// 
/// SETUP:
/// 1. Create empty GameObject "FezCameraRig" at (0,0,0)
/// 2. Add this component to it
/// 3. Create Cinemachine Camera as child, position at (0, 2, 15)
/// 4. Set Follow and LookAt to your player
/// 5. Assign references in this component
/// </summary>
public class FusedWorldRotation : MonoBehaviour
{
    // ========================================================================
    // SINGLETON PATTERN
    // ========================================================================
    // Singleton allows any script to access this controller via FezWorldRotation.Instance
    // This is useful for player controllers, trigger zones, etc.
    // ========================================================================
    
    #region Singleton
    
    /// <summary>
    /// Static instance accessible from anywhere via FezWorldRotation.Instance
    /// </summary>
    private static FusedWorldRotation _instance;    // Private backing field for the singleton
    
    /// <summary>
    /// Public property to access the singleton instance.
    /// Returns null if no instance exists in the scene.
    /// </summary>
    public static FusedWorldRotation Instance
    {
        get { return _instance; }                 // Simply return the private instance
    }
    
    #endregion
    
    // ========================================================================
    // EVENTS
    // ========================================================================
    // Events allow other scripts to react to rotation without tight coupling.
    // Subscribe with: FezWorldRotation.Instance.OnRotationStarted += MyMethod;
    // Unsubscribe with: FezWorldRotation.Instance.OnRotationStarted -= MyMethod;
    // ========================================================================
    
    #region Events
    
    /// <summary>
    /// Fired when rotation animation BEGINS.
    /// Parameter: the NEW face index we're rotating TO (0-3)
    /// Use this to freeze the player, play sounds, etc.
    /// </summary>
    public event Action<int> OnRotationStarted;
    
    /// <summary>
    /// Fired when rotation animation ENDS.
    /// Parameter: the face index we've arrived at (0-3)
    /// Use this to unfreeze the player, snap depth, update constraints, etc.
    /// </summary>
    public event Action<int> OnRotationCompleted;
    
    /// <summary>
    /// Fired every frame DURING rotation.
    /// Parameter: progress from 0.0 (start) to 1.0 (end)
    /// Use this for UI updates, visual effects that follow rotation progress.
    /// </summary>
    public event Action<float> OnRotationProgress;
    
    #endregion
    
    // ========================================================================
    // INSPECTOR REFERENCES
    // ========================================================================
    // These must be assigned in the Unity Inspector.
    // The rig is the parent that rotates; the player is what we follow.
    // ========================================================================
    
    #region References
    
    [Header("=== REQUIRED REFERENCES ===")]
    // Header creates a bold label in the Inspector for organization
    
    [Tooltip("The pivot rig that will rotate. This should be THIS GameObject or a parent containing the virtual camera.")]
    [SerializeField]                              // SerializeField exposes private fields to Inspector
    private Transform pivotRig;                   // The transform we'll rotate (usually this.transform)
    
    [Tooltip("The player transform. The rig will follow this target's X/Z position.")]
    [SerializeField]
    private Transform followTarget;               // Usually the player - rig follows their horizontal position
    
    [Tooltip("Reference to the Cinemachine Virtual Camera (optional - for direct access if needed).")]
    [SerializeField]
#if UNITY_6000_0_OR_NEWER
    private CinemachineCamera virtualCamera;      // Cinemachine 3.x uses CinemachineCamera
#else
    private CinemachineVirtualCamera virtualCamera; // Cinemachine 2.x uses CinemachineVirtualCamera
#endif
    
    #endregion
    
    // ========================================================================
    // MASTER CONTROLS
    // ========================================================================
    // These toggles control whether rotation can happen at all.
    // Useful for cutscenes, menus, or disabling rotation in certain areas.
    // ========================================================================
    
    #region Master Controls
    
    [Header("=== MASTER CONTROLS ===")]
    
    [Tooltip("Master toggle. If FALSE, ALL rotation is disabled regardless of other settings.")]
    [SerializeField]
    private bool canRotate = true;                // Master on/off switch for the entire system
    
    [Tooltip("If TRUE, player can rotate using input (Q/E keys or shoulder buttons).")]
    [SerializeField]
    private bool inputModeEnabled = true;         // Allows rotation via keyboard/gamepad
    
    [Tooltip("If TRUE, trigger zones in the level can cause rotation.")]
    [SerializeField]
    private bool triggerModeEnabled = true;       // Allows rotation via trigger colliders
    
    #endregion
    
    // ========================================================================
    // ROTATION SETTINGS
    // ========================================================================
    // These control how the rotation looks and feels.
    // ========================================================================
    
    #region Rotation Settings
    
    [Header("=== ROTATION SETTINGS ===")]
    
    [Tooltip("How long the 90-degree rotation takes in seconds. Lower = faster.")]
    [Range(0.1f, 2f)]                             // Range attribute creates a slider in Inspector
    [SerializeField]
    private float rotationDuration = 0.5f;        // Half a second feels responsive but smooth
    
    [Tooltip("Animation curve for rotation easing. Default is ease-in-out for smooth feel.")]
    [SerializeField]
    private AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    // AnimationCurve allows custom easing. EaseInOut starts slow, speeds up, slows down at end.
    
    [Tooltip("Which face to start at. 0=North (+Z), 1=East (+X), 2=South (-Z), 3=West (-X).")]
    [Range(0, 3)]
    [SerializeField]
    private int startingFaceIndex = 0;            // Which direction camera faces at game start
    
    #endregion
    
    // ========================================================================
    // FOLLOW SETTINGS
    // ========================================================================
    // The rig follows the player's horizontal position so the camera stays centered.
    // ========================================================================
    
    #region Follow Settings
    
    [Header("=== FOLLOW SETTINGS ===")]
    
    [Tooltip("How fast the rig follows the player. Higher = more responsive, lower = more floaty.")]
    [SerializeField]
    private float followSpeed = 10f;              // Lerp speed for following player
    
    [Tooltip("If TRUE, the rig continues following the player even during rotation animation.")]
    [SerializeField]
    private bool followDuringRotation = true;     // Usually want this on for smooth feel
    
    #endregion
    
    // ========================================================================
    // TRIGGER SETTINGS
    // ========================================================================
    // Settings for when rotation is triggered by trigger zones in the level.
    // ========================================================================
    
    #region Trigger Settings
    
    [Header("=== TRIGGER SETTINGS ===")]
    
    [Tooltip("Cooldown in seconds after a trigger-initiated rotation before another can occur.")]
    [SerializeField]
    private float triggerCooldown = 0.5f;         // Prevents rapid-fire rotation from triggers
    
    [Tooltip("If TRUE, a trigger can interrupt an ongoing rotation. If FALSE, must wait for completion.")]
    [SerializeField]
    private bool triggerCanInterrupt = false;     // Usually false to prevent jarring interruptions
    
    #endregion
    
    // ========================================================================
    // DEBUG SETTINGS
    // ========================================================================
    // Helpful for development and troubleshooting.
    // ========================================================================
    
    #region Debug
    
    [Header("=== DEBUG ===")]
    
    [Tooltip("Log rotation events to the Console for debugging.")]
    [SerializeField]
    private bool showDebugLogs = false;           // Enable to see rotation events in Console
    
    [Tooltip("Draw gizmos in Scene view showing face positions and current direction.")]
    [SerializeField]
    private bool showDebugGizmos = true;          // Visual helpers in Scene view
    
    #endregion
    
    // ========================================================================
    // PRIVATE STATE VARIABLES
    // ========================================================================
    // Internal state tracking - not exposed to Inspector.
    // ========================================================================
    
    #region Private Variables
    
    // The four cardinal angles (in degrees) the camera can face
    // Face 0 = 0° (North/+Z), Face 1 = 90° (East/+X), Face 2 = 180° (South/-Z), Face 3 = 270° (West/-X)
    private readonly float[] faceAngles = { 0f, 90f, 180f, 270f };
    
    // Current state tracking
    private int currentFaceIndex = 0;             // Which face we're currently at (0-3)
    private float currentAngle = 0f;              // Current Y rotation in degrees (0-360)
    private bool isRotating = false;              // TRUE while rotation animation is playing
    private float triggerCooldownTimer = 0f;      // Countdown timer for trigger cooldown
    private Coroutine activeRotationCoroutine;    // Reference to current rotation coroutine (for stopping)
    
    #endregion
    
    // ========================================================================
    // UNITY LIFECYCLE METHODS
    // ========================================================================
    // Called automatically by Unity at specific times.
    // ========================================================================
    
    #region Unity Lifecycle
    
    /// <summary>
    /// Called when the script instance is being loaded.
    /// Used for singleton setup - happens before Start().
    /// </summary>
    private void Awake()
    {
        // --- SINGLETON SETUP ---
        
        // Check if an instance already exists
        if (_instance != null && _instance != this)
        {
            // Another instance exists - destroy this duplicate
            Debug.LogWarning("[FezWorldRotation] Duplicate instance detected! Destroying this one.");
            Destroy(gameObject);                  // Remove the duplicate GameObject entirely
            return;                               // Exit early - don't run any more Awake code
        }
        
        // No existing instance - this becomes THE instance
        _instance = this;                         // Store reference to this instance
        
        // --- REFERENCE VALIDATION ---
        
        // If pivotRig wasn't assigned, assume we want to rotate this GameObject
        if (pivotRig == null)
        {
            pivotRig = transform;                 // Use our own transform as the pivot
            
            // Log this for debugging
            if (showDebugLogs)
            {
                Debug.Log("[FezWorldRotation] No pivot rig assigned - using this transform.");
            }
        }
    }
    
    /// <summary>
    /// Called on the frame when the script is enabled, after Awake.
    /// Used for initialization that depends on other objects being ready.
    /// </summary>
    private void Start()
    {
        // --- INITIALIZE STARTING STATE ---
        
        // Set our starting face from the Inspector setting
        currentFaceIndex = startingFaceIndex;     // Which face we begin at
        currentAngle = faceAngles[currentFaceIndex]; // Get the corresponding angle
        
        // Apply the initial rotation immediately (no animation)
        ApplyRotation(currentAngle);
        
        // Log startup info
        if (showDebugLogs)
        {
            Debug.Log($"[FezWorldRotation] Initialized at face {currentFaceIndex} ({currentAngle}°)");
        }
    }
    
    /// <summary>
    /// Called every frame.
    /// Used for timer updates and input that doesn't involve physics.
    /// </summary>
    private void Update()
    {
        // --- UPDATE TRIGGER COOLDOWN ---
        
        // Count down the trigger cooldown timer if it's active
        if (triggerCooldownTimer > 0f)
        {
            triggerCooldownTimer -= Time.deltaTime; // Subtract frame time from cooldown
        }
    }
    
    /// <summary>
    /// Called every frame after Update.
    /// Used for camera/follow logic that should happen after other updates.
    /// </summary>
    private void LateUpdate()
    {
        // --- FOLLOW TARGET ---
        
        // Make the rig follow the player's horizontal position
        UpdatePivotFollow();
    }
    
    /// <summary>
    /// Called when this object is destroyed.
    /// Used for cleanup - especially singleton cleanup.
    /// </summary>
    private void OnDestroy()
    {
        // --- SINGLETON CLEANUP ---
        
        // If this was THE instance, clear the static reference
        if (_instance == this)
        {
            _instance = null;                     // Allow a new instance to be created if needed
        }
    }
    
    #endregion
    
    // ========================================================================
    // INPUT HANDLERS
    // ========================================================================
    // These methods are called by Unity's Input System.
    // Connect them in the PlayerInput component's Events.
    // ========================================================================
    
    #region Input Handlers
    
    /// <summary>
    /// Called by Input System when RotateLeft action is triggered.
    /// Connect this to your RotateLeft action in the PlayerInput component.
    /// </summary>
    /// <param name="context">Contains info about the input (pressed, released, etc.)</param>
    public void OnRotateLeft(InputAction.CallbackContext context)
    {
        // Only respond to the "performed" phase (button fully pressed)
        // This prevents double-triggering on press and release
        if (context.performed)
        {
            // Check if input mode is allowed
            if (inputModeEnabled && canRotate)
            {
                // Rotate left (counter-clockwise) = -1
                RotateWorld(-1);
            }
        }
    }
    
    /// <summary>
    /// Called by Input System when RotateRight action is triggered.
    /// Connect this to your RotateRight action in the PlayerInput component.
    /// </summary>
    /// <param name="context">Contains info about the input (pressed, released, etc.)</param>
    public void OnRotateRight(InputAction.CallbackContext context)
    {
        // Only respond to the "performed" phase (button fully pressed)
        if (context.performed)
        {
            // Check if input mode is allowed
            if (inputModeEnabled && canRotate)
            {
                // Rotate right (clockwise) = +1
                RotateWorld(1);
            }
        }
    }
    
    #endregion
    
    // ========================================================================
    // PUBLIC ROTATION METHODS
    // ========================================================================
    // Call these from other scripts to trigger rotation.
    // ========================================================================
    
    #region Public Rotation Methods
    
    /// <summary>
    /// Rotates the world by 90 degrees in the specified direction.
    /// This is the main method to call for rotation.
    /// </summary>
    /// <param name="direction">-1 for left (counter-clockwise), 1 for right (clockwise)</param>
    public void RotateWorld(int direction)
    {
        // --- PRE-CHECKS ---
        
        // Check if rotation is enabled at all
        if (!canRotate)
        {
            if (showDebugLogs) Debug.Log("[FezWorldRotation] Rotation blocked: canRotate is false");
            return;                               // Exit - rotation disabled
        }
        
        // Check if we're already rotating
        if (isRotating)
        {
            if (showDebugLogs) Debug.Log("[FezWorldRotation] Rotation blocked: already rotating");
            return;                               // Exit - wait for current rotation to finish
        }
        
        // --- VALIDATE DIRECTION ---
        
        // Clamp direction to -1, 0, or 1
        direction = Mathf.Clamp(direction, -1, 1);
        
        // If direction is 0, nothing to do
        if (direction == 0)
        {
            return;                               // Exit - no rotation requested
        }
        
        // --- CALCULATE NEW FACE ---
        
        // Calculate the new face index
        // Adding 4 before modulo ensures we handle negative numbers correctly
        // Example: face 0 + direction -1 = -1. Adding 4 = 3. Modulo 4 = 3 (West). Correct!
        int newFaceIndex = (currentFaceIndex + direction + 4) % 4;
        
        // --- START ROTATION ---
        
        // Begin the rotation animation
        StartRotation(newFaceIndex, direction);
    }
    
    /// <summary>
    /// Rotates directly to a specific face, taking the shortest path.
    /// Useful for triggers that want to force a specific viewing angle.
    /// </summary>
    /// <param name="targetFaceIndex">Face to rotate to: 0=North, 1=East, 2=South, 3=West</param>
    public void RotateToFace(int targetFaceIndex)
    {
        // --- PRE-CHECKS ---
        
        // Check if rotation is enabled
        if (!canRotate)
        {
            if (showDebugLogs) Debug.Log("[FezWorldRotation] RotateToFace blocked: canRotate is false");
            return;
        }
        
        // Check if already rotating
        if (isRotating)
        {
            if (showDebugLogs) Debug.Log("[FezWorldRotation] RotateToFace blocked: already rotating");
            return;
        }
        
        // --- VALIDATE TARGET ---
        
        // Clamp to valid range (0-3)
        targetFaceIndex = Mathf.Clamp(targetFaceIndex, 0, 3);
        
        // If already at target face, nothing to do
        if (targetFaceIndex == currentFaceIndex)
        {
            if (showDebugLogs) Debug.Log($"[FezWorldRotation] Already at face {targetFaceIndex}");
            return;
        }
        
        // --- CALCULATE SHORTEST PATH ---
        
        // Find the shortest rotation direction
        // Difference can be -3 to +3
        int diff = targetFaceIndex - currentFaceIndex;
        
        // Determine direction based on difference
        int direction;
        
        if (diff == 1 || diff == -3)
        {
            // One step clockwise OR three steps counter-clockwise = go clockwise
            direction = 1;
        }
        else if (diff == -1 || diff == 3)
        {
            // One step counter-clockwise OR three steps clockwise = go counter-clockwise
            direction = -1;
        }
        else // diff == 2 or diff == -2
        {
            // Exactly opposite - pick clockwise arbitrarily
            direction = 1;
        }
        
        // --- START ROTATION ---
        
        StartRotation(targetFaceIndex, direction);
    }
    
    /// <summary>
    /// Called by trigger zones to initiate rotation.
    /// Respects trigger cooldown and interrupt settings.
    /// </summary>
    /// <param name="direction">-1 for left, 1 for right</param>
    public void RotateFromTrigger(int direction)
    {
        // --- CHECK TRIGGER MODE ---
        
        // Is trigger mode enabled?
        if (!triggerModeEnabled)
        {
            if (showDebugLogs) Debug.Log("[FezWorldRotation] Trigger blocked: triggerModeEnabled is false");
            return;
        }
        
        // Is the master toggle on?
        if (!canRotate)
        {
            if (showDebugLogs) Debug.Log("[FezWorldRotation] Trigger blocked: canRotate is false");
            return;
        }
        
        // --- CHECK COOLDOWN ---
        
        // Still in cooldown from last trigger?
        if (triggerCooldownTimer > 0f)
        {
            if (showDebugLogs) Debug.Log($"[FezWorldRotation] Trigger blocked: on cooldown ({triggerCooldownTimer:F1}s remaining)");
            return;
        }
        
        // --- CHECK IF ALREADY ROTATING ---
        
        if (isRotating)
        {
            // Can this trigger interrupt?
            if (!triggerCanInterrupt)
            {
                if (showDebugLogs) Debug.Log("[FezWorldRotation] Trigger blocked: already rotating and cannot interrupt");
                return;
            }
            
            // Stop current rotation to allow interrupt
            StopCurrentRotation();
        }
        
        // --- PERFORM ROTATION ---
        
        // Use the standard rotation method
        RotateWorld(direction);
        
        // Start cooldown timer
        triggerCooldownTimer = triggerCooldown;
    }
    
    /// <summary>
    /// Called by trigger zones to rotate to a specific face.
    /// Respects trigger cooldown and interrupt settings.
    /// </summary>
    /// <param name="targetFaceIndex">Face to rotate to (0-3)</param>
    public void RotateToFaceFromTrigger(int targetFaceIndex)
    {
        // --- CHECK TRIGGER MODE ---
        
        if (!triggerModeEnabled)
        {
            if (showDebugLogs) Debug.Log("[FezWorldRotation] Trigger blocked: triggerModeEnabled is false");
            return;
        }
        
        if (!canRotate)
        {
            if (showDebugLogs) Debug.Log("[FezWorldRotation] Trigger blocked: canRotate is false");
            return;
        }
        
        // --- CHECK COOLDOWN ---
        
        if (triggerCooldownTimer > 0f)
        {
            if (showDebugLogs) Debug.Log($"[FezWorldRotation] Trigger blocked: on cooldown ({triggerCooldownTimer:F1}s remaining)");
            return;
        }
        
        // --- CHECK IF ALREADY ROTATING ---
        
        if (isRotating)
        {
            if (!triggerCanInterrupt)
            {
                if (showDebugLogs) Debug.Log("[FezWorldRotation] Trigger blocked: already rotating and cannot interrupt");
                return;
            }
            
            StopCurrentRotation();
        }
        
        // --- PERFORM ROTATION ---
        
        RotateToFace(targetFaceIndex);
        
        // Start cooldown timer
        triggerCooldownTimer = triggerCooldown;
    }
    
    /// <summary>
    /// Instantly sets the rotation to a specific face WITHOUT animation.
    /// Useful for initialization or teleportation.
    /// </summary>
    /// <param name="faceIndex">Face to set to (0-3)</param>
    public void SetFaceImmediate(int faceIndex)
    {
        // Clamp to valid range
        faceIndex = Mathf.Clamp(faceIndex, 0, 3);
        
        // Update state
        currentFaceIndex = faceIndex;             // Set face index
        currentAngle = faceAngles[faceIndex];     // Set angle to match
        
        // Apply rotation immediately
        ApplyRotation(currentAngle);
        
        // Log
        if (showDebugLogs)
        {
            Debug.Log($"[FezWorldRotation] Instant set to face {faceIndex} ({currentAngle}°)");
        }
    }
    
    #endregion
    
    // ========================================================================
    // ROTATION LOGIC (PRIVATE)
    // ========================================================================
    // Internal methods that handle the actual rotation animation.
    // ========================================================================
    
    #region Rotation Logic
    
    /// <summary>
    /// Begins a rotation to the specified face.
    /// Called internally after all checks have passed.
    /// </summary>
    /// <param name="targetFaceIndex">The face to rotate to (0-3)</param>
    /// <param name="direction">-1 for counter-clockwise, 1 for clockwise</param>
    private void StartRotation(int targetFaceIndex, int direction)
    {
        // If there's an existing rotation coroutine, stop it first
        if (activeRotationCoroutine != null)
        {
            StopCoroutine(activeRotationCoroutine);   // Stop the old coroutine
        }
        
        // Start the rotation coroutine and store reference
        activeRotationCoroutine = StartCoroutine(RotationCoroutine(targetFaceIndex, direction));
    }
    
    /// <summary>
    /// Stops any currently running rotation.
    /// Used for interrupts.
    /// </summary>
    private void StopCurrentRotation()
    {
        // If a rotation is running, stop it
        if (activeRotationCoroutine != null)
        {
            StopCoroutine(activeRotationCoroutine);   // Stop the coroutine
            activeRotationCoroutine = null;           // Clear the reference
        }
        
        // Mark as not rotating
        isRotating = false;
        
        // Snap to nearest valid angle
        currentAngle = faceAngles[currentFaceIndex];
        ApplyRotation(currentAngle);
    }
    
    /// <summary>
    /// Coroutine that performs the animated rotation over time.
    /// Uses AnimationCurve for smooth easing.
    /// </summary>
    /// <param name="targetFaceIndex">Face we're rotating to</param>
    /// <param name="direction">Direction of rotation (-1 or 1)</param>
    /// <returns>IEnumerator for coroutine</returns>
    private IEnumerator RotationCoroutine(int targetFaceIndex, int direction)
    {
        // --- SETUP ---
        
        // Mark that we're now rotating
        isRotating = true;
        
        // Update the face index immediately (we're committed to this rotation)
        currentFaceIndex = targetFaceIndex;
        
        // Get start and target angles
        float startAngle = currentAngle;                  // Where we're starting from
        float targetAngle = faceAngles[targetFaceIndex];  // Where we're going to
        
        // --- HANDLE ANGLE WRAPPING ---
        // When rotating from 270° to 0° (or vice versa), we need to handle the wrap-around
        // Otherwise we'd rotate the "long way" around
        
        if (direction > 0 && targetAngle < startAngle)
        {
            // Rotating clockwise but target is "behind" us
            // Example: 270° -> 0° should go through 360°, not back through 180°
            targetAngle += 360f;
        }
        else if (direction < 0 && targetAngle > startAngle)
        {
            // Rotating counter-clockwise but target is "ahead" of us
            // Example: 0° -> 270° should go through -90°, not forward through 180°
            targetAngle -= 360f;
        }
        
        // --- FIRE START EVENT ---
        
        // Notify listeners that rotation is starting
        OnRotationStarted?.Invoke(targetFaceIndex);
        
        // Log
        if (showDebugLogs)
        {
            Debug.Log($"[FezWorldRotation] Rotation started: {startAngle}° -> {targetAngle}° (face {targetFaceIndex})");
        }
        
        // --- ANIMATION LOOP ---
        
        float elapsed = 0f;                               // Time elapsed since start
        
        // Loop until we've completed the duration
        while (elapsed < rotationDuration)
        {
            // Add frame time to elapsed
            elapsed += Time.deltaTime;
            
            // Calculate progress (0 to 1)
            float t = Mathf.Clamp01(elapsed / rotationDuration);
            
            // Apply easing curve
            // The curve remaps linear 0-1 to a custom curve (e.g., ease-in-out)
            float curvedT = rotationCurve.Evaluate(t);
            
            // Lerp (linear interpolate) from start to target based on curved progress
            currentAngle = Mathf.Lerp(startAngle, targetAngle, curvedT);
            
            // Apply the rotation to the rig
            ApplyRotation(currentAngle);
            
            // Fire progress event
            OnRotationProgress?.Invoke(t);
            
            // Wait for next frame
            yield return null;
        }
        
        // --- FINALIZE ---
        
        // Ensure we end exactly at the target
        currentAngle = targetAngle;
        
        // Normalize angle to 0-360 range
        NormalizeAngle();
        
        // Apply final rotation
        ApplyRotation(currentAngle);
        
        // Mark rotation as complete
        isRotating = false;
        activeRotationCoroutine = null;
        
        // --- FIRE COMPLETION EVENT ---
        
        OnRotationCompleted?.Invoke(currentFaceIndex);
        
        // Log
        if (showDebugLogs)
        {
            Debug.Log($"[FezWorldRotation] Rotation completed: face {currentFaceIndex} ({currentAngle}°)");
        }
    }
    
    /// <summary>
    /// Normalizes currentAngle to the 0-360 range.
    /// Called after rotation to keep angle values clean.
    /// </summary>
    private void NormalizeAngle()
    {
        // Handle angles >= 360
        while (currentAngle >= 360f)
        {
            currentAngle -= 360f;
        }
        
        // Handle negative angles
        while (currentAngle < 0f)
        {
            currentAngle += 360f;
        }
    }
    
    #endregion
    
    // ========================================================================
    // RIG MANIPULATION
    // ========================================================================
    // Methods that actually move/rotate the rig.
    // ========================================================================
    
    #region Rig Manipulation
    
    /// <summary>
    /// Applies a Y-axis rotation to the pivot rig.
    /// This is what actually rotates the camera around the scene.
    /// </summary>
    /// <param name="angle">Y rotation in degrees</param>
    private void ApplyRotation(float angle)
    {
        // Safety check - make sure we have a rig
        if (pivotRig == null)
        {
            Debug.LogError("[FezWorldRotation] Cannot apply rotation: pivotRig is null!");
            return;
        }
        
        // Set the rig's rotation
        // We only rotate around Y (up) axis - X and Z stay at 0
        pivotRig.rotation = Quaternion.Euler(0f, angle, 0f);
    }
    
    /// <summary>
    /// Makes the rig follow the player's horizontal position.
    /// Called in LateUpdate to happen after player movement.
    /// </summary>
    private void UpdatePivotFollow()
    {
        // Safety check - need both rig and target
        if (pivotRig == null || followTarget == null)
        {
            return;                               // Can't follow without references
        }
        
        // Check if we should follow during rotation
        if (isRotating && !followDuringRotation)
        {
            return;                               // Don't follow while rotating
        }
        
        // Calculate target position (player's X/Z, but keep rig's Y)
        Vector3 targetPos = new Vector3(
            followTarget.position.x,              // Follow player's X position
            pivotRig.position.y,                  // Keep our current Y (don't follow vertically)
            followTarget.position.z              // Follow player's Z position
        );
        
        // Smoothly move toward target position
        // Lerp interpolates between current and target based on speed
        pivotRig.position = Vector3.Lerp(
            pivotRig.position,                    // Current position
            targetPos,                            // Target position
            Time.deltaTime * followSpeed         // How much to move this frame
        );
    }
    
    #endregion
    
    // ========================================================================
    // PUBLIC GETTERS
    // ========================================================================
    // Methods for other scripts to query the current state.
    // ========================================================================
    
    #region Public Getters
    
    /// <summary>
    /// Gets the current face index (0-3).
    /// 0=North (+Z), 1=East (+X), 2=South (-Z), 3=West (-X)
    /// </summary>
    public int GetCurrentFaceIndex()
    {
        return currentFaceIndex;
    }
    
    /// <summary>
    /// Gets the current rotation angle in degrees (0-360).
    /// </summary>
    public float GetCurrentAngle()
    {
        return currentAngle;
    }
    
    /// <summary>
    /// Returns TRUE if a rotation is currently in progress.
    /// </summary>
    public bool IsRotating()
    {
        return isRotating;
    }
    
    /// <summary>
    /// Gets the current FORWARD (depth) direction based on camera rotation.
    /// This is the direction the camera is "looking into" the scene.
    /// Used by player controller to constrain movement to the 2D plane.
    /// </summary>
    public Vector3 GetCurrentForward()
    {
        // Convert angle to radians for trig functions
        float radians = currentAngle * Mathf.Deg2Rad;
        
        // Calculate forward vector
        // Sin gives X component, Cos gives Z component
        // Negated because forward is INTO the screen (away from camera)
        return new Vector3(
            -Mathf.Sin(radians),                  // X component
            0f,                                   // No vertical component
            -Mathf.Cos(radians)                  // Z component
        );
    }
    
    /// <summary>
    /// Gets the current RIGHT (movement) direction based on camera rotation.
    /// This is the direction the player moves when pressing "right".
    /// Used by player controller for movement calculations.
    /// </summary>
    public Vector3 GetCurrentRight()
    {
        // Convert angle to radians
        float radians = currentAngle * Mathf.Deg2Rad;
        
        // Calculate right vector (perpendicular to forward)
        // Derived from forward by rotating 90 degrees
        return new Vector3(
            Mathf.Cos(radians),                   // X component
            0f,                                   // No vertical component
            -Mathf.Sin(radians)                  // Z component
        );
    }
    
    /// <summary>
    /// Returns TRUE if input-based rotation is currently allowed.
    /// </summary>
    public bool IsInputModeActive()
    {
        return canRotate && inputModeEnabled;
    }
    
    /// <summary>
    /// Returns TRUE if trigger-based rotation is currently allowed.
    /// </summary>
    public bool IsTriggerModeActive()
    {
        return canRotate && triggerModeEnabled;
    }
    
    /// <summary>
    /// Returns the follow target transform (usually the player).
    /// </summary>
    public Transform GetFollowTarget()
    {
        return followTarget;
    }
    
    /// <summary>
    /// Returns the pivot rig transform.
    /// </summary>
    public Transform GetPivotRig()
    {
        return pivotRig;
    }
    
    #endregion
    
    // ========================================================================
    // PUBLIC SETTERS / CONTROLS
    // ========================================================================
    // Methods for other scripts to control the rotation system.
    // ========================================================================
    
    #region Public Controls
    
    /// <summary>
    /// Sets the master toggle on or off.
    /// When off, ALL rotation is blocked.
    /// </summary>
    /// <param name="enabled">TRUE to enable rotation, FALSE to disable</param>
    public void SetSystemEnabled(bool enabled)
    {
        canRotate = enabled;
        
        if (showDebugLogs)
        {
            Debug.Log($"[FezWorldRotation] System {(enabled ? "ENABLED" : "DISABLED")}");
        }
    }
    
    /// <summary>
    /// Sets whether input-based rotation is allowed.
    /// </summary>
    /// <param name="enabled">TRUE to enable input rotation, FALSE to disable</param>
    public void SetInputModeEnabled(bool enabled)
    {
        inputModeEnabled = enabled;
        
        if (showDebugLogs)
        {
            Debug.Log($"[FezWorldRotation] Input mode {(enabled ? "ENABLED" : "DISABLED")}");
        }
    }
    
    /// <summary>
    /// Sets whether trigger-based rotation is allowed.
    /// </summary>
    /// <param name="enabled">TRUE to enable trigger rotation, FALSE to disable</param>
    public void SetTriggerModeEnabled(bool enabled)
    {
        triggerModeEnabled = enabled;
        
        if (showDebugLogs)
        {
            Debug.Log($"[FezWorldRotation] Trigger mode {(enabled ? "ENABLED" : "DISABLED")}");
        }
    }
    
    /// <summary>
    /// Sets the follow target at runtime.
    /// </summary>
    /// <param name="target">New target to follow (usually player)</param>
    public void SetFollowTarget(Transform target)
    {
        followTarget = target;
        
        if (showDebugLogs)
        {
            Debug.Log($"[FezWorldRotation] Follow target set to: {(target != null ? target.name : "null")}");
        }
    }
    
    #endregion
    
    // ========================================================================
    // EDITOR GIZMOS
    // ========================================================================
    // Visual debugging helpers shown in the Scene view.
    // Only compiled in Unity Editor, not in builds.
    // ========================================================================
    
    #region Editor Gizmos
    
#if UNITY_EDITOR
    /// <summary>
    /// Draws gizmos in the Scene view when this object is selected.
    /// Helps visualize face positions and current rotation.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Check if gizmos are enabled
        if (!showDebugGizmos)
        {
            return;
        }
        
        // Determine center point for gizmos
        Vector3 center = Vector3.zero;
        
        // Use follow target position if available
        if (followTarget != null)
        {
            center = followTarget.position;
            center.y = 0f;                        // Keep gizmos at ground level
        }
        else if (pivotRig != null)
        {
            center = pivotRig.position;
            center.y = 0f;
        }
        
        // --- DRAW FACE INDICATORS ---
        
        // Labels for each face
        string[] faceLabels = { "N (0)", "E (1)", "S (2)", "W (3)" };
        
        // Draw a sphere at each face position
        float radius = 3f;                        // Distance from center to draw spheres
        
        for (int i = 0; i < 4; i++)
        {
            // Calculate position for this face
            float angle = faceAngles[i] * Mathf.Deg2Rad;
            Vector3 pos = center + new Vector3(
                Mathf.Sin(angle) * radius,
                0f,
                Mathf.Cos(angle) * radius
            );
            
            // Current face is green, others are yellow
            Gizmos.color = (i == currentFaceIndex) ? Color.green : Color.yellow;
            
            // Draw sphere
            Gizmos.DrawWireSphere(pos, 0.3f);
            
            // Draw label
            UnityEditor.Handles.Label(pos + Vector3.up * 0.5f, faceLabels[i]);
        }
        
        // --- DRAW CURRENT DIRECTION ARROWS ---
        
        // Forward direction (depth)
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(center, GetCurrentForward() * radius);
        
        // Right direction (movement)
        Gizmos.color = Color.red;
        Gizmos.DrawRay(center, GetCurrentRight() * radius);
        
        // Up direction
        Gizmos.color = Color.green;
        Gizmos.DrawRay(center, Vector3.up * 2f);
    }
#endif
    
    #endregion
}
