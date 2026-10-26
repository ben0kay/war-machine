# Tree fixes, FPS and exploration controls — 2026-10-25

**Implemented; awaiting user/target-PC visual and performance acceptance.** Job commit is the commit introducing this file (`git log -1 --format=%H -- WORLD/Vegetation/Trees/Validation/README.md`). No WE phase is advanced by this separate requested refinement.

## Changes

- Fixed the leaf shader's invalid `return` in vertex() using a guarded wind block; zero strength/speed leaves leaves static. Added a little leaf backlighting.
- Smaller, folded four-triangle leaves grow along connected fine sprigs, secondary twigs and inner crown clusters. Main branches begin lower and attach to the actual crooked trunk. Default near mesh has **3,572 leaves**, bounded by the 3,600 budget, rather than the previous 648 large flat diamonds.
- Far mesh retains **608 leaves** with coarser silhouettes. Native Forward+ visibility fades blend levels around 110m and fade the outer range around 240m (20m margins). Far shadows default off. Chunk-local anchors preserve terrain roots and make native range culling use the right world location. Extra culling margin covers leaf wind. Cache coverage/retention reduces repeated chunk generation at boundaries.
- Top-left FPS label samples native FPS at **2Hz**, changes text only when the integer changes, and stops its timer when hidden. Settings: `DEBUG/Configuration/DebugMaster.tres`.
- Arrows alias WASD; **hold Ctrl down / RMB up**, preserving QE/Space, Shift, Esc, P and F8. Mouse-only input now wakes/stops flight. Duplicate aliases do not multiply movement. Existing captured-mouse/focus rules still apply.

## Executed validation

Linux, Godot **4.5.1 .NET**, .NET SDK **8.0.427**, Forward+ Vulkan via **llvmpipe software rendering**. The project's existing 4.7 feature marker and Windows driver configuration remain unchanged; their target-machine validation remains pending.

- C# compilation passed through the SDK's MSBuild entry point, with no reported warnings/errors. This host intermittently fails the normal .NET CLI startup process-stat lookup; direct MSBuild bypassed that host issue. Package restore used official Godot-bundled NuGet packages; no host-specific NuGet config is committed.
- **34 windowed integration checks passed** in ValidateTrees.gd: tree generation, budget, cheaper far mesh, LOD/fade configuration, far-shadow default, wind bounds, matching near/far roots, chunk anchors and actual terrain root height, no sub-chunk rebuild, FPS display/timer/hide/reactivate, all four arrow directions, Ctrl/RMB mappings, mouse-only wake/release, duplicate aliases, **actual held RMB ascent / Ctrl descent / Up forward motion**, unchanged saved terrain, tree disable/reactivate.
- Existing grass/mist/sun integration runner: **23 checks passed**. Existing P pause/F8 preview and saved terrain continue working.
- Actual TestWorld software Vulkan views rendered without shader compilation errors. Spawn loaded 625 logical trees across 49 cached chunks; native distance ranges cull/fade visible batches, so that is not a visible-tree count. Near/far batches represent the same logical trees, not twice as many trees.
- Saved terrain, original demo/addon files, environment settings and grass resources remain unchanged. No rebake or authored-data rewrite occurs.
- Terrain3D's existing `instance_reset_physics_interpolation()` deprecation warning remains; addon sources are unchanged.

## Rendered inspection

[World view](world.png) · [Closer canopy and FPS overlay](close.png)

These captures use the actual TestWorld with a fixed 10:00 sun and a moved diagnostic camera. The runner does not save world/resources. The FPS value shown belongs to **software rendering**, not the owner's graphics card. Scene render counters at 640×360 were 417 draws / 8,167,598 reported primitives for the world view; close-up counters depend on camera position. These whole-scene counters are not grass-only or tree-only GPU timings.

More near foliage increases geometry compared with the sparse first preview. The reduced far mesh, optional shadows, range and leaf budget control that cost, but **no FPS improvement or target-GPU result is claimed**. Procedural untextured trees remain preview art; the imported-mesh replacement API is preserved.

## Reproduce locally

Use your Godot .NET executable path in place of `godot`; .NET 8+ SDK must be on PATH. Do not rerun terrain baking.

```sh
dotnet build
godot --headless --path . --script res://WORLD/Vegetation/Trees/Validation/ValidateTrees.gd
godot --path . --resolution 640x360 --script res://WORLD/Vegetation/Trees/Validation/ValidateTrees.gd
godot --headless --path . --script res://VEGETATION/Validation/ValidateGrass.gd
```

Headless mode deliberately skips OS-window-dependent movement/wake checks; run the windowed command for all 34 checks. The optional `CaptureTrees.gd` runner saves world/close PNGs to `user://TreeValidation`, or use `-- --output=/absolute/path` after its command to choose another output directory. Diagnostic runners are GDScript for direct --script execution; gameplay glue remains C#.

Open `WORLD/testworld.tscn`, F5/F6: inspect leaf shape and green colour, wind at zero/normal strength, crown layers near/far, LOD/fade transitions in mist **and temporarily clearer air**, and native tree shadows. Test arrows, Ctrl, RMB press/hold/release, simultaneous opposite/duplicate inputs, Esc release/recapture, alt-tab, existing aliases and sky controls. Hide FPS and restore via DebugMaster/ApplySettings. Relaunch to verify root placement and saved landforms.

Profile on the target GPU at real resolution: stationary, flight/boost and chunk crossings; near/far views; both LOD levels; high/low leaf budgets; shadows off/on; tree enabled → disabled → enabled. Record CPU/GPU frame time, memory, draw calls and hitches. Fades may overlap mesh levels temporarily and are Forward+-specific. Imported meshes retain their materials, use outer distance fading and are not automatically given procedural LOD/wind.

## Performance lifecycle

Tree creation/rebuild runs on world initialization, explicit settings changes or coalesced height changes. Explorer chunk crossings update the bounded cache; stationary/sub-chunk movement does not generate meshes or scan the world. Trees/leaves have no per-instance CPU callbacks. Active leaf wind, native range fading/culling, rendering and enabled shadows are intentional GPU/native work. Off removes both levels. FPS uses one slow visible-only Timer. Flight wakes from keys/buttons and stops on no input/capture loss/focus loss. Target-PC frame timings and user visual acceptance remain pending.
