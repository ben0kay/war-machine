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


### DTP1 V2 visibility correction — 2026-10-31

The original DTP1 default safety rectangle X[-150,+155] × Z[-250,+110] relative to the village origin suppressed **all** dirt within a 305×360m rectangle, including the normal player spawn (WorldMaster SpawnXZ=(512,550)). That was overly broad; running the first preview and walking around the settlement could look completely unchanged. The authoring scene's intentional exit also occurred on failure, so process closure alone did not prove it painted terrain.

**Fix:** `DirtPatchProtectedAreas.cs` replaces the enormous rectangle with individually sized conservative pads around the current parking lot/store, Building001–004, the real axis-aligned roads, robot patrol and player arrival. `DirtPatchField` adds three organic witness dirt shapes just off the player's arrival roads (one ~25m to the left), making the result inspectable without a long walk. The procedural wider-world patches remain unchanged. Look for `DTP1 V2 SUCCESS: N soil texels` in the authoring Output; FPS Output must say `DTP1 V2: verified dirt preview ACTIVE` instead of fallback. The initial generated `GameplayDirtPreview/` remains unmodified; V2 uses **new** `GameplayDirtPreviewV2/` plus distinct staging, so **do not delete V1 or your original Gameplay terrain** merely to test this correction.

**Test after pulling and rebuilding:** run `BakeDirtPreview.tscn` F6 once, inspect Output for SUCCESS, then run the FPS scene F6. Look left of spawn, off the road, for exposed soil; test toggling `DirtPatchMaster.tres → UsePreviewInGameplay=false` as an A/B. In-game rendering, C# compilation, and actual Terrain3D save/load still need local verification. If V2 fails, paste the first error from the Godot Output panel. If V2 was already generated, authoring will refuse to overwrite it; retain/backup the V2 directory before any deliberate regeneration. DTP2 vegetation thinning remains separate.


### DTP1 V3 — single spawn-area soil patch (2026-10-31)

**Owner direction supersedes the wider/locator approach for now.** V3 is intentionally **one organic dirt patch only**, not a large procedural scatter and **not** a debug marker. The patch's central XZ coordinates are `WorldMaster.SpawnXZ + (-20,-8)` = **(492, 542)** with ~23m maximum X diameter and ~15m Z diameter before procedural edge feathering, just west of the spawn-to-parking-lot approach road. The existing store/lot protection was too generous (it extended across the grass in front of spawn); its bounds have been tightened to better fit the actual 56×36m parking apron and rear store, while the narrow driveway and roads remain separately protected. No dirt is written over manual painting, holes or protected infrastructure.

The master now defaults to `SingleSpawnPreviewPatch=true`; the wider cluster/trail generator is still available when that option is disabled, but **not used** in this test. `ShowNearSpawnPatches=false` by default to suppress the prior extra demonstration patches. V3 uses **`WORLD/Terrain/GameplayDirtPreviewV3/`**, not V2; the original `Gameplay/`, and earlier V1/V2 previews are left untouched.

**Test:** Pull/build. F6 `WORLD/Terrain/BakeDirtPreview.tscn` **once**; the author scene closes and should print `DTP1 V3 SUCCESS` with a nonzero soil-texel count. Then F6 `WORLD/FirstPerson/FirstPersonWorld.tscn`. Its Output should read `DTP1 V3: verified dirt preview ACTIVE`; from default spawn (512,550), turn slightly left toward the parking lot and examine ground around (492,542). Dirt is part of Terrain3D, with no overlay mesh/marker. Vegetation is unchanged until DTP2 and may partly obscure it. To regenerate the same V3 with new size/seed, intentionally back up/delete **only** `GameplayDirtPreviewV3/` before rebaking; never delete `Gameplay/`. Local Godot render/save/build not verified remotely.

### DTP1 V3 save-persistence correction — 2026-10-31

**Root cause:** Terrain3D 1.0.2 tracks `region.edited` (refresh/undo) separately from `region.modified` (disk-save required). The one-shot authoring script previously set only `edited=true`; native `save_directory()` could silently skip painted regions, leaving a copied but unchanged `.res` file. Checking preview directory/manifest and counting in-memory texels was therefore insufficient.

**Fix:** After changing a control map, the same baker sets both `edited=true` and `modified=true`. Before moving its staging directory to the V3 preview, it reopens each changed native region with `ResourceLoader.CacheMode.Ignore` and compares the entire saved control-map byte hash against the painted image. A mismatch fails the bake and leaves staging unpublished; it does not touch `Gameplay/`. The manifest version was changed to `DTP1-v3-savefix`, invalidating earlier V3 copies. Dirt coordinates, texture assets, terrain material, heightmap, scenery, vegetation and runtime code are unchanged.

**Rebake once after pulling/building:** back up and deliberately remove **only** the existing `WORLD/Terrain/GameplayDirtPreviewV3/` folder (and any stale `GameplayDirtPreviewV3.staging/` left by failed attempts); do **not** remove `WORLD/Terrain/Gameplay/`, V1 or V2. Run `BakeDirtPreview.tscn` with F6 and require the new `DTP1 V3 SUCCESS: N soil texels saved and read back` line. Then run the FPS scene F6 and inspect X492/Z542. C# compilation, the local native save/read-back operation and visual rendering are still pending validation.


### DTP1 V3 automatic preview replacement (2026-10-31)

**Owner-approved usability improvement (source implemented; local validation pending):** You no longer have to manually back up/delete `GameplayDirtPreviewV3/` or its leftover `.staging/` before rerunning the explicit `BakeDirtPreview.tscn` F6 scene. The baker recognizes only a flat, generated V3 folder containing the known Terrain3D region filenames and its `DTP1_V3_SOURCE_MANIFEST.txt`, with legacy or corrected V3 manifest header. It cleans a *known* stale staging directory, creates and read-back-verifies the new regions, moves the previous preview to a temporary `.previous/` backup, publishes the verified replacement and removes the backup. If publishing fails, it attempts to restore the previous preview. A leftover `.previous/` from an interrupted swap is recovered or safely removed on the next explicit bake.

**Protection:** Unrecognized or additional files/subdirectories, symlinks, missing/invalid preview ownership manifest, or unexpected region filenames cause a **fail-closed** error rather than unsafe deletion. The original authored `WORLD/Terrain/Gameplay/` terrain and V1/V2 previews are never cleanup targets. No folders are touched on FPS/world startup; only the user's explicit F6 authoring command performs replacement. The prior V3 manual-cleanup instructions above are historical and superseded by this section.

**Try:** Pull and build; F6 `WORLD/Terrain/BakeDirtPreview.tscn` once (it automatically regenerates V3 without asking for cleanup). Confirm `DTP1 V3 SUCCESS: ... soil texels saved and read back` in Output, then F6 FPS and look at X492/Z542. If cleanup is refused, read the error: unknown data was retained deliberately. No Godot build or rendered acceptance has been performed remotely.


## DTP1 map-wide authoring and obsolete preview cleanup — 2026-10-31

**Current intended configuration (supersedes the earlier single-patch V3 diagnostic descriptions above):** The original patch at world X492/Z542 is retained, but `DirtPatchMaster.tres` now defaults to `SingleSpawnPreviewPatch=false`, `AreaHalfExtent=500m`, `ClusterCount=200`, and `TrailCount=12`. This generates seeded organic islands and small tapered trails across approximately **1000 × 1000m** of the 1024m terrain, with soft edge fade, slope filtering, and the existing protected road/building/patrol/spawn masks. The V3 control-map writer uses the already verified `edited` + `modified` flags and mandatory disk re-read; no shaders, asset textures, relief, or vegetation logic change.

**Build and run:** Pull the branch and build Godot .NET; run `WORLD/Terrain/BakeDirtPreview.tscn` via F6 once to regenerate the `GameplayDirtPreviewV3/` derived data; check `DTP1 V3 authoring: area=... organic marks` and `DTP1 V3 SUCCESS: ... soil texels saved and read back`. Then F6 the FPS scene. The V3 manifest header is now `DTP1-v3-mapwide`: an earlier single-patch V3 is **not accepted as current** until rebaked. Changing the master later still requires an explicit rebake. No terrain painting happens on normal game startup.

**Cleanup:** V1 `GameplayDirtPreview/` is removed from Git because its sixteen committed region files were byte-for-byte identical to the original `Gameplay/`. V2 was local/generated and not committed. Following a successful V3 bake, the authoring scene deletes only *recognized* old V1/V2 preview folders with the exact DTP1 legacy manifest version and original region-file names; unknown/subfolder/symlink data is preserved and logged as a warning. Old V3 is replaced transactionally using the existing `.previous` backup path. No manual deletion is required in the normal case. Never delete `Gameplay/` or `PineLandscape/`. Generated preview files are ignored by Git until the owner explicitly accepts and chooses to version the baked result.

**Status:** implemented in source; local Godot .NET build, full-map bake, exact patch distribution and FPS visual acceptance pending. DTP2 probabilistic thinning of grass, shrubs and ferns is still **not** implemented.
