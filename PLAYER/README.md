# PLAYER

Active player: `Player.tscn`, `PlayerController.cs`, `PlayerSettings.cs` and `Configuration/PlayerMaster.tres`. Movement, crouch, rig animation and weapon pose stay with the player. `Equipment/PlayerWeaponController.cs` adapts mouse aim/fire to reusable EQUIPMENT mechanics.

`C/PlayerMeshBuilder.cs` builds a lightweight **stylized soldier** with rounded/tapered geometry, a tactical silhouette and code-generated camouflage fabric, while retaining the cached rig, crouch/walk pivots and weapon socket. **C/ means procedural mesh generation only**, not general C# scripts. `Artwork/` is reserved for a future imported model; any replacement must retain or adapt the rig/socket contract needed by PlayerController. Walking uses phase-offset knee flex, ankle roll, torso counter-rotation, shoulder sway and lightly bending elbows driven by actual travel, with tweakable gait settings in PlayerMaster. See `C/README.md` for visual details and checks.

`LegacyFirstPerson/` contains the parked explorer, flight/FPS settings, camera mode button and scene. The original WORLD/testworld.tscn still instances that scene. Its resources are separate from the active player.

WorldController supplies a runtime PlayerSettings copy after safe terrain placement. Controls now include **Q quick shove** in addition to arrows, Shift run, Right Ctrl crouch and mouse aim/left click fire. Shared `Equipment/PlayerQuickMelee.cs` dispatches the COMBAT/Melee contact; pose/animation work is deferred. No new polling loops or behaviour changes are intended by this relocation. Local compilation and gameplay checks remain pending.

The isolated FPS experiment subclasses this controller under DEBUG/TEST/FirstPerson/PLAYER. Virtual input/direction hooks reuse this same physical movement and crouch implementation. The normal player still uses arrows and cached overhead axes.

FPS refinement adds a minimal virtual UpdateHorizontalVelocity hook; its base implementation preserves the prior MoveToward acceleration formula. Only DEBUG/TEST/FirstPerson overrides braking/speeds/ground settings. Local build and top-down regression check pending.

## Top-down camera-relative movement — rotation fix

`PlayerController` still caches ground-plane screen-right and screen-forward axes rather than recomputing them every physics tick. The top-down `Camera.SetYaw()` now emits its existing `SettingsChanged` event, which refreshes those axes immediately. This keeps arrow-key walking relative to the *current* screen direction after the camera's 0–360° rotation slider changes, including while a movement key is held; tilt changes are handled as well. A previous camera subscription is removed on reinitialization and unload. No new update loop, movement input mapping, character acceleration, mouse aim, camera behaviour or FPS controls were changed. The FPS test overrides `GetMovementDirection()` and continues sampling the eye camera's current basis for movement.

**Local check pending:** build `dotnet build war-machine.csproj`, F6 `WORLD/World.tscn`, rotate the Camera slider to 0°, 90°, 180°, 270° and 360°, and verify arrow directions stay screen-relative while moving and after stopping. Briefly F6 the FPS test and verify arrow-key movement with mouse-look is unchanged. This was inspected statically only; no in-engine validation or terrain rebake.

## Quick shove momentum feel

The Q shove is usable while standing, walking or running in both overhead and FPS. Only its impact strength changes: `PlayerQuickMelee` samples real XZ speed once on attack start and scales the reaction through the three new `QuickShoveMaster.tres` movement tuning fields; no extra input, animations or permanent tick loops. Zero-speed pushes are deliberately small, full-speed runs considerably stronger. Knockback remains collision-aware in the zombie receiver and no HP is lost by default. Awaiting in-game tuning.
