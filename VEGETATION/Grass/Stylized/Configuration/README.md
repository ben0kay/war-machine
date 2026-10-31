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
