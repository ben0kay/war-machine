# ForestFern001 — stylized woodland ferns for War Machine

**Standalone art pack; not yet attached to a world spawner.** All nine glTF mesh assets are original procedural geometry based on the silhouette of arching woodland ferns, styled to complement `ForestGrass001` and the thinner-leaf `ForestShrub001`. No existing War Machine files are replaced.

## Install

Copy the **`ForestFern001`** directory (the root of this ZIP) to:

`<Godot project>/ASSETS/Vegetation/Plants/ForestFern001/`

Keep the `Models/`, `Materials/`, `Textures/` paths intact. Start Godot 4, allow glTF imports, then open `ForestFern001_Preview.tscn` and **F6**. This previews the original embedded atlas material; assigning the included `Materials/ForestFern001Wind.tres` to the imported mesh's sole surface adds wind and global tint in War Machine.

## Models

| Variant | Shape | Height (LOD1) | Footprint (LOD1, X × Z) | LOD1 tris | LOD2 tris | LOD3 tris |
|:--|:--|--:|:--|--:|--:|--:|
| A | Arching woodland, mid-sized | 2.06 m | 2.06 × 2.25 m | 2,572 | 1,042 | 496 |
| B | Taller feathery fronds | 2.30 m | 2.95 × 2.43 m | 3,118 | 1,228 | 608 |
| C | Wider spreading frond fan | 2.08 m | 3.31 × 2.77 m | 3,354 | 1,396 | 708 |

Each `Models/forest_fern_001_{a,b,c}_LOD{1,2,3}.gltf` has an adjacent `.bin` (do not separate them). LOD1 is the detailed near mesh, LOD2 is reduced mid distance, LOD3 is the far silhouette. All LODs use a single **opaque, double-sided, textured and vertex-coloured triangle surface**, so they can be consumed by a future chunked `MultiMeshInstance3D` spawner without a per-fern `Node3D` or game script. Triangles approximate lower source density as distance increases; they do not guarantee FPS gains.

## Art and materials

- Slender, individually shaped **geometric pinnae** arranged on arching rachises, rather than broad clunky leaves or cutout billboard planes.
- Irregular fern rosettes rooted at local Y=0; branch curvature and frond rotations are reproducibly varied by variant.
- Shared `Textures/forest_fern_001_atlas.png` (512×512), green/olive matte gradient with brownish-olive stems.
- All meshes use one native glTF PBR material that works immediately upon import and does not depend on project-specific shaders.
- `Materials/ForestFern001Wind.gdshader` and `ForestFern001Wind.tres` are optional **War Machine-specific** material overrides. They read the existing `wm_foliage_tint` and `wm_foliage_tint_strength` project-global shader uniforms, and accept `wind_direction`, `wind_strength`, `wind_speed`, `palette_response` and `foliage_backlight_strength`. The shader bends upper vertices while keeping roots anchored, and colours only foliage; vertex `COLOR_0.a` serves as a leaf/stem **tint-selection mask, not transparency**. These uniforms already exist in War Machine's Godot project, but are not defined by this asset pack.
- No collision, animation player, individual scripts, texture-alpha sorting, or per-leaf processing.

## Suggested future world integration (not included)

Load meshes/materials once, share them through the **existing** vegetation chunk/LOD architecture (32 m chunks, Terrain3D terrain sampling, road/building exclusions and one GV1 fog-based visibility envelope). Randomly mix A/B/C and vary yaw/scale moderately. Avoid a separate render-distance manager or one physics node per fern. Because each fern is taller and costlier than grass, scatter more sparsely and compare actual F2 frame time when populated. Keep a separate F1 fern enable toggle by filtering only fern candidates within the shared foliage spawner.

## Files

- `Models/` — 3 fern variants × 3 LODs, nine `.gltf`/`.bin` pairs.
- `Materials/` — optional shared War Machine wind/tint shader and material.
- `Textures/` — one local 512px gradient atlas.
- `Previews/ForestFern001_Variants.png` — **software-projected preview of actual LOD1 mesh geometry**, not a Godot screenshot.
- `ForestFern001_Preview.tscn` — standalone test scene containing the three highest-detail models.
- `Source/generate_ferns.py` — deterministic Python authoring script (requires numpy and Pillow); `Source/mesh_stats.json` — measured exported mesh bounds and triangles.

## Validation status

Source geometry, glTF JSON/accessors, binary byte lengths, relative texture links, LOD pairs, vertex normals, and ZIP CRC verified offline. **Godot import, shader compilation, in-game colour/wind and actual performance remain to be checked locally.** No repository files, terrain or vegetation spawn systems were modified while creating the ZIP.


## STG6 — world ground-cover registration (2026-10-31)

**Integrated in War Machine source; waiting for local Godot build/visual validation.** `VEGETATION/Grass/Stylized/Configuration/ForestFern001Master.tres` registers this pack in the existing `GroundCoverMaster.tres` plant list at `Share=0.05` (**5%** of detailed grass candidate placements). Three authored mesh variations and **all three LODs** use the shared GV1 fog-driven LOD0/LOD1/draw envelope and 32m MultiMesh batches. Root positions follow final Terrain3D heights, slope/road/building exclusion masks and low-density intra-chunk spacing. The included wind shader receives existing environment wind and shared foliage tint via the common ground-cover owner. F1 offers this fern's separate enabled/share/spacing/size/wind controls automatically. Original GLTFs, .bin files, materials and preview scene were not altered.
