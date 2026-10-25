# Environment Foundation — Implementation Plan

> **Plan only — no implementation authorized by this file.**  
> **Scope:** War Machine's **terrain, exploration environment, rendering distances, vegetation, atmosphere and environmental locations**. No robots, combat, spawning, crafting or survival gameplay in these phases.  
> **Owner:** `WORLD/` (cross-system dependencies may later live in `VEGETATION/`, `ENVIRONMENT/`, `PLAYER/` and `DEBUG/`).  
> **Guidance:** [Root game overview](../README.md) · [CodePreferences](../CodePreferences.md) · [WORLD ownership](README.md)

## Confirmed baseline and assumptions

At the time of this plan, the checked-in root contains the **starter Godot project** and documentation. `project.godot` identifies Godot **4.7**, **C#/.NET**, **Forward+**, **D3D12 on Windows**, and **Jolt Physics**. No gameplay world or addon files were present in the inspected repo. The owner is **installing Terrain3D locally**, which is not yet confirmation of a versioned, functional addon in GitHub.

Therefore **do not assume Terrain3D or Sky3D is installed**. Verify addon files, their declared/actual compatibility, editor startup, C# bindings, saved terrain data and rendering backend first. Prefer Vulkan if an addon has a confirmed D3D12 issue, but do not change the renderer merely on speculation.

Provisional test landscape: a **finite, expandable ~2 × 2 km** region with hills, a valley and distant ridges. This is a sizing experiment, not a mandated allocation. The goal is a compelling **real world scene** experienced in motion, not disconnected technical demos.

## Phase overview

| Phase | Goal | Recommended lead | Status |
| --- | --- | --- | --- |
| **WE0** | Audit engine/addons, confirm scene strategy and performance baseline | Regular Chat + Copilot | Planned |
| **WE1** | Boot into the real world with sculptable terrain and basic traversal | Codex / ChatGPT Work | Planned |
| **WE2** | Shape credible wilderness geography and terrain materials | Codex / ChatGPT Work | Planned |
| **WE3** | Validate distance rendering, LOD and scoped streaming | Codex / ChatGPT Work | Planned |
| **WE4** | Populate scalable vegetation and shared wind response | Codex / ChatGPT Work | Planned |
| **WE5** | Establish coherent sky, sunlight, fog and atmosphere | Codex / ChatGPT Work | Planned |
| **WE6** | Add rain, weather changes, wetness and environmental audio | Codex / ChatGPT Work | Planned |
| **WE7** | Add cave/landmark exploration and final environment polish | Codex / ChatGPT Work | Planned |
| **WE8** | Profile, tune quality tiers and accept the environment milestone | Copilot + Codex / ChatGPT Work | Planned |

**Status vocabulary:** Planned → In Progress → Implemented → Awaiting Validation → Accepted. An implementation or push is **not** validation or acceptance. Advance phases **only after explicit user instruction**; some phases can overlap after prerequisites are verified.

## WE0 — Terrain3D, toolchain and baseline audit

**Objective:** Establish the actual checked-in state, addon constraints, realistic test scale and a small, concrete environment architecture decision before coding.

- **Dependencies:** Repo checkout, Godot .NET editor, owner's locally installed addons.
- **Scope:** Check addon versions/enabled state, Jolt and renderer compatibility, repo/project import, C# compilation, terrain saving/import workflow, source control requirements, demo limitations and current performance baseline; decide which layer owns each system.
- **Out of scope:** Building terrain, robots, weather or a custom streamer.
- **Owning files:** `project.godot`, `CodePreferences.md`, `WORLD/README.md`, this plan; feature files only if an explicitly authorized audit fix is necessary.
- **Approach:** Verify a clean editor open and addon discovery; test Terrain3D against selected rendering backend on the target PC. Record actual GPU/RAM, viewport settings, errors and FPS/frame time where obtainable. Compare built-in terrain/LOD with requirements before proposing new code.
- **Data/config:** Define the future authoritative world seed, initial extents, streaming/visibility budget and quality knobs, without generating permanent files yet.
- **Performance:** No new runtime loops. Audit idle/editor cost, rendering frame time and addon overhead; distinguish measured numbers from targets.
- **Pass criteria:** Document precise installed versions, whether Terrain3D works, known blockers, baseline observations, and accepted approach to main-scene integration.
- **Assistant:** **Regular Chat** can audit, document and make isolated configuration plans; **Copilot** can run local build/editor checks and report exit codes; reserve Codex for real integration work.

## WE1 — Real playable WorldRoot and terrain foundation

**Objective:** Run the actual game in an integrated world scene with controllable exploration and visible sculptable terrain.

- **Dependencies:** WE0 compatibility decision, confirmed addon/toolchain.
- **Scope:** Main/startup scene, `WorldRoot.tscn`, selected terrain node/addon, baseline sky/light, terrain collision, a minimal exploration camera/controller and simple debug coordinates.
- **Out of scope:** Procedural biomes, rich vegetation, robots, elaborate movement, weather and full streaming.
- **Owning files:** `WORLD/WorldRoot.tscn`, `WORLD/` config and terrain subtree; `PLAYER/` basic controller; `DEBUG/` only as needed. These paths are proposed.
- **Approach:** Establish the world scene as the actual project run target. Create a small terrain patch first, confirm sculpt/paint/save/reload and player traversal, then expand extent only as needed. Avoid test-scene-only integration.
- **Data/config:** A single world size/origin and terrain data location; simple tweakable movement speed, camera height and terrain quality options.
- **Performance:** No per-terrain-cell polling. Keep terrain editor operations and runtime work separate; profile baseline as player moves and camera pans.
- **Pass criteria:** Launch project → traverse uneven terrain → re-open project → terrain edits and collision remain intact; compile and run without addon errors.
- **Assistant:** **Codex / ChatGPT Work**, because scene structure, C# input/collision and addon integration span files; **Copilot** for local verification.

## WE2 — Geographic composition and terrain authoring

**Objective:** Turn the blank terrain into a believable landscape with intentional sightlines and visual variety.

- **Dependencies:** WE1 usable sculptable world.
- **Scope:** Mountains, ridges, valleys, slopes, clearings, optional drainage line and a small palette of grounded terrain materials; assess the best hybrid of procedural starting shape and manual sculpting.
- **Out of scope:** Infinite terrain, full voxel destruction, detailed caves, complex hydrology or AI encounters.
- **Owning files:** `WORLD/Generation/`, `WORLD/Terrain/`, world config, terrain data and `ASSETS/Terrain/` if introduced.
- **Approach:** Seed broad landforms with deterministic noise/heightmap tools **only if they help**; hand-adjust features to make distinct locations. Use slope/altitude/mask rules as placement inputs, not independent competing world generators. Keep terrain authoring reproducible where feasible.
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

**Objective:** Give the land a dense, natural-looking forest with scalable rendering and motion.

- **Dependencies:** WE2 environmental placement masks, WE3 visibility strategy.
- **Scope:** A small consistent library of trees, saplings, bushes, ferns, grass, logs and rocks; deterministic clustered placement using altitude/slope/moisture rules; instance batching/LOD; shared wind for suitable foliage.
- **Out of scope:** Plant growth simulation, harvestable forests, fire, tree-by-tree physics or individually scripted grass blades.
- **Owning files:** `VEGETATION/`, `ASSETS/Vegetation/`, world region descriptors and shared `ENVIRONMENT/` wind source as required.
- **Approach:** Use multi-instance rendering where appropriate, separate large interactable trees from decorative instances only if necessary, vary scale/orientation without losing asset coherence; use shader/vertex wind driven by a few shared signals.
- **Data/config:** Plant density by region, placement seed, slope/altitude masks, wind strength, LOD transition distance, shadow quality and instance budget.
- **Performance:** Placement on chunk creation/config changes; remove on unload. Shader wind may remain continuous in active views; **no per-plant `_Process`**. Profile foliage overdraw, shadows and instance counts.
- **Pass criteria:** Dense woodland reads convincingly up close and far away; motion looks coherent; unload/reload keeps placements stable; measurable density/quality switches work.
- **Assistant:** **Codex / ChatGPT Work**, due to batching/shader/streaming integration; **Regular Chat** can author placement rules and config proposals.

## WE5 — Sky, sunlight, fog and atmospheric depth

**Objective:** Establish the project's damp, rugged and atmospheric visual identity, while keeping a single owner for each lighting control.

- **Dependencies:** WE1 main scene, WE2 landform and ideally WE4 foliage for real-world visual testing.
- **Scope:** Day/sun/sky solution (Godot built-ins or verified Sky3D), coherent lighting/exposure, clouds where practical, height/distance fog and localized mist pockets; optional basic time progression.
- **Out of scope:** Custom global illumination renderer, stacking multiple independent sky systems or full weather simulation.
- **Owning files:** `ENVIRONMENT/Lighting/`, `ENVIRONMENT/Atmosphere/`, environment config, world integration nodes.
- **Approach:** Pick **one** source of truth for sun/time/sky and one coordinator for fog/exposure. Leverage Forward+ and builtin volumetric fog where useful; test fog in forests, open valleys and caves to avoid excessive flat haze.
- **Data/config:** Time, sun orientation, shadow distance, cloud coverage, atmosphere density, fog height/distance and per-quality defaults.
- **Performance:** Shared global controls; update sun only at needed cadence or when adjusted. Static fog should not force scripts to poll when unchanged. Measure GPU cost of volumetrics and shadows.
- **Pass criteria:** Convincing morning/overcast/evening looks; distant landscape retains shape; coherent light and fog in the **main world**, with safe quality reductions.
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
- **Pass criteria:** A coherent world launches directly, feels atmospheric and worth exploring, retains authored content, shows meaningful distant scenery and meets an **agreed** frame-time target on the owner's PC. User visual acceptance is required to mark Accepted.
- **Assistant:** **Copilot** for repeatable local profiling/tests, **Codex / ChatGPT Work** for measured multi-system fixes and **Regular Chat** to summarize results/re-plan.

## Shared handoff and execution rules

1. **Before implementation:** Read `README.md`, `CodePreferences.md`, `WORLD/README.md`, this plan and all existing owning files. Verify what actually exists; update the baseline rather than pretending planned files are present.
2. **Phase authorization:** Only the phase explicitly selected by the user may be implemented. Planning this roadmap does not authorize WE0, WE1 or any future work.
3. **Integration:** Main `WorldRoot.tscn` is the acceptance environment. Test scenes are optional diagnostics only; never let them be the sole home of implemented systems.
4. **Conventions:** C#, simple and modular code, commented file headers and methods, visually separated code regions where useful, per-folder README and easy-to-tune master config, one GitHub commit **per authorized job**.
5. **Performance handoff:** Every new system must state its activation trigger, shutdown/idle behaviour, remaining continuous work, streaming strategy and measured versus unmeasured performance.
6. **Validation handoff:** Capture commit SHA, changed files, build/editor checks, user-visible test steps, results, outstanding issues and deferred checks. If using user's optional Copilot local-validation workflow, mark **Awaiting Validation** until results arrive.
7. **State updates:** Set each phase to Planned / In Progress / Implemented / Awaiting Validation / Accepted based on evidence and update owning docs. Archive superseded plans rather than leaving conflicting instructions.
8. **Division of labour:** **Regular Chat** for design, docs, phase review and safe isolated settings; **Codex/ChatGPT Work** for architecture, rendering, multi-file code and integration; **Copilot** for local compile/editor/profiler and authorized minor fixes; **Claude** optionally for an isolated review. These are recommendations, never implicit permission.

## Things to decide during the audit, not beforehand

- Actual Terrain3D install/version, Sky3D interest and addon dependencies.
- Target GPU/RAM and expected frame time at desired view distance.
- First-person vs third-person camera (a basic controller is sufficient for WE1).
- Whether custom terrain and region streaming are genuinely needed beyond addons/built-in LOD.
- Which assets and material style best communicate believable rugged wilderness.

**Environment milestone:** A player launches one real scene, explores varied smooth terrain with convincing vegetation and wind, sees distant hills, experiences sky/fog/rain changes, and can enter a cave or reach a distinctive landmark — **with no robot or survival mechanics required**.
