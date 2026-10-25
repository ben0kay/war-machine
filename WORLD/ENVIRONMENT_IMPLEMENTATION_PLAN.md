# Environment Foundation — Implementation Plan

> **Plan only — no implementation authorized by this file.**  
> **Scope:** War Machine's **terrain, exploration environment, rendering distances, vegetation, atmosphere and environmental locations**. No robots, combat, spawning, crafting or survival gameplay in these phases.  
> **Owner:** `WORLD/` (cross-system dependencies may later live in `VEGETATION/`, `ENVIRONMENT/`, `PLAYER/` and `DEBUG/`).  
> **Guidance:** [Root game overview](../README.md) · [CodePreferences](../CodePreferences.md) · [WORLD ownership](README.md)

## Confirmed baseline and current repository audit

The checked-in project is **Godot 4.7-featured C#/.NET, Forward+, Direct3D 12 and Jolt Physics** (per `project.godot`; local runtime compatibility is still unverified).

**Confirmed on GitHub `main` at plan revision:**
- `addons/terrain_3d/` contains **Terrain3D 1.0.2** (`plugin.cfg`) plus binaries/editor assets.
- `demo/Demo.tscn` is the *visually rich reference*: `demo/data/assets.tres` defines **Cliff** and **Grass** textures and foliage assets; the scene enables the terrain **autoshader** with `auto_base_texture`, `auto_overlay_texture`, `auto_slope`, macro variation and texture detiling. Terrain data lives under `res://demo/data`.
- `demo/CodeGeneratedDemo.tscn` + `demo/src/CodeGenerated.gd` demonstrate seeded-noise-style heightmap generation/import and instance placement. **Its simpler procedural textures are not a substitute for the polished main demo material settings.**
- `demo/components/Environment.tscn` provides a **static** procedural sky and directional sunlight, which must not also control the sky in our Sky3D world.
- `demo/src/DemoScene.gd` includes a conditional *Sky3D editor integration example*, referencing `res://addons/sky_3d/src/Sky3D.gd`, but **that reference does not mean the addon is installed**.
- **Sky3D and the other plugin mentioned by the owner are not yet visible in the inspected `main` addons directory.** Codex must re-check after the owner's push before integrating either; never fabricate their API or scenes. Preserve the extra plugin if present, but do not add it to the scope without identifying its role.

### Revised scene decision

**The first deliverable is `res://WORLD/testworld.tscn`, exactly named `testworld.tscn`.** It is the actual runnable **environment prototype**, not an isolated benchmark to abandon. Its terrain data, reusable player/controller and environment elements must have clear paths and be suitable for promotion into a future main `WorldRoot.tscn` **without rewriting everything**. Do not build WorldRoot in parallel now.

Choose a modest, performant initial footprint appropriate to the Terrain3D demo assets (expand later; ~2 x 2 km remains a possible subsequent test, **not an immediate requirement**). Prefer attractive close/mid scenery with distant hills and proper relief over an enormous empty plane.

### What counts as the first success

Play `testworld.tscn` and **see smooth hills/valleys, naturally blended grassy surfaces and exposed rock on steep slopes, some visibly wind-moving grass, a Sky3D sky and a sun that actually travels across the sky and changes scene lighting over time**. Travel around it with a simple camera/player and revisit the exact same saved landforms after relaunch. No robots, survival, weather or sophisticated world streaming.

## Phase overview

| Phase | Goal | Recommended lead | Status |
| --- | --- | --- | --- |
| **WE0** | Verify Terrain3D demo, Sky3D installation/API and one-scene design | Regular Chat + Copilot (Codex may include quick audit in WE1) | Planned |
| **WE1** | **Build playable `WORLD/testworld.tscn`: demo-quality terrain, grass/rock, wind-moving grass, Sky3D moving sun** | **Codex / ChatGPT Work** | **Planned — next proposed Codex task** |
| **WE2** | Refine terrain geography, materials and deterministic authoring workflow | Codex / ChatGPT Work | Planned |
| **WE3** | Validate long-distance views, terrain/foliage LOD and scoped streaming | Codex / ChatGPT Work | Planned |
| **WE4** | Expand vegetation into varied forests, rocks and coherent wind | Codex / ChatGPT Work | Planned |
| **WE5** | Polish existing Sky3D day/night, atmospheric fog, light and exposure | Codex / ChatGPT Work | Planned |
| **WE6** | Add rain, weather changes, wetness and environmental audio | Codex / ChatGPT Work | Planned |
| **WE7** | Add caves, landmarks and memorable exploration routes | Codex / ChatGPT Work | Planned |
| **WE8** | Profile, tune quality tiers, decide whether to promote testworld | Copilot + Codex / ChatGPT Work | Planned |

All phases are **Planned**. No code implementation is being authorized by this documentation update. **Status vocabulary:** Planned → In Progress → Implemented → Awaiting Validation → Accepted. Advance phases only on explicit instruction; building/pushing something is not proof it was tested or accepted.

## WE0 — Quick compatibility and demo audit

**Objective:** Establish how to reproduce Terrain3D's visual result and drive Sky3D's real sun before generating any War Machine assets.

- **Dependencies:** Godot .NET editor locally; current repo plus latest owner's addon push.
- **Scope:** Check `addons/` and `project.godot`, identify Sky3D version, license, plugin enablement, scene/script, time-of-day property/update mechanism and sun/light ownership; identify the third plugin. Compare `demo/Demo.tscn`, `demo/data/assets.tres`, `demo/src/CodeGenerated.gd`, `demo/components/Environment.tscn` and any Sky3D demo. Verify terrain/sun preview in editor/runtime and that binaries actually load.
- **Out of scope:** Writing an alternative terrain renderer, custom day/night framework, world streamer, procedural biome ecosystem or weather.
- **Owning files:** Source demo/addon files are **read-only references**; `WORLD/README.md` and this plan may record findings. Only modify other project files if explicitly authorized.
- **Approach:** Record exact installed versions; confirm the screenshot-quality techniques (Terrain3D cliff/grass texture assignments, auto-slope blend, instancing and vegetation motion). Check what specifically creates grass movement; don't infer from a screenshot alone. For Sky3D, use the installed addon API rather than guessing or duplicating DirectionalLight3D.
- **Data/config:** Define intended editable parameters (terrain seed/extent/elevation; sun start time, progression speed, pause; grass density/wind). Confirm where newly generated terrain can be saved independently of `demo/data`.
- **Performance:** Verify Forward+ backend and addon cost; note D3D12 errors if present. No new always-running work.
- **Pass criteria:** Confirm the two addons and third plugin, successful import/editor setup (or exact blockers), source locations for material, vegetation and sky, and recommended configuration to start WE1. A local visual check may be deferred to Copilot with explicit commands.
- **Assistant:** **Regular Chat** for source inspection/planning, **Copilot** for local launch/build checks. Codex can perform this brief audit at the **start of an expressly authorized WE1 job**, stopping and reporting if Sky3D is absent or incompatible.

## WE1 — First Codex deliverable: demo-inspired, playable testworld + Sky3D

**Objective:** Create `res://WORLD/testworld.tscn` as War Machine's **first real, reusable environment scene**. It should immediately show the best parts of Terrain3D's demo **plus Sky3D's continuously changing sky/sun**, without copying unrelated demo systems or building a huge framework.

**Dependencies:** WE0 compatibility checks or a short inline audit. **Sky3D must actually exist in the repository or be made available by the user.** Never silently replace it with a static Godot procedural sky and claim Sky3D integration succeeded.

### Required, visible result

- **Terrain:** A *new*, saved Terrain3D landscape with varied height: soft grassy areas, hills, at least one valley, steep exposed-rock slopes and a pleasing nearby/far view. Make a distinct world, not a renamed `demo/Demo.tscn`.
- **Materials:** Replicate the **quality** of the main Terrain3D demo, especially auto-blending **grass on flatter land to cliff/rock on steep slopes**. Inspect actual `Terrain3DMaterial` parameters and `assets.tres` before adapting; start from working demo texture assets/settings where their reuse/license permits, then tweak them in **our own saved resources**.
- **Vegetation:** Place enough grass patches/foliage to show density and terrain contour **and visibly move in the wind**. Inspect whether the demo uses terrain mesh assets, material vertex shaders or another technique. Reuse the simplest validated solution; no custom per-blade physics/scripts. If dense trees require extra assets, **defer trees** rather than introducing a new package.
- **Sky:** Use the **real installed Sky3D addon**, with appropriate sky/environment and its own sun/moon controller if supported. Sky3D must own scene sky/time/solar direction; **do not instantiate the Terrain3D demo's static `Environment.tscn` alongside it**.
- **Moving sun:** Starting in daytime, the sun visibly progresses across the sky and the terrain's light/shadows change as time passes. Supply adjustable **start time**, **day length/time scale** and **pause** (via central config or minimal debug controls). A faster preview mode for checking an entire day is desirable.
- **Exploration:** A lightweight C# camera/player can walk or fly around terrain, look around and safely spawn above ground. Reuse the demo's movement *concept*, but keep War Machine's own runtime scripts in C#.
- **Run path:** Set `testworld.tscn` as the temporary project main scene if there is no existing game entry point, so **F5 runs the result**. It must also run directly with **F6**. Do not change or delete `demo/` or addon sources.

### Suggested implementation steps (one authorized WE1 job, one commit)

1. Inspect addon binaries, Sky3D files/API and demo assets/scene dependencies; confirm whether the owner's other plugin is relevant. If Sky3D files are missing, **report that blocker; do not fake integration**.
2. Create `WORLD/testworld.tscn`, with a clean node hierarchy: root, Terrain3D, sky/environment owner, one player/camera and only lightweight optional debug components. **Do not create a competing `WorldRoot.tscn`.**
3. Derive a modest **deterministic terrain** using either a controlled noise/heightmap import inspired by `demo/src/CodeGenerated.gd` or a pre-authored terrain saved through Terrain3D. Prefer a one-shot editor/bake workflow rather than generating every game launch. Save new terrain files in **`WORLD/`**, not `res://demo/data`. Never overwrite or destructively regenerate player-authored changes.
4. Apply the **main** demo's rock/grass terrain resources and slope-based auto-shader logic (not the simpler code-generated demo material). Reuse resources where appropriate but avoid fragile, mutable links to demo save data. Verify imported assets and redistribution/licensing notes before copying any third-party art.
5. Place some low-cost vegetation with wind movement using Terrain3D's supported instancing or verified demo approach. Tune density for a convincing example before adding a new procedural vegetation framework.
6. Integrate **Sky3D** using its actual public interface, drive time continuously at configurable speed, allow pausing, and ensure sunlight/shadow direction updates correctly. Avoid duplicate WorldEnvironment/DirectionalLight3D instances. Keep third-party GDScript inside addon boundaries; War Machine's own glue should be C# unless a confirmed API restriction forces a documented exception.
7. Ensure terrain data/materials, scene transforms, player input and time configuration survive fresh editor open/relaunch. Compile/check, run with F5/F6, move around while sun moves, and compare visually with the original Terrain3D demo. Record evidence and limitations.

### File ownership and configuration

- **Proposed:** `WORLD/testworld.tscn`, `WORLD/Terrain/` or `WORLD/data/testworld/` (own terrain/resources), `WORLD/WorldConfig.cs` or equivalent master config; a minimal C# `PLAYER/` controller; a compact `ENVIRONMENT/` sky/time configuration and controller *only if needed*. Create folder READMEs when folders are created. Exact paths may adapt to working addon requirements.
- **Avoid:** Copying every demo scene/UI/enemy/navigation asset, modifying addon source, a custom Sky3D replacement, duplicating the sky, generalizing the PCG pipeline prematurely or making a heavy manager hierarchy.
- **Tweakable settings:** Terrain world seed/scale/elevation, slope/rock threshold, grass density/size/wind speed, sun start time, day length/time scale and pause; group in appropriate exported inspector categories / central configs as per CodePreferences.

### Performance and lifecycle

- **Generation:** One-shot **explicit create/regenerate** (ideally editor-time), *not* on each `_Ready`; a regenerate action must warn about overwriting edits. Fixed seed means stable output. Saved terrain is reused at runtime.
- **Sun/time:** Deliberate continuous work **only while enabled and unpaused**, at the addon's supported cadence. Paused time suspends unnecessary custom updates.
- **Grass/wind:** Shared shader/instancer-driven animation, not individual `_Process` per blade. Restrict scene complexity; use existing Terrain3D LOD instead of inventing streaming in WE1.
- **Disabled/idle:** Debug widgets and editor tools should not poll unnecessarily. Measure baseline and moving-sun frame time if possible, but do not assert unmeasured FPS.

### Validation and pass criteria

- Godot .NET project and GDExtensions open without errors; script compilation passes or clearly documented blockers remain.
- F5 and F6 open **`WORLD/testworld.tscn`**; the camera can traverse / inspect elevated terrain with correct collision or intentional free-fly mode.
- Ground reads as **grass on gentle slopes and rock on steep surfaces**, like the demo; nearby grass visibly **waves** without per-grass CPU scripts.
- **Sky3D is visibly present**, and an accelerated day demonstration makes the **sun travel and terrain shadows change**. Pause and resume work; only one authoritative sky/light setup exists.
- Saved terrain/materials and placement persist across restart; original `demo/` remains unchanged.
- Record what was automated versus inspected visually, CPU/GPU baseline if measured, known hardware/renderer issues and any pending local visual validation.

**Recommended assistant:** **Codex / ChatGPT Work** for multi-file editor/addon/C# integration; **Copilot** can run Godot locally, compile and inspect visual/performance results. **This phase is still Planned until the user explicitly says to implement WE1.**

## WE2 — Geographic composition and terrain authoring

**Objective:** Refine the first testworld landscape into a more convincing and varied wilderness with intentional sightlines, better terrain authoring tools and material composition.

- **Dependencies:** WE1 working, saved `testworld.tscn` with basic terrain/grass/rock and Sky3D sun.
- **Scope:** Mountains, ridges, valleys, slopes, clearings, optional drainage line and a small palette of grounded terrain materials; assess the best hybrid of procedural starting shape and manual sculpting.
- **Out of scope:** Infinite terrain, full voxel destruction, detailed caves, complex hydrology or AI encounters.
- **Owning files:** `WORLD/Generation/`, `WORLD/Terrain/`, world config, terrain data and `ASSETS/Terrain/` if introduced.
- **Approach:** Extend the simple, saved WE1 landscape. Develop optional deterministic noise/heightmap presets **only if they help**; hand-adjust features to make distinct locations. Use slope/altitude/mask rules as placement inputs, not independent competing world generators. Keep terrain authoring reproducible where feasible.
- **Data/config:** Preserve author edits separate from generator defaults; expose seed, scale, heights, slope thresholds and material masks. Do not re-generate destructively over manual work.
- **Performance:** Generation/terrain imports are event- or editor-driven, not perpetual frame work. Measure collision/terrain memory versus extent and texture choices.
- **Pass criteria:** The world has recognizable landforms, traversable routes and non-repeating sightlines; terrain saves accurately and renders at near/far viewpoints.
- **Assistant:** **Codex / ChatGPT Work** for terrain tools/asset integration; **Regular Chat** can refine geography rules, documentation and material choices.

## WE3 — Distance, LOD and world loading proof

**Objective:** Establish believable long-distance scenery before filling the map with expensive content.

- **Dependencies:** WE1–WE2 world and enough geometry to inspect distance.
- **Scope:** Inspect existing Terrain3D LOD/visibility before adding custom streaming; introduce lightweight distant mountains/tree silhouettes as needed; tune culling, shadow ranges and region activation boundaries. Instrument camera movement and pop-in.
- **Out of scope:** A fully generic world-partition engine, infinite world, enemy simulation or a custom render backend.
- **Owning files:** `WORLD/Streaming/`, `WORLD/Terrain/`, `DEBUG/` and world settings, **only if** a real gap requires these systems.
- **Approach:** First exploit built-in terrain LOD, mesh LOD and visibility ranges. Add a region-scoped streamer for **separate objects and detail** only when profiling demonstrates a need; visual distant scenery must not vanish when interactive detail unloads.
- **Data/config:** View distance, detail radius, region size, per-frame load/unload budget, transition and shadow-distance settings.
- **Performance:** Wake on player movement/crossing region thresholds, not full scans every frame. Hysteresis avoids reload thrashing; unload or sleep distant content; bound work per frame. Measure hitch length and memory.
- **Pass criteria:** Movement across multiple regions gives stable terrain and distant silhouettes, acceptable loading hitch/popping and documented profiler results.
- **Assistant:** **Codex / ChatGPT Work** for streaming architecture; **Copilot** for local frame captures. Do not optimize without an observed bottleneck.

## WE4 — Vegetation, instancing and wind

**Objective:** Expand WE1's small patch of moving grass into a dense, natural-looking forest with scalable rendering and coherent wind.

- **Dependencies:** WE1 grass/rock proof, WE2 placement masks and WE3 visibility strategy.
- **Scope:** A small consistent library of trees, saplings, bushes, ferns, grass, logs and rocks; deterministic clustered placement using altitude/slope/moisture rules; instance batching/LOD; shared wind for suitable foliage.
- **Out of scope:** Plant growth simulation, harvestable forests, fire, tree-by-tree physics or individually scripted grass blades.
- **Owning files:** `VEGETATION/`, `ASSETS/Vegetation/`, world region descriptors and shared `ENVIRONMENT/` wind source as required.
- **Approach:** Use multi-instance rendering where appropriate, separate large interactable trees from decorative instances only if necessary, vary scale/orientation without losing asset coherence; use shader/vertex wind driven by a few shared signals.
- **Data/config:** Plant density by region, placement seed, slope/altitude masks, wind strength, LOD transition distance, shadow quality and instance budget.
- **Performance:** Placement on chunk creation/config changes; remove on unload. Shader wind may remain continuous in active views; **no per-plant `_Process`**. Profile foliage overdraw, shadows and instance counts.
- **Pass criteria:** Dense woodland reads convincingly up close and far away; motion looks coherent; unload/reload keeps placements stable; measurable density/quality switches work.
- **Assistant:** **Codex / ChatGPT Work**, due to batching/shader/streaming integration; **Regular Chat** can author placement rules and config proposals.

## WE5 — Sky, sunlight, fog and atmospheric depth

**Objective:** Polish and deepen WE1's already-working Sky3D day/night and lighting into a cohesive, damp wilderness atmosphere.

- **Dependencies:** WE1 Sky3D moving-sun proof, WE2 terrain and ideally WE4 foliage for real-world visual testing.
- **Scope:** Refine Sky3D's sky/time/clouds and existing moving sunlight; add coherent lighting/exposure, height/distance fog and localized mist pockets. Sun movement itself belongs to **WE1**, not WE5.
- **Out of scope:** Custom global illumination renderer, stacking multiple independent sky systems or full weather simulation.
- **Owning files:** `ENVIRONMENT/Lighting/`, `ENVIRONMENT/Atmosphere/`, environment config, world integration nodes.
- **Approach:** Retain **WE1's single Sky3D source of truth** for sun/time/sky and add one coordinator for fog/exposure. Leverage Forward+ and builtin volumetric fog where useful; test fog in forests, open valleys and caves to avoid excessive flat haze.
- **Data/config:** Time, sun orientation, shadow distance, cloud coverage, atmosphere density, fog height/distance and per-quality defaults.
- **Performance:** Shared global controls; update sun only at needed cadence or when adjusted. Static fog should not force scripts to poll when unchanged. Measure GPU cost of volumetrics and shadows.
- **Pass criteria:** Convincing morning/overcast/evening looks; distant landscape retains shape; coherent light and fog in **testworld**, with safe quality reductions.
- **Assistant:** **Codex / ChatGPT Work** for shader/addon/editor integration; **Regular Chat** can choose visual targets and document tweakable controls.

## WE6 — Weather, precipitation, wind variation and ambience

**Objective:** Progress seamlessly from a calm forest to rain and storm conditions without wasteful idle effects.

- **Dependencies:** WE4 shared wind and foliage response, WE5 sky/fog/light ownership.
- **Scope:** Presets or controls for clear, cloudy, light/heavy rain, fog and storm; controlled wind gusts, localized rain around camera, lightning/thunder and environmental ambience; subtle wet-surface cues if feasible.
- **Out of scope:** Global water simulation, complex indoor precipitation physics, snow/blizzards unless requested, disaster destruction or survival penalties.
- **Owning files:** `ENVIRONMENT/Weather/`, `ENVIRONMENT/Audio/`, `FX/`, vegetation wind inputs and a centralized environment config.
- **Approach:** Prefer a small C# weather coordinator controlling built-in particles/sound/lighting/fog. Evaluate available weather addons **only** if they do not duplicate ownership. Keep weather independent of terrain layout and save/display state through explicit API.
- **Data/config:** Weather intensities, transition duration, rain spawn volume, fog/wind coupling, lightning frequency, audio levels and quality limits.
- **Performance:** Weather disabled means particle emission and splash callbacks **stop** after any intended fade. Conditions change via signals/config triggers; no hidden per-frame inactive system. Limit localized effects to visible/nearby region.
- **Pass criteria:** Toggle rain 0 → heavy → 0 → heavy and observe transitions, sound, cloud/fog/wind response; demonstrate inactive emitters sleep; profile storm versus clear cost and verify relaunch/reload.
- **Assistant:** **Codex / ChatGPT Work** for multi-effect integration; **Copilot** for local inactive/active profiler tests.

## WE7 — Caves, landmarks and environmental composition

**Objective:** Make exploring the landscape rewarding even with no enemies or survival loop.

- **Dependencies:** WE2 world shape, WE3 region visibility and WE5–WE6 ambience.
- **Scope:** One well-integrated cave entrance/interior, recognizable rock formation or ruin, a few exploration routes and point-of-interest dressing; optional atmospheric cues like dripping water or distant thunder.
- **Out of scope:** Combat arenas, dungeon loot, quests, machines, mining or universal terrain carving.
- **Owning files:** `WORLD/PointsOfInterest/`, cave mesh assets under `ASSETS/`, relevant world placement/streaming and environment volume controls.
- **Approach:** Use Terrain3D holes or suitable transitions with separate 3D cave meshes, not a voxel-world rewrite. Blend terrain/cave materials; verify collision, camera navigation and fog/light transitions. Ensure location identifiers stay stable.
- **Data/config:** World-space POI coordinates/ids, cave entrance transforms, visibility/load radii and local atmosphere overrides.
- **Performance:** Stream POI detail by distance, keep interiors dormant when unvisited, disable hidden effects, and watch cave occlusion/shadow costs.
- **Pass criteria:** Player can identify a landmark, travel there, enter/exit a cave and return without holes, clipping, navigation blocks, lost state or severe frame spikes.
- **Assistant:** **Codex / ChatGPT Work** for scene and terrain integration; **Regular Chat** for level-design notes and asset shortlist.

## WE8 — Environment acceptance and performance polish

**Objective:** Prove the environment is ready to serve as a foundation before considering mechanics.

- **Dependencies:** WE1–WE7 integrated to the degree requested/accepted.
- **Scope:** Practical target-PC quality profiles, frame time/memory/streaming tests, camera traversal checks, terrain/foliage/fog blending, audio mood, edge-case region tests and documentation reconciliation.
- **Out of scope:** Robots, enemy navigation, weapons, crafting, hunger, base building and networking.
- **Owning files:** Existing subsystem configs, `DEBUG/`, owning folder docs, this plan, and targeted fixes to measured bottlenecks.
- **Approach:** Establish repeatable traversal/viewpoints, baseline near/mid/far screenshots or descriptions, quality-tier comparisons and capture actual CPU/GPU profiler data. Fix issues based on evidence.
- **Data/config:** Named performance presets and documented hardware/settings, not hardcoded assumptions that all PCs match.
- **Performance:** Explicit tests for idle, start/stop, load/unload, reactivation, high density, rainy/storm conditions, cave transitions and distant views. No “optimized” claims without measurements.
- **Pass criteria:** **testworld.tscn** launches directly, feels atmospheric and worth exploring, retains authored content, shows meaningful distant scenery and meets an **agreed** frame-time target on the owner's PC. Decide with the user whether to promote/rename this same working scene or instantiate its reusable systems under a new `WorldRoot.tscn`; do not rebuild from scratch. User visual acceptance is required to mark Accepted.
- **Assistant:** **Copilot** for repeatable local profiling/tests, **Codex / ChatGPT Work** for measured multi-system fixes and **Regular Chat** to summarize results/re-plan.

## Shared handoff and execution rules

1. **Before implementation:** Read `README.md`, `CodePreferences.md`, `WORLD/README.md`, this plan and all existing owning files. Verify what actually exists; update the baseline rather than pretending planned files are present.
2. **Phase authorization:** Only the phase explicitly selected by the user may be implemented. Planning this roadmap does not authorize WE0, WE1 or any future work.
3. **Integration:** **`res://WORLD/testworld.tscn` is the explicitly requested playable environment scene** through the initial environment phases. Build real reusable systems into it; do not create a separate `WorldRoot.tscn` until the user authorizes promotion/integration. Optional tiny diagnostic scenes cannot replace testworld acceptance.
4. **Conventions:** C#, simple and modular code, commented file headers and methods, visually separated code regions where useful, per-folder README and easy-to-tune master config, one GitHub commit **per authorized job**.
5. **Performance handoff:** Every new system must state its activation trigger, shutdown/idle behaviour, remaining continuous work, streaming strategy and measured versus unmeasured performance.
6. **Validation handoff:** Capture commit SHA, changed files, build/editor checks, user-visible test steps, results, outstanding issues and deferred checks. If using user's optional Copilot local-validation workflow, mark **Awaiting Validation** until results arrive.
7. **State updates:** Set each phase to Planned / In Progress / Implemented / Awaiting Validation / Accepted based on evidence and update owning docs. Archive superseded plans rather than leaving conflicting instructions.
8. **Division of labour:** **Regular Chat** for design, docs, phase review and safe isolated settings; **Codex/ChatGPT Work** for architecture, rendering, multi-file code and integration; **Copilot** for local compile/editor/profiler and authorized minor fixes; **Claude** optionally for an isolated review. These are recommendations, never implicit permission.

## Things to decide during the audit, not beforehand

- Terrain3D 1.0.2 is checked in; verify its local/editor runtime and the **not-yet-visible Sky3D addon**, plus the other plugin's identity/version.
- Target GPU/RAM and expected frame time at desired view distance.
- First-person vs third-person camera (a basic controller is sufficient for WE1).
- Whether custom terrain and region streaming are genuinely needed beyond addons/built-in LOD.
- Which assets and material style best communicate believable rugged wilderness.

**Environment milestone:** A player launches **`WORLD/testworld.tscn`**, explores varied smooth terrain with convincing vegetation and wind, watches Sky3D's moving sun over distant hills, experiences later fog/rain changes and eventually visits a cave or landmark — **with no robot or survival mechanics required**.
