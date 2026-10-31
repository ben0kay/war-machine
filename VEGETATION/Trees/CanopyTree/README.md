# CanopyTree — DF1 tall high-canopy broadleaf asset

**Status:** Procedural C# art and standalone preview **implemented in source — awaiting local Godot/C# and visual acceptance**. Does **not** populate the DF0 east-side site: DF2 will handle placement after DF0 location approval.

## Appearance

- Tall straight/slightly bent pale trunk with dark bark streak variations.
- Irregular visible high branches beginning around 8–11m and multiple rounded faceted foliage clusters rather than cone-shaped pine tiers.
- Three distinct seeded versions, **17–22m** tall, with broad uneven upper crowns and deliberate gaps for dappled sun once nearby tree crowns overlap.
- A **younger filler-tree example** reuses the reduced-detail tree mesh scaled to **65%** (roughly 11–14m tall), without another generator or runtime object behaviour.
- Two opaque vertex-colour material surfaces per tree (bark and canopy), **no alpha blend or leaf object nodes**. Standalone sample casts standard sun shadows; future DF2/DF3 can adjust selective canopy shadows and GPU wind.

## Files

- `CanopyTree.tscn`: reusable visual-only Node3D wrapper with `CanopyTreeVisual.cs` and the shared master.
- `CanopyTreePreview.tscn`: isolated F6 scene with light, simple ground, three near variants, a smaller filler tree and a farther low-detail comparison. This scene is NOT the active survival world.
- `C/CanopyTreeMeshBuilder.cs`: near/far deterministic geometry generator, 2 surfaces; call it once per variant for later MultiMesh batching.
- `CanopyTreeSettings.cs` and `Configuration/CanopyTreeMaster.tres`: edit height/branching/foliage/colour values.
- `Artwork/`: reserved for future original imported GLB/textures.

## Quick visual check

Build `dotnet build war-machine.csproj` locally, open `VEGETATION/Trees/CanopyTree/CanopyTreePreview.tscn` and **F6**. You should see three different high-crowned broadleaf trees with visible trunk from ground to high crown, plus a smaller low-detail tree at rear left and a fifth distant low-detail tree behind them. Change `CanopyTreeMaster.tres` then restart to compare. Also open `CanopyTree.tscn` in the editor; its `[Tool]` preview script generates one tree after C# is compiled.

Inspect in Godot from 2m, 15m and 60m for strong trunk/leaf silhouette and daylight gaps; verify dark scar accents do not resemble unrealistically striped tubes, crown is broad without looking like spheres, and no GPU shader errors. The preview only has a fixed look-at camera; use the Godot editor 3D viewport for alternate angles. If art feels too stylized, tune `FoliageSize`, `CrownClustersPerBranch` and `CrownRadius` before DF2.

**No collider in DF1:** the visual prefab uses **None** (CollisionMode 0 conceptual decision), because this tree is an art preview, not a standalone gameplay object. DF2 will use the existing pine strategy: one native `StaticBody3D` per loaded forest chunk, simple trunk capsules on layer 5, no branch/leaf physics. No forest density, terrain, environment, zombie sight or existing pine changes.

**Performance activation:** procedural mesh generation only on preview ready/explicit preview reload; then the tree has zero `_Process`/`_PhysicsProcess` work, and the mesh is static. DF2 must generate/cache meshes once per variant rather than instancing `CanopyTree.tscn` hundreds of times. Current triangles/draw calls/FPS have **not** been measured. [DF plan](../../Forest/FOREST_IMPLEMENTATION_PLAN.md).

## DF2 forest mesh consumption (2026-10-29)

`VEGETATION/Forest/ForestPlacement.cs` generates the three DF1 near/far mesh pairs once per initialization, and reuses the simplified meshes at reduced scale for younger fillers. Chunk-owned MultiMeshes display the trees; `CanopyTree.tscn` is **not instantiated hundreds of times**. Bark/foliage keep two surfaces; a shared forest foliage shader provides top-down camera fade. Native trunk collision belongs to `ForestPlacement`, not this art prefab. Existing standalone DF1 preview remains unchanged. World placement is gated by ForestMaster.Enabled and DF0 survey acceptance.

**DF2 visual palette recovery (2026-10-30):** a screenshot of white trees prompted an isolated runtime material correction under `VEGETATION/Forest/Shaders`. Existing DF1 geometry and its vertex colour authoring remain unchanged. DF2 MultiMeshes now declare a white per-instance colour so vertex data is available; leaf and bark ShaderMaterials fall back to `CanopyTreeMaster.tres` colours if the renderer supplies white. The standalone `CanopyTreePreview.tscn` is unaffected. Confirm in FPS/World locally; this did not involve a model regeneration.

**DF2 white palette recovery (2026-10-31):** `CanopyTreeMeshBuilder.Create` now explicitly binds bark/leaf materials to the two ArrayMesh surfaces after SurfaceTool commit. Existing standalone `CanopyTreePreview.tscn` retains its authored vertex-colour shaders/materials. The shared DF2 world now defaults to a **safe native Godot StandardMaterial3D palette** for leaves/bark while we validate a previously white world result. No change to generated tree geometry or variant/LOD seeds. A `UseSafePaletteMaterials=false` master switch restores experimental custom shader mode; see [forest README](../../Forest/README.md).

## DF2.5 — subtle leaf-edge detail (2026-10-31)

The established round/faceted canopy masses are intentionally **preserved** to match War Machine's low-poly building language. Near-only `CanopyTreeMeshBuilder.AddCrownClump` adds just three short pointed outer-edge facets to each existing eight-sided clump; no extra clumps, leaf-card materials, alpha, scripts, nodes or shader dependencies. `CanopyTreeMaster.tres → LeafEdgeDetail=0.12` controls the slight leaf-like silhouette; **0** reverts exactly to the old clump mesh. Distant LODs and the young trees using far art are unchanged. The current safe native foliage/bark palette remains default to avoid the white-tree regression. Source implemented; Godot silhouette and FPS budget acceptance pending.
