using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Displays player stats (stamina, dash charges, speed bonus, state) from Fused_PlayerController.
/// Attach this to your Canvas and assign references in the Inspector.
/// 
/// SETUP:
/// 1. Add this script to your Canvas
/// 2. Drag your Player (with Fused_PlayerController) to Player Controller field
/// 3. Create UI elements (sliders, text, images) and assign them to the appropriate fields
/// 4. Configure colors and animation settings as desired
/// 
/// All fields are optional - only assign what you need!
/// </summary>
public class PlayerStatsUI : MonoBehaviour
{
    #region ═══════════════════════════════════════════════════════════════════
    //                           PLAYER REFERENCE
    #endregion ════════════════════════════════════════════════════════════════
    
    [Header("═══ PLAYER REFERENCE ═══")]
    [Tooltip("Drag your Player GameObject here (will auto-find if left empty)")]
    public FusedPlayerController playerController;
    
    #region ═══════════════════════════════════════════════════════════════════
    //                              STAMINA UI
    #endregion ════════════════════════════════════════════════════════════════
    
    [Header("═══ STAMINA UI ═══")]
    [Tooltip("Slider showing current stamina (0-1 range)")]
    public Slider staminaSlider;
    
    [Tooltip("The Fill image of the slider - will change color based on stamina level")]
    public Image staminaFillImage;
    
    [Tooltip("Text showing stamina as numbers like '75/100'")]
    public TMP_Text staminaText;
    
    [Tooltip("Alternative: Regular Unity Text instead of TextMeshPro")]
    public Text staminaTextLegacy;
    
    [Tooltip("Color when stamina is full (100%)")]
    public Color staminaFullColor = new Color(0.2f, 0.8f, 0.2f, 1f); // Green
    
    [Tooltip("Color when stamina is low")]
    public Color staminaLowColor = new Color(0.8f, 0.2f, 0.2f, 1f); // Red
    
    [Tooltip("Stamina percentage (0-1) below which it's considered 'low'")]
    [Range(0f, 1f)]
    public float staminaLowThreshold = 0.25f;
    
    [Tooltip("Show stamina only when not full (cleaner UI)")]
    public bool hideStaminaWhenFull = false;
    
    [Tooltip("Parent object to hide when stamina is full")]
    public GameObject staminaContainer;
    
    #region ═══════════════════════════════════════════════════════════════════
    //                           DASH UI (COUNTER)
    #endregion ════════════════════════════════════════════════════════════════
    
    [Header("═══ DASH UI (Counter Mode) ═══")]
    [Tooltip("Text showing dash charges as '2/3'")]
    public TMP_Text dashCounterText;
    
    [Tooltip("Alternative: Regular Unity Text")]
    public Text dashCounterTextLegacy;
    
    [Tooltip("Format string for dash counter. {0}=current, {1}=max")]
    public string dashCounterFormat = "{0}/{1}";
    
    #region ═══════════════════════════════════════════════════════════════════
    //                           DASH UI (ICONS)
    #endregion ════════════════════════════════════════════════════════════════
    
    [Header("═══ DASH UI (Icon Mode) ═══")]
    [Tooltip("Container with Horizontal Layout Group for dash icons")]
    public Transform dashIconContainer;
    
    [Tooltip("Prefab for a single dash charge icon (needs Image component)")]
    public GameObject dashIconPrefab;
    
    [Tooltip("Color when dash charge is available")]
    public Color dashAvailableColor = Color.white;
    
    [Tooltip("Color when dash charge is used/empty")]
    public Color dashUsedColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    
    [Tooltip("Scale multiplier when charge is available vs used")]
    public float dashAvailableScale = 1f;
    public float dashUsedScale = 0.8f;
    
    #region ═══════════════════════════════════════════════════════════════════
    //                         BUNNY HOP / SPEED UI
    #endregion ════════════════════════════════════════════════════════════════
    
    [Header("═══ SPEED BONUS UI ═══")]
    [Tooltip("Slider showing bunny hop speed bonus")]
    public Slider speedBonusSlider;
    
    [Tooltip("Text showing speed multiplier like '1.5x'")]
    public TMP_Text speedBonusText;
    
    [Tooltip("Maximum expected speed bonus (for slider max value)")]
    public float maxExpectedSpeedBonus = 1f;
    
    [Tooltip("Hide speed UI when no bonus active")]
    public bool hideSpeedWhenZero = true;
    
    [Tooltip("Container to hide when speed bonus is zero")]
    public GameObject speedContainer;
    
    #region ═══════════════════════════════════════════════════════════════════
    //                          STATE INDICATOR
    #endregion ════════════════════════════════════════════════════════════════
    
    [Header("═══ STATE INDICATOR ═══")]
    [Tooltip("Text showing current state name from StateData")]
    public TMP_Text stateNameText;
    
    [Tooltip("Image that shows the state's assigned color")]
    public Image stateColorIndicator;
    
    [Tooltip("Text showing state index like 'State 1' or 'State 2'")]
    public TMP_Text stateIndexText;
    
    #region ═══════════════════════════════════════════════════════════════════
    //                         STATUS INDICATORS
    #endregion ════════════════════════════════════════════════════════════════
    
    [Header("═══ STATUS INDICATORS ═══")]
    [Tooltip("Shows when player is wall sliding or clinging")]
    public GameObject wallSlideIndicator;
    
    [Tooltip("Shows when player is actively dashing")]
    public GameObject dashingIndicator;
    
    [Tooltip("Shows when player is in landing lag")]
    public GameObject landingLagIndicator;
    
    [Tooltip("Shows when player is invincible (during dash)")]
    public GameObject invincibleIndicator;
    
    [Tooltip("Shows when player is at jump apex")]
    public GameObject apexIndicator;
    
    [Tooltip("Shows when player is grounded")]
    public GameObject groundedIndicator;
    
    #region ═══════════════════════════════════════════════════════════════════
    //                        ANIMATION SETTINGS
    #endregion ════════════════════════════════════════════════════════════════
    
    [Header("═══ ANIMATION SETTINGS ═══")]
    [Tooltip("How fast sliders smoothly animate to new values")]
    [Range(1f, 50f)]
    public float sliderLerpSpeed = 10f;
    
    [Tooltip("Pulse/throb the stamina bar when low")]
    public bool pulseWhenLow = true;
    
    [Tooltip("Speed of the low stamina pulse animation")]
    public float pulseSpeed = 4f;
    
    [Tooltip("Intensity of the pulse (scale multiplier)")]
    [Range(0f, 0.2f)]
    public float pulseIntensity = 0.05f;
    
    [Tooltip("Shake UI when stamina depletes")]
    public bool shakeOnDeplete = true;
    
    [Tooltip("Shake intensity in pixels")]
    public float shakeIntensity = 5f;
    
    [Tooltip("Shake duration in seconds")]
    public float shakeDuration = 0.3f;
    
    [Tooltip("Pop/scale animation when dash refills")]
    public bool popOnDashRefill = true;
    
    [Tooltip("Scale multiplier for pop animation")]
    public float popScale = 1.3f;
    
    [Tooltip("Duration of pop animation")]
    public float popDuration = 0.15f;
    
    #region ═══════════════════════════════════════════════════════════════════
    //                          PRIVATE VARIABLES
    #endregion ════════════════════════════════════════════════════════════════
    
    // Dash icon tracking
    private List<Image> dashIcons = new List<Image>();
    private List<RectTransform> dashIconRects = new List<RectTransform>();
    private int lastMaxDashCharges = -1;
    private int lastCurrentDashCharges = -1;
    
    // Animation state
    private float targetStaminaValue;
    private float targetSpeedValue;
    private RectTransform staminaRect;
    private Vector3 staminaOriginalScale;
    private bool isShaking = false;
    
    // Cached values for change detection
    private float lastStamina = -1f;
    private string lastStateName = "";
    
    #region ═══════════════════════════════════════════════════════════════════
    //                           UNITY CALLBACKS
    #endregion ════════════════════════════════════════════════════════════════
    
    void Start()
    {
        InitializeUI();
    }
    
    void Update()
    {
        if (playerController == null) return;
        
        UpdateStaminaUI();
        UpdateDashUI();
        UpdateSpeedBonusUI();
        UpdateStateUI();
        UpdateStatusIndicators();
    }
    
    void OnEnable()
    {
        SubscribeToEvents();
    }
    
    void OnDisable()
    {
        UnsubscribeFromEvents();
    }
    
    #region ═══════════════════════════════════════════════════════════════════
    //                          INITIALIZATION
    #endregion ════════════════════════════════════════════════════════════════
    
    void InitializeUI()
    {
        // Auto-find player controller if not assigned
        if (playerController == null)
        {
            playerController = FindObjectOfType<FusedPlayerController>();
            if (playerController == null)
            {
                Debug.LogError("[PlayerStatsUI] No Fused_PlayerController found in scene! Please assign one in the Inspector.");
                enabled = false;
                return;
            }
            Debug.Log("[PlayerStatsUI] Auto-found player controller: " + playerController.gameObject.name);
        }
        
        // Cache stamina rect transform for animations
        if (staminaSlider != null)
        {
            staminaRect = staminaSlider.GetComponent<RectTransform>();
            staminaOriginalScale = staminaRect.localScale;
        }
        
        // Initialize dash icons if using icon mode
        if (dashIconContainer != null && dashIconPrefab != null)
        {
            int maxCharges = playerController.GetMaxDashCharges();
            RebuildDashIcons(maxCharges);
            lastMaxDashCharges = maxCharges;
            lastCurrentDashCharges = playerController.GetCurrentDashCharges();
        }
        
        // Initial update
        ForceUpdateAll();
    }
    
    void SubscribeToEvents()
    {
        if (playerController == null) return;
        
        // Subscribe to player events for animations (C# event syntax)
        playerController.OnStaminaDepleted += HandleStaminaDepleted;
        playerController.OnDashStarted += HandleDashStarted;
        playerController.OnDashEnded += HandleDashEnded;
    }
    
    void UnsubscribeFromEvents()
    {
        if (playerController == null) return;
        
        playerController.OnStaminaDepleted -= HandleStaminaDepleted;
        playerController.OnDashStarted -= HandleDashStarted;
        playerController.OnDashEnded -= HandleDashEnded;
    }
    
    #region ═══════════════════════════════════════════════════════════════════
    //                          STAMINA UPDATE
    #endregion ════════════════════════════════════════════════════════════════
    
    void UpdateStaminaUI()
    {
        // Skip if no stamina UI elements assigned
        if (staminaSlider == null && staminaText == null && staminaTextLegacy == null) return;
        
        float currentStamina = playerController.GetCurrentStamina();
        float maxStamina = playerController.GetMaxStamina();
        float staminaPercent = maxStamina > 0 ? currentStamina / maxStamina : 0f;
        
        // Handle visibility for "hide when full" option
        if (hideStaminaWhenFull && staminaContainer != null)
        {
            bool shouldShow = staminaPercent < 0.99f;
            if (staminaContainer.activeSelf != shouldShow)
            {
                staminaContainer.SetActive(shouldShow);
            }
        }
        
        // Update slider with smooth animation
        if (staminaSlider != null)
        {
            targetStaminaValue = staminaPercent;
            staminaSlider.value = Mathf.Lerp(staminaSlider.value, targetStaminaValue, Time.deltaTime * sliderLerpSpeed);
            
            // Update fill color (gradient from low to full)
            if (staminaFillImage != null)
            {
                staminaFillImage.color = Color.Lerp(staminaLowColor, staminaFullColor, staminaPercent);
            }
            
            // Pulse animation when low
            if (pulseWhenLow && staminaPercent < staminaLowThreshold && staminaRect != null && !isShaking)
            {
                float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseIntensity;
                staminaRect.localScale = staminaOriginalScale * pulse;
            }
            else if (staminaRect != null && !isShaking)
            {
                staminaRect.localScale = staminaOriginalScale;
            }
        }
        
        // Update text displays
        string staminaString = $"{Mathf.RoundToInt(currentStamina)}/{Mathf.RoundToInt(maxStamina)}";
        
        if (staminaText != null)
        {
            staminaText.text = staminaString;
        }
        if (staminaTextLegacy != null)
        {
            staminaTextLegacy.text = staminaString;
        }
        
        lastStamina = currentStamina;
    }
    
    #region ═══════════════════════════════════════════════════════════════════
    //                            DASH UPDATE
    #endregion ════════════════════════════════════════════════════════════════
    
    void UpdateDashUI()
    {
        int currentCharges = playerController.GetCurrentDashCharges();
        int maxCharges = playerController.GetMaxDashCharges();
        
        // Update counter text
        if (dashCounterText != null)
        {
            dashCounterText.text = string.Format(dashCounterFormat, currentCharges, maxCharges);
        }
        if (dashCounterTextLegacy != null)
        {
            dashCounterTextLegacy.text = string.Format(dashCounterFormat, currentCharges, maxCharges);
        }
        
        // Update icon display
        if (dashIconContainer != null)
        {
            // Rebuild icons if max charges changed (e.g., state switch)
            if (maxCharges != lastMaxDashCharges)
            {
                RebuildDashIcons(maxCharges);
                lastMaxDashCharges = maxCharges;
            }
            
            // Check for refill (current charges increased)
            if (currentCharges > lastCurrentDashCharges && popOnDashRefill)
            {
                // Play pop animation on refilled charges
                for (int i = lastCurrentDashCharges; i < currentCharges && i < dashIcons.Count; i++)
                {
                    if (dashIconRects[i] != null)
                    {
                        StartCoroutine(PopAnimationCoroutine(dashIconRects[i]));
                    }
                }
            }
            
            // Update icon appearance
            for (int i = 0; i < dashIcons.Count; i++)
            {
                bool isAvailable = i < currentCharges;
                
                if (dashIcons[i] != null)
                {
                    dashIcons[i].color = isAvailable ? dashAvailableColor : dashUsedColor;
                }
                
                if (dashIconRects[i] != null)
                {
                    float targetScale = isAvailable ? dashAvailableScale : dashUsedScale;
                    // Don't override if currently animating
                    // Simple approach: just set scale directly (animation will override temporarily)
                }
            }
            
            lastCurrentDashCharges = currentCharges;
        }
    }
    
    void RebuildDashIcons(int count)
    {
        // Destroy existing icons
        foreach (var icon in dashIcons)
        {
            if (icon != null && icon.gameObject != null)
            {
                Destroy(icon.gameObject);
            }
        }
        dashIcons.Clear();
        dashIconRects.Clear();
        
        // Create new icons
        if (dashIconPrefab == null || dashIconContainer == null) return;
        
        for (int i = 0; i < count; i++)
        {
            GameObject iconObj = Instantiate(dashIconPrefab, dashIconContainer);
            iconObj.name = $"DashIcon_{i}";
            
            Image iconImage = iconObj.GetComponent<Image>();
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            
            if (iconImage != null)
            {
                dashIcons.Add(iconImage);
            }
            else
            {
                dashIcons.Add(null);
                Debug.LogWarning($"[PlayerStatsUI] Dash icon prefab missing Image component!");
            }
            
            dashIconRects.Add(iconRect);
        }
    }
    
    #region ═══════════════════════════════════════════════════════════════════
    //                        SPEED BONUS UPDATE
    #endregion ════════════════════════════════════════════════════════════════
    
    void UpdateSpeedBonusUI()
    {
        if (speedBonusSlider == null && speedBonusText == null) return;
        
        float bonus = playerController.GetBunnyHopBonus();
        
        // Handle visibility
        if (hideSpeedWhenZero && speedContainer != null)
        {
            bool shouldShow = bonus > 0.01f;
            if (speedContainer.activeSelf != shouldShow)
            {
                speedContainer.SetActive(shouldShow);
            }
        }
        
        // Update slider
        if (speedBonusSlider != null)
        {
            targetSpeedValue = bonus / maxExpectedSpeedBonus;
            speedBonusSlider.value = Mathf.Lerp(speedBonusSlider.value, targetSpeedValue, Time.deltaTime * sliderLerpSpeed);
        }
        
        // Update text
        if (speedBonusText != null)
        {
            if (bonus > 0.01f)
            {
                speedBonusText.text = $"{1f + bonus:F1}x";
            }
            else
            {
                speedBonusText.text = "1.0x";
            }
        }
    }
    
    #region ═══════════════════════════════════════════════════════════════════
    //                          STATE UPDATE
    #endregion ════════════════════════════════════════════════════════════════
    
    void UpdateStateUI()
    {
        var stateData = playerController.GetCurrentStateData();
        if (stateData == null) return;
        
        // Only update if changed (optimization)
        if (stateData.stateName != lastStateName)
        {
            if (stateNameText != null)
            {
                stateNameText.text = stateData.stateName;
            }
            
            if (stateColorIndicator != null)
            {
                stateColorIndicator.color = stateData.stateColor;
            }
            
            lastStateName = stateData.stateName;
        }
        
        if (stateIndexText != null)
        {
            stateIndexText.text = $"State {playerController.GetCurrentStateIndex() + 1}";
        }
    }
    
    #region ═══════════════════════════════════════════════════════════════════
    //                       STATUS INDICATORS
    #endregion ════════════════════════════════════════════════════════════════
    
    void UpdateStatusIndicators()
    {
        // Wall slide/cling indicator
        if (wallSlideIndicator != null)
        {
            bool isOnWall = playerController.IsWallSliding() || playerController.IsWallClinging() || playerController.IsWallClimbing();
            if (wallSlideIndicator.activeSelf != isOnWall)
            {
                wallSlideIndicator.SetActive(isOnWall);
            }
        }
        
        // Dashing indicator
        if (dashingIndicator != null)
        {
            bool isDashing = playerController.IsDashing();
            if (dashingIndicator.activeSelf != isDashing)
            {
                dashingIndicator.SetActive(isDashing);
            }
        }
        
        // Landing lag indicator
        if (landingLagIndicator != null)
        {
            bool inLag = playerController.IsInLandingLag();
            if (landingLagIndicator.activeSelf != inLag)
            {
                landingLagIndicator.SetActive(inLag);
            }
        }
        
        // Invincible indicator
        if (invincibleIndicator != null)
        {
            bool invincible = playerController.IsInvincible();
            if (invincibleIndicator.activeSelf != invincible)
            {
                invincibleIndicator.SetActive(invincible);
            }
        }
        
        // Apex indicator
        if (apexIndicator != null)
        {
            bool atApex = playerController.IsAtApex();
            if (apexIndicator.activeSelf != atApex)
            {
                apexIndicator.SetActive(atApex);
            }
        }
        
        // Grounded indicator
        if (groundedIndicator != null)
        {
            bool grounded = playerController.IsGrounded();
            if (groundedIndicator.activeSelf != grounded)
            {
                groundedIndicator.SetActive(grounded);
            }
        }
    }
    
    #region ═══════════════════════════════════════════════════════════════════
    //                          EVENT HANDLERS
    #endregion ════════════════════════════════════════════════════════════════
    
    void HandleStaminaDepleted()
    {
        if (shakeOnDeplete)
        {
            StartCoroutine(ShakeCoroutine());
        }
        FlashStamina();
    }
    
    void HandleDashStarted(Vector3 direction)
    {
        // Could add effects here
    }
    
    void HandleDashEnded()
    {
        // Could add effects here
    }
    
    #region ═══════════════════════════════════════════════════════════════════
    //                         PUBLIC METHODS
    #endregion ════════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Force update all UI elements immediately (no lerping)
    /// </summary>
    public void ForceUpdateAll()
    {
        if (playerController == null) return;
        
        // Stamina
        float staminaPercent = playerController.GetMaxStamina() > 0 
            ? playerController.GetCurrentStamina() / playerController.GetMaxStamina() 
            : 0f;
        if (staminaSlider != null) staminaSlider.value = staminaPercent;
        
        // Speed
        float speedPercent = playerController.GetBunnyHopBonus() / maxExpectedSpeedBonus;
        if (speedBonusSlider != null) speedBonusSlider.value = speedPercent;
        
        // Force update all sections
        UpdateStaminaUI();
        UpdateDashUI();
        UpdateSpeedBonusUI();
        UpdateStateUI();
        UpdateStatusIndicators();
    }
    
    /// <summary>
    /// Flash the stamina bar red briefly
    /// </summary>
    public void FlashStamina()
    {
        if (staminaFillImage != null)
        {
            StartCoroutine(FlashCoroutine(staminaFillImage, Color.red, 0.15f));
        }
    }
    
    /// <summary>
    /// Flash all dash icons red (e.g., when trying to dash with no charges)
    /// </summary>
    public void FlashDashIcons()
    {
        foreach (var icon in dashIcons)
        {
            if (icon != null)
            {
                StartCoroutine(FlashCoroutine(icon, Color.red, 0.15f));
            }
        }
    }
    
    /// <summary>
    /// Manually trigger shake animation on stamina bar
    /// </summary>
    public void ShakeStaminaBar()
    {
        if (!isShaking && staminaRect != null)
        {
            StartCoroutine(ShakeCoroutine());
        }
    }
    
    #region ═══════════════════════════════════════════════════════════════════
    //                          COROUTINES
    #endregion ════════════════════════════════════════════════════════════════
    
    IEnumerator FlashCoroutine(Image target, Color flashColor, float duration)
    {
        if (target == null) yield break;
        
        Color originalColor = target.color;
        target.color = flashColor;
        yield return new WaitForSeconds(duration);
        
        // Restore (may have changed during flash)
        if (target != null)
        {
            target.color = originalColor;
        }
    }
    
    IEnumerator ShakeCoroutine()
    {
        if (staminaRect == null) yield break;
        
        isShaking = true;
        Vector2 originalPos = staminaRect.anchoredPosition;
        float elapsed = 0f;
        
        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeIntensity * (1f - elapsed / shakeDuration);
            float y = Random.Range(-1f, 1f) * shakeIntensity * (1f - elapsed / shakeDuration);
            staminaRect.anchoredPosition = originalPos + new Vector2(x, y);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        staminaRect.anchoredPosition = originalPos;
        isShaking = false;
    }
    
    IEnumerator PopAnimationCoroutine(RectTransform target)
    {
        if (target == null) yield break;
        
        Vector3 originalScale = Vector3.one * dashAvailableScale;
        Vector3 popScaleVec = Vector3.one * popScale;
        
        target.localScale = popScaleVec;
        
        float elapsed = 0f;
        while (elapsed < popDuration)
        {
            float t = elapsed / popDuration;
            // Ease out
            t = 1f - (1f - t) * (1f - t);
            target.localScale = Vector3.Lerp(popScaleVec, originalScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        target.localScale = originalScale;
    }
}
