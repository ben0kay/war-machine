# VG — Convenience-store village layout

**Status: Plan only; VG0–VG3 Planned.** Branch: `implement-td0-td1`. User requested a structured redesign of `WORLD/World.tscn`, with the existing convenience-store site as the centre. This document authorizes no automatic implementation. One commit per subsequently authorized job; brief checks and local visual acceptance per CodePreferences.

## Intended result

Replace the mixed showcase arrangement with the beginning of one small village: approach a convenience store and its parking lot, see a few zombies wandering around it, and discover existing buildings farther along a short village road. A giant robot patrols nearby open ground without walking through buildings or the main arrival area. Low surrounding hills and trees frame the settlement, with gaps for distant silhouettes through the mist.

Keep the reusable site intact. Village placement and terrain adapt around it; the asphalt does not tilt or bend to follow hills. Top-down is primary; the inherited FPS test sees the same physical layout. Night flicker/day-night lighting remains PL2, separately authorized.

| Phase | Result | Suggested assistant | State |
| --- | --- | --- | --- |
| VG0 | Establish site-centred layout, footprint/reservation data and bounded terrain preflight | Codex / Work; Regular Chat can review composition | Planned |
| VG1 | Author the complete site into World, fit Terrain3D/foundation, clear scatter and register only the store for building fading | Codex / Work — terrain, scene and initialization integration | Planned |
| VG2 | Relocate existing buildings/robot; connect village approaches and sculpt framing hills | Codex / Work — coordinated world layout and collision | Planned |
| VG3 | Site-centred player/zombie spawns, remove default showcase horde, brief integrated handoff | Codex / Work; owner/Copilot local acceptance | Planned |

Recommended implementation batches: VG0+VG1 first to establish the grounded centre; VG2+VG3 second to complete the village composition. Review each result before expanding. No new building models, inventory, zombie attacks, navigation system, water, endless world generation or new lighting framework.

## Confirmed source and current gaps

Read root README, CodePreferences, WORLD/README, this plan, the site's IMPLEMENTATION_PLAN/README, current scenes, WorldController/WorldSettings/WorldMaster, GameplayMountain, RobotPlacement and the existing scatter/grass exclusion code before editing.

- `WORLD/World.tscn` is the active world requested here. Do not edit legacy `testworld.tscn` or change project main-scene selection.
- `ConvenienceStoreSite001.tscn` is assembled and unplaced: 40x28m lot, store at local Z=-19.75, three parked cars, four lamps and named entry/road/player markers. Complete approximate footprint X +/-20m, Z -26..20m; actual mesh/marker/access bounds must be measured, not assumed exact.
- WorldController currently places Buildings001–004 and a car using `Settings.SpawnXZ + offset` and one centre terrain sample. Zombies also spawn from terrain height, not the visible asphalt/store floor. These are unsuitable as the final village placement contracts.
- Native exclusion rectangles already serve grass, pines and rocks. Site-wide rectangles must be available before those systems build. The existing robot rock clearing is hardcoded relative to spawn; replace it with reservations from the new actual patrol.
- RobotPlacement validates sampled ground and a +/-5m foot corridor on every route edge, but does not check building/prop overlap. It supports 3–4 markers. Preserve its reusable patrol/scanner and add bounded placement validation, not new robot AI.
- GameplayMountain already performs a one-shot runtime Terrain3D height-image sculpt, then updates native maps/collision without saving region files. Reuse that established approach for a bounded village terrain pass, after verifying safe resource isolation. Do not silently rebake or save the 1024m terrain.
- StructureVisibility discovers descendants of the active Structures root. A site's nested store can register without treating the lot as a building. OV1 geometric outside fading and existing indoor rules must remain intact.
- PER2 now gives zombies last-seen searches and actual-stop wander anchors. No navigation around buildings exists. Village spawn placement must not assume otherwise.

## Spatial composition and ownership

Use one independent village origin near the current playable centre, initially around world XZ (512,512), subject to full-footprint terrain checks. Do not relocate it automatically between runs. Changing the player start must not move village buildings or patrols.

| Zone | Proposed arrangement | Protection |
| --- | --- | --- |
| Centre | Whole convenience-store site at village origin; storefront faces +Z | Flat supported platform, clear rear/service access, all existing child alignment retained |
| Arrival | Player 25–40m south (+Z), facing into the lot; no immediate swarm | Safe ground and an open pedestrian approach |
| Village spine | Short road/track runs from the lot exit and bends around an east or west site edge toward -Z | Never run it through the store or put an inaccessible junction behind a wall |
| Near village | First relocated building roughly 70–100m from centre | Clear sightline gap and walkable separation |
| Far village | Other buildings roughly 110–180m away, staggered along the road | Footprint-based spacing; avoid repeating a tight showroom row |
| Robot | Outer western clearing, roughly 90–140m from centre, on 3–4 safe route markers | Entire body/feet/swept route clear of structures, cars, lamps and spawn areas |
| Scenery | Low irregular hills on flanks and behind village; forest beyond | Feathered slopes, keep routes/platforms/patrol clear and preserve some long views |

Distances are composition targets, not approved exact coordinates. VG0 records concrete transforms after measuring art, ground and clearance. All placements use world metres, not offsets relative to a moving player start.

Prefer an authored instance of the site beneath existing `WorldActorsRoot/PersistentPropsRoot/Structures`, retaining its complete independent prefab. Give it a stable name such as ConvenienceStoreSite001. Do not put StructureVisibility on the site root. Existing World roots and inherited FPS node paths remain stable. Use the existing WorldMaster as the world-layout tuning owner, with grouped village/site/building/spawn settings where needed; activate the site's currently-placeholder master only if it owns a real reusable per-site contract. Do not store competing authoritative placement coordinates in two resources.

## VG0 — Layout contract and terrain preflight

**Objective:** Establish one footprint-driven placement layout, independent of player spawn.

**Dependencies/owners:** WorldSettings/WorldMaster, WorldController, site prefab/markers, all building bounds, RobotPlacement and its route, actual Terrain3D data. Site README remains the local-layout reference.

**Work:**
- Introduce an explicit VillageOriginXZ (or equivalent stable layout origin) and planned authored site transform. Keep player start as a separate setting/marker. Preserve old offsets only where genuinely needed for migration; stop treating SpawnXZ as village origin.
- Record full site envelope from geometry once, plus separate explicit access/clearing margins: all asphalt, driveway, store floor, door landings, rear access, lamps and pedestrian connections. Include transformed coordinates if yawed.
- Sample a bounded grid over the full platform, with <=2m spacing where supported by terrain resolution, plus perimeter, corners, access markers and low/high points. Log min/max height and worst height changes; reject missing data. A centre or four-corner-only test is insufficient.
- Establish a target platform elevation from the surveyed ground, not simply its maximum. Record limits on cut/fill, slope and blend width; relocate the candidate or fail clearly if limits are exceeded. Do not make an arbitrarily tall floating slab to pass placement.
- Use one startup reservation list for site/building footprints, road/access corridors and expanded robot-route segments; consumers derive their own grass vs solid-scenery margins.

**Preserve/out of scope:** No art redesign, broad terrain rebake, streaming redesign or terrain writes in VG0. Source audit/settings/docs only if separately authorized.

**Performance/pass:** Fixed startup work, no runtime searching for a random village. Data yields explicit reproducible transforms, survey results and non-overlapping reservations; source and local scene checks distinguish measured ground from proposed coordinates.

## VG1 — Grounded site, exclusions and visibility

**Objective:** The site is genuinely part of World, with continuous support and access.

**Dependencies:** VG0 layout/reservations. Inspect Terrain3D sculpt/update APIs and source-map copying before edits.

**Approach:**
1. Author one site instance in World; prevent generic placement from spawning a duplicate.
2. Apply one deterministic, bounded runtime terrain pass around its survey-approved footprint. Flatten the protected platform beneath the asphalt/store and feather a broad irregular shoulder into existing ground. Retain shallow foundation/apron support as appropriate. No terrain protruding through pavement, unsupported outer edges or abrupt trenches around the sidewalk.
3. Coordinate existing mountain shaping so it cannot raise ground beneath the site; reserve roads/doors/patrols from both sculpt passes. Prefer one final native collision rebuild after all height edits, before activating player/zombies/robot.
4. Re-sample the complete platform and access edges after shaping; verify target tolerances and support. Missing/unsafe ground reports a clear failure rather than silently activating actors on it. Never save runtime edits to checked-in terrain regions.
5. Submit the complete footprint and connections to the existing pine/rock/grass exclusions before generation. Clear occupied surface areas, not an enormous decorative rectangle erasing the whole forest. Keep edge vegetation outside hard surfaces and walking clearances.
6. Register the nested store via current Structures discovery. Only store walls/roof/deck participate in its building fade. Parking lot and lamps remain solid; cars retain their own existing independent occluders. Verify store metadata/window openings with OV1 rather than modifying the general fade rule.

**Owners:** World scene/controller/settings, a small WORLD/Generation village-terrain helper only if needed, existing exclusions, site docs. Keep reusable prefab geometry and global camera/sky unchanged.

**Performance/pass:** Generation/survey once at startup; source masters and saved terrain untouched. Walk from surrounding ground onto the lot, sidewalk and both store exits; no floating/clipping pavement or trees/rocks/grass through it. Near-wall player visibility keeps store solid; actual cover fades store only. FPS uses the same surfaces; no inherited-scene duplicate site.

## VG2 — Village buildings, route and framing terrain

**Objective:** Replace the old showcase positions with a coherent small settlement.

**Dependencies:** Accepted site position/platform from VG1; measured footprints and existing robot geometry/settings.

**Work:**
- Move existing four buildings farther toward the north road, with staggered offsets/orientations and space to explore between them. Preserve one instance of each, doors, floor/stair behavior and current prefabs. Reuse the existing fuel station as another roadside place, not directly over the site's parking lot. Remove or relocate the old standalone showcase car rather than duplicating the site's three cars accidentally.
- Survey and ground each full building footprint/door access, using conservative individual foundations or bounded grading where needed. Do not reuse centre-only placement and assume the move is safe.
- Use a short connected track/road strip and modest shoulders to suggest a village; this is a single authored connection, not a road network generator or traffic system.
- Move the robot's existing 3–4-point patrol to open perimeter ground. Check all closed-loop segments, not just waypoints, against expanded structure/site/prop bounds using configured robot body/feet/stride clearance plus a safety margin. Include terrain slope and full foot corridor; account for scale. Native actor collision and behaviour remain unchanged. Fail dormant if no safe route exists; never allow a straight segment to cut through a building.
- Feed those route corridors into solid-tree/rock exclusions. Replace the old spawn-relative hardcoded rock clearing.
- Add 2–3 low irregular hill shoulders (roughly 4–10m initial height, broad 35–70m radii), with seeded surface variation and feathered falloff. Mask out all platforms, road/approaches and robot corridor. Keep a backdrop beyond the village but do not ring it with a uniform artificial wall or hide every distant view.
- Relocate or disable the old standalone mountain-test placement when its footprint/approach clashes with the village. Preserve its reusable code and saved terrain; do not stack conflicting test sculpts by accident.

**Owners:** World scene/settings/controller, WORLD/Generation, WORLD/Encounters/RobotPlacement and owning docs. No robot combat or procedural village framework.

**Performance/pass:** Startup-only reservation/route tests and sculpt; native collision rebuilt after final edits. Walk store→village road→other buildings without forced climbs or disconnected foundations. Observe two robot patrol cycles with body/feet clear of every structure/prop. User assesses composition under normal mist/nighttime.

## VG3 — Village population and starting experience

**Objective:** The store feels inhabited by threats rather than surrounded by a debug spawn grid.

**Dependencies:** VG1/VG2 collidable surfaces, routes and clearings finalized; PER2 retained.

**Work:**
- Use an explicit safe arrival marker south of the lot; face toward the store without changing camera controls or forcing nighttime.
- Replace default scattered test spawn arrays and enabled 20-zombie horde with one bounded site population, initially about 8–12 zombies: mostly slow wanderers across the parking margins, storefront and rear access; optionally one runner farther from the player. Keep the old horde as an explicitly OFF debug option rather than continuing to spawn it alongside the village population.
- Author candidate spawn markers/regions around the site with safe spacing from cars, lights, walls, doors and player spawn (roughly 15–20m initial player buffer, validated by available room). Do not spawn inside the store by default until interior movement is deliberately tested.
- Ground actors using the actual walkable collision surface (asphalt/sidewalk/store floor where appropriate), not just Terrain3D height beneath it. Validate body clearance, support slope, floor layer and spawn spacing before activation; skip invalid markers visibly rather than teleporting into geometry. PER2 goal sampling already supports solid surfaces; do not supply an incorrect under-asphalt spawn.
- Set initial home anchors to those safe positions, and choose a modest site-appropriate wander radius, roughly 6–10m; retain PER2's later relocation to the actual give-up position. Do not permanently confine zombies to the original site once they pursue the player.
- Keep robot patrol/foot corridor and player arrival free of initial zombie markers. Add any off-pavement spawn clearings before scatter generation. No ongoing respawn director or horde controller.
- Update status/help/location documentation to describe store, north village and perimeter patrol, replacing stale depot/robot/mountain directions.

**Owners:** World placement/config, site spawn markers if useful, WORLD/README and plan updates. Do not rewrite zombie perception, health, meshes or movement for a layout job. No new AI hearing/attacks/navigation.

**Performance/pass:** Spawn and clearance work bounded at initialization. Existing actor timers/movement and dormant static scenery remain owners; no village polling manager. Check store wandering, PER2 loss/reacquisition/new anchor, physical car/wall/trunk avoidance, player arrival buffer and both views. No claim that direct-steering zombies can route around every obstacle.

## Initialization order to preserve

Resolve layout/reservations → load saved terrain into runtime maps → survey and apply approved platform/hills → update Terrain3D and build final collision → place/ground scenery and gather exclusions → generate vegetation/rocks → place/initialize player/camera and view owners in their dependency-safe order → register nested store visibility → validate actual-surface zombie spawns and robot route → activate actors. Existing dependencies may require scene instantiation earlier, but physics/AI must stay dormant until solid ground and obstacle layout are final. Do not run visibility caches before procedural child art exists. If physics broadphase needs a deferred physics tick after adding surfaces, defer actor activation explicitly.

## Validation and handoff

Keep checks brief: source/resource/node-path/loop review, `git diff --check`, initial `dotnet build war-machine.csproj` if available. Open `WORLD/World.tscn`, F6; briefly regress inherited FPS. User/Copilot handles visual traversal, two patrol cycles and target-PC performance. Check full footprint including driveway/rear door, grass edges, store-only fade, old layout removal, actual-surface actor spawns and safe robot edges. Record build availability, exact files/commit, actual tested checks, any terrain/collision failures and remaining appearance/performance validation. Implementation stays Awaiting Validation until locally accepted.

## Relationship to existing plans

This is the world-composition owner for the new village. VG1 fulfills PL1's site-in-world integration plus the explicitly proposed bounded terrain adaptation; it supersedes PL1's earlier no-terrain-edit assumption only when that implementation scope is authorized. VG2/VG3 are additional world composition beyond the original isolated parking-lot job. PL2 lighting/flicker, PL4 visual polish, the wider SF/WE terrain roadmaps, OV2 and future navigation remain separate. No water system is included. Do not change old phase statuses until their work is implemented and reviewed.
