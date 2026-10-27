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
