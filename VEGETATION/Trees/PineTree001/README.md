# PineTree001 — stylized faceted conifer (comparison asset)

**Status:** New independent art family, *not yet replacing* the existing PineTree002 forest. Source design references the user's PineTree002 models and the stylized, low-/mid-poly forest concept approved 2026-10-30. The generated meshes are new geometry with a similar planted origin and approximately 22.45 m height; they are **not a decimation or texture conversion** of the original tree.

## Look and geometry

- Tall single trunk with a slightly bent, tapering faceted profile; broad, tiered pine-like branch sprays.
- Opaque, faceted foliage masses with modest gaps, surface-colour variation and no photographic needle atlases/alpha cutouts.
- Vertex colours provide the source greens and brown bark. No PNG texture files are needed. Texture style stays coherent with low-/mid-poly forest-floor assets.
- Two material surfaces in each model: **0 bark, 1 foliage**. Stable glTF/.bin naming and matching roots across all LODs.

| LOD | Trunk tris | Foliage tris | Total tris | Intended use |
| --- | ---: | ---: | ---: | --- |
| LOD1 | 306 | 4928 | **5234** | near-field full tiered canopy |
| LOD2 | 154 | 1200 | **1354** | simplified and wider canopy forms |
| LOD3 | 50 | 96 | **146** | distant simplified silhouette; no shadows recommended |

LODs deliberately differ in fine branch density. Their silhouette is not guaranteed identical at a hard switch; use the same batch ranges/hysteresis as PineTree002 when testing. Geometry is opaque, but **this is not an FPS benchmark**. A denser tree can still cost overdraw, depth and shadow time.

## Import and preview

1. Copy this entire folder to `ASSETS/Vegetation/Trees/PineTree001/` in your Godot project; the ZIP is project-root relative.
2. Open Godot and allow the three glTF assets to import.
3. Open `PineTree001_Preview.tscn` (editor or F6). It creates **only three preview trees** and binds bark/foliage shaders once on load, with LOD1/LOD2/LOD3 side by side.
4. The preview script is NOT intended for every live forest tree. Use `MultiMesh` with the prepared shared foliage material, just as in `PineForest.cs`.

The preview PNGs are simple software projections of the generated geometry, not screenshots from Godot. The exported `.gltf` files can also be imported into Blender for visual edits; Blender was unavailable for creating a native `.blend` source. `Source/generate_models.py` is the deterministic Python geometry authoring script (requires `numpy` and `Pillow`), supplied for reproducibility.

## Shared foliage tint in Final Machine

`Materials/PineTree001Foliage.gdshader` is a **Godot spatial shader** using mesh vertex colours, with `forest_tint`, `forest_tint_strength` and `palette_response`. It deliberately uses the same parameter names and colour-preserving blend as PineTree002 and grass. Bark material does NOT react to foliage tint.

**Existing shared control:** `WORLD/Vegetation/ForestFoliagePalette.cs` already updates all shared resources from the F1 panel on parameter changes. When integrating this asset in the *main forest*, add one entry to its `MaterialPaths` array:

```csharp
"res://ASSETS/Vegetation/Trees/PineTree001/Materials/PineTree001Foliage.tres",
```

Then use this `ShaderMaterial` on **surface 1** of the imported PineTree001 models (as done by `PineForest.PrepareNeedleMaterial`). `PineTree001PreviewVisual.gd` demonstrates binding both materials. No extra Godot global shader parameter, per-tree process function, or new palette system is needed. The global **effect** comes from the shared F1 palette updating shared material resources; it is not the Godot Project Settings Shader Globals feature.

**Do not replace PineTree002 in the existing world until visually accepted.** If the owner chooses to switch later, add a single configurable model/material selector in `PineForest` and retain its 70/280/600m switching, batching, collision, shadow and pine placements. This asset pack does not change `PineForest.cs` or test.tscn.

## Validation

- Confirmed glTF+bin byte lengths, source attribute counts, face winding, normals, triangle counts, surface order and vertex-colour ranges using the authored generator.
- Source normals are geometric flat-face normals; no reversed triangles or zero-area faces were intended. Software preview checks silhouettes, not Godot material/culling behaviour.
- **Not tested in Godot:** actual render, material uniform palette changes, light/front/back behaviour, imported glTF surface colours, shadowing, LOD transitions or FPS. Verify before changing the main forest.
