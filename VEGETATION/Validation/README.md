# Grass / close-mist refinement — validation, 2026-10-25

**Implemented; awaiting target-PC visual/performance acceptance.** Scope is the requested TestWorld grass replacement and close Sky3D mist, not the remaining WE2 geography or future vegetation gameplay. Job commit: `git log -1 --format=%H -- VEGETATION/Validation/README.md`.

## Review and resulting behaviour

The previous implementation used 46,845 sparse, solid-coloured crossed cards saved across the kilometre landscape, a simple sine sway, and an abrupt mesh visibility limit. The included particle demo instead uses dense RibbonTrailMesh blades, noise-shaped patch/clump height variation, colour variation, terrain normal alignment/slope filtering, painted-rock exclusion, wind-noise bending and scale-based distance fade.

The adaptation keeps that actual shader pipeline and noise setup in world-owned resources. Height defaults to 0.6 of the demo scale to keep clumps readable around the exploration camera; Height=1 restores the original scale. Linear blade colour is passed as a Vector3 so Godot does not apply a second colour conversion. Camera-relative fading runs in the ribbon vertex shader continuously; the original start-only distance filter is replaced. Missing regions and terrain holes reject particles safely instead of using invalid texture-array reads or NaN geometry.

The demo's GDScript controller uploads terrain maps and other parameters every physics tick and restarts the whole grid after approximately one metre of camera movement. The new C# controller uploads bindings only on initialization/map changes and recycles incoming tiles only at tile boundaries. Default 7×7 coverage maintains the full configured 60m radius anywhere inside the central tile; the reference uses 5×5 cells and may have a narrower covered strip near a grid edge. The extra simulation coverage has a GPU cost: use quality, density, radius and the hard budget to suit the target PC.

Sky3D's real SkyDome now receives FogEnabled=true, FogDensity=0.025, FogStart=0m, FogEnd=200m, FogFalloff=3 from EnvironmentMaster, once at launch or explicit ApplyMist. Sky3D's atmosphere/solar lighting still drives fog colour. No second WorldEnvironment, volumetric layer, replacement sun or weather manager was added.

## Automated checks actually run

Linux; Godot **4.5.1 .NET**, .NET SDK **8.0.427**. Project's existing 4.7 feature marker and Windows D3D12 configuration remain unchanged.

- `dotnet build --no-restore --nologo`: passed, zero warnings/errors.
- Both adapted shaders loaded/parsed successfully; actual Forward+ Vulkan rendering completed with no shader compilation errors.
- `ValidateGrass.gd`: **23 checks passed**. Covers default bounded pool and budget; dormant CPU callbacks; disabled legacy cards; actual SkyDome fog shader parameters; linear grass colour and fade distance; no stationary or sub-tile terrain uploads/restarts; exactly seven incoming tile restarts on a one-axis crossing; coalesced map refresh; disabled/zero-density shutdown; quality reduction; hard-budget limiting; reactivation; moving sun/P pause/F8 preview; preserved saved height; clean unload.
- `git diff --exit-code origin/main -- WORLD/Terrain/Data demo addons`: passed before publishing. All saved terrain/authoring bytes and original demo/addon files are unchanged. Only the world-owned legacy mesh asset is disabled.
- A headless editor import registered resources but its .NET editor MSBuild integration hit an environment-specific BuildEnvironmentHelper NullReferenceException. Direct CLI compilation and both headless and rendered game launches passed. Local .NET editor/F5/F6 validation is still required; the editor import is not reported as clean.
- Existing Terrain3D `instance_reset_physics_interpolation()` deprecation warning remains. No addon source was modified to silence it.

## Rendered appearance / measured counters

The real `WORLD/testworld.tscn` was rendered through Forward+ Vulkan using **llvmpipe software rendering**, at 640×360. Before, after and the **unmodified included particle grass scene** were compared at the same terrain point (512,500), 4m above ground, sun fixed at 10:00. Successive captures with fixed sun/cloud movement show grass wind movement. A second viewpoint looks across the misty valley.

Screenshots: [before](before.png), [adapted ribbons](after.png), [original demo ribbons in TestWorld](demo-reference.png), [closer valley mist](mist.png). Captures are diagnostic evidence; they are not a claim of target-PC visual acceptance. The before view uses original fog 0.01/1000m; all ribbon views use requested close mist 0.025/200m.

Godot render counters at the fixed comparison view (whole scene, not grass-only GPU timings):

| View | Draw calls | Primitives reported |
| --- | ---: | ---: |
| Legacy cards | 301 | 1,126,036 |
| Adapted High ribbons | 209 | 3,855,778 |
| Adapted Low ribbons | 209 | 1,782,178 |
| Unmodified demo ribbons in TestWorld | 192 | 2,289,058 |

High uses 451,584 particle slots; Low uses 112,896; Medium uses 268,324 with default density/range. Frustum/terrain/patch filtering means these are slot budgets, not visible blade counts. More geometry and active GPU simulation are expected compared with the sparse cards. **No target FPS, GPU frame-time gain or CPU/GPU allocation measurement is claimed.** Software-renderer timings are not representative of the owner's GPU.

## Reproduce and accept locally

Prerequisites: Godot .NET editor/executable, .NET 8+ SDK on PATH, checked-in addons/resources. Replace `godot` with your full executable path. Do not bake/regenerate terrain.

```sh
dotnet build
godot --headless --path . --script res://VEGETATION/Validation/ValidateGrass.gd
godot --path . --audio-driver Dummy --resolution 640x360 --script res://VEGETATION/Validation/CaptureGrass.gd
```

CaptureGrass is an optional diagnostic runner that instantiates the actual world; F5/F6 still run TestWorld normally. Default screenshot output is `user://GrassValidation`; use `-- --output=/absolute/output/path` to choose another directory. It temporarily toggles legacy/adapted/reference renderers in memory and never saves terrain or resources. Diagnostic scripts are GDScript to support direct `--script` execution; all project gameplay integration remains C#.

In `WORLD/testworld.tscn`, inspect near/mid/far views, patch variation, steep rock without grass, wind at 0 and normal strength, smooth grass shrink/fade while flying and while crossing tile boundaries, and much closer valley/background obscuration. Check the existing WASD/QE/Space/Shift/Esc/P/F8 controls and sun/shadow movement. Restart and verify authored landforms remain unchanged.

Profile **High/Medium/Low**, then disabled/zero density → enabled, stationary versus flight/teleport, looking toward/away from grass, wind off/on, and map unload/reload. Record target hardware/resolution, CPU/GPU frame time, draw calls, particle cost, memory and transition hitches. Check idle callbacks/uniform writes and native renderer behaviour under the owner's configured Godot/Windows driver. User visual acceptance and those target-PC profiler results remain pending.

## Lifecycle / limitations

Wake: world initialization, explicit ApplySettings, explorer transform signal on tile crossing, native terrain-map change signals. Sleep: no grass CPU process/physics callback exists; stationary camera triggers no movement signal; same-tile movement returns without GPU writes. Off/zero density removes emitters. Active grass intentionally simulates wind on GPU at configured cadence; zero wind skips its noise sample while grass rendering/particle bookkeeping remain. Terrain/height bounds update only when needed. Pool size is bounded; resources/signals clean up with the world.

This remains decorative, camera-local grass. It does not add trees, gameplay plants or a general streamer. Existing native region grass data is preserved but hidden; WorldMaster's legacy grass-bake controls do not drive this particle renderer. The new master resource is authoritative for visible grass. Full temporal smoothness, driver compatibility and target-GPU profiling require local acceptance.
