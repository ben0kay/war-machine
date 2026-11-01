# ForestGrass001 — UV-corrected, geometry-optimized stylized grass

**Active family:** three A/B/C variants, two authored LODs each, one opaque mesh surface. Runtime instancing is owned by `VEGETATION/Grass/Stylized/StylizedGrassPlacement.cs`.

## Dark-root/light-tip UV correction retained

These are the existing UV-corrected meshes, **not** the original reversed-gradient meshes. Each blade's base samples the dark atlas end (UV V≈0.08) and tip samples a lighter region (V≈0.95). The optimization does not modify UVs, vertex colours, atlas or shader.

## 2026-11-01 — reduce triangle count 16–20% without deleting blades

The previous UV-corrected models use five triangles per blade, consisting of two lower quads and one tapered pointed tip triangle. The least-curved blades were selectively simplified to three triangles by removing their first intermediate bend cross-section. **Every blade, original root and tip, overall bounds, texture UVs and colours are retained.** A small amount of lower-blade curvature is approximated by a flatter surface.

| Model | Before | After | Reduction | Blades retained |
| --- | ---: | ---: | ---: | ---: |
| A LOD1 | 75 | 61 | 18.67% | 15 |
| B LOD1 | 80 | 64 | 20% | 16 |
| C LOD1 | 75 | 61 | 18.67% | 15 |
| A LOD2 | 20 | 16 | 20% | 4 |
| B LOD2 | 25 | 21 | 16% | 5 |
| C LOD2 | 25 | 21 | 16% | 5 |

The exact triangle counts and selected blade indices are in `Source/mesh_optimization_stats.json`. The external downloadable `ForestGrass001_Optimized_15to20pct_DropIn.zip` also includes a standalone Python optimization script and before/after model previews. Do not run that optimizer a second time on meshes that are already optimized.

## Integration and validation

Only the six existing `Models/forest_grass_001_{a,b,c}_LOD{1,2}.{gltf,bin}` pairs were updated. Existing `Textures/`, `Materials/`, `StylizedGrass.gdshader`, global foliage tint, wind, deterministic 32m MultiMesh streaming, Terrain3D exclusions and GV1 fog-linked visibility/LOD remain unchanged. Billboard grass and `ForestGrass002` are separate families and untouched.

After pulling the active branch, allow Godot to reimport the six model files, then F6 `WORLD/FirstPerson/FirstPersonWorld.tscn`. Isolate `ForestGrass001` with F1 by temporarily disabling `ForestGrass002` and billboard filler. Inspect shape, tip/root colours, wind, LOD and F2 frame times/TRIS. Offline geometry checks passed (triangle count, indices, normals, root/tip UV preservation and exact bounds); **Godot visual/runtime validation and actual FPS gains are pending**. Reduced triangles do not guarantee proportional FPS improvement.
