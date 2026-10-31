# STG1 stylized patch controls

`StylizedGrassMaster.tres` (script `StylizedGrassSettings.cs`) is independent of the legacy GPU ribbon `VegetationConfig`. Enabled by default, it controls deterministic patch/group distribution in streamed 32m chunks.

| Group | Authored defaults | Meaning |
| --- | --- | --- |
| Scatter | Seed=49117, PatchesPerChunk=6, PatchChance=0.82, ClumpsPerPatch=12 | Candidate density. Change seed to relocate patches; reload to apply. |
| Patch radius | 1.7–3.6m | Random disk around each patch centre. |
| Clump scale | 0.75–1.15 | Uniform size multiplier on the original ~0.8m grass mesh. |
| Environment | MinimumNormalY=0.87, EdgeClearance=0.9 | Reject steep terrain; skip any patch root near authored roads/buildings. |
| GPU wind | WindMultiplier=0.06, WindSpeed=1.1 | Initial shared Environment wind direction/strength scaled for short grass blades. |
| Streaming | ChunkSize=32m, LOD=28m, RenderDistance=64m | Two imported GLTF detail levels, chunk-owned GPU MultiMeshes. |

To disable new patches, set Enabled=false. Old grass remains OFF in both old masters until explicitly re-enabled. [Full handoff](../README.md). Grass and stylized pine both read `wm_foliage_tint` and `wm_foliage_tint_strength` from **Project Settings → Shader Globals**, not this patch master.
