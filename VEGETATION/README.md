# VEGETATION — active-world grass

The existing TerrainGrass renderer is now integrated into **WORLD/World.tscn**. Shared VegetationMaster defaults are tuned for the overhead view; no separate FPS grass variant is preserved. Legacy compatibility remains available through the existing explorer initialization method.

## Current defaults

Medium quality, density **0.65**, hard budget **120,000 slots**, **20 FPS** particle simulation, **2 ribbon sections** (previously the mesh default of 5), shadows off. Height **0.45**, clump boost **0.8**, blade width **0.16**, wind multiplier **0.4**, minimum radius **64m**, **32m tiles**. Colour and terrain/noise-shaped patches retain the previous style. At default 48m view size/55° pitch on a 16:9 viewport, coverage rounds to 80m and allocates 117,649 slots versus the old 451,584; these are calculated allocations, not measured FPS gains. Blade geometry has fewer subdivisions and the ribbon shader drops the fine jitter/nonlinear bend work while retaining broad GPU wind sway.

World supplies explicit terrain, player anchor and active camera references. Player tile crossings recycle entering rows/columns; there is no C# per-frame grass callback. Projection/zoom/viewport changes estimate coverage in 16m steps, capped at **128m**, and rebuild only when that effective radius changes. Allocation never exceeds the configured budget. At extreme zoom/low angles, the finite grass boundary can enter view. Off/zero density frees emitters and stops their simulation.

Grass fades in horizontal ground distance around the camera's focus point rather than its elevated position, so orthographic height/perspective distance do not hide all nearby grass. Focus distance is an input-driven uniform; shader camera matrices handle follow motion. Terrain bindings and world-height AABBs still update only after terrain changes, and all subscriptions disconnect on unload.

**FilterPaintedRock=false** is intentional: the simple gameplay bake has default control maps (texture index 0), so the former unconditional rock-index filter would hide the entire field. Slope, holes and absent-region rejection remain. Enable FilterPaintedRock in the master if a later painted landscape needs that convention.

## In-game tuning

DEBUG/WorldTuning now includes grass enabled, density, height and wind multiplier. **Density and height apply when releasing the slider** to avoid rebuilding/restarting a particle pool on every drag event; mouse-wheel edits on these two controls are disabled. Shared wind strength/heading affect grass and weather together. Copy settings includes the current grass values. Runtime edits use a master copy and reset on scene restart. No terrain rebake needed.

## Validation status

Source/settings/shader-uniform and initialization/lifecycle review, resource paths and git whitespace checks only. **Godot/.NET unavailable here; shader compilation, rendering and performance are unverified.** Locally build and F6 WORLD/World.tscn; confirm terrain-following green patches, short readable blades, wind, walking/tile crossings, orthographic/perspective zoom, OFF/zero/reactivation, and copied grass values. Appearance/FPS acceptance stays with the owner.

---

The following notes describe the previous first-person tuning and are retained for reference; current defaults and run instructions above supersede them.

# VEGETATION — GPU ribbon grass

`Grass/TerrainGrass.cs` adapts Terrain3D's included MIT particle grass into the actual `WORLD/testworld.tscn`. It uses the same ribbon mesh, noise-shaped clumps/patches, randomized spacing/orientation/height, terrain colour map, slope filtering, tip pinching and wind bending. World-owned shader copies leave the addon/demo untouched.

The old baked crossed-card mesh asset is **disabled**, not removed from saved region data. No rebake is required. Native terrain height/control/colour maps and authored grass data remain byte-for-byte unchanged; the new decorative GPU placement follows those maps and rejects holes, absent regions and painted cliff texture 0.

## Data-driven controls

- `Configuration/VegetationMaster.tres`: enabled, density, Low/Medium/High quality, hard particle budget, simulation FPS, shadows, render radius, tile width, fade start, minimum terrain-normal Y, blade height/width, clump/patch variation, colour and seed.
- `../ENVIRONMENT/Configuration/EnvironmentMaster.tres`: shared wind direction, grass wind strength/speed, plus Sky3D mist. Wind speed now uses the demo's noise-UV units: **0.025**, rather than the old sine-sway value 1.4. Strength defaults to the demo's 1.0.
- Density=1 / High uses the demo's 0.25m spacing; Medium multiplies density by 0.6, Low by 0.25 (and caps simulation at 20 FPS). Increasing radius/density cannot exceed the configured particle budget: spacing increases to fit it. Defaults: 60m fade radius, 24m tiles, 30 FPS simulation, shadows off, 49 tiles / 451,584 allocated particle slots. Filtered blades are not drawn, but slots still have GPU cost.
- Default **Height=0.6** scales the demo's original height distribution so tall noise clumps fit the exploration camera better. Height=1 restores its original scale. BladeColour stores the demo's linear RGB values; avoid adding a source-colour conversion to the shader uniform.

Edit master resources in the Inspector and restart the scene. For explicit runtime changes call `Vegetation.ApplySettings()`; there is no config polling. Enabled=false or Density=0 removes emitters and all grass GPU simulation; call ApplySettings again to reactivate. Solar pause intentionally does not stop grass/cloud wind.

## Lifecycle / performance

World initializes grass after selecting saved terrain and placing the explorer. Explorer transform signals are checked against the current tile coordinate: no callbacks run while the camera is stationary. Inside the same tile there are no grass uniform writes or emitter restarts. Crossing a tile boundary recycles only incoming rows/columns (seven tiles for one axis crossing with defaults); teleports wrap the same fixed pool. GPU ribbon fading continuously follows the actual camera, avoiding per-metre restarts.

Terrain map signals coalesce into one deferred binding upload/restart. Height/control/colour RIDs, region map and AABBs are **not** rewritten per frame. Bounds include terrain height range and configured blade/wind margin; normal Godot frustum culling applies. Zero wind skips the GPU wind-noise lookup; active grass still retains particle/rendering cost. Off/zero density has no emitters. Child resources and subscriptions clean up on world unload. This is localized decorative rendering, not persistent world streaming or vegetation gameplay.

See [validation results](Validation/README.md), including before/after/reference captures and the target-PC checks still required. All project gameplay glue is C#; the optional GDScript validation harness is diagnostic only.

## Pine integration

Pine trees are now integrated under Trees/Pine/. Mesh code lives in C/, future models in Artwork/, placement/settings in the pine root. Shared defaults are tuned for overhead rendering, with enabled/density/size/wind controls.

## Dense canopy forest proposal (DF0–DF5; planning only, 2026-10-29)

[VEGETATION/Forest/FOREST_IMPLEMENTATION_PLAN.md](Forest/FOREST_IMPLEMENTATION_PLAN.md) is the implementation-ready plan for a **single 96 × 96m, FPS-first tall high-canopy woodland prototype** east of the village, opposite the robot's western patrol. Exact location is provisional and needs terrain/clearance survey. Reuse the existing pine chunk/MultiMesh/terrain/exclusion/physics/range architecture; do not simply set global pine density higher or instantiate per-tree scenes. Proposed separate phases cover location, broadleaf art, dense placement, real canopy shade, floor clutter and profiling. **Docs only; no new forest generator, model, resource or scene added.**

**DF0 source implemented, awaiting local verification (2026-10-29):** See [Forest/README.md](Forest/README.md) and [DF0 handoff](Forest/FOREST_IMPLEMENTATION_PLAN.md#df0-implementation-and-validation-handoff-2026-10-29). The disabled-by-default forest zone master provides a reversible east-side 96m-square candidate, startup slope/terrain/approach/reservation checks, optional static debug outline and an approved-only exclusion rectangle for the **existing** pine system. No new trees or changes to grass/rock/lighting.

**DF1 isolated art source (2026-10-29):** [CanopyTree prototype](Trees/CanopyTree/README.md) includes `CanopyTree.tscn`, an F6 preview of 3 seeded near variants, one small filler and one distant LOD and its appearance master. Preview-only, no world placement/collision/wind system or changes to pines, grass or the DF0 site. Runtime Godot validation pending.

**DF2 implemented in source (2026-10-29):** [Forest/README.md](Forest/README.md) now documents seeded dense canopy trees streamed in 32m MultiMesh chunks with trunk capsules, near/far artwork and event-driven top-down crown fading. DF0 approval + ForestMaster.Enabled controls activation; defaults OFF. The normal pines and rocks return when disabled. No new per-tree simulation and no global terrain or lighting changes. Godot visual/FPS acceptance remains pending.

**STG1 — GPU grass replaced by imported mesh patches (2026-10-30):** [Grass/Stylized](Grass/Stylized/README.md) is now enabled for both shared World and inherited FPS. Six uploaded `ForestGrass001` glTF LOD meshes are batched into irregular seeded patches, with **actual village road/building rectangles passed as exclusions**, no grass colliders or per-clump processing. Both old TerrainGrass masters are OFF (zero GPU particles); fallback remains. Godot Shader Globals now hold a common `wm_foliage_tint` + `wm_foliage_tint_strength` read by grass, imported pines and DF2 canopy foliage. Build/visual/FPS tests pending.
