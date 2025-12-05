# Simple Player Controller

Two scripts: settings + controller. That's it.

---

## Setup

1. **Import both `.cs` files** into your project

2. **Create settings asset**: Right-click → Create → Player Settings

3. **Setup player**:
   - Add `PlayerController` to your player
   - Assign the settings asset
   - Assign sprite transform (or leave empty to use self)

4. **Setup Input** (PlayerInput component):
   - Set Behavior to "Invoke Unity Events"
   - Connect: Move → `OnMove`, Jump → `OnJump`, Dash → `OnDash`, SwitchForm → `OnSwitchForm`

5. **Set layers** in settings: Ground Layer, Wall Layer

---

## Toggle Features

In the ScriptableObject inspector, toggle any feature on/off:

- `enableMovement` - left/right
- `enableJump` - ground jump
- `enableDoubleJump` - air jump
- `enableWallSlide` - slide down walls
- `enableWallJump` - jump off walls
- `enableDash` - ground dash
- `enableAirDash` - air dash
- `enableFlip` - sprite flipping

---

## Check State from Other Scripts

```csharp
PlayerController player = GetComponent<PlayerController>();

if (player.IsGrounded) { }
if (player.IsDashing) { }
if (player.IsWallSliding) { }
if (player.IsTouchingWall) { }
if (player.FacingDirection == 1) { } // 1 = right, -1 = left
```

---

## Form Switching (Ice/Fire)

1. Create two PlayerSettings assets with different values
2. Assign both to `iceSettings` and `fireSettings` on PlayerController
3. Toggle `enableFormSwitch` on
4. Add a "SwitchForm" input action and connect to `OnSwitchForm`

**From other scripts:**
```csharp
player.SwitchForm();     // Toggle between forms
player.SetIceForm();     // Force ice
player.SetFireForm();    // Force fire

if (player.IsIceForm) { } // Check current form
```

---

## Swap Settings (for forms)

```csharp
public PlayerSettings iceSettings;
public PlayerSettings flameSettings;

player.settings = iceSettings; // Switch to ice form
player.settings = flameSettings; // Switch to flame form
```
