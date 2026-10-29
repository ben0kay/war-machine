# WS1 — Fog-aware native render ranges

**Implemented in source; awaiting local build/visual/performance validation.** [World roadmap](../WORLD_STREAMING_IMPLEMENTATION_PLAN.md). This is rendering relevance, not section unloading or actor simulation control.

`WorldRenderCulling` is created once under SystemsRoot in the active world and inherited FPS test. It observes only `WorldActorsRoot/PersistentPropsRoot` and `SystemsRoot/VegetationRoot`: existing buildings, lot, cars, hedges, lamps, F1 GLB previews, pine/scatter mesh batches. One startup traversal registers supported native MeshInstance3D/MultiMeshInstance3D and local OmniLight3D/SpotLight3D; node-add/remove events handle later assets and rebuilds. Deferred registration waits for procedural geometry, GLB wrappers and final spawn transforms. The catalogue does not instantiate anything for this system.

> **SOC1A partial:** Per-asset `ShadowMode` is implemented via the shared GLB size master and one-time `PlaceableVisual` traversal. `Inherit` is default; mode `Disabled` removes that GLB's shadow casting (and naturally avoids WS1 shadow buffer for those meshes), with no changes to culling logic. Per-asset `RenderDistance` caps in [SOC1A plan](../Content/Assets/Objects/SOC1_OBJECT_DIAGNOSTICS_PLAN.md#soc1a--shadowmode--renderdistance) remain **unimplemented**.

## Policy and configuration

`WorldMaster.tres → RenderPolicy` owns Enabled, HiddenBuffer (8m), CameraRefreshStep (2m), ShadowBuffer (48m), DeformationPadding (2m), FadeLocalLights and LightFadeLength (8m). All are consumed. Existing World startup duplicates these resources; UI edits never write source masters. The debug panel's **Fog-aware rendering (WS1)** section offers an A/B switch and hidden/shadow buffers in both views; Copy settings includes their values with WS1 prefixes for identification (these are snapshot keys, not new resource property names).

The actual `PlayerDistanceFog` publishes an applied profile: metric, fully opaque status and the outer fog radius including maximum edge variation. Overhead uses player-horizontal X/Z distance; FPS uses its own camera-spherical fog profile. Only fully opaque enabled distance fog with an opaque backdrop authorizes a new geometry cutoff. The current FPS clear-sky profile deliberately retains its existing camera far clip and authored geometry ranges: otherwise hiding a fogged building can reveal brighter sky behind it. FPS local-light fading remains active, and geometry culling can opt in when FpsSkyFogOpacity is 1. The primary overhead profile already has an opaque backdrop. Translucent/OFF fog or disabled WS1 restores baseline native geometry/light settings. Existing tree/scatter authored caps remain authored caps; disabling WS1 does not remove pre-existing culling.

Native visibility ranges measure camera distance to the instance AABB centre. The policy converts the fog envelope conservatively using world-space mesh/batch bounds, maximum allowed camera orbit, object height, extra cull/deformation padding and a movement safety band. It adds a shadow buffer for shadow-casting geometry. Large objects/batches can remain rendered farther away than small ones; one exact distance for every object would be unsafe. Native culling then follows the camera automatically. No new material transparency, custom fade shader or per-object C# distance loop is used.

Local light fade includes the light's influence range and a conservative buffer, so a lamp outside the visible area can still illuminate nearby visible ground. Existing authored light fade curves remain untouched; WS1 only supplies a missing distance fade. It never writes fixture Visible/energy/ShadowEnabled or enables more shadowed lights. OFF and scope removal restore the original fade values. Global sky/sun/moon and transient muzzle lights are outside these static roots.

## Existing behavior preserved

- Imported GLBs and procedural meshes use the same bounds calculation; no copies or new meshes/materials are generated.
- Pine near/far LOD begin ranges, instance batches, shared materials/wind, trunk physics and chunk ownership remain. PinePlacement publishes its current authored end through `world_authored_range_end` metadata so zoom/profile edits and OFF restoration do not accumulate shorter cutoffs.
- Imported mesh LOD remains an importer/renderer feature. Generate LODs is enabled by default for imported GLB scenes; this job does not re-export models, add fake hand-authored LODs or change LOD bias. Check actual imported LOD appearance locally.
- No Visible/Transparency writer competes with StructureVisibility or camera obstruction fading. Collision layers/shapes, weapon hits, NPC logic, hearing and saving are untouched.
- GPU ribbon grass retains TerrainGrass's existing tile/shader relevance. Native Terrain3D server geometry, caves not in the active prop roots, moving actors, player/equipment, weather/particles and global effects are outside this adapter. They retain their existing owners/ranges; this job does not claim universal culling of all engine resources.

**Forest replacement boundary:** PineTree001 GLB previews now receive this policy automatically, including native imported mesh LOD where generated. The active procedural forest is not replaced. The existing PineSettings.ImportedMeshes route retains its batching/collision owner and is covered as MultiMeshes, but a complete GLB-scene-to-forest artwork conversion and compatible foliage wind/far-art selection need a separately approved art integration. Simply placing thousands of visual GLB scenes would lose the forest's efficient ownership.

## Authoring hooks and limitations

- Metadata `world_render_exempt=true` on a visual or any ancestor opts that subtree out. Useful for landmarks, unusual shaders or shadow casters that must remain relevant. To change exemptions/geometry/transforms at runtime, call the world's FogRenderPolicy `Refresh()`; after removing an exemption from previously unregistered nodes use `RefreshSubtree(root)`. Moving GLBs interactively will need this explicit authoring hook; no transform polling is added.
- Other owners that change visibility end ranges after registration should set `world_authored_range_end` to their intended uncapped end, then call Refresh. PinePlacement publishes metadata; owners must call Refresh after changing authored ranges. Unannotated static authored values are captured at registration.
- ShadowBuffer is a tuning safety band, not proof that arbitrarily long low-sun shadows/reflections are invisible. Check dawn/dusk/night locally; exempt an exceptional caster or increase its safety policy before accepting visual changes. Depth-overlay fog and transparent/deformed materials also require visual checks. No claim of pixel-perfect fog coverage or measured FPS gains.
- Shorter original render ranges are preserved, even when they would already pop before fog. WS1 does not silently extend authored draw distances or rebuild coverage. Fix an inappropriate original owner distance explicitly after observation.
- No object is freed, no actor deactivated, no terrain rebaked, no giant-world residency or memory saving promised. WS2+ remains separate.

## Activation, idle and lifecycle

Fog/policy settings, additions/removals and explicit edits trigger deferred work; no repeated scene-tree scan. Camera setting and motion signals now perform only a constant-cost guard. The conservative envelope covers the maximum allowed zoom/orbit plus a movement band, so scroll, smoothing, yaw and projection changes do not refresh all scenery. Player elevation changes by CameraRefreshStep or exceptional follow lag outside the envelope trigger a refresh. Native culling follows the camera automatically. No Process/PhysicsProcess/timer in the policy. OFF/translucent detaches the view subscription. Removal and exit restore authored values.

**Zoom regression fix (2026-10-28), awaiting local validation:** previously every camera SettingsChanged forced a full scenery pass, with additional passes during perspective smoothing. This caused reported scroll hitches when WS1 was enabled. FullRefreshCount is available for diagnostics; the smoke scene checks repeated zoom/orbit/projection/smoothing leaves it unchanged. The wider conservative envelope may retain some hidden geometry longer. This addresses the identified CPU refresh path; steady FPS and actual frame times still require local comparison.

Run the actual-code smoke scene and manual regression in [DEBUG/WorldStreaming/README.md](../../DEBUG/WorldStreaming/README.md). Neither Godot nor dotnet is available here; compilation, GPU render results and profiling are pending.

Engine references: [visibility ranges](https://docs.godotengine.org/en/stable/tutorials/3d/visibility_ranges.html), [native light fading](https://docs.godotengine.org/en/stable/classes/class_light3d.html), [imported mesh LOD](https://docs.godotengine.org/en/stable/tutorials/3d/mesh_lod.html).
