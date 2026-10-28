# PineMaster — forest source and distribution

The active world and inherited FPS test both use `Configuration/PineMaster.tres` via `PineSettings`. This master retains the original seed, 0.55 density, 20m spacing, 32m chunk streaming, slope/terrain filters, site/spawn exclusions, and 96m render distance.

## Reversible GLB comparison (new)

- `UsePineGlb = true` (default): forest batches use the **actual** `PineTree001.glb` mesh instead of generated procedural pines, with exactly the existing placement and terrain sampling. Toggle `false` then reload (or call `ApplySettings()`) to restore old procedural trees.
- `PineGlbScene = ExtResource("2")`: points at the canonical GLB artwork; do not instance the whole PineTree001 scene for every tree. Loader extracts one ArrayMesh and batches it into `MultiMeshInstance3D`.
- `PineGlbBaseScale = 1.0`: uniform imported-art size multiplier. Existing `MinimumScale/MaximumScale` continue seeded per tree; existing `HeightMultiplier` slider now multiplies GLB scale too. Native art approx. 14.4m tall.
- `PineGlbTrunkRadius = 0.45`: the source GLB's approximate base trunk radius, used for the same one-body-per-chunk trunk collision. `TrunkCollisionHeight` remains 4m at normal size.
- `UseGeneratedLod` and `FarLeafStride`: **procedural-only** for now. The imported scene uses the GLB's own importer-created mesh LOD data if present, without a dedicated second low-poly GLB. Rendering remains distance capped and subject to the existing WS1 policy.

One model shared by all trees; **no new per-tree scenes, scripts, update loops or spawning rules**. One shared imported mesh copy is created when rebuilding/initializing forest. The existing seed and variant RNG roll keep placement/size/rotation deterministic when switching art source.

**Performance warning:** PineTree001 is ~7,679 triangles across 5 material surfaces and has no custom distant GLB. It is substantially more expensive than a low-poly forest asset when hundreds are visible. Use the existing green/yellow/red FPS counter to compare modes at the same camera position. Runtime Godot import, C# build, visuals and performance are pending local validation.
