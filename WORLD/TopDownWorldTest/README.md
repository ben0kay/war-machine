# Top-down World Test — Active game-feel prototype

**TD0–TD2 implemented, Awaiting Local Validation. TD3 remains Planned.** Standalone preview scene: `WORLD/TopDownWorldTest/topdownWorldTest.tscn`. The project's main scene remains `WORLD/testworld.tscn`.

Git branch: `implement-td0-td1`. **This is the active development direction**, although `WORLD/testworld.tscn` remains the unchanged F5 main scene. The old first-person environment is parked, not deleted.

## Immediate focus — player, camera, robot

**Get these mechanics feeling good before adding new gameplay:**

- **Player:** readable low-poly walk/run/idle animation, responsive camera-relative control, smooth starts/stops/turning and dependable Terrain3D grounding.
- **Camera:** comfortable angled orthographic view, follow smoothing, zoom and composition; retain quick tuning controls.
- **Robot:** refine the existing Robot_001 heavy cyan biped's gait and foot placement, body motion, patrol timing, stopping and head/scanner sweep. Reuse the same shared robot assembly and controller.
- **No zombie work now:** the ZS sandbox plan was cancelled. Zombies, robot red targeting/weapons, survival meters, combat, crafting and base building are **future design possibilities only**, not currently authorized implementation phases.

Long-term goal: a fully 3D, painterly/low-poly orthographic survival game balancing survival, combat and base building, with dominant roaming robots and later weaker zombie nuisances. For now, **movement and robot feel are the only priorities**.

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
- `C/TopDownCameraControls.cs`: live camera panel with downward tilt (25–80°) and view-size sliders. Wheel zoom updates the same value. **Copy settings** copies labelled numeric JSON for the current camera setup to the clipboard; paste it into chat to request new defaults. Copies the chosen zoom target even while smoothing finishes. Runtime changes are temporary and do not save the master resource. Pitch preserves player centring; yaw stays fixed so movement axes remain consistent. Signal-driven UI has no polling loop and releases its camera subscription on exit.
- `Configuration/TopDownTestMaster.tres` / `C/TopDownTestSettings.cs`: test-only player/camera settings and bake-time seed/spawn/relief/hill settings. No source masters are mutated during gameplay.

Preview reuses existing terrain material/assets, with their legacy grass mesh disabled. No grass emitter, trees, rocks, cave, weather, haze or F9 panel is instanced. A basic procedural sky, ambient light and shadowed sun are used; no extra sky addon/controller coupling. The actual existing Robot_001 scene is directly instanced for **TD2**; its shared files are unchanged.

## Collision and idle cost

The single preview region uses Terrain3D **Full/Game collision (3)**, so collision covers the walking player and hill regardless of the angled camera's ~21m horizontal offset. Actual orthographic camera is still registered for terrain rendering/LOD. No height-teleport workaround is used for walking: CharacterBody3D/MoveAndSlide handles ground collision. Terrain is static; no recurring noise or terrain sampling loop.

Movement key/focus changes wake player PhysicsProcess. It runs during movement/deceleration/falling and sleeps on floor once input/velocity settle. Art rests immediately when actual horizontal travel stops; no per-limb callbacks or idle animation. Position changes wake camera follow; wheel events wake zoom. Camera interpolation stops once position/size settle, and target subscriptions disconnect on exit. No global per-frame searches or new manager.

## Verification / handoff

Here: source review, resource/path/ID checks, camera-axis and hill-slope calculations, C# delimiter smoke checks and `git diff --check`. **No build, native terrain bake, scene load or runtime/render check was possible**: Godot/.NET absent. These phases are implemented but **Awaiting Validation**.

Brief local check: bake once and confirm the second bake refuses overwrite; run preview; test W/A/S/D screen directions, diagonal speed, Shift, stopping, focus loss, wheel bounds, steady follow and walking over the hill without falling. Check idle callbacks settle. Confirm F5 still opens the original world. Appearance/FPS acceptance stays with the owner; no extensive test suite was added.

Camera tuning follow-up: source wiring and whitespace reviewed; Godot/.NET still unavailable here. Locally drag tilt to both limits, adjust view size and use wheel zoom off the panel, confirm walking still works after clicking sliders, then click Copy settings and paste into a text editor/chat to compare the numeric values with the labels. Reopening the scene should restore authored defaults. No terrain rebake is needed for camera tuning.

## TD2 — Existing robot patrol

`C/TopDownRobotPlacement.cs` is a preview-only placement adapter; it does not use the main world's WorldConfig/ExplorationController or copy WORLD/RobotEncounter.tscn. It samples this preview's Terrain3D, caches three/four editable route markers and invokes `RobotController.InitializeEncounter(route, groundSampler, player)`.

The `RobotPatrol` node centres on spawn offset **(-24, +8)m**, with four markers forming a **12m square**. It starts at **(-30, +14)m** relative to the player (~33m away), in view toward the left/west-southwest and away from the hill. Inspect/edit RobotPatrol/Route in the preview scene. Entire route edges plus a 5m foot corridor are checked once for finite/level terrain. Invalid placement leaves the actor hidden/dormant with an Output error; the player/camera preview remains usable.

The robot has a local EntitySettings startup resource with StartActive=false and a hidden placement root, so a missing terrain bake never starts patrol/scanning/audio at world origin. After safe positioning, the real robot is shown and explicitly activated. Its own runtime master copy, original biped gait, scan shader, sensing, spatial sounds, gravity/height support and lifecycle remain intact. Actor shutdown handles unload. No new per-frame placement/relevance loop or global manager; full test-terrain collision already covers both player and robot. The optional sensing candidate is the visible test player; no damage/chase is added.

Check locally after the existing one-time terrain bake: watch two full walk → stop → scan → recover → walk cycles while moving the player around. Confirm the whole body advances, feet stay grounded, cyan beam sweeps, audio is bounded, and unloading leaves no errors. Check missing-data startup remains dormant. Source/resource/route-camera math checks passed here; Godot/.NET unavailable, so build and runtime/visual acceptance are still pending. TD3 is not automatically implemented.
