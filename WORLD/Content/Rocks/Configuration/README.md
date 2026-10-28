Edit RockMaster.tres for generated shape, colours, placement, slope, range and budget-friendly defaults. Runtime UI edits copies; source settings remain unchanged.

GameplayRockMaster.tres supplies active top-down/FPS rock placement and collision independently of parked RockMaster. CollisionEnabled defaults false in shared settings; active profile enables layer 1 hulls. Cliff dimensions live in WORLD/Configuration/WorldMaster.tres.

## R1 — Procedural silhouette refinement

Both `GameplayRockMaster.tres` (active World/FPS, static collision) and `RockMaster.tres` (legacy) now have:

- `RockCrownFlatness = 0.65`: makes slabs and outcrops more convincingly flat, reduces central cone peaks. Use 0 for older peaked crowns or 1 for maximum flattening.
- `RockAngularVariation = 0.55`: shifts shoulder facets and chips one corner on angular/broken variants; 0 disables new chip distortion, 1 increases angularity.

Settings are used only when `RockGenerator` builds its **nine reused meshes** at startup or on `ContentScatter.ApplySettings()`. Density/seed, per-instance scale/yaw, terrain/slope exclusion and 1-body-per-chunk collision stay the same. Triangle counts and material surfaces stay fixed. No new GLB art, shaders or per-rock processing. The shader's triplanar texture controls (`RockTextureScale`, `RockNormalStrength`, `RockMossAmount`, `RockWetness`) remain independent.
