# IE — Shared interior volumes and view-specific environment exclusions

**Status: Plan only. IE0–IE4 Planned; no effects or gameplay changed.** Development branch: `implement-td0-td1`, never main unless explicitly requested. Implement only separately authorized phases/jobs, one commit per job, with brief proportional validation. Read root README, CodePreferences, ENVIRONMENT/README, SHARED_ENVIRONMENT_README, Weather/README, actual effect scripts/shaders and structure authoring docs before implementation.

## Owner direction / exact scope

Buildings should publish interior footprints/volumes so effects can respect interiors even when the player stands outside looking through a window or doorway. The same world data should be reusable by multiple environmental consumers, with **independent per-effect/per-view policies**.

**Explicit correction: hide/exclude rain only in FIRST PERSON. Top-down rain must remain visible and retain its current behavior, including while the player is inside.** Top-down is a gameplay view, not synonymous with orthographic projection: the overhead camera can also be perspective. Pass an explicit view policy rather than guessing from camera projection, height or current player occupancy.

| Effect | Top-down | First person | Scope |
| --- | --- | --- | --- |
| Local terrain ground haze / wisps | Exclude declared enclosed interior volumes | Exclude the same volumes | IE2 |
| Rain | Preserve existing rain; no new indoor mask, shutdown or shelter toggle | Suppress rain within declared interiors / under declared roof shelter | IE3 only |
| Global distance fog, sky, sun/moon and general atmospheric depth | Preserve | Preserve | Not an interior exclusion target |
| Snow, airborne motes, weather-addon local mist, future effects | Preserve current behavior initially | Preserve current behavior initially | Reusable policy extension later; not silently included in rain handling |

Outdoor rain should still be visible from an FPS interior through openings. Do not simply stop the entire player-centred weather emitter upon entering a building if that removes the exterior weather too. Likewise do not disable all ground haze globally when the player enters a store: outside haze should remain visible through windows.

The earlier site-grounding simplification (lower slabs/remove access wedges) is **a separate world/physics job**, not part of this environment plan.

## Confirmed source / constraints

Reviewed on the development branch at planning:
- GameEnvironment owns one shared environment instance and runtime EnvironmentConfig for both views; InitializeView binds WeatherBridge and GroundHaze to the explicit player/camera. Sky3D remains the sole sky/WorldEnvironment owner.
- GroundHaze builds one local FogVolume and GroundWisps fog shader, with cached terrain-height data and movement/tile-driven refreshes. It currently has no building-volume exclusion. Existing legacy initialization also supports cave/flight suppression.
- WeatherBridge instantiates TestWeather, uses existing addon particle materials, and applies explicit weather settings/coverage. Its legacy explorer path can suppress effects based on cave shelter; active player InitializeView does not register building shelter. Current ApplySettings uses one broad outdoors state, which must not become the new all-effects/all-views interior switch.
- StructureLayout exposes structure_occupied_bounds plus floor metadata. These support camera occupancy, not a declaration that every volume is enclosed. Petrol forecourt/canopy occupancy is intentionally different from an enclosed room; large merged building bounds include exterior space and cannot be trusted as interior masks.
- StructureVisibility is skipped by FPS camera-occlusion setup. Environment volume registration must work independently of that camera-fade lifecycle; FPS must still receive building interiors when physical art stays opaque.
- World and inherited FPS share the building/site prefabs, so metadata belongs with those prefabs, not duplicated in an FPS scene or a hardcoded building-name list.

## Phase table

| Phase | Goal | Suggested assistant | State |
| --- | --- | --- | --- |
| IE0 | Audit actual effect/render paths, building interiors and minimal view-policy contract | Regular Chat source review; Codex for shader/particle feasibility | Planned |
| IE1 | Shared world-local interior/shelter registration and prefab declarations | Codex / ChatGPT Work | Planned |
| IE2 | Spatial exclusion of local ground haze in both views | Codex / Work; owner local visual acceptance | Planned |
| IE3 | First-person-only rain shelter/exclusion; top-down unchanged | Codex / Work; owner local weather check | Planned |
| IE4 | Bounded local regression, lifecycle/performance handoff and docs | Owner/Copilot local checks; Codex targeted fixes | Planned |

Suggested implementation batches: IE0+IE1, then IE2, then IE3. Each needs explicit authorization; neither this plan nor a completed phase authorizes the next. IE4 accompanies each implemented consumer as appropriate; no large test suite by default.

## IE0 — Audit and freeze a small contract

**Objective:** Identify what actually needs masking before adding a registry or replacing materials.

Read GameEnvironment.cs, Weather/GroundHaze.cs, Weather/Shaders/GroundWisps.gdshader, Weather/WeatherBridge.cs, TestWeather.tscn/TestWeatherController.gd, used addon particle scripts/materials, FirstPersonWorld/FirstPersonCamera, World initialization and each building's metadata/mesh generator.

Record which contribution is local ground haze, addon mist or global volumetric/distance fog. Excluding one must not be claimed to remove all fog. Establish the current supported renderer/shader capabilities and a viable spatial mask for the actual rain particles. Prefer existing bounded GPU particle shelter/collision facilities if compatible; otherwise propose a minimal effect adapter before replacing the addon's whole shader/material pipeline. Do not claim that a registry by itself clips pixels or particles.

Audit enclosed shop/stockroom, depot/lodge/office interiors, floors/stairs and open petrol canopy separately. Inspect windows/door thresholds and roof coverage. Confirm IE3 affects rain only; document separate future opt-ins for snow/motes/addon mist.

**Pass:** Small data model, explicit view selection, concrete haze/rain consumer strategy, bounds/budget and actual authoring examples recorded here. No production effect changes or new always-running debug overlay. No renderer/addon upgrade.

## IE1 — Shared spatial declarations, independent of camera fading

**Objective:** Register reusable environmental volumes once per world, not separately for each effect.

Proposed ownership: ENVIRONMENT/Interiors/ for a small data/registry consumer API and README; ENVIRONMENT/GameEnvironment owns the world-local registry. Buildings own authored local volumes or a small prefab component/metadata adapter under WORLD/Content/Assets/Structures/Shared. Create only files needed by the implementation.

Declare **enclosed interior volumes** and **overhead shelter volumes** separately. Use a few local boxes / inverse transforms for rotated structures, with real vertical extents and floor-specific boxes. Irregular footprints use a union of small boxes, not one giant world AABB; do not mask open yards, porches, parking areas or under-floor exterior air by accident. Start with enclosed rooms at the convenience store plus existing village buildings, and roof-only shelter at the petrol canopy. Do not automatically classify occupied_bounds as sealed interior; use it only as an authoring hint where verified.

Register/unregister on prefab ready/exit, transform changes and explicit Rebuild/Refresh. An environment adapter may reuse existing metadata utilities, but must not require StructureVisibility.Attach or create a camera-occlusion context in FPS. Scope registrations to the explicit current world Structures root, with one initial discovery plus lifecycle events; no recurring whole-scene scans/static cross-world volume list. Building destruction/removal must release volumes. Source masters/metadata stay immutable at runtime.

Registry holds identities, local bounds, world transforms, classifications and explicit consumer flags. Consumers select nearby volumes intersecting their rendering/emission coverage and subscribe to meaningful layout changes. EnvironmentConfig supplies grouped consumer enable/budget settings if needed; geometry stays authored in the prefab, not duplicated in a global config. View policy is injected by the active view adapter, not inferred from perspective vs orthographic and not coupled to DEBUG class names inside shared environment code.

**Preserve:** Camera fades, interior floors/stairs, LOS/weapon/actor collision and all geometry. No physics body is required merely to advertise a logical volume.

**Pass/performance:** Store, two-storey building and open canopy register correctly in both views, move/refresh/unload without stale masks. No per-building process callback; no actor/body scans. Bound selection/upload costs and document overflow behavior rather than silently ignoring excess interiors.

## IE2 — Local ground-haze exclusion in both views

**Objective:** Enclosed rooms remain clear of the local ground wisps regardless of where the player stands.

Extend existing GroundHaze/GroundWisps shader with world-space interior membership masking. Prefer a small fixed-capacity GPU volume buffer/uniform set for nearby volumes intersecting the current FogVolume, uploaded on volume changes and existing coverage/tile/view changes. IE0 must confirm the actual fog-shader coordinate space and supported data representation. Clip density inside volumes, with restrained boundary feathering toward the exterior that does not leak haze through walls. Keep open door/window sightlines to outside haze, stair/floor vertical separation, rotated volumes and open canopies correct.

The player being indoors is not the mask. Do not change global fog density to solve local wisps. If other effects/global volumetric contributions remain visible indoors, record them explicitly; do not overpromise an entirely fog-free interior. Keep existing terrain-height approximation and cave/legacy API working.

**Owners:** GroundHaze.cs, GroundWisps.gdshader, IE1 registry and EnvironmentConfig only as necessary; owning docs.

**Pass:** From outside and inside, store floor/stockroom and lodge rooms lack local wisps while fog continues outside; same result in top-down orthographic, top-down perspective and FPS. Canopy/open lot retains haze unless separately declared enclosed. Toggle haze off/zero/on and spawn/remove/refresh a building. No per-frame C# uniform uploads, new terrain queries or per-volume callbacks; active GPU shader cost remains and must be bounded. Unknown/overflow cases warn and follow the documented safe fallback.

## IE3 — Rain masking ONLY in first person

**Objective:** First-person interiors/shelter are dry, with outdoor rainfall visible through openings; overhead rain retains its current presentation.

Use IE1 interior/shelter data via an explicit FirstPersonRainShelter policy. TopDown policy bypasses the new rain masking/collider setup entirely, even when its camera uses perspective and its player is inside the building. Do not call broad WeatherBridge outdoors=false to hide all weather/atmosphere effects.

Implement IE0's audited spatial strategy on the rain emitter(s) only: scoped GPU shelter/collision suppression or a minimal particle exclusion adapter. Ensure any helper collider/render layer affects only intended rain particles and never player/weapon/AI collision or top-down effects. Keep nearby sheltered drops hidden while retaining drops outside window/door openings. Roof-only canopy shelters FPS rain while leaving open-air ground haze policy independent.

Duplicate only per-instance mutable particle resources where necessary; do not globally mutate shared addon materials. On explicit mode change, restore the top-down material/emission settings and clear shelter state. Avoid identifying FPS from Projection.Perspective. Weather OFF/zero must stop emission/simulation as before; apply volume/mode/config updates by events. Existing cave behavior is preserved through a separate legacy adapter; no silent snow/motes/mist changes. Imported addon source modifications require a narrowly justified diff documented for maintenance.

**Owners:** WeatherBridge/TestWeather adapter, GameEnvironment view binding, minimal World/FPS view-policy injection, relevant config/material/helper and docs.

**Pass:** Same scene/rain intensity: FPS under roof is dry and sees outdoor rain; FPS outside rains normally. Top-down rain remains unchanged outside/inside/under canopy in BOTH projection modes. Check roof edges, windows, upstairs, mode/reload transitions, weather off/on, volume removal and multiple buildings. Do not introduce per-frame global scans, repeated collider rebuilding or per-drop C# work. Active rain's existing GPU/addon movement work remains intentional.

## IE4 — Delivery and local acceptance

Brief source/node/resource/loop review, git diff --check and one initial `dotnet build war-machine.csproj` where available. Godot local checks: F6 WORLD/World.tscn and WORLD/FirstPerson/FirstPersonWorld.tscn, inspect store from outside/inside, multi-storey lodge and open petrol canopy. Verify top-down rain unchanged, local haze clipped in both views, outside effects still visible from indoors, and actor collision/OV1/PER2 unaffected. Try new prefab, refresh, transform, unload/reload and off/zero/reactivation.

Report actual vs deferred checks, changed files and commit SHA, wake/update triggers, settled idle work, GPU/volume budget, overflow and known limitations. No measured performance claim without profiling. Mark implementation Awaiting Validation until user acceptance. No site-grounding changes, new global WorldEnvironment, full room/portal renderer, audio occlusion, weather physics, GI correction, indoor navigation or unrelated feature phases.
