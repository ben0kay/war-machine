# DF — Dense high-canopy forest test biome

**Repository:** `ben0kay/war-machine`  
**Working branch:** `implement-td0-td1` (never `main` without explicit instruction)  
**Status:** **DF0 and DF1 implemented in source — awaiting local validation.** DF2–DF5 remain planned and not authorized. No forest population, saved terrain or global lighting changes have been made.  
**Priority:** Primary FPS view; retain shared-world/secondary top-down compatibility.  
**Reference:** User's forest screenshots, especially the sunlit tall-trunk woodland with overlapping foliage high overhead (visual target, not a supplied asset/license).

## Phase register

| Phase | Goal / deliverable | Recommended implementer | Status |
| --- | --- | --- | --- |
| **DF0** | Survey east-side footprint, document exclusions and add reversible forest toggle | Regular Chat (contained integration) | **Implemented in source — awaiting local validation** |
| **DF1** | Tall high-crowned broadleaf tree, three seeded variants, cheaper far art and preview | Regular Chat (isolated procedural asset) | **Implemented in source — awaiting local validation** |
| **DF2** | Seed dense, non-uniform trees within only this footprint using chunked MultiMeshes and trunk collision | Codex/Work (streaming, physics and shared world) | **Planned — not authorized** |
| **DF3** | Interlocking crowns, selective real sun shadows, atmospheric depth and canopy-gap lighting | Codex/Work for shader/render integration; Regular Chat for simple authored tuning | **Planned — not authorized** |
| **DF4** | Forest-floor layers: grass, sparse undergrowth, logs/rocks and a subtle walkable route | Codex/Work for active-world scatter integration; Regular Chat for bounded config changes | **Planned — not authorized** |
| **DF5** | LOD/shadow/collider/performance pass, A/B testing, final world compatibility and docs | Codex/Work implementation; Copilot/owner for in-engine profiling and acceptance | **Planned — not authorized** |

This file is the **implementation-ready plan** for Option A, not approval to run every phase. Each phase should be independently reviewed before implementation. One GitHub commit per explicitly authorized job; brief checks by default, full visual/performance acceptance locally.

## 1. Visual and gameplay brief

Create a **small, dense woodland that feels large from ground level**: many tall straight trunks, patchy views through 20–40m of forest, a canopy that begins well above head height and largely fills the sky, narrow brighter gaps and a darker, softer understory. Silhouettes of zombies should emerge between trunks, without adding new zombie spawn/AI logic in this project. Keep a navigable interior and at least one plausible entry from the village.

The reference reads closer to an **aspen/birch-like, broadleaf high-canopy forest** than the current pointed conifers. Existing pines may appear sparingly at the edges; the interior needs a new broadleaf silhouette. A strong look comes from **tree geometry + distribution + actual shadow/canopy filtering**, not simply darkening ambient light.

**Provisional v1 art targets, subject to viewing in Godot:** dominant canopy trees ~15–22m tall, first large branches/crowns beginning around 7–11m, broad crowns ~5–9m across; secondary younger trees ~8–14m. At entry, show a bright edge leading into darker, trunk-rich interior; avoid a solid opaque ceiling that looks like one flat surface. Mix slender pale and darker bark, modest age/size variety, sparse low branches and irregular crowns, with mostly open walking height. Keep silhouettes and sun filtering convincing in FPS; top-down should remain readable.

## 2. Verified existing code and constraints (as read on 2026-10-29)

| System / file | Confirmed current behaviour | Design constraint for DF |
| --- | --- | --- |
| `VEGETATION/Trees/Pine/PinePlacement.cs` | Anchored to player movement, terrain height/slope sampling, deterministic random placement in streamed chunks, `MultiMeshInstance3D` batches with camera occlusion per-tree data, grouped `StaticBody3D`/capsule trunk shapes, per-chunk eviction, idle callbacks disabled | Reuse this placement/streaming strategy and exclusions. Do **not** instantiate a scene or processor for every tree. |
| `VEGETATION/Trees/Pine/Configuration/PineMaster.tres` | **32m chunks**, **20m spacing**, density **0.55**, render distance **96m**, native trunk collisions; currently authors **UsePineGlb=false** while the C# class default is true | Existing placement clamps spacing to **8–40m**. DF2 needs a region-specific denser strategy; don't globally change pine density or seed. |
| `VEGETATION/Trees/Pine/C/PineMeshBuilder.cs` and existing `PineTree001.glb` | Procedural pines and an optional optimized imported GLB tree, with per-batch material/shader handling and some LOD/fade support | Preserve existing pines outside DF area. Favor shared meshes, few material surfaces and a cheaper far crown; no GLB instance per tree. |
| `VEGETATION/Grass/TerrainGrass.cs` | Existing bounded GPU ribbon grass/tile pool follows terrain; scenery masks already remove grass from hard surfaces | Reuse basic grass; forest-floor look can tune local density/colour only if separate zone masking is feasible and budget-safe. |
| `WORLD/WorldController.cs`, `WORLD/WorldSettings.cs`, `WORLD/Configuration/WorldMaster.tres` | Shared active world and FPS inherit same terrain; startup creates pines with structure/road/robot/spawn exclusions before actors drive; village origin defaults to **(512, 512)** in X/Z | Forest needs one authored zone/reservation and an explicit On/Off A/B switch. Do not move buildings, patrols, player spawn, zombies or F5 startup. |
| `WORLD/Encounters/RobotPlacement.cs` and `WORLD/VILLAGE_IMPLEMENTATION_PLAN.md` | Western perimeter robot patrol on route markers, with body/feet corridor validation and reserved vegetation clearings | Locate forest **opposite that western patrol** and reject any patch that intersects its swept corridor or village access. |
| `WORLD/Rendering/README.md` | WS1 native visibility/ranges and fog-aware registration for vegetation batches; existing pines' authored range metadata is preserved | Register batches/rebuilds with WS1 instead of adding competing fading or per-tree distance checks. FPS clear-sky fog must not show geometry popping into visible sky. |
| `WORLD/Content/Vegetation/Bushes/` and `FallenLogs/` | Existing generators are described as integrated into **legacy TestWorld**, not active World | Reuse content if useful, but DF4 needs a deliberate integration/streaming boundary; do not assume floor props already appear in survival world. |
| `WORLD/Streaming/README.md` | WS2 currently streams selected village buildings; it is **not** a general-purpose streaming manager for every tree or persistent prop | DF tree chunks should manage their own lifecycle using the existing pine model; do not enroll trees into building-scene streaming without approval. |

**No scene/terrain runtime inspection has been performed.** Proposed locations, canopy coverage, tree counts and FPS cost below are design estimates, not measured facts.

## 3. Proposed test area and asset inventory

### Location: opposite the robot

- **Provisional visual target:** an eastern/southeastern wooded flank of the store-centred village, away from the robot's western route and the road/building/test-spawn clearings. Start evaluating a **96 × 96m** test patch, with a soft 10–20m density transition into existing pines/open terrain.
- **Survey-only candidate:** centre `VillageOriginXZ + (155, 55)` → roughly world **XZ (667, 567)** using current default village origin. This is **not** an approved transform and has **not** been checked against saved Terrain3D, occupied polygons, slopes, buildings, tree-root availability or view-distance. DF0 must validate it and move/shrink the patch if necessary.
- Include a 6–8m irregular walkable entry/corridor, a few pocket clearings and a recognisable interior viewing spot. The route should be reachable from the existing village without crossing robot patrol or a building, and forest should not erase the established town silhouettes.

### First asset set (prototype, not a full ecosystem)

| Asset / role | Authoring target | Runtime policy |
| --- | --- | --- |
| **CanopyTree01** | Tall near-vertical pale/dark bark trunk; irregular crown high overhead; 2–3 geometry variations from one seed or shared mesh | New `VEGETATION/Trees/CanopyTree/` owner, shared near/far meshes and materials; native trunk collision only |
| **CanopyTreeYoung** | Shorter, thinner matching tree, less dense crown | Share the same renderer/placement pipeline; collision only if trunk is solid enough to matter |
| **Forest understory** | Small fern/shrub clusters, not a uniform dense wall | Prefer cheap uncollidable batched/scattered shapes; no per-shrub AI/physics |
| **Ground accents** | A handful of logs, small rocks, stumps and sparse leaf litter | Reuse existing world content where suitable; bounded count and selective collision only |
| **Ground cover** | Existing grass + optional low-cost regional tint/patch logic | Reuse bounded GPU grass budget; avoid a second blade simulation |

Keep art assets below `VEGETATION/Trees/` according to its README. If any *new GLB placeable* object is authored, follow the canonical `WORLD/Content/Assets/Objects/OBJECT_AUTHORING_GUIDE.md` and its explicit CollisionMode rules instead of assuming tree placement exempts standalone props.

## 4. DF0 — footprint, exclusions and reversible switch

**Objective:** establish an approved, independently toggleable forest *test zone* without changing the existing pine forest's global density.

**Dependencies / owners:** `WORLD/WorldSettings.cs` and `WorldMaster.tres`; `WORLD/WorldController.cs`/village reservation data; `VEGETATION/Forest/` future config/zone definition; `VEGETATION/Trees/Pine/PinePlacement.cs` existing exclusion inputs.

**Implementation when authorized:**
1. Survey the 96m square plus transition band using saved Terrain3D samples: valid heights, slopes, footprint, access, route, hard-surface/tree exclusions and the robot's real marker corridor. Mark any location change in the master and README; don't silently anchor it to a moving player.
2. Author `DenseForestEnabled` or a forest-specific `ForestSettings.Enabled` toggle on a duplicated runtime master, with **exactly one authoritative zone centre/size**. Decide the config ownership with the existing WorldMaster convention before adding a second master. Default OFF until an initial patch passes basic scene validation.
3. Reserve/restore existing-pine candidates inside the patch through deterministic spatial masking; never clear trees globally or generate duplicate overlapping forests. Keep the existing hard-surface and spawn clearings as higher priority than forest density.
4. Plan a one-shot debug footprint outline / scene test marker only if it is necessary to approve layout; such a marker must be OFF for normal play. Do not introduce a world-scanning editor helper.

**Out of scope:** no new tree mesh, shade system, zombies, saved-terrain edits or operational forest population. **Performance:** startup/explicit toggle only, no idle callback. **Pass:** confirmed reachable footprint, exclusions and A/B state; World + FPS share one position and the robot patrol remains clear.

## 5. DF1 — tall canopy asset construction

**Objective:** make one convincing canopy tree at player eye height before scaling to 100+.

**Dependencies/owners:** approved DF0 location/profile, `VEGETATION/Trees/CanopyTree/C/`, `Artwork/`, `Configuration/CanopyTreeMaster.tres` (planned), `VEGETATION/Trees/Pine/C/` shared mesh/material precedent.

**Implementation when authorized:**
- Produce **one parent tree type with a small number of seeded silhouette variants**. Try a procedural low-poly, organic broadleaf trunk with large branch junctions and layered/two-sided foliage clumps; imported GLB is an optional alternative if the procedural art is inadequate.
- Prioritize crown volume and irregular gaps so directional sunlight can visibly pass between leaves. Aim for one bark and one foliage material/surface where feasible; use cutout/alpha scissor only if appropriate, avoid costly blended transparency overdraw.
- Keep generated/impostor far-art as a separate source, not an additional actual tree in the scene. Test sky silhouette, bark scale, branch proportions, wind subtleness and nighttime contrast at 2m, 15m and 60m.
- Small/reusable trunk collider contract only; no leaf/branch collision or node-level animation script.

**Out of scope:** placing a forest, tree harvesting, destructibility or foliage ray-blocking. **Performance:** template/material generation once; instances reuse immutable geometry. **Pass:** accepted standalone near and mid-distance appearance from FPS and acceptable top-down silhouette.

## 6. DF2 — dense but walkable placement

**Objective:** instantiate the forest *using batched chunks*, with varied trunk density and overlapping but non-monolithic crowns.

**Dependencies/owners:** DF0 footprint + DF1 art; `VEGETATION/Forest/ForestPlacement.cs` and `Configuration/ForestMaster.tres` (planned), shared `PinePlacement` chunk/culling/exclusion techniques, `WORLD/WorldController.cs`.

**Candidate starting targets only (tune with screenshots and profiler):**
- Primary mature trunks approximately **6–9m** average spacing; younger trunks can create a second, sparser layer. Existing pine `PinePlacement` hard-clamps 8m minimum, so **do not just set `PineSettings.Spacing=5` and assume it works**. Use a new bounded, region-only candidate sampler (or extract shared reusable placement helpers) with deterministic seed and explicit hard minimum trunk separation.
- Initial population for a **96 × 96m** patch: roughly **90–150 canopy trees** plus **25–60 younger/filler trunks**, after exclusion and spacing filters. Count targets are adjustable, not guarantees. Avoid placing every tree exactly on a grid or filling all space uniformly.
- Aim for crowns to fill much of the overhead view in the interior while leaving small sky breaks and at least one 6–8m passable winding lane. Use low-frequency noise for dense cores, thinning near entries and uneven clusters; no full-world biome noise change.
- Reuse 32m chunk neighbourhoods, deterministic chunk seeds, native visibility ranges, lightweight MultiMesh batches, one physical StaticBody per active chunk with simple capsule/box shapes only for selected solid trunks, physics layer **5** (current pine trunk layer). If a trunk would block entry/path or overlap reserved props, reject that candidate.
- Preserve `PinePlacement` original seeds/positions outside the patch, per-tree canopy occlusion fidelity where needed and native layer masks for player, zombies and weapons. Ensure chunk loading does not cause visible tree gaps while entering.

**Out of scope:** AI navigation upgrade, dense zombie spawning, destruction, saving/persistent forest instance state. **Performance:** placement on chunk entry/explicit rebuild, no per-tree `_Process` and no global per-frame scan. Trees/colliders unload on chunk exit; idle world unchanged. **Pass:** same placement on every reload, forest traversable, no trees in pavement/robot/path clearance, correct physics and no obvious chunk boundaries.

## 7. DF3 — canopy shadow and atmosphere

**Objective:** create genuine shaded forest interior, not merely dark textures or fog.

**Dependencies/owners:** DF2 batch/material/crown structure; `ENVIRONMENT/` sun/sky/weather owners, `WORLD/Rendering/` WS1 policy, existing pine shaders.

**Implementation when authorized:**
- First inspect day/night directional light and shadow settings. Enable **selective near-canopy sun shadow casting** where required, with shared broadleaf crown geometry and an explicit near/mid/far shadow policy. Check actual shadow cascades, bias, leaf aliasing and FPS GPU cost in the dense patch.
- Shape crown overlap and gaps so some direct light reaches soil; use haze for depth and terrain colour variation only as secondary mood aids. **Ambient tint or fog alone cannot physically block directional sunlight.**
- Preserve sky time-of-day, weather wind, FPS fog policy and WS1 range envelope. Do not add one light, raycast or per-leaf shadow processor for every tree. Top-down camera obstruction fading should continue through existing owner integration, with extra care that it does not erase the canopy in FPS.

**Out of scope:** volumetric shaft simulation, new global sky model, baked global illumination or new shadow systems. **Performance:** GPU-shadow cost evaluated at noon/sunset, worst dense viewpoint; distant crown shadow casting can be reduced selectively. **Pass:** visibly darker interior with distinct dappled patches by day, no excessive black void or LOD/shadow popping, nighttime remains readable under intended game lighting.

## 8. DF4 — restrained forest floor and traversal

**Objective:** make forest feel naturally inhabited without a clutter wall.

**Dependencies/owners:** accepted DF2–DF3 test area, `VEGETATION/Grass/` and legacy `WORLD/Content/Vegetation/{Bushes,FallenLogs}/`, `WORLD/Content/Rocks/`.

**Implementation when authorized:** keep grass as the basic texture, layer some ferns/shrub clumps near edges and damp low points, place very occasional fallen logs/stumps/rocks at believable orientations using terrain heights, avoid blocking the only interior route or zombie-friendly open spaces. Make forest-floor distribution follow the same zone mask/seeds/chunk lifecycle instead of adding scattered always-resident scene scripts. Grass/understory collision mostly OFF; larger logs use existing approved static prop collision policy. Optional leaf litter / darker soil tint only if terrain material/local decal approaches are compatible and cheap.

**Out of scope:** loot, harvesting, wildlife spawning or new vegetation interaction. **Performance:** no extra per-blade/per-shrub callbacks; limited static props and bounded grass budgets. **Pass:** looks varied from FPS standing/crouched and remains visually readable; player can walk interior without constant collision snags.

## 9. DF5 — acceptance, profiling and optimization

**Objective:** establish that the forest actually helps atmosphere at a tolerable CPU/GPU cost before any wider rollout.

**Dependencies/owners:** DF0–DF4; `DEBUG/WorldStreaming/` and FPS counter/profiler, active World/FPS scenes, WS1 and occlusion debug controls.

**Performance A/B procedure (owner/Copilot locally):**
1. Build with `dotnet build war-machine.csproj`. In Godot open `WORLD/FirstPerson/FirstPersonWorld.tscn` and press **F6**; for shared-world compatibility use `WORLD/World.tscn` separately. Do **not** use F5, which currently opens legacy TestWorld.
2. At fixed spawn/edge/interior viewpoints, compare **forest OFF versus ON** with the **same camera angle, time of day, fog/sky/weather, graphics settings and zombie population**. Record average/low FPS, frame time, render objects/draw calls, GPU time where supported, triangle count, active tree count, active trunk shapes and chunk rebuild spikes. Do not claim optimized without measurements.
3. Walk/crouch/run through forest, circle the canopy edge, aim/fire around trunks, enter/leave/return across 32m chunk boundaries and verify stable deterministic positions, no trunks inside reserved paths, no floating roots and no sudden disappearance of shadows near the camera. Repeat after toggling and scene reload.
4. Test morning/noon/sunset/night, changing wind/weather, low-angle sun, top-down camera fade, and zombie sight/hearing cover. Note **foliage should not automatically occlude LOS** just because it looks dense; only current physical trunk/occluder collision contracts apply unless separately designed.
5. If cost is excessive, tune **materials/surfaces, crown topology, near/far mesh LOD, cast-shadow radius and trunk collider relevance**, not a new per-tree tick. Preserve near-tree cover and correct melee/firearm interactions. Record final settings and measurements in this plan/README only after observed tests.

**Out of scope:** wide world biome migration, biome-specific zombie encounter director, navigation, procedural terrain rebake, dynamic harvesting, vehicle routes or persistence changes.

## 10. Files and handoff

**Existing files to consult when implementing:** `README.md`, `CodePreferences.md`, `VEGETATION/README.md`, `VEGETATION/Trees/README.md`, `VEGETATION/Trees/Pine/{PinePlacement.cs,PineSettings.cs,Configuration/PineMaster.tres}`, `WORLD/{WorldController.cs,WorldSettings.cs,README.md,VILLAGE_IMPLEMENTATION_PLAN.md}`, `WORLD/Encounters/RobotPlacement.cs`, `WORLD/Rendering/README.md`, `WORLD/Streaming/README.md`, `VEGETATION/Grass/README.md`, and `ENVIRONMENT/` lighting/sky owners.

**Proposed only (do not create until individual phase authorization):**

```text
VEGETATION/
  Forest/
    README.md                         [created by planning task]
    FOREST_IMPLEMENTATION_PLAN.md     [created by planning task]
    ForestPlacement.cs                [DF2 — proposed]
    Configuration/
      ForestSettings.cs               [DF0/DF2 — proposed]
      ForestMaster.tres               [DF0/DF2 — proposed]
  Trees/
    CanopyTree/
      README.md                       [DF1 — proposed]
      C/CanopyTreeMeshBuilder.cs       [DF1 — proposed]
      Artwork/                        [DF1 — optional GLB route]
      Configuration/CanopyTreeMaster.tres [DF1 — proposed]
WORLD/WorldController.cs              [DF0/DF2 — integration, existing file]
WORLD/WorldSettings.cs                [DF0 — location/enable policy, existing file]
WORLD/Configuration/WorldMaster.tres   [DF0 — authored zone, existing file]
```

**Recommended next authorized job:** **DF0 only** — validate a reachable east-side footprint and its scene/terrain/robot/structure exclusions, then agree on a stable authored master config and reversible switch. Once DF0 is accepted, do DF1 (one tree visual) and then DF2 (dense patch). Only tackle DF3–DF5 after the basic silhouettes and density are visually approved. Regular Chat can own plans, README updates and isolated values; use Codex/ChatGPT Work for integration, rendering/collision and performance-sensitive work, and Copilot/owner for local engine/build checks.

**Planning-task validation:** inspected existing repo source and docs; checked intended ownership and branch. No Godot/.NET runtime testing or forest rendering was done. The user supplied aesthetic screenshot as guidance only; do not commit it as an unlicensed in-game texture.

## DF0 implementation and validation handoff (2026-10-29)

**Current status:** source-implemented; local Godot build, terrain survey, visible footprint and performance checks **not performed**.

- Authoritative location/toggle: `VEGETATION/Forest/Configuration/ForestMaster.tres`, referenced only through `WORLD/Configuration/WorldMaster.tres → DenseForest` and deep-duplicated by `WorldController` at startup. `Enabled=false` default, optional `ShowDebugOutline=false`, absolute `CenterXZ=(667,567)`, `SizeXZ=(96,96)`, `TransitionWidth=12`, `SurveyStep=8`, `MaximumSlope=0.75`.
- `ForestZone.TrySurvey` is a single bounded *post-village-grading* startup survey: every sampled point across core+transition must have finite saved Terrain3D height and 2m local slope, at least 80% must meet the grade limit, and a 4m-step walk-in from the east village road to the western forest edge must be traversable. It rejects intersections with the **existing** pine reservations (actual site/building/road/robot corridors and player/zombie/deer clearing rectangles), and out-of-world locations. It prints detailed pass/fail messages. **This is a source-level runtime checker, not a remotely verified terrain survey.**
- When `Enabled=true` **and survey passes**, WorldController adds one reserved `Rect2` to the existing PinePlacement exclusions before its first chunk build. When OFF or rejected, old pines are untouched. This is a **preview clearing, not yet a forest**. It deliberately does not alter grass, rocks, zombie spawns or persistent-world/WS2 streaming. The 12m transition band is surveyed but not cleared; DF2 can mix old pines there.
- When `ShowDebugOutline=true`, one shared `ImmediateMesh` line outline is placed at sampled terrain perimeter on startup, even if `Enabled=false`; it has no collision or update callback. Use this to examine the candidate location in both FPS and top-down. OFF after inspection to remove it.
- **Performance:** zero DF0 work in steady state when OFF; with preview ON the bounded survey runs once (roughly 250–300 terrain sample points, three height calls each, plus entry checks), followed by the existing pine startup rebuild only. No new `_Process`, `_PhysicsProcess`, tree actor, shadow or physics query.
- **Local check:** `dotnet build war-machine.csproj` then F6 `WORLD/FirstPerson/FirstPersonWorld.tscn` and `WORLD/World.tscn` (F5 is legacy). Set `ForestMaster.tres → ShowDebugOutline=true`, keep `Enabled=false`, reload and walk east to inspect candidate border and console's DF0 terrain result. Then `Enabled=true`, reload: if approved, pines disappear **only** inside the preview square while surrounding trees/roads/robot/access remain. Revert false and reload to restore them. Ensure no errors, duplicated forest markers or blocked robot route. The full-sized area is **not** approved until user verifies the in-engine border, access and survey output.
- **Limitations:** a rectangular terrain/slope/clearance preflight is not full physics walkability, view/cover validation or final placement density. A single centre can be relocated using `ForestMaster.tres` if the candidate fails. DF1 can start after accepting art direction; DF2 should not place dense trees until DF0 location is visually accepted. No new tree scenes or dedicated forest chunk streamer yet.

**Changed owners:** `VEGETATION/Forest/ForestZone.cs`, `Configuration/ForestSettings.cs`, `Configuration/ForestMaster.tres`, `WORLD/WorldSettings.cs`, `WorldMaster.tres`, `WorldController.cs`, feature/parent READMEs. One commit on `implement-td0-td1`; SHA in Git history.

## DF1 — asset implementation handoff (2026-10-29)

**Source implemented; not yet accepted.** New [CanopyTree visual asset](../Trees/CanopyTree/README.md) has a procedural 17–22m high-crowned broadleaf tree with three deterministic seeded silhouettes, two opaque coloured material surfaces (wood/foliage), branching above walking height, and a reduced-clump distant mesh built from the same silhouette. One isolated [preview scene](../Trees/CanopyTree/CanopyTreePreview.tscn) displays three near trees, a shorter lower-detail filler, and one farther low-detail tree. The tree generator belongs in `VEGETATION/Trees/CanopyTree/C/`, master in its own Configuration, Artwork reserved for possible GLB replacement. **No active-world placement, DF0 toggle changes, forest-generation code, tree collisions, global wind or lighting changes.** DF0's site acceptance remains pending.

**DF1 differences from initial proposal:** rather than a separate sapling generator, DF1 includes a 0.65× younger filler-tree example using the same far mesh; DF2 can adopt age-specific scale/mix rules without a second art pipeline. GPU wind and canopy sunlight tuning are deferred to DF2/DF3, keeping this isolated art review reversible. Distant art uses a separate generated low-detail mesh, not mesh scene nodes.

**Local acceptance:** `dotnet build war-machine.csproj`, then F6 `VEGETATION/Trees/CanopyTree/CanopyTreePreview.tscn` and inspect 2m/15m/60m profiles in Godot editor. Compare three tall variants, the smaller filler and the far tree, crown gaps, trunk width/length, bark palette and branch silhouette. Verify exactly two mesh surfaces in Inspector; edit `CanopyTreeMaster.tres` values and restart preview. Confirm no `_Process`/physics updates and that World/FPS remain unchanged with DF0 OFF. No local Godot/.NET build or measured GPU performance was performed remotely. DF2 should build near/far templates once per variant and feed them through chunk-level `MultiMesh`, using the surveyed DF0 footprint and native collision layer 5.
