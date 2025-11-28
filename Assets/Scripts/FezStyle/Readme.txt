# Fez-Style Rotation System for Fourneige

A fully modular world rotation system integrated into your existing `_PlayerController`.
All features are toggle-based for maximum flexibility.

---

## Components Overview

| Script | Purpose |
|--------|---------|
| `_WorldRotationController` | Core rotation system with Input Mode and Trigger Mode |
| `_RotationTriggerZone` | Trigger collider that causes rotation on enter |
| `_PlayerController` | **UPDATED** - Your existing controller with Fez rotation integrated |
| `_FezDepthSnapper` | Snaps player to valid platforms after rotation |
| `_FezCameraSetup` | Configures orthographic camera for Fez-style visuals |

---

## Setup Instructions

### 1. Import Scripts

- **Replace** your existing `_PlayerController.cs` with the new version
- Copy other `_*.cs` files to your `Assets/Scripts/` folder
- Your `_PlayerStateData.cs` remains unchanged

### 2. Scene Setup

#### World Rotation Controller

1. Create an empty GameObject named `_WorldRotationController`
2. Add the `_WorldRotationController` component
3. Configure references:
   - **Camera Transform**: Assign your Main Camera
   - **Pivot Point**: Create an empty child or assign player
   - **Follow Target**: Assign player (optional)

#### Camera Setup

1. Select your Main Camera
2. Add `_FezCameraSetup` component
3. Enable **Use Orthographic** for true Fez style
4. Adjust **Orthographic Size** (8-10 recommended)

#### Player Setup (Your Existing Player)

1. Select your player with `_PlayerController`
2. In the **World Rotation Settings** section:
   - Enable **Use World Rotation**
   - Assign **World Rotation Controller** (or let it auto-find)
   - Configure freeze/snap options as desired
3. All your existing Fire/Ice states continue to work!

#### Depth Snapper (Optional but Recommended)

1. Create an empty GameObject named `_FezDepthSnapper`
2. Add the `_FezDepthSnapper` component
3. Configure **Platform Layer** to match your ground layers

---

## Mode Toggles

### Master Controls (on `_WorldRotationController`)

```
canRotate           - Master toggle for entire system
inputModeEnabled    - Enable Q/E keyboard rotation
triggerModeEnabled  - Enable trigger zone rotation
```

### Runtime Toggle Methods

```csharp
// Get the singleton instance
var rotation = _WorldRotationController.Instance;

// Toggle entire system
rotation.SetSystemEnabled(false);  // Disable all rotation
rotation.SetSystemEnabled(true);   // Enable all rotation

// Toggle input mode only
rotation.SetInputModeEnabled(false);  // Disable Q/E controls
rotation.SetInputModeEnabled(true);   // Enable Q/E controls

// Toggle trigger mode only
rotation.SetTriggerModeEnabled(false);  // Disable trigger zones
rotation.SetTriggerModeEnabled(true);   // Enable trigger zones
```

---

## Input Setup (Invoke Unity Events)

The rotation system uses Unity's New Input System with **"Invoke Unity Events"** behavior, matching your existing `_PlayerController` setup.

### Adding Rotation Actions to Your Input Actions Asset

1. Open your **InputSystem_Actions** asset
2. In the **Player** action map, add two new actions:
   - **RotateLeft** (Button type)
   - **RotateRight** (Button type)
3. Add bindings:
   - RotateLeft: `<Keyboard>/q`, `<Gamepad>/leftShoulder`
   - RotateRight: `<Keyboard>/e`, `<Gamepad>/rightShoulder`

### Connecting to WorldRotationController

**Option A: Same PlayerInput as Player (Recommended)**

1. Your player already has a **PlayerInput** component
2. Expand **Events > Player** in the PlayerInput inspector
3. Find **RotateLeft** and **RotateRight** events
4. Drag the `_WorldRotationController` GameObject and select:
   - `_WorldRotationController.OnRotateLeft`
   - `_WorldRotationController.OnRotateRight`

**Option B: Separate PlayerInput Component**

1. Add **PlayerInput** component to the `_WorldRotationController` GameObject
2. Assign your Input Actions asset
3. Set **Behavior** to "Invoke Unity Events"
4. Expand **Events > Player**
5. Connect RotateLeft → `OnRotateLeft`
6. Connect RotateRight → `OnRotateRight`

### Input Method Signatures

```csharp
// These match your existing input pattern (OnMove, OnJump, etc.)
public void OnRotateLeft(InputAction.CallbackContext ctx)
public void OnRotateRight(InputAction.CallbackContext ctx)
```

---

## Trigger Mode (Trigger Zones)

### Creating a Trigger Zone

1. Create a GameObject with a Collider (Box, Sphere, etc.)
2. Set **Is Trigger** = true on the Collider
3. Add `_RotationTriggerZone` component
4. Configure:
   - **Player Tag**: Tag that triggers rotation (default: "Player")
   - **Rotation Mode**: Directional or Target Face
   - **Direction/Target Face**: Which way to rotate

### Rotation Modes

**Directional Mode:**
- Rotates 90° left or right
- Good for continuous areas

**Target Face Mode:**
- Rotates to a specific face (0-3)
- Good for specific viewing angles

### Trigger Settings

```
oneShot         - Trigger only works once
triggerCooldown - Delay before trigger can activate again
```

---

## Events

### Subscribe to Rotation Events

```csharp
private void OnEnable()
{
    var rotation = _WorldRotationController.Instance;
    rotation.OnRotationStarted += HandleRotationStarted;
    rotation.OnRotationCompleted += HandleRotationCompleted;
    rotation.OnRotationProgress += HandleRotationProgress;
    rotation.OnModeToggled += HandleModeToggled;
}

private void HandleRotationStarted(int newFaceIndex)
{
    // Freeze enemies, pause physics, play sound, etc.
    Debug.Log($"Rotating to face {newFaceIndex}");
}

private void HandleRotationCompleted(int faceIndex)
{
    // Unfreeze, check new paths, etc.
    Debug.Log($"Now facing {faceIndex}");
}

private void HandleRotationProgress(float progress)
{
    // Update UI, visual effects (0 to 1)
}

private void HandleModeToggled(string modeName, bool enabled)
{
    // React to mode changes
    Debug.Log($"{modeName} is now {(enabled ? "ON" : "OFF")}");
}
```

---

## Public Getters

```csharp
var rotation = _WorldRotationController.Instance;

// Current state
int face = rotation.GetCurrentFaceIndex();    // 0-3
float angle = rotation.GetCurrentAngle();     // 0-360
bool rotating = rotation.IsRotating();        // true during animation

// Mode states
bool inputActive = rotation.IsInputModeActive();
bool triggerActive = rotation.IsTriggerModeActive();

// Direction vectors (for movement)
Vector3 forward = rotation.GetCurrentForward();
Vector3 right = rotation.GetCurrentRight();
```

---

## Integration with Fourneige

### State System Compatibility

The rotation system works independently of your Fire/Ice states:

```csharp
// In your _PlayerController or state switching logic
public void OnSwitchState(InputAction.CallbackContext ctx)
{
    // Check if world is rotating before allowing state switch
    if (_WorldRotationController.Instance.IsRotating())
    {
        Debug.Log("Cannot switch states during rotation!");
        return;
    }
    
    // ... rest of state switching logic
}
```

### Disabling Rotation During Specific States

```csharp
// Example: Disable rotation during dash
private void StartDash()
{
    _WorldRotationController.Instance.SetSystemEnabled(false);
    // ... dash logic
}

private void EndDash()
{
    _WorldRotationController.Instance.SetSystemEnabled(true);
}
```

---

## Customization

### Rotation Speed

```csharp
// In Inspector or code
rotation.rotationDuration = 0.3f;  // Faster
rotation.rotationDuration = 0.8f;  // Slower (more cinematic)
```

### Rotation Easing

Modify the **Rotation Curve** in the Inspector for different feels:
- Linear: Constant speed
- EaseInOut: Smooth start and stop (default)
- Custom: Create your own curve

### Camera Distance

```csharp
rotation.cameraDistance = 20f;      // Further back
rotation.cameraHeightOffset = 5f;   // Higher angle
```

---

## Troubleshooting

### Rotation Not Working

1. Check `canRotate` is TRUE
2. Check `inputModeEnabled` or `triggerModeEnabled`
3. Verify camera reference is assigned
4. Check console for error messages

### Player Falls After Rotation

1. Add `_FezDepthSnapper` component
2. Configure **Platform Layer** correctly
3. Increase **Max Snap Distance** if needed

### Triggers Not Working

1. Verify collider has **Is Trigger** = true
2. Check **Player Tag** matches your player's tag
3. Ensure `triggerModeEnabled` is TRUE
4. Check trigger cooldown hasn't blocked it

### Movement Direction Wrong After Rotation

1. Ensure `_FezPlayerController` has rotation reference
2. Movement uses `GetCurrentRight()` for direction
3. Check player is using the Fez controller, not standard one

---

## File Structure

```
Assets/
└── Scripts/
    ├── _PlayerController.cs          // UPDATED with Fez rotation integration
    ├── _PlayerStateData.cs           // Your existing state data (unchanged)
    ├── _WorldRotationController.cs   // Core rotation
    ├── _RotationTriggerZone.cs       // Trigger zones
    ├── _FezDepthSnapper.cs           // Platform snapping
    └── _FezCameraSetup.cs            // Camera config
```

---

## New Player Controller Variables

The following variables were added to `_PlayerController` under **World Rotation Settings**:

```csharp
[Header("World Rotation Settings")]
public bool useWorldRotation = false;              // Master toggle
public _WorldRotationController worldRotationController;  // Reference
public bool freezeDuringRotation = true;           // Freeze during rotation
public bool useRotationRelativeMovement = true;    // Adapt movement to camera
public bool snapAfterRotation = true;              // Snap to 2D plane after
```

### New Public Methods

```csharp
// Check if world is currently rotating
bool rotating = player.IsWorldRotating();

// Check if player is frozen for rotation
bool frozen = player.IsFrozenForRotation();
```

---

## Integration Notes

### Movement Adaptation

When `useRotationRelativeMovement` is TRUE:
- Movement uses `GetMovementRight()` which returns camera-relative direction
- Dash direction adapts to current camera view
- Player velocity is converted to new direction after rotation

### State System Compatibility

- State switching is **blocked** during world rotation
- All existing Fire/Ice toggles continue to work
- Rotation settings are separate from state data (they apply globally)

### Blocking Actions During Rotation

The following are automatically blocked when frozen for rotation:
- Movement input
- Jump input  
- Dash input
- State switching

---

## Quick Reference

### Enable Fez Rotation in Player

```csharp
// In Inspector, set:
useWorldRotation = true;
freezeDuringRotation = true;
useRotationRelativeMovement = true;
```

### Disable Rotation Temporarily

```csharp
// Disable all rotation
_WorldRotationController.Instance.SetSystemEnabled(false);

// Disable just input mode (keep triggers)
_WorldRotationController.Instance.SetInputModeEnabled(false);

// Disable just trigger mode (keep input)
_WorldRotationController.Instance.SetTriggerModeEnabled(false);
```

### Check Rotation State in Other Scripts

```csharp
// Check if currently rotating
if (_WorldRotationController.Instance.IsRotating())
{
    // Don't do something during rotation
}

// Get current camera direction for movement
Vector3 right = _WorldRotationController.Instance.GetCurrentRight();
Vector3 forward = _WorldRotationController.Instance.GetCurrentForward();
```