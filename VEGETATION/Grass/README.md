# Terrain-following GPU grass

- `TerrainGrass.cs`: event-driven tile pool, terrain signal subscriptions, one-shot configuration/binding, shutdown.
- `GrassPlacement.gdshader` / `PlacementMaterial.tres`: adaptation of Terrain3D's particle placement/wind-noise shader and cellular noise setup. Guards missing regions, safely rejects holes/steep ground/painted rock. No invalid texture-array layer fetches or NaN rejection geometry.
- `GrassRibbon.gdshader` / `RibbonMaterial.tres`: adaptation of the demo's organic ribbon shape, colour variation and wind bending, with continuous camera-relative grow/shrink distance fade and width compensation.

Both shaders retain Cory Petkovsek, Roope Palmroos and Contributors' copyright. Source and MIT license remain at `addons/terrain_3d/extras/particle_example/` and `addons/terrain_3d/LICENSE.txt`. Original files are unchanged. One shared material pair and ribbon mesh serve each controller's entire fixed tile pool; no per-blade C# objects or processing.

## Grass scenery exclusion

Shared WorldController now places scenery before initializing grass and supplies tighter world-XZ rectangles from actual generated prefab bounds, plus a 0.3m edge buffer. All three building interiors, porch/apron/forecourt areas and the car envelope reject grass in both top-down and inherited FPS views. Tree clearings retain their larger approach margins. Rectangular envelopes are conservative around irregular outlines; terrain painting/saves are unchanged and no rebake is needed.

TerrainGrass.SetExclusionAreas caches up to 64 rectangles and uploads the mask on initialization, explicit layout changes or grass rebuilds. GrassPlacement rejects randomized roots inside those rectangles before terrain sampling. Streamed tiles and quality/OFF/ON rebuilds reuse the cached mask; an explicit layout update restarts existing tiles once. No per-blade physics queries, CPU polling or new frame callbacks. GPU particle slots remain budgeted even when rejected.

Validation: source review of mask packing/comparisons, initialization ordering, rebuild retention and whitespace checks. Godot/.NET unavailable here; shader compilation and in-game checks remain pending. Build then F6 either view, check station pavement/interiors and grass outside, toggle/rebuild grass in top-down and walk away/back to stream tiles. Changes to scenery transforms after startup need an explicit mask refresh; current props are stationary.

## Particle compiler correction

The exclusion-mask change used an early return inside particle start(), which Godot rejects. It now records an excluded flag, marks that slot inactive and gates terrain sampling/initialization behind ACTIVE, without a processor return. Exclusions and OFF/rebuild behavior are retained. The screenshot's null pipeline / supplied (32) push-constant errors are downstream of the failed grass shader compilation, not object counts. Source/whitespace checks passed; local shader compilation/runtime remains pending. Restart the running scene after rebuilding/reloading the shader and clear old debugger entries to verify the cascade stops.

## STG1 — current imported stylized grass (2026-10-30)

**Active system:** [Stylized/README.md](Stylized/README.md) now documents the replacement: `StylizedGrassPlacement.cs` streams sparse seeded clusters of `ASSETS/Vegetation/Grass/ForestGrass001` (three authored model variants, near/far LODs) in 32m MultiMesh chunks, sharing existing World/Village road and structure exclusions with edge clearance. The **older GPUParticles3D TerrainGrass** is preserved for fallback but `Enabled=false` in both FPS and top-down authoring resources, so it constructs **zero GPU particle emitters** at scene start. F1's old grass controls still address the disabled legacy system, not the new patch master; avoid turning old grass back ON for normal comparisons. Global foliage palette is now shared across the imported grass, stylized pine and DF2 canopy shaders. Source integrated, Godot build/visual/performance acceptance pending.
