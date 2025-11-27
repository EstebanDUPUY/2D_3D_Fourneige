using UnityEngine;

/// <summary>
/// Configures camera for Fez-style orthographic visuals.
/// Provides smooth zoom and projection transitions.
/// 
/// Core Systems:
/// - Orthographic projection for true 2D appearance
/// - Smooth zoom transitions
/// - Optional perspective mode for debugging
/// 
/// Usage:
/// 1. Attach to your Main Camera
/// 2. Enable orthographic mode for Fez-style visuals
/// 3. Adjust orthographic size for desired zoom level
/// </summary>
[RequireComponent(typeof(Camera))]
public class _FezCameraSetup : MonoBehaviour
{
    #region Variables

    // ==================== PROJECTION SETTINGS ====================
    #region PROJECTION SETTINGS

    [Header("Projection Settings")]

    /// <summary>
    /// If TRUE, uses orthographic projection (true Fez style).
    /// If FALSE, uses perspective projection.
    /// </summary>
    [Tooltip("Use orthographic projection for Fez-style 2D look")]
    public bool useOrthographic = true;

    /// <summary>
    /// Size of the orthographic camera view.
    /// Smaller values = more zoomed in.
    /// Typical range: 5-15
    /// </summary>
    [Tooltip("Orthographic camera size (zoom level)")]
    public float orthographicSize = 8f;

    /// <summary>
    /// Field of view when using perspective projection.
    /// Only used when useOrthographic is FALSE.
    /// </summary>
    [Tooltip("Field of view for perspective mode")]
    [Range(30f, 120f)]
    public float perspectiveFOV = 60f;

    #endregion

    // ==================== CLIPPING PLANES ====================
    #region CLIPPING PLANES

    [Header("Clipping Planes")]

    /// <summary>
    /// Near clipping plane distance.
    /// Objects closer than this are not rendered.
    /// </summary>
    [Tooltip("Near clipping plane")]
    public float nearClip = 0.1f;

    /// <summary>
    /// Far clipping plane distance.
    /// Objects farther than this are not rendered.
    /// </summary>
    [Tooltip("Far clipping plane")]
    public float farClip = 100f;

    #endregion

    // ==================== ZOOM SETTINGS ====================
    #region ZOOM SETTINGS

    [Header("Zoom Settings")]

    /// <summary>
    /// If TRUE, enables smooth zoom transitions.
    /// If FALSE, zoom changes are instant.
    /// </summary>
    [Tooltip("Enable smooth zoom transitions")]
    public bool smoothZoom = true;

    /// <summary>
    /// Duration of smooth zoom transition in seconds.
    /// </summary>
    [Tooltip("Duration of zoom transition")]
    public float zoomDuration = 0.3f;

    /// <summary>
    /// Animation curve for zoom easing.
    /// </summary>
    [Tooltip("Easing curve for zoom")]
    public AnimationCurve zoomCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    #endregion

    // ==================== DEBUG ====================
    #region DEBUG

    [Header("Debug")]

    [Tooltip("Show debug info in console")]
    public bool showDebugLogs = false;

    #endregion

    // ==================== PRIVATE VARIABLES ====================
    #region PRIVATE VARIABLES

    // Cached camera component
    private Camera cam;

    // Zoom transition state
    private Coroutine zoomCoroutine;
    private float targetZoom;

    #endregion

    #endregion

    // ==================== UNITY LIFECYCLE ====================
    #region UNITY LIFECYCLE

    private void Awake()
    {
        cam = GetComponent<Camera>();
        targetZoom = orthographicSize;
    }

    private void Start()
    {
        ApplySettings();
    }

    private void OnValidate()
    {
        // Apply settings in editor when values change
        if (cam == null)
        {
            cam = GetComponent<Camera>();
        }

        if (cam != null)
        {
            ApplySettings();
        }
    }

    #endregion

    // ==================== SETTINGS APPLICATION ====================
    #region SETTINGS APPLICATION

    /// <summary>
    /// Applies all camera settings.
    /// Called on Start and when values change in editor.
    /// </summary>
    public void ApplySettings()
    {
        if (cam == null)
        {
            return;
        }

        // Projection mode
        cam.orthographic = useOrthographic;

        // Size/FOV
        if (useOrthographic)
        {
            cam.orthographicSize = orthographicSize;
        }
        else
        {
            cam.fieldOfView = perspectiveFOV;
        }

        // Clipping planes
        cam.nearClipPlane = nearClip;
        cam.farClipPlane = farClip;

        if (showDebugLogs)
        {
            Debug.Log($"[_FezCameraSetup] Settings applied: ortho={useOrthographic}, size={orthographicSize}");
        }
    }

    #endregion

    // ==================== PROJECTION CONTROL ====================
    #region PROJECTION CONTROL

    /// <summary>
    /// Sets the projection mode.
    /// </summary>
    /// <param name="orthographic">TRUE for orthographic, FALSE for perspective</param>
    /// <param name="instant">If TRUE, changes instantly. If FALSE, may animate.</param>
    public void SetOrthographic(bool orthographic, bool instant = true)
    {
        useOrthographic = orthographic;

        if (instant)
        {
            ApplySettings();
        }
        else
        {
            // Note: Unity doesn't support smooth ortho/perspective transitions
            // This is left as a placeholder for potential future implementation
            ApplySettings();
        }

        if (showDebugLogs)
        {
            Debug.Log($"[_FezCameraSetup] Projection set to {(orthographic ? "Orthographic" : "Perspective")}");
        }
    }

    #endregion

    // ==================== ZOOM CONTROL ====================
    #region ZOOM CONTROL

    /// <summary>
    /// Sets the zoom level (orthographic size).
    /// </summary>
    /// <param name="newSize">Target orthographic size</param>
    /// <param name="instant">If TRUE, changes instantly</param>
    public void SetZoom(float newSize, bool instant = false)
    {
        targetZoom = newSize;

        if (instant || !smoothZoom)
        {
            orthographicSize = newSize;
            if (useOrthographic && cam != null)
            {
                cam.orthographicSize = newSize;
            }
        }
        else
        {
            // Start smooth zoom
            if (zoomCoroutine != null)
            {
                StopCoroutine(zoomCoroutine);
            }
            zoomCoroutine = StartCoroutine(SmoothZoomCoroutine(newSize));
        }
    }

    /// <summary>
    /// Coroutine for smooth zoom transition.
    /// </summary>
    private System.Collections.IEnumerator SmoothZoomCoroutine(float targetSize)
    {
        float startSize = orthographicSize;
        float elapsed = 0f;

        while (elapsed < zoomDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / zoomDuration;
            float curvedT = zoomCurve.Evaluate(t);

            orthographicSize = Mathf.Lerp(startSize, targetSize, curvedT);

            if (useOrthographic && cam != null)
            {
                cam.orthographicSize = orthographicSize;
            }

            yield return null;
        }

        // Ensure final value is exact
        orthographicSize = targetSize;
        if (useOrthographic && cam != null)
        {
            cam.orthographicSize = targetSize;
        }

        zoomCoroutine = null;

        if (showDebugLogs)
        {
            Debug.Log($"[_FezCameraSetup] Zoom complete: size={targetSize}");
        }
    }

    /// <summary>
    /// Zooms in by a relative amount.
    /// </summary>
    /// <param name="amount">Amount to zoom in (positive = zoom in)</param>
    public void ZoomIn(float amount = 1f)
    {
        SetZoom(orthographicSize - amount);
    }

    /// <summary>
    /// Zooms out by a relative amount.
    /// </summary>
    /// <param name="amount">Amount to zoom out (positive = zoom out)</param>
    public void ZoomOut(float amount = 1f)
    {
        SetZoom(orthographicSize + amount);
    }

    #endregion

    // ==================== PUBLIC GETTERS ====================
    #region PUBLIC GETTERS

    /// <summary>
    /// Gets the current orthographic size.
    /// </summary>
    public float GetCurrentZoom()
    {
        return orthographicSize;
    }

    /// <summary>
    /// Gets the target zoom (may differ from current during transition).
    /// </summary>
    public float GetTargetZoom()
    {
        return targetZoom;
    }

    /// <summary>
    /// Returns TRUE if currently in orthographic mode.
    /// </summary>
    public bool IsOrthographic()
    {
        return useOrthographic;
    }

    /// <summary>
    /// Returns TRUE if a zoom transition is in progress.
    /// </summary>
    public bool IsZooming()
    {
        return zoomCoroutine != null;
    }

    #endregion
}