# ForestFern002 — low, extra-wide woodland ferns (War Machine / Godot 4)

**Standalone asset pack.** ForestFern002 is a second fern family built to complement, not replace, ForestFern001. Its natural geometry is intentionally **much lower and wider**: arching stems rise from one ground-level rhizome, spread sideways to 4–5 m, then taper toward drooping tips. Fine, separate geometric leaflets echo the stylized ForestGrass001 / ForestFern001 design. No existing project files or world vegetation code have been changed.

## Installation

1. Extract the `ForestFern002` folder from this ZIP into `<Godot project>/ASSETS/Vegetation/Plants/`.
2. Preserve the `Models/`, `Materials/`, `Textures/`, and `Source/` subfolders. Each `.gltf` needs its adjacent `.bin` file, and all use `../Textures/forest_fern_002_atlas.png`.
3. Open Godot 4 and let the glTFs import. Open `ASSETS/Vegetation/Plants/ForestFern002/ForestFern002_Preview.tscn` and run **F6**. Alternatively instance an individual LOD1 `.gltf` directly into a scene.
4. The glTF mesh contains a normal Godot-importable PBR material with the atlas. **For shared foliage tint and GPU wind**, override the mesh's **single surface material** with `Materials/ForestFern002Wind.tres` in the War Machine project. This material references the existing `wm_foliage_tint` and `wm_foliage_tint_strength` global shader uniforms, so it expects War Machine's globals to exist.

## Original model dimensions and triangle counts

| Model | Character | Approx. LOD1 height | LOD1 footprint (X × Z) | LOD1 tris | LOD2 tris | LOD3 tris |
|---|---|---:|---:|---:|---:|---:|
| A | Low spreading rosette | **1.09 m** | **4.00 × 4.36 m** | 3,868 | 1,564 | 898 |
| B | Wide asymmetric fan | **1.34 m** | **5.14 × 4.86 m** | 3,810 | 1,510 | 836 |
| C | Dense groundcover crown | **1.05 m** | **4.76 × 4.80 m** | 4,852 | 1,952 | 1,084 |

For comparison, ForestFern001's three LOD1 assets are approximately **2.06–2.30 m tall** with **2.06–3.31 m X-footprints**. ForestFern002 is lower and visually wider, not a uniformly scaled old fern.

## Asset design

- Three asymmetric, seed-stable radial fans; full outer frond outlines are kept through LOD1/2/3 so transitions change fine detail rather than entire shape.
- Very slender, tapered pinnae and non-photographic matte, olive-to-green palette; thin faceted geometry, not flat alpha-billboard cards.
- **One double-sided opaque surface per mesh**, textured with a shared 512 × 512 RGB gradient atlas plus vertex colours. Vertex `COLOR_0.a` is a **tint selection mask**, not transparency (1 = leaflet, 0 = rachis).
- Fronds begin at their common grounded pivot (local Y=0). No collision bodies, per-leaf scripts, per-instance CPU wind loops, animation tracks, or separate world system.
- Optional Wind shader bends fronds mainly based on **distance from the centre root** rather than height, which suits this very low, spreading silhouette. The shader accepts `wind_strength`, `wind_speed`, `wind_direction`, `palette_response`, and global foliage tint.

## Suggested integration, not implemented

Use the existing shared fog-driven vegetation range (`VEGETATION/Configuration/VegetationVisibilityMaster.tres`) and the existing Terrain3D/32m MultiMesh streaming and road/building exclusion architecture. Load LOD meshes once per variant, share the single material, and scatter with randomized yaw and modest scale variation (for example 0.8–1.15). Because each specimen can cover about **4–5 m across**, give it clearance from roads, building foundations and other major foliage, and start at **lower density than grass**. Do not make a second world vegetation spawner solely for this pack. Mesh count is not a GPU performance measurement.

## File structure

- `Models/forest_fern_002_{a,b,c}_LOD{1,2,3}.gltf` — nine separate Godot-importable meshes; corresponding `.bin` binary buffers.
- `Materials/ForestFern002Wind.gdshader` and `.tres` — optional War Machine wind and shared tint shader.
- `Textures/forest_fern_002_atlas.png` — local shared atlas.
- `ForestFern002_Preview.tscn` — standalone Godot 3D preview scene, no gameplay scene changes.
- `Previews/ForestFern002_Variants.png` — actual-mesh software projection (not a Godot screenshot).
- `Source/generate_ferns.py` — deterministic self-contained Python authoring script requiring numpy and Pillow; `mesh_stats.json` lists measured model dimensions and triangle counts.

## Validation status

Generated glTF mesh geometry, binary accessor byte bounds, mesh dimensions, LOD counts, normals/triangles, relative texture paths and ZIP CRC are verified offline. **Godot import/rendering, shader compilation, actual GPU frame time and in-world appearance remain for local validation.**


## STG6 — world ground-cover registration (2026-10-31)

**Integrated in War Machine source; waiting for local Godot build/visual validation.** `VEGETATION/Grass/Stylized/Configuration/ForestFern002Master.tres` registers this pack in the existing `GroundCoverMaster.tres` plant list at `Share=0.05` (**5%** of detailed grass candidate placements). Three authored mesh variations and **all three LODs** use the shared GV1 fog-driven LOD0/LOD1/draw envelope and 32m MultiMesh batches. Root positions follow final Terrain3D heights, slope/road/building exclusion masks and low-density intra-chunk spacing. The included wind shader receives existing environment wind and shared foliage tint via the common ground-cover owner. F1 offers this fern's separate enabled/share/spacing/size/wind controls automatically. Original GLTFs, .bin files, materials and preview scene were not altered.
