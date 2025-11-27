using UnityEngine;

/// <summary>
/// Simple trigger zone for Fez-style rotation.
/// Place on trigger colliders to rotate player and camera perspective.
/// 
/// Setup:
/// 1. Create GameObject with Collider (Is Trigger = ON)
/// 2. Attach this script
/// 3. Set direction (Left or Right)
/// 4. Done! FezRotationSystem is found automatically
/// </summary>
[RequireComponent(typeof(Collider))]
public class FezRotationTrigger : MonoBehaviour
{
    #region Variables
    
    // ==================== SETTINGS ====================
    //[Header("Trigger Settings")]
    
    public enum RotationDirection { Left, Right }
    
    [Tooltip("Which way to rotate")]
    public RotationDirection direction = RotationDirection.Right;
    
    [Tooltip("Player GameObject tag")]
    public string playerTag = "Player";
    
    [Tooltip("Trigger only once?")]
    public bool oneTimeUse = false;
    
    [Tooltip("Require player grounded?")]
    public bool requireGrounded = false;
    
    // ==================== DEBUG ====================
    [Header("Debug (Read Only)")]
    
    [Tooltip("Player inside trigger?")]
    public bool playerInside = false;
    
    [Tooltip("Trigger used?")]
    public bool hasBeenUsed = false;
    
    // ==================== VISUALS ====================
    [Header("Visuals")]
    
    [Tooltip("Gizmo color")]
    public Color gizmoColor = Color.yellow;
    
    [Tooltip("Arrow size")]
    public float arrowSize = 1f;
    
    // Private
    private FezRotationSystem rotationSystem;
    
    #endregion
    
    // ==================== UNITY LIFECYCLE ====================
    #region UNITY LIFECYCLE
    
    private void Awake()
    {
        // Validate trigger
        Collider col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            Debug.LogError($"[FezTrigger] '{name}' collider is not a trigger! Enable 'Is Trigger'.");
        }
        
        // Find rotation system
        rotationSystem = FindObjectOfType<FezRotationSystem>();
        if (rotationSystem == null)
        {
            Debug.LogError($"[FezTrigger] FezRotationSystem not found! Attach to Main Camera.");
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Check player tag
        if (!other.CompareTag(playerTag))
        {
            return;
        }
        
        playerInside = true;
        
        // Check if can trigger
        if (!CanTrigger())
        {
            return;
        }
        
        // Optional: Check grounded
        if (requireGrounded)
        {
            _PlayerController pc = other.GetComponent<_PlayerController>();
            // TODO: Add public isGrounded to PlayerController
            // For now, always allow
        }
        
        // Trigger rotation
        TriggerRotation();
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInside = false;
        }
    }
    
    #endregion
    
    // ==================== TRIGGER LOGIC ====================
    #region TRIGGER LOGIC
    
    private bool CanTrigger()
    {
        if (rotationSystem == null)
        {
            Debug.LogError("[FezTrigger] No rotation system!");
            return false;
        }
        
        if (rotationSystem.isRotating)
        {
            return false;
        }
        
        if (oneTimeUse && hasBeenUsed)
        {
            return false;
        }
        
        return true;
    }
    
    private void TriggerRotation()
    {
        bool success = false;
        
        if (direction == RotationDirection.Right)
        {
            success = rotationSystem.RotateRight();
            Debug.Log($"[FezTrigger] Triggered RIGHT from '{name}'");
        }
        else
        {
            success = rotationSystem.RotateLeft();
            Debug.Log($"[FezTrigger] Triggered LEFT from '{name}'");
        }
        
        if (success && oneTimeUse)
        {
            hasBeenUsed = true;
        }
    }
    
    public void ResetTrigger()
    {
        hasBeenUsed = false;
        playerInside = false;
    }
    
    #endregion
    
    // ==================== DEBUG ====================
    #region DEBUG
    
    private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) return;
        
        // Color based on state
        Color color = gizmoColor;
        if (hasBeenUsed && oneTimeUse) color = Color.gray;
        else if (playerInside) color = Color.green;
        else if (rotationSystem != null && rotationSystem.isRotating) color = Color.red;
        
        Gizmos.color = color;
        
        // Draw bounds
        if (col is BoxCollider box)
        {
            Matrix4x4 m = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.matrix = m;
            Gizmos.DrawWireCube(box.center, box.size);
            Gizmos.matrix = Matrix4x4.identity;
        }
        else
        {
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
        
        // Draw arrow
        Vector3 center = transform.position;
        Vector3 arrowStart = center + transform.forward * arrowSize;
        
        if (direction == RotationDirection.Right)
        {
            // Curved arrow right
            Vector3 arrowEnd = center + transform.right * arrowSize;
            Gizmos.DrawLine(arrowStart, arrowEnd);
            
            // Arrowhead
            Gizmos.DrawLine(arrowEnd, arrowEnd - transform.right * 0.3f * arrowSize + transform.up * 0.2f * arrowSize);
            Gizmos.DrawLine(arrowEnd, arrowEnd - transform.right * 0.3f * arrowSize - transform.up * 0.2f * arrowSize);
        }
        else
        {
            // Curved arrow left
            Vector3 arrowEnd = center - transform.right * arrowSize;
            Gizmos.DrawLine(arrowStart, arrowEnd);
            
            // Arrowhead
            Gizmos.DrawLine(arrowEnd, arrowEnd + transform.right * 0.3f * arrowSize + transform.up * 0.2f * arrowSize);
            Gizmos.DrawLine(arrowEnd, arrowEnd + transform.right * 0.3f * arrowSize - transform.up * 0.2f * arrowSize);
        }
    }
    
    #endregion
}