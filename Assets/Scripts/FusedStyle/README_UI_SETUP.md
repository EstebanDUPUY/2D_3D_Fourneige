# Player Stats UI Setup Guide

## Table of Contents
1. [Overview](#overview)
2. [Quick Setup (5 Minutes)](#quick-setup-5-minutes)
3. [UI Script](#ui-script)
4. [Detailed Canvas Setup](#detailed-canvas-setup)
5. [Stamina Bar Setup](#stamina-bar-setup)
6. [Dash Charges Setup](#dash-charges-setup)
7. [Advanced: Icon-Based Dash Display](#advanced-icon-based-dash-display)
8. [Advanced: Animated UI Effects](#advanced-animated-ui-effects)
9. [Complete Prefab Hierarchy](#complete-prefab-hierarchy)

---

## Overview

This tutorial shows how to create UI elements that display:
- **Stamina** (slider/bar that depletes during wall climbing)
- **Dash Charges** (icons or counter showing available dashes)
- **Bunny Hop Bonus** (optional speed indicator)
- **State Indicator** (which state data is active)

### What You'll Create

```
┌─────────────────────────────────────────┐
│  ❤️ STAMINA ████████████░░░░ 75/100     │
│  ⚡ DASH    ● ● ○  (2/3 charges)        │
│  🏃 SPEED   1.2x (bunny hop bonus)      │
└─────────────────────────────────────────┘
```

---

## Quick Setup (5 Minutes)

### Step 1: Create Canvas
1. Right-click in Hierarchy → **UI → Canvas**
2. Set Canvas Scaler:
   - UI Scale Mode: **Scale With Screen Size**
   - Reference Resolution: **1920 x 1080**
   - Match: **0.5**

### Step 2: Create Stamina Bar
1. Right-click on Canvas → **UI → Slider**
2. Name it: `StaminaSlider`
3. Anchor to top-left (hold Alt + click top-left preset)
4. Position: `(150, -50)`
5. Size: `(200, 30)`
6. Delete the **Handle Slide Area** child (we don't need it)
7. Set Slider values:
   - Min Value: `0`
   - Max Value: `100`
   - Value: `100`
   - Interactable: **OFF** (uncheck)

### Step 3: Create the UI Manager Script
1. Create new C# script: `PlayerStatsUI.cs`
2. Copy the code from [UI Script](#ui-script) section below
3. Add script to Canvas
4. Drag references in Inspector

### Step 4: Connect References
1. Select Canvas
2. Drag `Player` to **Player Controller** field
3. Drag `StaminaSlider` to **Stamina Slider** field

Done! Press Play and your stamina bar updates automatically.

---

## UI Script

Create this script and add it to your Canvas:

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Displays player stats (stamina, dash, speed) from Fused_PlayerController.
/// Attach to Canvas and assign references in Inspector.
/// </summary>
public class PlayerStatsUI : MonoBehaviour
{
    [Header("=== PLAYER REFERENCE ===")]
    [Tooltip("Drag your Player GameObject here")]
    public Fused_PlayerController playerController;
    
    [Header("=== STAMINA UI ===")]
    [Tooltip("Slider showing current stamina (optional)")]
    public Slider staminaSlider;
    
    [Tooltip("Image for fill - will change color based on stamina level (optional)")]
    public Image staminaFillImage;
    
    [Tooltip("Text showing stamina numbers like '75/100' (optional)")]
    public TMP_Text staminaText;
    
    [Tooltip("Color when stamina is full")]
    public Color staminaFullColor = new Color(0.2f, 0.8f, 0.2f); // Green
    
    [Tooltip("Color when stamina is low")]
    public Color staminaLowColor = new Color(0.8f, 0.2f, 0.2f); // Red
    
    [Tooltip("Threshold (0-1) below which stamina shows as 'low'")]
    [Range(0f, 1f)]
    public float staminaLowThreshold = 0.25f;
    
    [Header("=== DASH UI (Counter Mode) ===")]
    [Tooltip("Text showing dash charges like '2/3' (optional)")]
    public TMP_Text dashCounterText;
    
    [Header("=== DASH UI (Icon Mode) ===")]
    [Tooltip("Parent object containing dash charge icons (optional)")]
    public Transform dashIconContainer;
    
    [Tooltip("Prefab for a single dash charge icon")]
    public GameObject dashIconPrefab;
    
    [Tooltip("Color when dash charge is available")]
    public Color dashAvailableColor = Color.white;
    
    [Tooltip("Color when dash charge is used")]
    public Color dashUsedColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    
    [Header("=== BUNNY HOP / SPEED UI ===")]
    [Tooltip("Slider showing bunny hop speed bonus (optional)")]
    public Slider speedBonusSlider;
    
    [Tooltip("Text showing speed multiplier like '1.5x' (optional)")]
    public TMP_Text speedBonusText;
    
    [Header("=== STATE INDICATOR ===")]
    [Tooltip("Text showing current state name (optional)")]
    public TMP_Text stateNameText;
    
    [Tooltip("Image that changes color based on state (optional)")]
    public Image stateColorIndicator;
    
    [Header("=== STATUS INDICATORS ===")]
    [Tooltip("GameObject to show/hide when wall sliding (optional)")]
    public GameObject wallSlideIndicator;
    
    [Tooltip("GameObject to show/hide when dashing (optional)")]
    public GameObject dashingIndicator;
    
    [Tooltip("GameObject to show/hide when in landing lag (optional)")]
    public GameObject landingLagIndicator;
    
    [Header("=== ANIMATION SETTINGS ===")]
    [Tooltip("How fast sliders animate to new values")]
    public float sliderLerpSpeed = 10f;
    
    [Tooltip("Pulse the stamina bar when low")]
    public bool pulseWhenLow = true;
    
    [Tooltip("Speed of low stamina pulse")]
    public float pulseSpeed = 4f;
    
    // Private cached values
    private List<Image> dashIcons = new List<Image>();
    private int lastMaxDashCharges = -1;
    private float targetStamina;
    private float targetSpeedBonus;
    private RectTransform staminaRect;
    private Vector3 staminaOriginalScale;
    
    void Start()
    {
        // Auto-find player if not assigned
        if (playerController == null)
        {
            playerController = FindObjectOfType<Fused_PlayerController>();
            if (playerController == null)
            {
                Debug.LogError("PlayerStatsUI: No Fused_PlayerController found! Please assign one.");
                enabled = false;
                return;
            }
        }
        
        // Cache stamina rect for pulse animation
        if (staminaSlider != null)
        {
            staminaRect = staminaSlider.GetComponent<RectTransform>();
            staminaOriginalScale = staminaRect.localScale;
        }
        
        // Initialize dash icons if using icon mode
        if (dashIconContainer != null && dashIconPrefab != null)
        {
            RebuildDashIcons(playerController.GetMaxDashCharges());
        }
        
        // Initial UI update
        UpdateAllUI();
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
    
    /// <summary>
    /// Updates the stamina slider and text
    /// </summary>
    void UpdateStaminaUI()
    {
        if (staminaSlider == null && staminaText == null) return;
        
        float currentStamina = playerController.GetCurrentStamina();
        float maxStamina = playerController.GetMaxStamina();
        float staminaPercent = maxStamina > 0 ? currentStamina / maxStamina : 0;
        
        // Smooth slider animation
        if (staminaSlider != null)
        {
            targetStamina = staminaPercent;
            staminaSlider.value = Mathf.Lerp(staminaSlider.value, targetStamina, Time.deltaTime * sliderLerpSpeed);
            
            // Update fill color
            if (staminaFillImage != null)
            {
                staminaFillImage.color = Color.Lerp(staminaLowColor, staminaFullColor, staminaPercent);
            }
            
            // Pulse animation when low
            if (pulseWhenLow && staminaPercent < staminaLowThreshold && staminaRect != null)
            {
                float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * 0.05f;
                staminaRect.localScale = staminaOriginalScale * pulse;
            }
            else if (staminaRect != null)
            {
                staminaRect.localScale = staminaOriginalScale;
            }
        }
        
        // Update text
        if (staminaText != null)
        {
            staminaText.text = $"{Mathf.RoundToInt(currentStamina)}/{Mathf.RoundToInt(maxStamina)}";
        }
    }
    
    /// <summary>
    /// Updates dash counter text or icons
    /// </summary>
    void UpdateDashUI()
    {
        int currentCharges = playerController.GetCurrentDashCharges();
        int maxCharges = playerController.GetMaxDashCharges();
        
        // Counter mode (text)
        if (dashCounterText != null)
        {
            dashCounterText.text = $"{currentCharges}/{maxCharges}";
        }
        
        // Icon mode
        if (dashIconContainer != null)
        {
            // Rebuild icons if max changed
            if (maxCharges != lastMaxDashCharges)
            {
                RebuildDashIcons(maxCharges);
                lastMaxDashCharges = maxCharges;
            }
            
            // Update icon colors
            for (int i = 0; i < dashIcons.Count; i++)
            {
                if (dashIcons[i] != null)
                {
                    dashIcons[i].color = i < currentCharges ? dashAvailableColor : dashUsedColor;
                }
            }
        }
    }
    
    /// <summary>
    /// Rebuilds dash icons when max charges changes
    /// </summary>
    void RebuildDashIcons(int count)
    {
        // Clear existing icons
        foreach (var icon in dashIcons)
        {
            if (icon != null) Destroy(icon.gameObject);
        }
        dashIcons.Clear();
        
        // Create new icons
        if (dashIconPrefab == null || dashIconContainer == null) return;
        
        for (int i = 0; i < count; i++)
        {
            GameObject iconObj = Instantiate(dashIconPrefab, dashIconContainer);
            Image iconImage = iconObj.GetComponent<Image>();
            if (iconImage != null)
            {
                dashIcons.Add(iconImage);
            }
        }
    }
    
    /// <summary>
    /// Updates bunny hop speed bonus display
    /// </summary>
    void UpdateSpeedBonusUI()
    {
        if (speedBonusSlider == null && speedBonusText == null) return;
        
        float bonus = playerController.GetBunnyHopBonus();
        
        if (speedBonusSlider != null)
        {
            targetSpeedBonus = bonus;
            speedBonusSlider.value = Mathf.Lerp(speedBonusSlider.value, targetSpeedBonus, Time.deltaTime * sliderLerpSpeed);
        }
        
        if (speedBonusText != null)
        {
            if (bonus > 0.01f)
            {
                speedBonusText.text = $"{1f + bonus:F1}x";
                speedBonusText.gameObject.SetActive(true);
            }
            else
            {
                speedBonusText.gameObject.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// Updates state name and color indicator
    /// </summary>
    void UpdateStateUI()
    {
        var stateData = playerController.GetCurrentStateData();
        if (stateData == null) return;
        
        if (stateNameText != null)
        {
            stateNameText.text = stateData.stateName;
        }
        
        if (stateColorIndicator != null)
        {
            stateColorIndicator.color = stateData.stateColor;
        }
    }
    
    /// <summary>
    /// Updates status indicator visibility
    /// </summary>
    void UpdateStatusIndicators()
    {
        if (wallSlideIndicator != null)
        {
            wallSlideIndicator.SetActive(playerController.IsWallSliding || playerController.IsWallClinging);
        }
        
        if (dashingIndicator != null)
        {
            dashingIndicator.SetActive(playerController.IsDashing);
        }
        
        if (landingLagIndicator != null)
        {
            landingLagIndicator.SetActive(playerController.IsInLandingLag);
        }
    }
    
    /// <summary>
    /// Force update all UI elements
    /// </summary>
    public void UpdateAllUI()
    {
        UpdateStaminaUI();
        UpdateDashUI();
        UpdateSpeedBonusUI();
        UpdateStateUI();
        UpdateStatusIndicators();
    }
    
    /// <summary>
    /// Flash the stamina bar (call when stamina depleted)
    /// </summary>
    public void FlashStamina()
    {
        if (staminaFillImage != null)
        {
            StartCoroutine(FlashCoroutine(staminaFillImage, Color.red, 0.2f));
        }
    }
    
    /// <summary>
    /// Flash the dash icons (call when trying to dash with no charges)
    /// </summary>
    public void FlashDash()
    {
        foreach (var icon in dashIcons)
        {
            if (icon != null)
            {
                StartCoroutine(FlashCoroutine(icon, Color.red, 0.2f));
            }
        }
    }
    
    System.Collections.IEnumerator FlashCoroutine(Image target, Color flashColor, float duration)
    {
        Color originalColor = target.color;
        target.color = flashColor;
        yield return new WaitForSeconds(duration);
        target.color = originalColor;
    }
}
```

---

## Detailed Canvas Setup

### Step 1: Create Canvas

1. **Right-click in Hierarchy** → UI → Canvas
2. Select the Canvas and configure:

| Component | Setting | Value |
|-----------|---------|-------|
| **Canvas** | Render Mode | Screen Space - Overlay |
| **Canvas Scaler** | UI Scale Mode | Scale With Screen Size |
| | Reference Resolution | 1920 x 1080 |
| | Screen Match Mode | Match Width Or Height |
| | Match | 0.5 |
| **Graphic Raycaster** | (keep defaults) | |

### Step 2: Create Stats Panel

1. **Right-click on Canvas** → Create Empty
2. Name it: `StatsPanel`
3. Configure RectTransform:
   - Anchor: Top-Left (Alt+Click preset)
   - Pivot: (0, 1)
   - Position: (20, -20)
   - Size: (300, 150)

---

## Stamina Bar Setup

### Create the Slider

1. **Right-click on StatsPanel** → UI → Slider
2. Name it: `StaminaSlider`
3. Configure RectTransform:
   - Anchor: Top-Left
   - Position: (10, -10)
   - Size: (250, 25)

### Configure Slider Component

| Setting | Value |
|---------|-------|
| Interactable | **OFF** (uncheck) |
| Transition | None |
| Direction | Left To Right |
| Min Value | 0 |
| Max Value | 1 |
| Value | 1 |

### Style the Slider

1. **Delete** the `Handle Slide Area` child (we don't need the handle)

2. Select `Background`:
   - Color: Dark gray `(30, 30, 30, 200)`
   
3. Select `Fill Area → Fill`:
   - Color: Green `(50, 200, 50, 255)`
   - This is the `staminaFillImage` reference

### Add Label

1. **Right-click on StaminaSlider** → UI → Text - TextMeshPro
2. Name it: `StaminaLabel`
3. Position: (-60, 0)
4. Size: (50, 25)
5. Text: `STA`
6. Font Size: 14
7. Alignment: Right, Middle

### Add Value Text

1. **Right-click on StaminaSlider** → UI → Text - TextMeshPro
2. Name it: `StaminaValue`
3. Anchor: Right
4. Position: (40, 0)
5. Size: (60, 25)
6. Text: `100/100`
7. Font Size: 12
8. Alignment: Left, Middle
9. This is the `staminaText` reference

### Final Stamina Hierarchy

```
StaminaSlider
├── Background
├── Fill Area
│   └── Fill (← staminaFillImage)
├── StaminaLabel ("STA")
└── StaminaValue (← staminaText)
```

---

## Dash Charges Setup

### Option A: Counter Text (Simple)

1. **Right-click on StatsPanel** → UI → Text - TextMeshPro
2. Name it: `DashCounter`
3. Position: (10, -45)
4. Size: (100, 30)
5. Text: `DASH: 1/1`
6. Font Size: 16
7. This is the `dashCounterText` reference

### Option B: Icon Display (Visual)

#### Create Container

1. **Right-click on StatsPanel** → Create Empty
2. Name it: `DashIconContainer`
3. Add **Horizontal Layout Group** component:
   - Spacing: 10
   - Child Alignment: Middle Left
   - Child Force Expand: OFF (both)
   - Control Child Size: OFF (both)
4. Position: (10, -45)
5. Size: (200, 40)
6. This is the `dashIconContainer` reference

#### Create Icon Prefab

1. **Right-click on DashIconContainer** → UI → Image
2. Name it: `DashIcon`
3. Size: (30, 30)
4. Source Image: Circle sprite (or your custom dash icon)
5. Color: White

6. **Create Prefab**:
   - Drag `DashIcon` to Project window (Assets folder)
   - Delete the instance from DashIconContainer
   - This prefab is the `dashIconPrefab` reference

---

## Advanced: Icon-Based Dash Display

For a more polished look with custom icons:

### Create Custom Dash Icon Prefab

```
DashIcon (Prefab)
├── Image (main icon)
│   └── Source Image: Your dash sprite
│   └── Color: White (will be tinted by script)
└── Glow (optional child Image)
    └── Source Image: Soft circle
    └── Color: Cyan with low alpha
```

### Animator for Icon (Optional)

1. Select DashIcon prefab
2. Add **Animator** component
3. Create Animation Controller with states:
   - `Available` - Normal scale
   - `Used` - Slightly smaller, grayed out
   - `Refilling` - Pulsing animation

---

## Advanced: Animated UI Effects

### Stamina Drain Animation

Add this to PlayerStatsUI for a "shake" effect when stamina is low:

```csharp
[Header("=== ADVANCED EFFECTS ===")]
public bool shakeWhenDepleted = true;
public float shakeIntensity = 5f;
public float shakeDuration = 0.3f;

private bool isShaking = false;

public void TriggerStaminaShake()
{
    if (!isShaking && shakeWhenDepleted && staminaRect != null)
    {
        StartCoroutine(ShakeCoroutine());
    }
}

System.Collections.IEnumerator ShakeCoroutine()
{
    isShaking = true;
    Vector3 originalPos = staminaRect.anchoredPosition;
    float elapsed = 0f;
    
    while (elapsed < shakeDuration)
    {
        float x = Random.Range(-1f, 1f) * shakeIntensity;
        float y = Random.Range(-1f, 1f) * shakeIntensity;
        staminaRect.anchoredPosition = originalPos + new Vector3(x, y, 0);
        elapsed += Time.deltaTime;
        yield return null;
    }
    
    staminaRect.anchoredPosition = originalPos;
    isShaking = false;
}
```

### Dash Refill Animation

```csharp
public void PlayDashRefillEffect(int chargeIndex)
{
    if (chargeIndex < dashIcons.Count && dashIcons[chargeIndex] != null)
    {
        StartCoroutine(RefillPopCoroutine(dashIcons[chargeIndex].transform));
    }
}

System.Collections.IEnumerator RefillPopCoroutine(Transform target)
{
    Vector3 originalScale = target.localScale;
    target.localScale = originalScale * 1.5f;
    
    float elapsed = 0f;
    float duration = 0.2f;
    
    while (elapsed < duration)
    {
        target.localScale = Vector3.Lerp(originalScale * 1.5f, originalScale, elapsed / duration);
        elapsed += Time.deltaTime;
        yield return null;
    }
    
    target.localScale = originalScale;
}
```

### Connect to Player Events

In `Start()`, subscribe to player events:

```csharp
void Start()
{
    // ... existing code ...
    
    // Subscribe to events
    if (playerController != null)
    {
        playerController.OnStaminaDepleted.AddListener(OnStaminaDepleted);
        playerController.OnDashStarted.AddListener(OnDashUsed);
    }
}

void OnDestroy()
{
    // Unsubscribe
    if (playerController != null)
    {
        playerController.OnStaminaDepleted.RemoveListener(OnStaminaDepleted);
        playerController.OnDashStarted.RemoveListener(OnDashUsed);
    }
}

void OnStaminaDepleted()
{
    TriggerStaminaShake();
    FlashStamina();
}

void OnDashUsed(Vector3 direction)
{
    // Optional: play dash used effect
}
```

---

## Complete Prefab Hierarchy

Here's the full recommended UI structure:

```
Canvas
├── Canvas Scaler (Scale With Screen Size, 1920x1080)
│
└── StatsPanel (Anchor: Top-Left)
    │
    ├── StaminaContainer
    │   ├── StaminaIcon (Image, optional)
    │   ├── StaminaSlider
    │   │   ├── Background
    │   │   └── Fill Area
    │   │       └── Fill (← staminaFillImage)
    │   └── StaminaText (← staminaText)
    │
    ├── DashContainer
    │   ├── DashIcon (Image, optional)
    │   ├── DashIconContainer (← dashIconContainer)
    │   │   └── (Icons spawned here from prefab)
    │   └── DashText (← dashCounterText, optional)
    │
    ├── SpeedContainer (optional, for bunny hop)
    │   ├── SpeedIcon
    │   ├── SpeedSlider (← speedBonusSlider)
    │   └── SpeedText (← speedBonusText)
    │
    └── StateIndicator (optional)
        ├── StateColorBox (← stateColorIndicator)
        └── StateNameText (← stateNameText)
```

---

## Quick Reference: Inspector Setup

### On Canvas (PlayerStatsUI component):

| Field | Drag This |
|-------|-----------|
| **Player Controller** | Player GameObject |
| **Stamina Slider** | StaminaSlider |
| **Stamina Fill Image** | Fill (child of StaminaSlider) |
| **Stamina Text** | StaminaText (TMP) |
| **Dash Counter Text** | DashText (TMP) |
| **Dash Icon Container** | DashIconContainer |
| **Dash Icon Prefab** | DashIcon prefab from Project |
| **Speed Bonus Slider** | SpeedSlider (optional) |
| **Speed Bonus Text** | SpeedText (optional) |
| **State Name Text** | StateNameText (optional) |
| **State Color Indicator** | StateColorBox (optional) |

---

## Testing Checklist

Press Play and verify:

- [ ] Stamina bar decreases when wall climbing/sliding
- [ ] Stamina bar color shifts from green to red when low
- [ ] Stamina bar pulses when below threshold
- [ ] Dash icons/counter updates when dashing
- [ ] Dash icons gray out when used
- [ ] Dash icons light up when refilled
- [ ] Speed bonus appears during bunny hop
- [ ] State name/color updates when switching states

---

## Tips

1. **Performance**: The script uses `Update()` which is fine for UI. For many UI elements, consider using events instead.

2. **Scaling**: Use `Canvas Scaler` with "Scale With Screen Size" for responsive UI across resolutions.

3. **Visibility**: Add a `CanvasGroup` to StatsPanel to fade the whole UI in/out.

4. **Polish**: Add slight delays before updating (0.1s) to prevent jittery numbers.

5. **Mobile**: Increase touch targets if needed, and consider repositioning for thumbs.
