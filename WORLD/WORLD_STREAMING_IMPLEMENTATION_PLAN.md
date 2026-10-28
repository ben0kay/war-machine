# WS — Finite world, streaming and town authoring

**Status (2026-10-28): WS0/WS1 implemented in source, awaiting local validation. WS2 isolated proof demonstrated locally; four-building active integration implemented, awaiting local validation; WS3–WS7 remain Planned.** Owner explicitly authorized WS0 with a top-centre current-section heading/grid, then WS1 fog-aware rendering. Branch: `implement-td0-td1`; one commit per subsequently authorized job. Read root README, CodePreferences, this plan, WORLD README and current owning sources before each phase. Do not resume the parked wilderness roadmap or generate a giant map automatically.

## Direction

Build a large **finite, expandable authored world**, centred on a fairly built-up town, with surrounding forest, hills, terrain, caves and eventual drivable vehicles. Establish coordinates and section ownership early; populate and extend the detailed area gradually. Exact world dimensions are undecided. Existing playable terrain remains the first test area; reserving a large logical map must not allocate or generate that whole map on startup.

Use deliberate town layout plus constrained procedural assistance. The owner places roads, intersections, important buildings and sites. Generators may create validated building variants, forest detail and roadside dressing. Alleyways, rear access and pedestrian connections are reserved before final building placement. Furniture uses legal sockets/clearances, not arbitrary wall positions that can put a picture over a doorway. World-layout choice and chunk streaming are independent: hand-placed scenes can stream just as procedural ones can.

## Phase overview

| Phase | Deliverable | Dependencies | Suggested executor | State |
| --- | --- | --- | --- | --- |
| **WS0** | Section/identity/placement contract and read-only boundary/ownership proof | Current active world/asset audit | Codex; Regular Chat design review | Implemented; Awaiting Validation |
| **WS1** | Shared fog-aware render/relevance policy for procedural and imported assets | WS0; current fog/vegetation audit | Codex; owner visual check | Implemented; Awaiting Validation |
| **WS2** | Bounded loading/unloading of a few static test sections | WS0; WS1 compatible visibility | Codex | Implemented; Awaiting Validation |
| **WS3** | Persistent state round-trip and safe unload/save lifecycle | WS2; coordinate existing IX8/BI4 plans | Codex; Copilot local checks | Planned |
| **WS4** | Nearby encounter activation and persistent actor ownership | WS3; actor lifecycle audit | Codex | Planned |
| **WS5** | Roaming placement/edit/save workflow using section records | WS0 schema; WS3 storage | Codex; Regular Chat catalogue/UI work | Planned |
| **WS6** | One representative town block and restrained population tools | WS1–WS5 locally validated | Owner layout; Codex integration | Planned |
| **WS7** | Expand geography and tune traversal/preload budgets | Proven block; later vehicle requirements | Codex; owner/Copilot profiling | Planned |

Each phase is independently authorized. Complete implementation is not acceptance: Planned → In Progress → Implemented → Awaiting Validation → Accepted. Do not implement later phases because this roadmap exists.

## Current source reality and existing owners

- `World.tscn` is the primary game; inherited `WORLD/FirstPerson/FirstPersonWorld.tscn` remains a compatibility/debug view. F5 still points at parked `testworld.tscn`; do not change it here.
- `WorldController` initializes terrain, village, actors, environment, equipment and HUD. `VillageLayout` measures prefab footprints and reserves approaches/patrols; `Generation/VillageTerrain` grades the existing terrain during initialization. These are not a persistent whole-world streamer.
- Native terrain is currently one saved gameplay area. Startup requests full-game terrain collision. Vegetation has existing player-centred batch/chunk lifecycles; pine placement already configures visibility ranges. Reuse those mechanisms, not a second independent forest manager.
- `ENVIRONMENT/Fog/PlayerDistanceFog.cs` supplies distance-fog presentation. Its gameplay/player-centred distance and optional camera-distance policy differ from native geometry visibility ranges. Thick fog is an artistic constraint; it is not automatically CPU suspension or memory unloading.
- `WORLD/Content/Assets/` is the canonical reusable library. Procedural C# and imported GLBs coexist; GLB visual wrappers do not universally inherit every procedural tree's batching, wind, collision, LOD or fade behavior today.
- Persistent actors/props have stable scene-tree roots from the existing restructure. Existing scene names, structure visibility, world-local AUDIO/hearing, player/HUD and prefab metadata must survive section activation/removal.
- There is no confirmed general saved placement catalogue, mutable world-state database or persistent town-section streamer in this source. Review actual code again before introducing one. Repository history in another project is not evidence this project already has those systems.

Related plans: [world hierarchy](WORLD_SCENE_RESTRUCTURE_PLAN.md), [village](VILLAGE_IMPLEMENTATION_PLAN.md), [surface](SURFACE_IMPLEMENTATION_PLAN.md), [caves](Content/Caves/IMPLEMENTATION_PLAN.md), [hybrid houses reference](../NOTES/Ideas/PROCEDURAL_HOUSES_AND_HYBRID_WORLD.md), [interaction IX8](../INTERACTION/IMPLEMENTATION_PLAN.md), [inventory BI4](../UI/GameplayHUD/Inventory/IMPLEMENTATION_PLAN.md). This roadmap coordinates world lifetime; those owners retain gameplay, inventory and generation responsibilities.

## Shared design contracts

1. **Separate stored description, live scene and saved changes.** Asset definitions identify reusable prefabs. Placement records identify authored instances. Mutable state records describe what happened to each instance. A missing scene instance is not a missing building in the world description.
2. **One owner per instance.** Choose an anchor-based section owner; index footprint overlap separately for loading dependencies. A house/site crossing a boundary is instantiated once, not once per touched section. Roof, lamps, children and shared references cannot create duplicate copies. Stable instance IDs survive scene reload, save/reload, asset-path changes and moving an object.
3. **Separate render, simulation and residency.** Geometry can stop drawing behind fog while collision/AI required for gameplay remain. Fully distant sections release live scenes only after state is preserved. Global sky, environment, player, camera and HUD are persistent services, not ordinary culled section props.
4. **Treat distance consistently.** Define the gameplay reference position and camera/frustum envelope explicitly. Do not assume a camera-based 3D visibility distance equals the player's horizontal fog distance. Use object/batch bounds for large buildings, tall trees and multi-cell sites. Light/shadow behavior needs its own audit, not just hiding mesh nodes.
5. **Load earlier than unload.** Keep a hysteresis band. Preload beyond the visible/interactive area and enlarge the requested region for speed/camera look-ahead. Collision must be ready before entry. A pursuing actor or ongoing interaction can temporarily pin a section beyond its normal distance.
6. **No arbitrary despawn at the fog wall.** Combat, moving actors, vehicles, ownership transfers and a player's interior occupancy require leases/explicit ownership. Forgetting a zombie is not killing it, and reloading a spawner is not permission to respawn its killed population.
7. **Authoring and play saves differ.** Building/hedge placements belong to the authored map; dead actors, moved items and collected loot belong to runtime saves. Do not overwrite source scenes/terrain from a gameplay save or rebuild mutable objects from seeds after they have changed.
8. **Bound work and memory.** No synchronous whole-town generation or whole-scene scan per frame. Background resource work must use supported engine-safe APIs; scene-tree/physics attachment happens on the main thread with a budget. Cancel stale loads, deduplicate requests, cap in-flight jobs and release resource references when appropriate. Removing nodes alone does not guarantee shared mesh/texture memory is freed.
9. **Terrain is a separate adapter.** Logical placement sections need not equal Terrain3D native region sizes. Do not slice/rebake the current terrain as a side effect of object streaming. Audit height/material residency and collision independently before promising a large terrain-streaming solution.

## WS0 — Section ownership and saved placement foundation

**Objective:** Establish exactly what a section means and how instances belong to it before anything disappears from the current world.

**Deliverables / owning files (proposed; create only what is needed):**

- A small `WORLD/Streaming/` owner with README and plain section-coordinate/placement-record helpers. Keep C# model/controllers out of art `C/` folders. Reuse an existing equivalent if the live audit finds one.
- A typed world manifest with world ID, schema version, seed, logical finite bounds and configurable section size. A **128m X/Z square** is a starting proposal to test, not a locked requirement or a terrain rebake request. Store consumed settings through `WORLD/Configuration/WorldMaster.tres` or its typed nested profile, not scattered constants.
- Placement records containing persistent instance ID, stable asset ID, owning section, world transform, footprint/dependency bounds, authoring seed and supported configuration overrides. Resolve asset IDs to canonical `.tscn` paths via one small catalogue/adapter. Do not save only a temporary Godot object instance ID or regenerate authored GUIDs on every load.
- A read-only diagnostic under `DEBUG/WorldStreaming/` showing the player's section coordinate, nearby section bounds and ownership for a few explicitly selected existing placements. No extra debug callbacks while disabled; avoid conflicting with existing F1/F7/F9 controls.
- A concise migration map for the convenience-store site and representative building/vegetation/actor owners. The composed store site can start as one owned placement; a later split must not duplicate its prefab children.

**Approach:** audit current sources; pick a fixed world origin; compute section coordinates using floor division (including negative coordinates); assign owners by a documented anchor rule; use bounds overlap only to request dependencies. Represent existing scene objects in a diagnostic manifest without reparenting, replacing or duplicating them. Validate serialization of the manifest/placement description separately from a full gameplay save.

**What the first result looks like:** the normal World still runs. An explicitly enabled debug view shows an X/Z grid and the current section ID while walking across boundaries. The store is listed once under its owning section even if its bounds touch another. No houses vanish, no zombie spawn policy changes, and no fog/terrain parameters change. This is a contract/ownership proof, not completed streaming.

**Out of scope:** unloading existing World content, background loading, player-state saves, runtime mutation persistence, replacing pine art, a giant terrain generator, town construction, a drivable car or a complete editor plugin.

**Configuration/preserve:** use runtime copies; stable authored IDs/seed/transform survive manifest round-trip. Preserve World startup, terrain data, existing village transforms, NPC AI, collision, player/HUD, weather/fog and FPS inheritance.

**Performance:** section lookup is arithmetic; recompute nearby diagnostic cells on cell changes/config changes. The optional debug view wakes only while enabled; no section renderer or background tick exists when off. No eager loading of scenes referenced by the logical catalogue merely to draw the grid.

**Pass:** correct positive/negative coordinates and boundary crossings; finite bounds handled explicitly; stable IDs after serialization/reload; one owner for a boundary-spanning prefab; no new duplicates or runtime behavior changes; diagnostics OFF sleeps. Brief build/F6 where available. Codex owns the cross-system audit/schema; Regular Chat can review data/layout choices; Copilot/owner validates locally.

## WS1 — Fog-aware rendering and relevance

**Objective:** give both procedural geometry and GLB assets a shared policy without losing batching or gameplay collision.

**Scope/files:** existing environment distance settings, pine/grass owners, reusable asset wrappers and a small streaming render-policy adapter where needed. Audit mesh, MultiMesh, light, shadow and imported-model paths. Replace a pine's visual model through its existing placement owner as a separate explicit asset-integration choice, not thousands of standalone GLB scene roots by default.

**Approach:** record each consumer's current cutoff metric; establish tunable visual cutoff/fade and preload envelopes that hide transitions in the thick fog across overhead zoom and FPS. Apply on registration/settings changes. For repeated vegetation, keep spatial batches and source meshes shared. Large props require bounds-aware handling. Suspend optional cosmetic work outside relevance; do not silently suspend essential AI or remove colliders.

**Out of scope/preserve:** no actual section unload/save, new fog appearance or global hide-every-Node3D loop. Preserve cover, weapon range/collision, structure fading, wind and existing vegetation ownership. Procedural versus imported is a visual-source distinction, not a different lifetime rule.

**Cost/pass:** no per-object polling; shared bounded distance updates only where necessary, native visibility where appropriate. Check distant GLB/procedural equivalence, edge popping, camera zoom/turn, tall objects and shadows. Measure one dense comparison locally; no claimed FPS gain without measurement. Codex integration, Regular Chat visual tuning, owner/Copilot acceptance.

## WS2 — Small static-section streaming proof

**Objective:** load and unload a few sections without a town-sized workload.

**Scope/files:** `WORLD/Streaming/` lifetime/request scheduler and an isolated `DEBUG/WorldStreaming/` test scene with a few static placement records. Opt into the active world only after the isolated proof works. Reuse stable World roots and resource/prefab lifetime hooks.

**Approach:** request nearby bounds on movement/cell changes, preload farther than visibility, unload only beyond a larger hysteresis boundary. Background-load resources where supported; budget main-thread instantiation, collision readiness and teardown. Pin sections needed by player/camera and dependencies. Start with static objects whose state does not change; use one site ownership record across boundaries. Terrain remains the existing area unless a separately verified terrain adapter is ready.

**Data/preserve:** manifest and source resources are read-only; no saving mutable world state yet. Do not unload player, global systems, ongoing encounters or current interactive items in this proof. No runtime terrain rebake.

**Cost/pass:** queue/in-flight/resident instance caps, canceled obsolete requests, no duplicate load or boundary thrashing, settled scheduler sleeps. Walk away/return repeatedly and reload scene; verify one scene per ID, footprint dependencies and no missing collision/pop-in. Record actual timings/memory locally, including first-use shader/physics spikes. Codex implementation; Copilot/owner brief scene checks.

## WS3 — Persistence before mutable unloading

**Objective:** preserve changed objects across unload/reload and application restart.

**Scope/files:** a minimal versioned world-state repository with stable instance records and a world persistence adapter; coordinate IX8/BI4 rather than inventing separate item-save authorities. Keep authored placement data separate from runtime state deltas and support unknown/missing assets without silently deleting their saved state.

**Approach:** choose a small authorized mutation contract (e.g. one moved placed prop) for the first test. Capture committed state before unload; restore before exposure to gameplay. Record removal/tombstones, changed transform and deterministic generator version/seed. Dirty writes are bounded and safe/atomic; write failure must retain unsaved state/instance rather than silently discarding it. Save active mutable sections on explicit save/shutdown as supported; no disk write per frame. Document schema upgrade policy and world IDs.

**Out of scope/preserve:** no full inventory/player/medical save or complete destructible-town simulation. Do not claim persistence for state types without a tested adapter. No respawn/reset simply because an asset leaves memory.

**Cost/pass:** event-driven dirty tracking and bounded storage work; no idle serialization. Move/remove the test object, unload/return, restart and verify the same identity/state once; test failed write, missing asset, stale/canceled load and scene exit. Codex for ownership/storage, Copilot local filesystem tests.

## WS4 — Encounters and mobile actors

**Objective:** placed encounter descriptions activate relevant zombies without keeping every actor alive across the map.

**Scope/files:** world-owned encounter/spawner records/adapters, ENTITIES lifecycle integration and persistent population state. Compose through existing health, sensing, hearing and movement; actors still own their AI. No zombie attack feature is implied.

**Approach:** a stable encounter ID plus seeded member IDs; near-player activation after ground/clearance checks; bounded per-frame spawn work and total active population cap. Save killed members and surviving state according to an explicit minimum contract. Pin pursuits/combat/interaction beyond fog; sleep or serialize genuinely irrelevant actors. Define actor ownership transfer across sections before unloading their former section. Spawn-point authoring does not itself instantiate its entire population.

**Out of scope/preserve:** no new navigation, infinite hordes, offline full-AI simulation or automatic population respawn. Existing visual/hearing pursuit and saved deaths must survive nearby lifecycle decisions.

**Cost/pass:** activate on relevance crossings, not a timer per dormant spawner. Verify killed zombies do not return just by crossing a boundary, survivors are not duplicated, pursuits remain continuous, invalid spawn candidates are skipped safely, listeners/audio subscriptions release and active counts stay bounded. Codex; owner/Copilot local encounters.

## WS5 — Roam, place, edit and save

**Objective:** the owner can build the town using canonical scenes and persistent authored placement records.

**Scope/files:** `DEBUG/WorldAuthoring/` tool or a focused extension of the existing F1 PlaceableSpawner, backed by WS0 records/WS3 storage. Existing F1 previews are temporary; do not pretend their current spawn is persistent map authoring.

**Approach:** select catalogue asset; ground-aware placement ghost; rotate/move/delete with a clear selection; retain/duplicate configuration intentionally; save/reload authored placements and stable IDs. Add basic undo or an isolated draft file so experimenting does not damage the map. Respect terrain footprint/slope and protected entrances, then add authored region/spawner markers. No silent terrain flattening when dropping a house. Keep play-save state separate from editing the baseline map.

**Out of scope/preserve:** no giant editor framework, furniture autopopulation, terrain sculpting rewrite or procedural road network in the first tool. Preserve canonical prefab identity, standalone assets and existing debug previews.

**Cost/pass:** explicit actions activate placement checks/previews; tool OFF has no input/polling/ghost. Place several structures/hedges and an encounter marker across sections, save/restart and see identical placements, IDs and variants without duplicate gameplay instances. Regular Chat can own isolated catalogue/UI polish; Codex owns placement/storage integration.

## WS6 — Representative built-up town block

**Objective:** prove density, layout and practical rendering budgets before scaling the town.

**Scope/files:** authored block/site placements, reusable Assets and constrained generation settings. Owner lays out an intersection, houses/shops, rear alley/service access, parked cars, lamps, benches and hedges. Reserve street/footpath/entrance/vehicle clearances before decoration. House variants use validated layouts and legal furnishing sockets.

**Approach/data:** hand-place the main composition; use deterministic optional tools for compatible lamp spacing, hedge runs and minor dressing. Do not generate random houses into arbitrary gaps. Vary a few well-designed assets before creating hundreds. Keep lamp visibility/relevance separate from shadow budgets; audit interior detail, transparent materials and draw calls.

**Out of scope/preserve:** no giant town at once, new traffic/driving AI, general procedural architecture solver or automatic medical/loot gameplay. Streets and alleys remain walkable; structure visibility and effects respect their existing owners.

**Cost/pass:** compare the same dense block at representative day/night views on the target PC, with GLB/procedural assets, active encounter limits, collisions and save/reload. Count resident instances/materials/lights and capture bottlenecks before changing budgets. Owner directs art/layout; Codex integrates cross-feature tools; Regular Chat handles isolated asset work.

## WS7 — Expand the world and prepare faster traversal

**Objective:** grow from a validated block into connected town districts and surrounding geography without unbounded startup/memory costs.

**Scope/files:** manifest bounds/section catalogue, terrain adapter and relevance budgets; future vehicle ownership/preload requirements. Expand a populated perimeter gradually. Design forest/hill/cave connections against road and town footprint first. A large low-detail layout map can remain lightweight and separate from loaded gameplay sections.

**Approach:** choose final-ish world extent only after travel/scale trials. Validate Terrain3D region/material/height/collision residency, cross-section caves/sites, async completion and GPU resource retention. Reserve preload distance from maximum intended speed, measured load latency and camera envelope; use conservative readiness fallback if required collision is not ready. A drivable car is a separate feature with stable mobile identity, ownership transfer and save adapters, not added by this phase automatically.

**Out of scope/preserve:** no implicit infinite world, floating origin, multiplayer or offline simulation. Add large-coordinate support only if measured/world-coordinate needs justify it. Keep the original gameplay area and saves compatible or migrate deliberately.

**Cost/pass:** long traversal and return, simulated faster travel, bounded resident memory/AI/lights, no holes/pop-in/deadlocks, no lost state at section edges, capped request queues and idle work. Codex integration; owner/Copilot targeted profiling/acceptance.

## Decisions to make during WS0, not blockers for writing this plan

- Initial logical world bounds and origin; prototype section size (128m proposal) and maximum site footprint.
- Asset ID resolution and authoring storage format using current Godot/C# facilities.
- Which few current objects become diagnostic manifest records, and which always-resident roots stay excluded.
- How existing vegetation batches map to section relevance without overlapping ownership; how current fog metrics constrain render envelopes.
- Record terrain streaming/collision limitations honestly. A working static-object streamer is not proof of a streamed gigantic terrain.

**Next step after WS0/WS1 local acceptance:** separately authorize WS2. Do not automatically implement them.

## WS0 implementation handoff — 2026-10-28

Added typed section defaults under WorldMaster, a plain finite manifest/placement/asset-ID model, explicit five-instance bootstrap identities and startup read-only capture from the actual store/building nodes. Geometry and scene transforms are not changed. The store owns its composed children once; overlap cells are dependencies, not duplicate instances. The initial logical bounds match the existing 1024m test area; default cell size is 128m with origin (0,0). Negative-coordinate support is tested separately with configurable negative bounds. Descriptions round-trip in memory, preserving IDs/transform/seed/override strings; no disk gameplay save exists.

The requested top-centre heading shows current section (default spawn 4,4), with an explicit outside-bounds status. F3 toggles a nine-cell map of section IDs, owner counts and footprint outlines. Heading updates at cell/bounds changes from existing movement signals. No new Process/PhysicsProcess/timer, actor lifecycle changes, terrain modification or culling. Both displays OFF removes movement subscriptions; input-only F3 remains. Existing help is shifted below the heading. The optional map scales to smaller viewports on resize events only; the heading stays anchored.

Read [Streaming/README.md](Streaming/README.md) for the migration/exclusion map, limitations and identity rules; [DEBUG/WorldStreaming/README.md](../DEBUG/WorldStreaming/README.md) for test commands. Source/resource/whitespace checks performed. `dotnet build war-machine.csproj` could not run because dotnet is absent (exit 127); Godot is absent too. Contract smoke test, world/FPS heading/grid/owner checks, layout and visual acceptance are pending. No measured performance claim. Delivery SHA is recorded in the job response; this handoff is part of that commit.

## WS1 implementation handoff — 2026-10-28

Added WORLD/Rendering native range policy for static props and mesh vegetation, with conservative conversion from the actual overhead/FPS fog metrics. GLBs register through normal scene additions, including F1 previews; procedural batches keep their mesh/material/LOD/collision owners. Geometry bounds, fog edge variation, camera offset/height, small wind padding, margins and shadow buffers protect the cutoff. Local lights get native influence-aware fades only when no authored fade already owns them. Fully opaque fog is required; OFF/translucent restores original ranges/fades. The FPS clear-sky profile retains its existing far clip/authored geometry ranges to avoid fog-silhouette-to-sky popping; new geometry caps require an opaque backdrop (normal overhead already has one). FPS local lights still participate. Pine metadata preserves its currently authored LOD ends through zoom/rebuilds. Existing shorter draw ranges are retained.

WorldMaster RenderPolicy fields and the debug panel's WS1 toggle/buffers are wired in both views. No new visibility/transparency writer, mesh generation, source asset replacement, terrain edit, collision/AI suspension, extra shadowed lights or section unload. Native GLB mesh LOD is retained rather than faked. Actual forest replacement remains a separate artwork integration; existing imported-Mesh batches are supported. GPU grass, native terrain, actors and global effects retain their current relevance owners.

Read [Rendering/README.md](Rendering/README.md) for precise coverage, dynamic edit/exemption hooks, authored-range composition, idle/lifecycle behavior and long-shadow/transparent-art caveats. Source/resource checks and policy arithmetic review performed. `dotnet build war-machine.csproj` failed to start (tool absent, exit 127). Godot is absent: RenderPolicyTest, GLB import/LOD inspection, world/FPS day/night/zoom/edge checks and performance measurement remain pending. No measured gain or visual acceptance claimed. WS2–WS7 remain unimplemented. Delivery SHA is recorded in the job response.

### WS1 zoom regression follow-up — 2026-10-28

Owner reports scrolling hitches disappear with WS1 disabled. Removed full scenery refreshes from ordinary zoom/settings events, using a conservative maximum-orbit envelope instead. Added a refresh-count regression assertion. Source checks only; compilation, repeated-scroll A/B and steady FPS remain awaiting local validation. WS2 remains deferred.

### WS2 isolated proof handoff — 2026-10-28

Implemented StaticSectionStreamer with bounded manifest/requests/resident roots, threaded resource reads, per-frame scene-operation budget, footprint-based hysteresis, stable-ID deduplication, obsolete-request draining and idle sleep. StaticStreamingProof.tscn provides five section-boundary pairs (procedural C# car + GLB hedge), collision walker and live counts. Config lives in DEBUG/WorldStreaming/Configuration/StreamingProof.tres. No active-world opt-in until the isolated proof is locally validated; World.tscn, village ownership, terrain, AI and WS3 persistence remain untouched. Scope and limitations are in Streaming/README.md; executable checks/handoff are in DEBUG/WorldStreaming/README.md. Source/reference/whitespace review only; compilation, runtime collision, first-use stalls and memory remain unverified here. Delivery SHA is in the job response.

### WS2 active-world integration handoff — 2026-10-28

Owner demonstrated isolated proof unload/return counts, then explicitly authorized real-world integration. WorldStaticStreaming now adopts existing Building001–004 after startup in World.tscn and inherited FPS, preserving terrain/layout/names/transforms. Actor footprint pins protect occupied scenes; existing StructureVisibility/CameraOcclusion and WS1 lifecycle hooks handle reload. Clip/orbit-derived conservative preload, hysteresis and the same capped scheduler apply. WorldMaster exposes startup opt-out and streaming profile; section heading shows real residency/load/unload status. Convenience-store composite, roads, terrain, vegetation, actors and F1 previews retain existing owners. No WS3 persistence or wider-town migration. Source/reference checks only here; compilation, the supplied integration contract smoke test and local World/FPS collision/fading/performance acceptance remain pending. See Streaming/README.md and DEBUG/WorldStreaming/README.md. Delivery SHA is in the job response.

### FPS distance tuning — 2026-10-28

Owner authorized roughly 45s sprint-equivalent unload using the observed 50s baseline, plus 15% farther opaque FPS fog. FPS fog edge 100→115m; eye-camera allowance 4m and FPS-only configurable 64m hysteresis produce preload/unload 272/336m (formerly 288/368). Empirical estimate 50×336/368≈45.7s, no timed despawn. Top-down unchanged. Source/math checks only; compilation and same-route FPS pop-in/collision/timing validation remain local. See FPS README.
