# ForestGrass001 — experimental crossed-billboard clumps

This is a **separate asset**. It does not replace ForestGrass001 LOD1/LOD2; War Machine now uses it as an independent large-patch layer through STG3B in `VEGETATION/Grass/Stylized/StylizedGrassPlacement.cs`. The three A/B/C models are authored from *actual orthographic renders* of the previously supplied 20%-lighter ForestGrass001 meshes and their original dark-green foliage texture. All grass silhouettes are drawn from existing grass source geometry, not placeholder brush marks.

## Contents and geometry

| Variant | Size variation | Geometry | Atlas |
| --- | --- | --- | --- |
| A | 0.57–0.78 m tall; 0.43–0.68 m wide | 3 crossed/tilted cards, **6 triangles** | 3 distinct projected clumps in a shared 768×1536 RGBA atlas |
| B | 0.61–0.88 m tall; 0.48–0.71 m wide | 3 crossed/tilted cards, **6 triangles** | 3 distinct projected clumps in a shared 768×1536 RGBA atlas |
| C | 0.60–0.95 m tall; 0.45–0.65 m wide | 3 crossed/tilted cards, **6 triangles** | 3 distinct projected clumps in a shared 768×1536 RGBA atlas |

The cards have different yaw (approximately 5°–149°), small X/Z offsets around the origin (~0.01–0.10 m), different heights/widths, and gentle lean applied to the top edge. The 3 distinct source views in the atlas make the intersecting cards look less repetitive. Geometry is lightweight and rooted at Y=0. The baked alpha gradually fades in over the bottom ~19% of the texture; this is intended to soften the connection to uneven soil, not to become a transparent-blending material.

The included glTF uses **double-sided MASK alpha** (cutoff 0.24), not expensive alpha sorting, and the texture already contains a root fade. These are **static crossed cards**, also often called X-billboards; they are NOT continuously camera-facing quads and have no per-camera scripts. Three small cards avoid the paper-thin side view of a single camera-locked sprite.

## Integration / test

Place the folder at `ASSETS/Vegetation/Grass/ForestGrass001_Billboard/` in the Godot project (the ZIP contains the full path). The three `Models/*.gltf` files are normal PackedScenes usable in Godot and Blender. The imported glTF has a functional opaque-cutout foliage material already assigned. All A/B/C models now use **the same `Textures/forest_grass_001_billboard_shared_atlas.png`**, with UVs pointing at different atlas rows. `Materials/ForestGrass001BillboardWind.tres` and `ForestGrass001BillboardWind_DistanceFade.tres` provide two **single shared material** options compatible with the three A/B/C variants (the latter enables the existing distance-fade parameters).

For an isolated visual test, instance a few of these glTFs in the editor and scale/rotate them. For a billboard-only placement trial, create separate scatter settings that select A/B/C billboard meshes as **both** NearScenes and FarScenes and set the single shared `ForestGrass001BillboardWind_DistanceFade.tres` material. Existing `GrassScatter` uses one material override for BOTH near and far, so **do not mix original ForestGrass001 near meshes and new billboard far meshes using that shared override**; the original UVs use a different atlas. Supporting original-near/billboard-far in one scatter requires a separate near/far material reference in GrassScatter, which is intentionally not changed by this asset-only trial. Those isolated `GrassScatter` instructions describe a separate test route. The War Machine STG3 live integration instead uses its existing WorldController grass masks and stream chunks, with a distinct billboard material.

The billboard base fade uses baked alpha and alpha scissor, so the gradient will look smoother with alpha-to-coverage or alpha hashing if enabled later; it is not guaranteed smooth at every angle/shadow condition in default Godot. Test at player eye level against brown soil. Avoid using these cards too close to the camera if their silhouettes look flat. No collision, shadows or per-frame node scripts are provided.

## Performance / comparison

Previous simplified LOD1 grass is 72 / 77 / 74 triangles for A / B / C; LOD2 is 19 / 22 / 24. This experimental billboard clump uses **6 triangles each** (~92% less than simplified LOD1), at the cost of larger alpha-cutout surfaces; GPU pixel overdraw and texture bandwidth can outweigh triangle savings at huge densities. Texture memory is one shared 768×1536 RGBA atlas across all variants (plus the optional 3 separate source-render captures included for inspection). This is a geometry experiment, **not a proven FPS improvement**. Test same-camera F2 frame time and compare the source LOD against billboards at the intended distances.

## Source and validation

Built by `build_forest_grass_billboard.py` included at the ZIP root. All glTF buffers, UV bounds, triangle counts, textures and asset links have been checked with programmatic checks. Rendering in Godot/Blender, mipmapped root transparency, alpha edge quality and actual FPS are pending local visual checks. Original grass and tree assets in the user's ZIP are not overwritten.

## Current War Machine usage (STG3)

Enabled by `VEGETATION/Grass/Stylized/Configuration/BillboardGrassMaster.tres` as variable-sized, irregular **high-density patches** alongside the unchanged original stylized grass (STG3B replaces STG3's dispersed cells). Its three variant meshes use this folder's shared `ForestGrass001BillboardWind.tres` and the project-global foliage tint; no original grass mesh/atlas is overwritten. Patch density, footprint radius, member count, individual size, occasional extra tilt, slope and exclusion clearance are tuned independently, while disappearance distance follows the single global fog/vegetation visibility policy. Source integration awaiting local Godot/visual/FPS acceptance.

## BGR1 — root/tip colour and thin-edge import recovery (2026-10-31)

**Source change; local Godot visual acceptance required.** The A/B/C billboard glTF meshes, UVs,
single shared atlas PNG, deterministic placement, wind, fog/LOD rules and collision are untouched.
The authored source atlas already contains relatively bright lower blades and near-black narrow tips,
so flipping UVs would be incorrect. The existing shared billboard shader now uses local pre-wind
vertex height to darken lower blades (`root_darkening=0.52`) and apply a small green-tinted
luminance floor to **visible upper blade pixels** (`tip_luminance_floor=0.12`). Both are editable
ShaderMaterial uniforms (set 0 to bypass either correction); the original atlas alpha and cutout
threshold 0.24 are unchanged. This is a render-time colour correction, **not** a rewrite of the
baked atlas. It does not add geometry, C# work or another grass material/spawner.

The atlas import preset now requests **Lossless** texture compression, mipmaps and
**Preserve Alpha Test Coverage** with threshold 0.5, retaining Fix Alpha Border.
Godot needs to reimport the PNG locally for these settings to apply. The single
768×1536 RGBA atlas will consume more VRAM than the old S3TC texture; compare visual
quality and GPU use at the same camera position before accepting.

**Check locally:** Pull branch and let Godot reimport, then F6
`WORLD/FirstPerson/FirstPersonWorld.tscn`. In F1 Foliage, temporarily turn off
the detailed stylized grass so the billboard layer can be isolated. Stand at the
same grass patch used in the original screenshot and inspect roots, thin tips,
near/far aliasing and wind under day lighting. Toggle the two material parameters
to 0 one at a time to determine whether the problem is baked colour or alpha
filtering; compare F2 frame time with billboard grass on/off. No Godot renderer
or FPS measurements were available from the source editor.
