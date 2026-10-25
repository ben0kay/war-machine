# WE0 / WE1 implementation and validation — 2026-10-25

**State: Awaiting Validation.** Implementation and headless checks passed. User visual acceptance, target-PC frame timing and the configured Godot 4.7 / Windows D3D12 runtime are pending. Job commit: the commit that introduces this file (`git log -1 --format=%H -- WORLD/WE0_WE1_VALIDATION.md`). One implementation commit is used.

## WE0 audit

- Terrain3D 1.0.2: Linux/Windows binaries exist, minimum declared Godot 4.4. Linux native extension successfully loads in Godot 4.5.1.
- Sky3D 2.1 is installed, MIT licensed and enabled beside Terrain3D. Both checked-in addon folders were identified; no third addon folder is present. `demo/Sky3DDemo.tscn` is a demo, not a separate plugin.
- Actual API: Sky3D `current_time`, `minutes_per_day`, `update_interval`, `game_time_enabled`; TimeOfDay starts/stops a Timer. SunLight and MoonLight are controlled by SkyDome. No independent solar calculation was added.
- Main Terrain3D demo uses cliff/grass slope autoblending, macro variation, projection, dual scaling and CC0 ambientCG textures. Its wind-moving grass is the GPU particle example, **not** its static texture-card asset. WE1 uses Terrain3D saved mesh instances with a small shared wind shader inspired by that example; it avoids the example's per-frame terrain-uniform rewrites.
- Native Terrain3D regions save independently under WORLD. Runtime never imports/generates a map. Bake refuses existing `.res` files.

## Checks actually run

Environment: Linux, Godot **4.5.1 .NET**, .NET SDK **8.0.427**; `war-machine.csproj` pins Godot.NET.Sdk 4.5.1. Project's original 4.7 feature marker and Windows D3D12 configuration are retained. Local 4.7 compatibility is not asserted. The .NET editor may offer its normal SDK upgrade on opening with a newer Godot version.

- `dotnet build --no-restore --nologo`: **passed**, zero warnings/errors. Initial restore used official Godot release bundled NuGet packages because network restore was slow; no machine-specific NuGet configuration is committed.
- `godot --headless --editor --path . --import`: **passed** with the SDK on PATH. A pristine import reports pre-existing missing `.mtl` references in unrelated demo primitive meshes. Original assets are preserved.
- `godot --headless --path . res://WORLD/Generation/BakeTerrain.tscn`: **passed**, one saved 1024m region, seed 78341, 46,845 grass clumps.
- Repeating that bake: **expected exit 1**, existing-data protection triggered before mutation.
- Headless main-scene launch and temporary integration harness: **passed**. Confirmed one loaded region, finite spawn height (47.9678688m), 4m spawn clearance, persisted grass instance dictionary, autoshader, dormant idle player, no custom sky process loop and four Sky3D ownership children.
- Timed integration checks: **passed** advancing Sky3D time and changing sunlight transform; P stops the timer and holds time; P resumes; F8 selects 1-minute day and returns to 24-minute configured day. Destroy/reinstantiate preserves sampled terrain height.
- Native Terrain3D emits an `instance_reset_physics_interpolation()` deprecation warning under 4.5.1; no addon source was changed.

No rendered screenshot, grass animation, material appearance, shadows, GPU measurements, keyboard flight on a physical window or actual F5/F6 editor key presses were validated here. Headless loading is not visual acceptance. No FPS claim is made.

## Local acceptance / Copilot handoff

Prerequisites: Godot .NET editor, .NET 8+ SDK on PATH, installed project addons. Read CodePreferences. Initially run checks without changing code, and return command/exit code plus failures.

```sh
dotnet build
godot --headless --editor --path . --import
godot --headless --path . --quit-after 180
```

Use the full path of your Godot .NET executable if `godot` is not in PATH. Do not rerun the bake to play. Expected: no C# compile or world/addon loading errors; the noted native deprecation or unrelated demo `.mtl` warnings may appear.

1. Open `WORLD/testworld.tscn`. F5 and F6 should show the same world. Inspect varied valley/ridges, flatter grass and exposed steep rock. Compare with `demo/Demo.tscn`; accepted material quality needs human judgment.
2. WASD/QE/Space/Shift/Esc: traverse near and far, inspect grass moving in wind, release keys and alt-tab. Free-fly intentionally passes through terrain; no walking collision is claimed.
3. F8: watch a complete accelerated day, sun/moon, terrain lighting and shadows; P freezes solar movement, P resumes, F8 restores normal speed. There must be no second static sun/environment.
4. Restart Godot. Landforms/materials/grass should persist. Test authored sculpt/save/reopen on a backup.
5. Profile stationary, flight, paused sun, accelerated sun, grass near/far; record hardware, CPU/GPU frame time and memory. Verify player callbacks stop idle. Inspect Terrain3D LOD/culling and pop-in. No world streaming is implemented in WE1.

## Performance handoff

Baking runs once only on explicit authoring scene execution and exits. Runtime loads native data. Player wakes from input and stops on no movement/capture loss/focus loss. World spawn work runs once. Sky bridge responds to startup and key events, with no frame polling. Sky3D owns active time timer and cloud processing; solar pause stops the timer. Terrain3D batches/culls saved grass instances; one wind shader animates tips on GPU without CPU blade scripts. No duplicate manager, custom streamer or per-frame full-world scan was introduced.

## Limits and next action

Finite 1km square has visible edges and no distant backdrop yet. No trees, rain, caves, water, survival or enemies. The initial free-flight camera is reusable. Directional shadows are enabled, but visual quality and target-engine renderer compatibility remain unverified. User approval of appearance/performance is needed to mark WE0/WE1 Accepted. WE2 and later are not authorized by this job.
