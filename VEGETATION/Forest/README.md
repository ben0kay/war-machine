# Dense canopy forest — prototype planning

**Status: PLAN ONLY — no forest scene, tree model, new configuration resource, generator, world placement or gameplay changes implemented.**

The dense-forest idea is an FPS-first woodland test patch for the shared `WORLD/World.tscn` and `WORLD/FirstPerson/FirstPersonWorld.tscn`, envisioned **east of the village, away from the robot's western patrol**. The visual reference is a tall, layered woodland: closely spaced, mostly straight trunks, a high interlocking leafy canopy, broken sunlight, distant silhouettes in haze and restrained ground clutter. The forest should feel walkable but enclosed and tense.

**Start here:** [FOREST_IMPLEMENTATION_PLAN.md](FOREST_IMPLEMENTATION_PLAN.md). It includes a phase table (F0–F5), provisional test-area coordinates, asset list, placement/collision rules, lighting, GPU/CPU budgets, source ownership, validation and approval boundaries.

## Existing foundation to reuse

- `../Trees/Pine/PinePlacement.cs` already owns event-driven **32m** chunk loading/eviction, terrain-aware seeded placement, `MultiMesh` batching, slope/scenery exclusions, per-chunk trunk collision, tree canopy fading and render ranges. Do not create one active Node3D script per tree or fork an entire streaming framework for this small test.
- `../Trees/Pine/Configuration/PineMaster.tres` currently authors **20m spacing, density 0.55**, and `UsePineGlb=false` (C# default may differ). Its built-in spacing is clamped to **8–40m**. This is **not** enough for the reference's denser layered forest; a future dedicated patch/spawner or carefully shared placement helper is required rather than globally densifying existing pines.
- `../Grass/TerrainGrass.cs` already supplies GPU grass, and `../../WORLD/Content/Vegetation/Bushes/` and `FallenLogs/` contain reusable decoration prototypes from legacy TestWorld; they are **not presently active-world forest floor spawns**.
- `../../WORLD/WorldController.cs` supplies shared terrain, player anchor, exclusions, vegetation and view composition. `../../WORLD/Rendering/` owns WS1 fog-aware render policy. Use the existing world, terrain and collision systems.

**Plan ownership:** Forest zone/policy lives here; individual tree art should live under `VEGETATION/Trees/<TreeType>/` with `C/`, `Artwork/`, `Configuration/` and a README per the current authoring conventions. Keep active-world integration with WORLD. No code/config files exist in this new folder yet.

**Next approval to request:** **F0** (actual location/footprint and feature switch). F1/F2/etc. each need separate authorization; do not assume this document approves them.
