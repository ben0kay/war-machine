# DTP — Organic forest dirt, terrain blending and ground-cover response

**Repository:** `ben0kay/war-machine`  
**Branch:** `implement-td0-td1` (not `main`)  
**Owner:** `WORLD/Terrain/` for texture/material/authoring; `VEGETATION/Grass/Stylized/` for ground-cover placement.  
**Visual target:** irregular, branching exposed-earth openings and faint tracks through the green forest floor, with gradual grass/plant thinning. The user reference is a visual target, not an existing source asset.

> Scope: DTP0 is authorized. DTP1–DTP3 are planning only; never run a terrain rewrite or change vegetation density just because this plan exists. Prefer the primary FPS scene `WORLD/FirstPerson/FirstPersonWorld.tscn` for visual testing, with `WORLD/World.tscn` as the shared base. Do not change the F5 entry/legacy world.

## Phase overview

| Phase | Goal | Suggested assistant | Status |
| --- | --- | --- | --- |
| **DTP0** | Prepare subtle dirt/grass colour harmony, verify resource paths and import expectations, and document visual checks | ChatGPT / regular chat | **Implemented in source; awaiting local Godot visual/import acceptance** |
| **DTP1** | Explicit seeded, organic dirt **authoring** on the existing Terrain3D control maps; preserve authored land | Codex or ChatGPT Work | **Planned — not authorized** |
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

## DTP1 — Organic terrain dirt authoring (planned)

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
