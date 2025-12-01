# Fez-Style World Rotation System for Unity (Cinemachine Edition)

## Overview

This system implements Fez-style 90-degree world rotation fully integrated with **Cinemachine**. When the player triggers a rotation, the camera smoothly orbits around the scene, revealing new perspectives and creating puzzle opportunities through depth alignment.

---

## Files Included

| File | Description | Size |
|------|-------------|------|
| `FezWorldRotation.cs` | Main rotation controller (Cinemachine pivot rig) | ~20KB |
| `FezRotationTrigger.cs` | Trigger zones that cause rotation | ~7KB |
| `FezDepthSnapper.cs` | Snaps player to valid positions after rotation | ~12KB |
| `_FezPlayerController.cs` | Full player controller with rotation integration | ~88KB |

---

## Quick Setup (5 Minutes)

### Step 1: Create the Camera Rig

```
Scene Hierarchy:
├── Main Camera
│   └── [Add: CinemachineBrain component]
│
├── FezCameraRig (Empty GameObject at 0,0,0)
│   └── [Add: FezWorldRotation component]
│   │
│   └── CinemachineCamera (Child of rig)
│       ├── Position: (0, 2, 15)  [or your preferred offset]
│       ├── Follow: Player
│       ├── LookAt: Player
│       └── Lens > Orthographic Size: 8
│
├── Player
│   └── [Add: _FezPlayerController component]
│
└── FezDepthSnapper (Empty GameObject)
    └── [Add: FezDepthSnapper component]
```

### Step 2: Configure FezWorldRotation

On the `FezCameraRig` object:
1. **Pivot Rig**: Assign self (FezCameraRig)
2. **Follow Target**: Assign your Player
3. **Rotation Duration**: 0.5 (adjust for feel)
4. **Starting Face Index**: 0 (North)

### Step 3: Configure Player

On your Player object:
1. **Use World Rotation**: ✓ (checked)
2. **World Rotation Controller**: Leave empty (auto-finds)
3. **Freeze During Rotation**: ✓ (recommended)
4. **Use Rotation Relative Movement**: ✓ (recommended)

### Step 4: Set Up Input

In your Input Actions asset, create:
- **RotateLeft** action (Q key or Left Shoulder)
- **RotateRight** action (E key or Right Shoulder)

Connect to `FezWorldRotation`:
- `OnRotateLeft(InputAction.CallbackContext)` 
- `OnRotateRight(InputAction.CallbackContext)`

---

## Component Details

### FezWorldRotation.cs

The core rotation controller. Uses a **pivot rig** that contains the Cinemachine camera as a child.

#### How It Works
```
[FezCameraRig] ← This rotates 90° at a time
    └── [CinemachineCamera] ← Child orbits with parent
            └── Follow → Player
```

When `RotateWorld()` is called:
1. Fires `OnRotationStarted` event (player freezes)
2. Animates rig rotation over `rotationDuration` seconds
3. Uses `rotationCurve` for smooth easing
4. Fires `OnRotationCompleted` event (player unfreezes)

#### Key Properties

| Property | Type | Description |
|----------|------|-------------|
| `canRotate` | bool | Master toggle for all rotation |
| `inputModeEnabled` | bool | Allow rotation via Q/E keys |
| `triggerModeEnabled` | bool | Allow rotation via trigger zones |
| `rotationDuration` | float | Animation duration in seconds |
| `startingFaceIndex` | int | Which face to start at (0-3) |

#### Public Methods

```csharp
// Rotate 90° left or right
FezWorldRotation.Instance.RotateWorld(-1);  // Left (counter-clockwise)
FezWorldRotation.Instance.RotateWorld(1);   // Right (clockwise)

// Rotate to specific face (takes shortest path)
FezWorldRotation.Instance.RotateToFace(2);  // Rotate to South

// Query state
int face = FezWorldRotation.Instance.GetCurrentFaceIndex();  // 0-3
bool rotating = FezWorldRotation.Instance.IsRotating();
Vector3 right = FezWorldRotation.Instance.GetCurrentRight(); // Movement direction
Vector3 forward = FezWorldRotation.Instance.GetCurrentForward(); // Depth direction

// Instant teleport (no animation)
FezWorldRotation.Instance.SetFaceImmediate(0);

// Enable/disable at runtime
FezWorldRotation.Instance.SetSystemEnabled(false);  // Disable rotation
FezWorldRotation.Instance.SetInputModeEnabled(true);  // Enable Q/E input
```

#### Events

```csharp
// Subscribe to events
FezWorldRotation.Instance.OnRotationStarted += OnRotStart;
FezWorldRotation.Instance.OnRotationCompleted += OnRotEnd;
FezWorldRotation.Instance.OnRotationProgress += OnRotProgress;

// Event signatures
void OnRotStart(int newFaceIndex) { }      // Rotation begins
void OnRotEnd(int faceIndex) { }            // Rotation ends
void OnRotProgress(float progress) { }      // 0.0 to 1.0 during rotation
```

---

### FezRotationTrigger.cs

Creates trigger volumes that rotate the world when the player enters.

#### Setup
1. Create a GameObject with a Collider
2. Enable "Is Trigger" on the Collider
3. Add `FezRotationTrigger` component
4. Set `playerTag` to match your player's tag

#### Modes

| Mode | Description |
|------|-------------|
| **Directional** | Rotates left or right by 90° |
| **TargetFace** | Rotates to a specific face (0-3) |

---

### FezDepthSnapper.cs

After rotation, platforms that were at different depths may now align. This component finds the nearest valid ground position and snaps the player there.

#### How It Works
1. Listens for `OnRotationCompleted` event
2. Waits for `snapDelay` seconds (rotation settle time)
3. Casts rays in the new depth direction
4. Finds nearest platform within `maxSnapDistance`
5. Snaps player to valid ground position

#### Key Settings

| Setting | Default | Description |
|---------|---------|-------------|
| `maxSnapDistance` | 10 | How far to search for platforms |
| `snapDelay` | 0.1 | Wait time after rotation |
| `smoothSnap` | false | Interpolate vs instant snap |
| `preferGroundSnap` | true | Prioritize grounded positions |

---

### _FezPlayerController.cs

Full-featured platformer controller with Fez rotation integration.

#### Rotation-Related Settings

```csharp
[Header("World Rotation Settings")]
public bool useWorldRotation = true;           // Enable rotation integration
public FezWorldRotation worldRotationController;  // Auto-finds if null
public bool freezeDuringRotation = true;       // Stop player during rotation
public bool useRotationRelativeMovement = true; // Camera-relative controls
public bool snapAfterRotation = true;          // Trigger depth snap

[Header("Rotation Physics")]
public bool clearDepthVelocityOnRotation = true;   // Stop depth drift
public bool preserveHorizontalMomentum = true;     // Keep speed through rotation
public float wallCheckDelayAfterRotation = 0.1f;   // Wall check cooldown
```

#### How Movement Works After Rotation

When `useRotationRelativeMovement` is TRUE:
- "Right" input always moves player right **on screen**
- After rotating, the world direction changes, but controls stay intuitive
- Internally calls `worldRotationController.GetCurrentRight()` for movement axis

---

## Face Index Reference

```
Face 0 = North (+Z) → Camera looks at -Z
Face 1 = East (+X)  → Camera looks at -X  
Face 2 = South (-Z) → Camera looks at +Z
Face 3 = West (-X)  → Camera looks at +X

Rotation wraps: 3 → 0 → 1 → 2 → 3 → 0 ...
```

---

## Common Issues & Solutions

### Camera Doesn't Rotate
- ✓ Check `canRotate` is TRUE
- ✓ Check `inputModeEnabled` is TRUE (for Q/E input)
- ✓ Verify Input Actions are connected to OnRotateLeft/OnRotateRight

### Player Doesn't Freeze During Rotation
- ✓ Check `useWorldRotation` is TRUE on player
- ✓ Check `freezeDuringRotation` is TRUE on player
- ✓ Verify controller reference (auto-finds via singleton)

### Player Falls Through World After Rotation
- ✓ Add `FezDepthSnapper` component to scene
- ✓ Check `platformLayer` mask includes your ground
- ✓ Increase `maxSnapDistance` if platforms are far apart

### Movement Is Wrong After Rotation
- ✓ Enable `useRotationRelativeMovement` on player
- ✓ Check `preserveHorizontalMomentum` setting
- ✓ Enable `clearDepthVelocityOnRotation` to prevent drift

### Triggers Don't Work
- ✓ Check `triggerModeEnabled` is TRUE on FezWorldRotation
- ✓ Check player has correct tag (default: "Player")
- ✓ Check collider has "Is Trigger" enabled

---

## Advanced: Creating Custom Rotation Responses

```csharp
public class MyCustomBehavior : MonoBehaviour
{
    void Start()
    {
        // Subscribe to rotation events
        if (FezWorldRotation.Instance != null)
        {
            FezWorldRotation.Instance.OnRotationStarted += OnRotStart;
            FezWorldRotation.Instance.OnRotationCompleted += OnRotEnd;
        }
    }
    
    void OnDestroy()
    {
        // Always unsubscribe to prevent memory leaks!
        if (FezWorldRotation.Instance != null)
        {
            FezWorldRotation.Instance.OnRotationStarted -= OnRotStart;
            FezWorldRotation.Instance.OnRotationCompleted -= OnRotEnd;
        }
    }
    
    void OnRotStart(int newFaceIndex)
    {
        // Called when rotation begins
        Debug.Log($"Rotating to face {newFaceIndex}");
    }
    
    void OnRotEnd(int faceIndex)
    {
        // Called when rotation completes
        // Good place to:
        // - Play sound effects
        // - Update UI
        // - Check puzzle conditions
        // - Enable/disable objects
    }
}
```

---

## Performance Notes

- Rotation uses coroutines (no allocations during animation)
- Physics constraints update only on rotation complete
- 4-direction wall checks are optional and throttled
- All components use singleton pattern for efficient access

---

## Version Compatibility

| Unity Version | Cinemachine | Status |
|--------------|-------------|--------|
| Unity 6+ | Cinemachine 3.x | ✓ Tested |
| Unity 2022+ | Cinemachine 2.x | Should work |
| Unity 2021 | Cinemachine 2.x | Should work |

The code uses preprocessor directives to handle Cinemachine 3.x API changes:
```csharp
#if UNITY_6000_0_OR_NEWER
using Unity.Cinemachine;        // CM 3.x
#else
using Cinemachine;              // CM 2.x
#endif
```

---

## Credits

Inspired by Polytron's **FEZ** (2012) - one of the most innovative puzzle platformers ever made.

---

*Happy rotating! 🎮*
