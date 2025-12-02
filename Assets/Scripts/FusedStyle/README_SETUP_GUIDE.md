# Fused Player Controller - Complete Setup Guide

## Table of Contents
1. [Overview](#overview)
2. [Required Packages](#required-packages)
3. [Scene Hierarchy Setup](#scene-hierarchy-setup)
4. [Step-by-Step Setup](#step-by-step-setup)
5. [Input System Configuration](#input-system-configuration)
6. [Player State Data Configuration](#player-state-data-configuration)
7. [Troubleshooting](#troubleshooting)
8. [Recommended Values for Celeste Feel](#recommended-values-for-celeste-feel)

---

## Overview

This system combines **Celeste-style instant, snappy movement** with **Fez-style world rotation**. The camera system uses **Cinemachine** for smooth following, while the **FezWorldRotation** script handles the 90° perspective shifts.

### How the Camera System Works

```
[FezCameraRig] (Empty GameObject at 0,0,0)
    │
    ├── FezWorldRotation.cs (rotates this rig 90° per step)
    │
    └── [CinemachineCamera] (child - orbits WITH the rig)
            │
            ├── Follow Target: Player
            └── LookAt Target: Player
```

When the world rotates:
1. `FezWorldRotation` rotates the **rig** (parent)
2. The Cinemachine camera (child) **orbits with it**
3. Cinemachine continues to **follow the player** smoothly

---

## Required Packages

Install these via **Window > Package Manager**:

| Package | Version | Purpose |
|---------|---------|---------|
| **Cinemachine** | 3.x (Unity 6) or 2.x (older) | Camera following |
| **Input System** | 1.7+ | Player input |

### Installing Packages

1. Open **Window > Package Manager**
2. Click **+ > Add package by name**
3. Add: `com.unity.cinemachine`
4. Add: `com.unity.inputsystem`
5. If prompted, restart Unity and enable the new Input System

---

## Scene Hierarchy Setup

Create this exact hierarchy in your scene:

```
📁 Scene
│
├── 🎥 MainCamera (Tag: MainCamera)
│       └── [No scripts needed - Cinemachine controls it]
│
├── 🎬 FezCameraRig (Position: 0, 0, 0)
│       │
│       ├── 📜 FezWorldRotation.cs
│       │
│       └── 🎥 CinemachineCamera (Position: 0, 5, -15)
│               │
│               ├── CinemachineCamera component (Unity 6)
│               │   OR CinemachineVirtualCamera (older Unity)
│               │
│               ├── Follow: [Player]
│               └── LookAt: [Player]
│
├── 🧍 Player (Position: 0, 1, 0)
│       │
│       ├── 📜 Fused_PlayerController.cs
│       ├── 📜 FezDepthSnapper.cs (optional)
│       ├── 🔲 Rigidbody
│       ├── 🔲 CapsuleCollider (or BoxCollider)
│       ├── 📜 PlayerInput (Input System)
│       │
│       └── 👁️ Visual (Child - Position: 0, 0, 0)
│               │
│               └── 🖼️ SpriteRenderer
│
├── 💡 Directional Light
│
└── 🏗️ Environment
        ├── Ground (with Collider, Layer: Ground)
        └── Walls (with Colliders, Layer: Wall)
```

---

## Step-by-Step Setup

### STEP 1: Create Layers

1. Go to **Edit > Project Settings > Tags and Layers**
2. Add these layers:
   - Layer 6: `Ground`
   - Layer 7: `Wall`
   - Layer 8: `Player` (optional)

---

### STEP 2: Create the Camera Rig

1. **Create empty GameObject**: Right-click in Hierarchy > Create Empty
2. **Name it**: `FezCameraRig`
3. **Position**: Set Transform to `(0, 0, 0)`
4. **Add script**: Add `FezWorldRotation.cs` component

---

### STEP 3: Create the Cinemachine Camera

#### For Unity 6+ (Cinemachine 3.x):

1. **Right-click on FezCameraRig** > Cinemachine > Cinemachine Camera
2. A `CinemachineCamera` is created as a **child** of the rig
3. **Position the camera child**: `(0, 5, -15)` (relative to rig)
4. **Rotation**: `(15, 0, 0)` for a slight downward angle

#### For Unity 2021-2022 (Cinemachine 2.x):

1. **Right-click on FezCameraRig** > Cinemachine > Virtual Camera
2. Position: `(0, 5, -15)`
3. Rotation: `(15, 0, 0)`

---

### STEP 4: Configure FezWorldRotation

Select `FezCameraRig` and configure the `FezWorldRotation` component:

| Field | Value | Description |
|-------|-------|-------------|
| **Pivot Rig** | `FezCameraRig` (self) | Drag itself here |
| **Follow Target** | `Player` | Drag your player here |
| **Virtual Camera** | The child Cinemachine Camera | Optional but recommended |
| **Can Rotate** | ✓ | Master toggle |
| **Input Mode Enabled** | ✓ | Allow Q/E rotation |
| **Trigger Mode Enabled** | ✓ | Allow trigger zones |
| **Rotation Duration** | 0.5 | Seconds per 90° rotation |
| **Follow Speed** | 10 | How fast rig follows player |
| **Follow During Rotation** | ✓ | Keep following while rotating |

---

### STEP 5: Configure Cinemachine Camera

Select the Cinemachine Camera (child of rig):

#### For Cinemachine 3.x (Unity 6):

| Field | Value |
|-------|-------|
| **Tracking Target** | Player |
| **Look At Target** | Player |
| **Body > Position Composer** | (Add if not present) |
| **Aim > Rotation Composer** | (Add if not present) |

#### For Cinemachine 2.x:

| Field | Value |
|-------|-------|
| **Follow** | Player |
| **Look At** | Player |
| **Body** | Framing Transposer or Transposer |
| **Aim** | Composer |

##### Body Settings (Framing Transposer):
- Camera Distance: 15
- Screen X: 0.5
- Screen Y: 0.5
- Dead Zone Width: 0.1
- Dead Zone Height: 0.1

---

### STEP 6: Create the Player

1. **Create Capsule**: Right-click > 3D Object > Capsule
2. **Name it**: `Player`
3. **Position**: `(0, 1, 0)`
4. **Tag**: Set to `Player`
5. **Layer**: Set to `Player` (optional)

#### Add Components to Player:

| Component | Settings |
|-----------|----------|
| **Rigidbody** | Mass: 1, Use Gravity: OFF (controller handles it), Constraints: Freeze all rotation |
| **CapsuleCollider** | Height: 2, Radius: 0.5 |
| **Fused_PlayerController** | (see configuration below) |
| **PlayerInput** | (Input System component) |
| **FezDepthSnapper** | Optional - snaps player to platforms after rotation |

---

### STEP 7: Create the Visual Child

1. **Create child under Player**: Right-click on Player > Create Empty
2. **Name it**: `Visual`
3. **Position**: `(0, 0, 0)` relative to Player
4. **Add SpriteRenderer** or your character model

If using a sprite:
1. Add **SpriteRenderer** component to Visual
2. Assign your sprite
3. Set **Sprite Sort Point** to `Pivot`

---

### STEP 8: Create Player State Data

1. **Right-click in Project**: Create > Fused > Player State Data
2. **Name it**: `PlayerState_Default` (or Fire/Ice for dual states)
3. **Configure values** (see recommended values section below)

---

### STEP 9: Configure Fused_PlayerController

Select the Player and configure `Fused_PlayerController`:

| Section | Field | Value |
|---------|-------|-------|
| **References** | Visual Transform | Drag `Visual` child here |
| | Primary State Data | Drag your state data asset |
| | Secondary State Data | Optional second state |
| **Detection Layers** | Ground Layer | Select `Ground` layer |
| | Wall Layer | Select `Wall` layer |
| **World Rotation** | Use World Rotation | ✓ |
| | World Rotation Controller | Drag `FezCameraRig` here (auto-finds if left empty) |

---

### STEP 10: Create Ground and Walls

#### Ground:
1. Create > 3D Object > Cube
2. Scale: `(20, 1, 20)`
3. Position: `(0, -0.5, 0)`
4. **Layer**: `Ground`
5. Add material for visibility

#### Walls:
1. Create > 3D Object > Cube
2. Scale as needed
3. **Layer**: `Wall`

---

## Input System Configuration

### STEP 1: Create Input Actions Asset

1. **Right-click in Project**: Create > Input Actions
2. **Name it**: `PlayerInputActions`
3. **Double-click** to open Input Actions editor

### STEP 2: Create Action Map

1. Click **+** next to Action Maps
2. Name it: `Player`

### STEP 3: Create Actions

Add these actions to the `Player` action map:

| Action Name | Action Type | Control Type | Bindings |
|-------------|-------------|--------------|----------|
| **Move** | Value | Vector2 | WASD, Left Stick |
| **Jump** | Button | Button | Space, South Button (A) |
| **Dash** | Button | Button | Shift, West Button (X) |
| **Grab** | Button | Button | Z, Left Trigger |
| **FastFall** | Button | Button | S (down), Left Stick Down |
| **StateSwitch** | Button | Button | Tab, Right Bumper |
| **RotateLeft** | Button | Button | Q, Left Bumper |
| **RotateRight** | Button | Button | E, Right Bumper |

### STEP 4: Configure Move Action

1. Select `Move` action
2. Add binding > **2D Vector Composite** (for WASD)
   - Up: W
   - Down: S
   - Left: A
   - Right: D
3. Add binding > **Left Stick** (for gamepad)

### STEP 5: Save and Generate C# Class

1. Click **Save Asset**
2. Check **Generate C# Class**
3. Click **Apply**

### STEP 6: Add PlayerInput Component

1. Select Player
2. Add **PlayerInput** component
3. **Actions**: Drag your `PlayerInputActions` asset
4. **Default Map**: `Player`
5. **Behavior**: `Invoke Unity Events`

### STEP 7: Connect Events

Expand `Events > Player` and connect:

| Event | Target | Function |
|-------|--------|----------|
| **Move** | Player | Fused_PlayerController.OnMove |
| **Jump** | Player | Fused_PlayerController.OnJumpInput |
| **Dash** | Player | Fused_PlayerController.OnDashInput |
| **Grab** | Player | Fused_PlayerController.OnGrabInput |
| **FastFall** | Player | Fused_PlayerController.OnFastFallInput |
| **StateSwitch** | Player | Fused_PlayerController.OnStateSwitchInput |
| **RotateLeft** | FezCameraRig | FezWorldRotation.OnRotateLeft |
| **RotateRight** | FezCameraRig | FezWorldRotation.OnRotateRight |

---

## Player State Data Configuration

### Recommended Values for Celeste Feel

```
═══════════════════════════════════════
        IDENTIFICATION
═══════════════════════════════════════
State Name: "Celeste Default"
State Color: White (or your preference)

═══════════════════════════════════════
        MASTER CONTROLS
═══════════════════════════════════════
Can Move: ✓
Can Switch State: ✓
State Switch Cooldown: 0.5

═══════════════════════════════════════
        HORIZONTAL MOVEMENT
═══════════════════════════════════════
Max Move Speed: 9
Minimum Speed Threshold: 0.1
Acceleration Mode: INSTANT ← KEY FOR CELESTE FEEL
Deceleration Mode: INSTANT ← KEY FOR CELESTE FEEL
Instant Turning: ✓

═══════════════════════════════════════
        JUMPING
═══════════════════════════════════════
Can Jump: ✓
Jump Force: 10.5
Jump As Impulse: ✗ (unchecked = more predictable)
Variable Jump Height: ✓
Min Jump Multiplier: 0.4
Max Jump Hold Time: 0.2
Coyote Time Enabled: ✓
Coyote Time Duration: 0.1
Jump Buffer Enabled: ✓
Jump Buffer Duration: 0.1

═══════════════════════════════════════
        GRAVITY & FALLING
═══════════════════════════════════════
Gravity Mode: VARIABLE ← KEY FOR CELESTE FEEL
Base Gravity: 40
Rising Gravity Multiplier: 0.5
Jump Cut Gravity Multiplier: 2.5
Falling Gravity Multiplier: 1.5
Apex Hang Enabled: ✓
Apex Velocity Threshold: 2
Apex Gravity Multiplier: 0.4
Max Fall Speed: 25

═══════════════════════════════════════
        WALL MECHANICS
═══════════════════════════════════════
Wall Slide Mode: Automatic
Wall Slide Speed: -3
Wall Climb Enabled: ✓
Wall Climb Speed: 4
Stamina Mode: Drain On Wall
Max Stamina: 100
Stamina Drain Rate: 20

═══════════════════════════════════════
        WALL JUMP
═══════════════════════════════════════
Wall Jump Enabled: ✓
Wall Jump Horizontal Force: 8
Wall Jump Control Lock Enabled: ✓
Wall Jump Control Lock Duration: 0.15
Wall Jump Lock Control Multiplier: 0.3

═══════════════════════════════════════
        DASH
═══════════════════════════════════════
Dash Enabled: ✓
Air Dash Enabled: ✓
Dash Direction Mode: Input Eight Way
Max Dash Charges: 1
Dash Refill Mode: On Ground Touch
Dash Speed: 24
Dash Duration: 0.15
Dash Freezes Gravity: ✓
Dash End Velocity Multiplier: 0.4
```

---

## Troubleshooting

### Camera Not Following Player

**Symptoms**: Camera stays in place, doesn't move with player

**Solutions**:
1. ✓ Check Cinemachine Camera has **Follow Target** set to Player
2. ✓ Check Cinemachine Camera is a **child** of FezCameraRig
3. ✓ Check FezWorldRotation has **Follow Target** set to Player
4. ✓ Check **Follow Speed** > 0 (try 10)
5. ✓ Make sure MainCamera exists and has **MainCamera** tag

### Camera Not Rotating

**Symptoms**: Q/E doesn't rotate the world

**Solutions**:
1. ✓ Check **Can Rotate** is enabled on FezWorldRotation
2. ✓ Check **Input Mode Enabled** is enabled
3. ✓ Check input events are connected (RotateLeft/RotateRight)
4. ✓ Check **Is Rotating** isn't stuck (in inspector during play)

### Player Not Moving

**Symptoms**: WASD does nothing

**Solutions**:
1. ✓ Check **Can Move** is enabled in state data
2. ✓ Check state data is assigned to controller
3. ✓ Check PlayerInput component has actions assigned
4. ✓ Check Move event is connected to OnMove
5. ✓ Check Rigidbody isn't set to Kinematic
6. ✓ Check **Max Move Speed** > 0

### Player Falling Through Ground

**Symptoms**: Player drops through floor

**Solutions**:
1. ✓ Check ground has a **Collider** component
2. ✓ Check ground layer is set to `Ground`
3. ✓ Check **Ground Layer** mask includes Ground layer in controller
4. ✓ Check Rigidbody Collision Detection is `Continuous`

### Player Not Jumping

**Symptoms**: Space does nothing

**Solutions**:
1. ✓ Check **Can Jump** is enabled in state data
2. ✓ Check Jump event is connected to OnJumpInput
3. ✓ Check **Jump Force** > 0
4. ✓ Check player is detected as grounded (see gizmos)

### Wall Slide Not Working

**Symptoms**: Player doesn't slide on walls

**Solutions**:
1. ✓ Check wall has a **Collider** component
2. ✓ Check wall layer is set to `Wall`
3. ✓ Check **Wall Layer** mask includes Wall layer
4. ✓ Check **Wall Slide Mode** is not Disabled
5. ✓ Check **Stamina** hasn't depleted

### Depth Snapping Issues After Rotation

**Symptoms**: Player floats or clips after world rotation

**Solutions**:
1. ✓ Add **FezDepthSnapper** component to Player
2. ✓ Check snap settings (Max Snap Distance, etc.)
3. ✓ Check platforms have colliders on correct layers

### Gizmos Not Showing

**Solutions**:
1. ✓ Enable **Gizmos** button in Scene view toolbar
2. ✓ Check debug toggles in controller inspector
3. ✓ Select the player to see OnDrawGizmosSelected

---

## Quick Reference: Component Checklist

### FezCameraRig needs:
- [ ] FezWorldRotation script
- [ ] Pivot Rig reference (itself)
- [ ] Follow Target reference (Player)
- [ ] Child Cinemachine Camera

### Cinemachine Camera needs:
- [ ] Follow/Tracking Target (Player)
- [ ] LookAt Target (Player)
- [ ] Proper position relative to rig

### Player needs:
- [ ] Fused_PlayerController script
- [ ] Rigidbody (Use Gravity OFF)
- [ ] Collider
- [ ] PlayerInput component
- [ ] Visual child with SpriteRenderer
- [ ] Visual Transform reference set
- [ ] State Data assigned
- [ ] Layer masks configured

### State Data needs:
- [ ] Created via Create > Fused > Player State Data
- [ ] Assigned to controller
- [ ] Can Move enabled
- [ ] Reasonable values set

---

## Final Verification

Test in Play mode:
1. ✓ Camera follows player smoothly
2. ✓ WASD moves player (instant response)
3. ✓ Space jumps (variable height with hold)
4. ✓ Q/E rotates world 90°
5. ✓ Shift dashes in input direction
6. ✓ Walls detected (see gizmos)
7. ✓ Wall slide works when touching walls
8. ✓ Debug labels show in scene view

If all checks pass, your setup is complete! 🎮
