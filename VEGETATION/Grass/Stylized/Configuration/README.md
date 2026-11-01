# STG1 stylized patch controls

`StylizedGrassMaster.tres` (script `StylizedGrassSettings.cs`) is independent of the legacy GPU ribbon `VegetationConfig`. Enabled by default, it controls deterministic patch/group distribution in streamed 32m chunks.

| Group | Authored defaults | Meaning |
| --- | --- | --- |
| Scatter | Seed=49117, PatchesPerChunk=6, PatchChance=0.82, ClumpsPerPatch=12 | Candidate density. Change seed to relocate patches; reload to apply. |
| Patch radius | 1.7–3.6m | Random disk around each patch centre. |
| Clump scale | 0.75–1.15 | Uniform size multiplier on the original ~0.8m grass mesh. |
| Environment | MinimumNormalY=0.87, EdgeClearance=0.9 | Reject steep terrain; skip any patch root near authored roads/buildings. |
| GPU wind | WindMultiplier=0.06, WindSpeed=1.1 | Initial shared Environment wind direction/strength scaled for short grass blades. |
| Streaming | ChunkSize=32m | Two GLTF detail levels; both distances and last-visible cutoff are from the single GV1 fog-linked visibility master. |

To disable new patches, set Enabled=false. Old grass remains OFF in both old masters until explicitly re-enabled. [Full handoff](../README.md). Grass and stylized pine both read `wm_foliage_tint` and `wm_foliage_tint_strength` from **Project Settings → Shader Globals**, not this patch master.

## STG2 — denser, taller, farther patches (2026-10-30)

| Parameter | Before | Current |
| --- | ---: | ---: |
| Patches per 32m chunk | 6 | **8** |
| Patch chance | 0.82 | **0.90** |
| Clumps per patch | 12 | **15** |
| Patch radius | 1.7–3.6 m | **2.1–4.7 m** |
| Individual size | 0.75–1.15 | **0.85–1.50** |
| LOD change | 28 m | **GV1 shared fraction (35% of fog-linked range)** |
| Render range | 64 m | **GV1 shared fog-driven distance** |

More scattered clumps with slight taller variation and longer-distance coverage. No changes to the saved mesh files, seeded world/chunk layout method, slope/road/structure exclusion logic or native MultiMesh architecture. The exclusion margin scales with clump size, so bigger plants retain extra roadside clearance. These new defaults increase render cost; visual/FPS acceptance required.

Runtime testing is now easier: expand **F1 → Foliage → Grass** for actual imported-grass patch density, clumps, max spread, max size and wind. Sliders regenerate only on drag release. **F1 → Foliage → Shared tree + grass tint** now has a live colour picker and strength slider (0 original, 1 strongest); both the imported grass and stylized pines, plus DF2 canopy, read these same `wm_foliage_*` global uniforms. Changes are not saved to master files. Keep permanent defaults in this resource and Godot → Project Settings → Shader Globals.

## STG3B — billboard patch controls (current, 2026-10-31)

`BillboardGrassMaster.tres` controls the independent A/B/C six-triangle billboard layer. Its **current** algorithm is seeded large patches, **not** STG3's jittered grid: 9 attempts/chunk, 90% probability, 28–64 billboards per patch, variable 2.2–7.2m radius, irregular rotated ellipse and patch edge, scales 0.60–1.55× with slight per-patch bias, plus an extra 0–5° lean on ~35% of instances. These are attempts, not accepted instance counts; steep terrain and road/building exclusions still reject roots. All tuning is in the master, and ON/OFF does not affect the original stylized clumps. The removed STG3 `CellSize`/`Coverage` fields are no longer used. Shared GPU batching, seed-stable 32m streaming, wind/tint and the GV1 global fog envelope remain unchanged. Awaiting user visual/FPS acceptance.

## STG3C — F1 billboard grass live preview (2026-10-31)

Open **F1 → Foliage → Billboard grass** to tune enabled state, patch attempts/chance, number of billboards per patch, both radius and scale endpoints, extra tilt chance/angle and billboard wind. Patch changes apply on slider release (one `ApplySettings` rebuild); wind changes on input. This preview mutates only the current runtime copy. **Copy settings** exports matching `BillboardGrass*` values; paste the desired numbers into `BillboardGrassMaster.tres` to persist them. The original stylized grass has its own separate controls, and all vegetation visibility stays linked to the global fog range. Local Godot/visual/FPS verification pending.

## Saved user-tuned grass presets (2026-10-31)

The active `StylizedGrassMaster.tres` now defaults to Enabled=true, **12 patches per 32m chunk, 30 clumps per patch, maximum patch radius 4.7m and maximum scale 2.3×**; all other stylized fields keep their earlier values (including the original seed, 0.9 chance and 0.85 minimum scale). The independent `BillboardGrassMaster.tres` now defaults to Enabled=true, **7 patch attempts/chunk, chance 0.95, 49–82 billboards/patch, 3.1–7.2m patch radius, 1.05–1.65× scale, extra tilt chance 0.6, maximum tilt 8°, and wind multiplier 0.2**. These values are the owner's F1 snapshot, now persisted into the actual runtime-loaded masters. The existing shared exclusion rules, chunk streaming, LOD/fog visibility and original art remain unchanged. This increases geometry and potentially alpha overdraw relative to earlier defaults, especially with the large original stylized scale; measure FPS locally. Source preset only, awaiting local Godot/render performance check.


## STG7 — Forest Grass 002 dense beds (2026-10-31)

`ForestGrass002Master.tres` enables the uploaded compact 002 A/B/C two-LOD meshes as the **default short grass bed layer**. Its separate seed, patch count, 70–115 compact tufts per bed, 3.5–6.5m radius and 0.85–1.25× scale produce irregular large bed-like clusters with close roots (no minimum intra-bed spacing). They are not cross-card billboards. The shared 32m streamed MultiMesh controller keeps terrain slopes, road/building exclusions, shared GPU wind, foliage tint and GV1 fog-based LOD. These defaults intentionally bound near-camera geometry; inspect FPS/TRIS when tuning tufts upward.

`BillboardGrassMaster.tres` is now **Enabled=false**, but the old crossed-card art and F1 settings are preserved for comparison. `StylizedGrassMaster.tres` (Forest Grass 001) and the shrub/fern shares in `GroundCoverMaster.tres` are unchanged. `GroundCoverMaster.tres` references both independent families, so turning billboard ON later is opt-in rather than replacing 002. F1 → Foliage → Forest Grass 002 · short beds offers live, deferred density/radius/scale and wind tuning; copy values into `ForestGrass002Master.tres` to persist. Changing a setting rebuilds the same existing chunks only on demand; no extra per-frame system. Local build/visual acceptance pending.


## DTP2 — Terrain3D soil-aware undergrowth (2026-11-01)

**Source implemented, local Godot/.NET and FPS validation pending.** A new `SoilResponseMaster.tres` lives in the existing `GroundCoverMaster.tres → SoilResponse` branch, reachable from `FoliageMaster.tres → GroundCover`. Enabled by default, it contains separate grass-family sensitivity controls: ForestGrass001 **0.88**, ForestGrass002 **0.90**, optional billboard **0.94**, independent seed **184213** and dirt-edge exponent **1.5**. Each existing `GroundCoverPlantSettings` controls its own `SoilSensitivity`: ForestShrub001 **0.72**, ForestFern001 **0.80**, ForestFern002 **0.80**. At completely painted dirt these retain approximately **12%, 10%, 6%, 28%, and 20%** of otherwise valid candidates respectively. Intermediate dirt blends thin plants progressively instead of imposing a bare/no-grass mask.

`WORLD/Terrain/SoilCoverageSampler.cs` is a **read-only** sampler of saved Terrain3D 1.0.2 native R32 control-map bits: auto/hole flags, base/overlay texture IDs and the 8-bit material blend. It caches the byte array once per encountered native terrain region, so hot-path samples are simple in-memory reads, not Godot API calls per candidate. Missing regions and unsupported control-map formats fail open (no thinning). The visual GPU height-aware texture blend can differ a little from the raw 8-bit paint weight. Terrain3D `control_maps_changed` and `height_maps_changed` invalidate the cache and trigger the existing grass refresh; efficient *targeted* dirty-chunk refresh remains DTP3.

Both normal and FG0/PERF1 incremental grass paths share the same source candidate iterators. `CreatePatchClumps` filters ForestGrass001 plus selected shrub/fern species; `CreateDensePatches` filters ForestGrass002 or opt-in billboards. Each candidate is tested with a separate position-hash **after** all prior RNG draws, preserving the original placement positions when DTP2 is disabled. There is no additional world generator, FoliageManager, MultiMesh batch owner, scheduler, per-frame loop or terrain modification.

**F1 comparison:** Foliage → `DTP2 · soil-aware ground cover` toggles the whole response and previews per-grass sensitivity/edge exponent; each plant's own F1 subsection exposes its soil sensitivity. F1 settings are runtime only, and Copy settings exports the DTP2 values. Pull and rebuild C#, run FPS scene F6 (no DTP1 rebake needed if V3 is active), inspect exposed soil at X492/Z542, toggle DTP2 ON/OFF and walk several 32m chunk boundaries. Check F2 `Grass.BuildStep` timing, LODs, slope/road clearance, grass+fern variety and frame pacing. Original saved Terrain3D source and V3 files remain unchanged.
