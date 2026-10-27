# PLAYER

Active player: `Player.tscn`, `PlayerController.cs`, `PlayerSettings.cs` and `Configuration/PlayerMaster.tres`. Movement, crouch, rig animation and weapon pose stay with the player. `Equipment/PlayerWeaponController.cs` adapts mouse aim/fire to reusable EQUIPMENT mechanics.

`C/PlayerMeshBuilder.cs` builds a lightweight **stylized soldier** with rounded/tapered geometry, a tactical silhouette and code-generated camouflage fabric, while retaining the cached rig, crouch/walk pivots and weapon socket. **C/ means procedural mesh generation only**, not general C# scripts. `Artwork/` is reserved for a future imported model; any replacement must retain or adapt the rig/socket contract needed by PlayerController. See `C/README.md` for visual details and checks.

`LegacyFirstPerson/` contains the parked explorer, flight/FPS settings, camera mode button and scene. The original WORLD/testworld.tscn still instances that scene. Its resources are separate from the active player.

WorldController supplies a runtime PlayerSettings copy after safe terrain placement. Controls remain arrows, Shift run, Right Ctrl crouch, mouse aim/left click fire. No new polling loops or behaviour changes are intended by this relocation. Local compilation and gameplay checks remain pending.

The isolated FPS experiment subclasses this controller under DEBUG/TEST/FirstPerson/PLAYER. Virtual input/direction hooks reuse this same physical movement and crouch implementation. The normal player still uses arrows and cached overhead axes.

FPS refinement adds a minimal virtual UpdateHorizontalVelocity hook; its base implementation preserves the prior MoveToward acceleration formula. Only DEBUG/TEST/FirstPerson overrides braking/speeds/ground settings. Local build and top-down regression check pending.
