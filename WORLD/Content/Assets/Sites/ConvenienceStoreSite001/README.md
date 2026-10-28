> **PARK3 CURRENT LAYOUT (2026-10-28):** The same **56×36m** convenience-store asphalt now has **three marked rows / 42 bays**, a **9.3m-wide central entrance corridor**, two **6m circulation aisles**, **7 existing parked cars**, **8 streetlamps** (four original, four new interior fixtures), four small noncolliding concrete lamp pads, eight original storefront hedges and the same expanded 24×16m store. The two +Z approach-side lamps move outward to avoid the new far-row stalls. The rest of this README contains earlier 14-bay/four-lamp implementation history; use the PARK3 section at the end for current geometry. Source implemented, local Godot visual/collision testing pending.

> **CS2 current layout (2026-10-28):** convenience store expanded to **24×16m = twice the previous floor area**, moved behind the existing hedge and sidewalk line; rear marker and a simple flush front access strip updated. Existing 56×36m car park, eight hedges, seven cars, four lamps, road and site origin are unchanged. Source implemented, Godot validation pending. **Older store position/size references below are historical unless updated.**

**Current ground contract:** flat terrain-backed site, no added wedges or duplicate pavement/floor collision. See Flush ground-alignment correction below; older ramp descriptions are historical.

# ConvenienceStoreSite001 — village-centre roadside location

**Current:** integrated in WORLD/World.tscn, enlarged to **56 x 36m asphalt, 14 bays and 7 parked cars**. Local build/visual acceptance pending. See IMPLEMENTATION_PLAN.md and ../../../VILLAGE_IMPLEMENTATION_PLAN.md for phase history; older 40x28m / 8-bay / 3-car descriptions are superseded.

The site is a native PackedScene composition: ParkingLot001, one ConvenienceStore001 beneath Structures/Store, seven existing Car001 instances under ParkedCars, four StreetLamp001 instances under StreetLighting, and eight reused Hedge001 scenes under Landscaping/ParkingWalkwayHedges. All 21 child prefab instances stay independently reusable; no copied meshes or site processing loop. World owns placement/terrain/actors; the site owns relative child transforms.

## Current authored positions (site-local metres)

| Element | Position/details |
| --- | --- |
| Parking lot | Origin; main asphalt 56 x 36, +Z driveway ends at Z=24 |
| Store | (0,0,-29.25); 24×16m; front at Z=-21.25 with 3.5m setback behind former storefront line; rear Z=-37.25 |
| Parked cars | Bays 01/03/05/08/10/12/14; X=-20.15/-13.95/-7.75/1.55/7.75/13.95/20.15, Y=0.12, Z=-12.03 |
| Car orientation | Bays 03 and 12 backed in; others face -Z. Doors closed by default |
| Parking-edge hedge border | Eight Hedge001 instances at X=±4.5/9.4/14.3/19.2, site Y=0.15, Z=-15.7; two runs flanking an open centre. |
| Store lamps | X=-25/+25, Y=0.12, Z=-12.2; heads face inward |
| Approach lamps | X=-24/+24, Y=0.12, Z=12.5; heads face inward |
| FrontEntrance marker | (0,0.12,-20.9) |
| RearServiceExit marker | (0,0.12,-37.65) |
| StorefrontApproach | 3.6m wide × 3.62m long flat concrete strip, centre (0,0.013,-19.45), visual only: connects new frontage to old central sidewalk gap |
| RoadConnection marker | (0,0.12,24) |
| PlayerTestSpawn marker | (0,0.16,4); passive only, not the world's arrival spawn |

The site now uses flush native terrain support with no perimeter wedges (current correction below). Seven empty bays and the broad circulation aisle remain free. Lamp count stays four (existing spot/fill lighting, shadows off by default), so adding cars does not add local lights. PL2 flicker/day-night/store-glow remains separate. Lighting ownership remains ENVIRONMENT/Lighting/Fixtures/StreetLamp; geometry remains in Assets/Objects/Static/Lighting/StreetLamp001.

## World fit

The authored site is beneath WorldActorsRoot/PersistentPropsRoot/Structures. The nested store alone registers shared StructureVisibility; asphalt/lamps never inherit whole-store fading. Cars retain their own existing independent camera occluders.

The expanded mesh envelope includes X +/-28.8m parking shoulders and Z approximately -37.7..24.8, plus reserved rear/driveway access. VillageLayout measures generated bounds, surveys the entire expanded footprint and grades/clears it via existing systems. Road start and extra access reservations now follow the actual RoadConnection/RearServiceExit markers instead of the previous fixed coordinates. Rear access follows its moved marker on the same graded walking plane. The northbound village road stays at X=35 (nearest edge X=32), outside the wider paving. Other buildings/robot remain in their existing safe village locations; player arrival remains Z=38. Two rear slow-zombie markers move to X=±18, Z=-42; the runner to X=20, Z=-43; actual collision support/clearance and player buffer still gate spawning. Saved terrain is not rebaked.

## Brief checks

Source/numeric checks cover 14-bay width, exact car-to-bay placement, seven separate car envelopes, new lot/road separation, arrival buffer and scene resource references. Godot/.NET unavailable: compilation and runtime appearance/collision are not verified. Pull/build/reopen and F6 WORLD/World.tscn. Inspect all seven cars inside marked bays, lamp bases, open circulation/doors, store/sidewalk alignment, slope access from every edge, complete graded support and no grass/rocks through asphalt. Walk rear service ramp and dirt road join; check store-only fades, local zombies/PER2 and FPS. World startup logs should report the larger footprint survey without layout errors.

## Flush ground-alignment correction (current; supersedes ramp fixes)

Owner requested the simpler flat-ground solution. Site groups ParkingLot/Structures/ParkedCars/StreetLighting/SiteMarkers now sit at local Y=-0.12m, embedding the 12cm foundations so pavement, store floor, car/lamp bases and entry markers share the site's root walking elevation. World still grounds the root at VillageLayout.Level; all protected platform terrain is surveyed/graded to that same elevation, with post-check tolerance tightened from 3.5cm to 2mm. Roads still connect via markers.

The site explicitly enables UseTerrainSupport on ParkingLot and Store: native Terrain3D provides the actual walking floor throughout the lot/driveway/interior. Duplicate pavement/store-floor collision boxes are omitted for those instances; standalone prefab defaults retain optional floor support. Building walls/roof, cars, poles and intentionally raised sidewalk curbs remain collidable. The central entrance strip is visual only. Asphalt/store-floor art renders 1cm above the plane to avoid terrain z-fighting; road art is 6mm above it. This visual clearance adds no physical step. No new player step/teleport code or collision-mask workaround.

All asphalt shoulder meshes/wedge colliders, rear-door access wedge, EdgeRampWidth and the world BuildSiteAccess call are removed. This supersedes all previous ramp/wedge instructions above. The same 56x36m, 14-bay, seven-car layout, spawn safety checks, full-footprint vegetation exclusions, store-only visibility and PER2 behavior are preserved. Environment interior exclusion remains a separate plan; no haze/rain changes here.

No new per-frame work: art generation, bounds, terrain survey/grading and support verification remain startup-only. No saved region rebake/write. Numeric/source checks verify common walking heights, the two site terrain-support overrides, retained seven cars, valid resource paths and absence of wedge/trimesh floor code; diff/loop review passed. Godot/.NET unavailable: compilation, Jolt contacts and traversal remain unverified. Locally pull/build/reload, F6 World/FPS, walk directly onto every open pavement edge and into front/rear openings, then check cars/curbs/walls still block normally. No terrain rebake needed. For any remaining obstruction, hold movement and F10 to report the actual collider instead of changing ground height blindly.

## Four taller, brighter street lamps — 2026-10-28

The existing four `StreetLamp001.tscn` instances are unchanged in number, site-relative position and inward-facing yaw. Their **shared physical master** now makes each pole 7.0m tall (was 7.4m); the existing `StreetLampMesh` binding also raises the attached light emitter. Their **shared ENVIRONMENT lighting master** now supplies a 20m-range, 63°-half-angle, 4.5-energy downward spotlight and a 12m-range, 0.75-energy shadowless omni fill. This increases both spotlight footprint and surrounding brightness without adding any new light nodes, creating a per-site alternate preset, enabling shadows or changing Sky3D. Test after pulling/recompiling to confirm visual reach across the enlarged 56×36m lot; existing lamps still cannot illuminate every square metre of the large centre apron at once.

## Hedge-row landscaping at the parking-side walkway (2026-10-28)

**Implemented in source; local visual acceptance pending.** The store site now instances **eight canonical `Hedge001.tscn` objects**, four on the west side and four on the east side of the central storefront access. Both lines run parallel to the storefront (X axis), **between the parking bay heads and the raised pedestrian walkway**. These are reusable prefab instances, not baked duplicates or new mesh generators.

- **Placement:** `Landscaping/ParkingWalkwayHedges` is a sibling of the store's `Structures` group and the car/lamp groups. Site-local hedge centres: X=±4.5, ±9.4, ±14.3, ±19.2, Z=-15.70. The site has 14 bay markers from X=-21.7 to +21.7, while the raised sidewalk occupies Z≈-17.75..-15.35. Hedges sit on the sidewalk's **parking-facing edge**, not inside parking stalls or the central 3.2m lowered entrance strip.
- **Dimensions:** the eight instances share `Configuration/HedgeRowMaster.tres`, a **site-local override** of the standard `PlaceableSizeSettings` with `SizeMultiplier = Vector3(1, 1.125, 0.43)`. The GLB's native 4.805m length becomes 4.805m, **height ~2.41m (2.5× previous height)**, **depth ~0.55m**; each is separated by about 9.5cm. The central opening between end hedges is ~4.2m wide. There's **~1.77m of walking surface** on the store-facing side of the hedge mass.
- **Elevation:** site `Landscaping` parent Y=-0.12 matches other site roots; each hedge child is Y=0.27, so its actual base lies at **site Y=0.15**, the raised sidewalk top. This is intentional: don't accidentally set hedge roots at asphalt walking Y=0 where they'd be sunk into the raised sidewalk.
- **Configuration:** edit only `Configuration/HedgeRowMaster.tres` to adjust **all eight site hedges** (shared size); global `Hedge001/Configuration/HedgeMaster.tres` remains unchanged for other uses. Position/count remain explicitly authored in the site `.tscn`, not procedurally generated or randomly respawned.

**Preserved:** 14 marked bays, seven cars, parking lines, raised sidewalk curbs, open central entrance, store/side/rear access, four lamps, terrain support and existing store-only fade. Hedges are in a sibling visual group outside `Structures` and do not get mistaken for store walls/occluders. **Hedge001 currently has no collision**; these are visual boundaries only and don't block actors or bullets. Do not add physics until separately authorized.

**Local validation:** pull dev, Godot .NET import and `dotnet build war-machine.csproj`, F6 `WORLD/World.tscn`. Inspect the planting along the curb from overhead/FPS, ensuring no trees clip into parked vehicle fronts or bay lines, path/central lowered entrance remain legible, hedge feet rest on top of sidewalk, no vegetation floats and no store fade regression. Walk along the ~1.77m path behind the hedge and through the central opening; confirm no unexpected collision. Test around parking bays 1/14 and the two lamp bases. This commit does not add a terrain rebake or new per-frame code.


## Hedge foliage colour and height fix — 2026-10-28

The first in-game screenshot showed **white** hedges despite the original GLB containing green `COLOR_0` vertex attributes; its only imported PBR material had a **white** `baseColorFactor`, so any render path not applying vertex colours displayed white. `Hedge001.glb` is now explicitly **green at the PBR material level**, with neutral greyscale per-vertex brightness variation. This ensures that the model looks green even if the importer ignores vertex colour multiplication; supported pipelines retain dark/light leaf variation. Original mesh/silhouette, material count and texture-free rendering are preserved.

As requested, only the **site-local** `Configuration/HedgeRowMaster.tres` height multiplier changed **0.45 → 1.125 (×2.5)**; the site hedge height is now about **2.41m**, while X width, Z depth, positions, opening and sidewalk grounding are identical. The reusable hedge's own default-size master is unchanged. No new C# scripts, terrain operations, colliders, physics or occlusion logic.

**Local validation still required:** allow Godot to reimport updated `Hedge001.glb`, reload the world scene, check that the hedgerows are green and approximately 2.5× taller in top-down and FPS. Tall hedges will block more of the storefront view, intentionally; physical collision is still absent. If you still see white, check Godot's imported scene/material cache and the inspector's effective MeshInstance3D surface override. No in-game render could be run in the remote environment.

## Hedge foliage colour: photo-matched muted olive revision (2026-10-28)

The user's trimmed-hedge reference photo shows naturally uneven **deep olive greens and mossy highlights**, not the earlier bright, saturated evergreen. `Hedge001.glb` now uses **three low-saturation opaque foliage materials** (shadow, moss and soft olive highlight), with subtle vertex-brightness variation. Each material has a non-white PBR base to preserve the intended colour if Godot does not honour glTF `COLOR_0`. All 2,540 triangles and mesh proportions are preserved; draw-surface count is now three, rather than one. No extra textures or shaders were added.

**Site-specific taller dimensions are unchanged:** eight existing instances still use `Configuration/HedgeRowMaster.tres` with `SizeMultiplier = Vector3(1, 1.125, 0.43)` (~2.41m high), the same coordinates/central gap, and no collision. Reimport the GLB in Godot and restart the world before visual comparison from both perspectives. Inspect imported material overrides if the former bright colour persists. Runtime appearance has not been verified remotely.

## CS2 — enlarged store set back from hedges

The site retains its original world origin and **56×36m lot**; the independently packed `Store` alone moves 5.5m farther negative Z **by centre** to compensate for its larger depth (12→16m) and achieve a **3.5m net setback at the shop front**. The hedge line remains site Z=-15.7 and the lowered central entrance remains open. A small `StorefrontApproach` (noncolliding concrete) fills the new access gap from the parking walkway to the shop front. Store front Z=-21.25 / rear Z=-37.25; main site entrance marker Z=-20.9, rear marker Z=-37.65 (X=0).

The new full-footprint bounds are still surveyed, graded and excluded from foliage by `VillageLayout`; no saved terrain file changes and no physical ramp/stair/lip was introduced. The two previously near-rear slow-zombie offsets and one runner offset have been moved outside the enlarged building in `WORLD/Configuration/WorldMaster.tres` and `WORLD/WorldSettings.cs`. All car, hedge, lamp and asphalt transforms remain unchanged.

**Acceptance:** see [store CS2 test list](../../Structures/ConvenienceStore001/README.md). In particular check the new front strip is flush with the graded ground, the entrance is now visible behind hedges and the two rear rooms/corridor are navigable. The larger footprint may survey a slightly different terrain elevation; startup grade/overlap checks remain authoritative. No runtime Godot test has been performed remotely.

## Shared automatic night lighting (2026-10-28)

Four site StreetLamp001 instances now inherit the new **global** environment schedule (Sky3D time 17:15 ON / 06:30 OFF), switching off during the day without site-local polling or script. Each has one faint soft shader cone (default 3m radius, 7.4m fallback length, opacity 0.035) in addition to the existing real spot/omni illumination; the effect disables with the native lights. Masters live under `ENVIRONMENT/Lighting/Fixtures/StreetLamp/Configuration/`; the authored site scene, transforms and lamp count are untouched. Source implemented, local validation pending.

**Streetlamp visual correction (2026-10-28):** All four unchanged StreetLamp001 prefabs now automatically size their visible light cones from their 7m pole-head height, extending ~0.85m past the base so the beam doesn't visibly terminate in midair. The shared streetlamp switch-on time moves from 18:30 to **17:15**, while morning switch-off remains **06:30**. Schedule and cone overlap are in the shared `ENVIRONMENT/Lighting/Fixtures/StreetLamp/Configuration/` masters. Local FPS and top-down visual tests pending.

## PARK3 — expanded centre/road-side parking rows and interior lights (2026-10-28)

**Current scene geometry (supersedes historical 14-bay/four-lamp counts above):** Three rows totalling **42 marked spaces**. The original fourteen storefront spaces remain at Z=-14.83..-9.23 and their parked seven Car001 instances are not moved. The middle split bank has 7 spaces each side (14) at Z=-3.23..2.37; the southern split bank has 7 per side (14) at Z=8.37..13.97. Middle/south rows share a 9.3m central corridor (X=-4.65..4.65), continuously connecting the +Z road/driveway approach to the original parking aisle. There are **6.0m** vehicle circulation aisles from Z=-9.23 to -3.23 and from Z=2.37 to 8.37. A 4.03m southern asphalt buffer remains before edge Z=18. This is a more populated-looking **three-row marked layout**, not a change to asphalt size, grading or the world road.

| Area | Authored position / change |
| --- | --- |
| Original front row | 14 spaces near store (Bay_01..Bay_14); hedge line, sidewalk and entrance preserved |
| Middle row | 14 new spaces, seven west and seven east (Bay_15..Bay_28) |
| Road-facing row | 14 new spaces, seven west and seven east (Bay_29..Bay_42) |
| Centre entry route | 9.3m open corridor between the two new banks; existing 8m-wide road stub is unchanged |
| Interior north lamps | New `Lamp_WestNorthAisle` (-4,0,-6.23), `Lamp_EastNorthAisle` (4,0,-6.23) |
| Interior south lamps | New `Lamp_WestSouthAisle` (-4,0,5.37), `Lamp_EastSouthAisle` (4,0,5.37) |
| Existing perimeter approach lamps | Shift west/east from X=±24 to **±27.15** at Z=12.5 to clear far-row stalls; existing storefront lamps unchanged |

New interior lamps stand in four **1.0×1.8m shallow noncolliding concrete pads** along the central access corridor shoulders, with pole centres 8m apart across X. Inward driving corridor between the pad edges remains approximately **7m**, and the lamp arms rotate **outward over the new parking banks**, avoiding the central passage. All four lamps reuse the same `StreetLamp001.tscn` fixture/lighting defaults and **global 17:15–06:30** Sky3D on/off schedule; no new lights or scene scripts. Total is 8 existing/new lamps, not 8 additional. The small islands only provide visual grounding (no new curb collision); existing pole collider remains.

`ParkingLot` binds the new per-site `Configuration/SiteParkingLotMaster.tres` to enable 3 rows. Standalone `ParkingLot001` keeps its original one-row master. Original hedge placements, store/entry, seven parked cars, lane markings, Terrain3D support, site origin/markers, protected footprint, vegetation clearing and house fade registration are unchanged. New painted rows and markers are built once; the new lamps are native scene instances, not random placements. No new processing loops or terrain rebake.

**Local QA:** Compile `dotnet build war-machine.csproj`, F6 `WORLD/World.tscn` and FPS test. From elevated camera verify all 42 stalls have distinct painted outlines and no line/hedge intersections, inspect normal-size cars still fit existing stalls; walk/drive from +Z driveway up the central gap then turn west/east into both 6m cross aisles. Confirm concrete lamp pads do not block the middle passage, their lights illuminate bank edges without colliding with cars, both relocated approach lamps sit beside—not inside—Bay_29..Bay_42, and lamps still turn off/on via global schedule. Check slopes, asphalt/pad Z-fighting and the original storefront walkway/door. Runtime physics/rendering not tested remotely.

## Additional 20 zombies for performance testing (2026-10-28)

The shared world settings (not the site prefab scene itself) now request **16 additional slow and 4 additional runner zombies** around this site. Slow: 8 along the free cross-aisles between the parking rows, 2 in the central approach, 2 flanking the store, and 4 near/behind the rear service area. Runners: 2 flanking the building and 2 behind it. These are independent of the existing nine-slow/one-runner placement. The 42 parking bays, seven cars, eight lamps, hedge line, entrance, road and terrain grading are unchanged.

Toggle or move them in `WORLD/Configuration/WorldMaster.tres`: `StoreStressZombiesEnabled`, `StoreStressSlowOffsets` and `StoreStressRunnerOffsets`. Existing `SpawnZombieSet` safety checks gate every actor, so Godot Output reports successful accepted spawns and warnings for any collision/terrain skip. Both camera views use this stress population; optional horde count remains zero. Runtime checks, CPU/GPU performance and real navigation around cars and the store have not been verified here.
