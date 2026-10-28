# World sections, placement descriptions and static streaming

**WS2 proof locally demonstrated; four-building World/FPS integration implemented, awaiting local build/runtime validation. No gameplay save system yet.** [Roadmap](../WORLD_STREAMING_IMPLEMENTATION_PLAN.md).

- `WorldSectionSettings` is nested in `WORLD/Configuration/WorldMaster.tres`: world ID, grid origin, 128m section size, finite logical bounds (current prototype 0–1024m X/Z), heading and initial grid visibility. These bounds describe the current test area, not a promised final world size. No terrain allocation/bake follows from increasing logical bounds.
- `SectionCoordinate` uses floor division, including negative coordinates. World and footprint bounds are half-open. The player can leave logical bounds without changing gameplay; the heading reports that explicitly.
- `WorldPlacementRecord` stores an authored instance ID, stable asset ID, owner section, seed, 12-float world transform, four-float X/Z footprint and string overrides. No Godot object ID, retained Node, PackedScene or loaded artwork is serialized. Current captures have no custom overrides; generic override data round-trips but is not applied to scenes in WS0.
- `WorldAssetCatalog` maps seven stable prototype IDs to canonical scenes using strings only. Resolving a path does not load it. It is an explicit prototype catalogue, not discovery of all future assets. Change the path behind an asset ID when moving an asset; do not replace saved IDs with paths.
- `WorldManifest` validates schema/version, finite geometry, supported section size, unique instance IDs, known asset IDs and anchor ownership. `ToJson`/`FromJson` round-trip only the description in memory. There are no source/gameplay file writes and no persisted mutable-state claims. Footprint overlaps are reference cells; they never duplicate the owned placement. Prototype footprint dependency coverage is capped at 4096 cells.
- `WorldManifestCapture` reads the store site and Building001–004 once after startup. Stable instance IDs are explicitly authored constants scoped by WorldId; they are not generated on every run. Transforms/footprints follow actual current instances and master settings. Moving or renaming a node in future authoring must preserve its InstanceId. Future new placements need IDs saved at authoring time, not generated on loading.

## Prototype ownership map

| Current owner | WS0 treatment |
| --- | --- |
| ConvenienceStoreSite001 and its store/lot/cars/lamps | One owned site placement; children remain prefab references, not extra placements |
| Building001–004 | One record each, with real transform/bounds |
| Runtime pine/grass/rock batches | Excluded; current vegetation/scatter owner keeps seed, chunk and GPU lifetime |
| Zombies/deer/robot | Excluded; actor/encounter identity and mobile ownership are WS4 work |
| Terrain and caves | Excluded; future native terrain/cave adapters must address collision and cross-boundary residency separately |
| Player, camera, HUD, audio, hearing, environment | Always-current world services, not static placement records |

An anchor determines one owner. `CoveredSections` indexes overlapping footprint cells inside finite bounds; later streaming must load the owner's instance when a dependency cell needs it, rather than placing a second copy. WS0 diagnostics do not own gameplay lifetime. The WS2 adapter below adopts four selected placements; this remains a representative manifest, not an authoring save capable of rebuilding the whole world.

## Diagnostics and local checks

The normal world and inherited FPS show **Section (X, Z)** at top centre; default player spawn is Section (4, 4). **F3** toggles the read-only nine-cell grid. The gold boxes are recorded footprints, not residency or a terrain overlay. See [diagnostic/test README](../../DEBUG/WorldStreaming/README.md).

No process/physics callback is introduced. Existing player PositionChanged signals perform constant-time section lookup; labels/map redraw only at cell/bounds changes or explicit toggles. With both heading and grid off, movement subscriptions are removed; F3 input remains available. Grid nodes hold data only, never own gameplay entities. Exit detaches subscriptions. Startup's bounded five-prefab bounds capture/JSON validation is not repeated during motion.

Source/resource/whitespace checks ran; neither Godot nor dotnet is available here. Build and runtime checks remain pending. WS3–WS7 remain planned.

## WS2 — isolated static residency proof

Run `DEBUG/WorldStreaming/StaticStreamingProof.tscn` with **F6**. This is deliberately separate from World.tscn: no village reparenting, terrain streaming/rebake, actor changes, mutable save data or automatic active-world opt-in. WASD moves the collision walker; D walks east along five 128m-spaced pairs of a procedural C# Car001 and imported GLB Hedge001. A returns; Shift speeds up. Cars straddle four section footprints but retain one placement ID/scene. The labels and flat test ground remain persistent diagnostics. Cars have collision; hedges retain their visual-only prefab contract.

`StaticSectionStreamer` consumes a validated, cloned WS0 manifest. One identity dictionary owns live instances; actual X/Z footprint distance handles cross-section dependencies without creating a scene per overlap. It does **not** stream whole terrain cells or scan the scene tree. A small capped manifest is inspected on movement notifications (8m default), with 180m preload / 240m unload hysteresis. The isolated camera follows the walker, has a fixed 120m far clip and no zoom, so the load envelope covers its reach plus follow offset and movement band. These test distances are not a production vehicle/zoom readiness guarantee.

`DEBUG/WorldStreaming/Configuration/StreamingProof.tres` owns the proof configuration, using `WorldStreamingSettings`. MaxPlacements=128 caps input; MaxResident=16 caps live scene roots; MaxInFlight=2 caps retained path requests; OperationsPerFrame=1 caps scene attachments/removals. Settings are copied at initialization. Request bookkeeping is bounded by the manifest/caps. Resident counts include immediately attached scenes; an explicit two-physics-frame readiness marker lets the test walker refuse entry into unready footprints. This is a harness safety check, **not** a production physics barrier or a replacement for actual collision tests.

Main-thread work wakes on moved anchor/initial request. Resource reads use ResourceLoader threaded APIs; scene Instantiate/AddChild/_Ready and Free remain on the main thread. One large procedural prefab can still hitch inside its indivisible Ready callback: count budgets are not millisecond guarantees. Do not use this proof as evidence that loading a whole shop is already stall-free. Ordinary frames only poll capped jobs and inspect the bounded placement list while work remains; settled/empty/capacity-blocked schedulers disable processing. No scheduler timers, idle physics callbacks or per-object distance scripts. At capacity, later desired placements wait until slots become available; this prototype does not implement priority eviction or production readiness guarantees under overload. Keep the test demand below its resident cap.

Obsolete queued intent is removed on movement. Godot has no threaded-request cancel API: started obsolete reads finish, their result is drained and references released without attachment. `StaticSectionLoadDrain` temporarily finishes pending reads after a scene switch and frees itself; normal settled play has no drain node. Failed assets are warned and suppressed for that streamer lifetime (restart scene to retry); source descriptions are not deleted. Engine import/resource caching may retain shared data, so unloading nodes is not a measured memory reduction.

Overrides are explicitly rejected: this phase instantiates canonical static defaults only. Authoring seed/identity remain stored in the manifest; no new seeded generation is performed. Do not opt mutable props or encounters into this streamer before WS3/WS4. The test does not alter WS1 ranges; actual GLB/procedural scene nodes retain native renderer behaviour. Active-world WS1 registration/unregistration remains compatible via existing node-added/removed hooks, but active-world migration requires a separately validated integration.

Local tests and expected results: [WS2 handoff](../../DEBUG/WorldStreaming/README.md#ws2-static-streaming-proof). Implemented in source, awaiting compilation/F6/performance acceptance. No measured frame-time or memory claims.

## WS2 active World / inherited FPS integration

**Enabled by default for the four village Building001–004 instances.** `WorldMaster → StreamVillageBuildings` switches this startup integration; `Streaming` holds minimum distances/caps. Set false and restart for the previous always-resident buildings. Non-village/showcase mode remains unchanged. The convenience-store site (including its lot/cars/lamps/hedges), roads, vegetation, terrain, caves, F1 previews, player and all other actors remain with their current owners. Do not read this as universal world streaming or WS3 persistence.

`WorldStaticStreaming` initializes **after** terrain grading, visibility, section capture, NPC and patrol startup. It filters the captured five-placement manifest to the four canonical static buildings, validates all source nodes, then uses `StaticSectionStreamer.Adopt` on those exact instances: no startup duplicates, reparenting, new generation or changed floors/transforms. On reload it preserves the captured world transform, stable ID and original Building001–004 names. VillageLayout drops its startup node list after handoff; its value-only roads/reservations/footprints remain for terrain and actors. Canonical defaults are reconstructed on return; future mutable state/overrides need WS3 before inclusion.

The bridge caches CharacterBody3D membership under PersistentEntitiesRoot through node add/remove signals. Before a proposed unload it checks that bounded actor set against the placement footprint plus 8m: an occupied/nearby building stays resident. Blocked placements sleep and are reconsidered at the next player movement batch (8m), conservatively retaining them longer if an actor leaves while the player stands still. No AI/collision/health owner is deactivated. This is static footprint protection, not WS4 actor streaming or general combat/interaction leases.

Preload is at least the full camera far clip + maximum permitted camera orbit + a 32m band, rounded upward to 16m; unload preserves at least the configured hysteresis band. Current top-down defaults resolve to **320m load / 400m unload**. FPS resolves from its own current far clip (normally 220m); fog-off FPS far clip can make all four buildings resident. `Camera.RenderRangeChanged` announces explicit FPS far-plane changes after applying them. Settings/orbit notifications perform only constant-cost envelope comparison; unchanged envelopes never walk scenery or force streaming refreshes. Larger explicit view ranges may require an asynchronous warm-up; production teleport/vehicle readiness is outside this pass.

The top-centre section heading now appends **Buildings N/4 · L…/U… · asleep/loading**, with pin/failure counts when present. L includes the four adopted startup scenes; the F3 footprint map still represents descriptions, not live nodes. Heading updates come from existing movement/residency signals. Scene exit detaches bridge/actor/camera/HUD callbacks; streamer owns only its selected four placements.

Reload attaches under the original Structures root, so `CameraOcclusion.RegisterStructures` / StructureVisibility Ready/Exit hooks restore top-down floor/roof/exterior fading. FPS keeps its existing opaque building presentation. WS1 continues to register/unregister native meshes through its existing node lifecycle hooks, with no new fading writer or camera loop. Ground support remains the existing terrain; no rebake required. Resource reads are threaded, but one procedural building's Instantiate/Ready/teardown can still cause a first-use spike; operation counts are bounded, not a guaranteed time budget.

Source/reference/whitespace checks passed. No Godot/.NET here: integration smoke test, collision/fading round trip in both views and frame timing remain **awaiting local validation**. See the debug README for exact checks. The owner previously demonstrated the isolated proof's unload/return counts; that is not evidence this new building integration has run.

### FPS-only distance tuning follow-up

FPS default fog now reaches 115m (+15%), linked far clip 235m. The FPS eye-camera offset allowance is 4m and its copied profile uses a configurable 64m hysteresis band, yielding 272m preload / 336m unload (previously 288/368). Top-down defaults remain 320/400. The owner's 50s observation scales to roughly 45.7s on the same sprint route; distances drive residency, never elapsed time. Fog-off clip expansion, actor pins and scheduler limits remain. See the FPS README for calculations and local checks; runtime acceptance pending.
