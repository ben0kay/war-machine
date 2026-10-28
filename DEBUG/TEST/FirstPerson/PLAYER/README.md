# PLAYER

`FirstPersonPlayer.cs` adapts arrow-key and current camera yaw to the existing shared player physics/crouch. Third-person art is hidden; no duplicate movement or procedural mesh code.

FPS-only acceleration, braking, reversal and ground-contact settings now come from the camera's FirstPersonMaster. It publishes one eye pose after shared physics, including crouch. The shared controller's new velocity hook retains its old default formula for top-down.

## Shared RMB jump (2026-10-28)

The inherited PlayerController handles grounded jump impulse and gravity for both views. `FirstPersonPlayer.CanAcceptJumpInput()` only permits the right-mouse press while mouse look is captured; Esc-released cursor/debug UI cannot trigger a jump. FPS camera eye positioning remains derived from player physics snapshots, so vertical jumps move the view without a duplicate camera jump loop. Uses shared `PlayerMaster.tres` `JumpVelocity=6.0`m/s. No new FPS-specific movement controls or processing loops.

## Shared prone (2026-10-28)

The FPS scene inherits the **same** player stance state, low capsule and slow crawl from `PLAYER/PlayerController.cs`; no duplicate FPS prone movement or camera loop. **Right Ctrl** toggles prone, **Left Ctrl/C** crouches, **RMB** jumps only standing and when the cursor is captured. `FirstPersonCamera.RecordPhysicsPose(CapsuleHeight)` automatically lowers/interpolates the eye to about 0.60m at the default 0.75m prone collision height. The native FPS movement/acceleration/braking override remains intact, while `ProneSpeedMultiplier=0.17` from PlayerMaster makes crawling far slower than crouching. Test low ceilings, input blocking, collisions and view-height transitions locally; the physical low capsule is not a long horizontal prone hitbox yet.
