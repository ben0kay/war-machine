# PLAYER

`FirstPersonPlayer.cs` adapts arrow-key and current camera yaw to the existing shared player physics/crouch. Third-person art is hidden; no duplicate movement or procedural mesh code.

FPS-only acceleration, braking, reversal and ground-contact settings now come from the camera's FirstPersonMaster. It publishes one eye pose after shared physics, including crouch. The shared controller's new velocity hook retains its old default formula for top-down.

## Shared RMB jump (2026-10-28)

The inherited PlayerController handles grounded jump impulse and gravity for both views. `FirstPersonPlayer.CanAcceptJumpInput()` only permits the right-mouse press while mouse look is captured; Esc-released cursor/debug UI cannot trigger a jump. FPS camera eye positioning remains derived from player physics snapshots, so vertical jumps move the view without a duplicate camera jump loop. Uses shared `PlayerMaster.tres` `JumpVelocity=6.0`m/s. No new FPS-specific movement controls or processing loops.
