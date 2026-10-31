# Terrain resources and saved world

`Data/terrain3d_00_00.res` is an independently baked 1024m Terrain3D region containing height/control/color maps and grass instance data. It is loaded directly at runtime and remains editable with Terrain3D's editor tools. Commit saved sculpting changes deliberately.

`TerrainMaterial.tres` adapts the main Terrain3D demo's autoshader, cliff base (0), grass overlay (1), auto slope 0.45, macro noise, projection and dual scaling. World background is disabled so the finite edge is explicit. Better distant scenery belongs to WE2/WE3.

`TerrainAssets.tres` retains the original baked crossed-card asset as **LegacyBakedGrass, enabled=false**. `GrassMaterial.tres` / `Grass.gdshader` are retained only for legacy authoring and before/after comparison. They are not the visible grass renderer. Native saved instance data is preserved without modification.

Visible, terrain-following ribbon GPU grass now belongs to `VEGETATION/`, with density/range/height/quality under VegetationMaster and wind under EnvironmentMaster. No regeneration of terrain or native grass instances is needed; WorldMaster's Grass Bake settings affect only legacy offline authoring. See `VEGETATION/README.md` for the active renderer.

The two **active Terrain3D texture layers** (Ground037 grass/ground and Rock023 cliff/rock) now reference **byte-identical copies** of all four albedo/height and normal/roughness PNGs at `ASSETS/TERRAIN/`. `TerrainAssets.tres` is the only texture-path entry point for `WORLD/World.tscn` and the inherited FPS scene; `WorldController.BindTerrainAppearance` loads the same asset resource on startup. The original demo PNGs remain in `demo/assets/textures/` unchanged for comparisons and any demo consumers. Godot's import UIDs can differ between the old and new paths, so the four relocated texture references in `TerrainAssets.tres` deliberately use paths without stale old UIDs. **Allow Godot to reimport the copied PNGs after pulling**. No saved height/control maps, shaders, terrain visual parameters or scene nodes changed.

Licensing: the original `demo/assets/textures/asset_licenses.txt` documents ambientCG Ground037 and Rock023 as **CC0**; keep this original notice and `addons/terrain_3d/LICENSE.txt`. The grass shader technique is adapted from Terrain3D's MIT example.


## TDT1 — seamless forest dirt paint layer (2026-10-31)

**Source integrated; local Godot rendering/brush-preview acceptance pending.** A third `Terrain3DTextureAsset`, **Forest Dirt (id=2)**, is added to the existing `TerrainAssets.tres` list without replacing layer 0 Cliff or layer 1 Grass, changing `TerrainMaterial.tres`'s existing autoshader settings, rewriting saved terrain maps or changing `World.tscn`. Both the base world and inherited FPS world already load this resource; `WorldController.BindTerrainAppearance` duplicates the same asset and sees all three entries at startup.

New original procedural **1024×1024, seamless RGBA** texture set in `ASSETS/TERRAIN/`:

- `forest_dirt_001_alb_ht.png` — RGB earthy forest-soil albedo with small stones/organic grit; alpha is a continuous height field (centred about 0.5, with shallow stones and depressions).
- `forest_dirt_001_nrm_rgh.png` — OpenGL tangent-space normal RGB derived from the same periodic heightfield; alpha is matte earthy roughness (roughly 0.85–0.96).

Both are **mathematically periodic** (toroidal multi-octave noise, edge-wrapped stones/litter); a focused edge comparison found average RGB delta below 1/255 across the tile boundaries. This measures texture seams only, not Godot in-game appearance. The file packing matches Terrain3D's source [texture-preparation specification](https://github.com/TokisanGames/Terrain3D/blob/main/doc/docs/texture_prep.md). The new layer uses `uv_scale=0.2`, compatible with existing grass texture scale, and has its own detiling rotation. The original ambientCG textures remain unchanged.

**Painting:** pull, let Godot import the new PNGs, select `WORLD/World.tscn → TerrainRoot/Terrain3D`, choose the Terrain3D **Texture/Paint** brush and the **Forest Dirt** (slot 2) texture. Paint irregular, softly blended paths and earth patches manually. **No dirt marks appear in the existing world until painted**, and **no gameplay dirt-path generation** was introduced. Before painting, back up/save intentional Terrain3D data edits and commit the resulting saved control map(s) deliberately. When importing the new PNGs ensure their texture import mode/format matches the two existing terrain layers (Terrain3D recommends VRAM Compressed, High Quality, mipmaps ON, Normal Map Disabled for these packed RGBA maps). Normal-map auto-detection must not reinterpret the packed RGB+A format. Inspect the dirt under daylight and fog and adjust paint brush/falloff to match the desired screenshot look.
