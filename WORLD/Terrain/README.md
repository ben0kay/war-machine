# Terrain resources and saved world

`Data/terrain3d_00_00.res` is an independently baked 1024m Terrain3D region containing height/control/color maps and grass instance data. It is loaded directly at runtime and remains editable with Terrain3D's editor tools. Commit saved sculpting changes deliberately.

`TerrainMaterial.tres` adapts the main Terrain3D demo's autoshader, cliff base (0), grass overlay (1), auto slope 0.45, macro noise, projection and dual scaling. World background is disabled so the finite edge is explicit. Better distant scenery belongs to WE2/WE3.

`TerrainAssets.tres` retains the original baked crossed-card asset as **LegacyBakedGrass, enabled=false**. `GrassMaterial.tres` / `Grass.gdshader` are retained only for legacy authoring and before/after comparison. They are not the visible grass renderer. Native saved instance data is preserved without modification.

Visible, terrain-following ribbon GPU grass now belongs to `VEGETATION/`, with density/range/height/quality under VegetationMaster and wind under EnvironmentMaster. No regeneration of terrain or native grass instances is needed; WorldMaster's Grass Bake settings affect only legacy offline authoring. See `VEGETATION/README.md` for the active renderer.

The two **original Terrain3D texture layers** (Ground037 grass/ground and Rock023 cliff/rock) now reference **byte-identical copies** of all four albedo/height and normal/roughness PNGs at `ASSETS/TERRAIN/`. `TerrainAssets.tres` is the only texture-path entry point for `WORLD/World.tscn` and the inherited FPS scene; `WorldController.BindTerrainAppearance` loads the same asset resource on startup. The original demo PNGs remain in `demo/assets/textures/` unchanged for comparisons and any demo consumers. Godot's import UIDs can differ between the old and new paths, so the four relocated texture references in `TerrainAssets.tres` deliberately use paths without stale old UIDs. **Allow Godot to reimport the copied PNGs after pulling**. No saved height/control maps, shaders, terrain visual parameters or scene nodes changed.

Licensing: the original `demo/assets/textures/asset_licenses.txt` documents ambientCG Ground037 and Rock023 as **CC0**; keep this original notice and `addons/terrain_3d/LICENSE.txt`. The grass shader technique is adapted from Terrain3D's MIT example.


## TDT1 — seamless forest dirt paint layer (2026-10-31)

**Source integrated; local Godot rendering/brush-preview acceptance pending.** A third `Terrain3DTextureAsset`, **Forest Dirt (id=2)**, is added to the existing `TerrainAssets.tres` list without replacing layer 0 Cliff or layer 1 Grass, changing `TerrainMaterial.tres`'s existing autoshader settings, rewriting saved terrain maps or changing `World.tscn`. Both the base world and inherited FPS world already load this resource; `WorldController.BindTerrainAppearance` duplicates the same asset and sees all three entries at startup.

New original procedural **1024×1024, seamless RGBA** texture set in `ASSETS/TERRAIN/`:

- `forest_dirt_001_alb_ht.png` — RGB earthy forest-soil albedo with small stones/organic grit; alpha is a continuous height field (centred about 0.5, with shallow stones and depressions).
- `forest_dirt_001_nrm_rgh.png` — OpenGL tangent-space normal RGB derived from the same periodic heightfield; alpha is matte earthy roughness (roughly 0.85–0.96).

Both are **mathematically periodic** (toroidal multi-octave noise, edge-wrapped stones/litter); a focused edge comparison found average RGB delta below 1/255 across the tile boundaries. This measures texture seams only, not Godot in-game appearance. The file packing matches Terrain3D's source [texture-preparation specification](https://github.com/TokisanGames/Terrain3D/blob/main/doc/docs/texture_prep.md). The new layer uses `uv_scale=0.2`, compatible with existing grass texture scale, and has its own detiling rotation. The original ambientCG textures remain unchanged.

**Painting:** pull, let Godot import the new PNGs, select `WORLD/World.tscn → TerrainRoot/Terrain3D`, choose the Terrain3D **Texture/Paint** brush and the **Forest Dirt** (slot 2) texture. Paint irregular, softly blended paths and earth patches manually. **No dirt marks appear in the existing world until painted**, and **no gameplay dirt-path generation** was introduced. Before painting, back up/save intentional Terrain3D data edits and commit the resulting saved control map(s) deliberately. When importing the new PNGs ensure their texture import mode/format matches the two existing terrain layers (Terrain3D recommends VRAM Compressed, High Quality, mipmaps ON, Normal Map Disabled for these packed RGBA maps). Normal-map auto-detection must not reinterpret the packed RGB+A format. Inspect the dirt under daylight and fog and adjust paint brush/falloff to match the desired screenshot look.


## DTP0 — forest-floor colour harmony (2026-10-31)

**Implemented in source; local Godot import/visual acceptance pending.** The existing Forest Dirt (slot 2) is now tinted with `albedo_color=Color(0.9, 0.94, 0.87, 1)` and its normal relief lowered to `normal_depth=0.85`. This is a conservative, reversible *material-resource adjustment*, not a recoloured PNG: the original 1024² seamless albedo/height and normal/roughness images, alpha channels, texture IDs, UV scale and roughness stay intact. Existing Grass (1), Cliff (0), Terrain3D autoshader and saved terrain/control maps are unchanged. **The world will not show new dirt until a later authorized painting phase or a temporary manual brush test.**

Pull and reimport the `ASSETS/TERRAIN/` images in Godot, ensure compatible 1024² high-quality VRAM/mipmap imports across the Terrain3D texture arrays (and no normal-map reinterpretation of packed normals+roughness), then F6 `WORLD/FirstPerson/FirstPersonWorld.tscn`. Try a **temporary unsaved** Forest Dirt brush stroke, check the muted brown vs. green in bright/dim foggy lighting, and discard/undo the edit. This repo-only pass cannot certify imported GPU formats or in-game blending. The full staged implementation plan is [DTP_DIRT_GROUND_IMPLEMENTATION_PLAN.md](DTP_DIRT_GROUND_IMPLEMENTATION_PLAN.md). DTP1–DTP3 are **planned, not implemented**.

 
## DTP1 — organic forest dirt authoring preview (2026-10-31)

**Implemented in source; local Godot C# build, texture paint and saved-region validation pending.** `BakeDirtPreview.tscn` is a dedicated one-shot authoring scene. It copies the committed/user-local `WORLD/Terrain/Gameplay/*.res` into a separate staging folder, writes only existing Terrain3D control-map texels there, saves the copies and moves them to `WORLD/Terrain/GameplayDirtPreview/` upon success. The original native height, control and colour maps are never changed. The authoring command will refuse to overwrite an existing preview or staging folder.

**To try the dirt patches:**
1. Pull the branch, open Godot .NET, allow PNG imports and build `war-machine.csproj`.
2. Open **`WORLD/Terrain/BakeDirtPreview.tscn`**, press **F6 once**, and read the printed count of painted texels/regions.
3. Open **`WORLD/FirstPerson/FirstPersonWorld.tscn`**, press **F6**. `Configuration/DirtPatchMaster.tres → UsePreviewInGameplay=true` will select the dirt preview **only when all copied .res files match the original source-hash manifest**. If it is missing or stale, gameplay uses the original terrain without changes.
4. To compare, set `UsePreviewInGameplay=false` and reopen FPS. To regenerate with a different seed/size/density, first back up and intentionally delete only `GameplayDirtPreview/` (or an incomplete `GameplayDirtPreview.staging/`), then rerun the one-shot authoring scene. Do **not** delete `Gameplay/` or rebake it.

**What it paints:** seeded overlapping asymmetric earth islands, small satellites and narrow winding trails within a master-configured 760m square near the village, with world-coordinate noise distortion, soft edge falloff, slope fade and a generously excluded village core. Control pixels keep non-material bitfields and skip native holes and all already manually painted (`auto=0`) surfaces. Eligible pixels become manual base Grass texture **1**, overlay Forest Dirt texture **2**, blended by a variable 0–255 weight. Unpainted ground retains the original grass/cliff autoshader, material settings and geometry. No decals, surface meshes, physics or per-frame updates.

**Important limitations:** The source gameplay's VW1 terrain height sculpt is applied *after* saved data loads; check newly steepened slopes in the FPS world. The safety rectangle protects the current central village and roads but is conservative, not a perfect polygon around every future settlement. Newly added distant buildings require reviewing `ProtectedMin/ProtectedMax`. Existing grass/billboard/shrub/fern placement still ignores dirt; DTP2 will address vegetation response. Generated preview .res files may show as untracked Git changes; commit only if visually accepted. See [DTP plan](DTP_DIRT_GROUND_IMPLEMENTATION_PLAN.md).
