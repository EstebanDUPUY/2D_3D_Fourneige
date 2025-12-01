# Fez-Style Rotation System - Unified Edition

A fully modular world rotation system that works with **both** standard cameras and **Cinemachine**.
All components work together seamlessly like LEGO bricks.

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                      FezRotationBridge                          │
│              (Unified Interface - Auto-detects controller)      │
└─────────────────────┬───────────────────────────────────────────┘
                      │
          ┌───────────┴───────────┐
          ▼                       ▼
┌─────────────────────┐  ┌─────────────────────────┐
│ _WorldRotation      │  │ FezCinemachineRotation  │
│ Controller          │  │ (Cinemachine support)   │
│ (Original/Direct)   │  │                         │
└─────────────────────┘  └─────────────────────────┘
          │                       │
          └───────────┬───────────┘
                      ▼
        ┌─────────────────────────┐
        │  All Other Components   │
        │  • _FezPlayerController │
        │  • _FezDepthSnapper     │
        │  • FezRotationTrigger   │
        └─────────────────────────┘
```

---

## Quick Start

### Option A: Using Cinemachine (Recommended)

1. **Add FezCinemachineRotation** to an empty GameObject (e.g., "FezCameraRig")
2. **Create Virtual Camera** as child:
   - Position: `(0, 2, 15)`
   - Follow: Player
   - LookAt: Player
   - Lens: Orthographic, Size 8
3. **Configure FezCinemachineRotation**:
   - Rotation Method: `RotatePivotRig`
   - Pivot Rig: The parent GameObject
   - Follow Target: Player
4. **On Player**: Set `useRotationBridge = true` (default)
5. **On DepthSnapper**: Set `useRotationBridge = true` (default)
6. **Done!** The bridge auto-detects everything.

### Option B: Using Original Controller

1. **Add _WorldRotationController** to empty GameObject
2. **Assign camera** and pivot references
3. **On Player**: Set `useRotationBridge = true` OR assign controller directly
4. **Done!** Works exactly as before.

---

## Component Reference

| Script | Purpose |
|--------|---------|
| `FezRotationBridge` | **Unified interface** - auto-detects and routes to active controller |
| `FezCinemachineRotation` | Cinemachine-compatible rotation controller |
| `FezCinemachineRigSetup` | Helper to auto-create Cinemachine rig |
| `_WorldRotationController` | Original direct camera rotation |
| `_FezPlayerController` | Player controller (works with either system) |
| `_FezDepthSnapper` | Depth snapping (works with either system) |
| `FezRotationTriggerZone` | Trigger zones (works with either system) |

---

## How the Bridge Works

The `FezRotationBridge` automatically:

1. **Detects** which rotation controller exists in your scene
2. **Prefers Cinemachine** if both controllers exist
3. **Forwards** all calls to the active controller
4. **Relays events** from the active controller

### Using the Bridge

```csharp
// Static helpers (most common)
bool rotating = FezRotationBridge.IsWorldRotating();
Vector3 forward = FezRotationBridge.GetWorldForward();
Vector3 right = FezRotationBridge.GetWorldRight();
int face = FezRotationBridge.GetFaceIndex();

// Instance methods
FezRotationBridge.Instance.RotateWorld(1);      // Rotate right
FezRotationBridge.Instance.RotateToFace(2);     // Go to face 2
FezRotationBridge.Instance.SetSystemEnabled(false);

// Events
FezRotationBridge.OnRotationStarted += (int face) => { };
FezRotationBridge.OnRotationCompleted += (int face) => { };
FezRotationBridge.OnRotationProgress += (float progress) => { };
```

---

## Player Controller Settings

The `_FezPlayerController` has these rotation settings:

```
World Rotation Settings
├── Use World Rotation: ✓         (Enable rotation integration)
├── Use Rotation Bridge: ✓        (Use unified bridge - RECOMMENDED)
├── World Rotation Controller:    (Only if bridge disabled)
├── Freeze During Rotation: ✓     
├── Use Rotation Relative Movement: ✓
└── Snap After Rotation: ✓

Rotation Physics
├── Clear Depth Velocity On Rotation: ✓
├── Preserve Horizontal Momentum: ✓
└── Wall Check Delay After Rotation: 0.1
```

### Bridge vs Direct Reference

| Setting | Behavior |
|---------|----------|
| `useRotationBridge = true` | Uses FezRotationBridge (works with ANY controller) |
| `useRotationBridge = false` | Uses `worldRotationController` reference directly |

**Recommendation**: Always use the bridge unless you have a specific reason not to.

---

## Depth Snapper Settings

```
References
├── Use Rotation Bridge: ✓        (Use unified bridge - RECOMMENDED)
└── World Rotation:               (Only if bridge disabled)
```

---

## Scene Hierarchy Examples

### Cinemachine Setup
```
Scene
├── Main Camera                   [CinemachineBrain]
├── FezCameraRig                  [FezCinemachineRotation]
│   └── CinemachineCamera         [CinemachineCamera] Follow/LookAt: Player
├── Player                        [_FezPlayerController]
├── FezDepthSnapper              [_FezDepthSnapper]
└── Level
    └── RotateTrigger            [FezRotationTriggerZone]
```

### Original Controller Setup
```
Scene
├── Main Camera                   (Controlled by _WorldRotationController)
├── _WorldRotationController      [_WorldRotationController]
├── Player                        [_FezPlayerController]
├── FezDepthSnapper              [_FezDepthSnapper]
└── Level
    └── RotateTrigger            [FezRotationTriggerZone]
```

### Mixed/Migration Setup
```
Scene
├── Main Camera                   [CinemachineBrain]
├── FezCameraRig                  [FezCinemachineRotation] ← Active
│   └── CinemachineCamera         
├── _WorldRotationController      [_WorldRotationController] ← Ignored (Cinemachine takes priority)
├── Player                        [_FezPlayerController, useRotationBridge=true]
└── ...
```

---

## Migration Guide

### From Original to Cinemachine

1. Keep your existing `_WorldRotationController` (as backup)
2. Add `FezCinemachineRotation` and set up Cinemachine rig
3. Ensure `useRotationBridge = true` on Player and DepthSnapper
4. The bridge auto-switches to Cinemachine
5. Test everything works
6. Remove old `_WorldRotationController` when ready

### From Cinemachine to Original

1. Disable or remove `FezCinemachineRotation`
2. Add/enable `_WorldRotationController`
3. Bridge auto-falls-back to original controller
4. No other changes needed!

---

## API Reference

### FezRotationBridge

```csharp
// Singleton
FezRotationBridge.Instance

// Static Helpers
static bool IsWorldRotating()
static Vector3 GetWorldForward()
static Vector3 GetWorldRight()
static int GetFaceIndex()

// Instance Methods
void RotateWorld(int direction)           // -1 left, 1 right
void RotateToFace(int faceIndex)          // 0-3
void RotateFromTrigger(int direction)
void RotateToFaceFromTrigger(int faceIndex)
void SetSystemEnabled(bool enabled)
void SetInputModeEnabled(bool enabled)
void SetTriggerModeEnabled(bool enabled)
void DetectControllers()                  // Re-scan for controllers
void ForceControllerType(ActiveControllerType type)

// Queries
bool IsRotating()
bool IsAvailable                          // Any controller exists?
int GetCurrentFaceIndex()
float GetCurrentAngle()
Vector3 GetCurrentForward()
Vector3 GetCurrentRight()
ActiveControllerType GetActiveControllerType()

// Direct Access (if needed)
_WorldRotationController GetOriginalController()
FezCinemachineRotation GetCinemachineController()

// Static Events
static event Action<int> OnRotationStarted
static event Action<int> OnRotationCompleted
static event Action<float> OnRotationProgress
```

---

## Troubleshooting

### Bridge Not Finding Controller
```
[FezRotationBridge] No rotation controller found in scene!
```
**Solution**: Add either `_WorldRotationController` or `FezCinemachineRotation` to your scene.

### Player Not Responding to Rotation
1. Check `useWorldRotation = true` on player
2. Check `useRotationBridge = true` (or assign controller directly)
3. Verify a rotation controller exists in scene

### Cinemachine Controller Not Being Used
The bridge prefers Cinemachine. If original is being used instead:
1. Check `FezCinemachineRotation` component is enabled
2. Call `FezRotationBridge.Instance.DetectControllers()` to re-scan

### Want to Force Original Controller
```csharp
FezRotationBridge.Instance.ForceControllerType(
    FezRotationBridge.ActiveControllerType.Original
);
```

---

## Performance Notes

- Bridge adds negligible overhead (single null check + delegation)
- All components cache their controller reference after first access
- Events are relayed synchronously (no allocation)

---

## Version History

### v3.0 (Unified Edition)
- Added FezRotationBridge for unified controller access
- Added FezCinemachineRotation for Cinemachine support
- Updated _FezPlayerController to use bridge
- Updated _FezDepthSnapper to use bridge
- Updated FezRotationTriggerZone to use bridge
- Full backward compatibility with original system
- All components work as "LEGO bricks" - mix and match freely
