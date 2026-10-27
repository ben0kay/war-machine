# PLAYER

Active player: `Player.tscn`, `PlayerController.cs`, `PlayerSettings.cs` and `Configuration/PlayerMaster.tres`. Movement, crouch, rig animation and weapon pose stay with the player. `Equipment/PlayerWeaponController.cs` adapts mouse aim/fire to reusable EQUIPMENT mechanics.

`C/PlayerMeshBuilder.cs` constructs the current procedural placeholder and cached rig. **C/ means procedural mesh generation only**, not general C# scripts. `Artwork/` is the future imported model location; a replacement must retain the rig/socket contract needed by PlayerController.

`LegacyFirstPerson/` contains the parked explorer, flight/FPS settings, camera mode button and scene. The original WORLD/testworld.tscn still instances that scene. Its resources are separate from the active player.

WorldController supplies a runtime PlayerSettings copy after safe terrain placement. Controls remain arrows, Shift run, Right Ctrl crouch, mouse aim/left click fire. No new polling loops or behaviour changes are intended by this relocation. Local compilation and gameplay checks remain pending.
