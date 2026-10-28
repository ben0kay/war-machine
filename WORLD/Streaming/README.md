# WS0 — World sections and placement description

**Implemented; awaiting local build/runtime validation. No scene streaming or gameplay save system yet.** [Roadmap](../WORLD_STREAMING_IMPLEMENTATION_PLAN.md).

- `WorldSectionSettings` is nested in `WORLD/Configuration/WorldMaster.tres`: world ID, grid origin, 128m section size, finite logical bounds (current prototype 0–1024m X/Z), heading and initial grid visibility. These bounds describe the current test area, not a promised final world size. No terrain allocation/bake follows from increasing logical bounds.
- `SectionCoordinate` uses floor division, including negative coordinates. World and footprint bounds are half-open. The player can leave logical bounds without changing gameplay; the heading reports that explicitly.
- `WorldPlacementRecord` stores an authored instance ID, stable asset ID, owner section, seed, 12-float world transform, four-float X/Z footprint and string overrides. No Godot object ID, retained Node, PackedScene or loaded artwork is serialized. Current captures have no custom overrides; generic override data round-trips but is not applied to scenes in WS0.
- `WorldAssetCatalog` maps five stable prototype IDs to canonical scenes using strings only. Resolving a path does not load it. It is an explicit prototype catalogue, not discovery of all future assets. Change the path behind an asset ID when moving an asset; do not replace saved IDs with paths.
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

An anchor determines one owner. `CoveredSections` indexes overlapping footprint cells inside finite bounds; later streaming must load the owner's instance when a dependency cell needs it, rather than placing a second copy. No gameplay object is reparented or unloaded. Current data is a diagnostic manifest of representative existing placements, not an authoring save capable of rebuilding the whole scene.

## Diagnostics and local checks

The normal world and inherited FPS show **Section (X, Z)** at top centre; default player spawn is Section (4, 4). **F3** toggles the read-only nine-cell grid. The gold boxes are recorded footprints, not residency or a terrain overlay. See [diagnostic/test README](../../DEBUG/WorldStreaming/README.md).

No process/physics callback is introduced. Existing player PositionChanged signals perform constant-time section lookup; labels/map redraw only at cell/bounds changes or explicit toggles. With both heading and grid off, movement subscriptions are removed; F3 input remains available. Grid nodes hold data only, never own gameplay entities. Exit detaches subscriptions. Startup's bounded five-prefab bounds capture/JSON validation is not repeated during motion.

Source/resource/whitespace checks ran; neither Godot nor dotnet is available here. Build, the actual C# contract test, F6 heading/grid, FPS, disabled diagnostics and object-count checks remain pending. WS1–WS7 remain unimplemented.
