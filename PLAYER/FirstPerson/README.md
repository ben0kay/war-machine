> **Current ownership:** Production FPS adapter/profile in `PLAYER/FirstPerson`. [View composition and settings](../../WORLD/FirstPerson/README.md). Older test-folder statements below are historical; current paths and behaviour are defined by the promoted source.

# PLAYER

`FirstPersonPlayer.cs` adapts arrow-key and current camera yaw to the existing shared player physics/crouch. Third-person art is hidden; no duplicate movement or procedural mesh code.

FPS-only acceleration, braking, reversal and ground-contact settings now come from the camera's FirstPersonMaster. It publishes one eye pose after shared physics, including crouch. The shared controller's new velocity hook retains its old default formula for top-down.

## Shared RMB jump (2026-10-28)

The inherited PlayerController handles grounded jump impulse and gravity for both views. `FirstPersonPlayer.CanAcceptJumpInput()` only permits the right-mouse press while mouse look is captured; Esc-released cursor/debug UI cannot trigger a jump. FPS camera eye positioning remains derived from player physics snapshots, so vertical jumps move the view without a duplicate camera jump loop. Uses shared `PlayerMaster.tres` `JumpVelocity=6.0`m/s. No new FPS-specific movement controls or processing loops.

## Shared prone (2026-10-28)

The FPS scene inherits the **same** player stance state, low capsule and slow crawl from `PLAYER/PlayerController.cs`; no duplicate FPS prone movement or camera loop. **Right Ctrl** toggles prone, **Left Ctrl/C** crouches, **RMB** jumps only standing and when the cursor is captured. `FirstPersonCamera.RecordPhysicsPose(CapsuleHeight)` automatically lowers/interpolates the eye to about 0.60m at the default 0.75m prone collision height. The native FPS movement/acceleration/braking override remains intact, while `ProneSpeedMultiplier=0.17` from PlayerMaster makes crawling far slower than crouching. Test low ceilings, input blocking, collisions and view-height transitions locally; the physical low capsule is not a long horizontal prone hitbox yet.

## L0a — render-frequency lean

The FPS camera now receives target lean from shared physics and blends its visible camera roll and lateral offset on render frames with `LeanRenderSmoothingSeconds` (0.045s). This avoids the discrete physics-step rotation previously visible when leaning. Mouse yaw and recoil refresh a resting eye offset when already leaned. Weapons remain camera children. FPS authored tilt/shift increased to 16° / 0.20m; physics, stance and collision are unchanged. Local FPS visual and gun-recoil checks pending.


## ADS0 — FPS-only movement modifier (2026-10-31)

The FPS equipment adapter updates `FirstPersonPlayer.SetAdsMovementMultiplier` only on held ADS start/stop or cancellation. Shared `PlayerMovement` requests `GetMovementSpeedMultiplier()`, which defaults to 1.0 for top-down; the FPS override supplies the current ADS factor (rifle 0.70, MachineGun 0.85). This composes multiplicatively with stance, sprint, lean and corpse slowdown. The same physics driver wakes for movement changes; no second movement loop. See [ADS plan](../../EQUIPMENT/Weapons/ADS_IMPLEMENTATION_PLAN.md). Source implemented; local verification pending.

## ADS1 — independent carried firearm penalty (2026-10-31)

The FPS player now separately stores `_equippedMovementMultiplier` and `_adsMovementMultiplier`. `FirstPersonWeapon` applies the carried multiplier once after successful slot selection (rifle 0.90×, MachineGun 0.97×, bat 1.0×); the existing movement-speed hook returns their product. Each setter wakes existing physics only when the value changes; blocked input cancels ADS but the carried multiplier stays tied to the selected weapon. Tear-down restores 1.0×. Top-down movement remains unchanged. Source implemented; Godot/.NET local validation pending.

