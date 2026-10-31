# DTP — Organic forest dirt, terrain blending and ground-cover response

**Repository:** `ben0kay/war-machine`  
**Branch:** `implement-td0-td1` (not `main`)  
**Owner:** `WORLD/Terrain/` for texture/material/authoring; `VEGETATION/Grass/Stylized/` for ground-cover placement.  
**Visual target:** irregular, branching exposed-earth openings and faint tracks through the green forest floor, with gradual grass/plant thinning. The user reference is a visual target, not an existing source asset.

> Scope: **DTP0 and DTP1** have been authorized and implemented in source (awaiting local validation). **DTP2–DTP3** remain planning only. Never paint native source terrain or change vegetation density just because this plan exists. Prefer the primary FPS scene `WORLD/FirstPerson/FirstPersonWorld.tscn` for visual testing, with `WORLD/World.tscn` as the shared base. Do not change the F5 entry/legacy world.

## Phase overview

| Phase | Goal | Suggested assistant | Status |
| --- | --- | --- | --- |
| **DTP0** | Prepare subtle dirt/grass colour harmony, verify resource paths and import expectations, and document visual checks | ChatGPT / regular chat | **Implemented in source; awaiting local Godot visual/import acceptance** |
| **DTP1** | One-shot seeded organic dirt authoring into an isolated native Terrain3D preview copy | Codex or ChatGPT Work | **Implemented in source; awaiting local .NET/Godot and visual validation** |
| **DTP2** | Read painted soil coverage at ground-cover candidates and thin detailed/billboard grass, shrubs and ferns probabilistically | Codex or ChatGPT Work | **Planned — not authorized** |
| **DTP3** | Expose dirt authoring controls/debug diagnostics and refresh only changed vegetation chunks | Codex or ChatGPT Work | **Planned — not authorized** |

## Confirmed source baseline (2026-10-31)

- `ASSETS/TERRAIN/forest_dirt_001_alb_ht.png` (RGB colour + alpha height) and `forest_dirt_001_nrm_rgh.png` (RGB OpenGL normal + alpha roughness) are present as an existing 1024² seamless source pair; they are **not** a prepainted path.
- `WORLD/Terrain/TerrainAssets.tres`: texture slot **0 Cliff**, **1 Grass**, **2 Forest Dirt**. Forest Dirt initially had no albedo tint and `normal_depth=1.0`, `ao_strength=1.0`, `uv_scale=0.2`, `detiling_rotation=0.137`.
- `WORLD/Terrain/TerrainMaterial.tres` uses the existing Terrain3D autoshader (0/1 for slope-based cliff/grass) with macro variation and dual scaling. Manual texture ID/paint blending must respect the native auto/manual control-bit semantics; **do not disable autoshading globally** merely to paint dirt.
- `WORLD/World.tscn` references `TerrainAssets.tres` through `TerrainRoot/Terrain3D`; `WorldController.BindTerrainAppearance()` loads and duplicates that resource at startup. `FirstPersonWorld.tscn` inherits the world.
- `VEGETATION/Grass/Stylized/StylizedGrassPlacement.cs` already owns one seeded, 32m-chunk MultiMesh placement pipeline for detailed stylized grass, separate billboard filler, ForestShrub001, ForestFern001 and ForestFern002. It currently rejects road/building exclusion rectangles and steep/missing terrain; it **does not sample dirt material coverage**. Ground-cover candidate shares are 80% detailed grass, 10% shrub, 5% fern001 and 5% fern002, before per-species rejections.
- `WORLD/Generation/VillageTerrain.cs` edits a **runtime copy of height maps** once; native saved `WORLD/Terrain/Gameplay/*.res` regions must be protected. LessonsLearned LL002 warns that saved native maps persist independently of scripts.
- There are no committed `ASSETS/TERRAIN/*.png.import` sidecars. The Godot editor must import the source files; a repository-only review cannot certify the locally generated GPU format or pixel rendering.

## DTP0 — Material preparation and conservative appearance pass

**Objective:** make the existing dirt layer less saturated/overly warm in War Machine's cool, misty green forest palette, and slightly soften granular highlights while retaining the original 1024² seamless art and alpha channels.

**Authorized/source implementation:**
1. In `WORLD/Terrain/TerrainAssets.tres`, **Forest Dirt only**, set `albedo_color=Color(0.9, 0.94, 0.87, 1)` and `normal_depth=0.85`. The RGB multiplier gently mutes/olivifies the existing brown; 0.85 reduces normal relief a little. Values are intentionally conservative and editor-adjustable.
2. Keep `id=2`, original PNGs, packed height/roughness alpha, `ao_strength=1.0`, `uv_scale=0.2`, detiling rotation, 0/1 textures, material shader, roads and saved control maps unchanged.
3. Document the Godot import requirements and a **temporary, unsaved brush test**; do not manufacture `.import` cache artifacts with guessed UIDs or Windows-specific hashes.
4. No new C# manager, no idle loop, no node, no terrain paint and no foliage change in this phase.

**Why not rewrite PNGs in DTP0?** Texture files are existing seam-safe, packed terrain assets. A non-destructive layer tint gives a quick A/B and avoids desynchronizing their RGB normal/height maps. If the user later sees an unwanted hue or large-scale repeating structure in-game, review the actual decoded pixels and create an authored replacement pair in an explicitly approved art pass, instead of claiming pixel quality from repository metadata.

**Import check (local Godot, not run via repository connector):**
- Pull the branch and open Godot .NET; ensure all six terrain PNGs import successfully. Ground037/Rock023 references now live at `ASSETS/TERRAIN/`, not the demo folder.
- In the **Import** dock, ensure terrain PNGs share consistent **VRAM Compressed**, **High Quality**, and **mipmaps enabled** settings. Set **Normal Map = Disabled** for the packed `*_nrm_rgh.png` maps, so their alpha roughness channel is retained. Use the same compatible GPU format for corresponding terrain arrays and verify dimensions match.
- Confirm that `TerrainAssets.tres` has three valid layers and no magenta/checkered replacement textures.
- In `WORLD/FirstPerson/FirstPersonWorld.tscn` (F6), under daylight and fog, temporarily brush a small irregular **Forest Dirt** mark on an expendable terrain area; where the autoshader controls the surface, select the correct Terrain3D manual texture-paint mode for that spot. **Discard/undo the test** and do not save native terrain resources unless intentionally authoring.
- Check near/far colour blend, dark/bright lighting, roughness, UV tiling and edge softness. Also verify the original grass/cliff textures and no change to foliage positions.
- If the new import shader looks wrong or the material fails, revert only the Forest Dirt tint/normal depth in the inspector and capture the first Godot error.

**Performance:** terrain resource changes are loaded at world initialization; **zero new per-frame work**, zero extra draw objects and zero changed chunk workload. No measured FPS benefit is claimed.

**Acceptance:** source resources parse/import; layer order 0/1/2 intact; temporary dirt looks suitably muted under FPS lighting; grass/cliffs unaffected. **Status: implementation in repository only, local import/render validation pending.**

## DTP1 — Organic terrain dirt authoring (source implemented; awaiting validation)

**Objective:** paint large asymmetric, branching brown forest-floor openings resembling the supplied screenshot: subtle 1–4m satellites, irregular 5–18m openings, and longer ~15–45m meandering links, with colour blending at fuzzy boundaries. These are tunable starting size bands.

**Dependencies:** DTP0 accepted, installed Terrain3D control-map API validated against addon source, a backed-up/versioned native terrain data directory, identified areas to protect. Examine the current village road/plinth masks and painted-control-map bits *before* implementation.

**Scope/owner:** add a focused `WORLD/Terrain/` authoring helper and typed `Configuration/` master **only when implementing**. Use deterministic noise/seed in world XZ, multi-scale displacement and soft falloff, avoiding perfect ellipses, repeated spacing, hard edges, sharp branching angles and tile-aligned borders.

**Important integration contract:**
- Apply to **Terrain3D control maps** and its existing Grass/Dirt texture IDs; do **not** spawn meshes over ground and do **not** change heightmaps, collider topology, existing roads/building slabs or the slope-driven rock layer.
- Terrain3D autoshader currently handles 0/1; explicitly determine and update only the correct per-pixel auto/manual bits/texture weights so slot 2 actually renders. Preserve manual edits and existing native region map data; don't paint over roads, structures, authored pads, caves or user overrides.
- Prefer an explicit **offline/editor authoring command with preview/backup/undo**, not every-launch runtime painting. If native editor API cannot support non-destructive control-map edits safely, isolate to a new copy of the terrain test data until validated.
- A single shared coverage field/control-map result is authoritative for later vegetation acceptance; do not separately generate visual dirt and foliage masks with mismatched seeds.

**Performance:** bounded one-shot authoring pass over selected regions; no `_Process` or `_PhysicsProcess` when idle. Batch native map updates; never allocate full-world arrays per frame.

**Validation/pass:** original saved region checksums preserved unless the owner explicitly saves a paint operation; consistent shapes and soft edges across region/chunk borders; material IDs and rock slopes remain correct; stable seed; no spillage across roads, building floors or basements; inspect FPS near/mid/far and fog.


### DTP1 implemented-source handoff (2026-10-31)

- **Owner/files:** `WORLD/Terrain/BakeDirtPreview.tscn` runs `DirtPatchAuthoring.cs` once; `DirtPatchField.cs` generates world-XZ deterministic shapes; `Configuration/DirtPatchSettings.cs` and `DirtPatchMaster.tres` expose seed/shape/density/feather/slope/village guard settings. `WorldController` only selects a valid preview; it does not paint or save on gameplay launch. `UsePreviewInGameplay=true` does nothing until the one-shot scene creates a valid copy.
- **Workflow:** F6 `BakeDirtPreview.tscn` explicitly. The scene refuses an existing output or staging folder, copies original `Gameplay/*.res` files, records SHA-256 of every source file, loads only the copies, writes packed control maps, saves copies to staging, then moves the staging directory to `GameplayDirtPreview/`. A failed attempt does **not** publish a preview; inspect/clean only the staging folder. Then F6 `WORLD/FirstPerson/FirstPersonWorld.tscn`. Change master seed/shape settings only after safely removing/regenerating the disposable preview.
- **Native Terrain3D format:** control map = `Image.Format.Rf` with **raw little-endian uint32 bitfields**, not numeric floats. Existing automatic pixels are converted to manual *base slot 1 Grass* + *overlay slot 2 Forest Dirt*, with an 8-bit procedural blend weight. The authoring code changes only base/overlay/blend/autoshader bits; it preserves UV rotation/scale, hole, navigation and all reserved bits, and skips any prior manual pixels or holes. It calls native `region.set_map(1, Image)`, `data.update_maps(1, true, false)` and `data.save_directory(staging)`. Unpainted pixels keep the existing Grass/Cliff autoshader.
- **Protected content:** authoring is limited to a world-space square centred at `WorldMaster.VillageOriginXZ`, with noise-distorted elliptical clusters, small satellites, and tapered multi-segment trails (no repeated tile geometry). The master safety rectangle defaults to X[-150,+155] / Z[-250,+110] relative to the village origin; roads/store/buildings/robot corridor in that region are excluded. The paint also fades out on steep **source** heights. This broad exclusion is conservative rather than a surveyed building mask; review it after moving or adding buildings. Source files remain unchanged, and the FPS runtime VW1 sculpt may alter slopes further after preview loading.
- **Performance/lifecycle:** runs once only when the authoring scene is explicitly launched; no new per-frame CPU checks, new vegetation spawner, draw meshes or collider. A simple preview-validity file/hash check runs once on world initialization. 32m vegetation chunk streaming, global fog/LOD and vegetation densities are unaffected.
- **Validation needed locally (not yet run here):** `dotnet build war-machine.csproj`; run `BakeDirtPreview.tscn` once; verify every `Gameplay/*.res` source checksum unchanged, preview directory has the same region count plus manifest, and DTP1 prints nonzero changed texels. F6 primary FPS scene; verify irregular soil shape, soft transitions, correct dirt texture ID, no holes/roads/structures painted, rock slopes outside patches and no visual repetition. Compare using `UsePreviewInGameplay=false`; confirm stale previews fall back after original source edits and that the bake refuses a second run without backup/removal. Check vegetation remains unchanged until DTP2 and measure FPS locally if needed.
- **No claimed visual acceptance:** the assistant verified connector source changes only. Godot .NET compilation, native Terrain3D save/load, GPU texture-array imports and playtest have not run; do not mark DTP1 Accepted yet.


### DTP1 V2 bugfix / visibility acceptance (2026-10-31)

**Confirmed root cause:** V1's single protected rectangle X[-150,+155], Z[-250,+110] relative to the village origin included the player's default spawn and the nearby landscape. A successful bake could therefore display no dirt around the starting village, even after walking a considerable distance. The separate authoring scene also quits on either success or error, so its closing is not proof of completion.

**Source fix:** `DirtPatchProtectedAreas.cs` declares individual conservative building/site/road/patrol/spawn exclusion pads, rather than the giant village rectangle. `DirtPatchField.cs` adds three close-enough organic witness patches just off the spawn approach; these are visual-test anchors, not a new per-frame decorator. Settings include `ShowNearSpawnPatches`. The old V1 output remains unchanged and is not deleted. The new `GameplayDirtPreviewV2/` is a standalone copy guarded by SHA-256 source-region manifest `DTP1_V2_SOURCE_MANIFEST.txt`. Source regions, manually painted pixels, terrain height, and vegetation are untouched.

**Validation:** pull and build, explicitly F6 `WORLD/Terrain/BakeDirtPreview.tscn`; confirm console `DTP1 V2 SUCCESS: ... soil texels` and valid preview directory. F6 FPS scene and confirm `DTP1 V2: verified dirt preview ACTIVE` in Output. Look immediately left of the spawn approach, rather than exploring far from the village. Toggle `UsePreviewInGameplay=false` to compare original ground. If no dirt despite both log messages, investigate Terrain3D material texture array/paint blend and native region save/load with Godot diagnostics; **not yet visually verified**.

### DTP1 V3 — one visible dirt patch near spawn (2026-10-31)

**User requested a single localized ground patch** rather than the proposed nearest-soil locator/debug markers. V3 defaults to `SingleSpawnPreviewPatch=true` so `DirtPatchField` creates **only one** irregular, feathered ellipse centred at world XZ `WorldMaster.SpawnXZ + (-20,-8)`, or (492,542) with the current (512,550) spawn. Width is ~23m X by ~15m Z before feathering; enough to inspect on foot. It lies to the west of the central access road, near the parking lot without painting onto the asphalt.

The former site protection rectangle excluded ground almost up to spawn and would clip away this new patch. `DirtPatchProtectedAreas` now uses a tighter fit around the actual parking lot and store, while retaining separate protected road, other structure, robot and spawn footprints. Authored/manual Terrain3D texels remain preserved. The general-purpose seeded cluster/trail implementation is retained but disabled for V3 by the explicit single-patch setting; no new vegetation, markers or new per-frame work.

**Preview lifecycle:** `DirtPatchAuthoring` writes only to **`GameplayDirtPreviewV3/`**; `Gameplay/`, V1 and V2 remain unchanged. Source manifest version V3 and the V3 console success message distinguish this from prior generated files. FPS will use V3 only after `BakeDirtPreview.tscn` is explicitly run (F6). After pull/build, check author console `DTP1 V3 SUCCESS` with nonzero dirt pixels, then FPS `DTP1 V3: verified dirt preview ACTIVE` and visually inspect from spawn toward X492,Z542. DTP2 plant-density response is not implemented. Runtime/visual acceptance pending.

### DTP1 map-wide switch and preview consolidation (2026-10-31)

**Owner-authorized source change:** map-wide deterministic dirt distribution now defaults on; 200 clusters with 2–5 elliptical/satellite shapes per cluster plus 12 winding trails and the previously validated near-spawn X492/Z542 patch, over a 1000×1000m authored region (X/Z about 12..1012). Single-patch mode remains available in `DirtPatchMaster.tres` but defaults off. Road/building/slope/manual-paint protections and the existing native V3 control-map save/read-back mechanism are retained.

**Preview lifecycle:** New V3 authoring produces manifest version `DTP1-v3-mapwide`; the old single-patch preview is rejected by FPS until an explicit F6 bake. On successful bake only, `DirtPatchAuthoring` cleans generated legacy V1/V2 folders if their precise manifest and native region file set prove ownership; ambiguous/edited/foreign content is protected. V1's 16 region files were identical to Gameplay, so its tracked copy is deleted from the branch. Generated V3 staging/copy folders are ignored by Git, **not** written into original Gameplay. Keep `PineLandscape` and other terrain assets unchanged. Startup cost remains only the existing V3 preview verification; generation/scans only happen on the explicit authoring scene, not during normal FPS play.

**Acceptance:** Pull and build Godot .NET, F6 `BakeDirtPreview.tscn` then F6 the FPS scene. Check console for mapwide mark count, nonzero persisted soil samples and V3 active. Visually inspect several forest zones and village margins for patch variety, roads/buildings remaining clear, and unchanged rock slopes. Vegetation response is deferred to DTP2. **Source implemented; full-map rendered acceptance pending.**

## DTP2 — Soil-aware ground cover (planned)

**Objective:** grass and plants become progressively less likely to spawn on dirt without sterilizing every patch. Keep natural, occasional growth even at brown patch centres.

**Dependencies:** DTP1 authoritative coverage source and Godot Terrain3D control-map read semantics; `StylizedGrassPlacement`'s seeded candidate order, exclusion masks and two different grass streams.

**Owned integration:** a narrow read-only terrain coverage sampler shared at the Terrain/Vegetation boundary, invoked during `CreatePatchClumps()` **and** `CreateFiller()`. Do not create a second spawner, physics queries or per-frame sampling. Inspect whether existing `height_maps_changed` refresh also covers paint/control-map edits; subscribe to the suitable native signal or explicitly dirty affected chunks after authored paint. Cache/read only when chunks build.

**Candidate thinning:** sample coverage `d` in [0,1], then use a deterministic per-candidate acceptance stream and, initially, `P(keep) = 1 - sensitivity * pow(d, 1.5)` (clamped to [0,1]). Starting proposed sensitivities: detailed grass **0.88**, billboard grass **0.94**, shrubs **0.72**, ferns **0.80**. At fully exposed dirt, this retains roughly 12/6/28/20% of candidates respectively; partial dirt grades smoothly. These are inspector-tunable *proposals*, not accepted defaults. Respect texture height-blending/visible edge mismatches when calibrating the sampler.
- Keep the existing 80/10/5/5 detailed candidate selection and separate billboard seed. **Do not consume the original selection/spawn RNG** for dirt rejection; derive a separate stable hash/stream so dirt OFF gives the exact previous distributions.
- After per-species existing clearance and slope tests, reject on soil chance (or earlier if doing so is cheaper without perturbing RNG).
- Trees, road/cave masks, current F1 foliage toggles, fog-driven LOD and global streaming remain unchanged.

**Performance:** O(1) bounded soil query per candidate at **chunk construction**, no per-instance nodes or frame loops; predictable unload/reload and no extra draw batches. Validate visual counts and frame time with dirt ON/OFF, stationary idle, chunk re-entry and boundary transitions.

**Validation/pass:** bare-ish dirt centres, transitional tufts, preserved normal green spawn layout, no popping after chunk reload, stable seeds, consistent billboard/detailed shrub/fern handling and no vegetation leaking over protected areas.

## DTP3 — Authoring UX and targeted refresh (planned)

**Objective:** expose dirt patch seed, coverage, typical width, branching, feathering and an enable/preview toggle in the appropriate existing debug/editor controls; optional soil-coverage overlay and sampled candidate rejection stats. Keep F1 organized, not a second master dashboard.

**Dependencies:** DTP1 painter and DTP2 sampler; inspect existing DEBUG UI contract and validation flow first.

**Implementation:** apply/revert only on explicit authoring action; refresh **only affected ground-cover chunks**, not the whole forest. Offer a safe control-map backup or generated-overlay preview before writes. Avoid repeated import/terrain writes while dragging sliders; apply on release. Respect existing changed-region signal and user-authored control maps.

**Performance:** idle cost zero, refresh bounded to touched terrain/active chunks; no always-on texture scans or per-frame C# loops. Test release/re-enter, toggle OFF/ON, exact seed regeneration and save/reload.

## Simplifications and release boundaries

- **Recommended order:** tune/readable dirt material first (DTP0), then a few **explicit test patches** in copied native terrain (DTP1), then connect ground-cover rejection (DTP2), finally add user-facing controls (DTP3).
- Keep the procedural dirt mask a **visual surface definition**, not a runtime digging/destruction layer or navigation/pathfinding system. Those are outside this plan.
- Do not automatically change tree scattering. Tree trunks can naturally sit on dirt.
- If authoring dirt manually in Terrain3D already achieves the desired visual coverage, simplify DTP1 to the user-controlled paint workflow; DTP2 can still read the painted control map.
- This plan does not authorize changes to saved terrain, existing buildings, startup scene, vegetation settings or distant fog.

## DTP0 handoff

- Implementation change: resource-only `Forest Dirt` colour/normal adjustment (no changed source PNGs, controls, heightmaps or C#). Documentation and this implementation plan accompany it.
- Source review: branch, paths, layer ordering, existing Terrain3D material and world consumers inspected. **Godot/.NET build, GPU import, rendered material and FPS measurement not run**; user visual acceptance required.
