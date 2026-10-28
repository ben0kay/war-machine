# WS0 section diagnostics

**Top-centre heading:** current player X/Z section, updated on boundary crossing. WorldMaster → Sections controls ShowHeading, ShowGrid, SectionSize, Origin, LogicalBounds and WorldId. Default 128m cells put player spawn at (4,4). Heading reserves the top row; existing world help moves down rather than overlapping it.

**F3:** toggle the read-only X/Z grid (positive X right, positive Z down). The centre square is the player's section, not an exact moving player marker. Nine cells show IDs/owned-placement counts; gold outlines show clipped footprint overlap. A building may touch several cells but contributes one owner count. This grid does not report loaded/unloaded state because WS0 does not stream anything. It does not capture mouse or pause gameplay. F1/F7/F8/F9 keep their current actions.

`WorldSectionDiagnostics` binds existing player movement signals, does not poll the player and disconnects at exit. `SectionMap` redraws only at section changes or when shown. Disable both heading/grid in the master for no movement subscription. FPS uses the same shared composition. Settings/manifest validation failure warns and disables only this diagnostic, leaving world startup running.

## Local contract smoke test

Neither Godot nor dotnet is installed in the implementation environment. These actual-code tests are **provided but not run**:

```sh
dotnet build war-machine.csproj
godot --headless --path . res://DEBUG/WorldStreaming/SectionManifestTest.tscn
```

Use your installed Godot .NET executable path/name. Expected `[WS0] PASS` and exit 0; exceptions produce FAIL and exit 1. Tests cover negative/exact-edge/offset coordinates, half-open finite bounds, four dependency references with one owned placement, schema/world identity, transforms/seeds/overrides round-trip, duplicate IDs, unknown assets and invalid owner/version. No terrain or reusable asset scene is loaded by this test.

F6 World and FPS: heading shows (4,4) at default spawn; cross X=512 or Z=512 to see cell changes; F3 shows five records in village mode and closes cleanly. Confirm buildings/store stay present and retain collision/fading. No duplicate cars/lamps or changed actor activity. Inspect a footprint crossing a line: gold spans cells but owner count is one. Resize a normal desktop viewport (e.g. 1280x720 and 640x360), verify top heading/help, then run with both display flags false. The optional map scales down on viewport resize; the heading remains anchored. Stop/restart and check IDs/record count are stable. There is no disk save, async loading, fog change, actor lifecycle change or WS1 implementation in this job.

## WS1 native render policy tests and local A/B

```sh
dotnet build war-machine.csproj
godot --headless --path . res://DEBUG/WorldStreaming/RenderPolicyTest.tscn
```

Expected `[WS1] PASS` / exit 0. This actual-code test is supplied but **not executed here** (no Godot/dotnet). It checks conservative bounds, authored LOD ends/begins, applied fog edge, exemption, local-light Visible independence, translucent/OFF restoration, reactivation and scope removal. Build/import is mandatory before trusting source completion.

F6 World and FPS. Use the debug panel's Fog-aware rendering (WS1) A/B toggle. F1 spawn PineTree001 and ordinary GLB furniture, then walk/zoom away and back; check shapes remain full-detail nearby, native LOD where generated, and no early disappearance. Inspect Native VisibilityRangeEnd / light fade values in the Remote tree; geometry/physics nodes must remain resident. In FPS, the default clear sky preserves its existing far clip/authored geometry ranges; new geometry caps require FpsSkyFogOpacity=1, while local light fades still apply. Test fog disabled, opacity below 1, edge/end sliders, perspective/orthographic, crouch/jump, night lamps, dawn/dusk shadows, repeated F1 spawn/clear, pine density rebuild and scene reload. Fixture OFF must stay OFF; no new shadow flags. Check imported models and generated tree near/far levels do not compete.

Profile the same view with WS1 ON/OFF after imports/shaders settle; record draw calls, triangles, frame timing and registered instance count via FogRenderPolicy.RegisteredCount. Do not compare different camera views or claim memory-unloading improvements. Tall/shadowed geometry may remain farther for safety. GPU grass/native terrain and actor relevance retain their existing owners; this test does not prove those stream. See [policy details](../../WORLD/Rendering/README.md).

## WS2 static streaming proof

Open **StaticStreamingProof.tscn → F6** (no terrain bake needed). WASD moves; D east / A west; Shift faster. Top-centre text shows current section, resident/desired counts, retained requests, cumulative loads/unloads and working/asleep. Walk from X=32 past the five paired markers up to X=640, then return. The car is procedural C# with solid collision; the hedge is the canonical imported GLB with its existing visual-only contract. Walk around cars with W/S.

Expected: upcoming pairs load before they enter the fixed camera reach; distant pairs leave StreamedStaticRoot, returning recreates one instance per stable ID. A car crossing section lines does not duplicate. The scheduler reads asleep at rest after work finishes, including empty areas; requests <=2, resident roots <=16, <=1 attachment/removal per process frame. Inspect Remote tree for actual disappearance, not just culling. Ground/marker labels are persistent test helpers. No active-world village migration, actor streaming, terrain streaming, persistence, fog change or WS3 behaviour is included.

```sh
dotnet build war-machine.csproj
godot --headless --path . res://DEBUG/WorldStreaming/StaticStreamingProof.tscn -- --ws2-test
```

Supplied but **not run here**: no Godot/.NET executable. Expected `[WS2] PASS` / exit 0. Automated checks cover procedural + GLB loading, four footprint cells/one ID, boundary crossings, hysteresis, unload/return, obsolete request draining, caps/sleep, and a physics ray against the returned car. Stop/restart the F6 scene during loading and when settled; verify no orphan proof scenes or permanently active StaticSectionLoadDrain nodes. Repeat a few loops and inspect car collision. Compile/import before treating this as accepted.

Profile first visit and warm return separately for frame-time spikes and memory; resource import/shaders/physics and procedural Ready can still stall. Threaded resource reading is not threaded scene instantiation. Briefly open World.tscn/FPS afterward: neither should gain a streamer or lose existing scenery. Share build errors, visible pop-in, count growth, or collision failures before active-world opt-in. WS3 remains unimplemented.

## WS2 active World/FPS integration checks

The separate proof has now been followed by a **Building001–004 integration in normal World.tscn and inherited FirstPersonTest.tscn**. Normal arrow-key controls remain unchanged. Build C#, F6 World, and check top-centre `Buildings 4/4 · L4/U0 · asleep` after startup. Walk well away from the village (default top-down unload is roughly 400m from each building footprint), then return. Expect count/unload changes, restored original names/transforms and no duplicates. Store site, roads, terrain, grass/trees and all actors retain existing lifetime; F1 previews are not persistent streamed placements.

Inspect Remote `WorldActorsRoot/PersistentPropsRoot/Structures`: the four selected roots must actually disappear and return; the convenience-store root stays. Enter returned buildings and check wall/floor/stair collision, roof/floor reveals and exterior fading, including zoom/orbit. Default terrain needs **no rebake**. Check WS1 on/off still behaves correctly. NPCs close to a selected footprint pin that building (heading shows pinned count); move away again after the NPC leaves to allow reconsideration. Do not expect the pinned scheduler to poll while stationary.

Briefly repeat unload/return in FPS; it keeps opaque buildings and uses its own clip-derived preload distance. Disabling FPS distance fog expands residency to cover its original far clip (so all four may stay loaded). Explicit range expansion may need a warm-up; this pass is not teleport/vehicle readiness. Ordinary scroll/orbit must not force full scenery refreshes or cause a new always-running streamer. Stop/restart while loading and settled; watch for stale node/visibility errors. Measure first-return procedural generation separately from warm traversal; no stall-free claim has been made.

Optional actual-code integration contract smoke test (provided, **not run here**):

```sh
godot --headless --path . res://DEBUG/WorldStreaming/ActiveStreamingContractTest.tscn
```

Expected `[WS2 integration] PASS` / exit 0. Tests adoption without duplicates, sleeping pins and release, preserved identity/name/transform on canonical procedural reload, envelope expansion and top-down/FPS/fog-off distances. It does not instantiate the terrain/village or replace the manual collision/fading checks. Set `WorldMaster.StreamVillageBuildings=false` and restart to compare the previous resident-building behaviour. WS3 remains planned; no mutable saves are implemented.
