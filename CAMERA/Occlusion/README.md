# Scenery obstruction fading

Camera-owned logic lives here, separate from CAMERA/Camera.cs projection/follow code. WorldController attaches CameraOcclusion beneath the active camera and registers the pine provider. **Only world objects fade: entities/players/zombies/robots are excluded.**

| Phase | Scope | Assistant | Status |
| --- | --- | --- | --- |
| OC0 | Scenery-only camera corridor check, 50% fade, pines and reusable object component | Codex / ChatGPT Work | Implemented, Awaiting Local Validation |

## Behaviour and defaults

CameraMaster/CameraSettings own ObstructionFadeEnabled=true, ObstructedOpacity=0.5, ObstructionRadius=0.45m, ObstructionChecksPerSecond=8 and ObstructionFadeSeconds=0.2. Values also appear in Copy settings. The query uses the upper-body screen ray through the player, respecting orthographic offset rays as well as perspective. A narrow capsule captures more of the player silhouette than a single thin line. Multiple obstructions can fade together; the normal opacity restores after they clear. The player uses a lower target point when crouching.

Player/camera motion, stance changes and scenery layout/movement changes coalesce into at most eight checks per second. **No query timer runs while stationary.** Queries run in physics. Interpolation uses a frame callback only while opacity transitions; settled faded objects do not keep that callback alive. OFF via ApplySettings restores all tracked objects and stops checks. Exit restores valid objects and disconnects all external events. No full-scene searches, per-tree nodes or per-frame physics queries.

## Pine batches

PinePlacement implements ICameraOcclusionProvider. It caches each procedural canopy's world bounds plus a broad bound per chunk when constructing MultiMeshes. Checks reject chunks before testing individual canopy bounds; no physics proxy is created for each tree. Bounds are conservative, so a sparse branch gap can still trigger a fade. Tree layout/eviction changes invalidate the camera once.

One MultiMesh custom-data red channel controls that tree's foliage in **both near/far LODs**. Bark stays opaque. LeafWind uses ordered screen-space coverage dithering: at 0.5 about half the canopy pixels remain, giving see-through coverage without switching all foliage into alpha sorting. This is visibly stippled rather than smooth glass transparency. Intermediate coverage steps follow the camera fade interpolation. Other foliage using the shader defaults to ignoring this channel. Arbitrary imported tree shaders require an adapter before they can use per-tree fading.

## Buildings, walls and other objects

Add a Node3D child with `CameraOccluder.cs` to the object's prefab. ObjectRoot defaults to its parent. The component collects that object's MeshInstance3D children once (skipping character subtrees), after procedural parent Ready has finished, and creates one Area3D box proxy on **physics layer 8 (bit 128)**. This is a camera-only query layer, with collision mask zero; it does not add solid collision or weapon targets. Ancestors that are CharacterBody3D are rejected, preventing use on current entities.

Ordinary object fading uses GeometryInstance3D.Transparency on each mesh, preserving authored transparency and source materials (Forward+). Object-sized bounds fade the complete object; no per-wall segmentation or roof/indoors visibility system is implied. **Building001.tscn already includes this component**, ready whenever placed in the active world; this job does not place a new building. Generated geometry must exist before its deferred bounds setup. Objects deforming/rebuilding later need their own bounds-refresh integration. Moving the object notifies the camera through transform changes, without polling.

## Brief handoff

Source/resource references, active settings, entity-exclusion/layer routing, dual-LOD custom data and cleanup paths reviewed; git whitespace check passed. **No Godot/.NET is installed here: C# build, physics integration, shader compile and visual/performance acceptance are unverified.**

Locally build and F6 WORLD/World.tscn, walk behind/clear pines in both projections, crouch, zoom through LOD transitions and verify only obstructing canopies stipple/restore. Watch entities cross the view: they must remain fully opaque. Check pine OFF/density rebuild while faded, scene unload, and idle/transition callbacks. Place Building001 with the existing world placement workflow (or instance in a small local test) and verify its mesh fade/restoration and intact solid collision. Toggle the camera setting off through ApplySettings and confirm immediate restoration. No terrain rebake required.

## Section and floor composition

CameraOccluder optionally resolves VisualRootPath relative to ObjectRoot/parent at deferred setup. Only that subtree is collected/proxied; an empty path preserves prior behavior for existing prefabs. CreateObstructionProxy=false supports presentation-only owners (decks/furnishings). SetPresentationOpacity composes with authored opacity and SetCameraOpacity, using one writer per mesh; SetCameraFadeEnabled(false) restores the camera factor while retaining floor presentation. No global material mutation. Values only change on explicit events/transitions.

Building002 installs disjoint section owners via Shared/BuildingFloorVisibility in top-down World initialization. Player movement updates local-space floor/stair occupancy, and short presentation transitions sleep after settling. Lower walls restore upstairs, upper deck/furnishings restore at landing, roof stays faded indoors. Stair meshes/collision remain untouched. Whole-prefab proxy removed from Building002. FPS override skips this initialization entirely.

Building003 now has independent shop-roof/canopy proxies selecting only generated roof subgroups. Pumps, contents, forecourt and posts no longer receive whole-station fading. StationShell walls instead use individual camera obstruction targets. Camera opacity/duration remain CameraMaster settings. No automatic interior unloading, gameplay sleeping, or engine occlusion-culling feature is introduced by this visual change. Local Godot/.NET build and visual acceptance pending.

FadeIndividualMeshes enables per-mesh wall proxies under one cached section owner. CameraMeshFadeTarget is a lightweight RefCounted handle (no callbacks), so the existing bounded camera-to-player capsule query can fade only blocking pieces instead of whole wall groups. The query still returns at most 64 hits; this first-pass building population is bounded, not an unlimited crowd/structure solution. Shared presentation multiplies with each wall's own camera factor. Restored ground walls ignore their camera factor upstairs. No per-wall tick or extra raycasts.

Outside-house obstruction fallback: BuildingFloorVisibility now registers as an explicit camera occlusion provider/target. If the camera-to-player corridor intersects its cached whole-house bounds while the player is outside, the entire lodge (including deck, stairs, furnishings and trim) fades using CameraMaster's obstruction opacity/duration. Leaving the obstruction restores it. Indoors that factor is ignored, preserving occupied-floor furniture and stair readability. Each disjoint mesh owner multiplies the outside factor with existing presentation/camera factors; there is no second transparency writer. The check joins the existing event-driven camera cadence (no extra ray, physics proxy, timer or frame loop); bounds are collected once and endpoints transformed to current building space. This fallback applies to the lodge only: petrol station retains roof/wall-only fading, solid pumps and contents. Source checks only; local Godot build/visual validation pending.

## Outside-building opacity — current shared implementation

Owner authorized this for **Building001, Building002 and Building003**. Each owning Settings/Master now exposes OutsideBuildingOpacity=0.15 (0 invisible, 1 opaque). Shared BuildingObstructionVisibility replaces the lodge-only fallback; BuildingFloorVisibility now owns indoor floors only. CameraOcclusion accepts ICameraOpacityOverride; trees and ordinary scenery retain CameraMaster's global 0.5 and fade duration.

Outside, each building participates as one coherent target. Whole-building mode suppresses individual wall/roof targets and their camera factors; authored opacity and floor presentation still compose through one writer per mesh. Existing section owners are cached once, and a passive owner handles unowned art using a cached exclusion set. Camera-to-player bounds checks reuse the existing provider cadence, without extra rays, physics proxies, timers, runtime mesh scans or idle polling. Active transitions settle and sleep. Player movement updates local occupied zones; bounds use current building transform.

Entry restores sectional participation, and the existing animation restores the building factor smoothly to 1. Depot keeps its prior interior fade. Lodge keeps floor/stair/roof/wall rules. Station includes shop and open forecourt/canopy occupancy, so standing at pumps leaves contents solid; **outside behind the station**, its full art now fades as authorized. FPS skips camera occlusion composition; collision, LOS and geometry unchanged.

**Layer stacking:** try the lower per-surface opacity first. No capped coverage heuristic or per-frame mesh counting added without visual evidence. Actual soldier readability at 0.15 is not verified; overlapping surfaces may still obscure him. Per-master values can be tuned down to 0 if necessary. This supersedes older lodge-only/global-opacity descriptions.

Validation: source ownership/composition/lifecycle, target defaults, numeric opacity endpoints, resource wiring, mandatory for/foreach syntax review and whitespace checks. No Godot/.NET executable here: compilation and visual/performance acceptance pending. Locally run `dotnet build war-machine.csproj`, F6 WORLD/World.tscn, walk behind all three buildings from several angles, compare opacity 0/0.15/1 after restarting, move clear/enter and stop while faded. Check lodge stairs/floors, station pumps, trees and FPS. No rebake required.
