# TD — Orthographic Top-Down World Test

> **Plan only: TD0–TD3 Planned.** This does not implement a new scene or change the playable main world.
>
> Create `WORLD/TopDownWorldTest/topdownWorldTest.tscn`, an isolated Godot 4 C# 3D experiment. The real main world `WORLD/testworld.tscn` must remain untouched and be the project default.

## Intent and boundaries

Try a Project Zomboid-like **angled orthographic camera** in a fully 3D environment. A simple ~1.8m low-poly player walks around fairly flat Terrain3D with one small hill, while the **actual existing Robot_001** walks, stops, visibly scans cyan and resumes patrol nearby. Compare the feel of this view against the existing first-person world.

No grass, trees, rocks, bushes, caves, weather, ground haze, inventory, combat, extra enemies, F9 panel or terrain streaming framework. Sky3D for simple daylight/clouds is optional; if coupling makes it troublesome, use a basic sky, sun and ambient light.

Read root `README.md`, `CodePreferences.md`, `WORLD/README.md`, `WORLD/Generation/TerrainBake.cs`, `WORLD/RobotEncounter.cs`, `ENTITIES/TYPES/Robots/Robot_001/C/RobotController.cs`, and current Robot_001 README before implementation. Brief checks only, per CodePreferences.

## Phases

| Phase | Scope | Status |
| --- | --- | --- |
| **TD0** | Standalone scene and basic Terrain3D preview terrain | Planned |
| **TD1** | Low-poly human, walking, angled orthographic follow camera | Planned |
| **TD2** | Existing robot on nearby walk/stop/scan patrol | Planned |
| **TD3** | Small settings, documentation and sanity checks | Planned |

All phases are appropriate for Codex / ChatGPT Work. Implement only authorized phases. When all TD0–TD3 are authorized as one prototype job, keep it to one commit.

### TD0 — Separate simple Terrain3D

**Objective:** Collidable light-preview terrain, mostly level with gentle variation and one low hill, runnable as its own scene.

- Make a separate test Terrain3D data directory, e.g. `WORLD/TopDownWorldTest/Terrain/Data/`; **do not read/write** `WORLD/Terrain/PineLandscape` or `WORLD/Terrain/Data`.
- Reuse existing Terrain3D assets/material references, but **not** grass or existing world content nodes. Use one low-frequency FastNoiseLite relief pass and one rounded 4–8m hill off the central walking/patrol area. A 256–512m patch is preferred if the installed addon supports it; otherwise use its reliable 1024m region.
- Copy only the relevant *offline* terrain import/save pattern from `WORLD/Generation/TerrainBake.cs`. Commit the generated native preview terrain data if feasible; otherwise provide a documented one-shot creation command that refuses to overwrite existing data. Do not require world regeneration whenever the scene runs.
- The root scene should initialize the actual test Terrain3D camera reference and collision. Keep player and robot on gently sloped ground. Don't change `project.godot` main-scene selection.
- **Pass:** Run Current Scene shows simple ground, low hill and dependable walkable collision.

### TD1 — Visible player and orthographic camera

**Objective:** Immediately controllable camera-relative walking with simple visible animation.

- Add a **separate** test-player `CharacterBody3D` with capsule collision, gravity, floor snap and `MoveAndSlide`. Do not modify `PLAYER/ExplorationController.cs`.
- Construct a simple original low-poly humanoid from box/capsule meshes: head, body, arms, legs and feet. Idle pose; alternating arms/legs and subtle bob while walking; rotate toward move direction. Drive all visual limbs from a single controller, idle animation sleeps.
- A dedicated `Camera3D`: **Projection Orthographic**, fixed yaw around 45°, angled downward around **55°**, `Size` initially **40–55m**. Smoothly follow the player in world space, not the animated art root.
- **WASD = screen-relative movement** by projecting camera forward/right onto XZ, Shift = optional run, mouse wheel = bounded smooth zoom. Cursor need not be locked. Camera remains stable and player stays in frame at the hill.
- Register the active ortho camera with Terrain3D using `set_camera`. Check that its camera-centered LOD/collision region still covers the player even when the camera has a horizontal offset. Grounding/collision must not rely on a distant viewer patch.
- Config: camera pitch/yaw, size, zoom bounds, follow smoothing, player speed and gait rate under a test-only `Configuration/TopDownTestMaster.tres`.
- **Pass:** Player walks in expected screen directions, visibly steps, stops cleanly and follows uneven ground; camera follows and zooms.

### TD2 — Same existing Robot_001

**Objective:** Reuse, not recreate, the existing large biped and cyan scanner.

- Instance `res://ENTITIES/TYPES/Robots/Robot_001/Robot_001.tscn` directly.
- Do **not** copy the robot model/animation, or drag the entire `WORLD/RobotEncounter.tscn` into the test: its placement script assumes `ExplorationController` and `WorldConfig`.
- Instead create a tiny test-scene placement helper with 3–4 editable markers, sample **this test terrain's** `get_height` for waypoints and foot targets, and call the real `RobotController.InitializeEncounter(route, groundSampler, lowPolyPlayer)`.
- Robot should begin within roughly 20–35m of player spawn and visibly walk a safe, level loop, stop, sweep its beam and depart repeatedly. Keep visual/audio/scan/leg code from the existing robot unchanged. Player is an optional scan target; no pursuit or damage.
- **Pass:** Robot body actually moves and stays grounded, legs move, cyan beam sweeps for 2 cycles, and player can walk around while watching.

### TD3 — Usable single-scene experiment

- Keep scene ownership under `WORLD/TopDownWorldTest/` with `README.md`, this plan, a concise `C/` directory and test-only settings resource. Suggested code: `TopDownWorldTest.cs`, `TopDownPlayer.cs`, `TopDownCamera.cs`; create other files only when they earn their existence.
- Tiny label with controls only if helpful. No new framework or weather/foliage controllers.
- Performance: static terrain; player animates only in motion; camera follows only until settled; existing robot uses its active patrol and scan timers. No global per-frame scene searches, redundant materials or long-running noise.
- Brief validation: compile, run current scene, check WASD/zoom/hill/robot loop when local tools exist; report unverified tests honestly. The user will do appearance/FPS assessment.
- Do not update the original TestWorld or any saved normal-world terrain, weather settings or robot master.

## Suggested Codex instruction

Read the referenced files and implement **TD0–TD3 together** as one small, standalone `WORLD/TopDownWorldTest/topdownWorldTest.tscn` preview. Use separate simple Terrain3D ground with a hill, one primitive animated low-poly walking player, an angled following orthographic camera, and the **existing Robot_001** performing its real patrol/stop/cyan scan loop. No vegetation, rocks, caves, grass, weather or combat. Preserve the original world completely. Keep validation short, update documentation, then commit/push once.
