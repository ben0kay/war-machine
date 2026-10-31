# ForestShrub001 — stylized woodland understory weeds

**Original, drop-in Godot 4 glTF mesh pack (asset-only; not automatically placed in War Machine).**

Six **upright wild shrub/weed** silhouettes inspired by the supplied example: fine branching dark olive-brown stems, broad pointed/faceted leaves, irregular leaf spacing, and occasional tiny cream flower umbels. Art is intended to sit between the existing `ForestGrass001` geometry and War Machine's stylized trees. This is **not a rounded hedge**.

## Install

1. Put the entire `ForestShrub001/` folder in **`res://ASSETS/Vegetation/Plants/`**. After copying, `res://ASSETS/Vegetation/Plants/ForestShrub001/Models/forest_shrub_001_a_LOD1.gltf` must exist.
2. Open Godot 4 .NET and let it import all `.gltf`, `.bin`, and `.png` files. **Do not detach `.bin` files or move the `Textures/` folder**; the glTF files reference them by relative paths.
3. Open `ForestShrub001_Preview.tscn` and press **F6** to inspect all six LOD1 variants, or drag an individual model from `Models/` into a scene. The preview scene does not modify the game world.
4. For future shared tint + wind when your C# vegetation placement system imports an `ArrayMesh`, override **surface 0** with `Materials/ForestShrub001Wind.tres` (load and duplicate once, not per shrub). Godot's existing `wm_foliage_tint` / `wm_foliage_tint_strength` global uniforms must be present, as they already are in War Machine.

## Models

| Variant | Appearance | LOD1 triangles | LOD2 triangles | Approximate height |
| --- | --- | ---: | ---: | ---: |
| `a` | Tall flowering | 564 | 150 | 1.52 m |
| `b` | Branching flowering | 766 | 172 | 1.31 m |
| `c` | Airy upright green | 362 | 104 | 1.11 m |
| `d` | Two-stem broadleaf | 604 | 208 | 1.35 m |
| `e` | Tall leafy, open crown | 604 | 188 | 1.61 m |
| `f` | Young smaller shoots | 566 | 158 | 0.87 m |

Each mesh has **one indexed triangle primitive / one material surface**, with double-sided **opaque geometry** (no alpha-blended grass cards). The leaves use genuinely shaped and folded geometry; stems are lightweight faceted tubes; white florets use tiny triangles. Models have a planted **Y=0 pivot**, **Y-up**, metre units, full UVs, authored normals, and RGBA vertex colours. `COLOR.a` is **only a semantic mask** to exempt stems/flowers from the shared foliage tint; it does not control rendering transparency (material is OPAQUE).

`LOD1` is the normal detailed model; `LOD2` uses fewer leaves, simpler stems, and cheaper flower heads. Both share the same resource names/material and approximate silhouette but are separately authored geometry. Variant positions are deterministic in the `Source/` generator; Godot placement/scattering should randomize scale and yaw independently.

## Palette, lighting and wind

- **Shared texture**: `Textures/forest_shrub_001_atlas.png`, 512×512 diffuse atlas, deliberately matched to the muted forest-green / olive family used by `ForestGrass001` (additional brown stem and ivory flower bands). The shader multiplies that source texture by vertex colours.
- **Native imported PBR**: the models already display with their own glTF double-sided, opaque, rough material—useful for standalone asset preview without any code change.
- **Optional Godot shader override**: `Materials/ForestShrub001Wind.tres`, using the same global shader tint uniforms as stylized grass. Light root-anchored GPU sway; no individual `Node3D` update loops. Only the **leaves** adopt the global foliage tint; bark/flowers remain independently coloured.
- Shader assumes the named project globals `wm_foliage_tint` and `wm_foliage_tint_strength` exist (true in the current War Machine branch). In a standalone project without these globals, use the imported PBR material or declare the globals first.

## Scattering and performance notes

- This pack contains **assets, not a new vegetation placement controller**. It does **not** change `VEGETATION/Grass/Stylized`, the active `World.tscn`, Terrain3D data, F1 controls, exclusion masks, the world seed or fog-linked range.
- For scatter, reuse your existing seeded Terrain3D height/slope sampling, road/building exclusions, 32 m streamed chunks, and **shared GV1 vegetation visibility policy**. Do **not** add per-shrub `_Process`, independent render distances, or individual collision by default.
- Batch instances as `MultiMeshInstance3D` per variant+LOD, with one shared mesh and one shared material per batch. The six variants are a visual palette, not a requirement to render all six in every chunk; tune scatter density against camera frame time and draw-call budget.
- Best first placement: scattered sparse-to-medium tall woodland plants in **small irregular pockets**, mixed with your current stylized grass; size jitter around 0.75–1.25×, varying rotations. Leave building footprints and roads clear.
- The LOD threshold and final draw range should be assigned through **War Machine's current `VEGETATION/Configuration/VegetationVisibilityMaster.tres`**, not buried in a new master. Native range crossfading and collision are **not installed by this asset pack**.

## Reproducibility and validation

`Source/generate_shrubs.py` (Python 3, `numpy`, `Pillow`) deterministically regenerates all meshes, atlases, source stats and the **software-projected** `Previews/ForestShrub001_Variants.png`. The PNG is a geometry-derived preview, not a Godot render or a screenshot of tested gameplay. `Source/mesh_stats.json` contains measured per-model vertex, triangle, bounding-box and height counts.

Source was reviewed for glTF buffer/index consistency and asset paths. **Godot import, shader compilation, visually correct normals from every camera direction, shadows and performance have not been validated in the game.** Inspect the prefab in Godot before any mass scattering and make any art revisions in the included source rather than altering grass or existing shaders.

**No gameplay or repository files were modified when this ZIP was produced.**
