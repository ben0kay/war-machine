# Top-down World Test — TD0/TD1

**Implemented, Awaiting Local Validation. TD2/TD3 remain Planned.** Standalone preview scene: `WORLD/TopDownWorldTest/topdownWorldTest.tscn`. The project's main scene remains `WORLD/testworld.tscn`.

Git branch: `implement-td0-td1` (original `main` is unchanged).

## First run

1. Open the repository in your Godot **.NET** editor and build the C# project (`dotnet build war-machine.csproj` also works with its configured Godot SDK available).
2. Open `WORLD/TopDownWorldTest/Terrain/BakePreviewTerrain.tscn` and **Run Current Scene (F6)** once. It writes one native Terrain3D region only to this preview's `Terrain/Data/`, then exits. No overwrite: existing `.res` files cause a deliberate error.
3. Open `WORLD/TopDownWorldTest/topdownWorldTest.tscn` and **Run Current Scene (F6)**. Use **WASD** in screen directions, **Shift** to run and **mouse wheel** to zoom. Cursor stays visible. F5 still launches the original main world.

Command-line alternative after building/importing, replacing `godot` with your Godot .NET executable:

```sh
godot --headless --path . res://WORLD/TopDownWorldTest/Terrain/BakePreviewTerrain.tscn
godot --path . res://WORLD/TopDownWorldTest/topdownWorldTest.tscn
```

A missing terrain bake displays setup instructions instead of dropping the player into empty space or generating land on launch. Native data is not committed yet because this host has neither Godot nor dotnet. Once baked locally, `.res` files can be committed as preview authoring assets. To deliberately rebake, back up/remove **only this preview's** `.res` files before running its baker again. Changing bake-time settings does not silently change saved terrain.

## Ownership and defaults

- `C/TopDownTerrainBake.cs`: offline seed-based 1024×1024m terrain with low relief and a compact 5m rounded hill. One reliable 1024m native region follows the repository's established import/save API. It cannot target another directory. The larger region is the simple compatibility fallback rather than speculative smaller-region API changes.
- `C/TopDownWorldTest.cs`: load-only composition, runtime settings copy, safe spawn sampling and the active camera's Terrain3D registration.
- `C/TopDownPlayer.cs`: separate 1.8m visible box humanoid and one capsule, gravity, floor snap, acceleration/braking, collision and one joint animation driver. Existing ExplorationController is untouched.
- `C/TopDownCamera.cs`: 45° yaw / 55° down, orthographic size 48m, smoothly follows the physical root and zooms between 20–80m.
- `Configuration/TopDownTestMaster.tres` / `C/TopDownTestSettings.cs`: test-only player/camera settings and bake-time seed/spawn/relief/hill settings. No source masters are mutated during gameplay.

Preview reuses existing terrain material/assets, with their legacy grass mesh disabled. No grass emitter, trees, rocks, cave, weather, haze, F9 panel or robot is instanced. A basic procedural sky, ambient light and shadowed sun are used; no extra sky addon/controller coupling. Existing Robot_001 integration is **TD2**, not part of this job.

## Collision and idle cost

The single preview region uses Terrain3D **Full/Game collision (3)**, so collision covers the walking player and hill regardless of the angled camera's ~21m horizontal offset. Actual orthographic camera is still registered for terrain rendering/LOD. No height-teleport workaround is used for walking: CharacterBody3D/MoveAndSlide handles ground collision. Terrain is static; no recurring noise or terrain sampling loop.

Movement key/focus changes wake player PhysicsProcess. It runs during movement/deceleration/falling and sleeps on floor once input/velocity settle. Art rests immediately when actual horizontal travel stops; no per-limb callbacks or idle animation. Position changes wake camera follow; wheel events wake zoom. Camera interpolation stops once position/size settle, and target subscriptions disconnect on exit. No global per-frame searches or new manager.

## Verification / handoff

Here: source review, resource/path/ID checks, camera-axis and hill-slope calculations, C# delimiter smoke checks and `git diff --check`. **No build, native terrain bake, scene load or runtime/render check was possible**: Godot/.NET absent. These phases are implemented but **Awaiting Validation**.

Brief local check: bake once and confirm the second bake refuses overwrite; run preview; test W/A/S/D screen directions, diagonal speed, Shift, stopping, focus loss, wheel bounds, steady follow and walking over the hill without falling. Check idle callbacks settle. Confirm F5 still opens the original world. Appearance/FPS acceptance stays with the owner; no extensive test suite was added.
