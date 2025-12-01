using UnityEngine;
#if CINEMACHINE_3 || UNITY_6000_0_OR_NEWER
using Unity.Cinemachine;
#else
using Cinemachine;
#endif

/// <summary>
/// Helper component that creates and configures a Cinemachine setup optimized for Fez-style rotation.
/// 
/// This creates a hierarchy:
///   CameraRig (rotates around Y)
///     └── CinemachineVirtualCamera (positioned at distance, looks at target)
/// 
/// The rig rotates while Cinemachine handles smooth following and look-at behavior.
/// 
/// USAGE:
/// 1. Add this component to any GameObject
/// 2. Assign your player/follow target
/// 3. Click "Create Cinemachine Rig" in context menu or call CreateRig() at runtime
/// 4. The created rig will auto-connect to FezCinemachineRotation if present
/// </summary>
public class FezCinemachineRigSetup : MonoBehaviour
{
    #region Inspector Fields

    [Header("Target")]
    [Tooltip("The target the camera should follow and look at")]
    public Transform followTarget;

    [Header("Camera Settings")]
    [Tooltip("Distance from target to camera")]
    public float cameraDistance = 15f;
    
    [Tooltip("Height offset from target")]
    public float cameraHeight = 2f;
    
    [Tooltip("Use orthographic projection (recommended for Fez-style)")]
    public bool useOrthographic = true;
    
    [Tooltip("Orthographic size (zoom level)")]
    public float orthographicSize = 8f;
    
    [Tooltip("Field of view if using perspective")]
    public float fieldOfView = 60f;

    [Header("Cinemachine Settings")]
    [Tooltip("Damping for follow smoothing (0 = instant)")]
    public float followDamping = 0.5f;
    
    [Tooltip("Damping for rotation smoothing")]
    public float rotationDamping = 0.2f;
    
    [Tooltip("Dead zone width (0-1)")]
    [Range(0f, 1f)]
    public float deadZoneWidth = 0.1f;
    
    [Tooltip("Dead zone height (0-1)")]
    [Range(0f, 1f)]
    public float deadZoneHeight = 0.1f;

    [Header("Created References (Auto-filled)")]
    [SerializeField] private Transform createdRig;
#if CINEMACHINE_3 || UNITY_6000_0_OR_NEWER
    [SerializeField] private CinemachineCamera createdVirtualCamera;
#else
    [SerializeField] private CinemachineVirtualCamera createdVirtualCamera;
#endif

    #endregion

    #region Public Methods

    /// <summary>
    /// Creates the Cinemachine rig hierarchy.
    /// </summary>
    [ContextMenu("Create Cinemachine Rig")]
    public void CreateRig()
    {
        if (createdRig != null)
        {
            Debug.LogWarning("[FezCinemachineRigSetup] Rig already exists. Destroy it first to recreate.");
            return;
        }

        // Create the pivot rig
        GameObject rigObj = new GameObject("FezCameraRig");
        createdRig = rigObj.transform;
        
        if (followTarget != null)
        {
            createdRig.position = new Vector3(followTarget.position.x, 0f, followTarget.position.z);
        }

        // Create virtual camera as child
        GameObject vcamObj = new GameObject("FezVirtualCamera");
        vcamObj.transform.SetParent(createdRig);
        
        // Position camera at distance
        vcamObj.transform.localPosition = new Vector3(0f, cameraHeight, cameraDistance);
        vcamObj.transform.localRotation = Quaternion.identity;

#if CINEMACHINE_3 || UNITY_6000_0_OR_NEWER
        // Cinemachine 3.x setup
        createdVirtualCamera = vcamObj.AddComponent<CinemachineCamera>();
        
        // Add follow component
        var follow = vcamObj.AddComponent<CinemachineFollow>();
        var trackerSettings = follow.TrackerSettings;
        trackerSettings.PositionDamping = new Vector3(followDamping, followDamping, followDamping);
        trackerSettings.RotationDamping = new Vector3(rotationDamping, rotationDamping, rotationDamping);
        follow.TrackerSettings = trackerSettings;
        
        // Add rotation composer for look-at
        var composer = vcamObj.AddComponent<CinemachineRotationComposer>();
        var composition = composer.Composition;
        var deadZone = composition.DeadZone;
        deadZone.Enabled = true;
        deadZone.Size = new Vector2(deadZoneWidth, deadZoneHeight);
        composition.DeadZone = deadZone;
        composer.Composition = composition;
        
        if (followTarget != null)
        {
            createdVirtualCamera.Follow = followTarget;
            createdVirtualCamera.LookAt = followTarget;
        }
#else
        // Cinemachine 2.x setup
        createdVirtualCamera = vcamObj.AddComponent<CinemachineVirtualCamera>();
        
        // Configure body as Transposer (simpler than Orbital for this use case)
        var body = createdVirtualCamera.AddCinemachineComponent<CinemachineTransposer>();
        body.m_BindingMode = CinemachineTransposer.BindingMode.LockToTarget;
        body.m_FollowOffset = new Vector3(0f, cameraHeight, cameraDistance);
        body.m_XDamping = followDamping;
        body.m_YDamping = followDamping;
        body.m_ZDamping = followDamping;
        
        // Configure aim as Composer
        var aim = createdVirtualCamera.AddCinemachineComponent<CinemachineComposer>();
        aim.m_DeadZoneWidth = deadZoneWidth;
        aim.m_DeadZoneHeight = deadZoneHeight;
        
        if (followTarget != null)
        {
            createdVirtualCamera.Follow = followTarget;
            createdVirtualCamera.LookAt = followTarget;
        }
#endif

        // Configure camera projection
        ConfigureCameraProjection();

        // Auto-connect to rotation controller if present
        ConnectToRotationController();

        Debug.Log("[FezCinemachineRigSetup] Cinemachine rig created successfully!");
    }

    /// <summary>
    /// Destroys the created rig.
    /// </summary>
    [ContextMenu("Destroy Rig")]
    public void DestroyRig()
    {
        if (createdRig != null)
        {
            if (Application.isPlaying)
            {
                Destroy(createdRig.gameObject);
            }
            else
            {
                DestroyImmediate(createdRig.gameObject);
            }
            createdRig = null;
            createdVirtualCamera = null;
            Debug.Log("[FezCinemachineRigSetup] Rig destroyed.");
        }
    }

    /// <summary>
    /// Updates the camera projection settings.
    /// </summary>
    [ContextMenu("Update Camera Settings")]
    public void ConfigureCameraProjection()
    {
        if (createdVirtualCamera == null) return;

#if CINEMACHINE_3 || UNITY_6000_0_OR_NEWER
        // In Cinemachine 3.x, Lens is a struct - get it, modify it, set it back
        var lens = createdVirtualCamera.Lens;
        if (useOrthographic)
        {
            lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
            lens.OrthographicSize = orthographicSize;
        }
        else
        {
            lens.ModeOverride = LensSettings.OverrideModes.Perspective;
            lens.FieldOfView = fieldOfView;
        }
        createdVirtualCamera.Lens = lens;
#else
        createdVirtualCamera.m_Lens.Orthographic = useOrthographic;
        if (useOrthographic)
        {
            createdVirtualCamera.m_Lens.OrthographicSize = orthographicSize;
        }
        else
        {
            createdVirtualCamera.m_Lens.FieldOfView = fieldOfView;
        }
#endif
    }

    /// <summary>
    /// Gets the created rig transform.
    /// </summary>
    public Transform GetRig() => createdRig;

    /// <summary>
    /// Gets the created virtual camera.
    /// </summary>
#if CINEMACHINE_3 || UNITY_6000_0_OR_NEWER
    public CinemachineCamera GetVirtualCamera() => createdVirtualCamera;
#else
    public CinemachineVirtualCamera GetVirtualCamera() => createdVirtualCamera;
#endif

    #endregion

    #region Private Methods

    private void ConnectToRotationController()
    {
        var rotationController = FindObjectOfType<FezCinemachineRotation>();
        if (rotationController != null)
        {
            rotationController.pivotRig = createdRig;
            rotationController.followTarget = followTarget;
            rotationController.rotationMethod = FezCinemachineRotation.RotationMethod.RotatePivotRig;
            Debug.Log("[FezCinemachineRigSetup] Auto-connected to FezCinemachineRotation controller.");
        }
    }

    #endregion

    #region Editor Gizmos

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector3 center = followTarget != null ? followTarget.position : transform.position;
        center.y = 0f;

        // Draw camera position preview
        Vector3 camPos = center + new Vector3(0f, cameraHeight, cameraDistance);
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(camPos, 0.5f);
        Gizmos.DrawLine(center + Vector3.up * cameraHeight, camPos);

        // Draw rotation preview
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        for (int i = 0; i < 36; i++)
        {
            float a1 = i * 10f * Mathf.Deg2Rad;
            float a2 = (i + 1) * 10f * Mathf.Deg2Rad;
            
            Vector3 p1 = center + new Vector3(Mathf.Sin(a1) * cameraDistance, cameraHeight, Mathf.Cos(a1) * cameraDistance);
            Vector3 p2 = center + new Vector3(Mathf.Sin(a2) * cameraDistance, cameraHeight, Mathf.Cos(a2) * cameraDistance);
            
            Gizmos.DrawLine(p1, p2);
        }

        // Draw orthographic bounds preview
        if (useOrthographic)
        {
            Gizmos.color = Color.yellow;
            float aspect = 16f / 9f; // Assume 16:9
            float halfHeight = orthographicSize;
            float halfWidth = halfHeight * aspect;
            
            Vector3 lookPoint = center + Vector3.up * cameraHeight;
            Vector3 right = Vector3.right;
            Vector3 up = Vector3.up;
            
            Vector3 tl = lookPoint + up * halfHeight - right * halfWidth;
            Vector3 tr = lookPoint + up * halfHeight + right * halfWidth;
            Vector3 bl = lookPoint - up * halfHeight - right * halfWidth;
            Vector3 br = lookPoint - up * halfHeight + right * halfWidth;
            
            Gizmos.DrawLine(tl, tr);
            Gizmos.DrawLine(tr, br);
            Gizmos.DrawLine(br, bl);
            Gizmos.DrawLine(bl, tl);
        }
    }
#endif

    #endregion
}
