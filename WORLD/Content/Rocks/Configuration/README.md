Edit RockMaster.tres for generated shape, colours, placement, slope, range and budget-friendly defaults. Runtime UI edits copies; source settings remain unchanged.

GameplayRockMaster.tres supplies active top-down/FPS rock placement and collision independently of parked RockMaster. CollisionEnabled defaults false in shared settings; active profile enables layer 1 hulls. Cliff dimensions live in WORLD/Configuration/WorldMaster.tres.

## R1 — Procedural silhouette refinement

Both `GameplayRockMaster.tres` (active World/FPS, static collision) and `RockMaster.tres` (legacy) now have:

- `RockCrownFlatness = 0.65`: makes slabs and outcrops more convincingly flat, reduces central cone peaks. Use 0 for older peaked crowns or 1 for maximum flattening.
- `RockAngularVariation = 0.55`: shifts shoulder facets and chips one corner on angular/broken variants; 0 disables new chip distortion, 1 increases angularity.

Settings are used only when `RockGenerator` builds its **nine reused meshes** at startup or on `ContentScatter.ApplySettings()`. Density/seed, per-instance scale/yaw, terrain/slope exclusion and 1-body-per-chunk collision stay the same. Triangle counts and material surfaces stay fixed. No new GLB art, shaders or per-rock processing. The shader's triplanar texture controls (`RockTextureScale`, `RockNormalStrength`, `RockMossAmount`, `RockWetness`) remain independent.

## R2 — Natural cover boulders in open fields

Active `GameplayRockMaster.tres` adds:

- `RockCoverEnabled = true`: build two additional shared silhouettes (11 total); legacy `RockMaster.tres` remains unchanged/disabled.
- `RockCoverChance = 0.22`: about **22% of accepted rock candidates**, selected with a position/seed hash after the ordinary RNG variant roll. This does not add a new RNG draw or move previously accepted placement points; each chosen cover rock replaces a normal candidate rather than creating extra rocks.
- `RockCoverHeightMultiplier = 1.0`: independent height adjustment on the two cover variants, range 0.7–1.6. Increase to 1.2 if full-body shelter is too low; reduces no other rock sizes.

Type 9 is a broad tall boulder, type 10 is a longer crouching-height outcrop. Existing density 0.35, spacing 12m, Terrain3D placement, slope checks, store/building/road exclusions, shared stone materials and cached static hull collision are preserved. The larger cover dimensions increase the **conservative exclusion margin** used near buildings/spawn areas, as intended to prevent large rocks clipping into critical sites. Chunk streaming and one static collision body per occupied chunk remain unchanged.

No new AI/LOS feature is created; cover effectiveness depends on existing physics rays hitting the larger solid convex hulls on collision layer 1. Rebuild/reload after tuning. Actual Godot render, compile and combat LOS checks pending.
