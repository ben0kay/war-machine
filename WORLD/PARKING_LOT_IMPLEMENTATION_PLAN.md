# PL — Nighttime convenience-store parking lot

**Plan only. All phases Planned. Preferred implementer: Regular Chat, as requested by Ben.**
No scene, terrain, player, environment or gameplay changes are authorized by this document. Implement only the phase(s) Ben requests. One commit per authorized job.

## Purpose and plain-language scope

Extend the existing wilderness with one compact roadside store exterior: a small parking lot, 3–4 stationary cars, a sidewalk/curb, a short road connection and several street lamps, including one flickering lamp. A short approach through the trees should reveal the warm store lights against the cold mist.

Use the location to judge nighttime atmosphere and eventually compare first-person, third-person and angled top-down views. Do not commit the game to a perspective yet. Begin with readable primitive/procedural geometry and consistent materials, not detailed imported art.

| Phase | Goal | Suggested assistant | Status |
| --- | --- | --- | --- |
| PL0 | Reusable site scene and rough layout | Regular Chat — isolated scene/content work | Planned |
| PL1 | Place the site in TestWorld with safe access and collision | Regular Chat — small, bounded integration; report blockers | Planned |
| PL2 | Night lighting, repeatable preview and one flickering lamp | Regular Chat — local effects using existing environment ownership | Planned |
| PL3 | Optional camera comparison using the same site | Regular Chat for isolated prototype; review shared-controller scope first | Planned |
| PL4 | Small visual refinement and user handoff | Regular Chat — focused content/configuration adjustments | Planned |

Suggested review points: PL0–PL1 establish scale; PL2 establishes atmosphere; decide whether PL3 is needed before further polish. Phases are separately authorized, not an automatic sequence.

## Confirmed baseline and unresolved details

Read root README.md and CodePreferences.md first. This plan was checked against WORLD/README.md, WORLD/Content/README.md, PLAYER/README.md, ENVIRONMENT/README.md, WORLD/TestWorld.cs, PLAYER/ExplorationController.cs and WORLD/TopDownWorldTest/IMPLEMENTATION_PLAN.md on the default branch.

- WORLD/testworld.tscn is the integrated main world. TestWorld initializes existing terrain/content; it does not bake terrain on launch.
- Explorer already supports collision-free flight and collidable first-person walking. It has no visible body or third-person/top-down mode. F6 and the camera button already switch flight/walking.
- Existing consumers depend on Explorer movement events and its Camera3D path; camera work must inspect those dependencies rather than moving/removing nodes casually.
- Sky3D is the existing global sky/time/light/fog owner. EnvironmentMaster and SkyController supply shared configuration. Do not add a competing WorldEnvironment.
- Procedural scatter has no general site exclusion facility confirmed here. Exact site coordinates, ground suitability and local vegetation clearance require inspection of actual placement scripts and terrain.
- WORLD/Content/Structures/README.md does not currently exist on the default branch; Structures is proposed ownership, not an existing implemented subsystem.
- The separate TD0–TD3 orthographic experiment is still a plan. Do not assume it exists or implement/replace it under this job.
- Existing docs report local build/visual checks pending. Documentation is context, not evidence that current runtime behavior passed validation.

Before each implementation phase, inspect the actual owning scenes/configs/scripts and current branch. Resolve ordinary layout choices within scope; report substantial shared-system changes before broadening the job.

## Ownership and configuration

Proposed reusable site:
- WORLD/Content/Structures/ConvenienceStore/ConvenienceStoreSite.tscn
- Matching README.md, C/, Configuration/ and Artwork/ as needed.
- A small ConvenienceStoreSiteMaster resource for site dimensions, colours, placement and light/flicker settings. Create only files that earn their existence.
- Add WORLD/Content/Structures/README.md when that folder is actually implemented.
- Keep reusable lamp/car scenes local initially; promote them to shared world-content ownership only when another real consumer needs them.
- PLAYER owns any reusable camera/player changes. ENVIRONMENT retains global time/weather/fog. WORLD owns placement and the approach.

Prefer one site assembly helper and one optional flicker component over managers for every prop. Static meshes/materials/collision should be shared where practical. Follow C# headers, method separators, appropriate regions and grouped inspector exports.

## PL0 — Site layout

**Objective:** One independently editable site with plausible human scale.
**Dependencies:** Inspect current content scene/material conventions. No TestWorld changes required yet.
**Scope/approach:** Assemble an approximately 35–45m wide lot, 6–8 marked bays, 3–4 simple original parked cars, an approximately 12–16m wide store shell, raised sidewalk, curb, lamp positions and short road stub. Dimensions are starting points, freely tunable. Use meter-scale geometry, simple asphalt/concrete/paint/glass/body materials and an exterior sign. Suggest warm window panels with a shallow interior silhouette rather than a full interior.
**Owning files:** Proposed site folder and Structures README.
**Preserve/out of scope:** Existing world/player/environment; no store simulation, driving, doors, inventory, NPCs or detailed interior.
**Data/performance:** Shared meshes/materials, static geometry, no per-prop processing; config changes rebuild only explicitly.
**Pass:** Scene references resolve; lot, sidewalk, store and cars are distinct and correctly scaled. User reviews appearance locally.

## PL1 — Integrate into the existing world

**Objective:** Reach the site naturally in TestWorld without damaging saved terrain.
**Dependencies:** PL0; inspect testworld.tscn, terrain sampling, scatter/tree/grass placement and cave/robot locations.
**Scope/approach:** Find a reasonably level patch near an existing exploration route, away from cave/robot conflicts. Instance the site once and initialize after terrain is ready. Use a simple ground-supported foundation/apron with conservative edge transitions. Sample footprint heights once; fail clearly if too steep rather than silently floating or excavating. A site requiring broad terrain edits should be relocated or separately reviewed. Provide a short forest approach and temporary location coordinates/marker if useful.
**Owning files:** Site placement helper/config, WORLD/TestWorld.cs and testworld.tscn only as necessary; WORLD/README.md and content docs.
**Preserve/out of scope:** Saved Terrain3D regions, existing spawn and main-scene selection, cave/robot behavior and existing controls; no terrain rebake or new streaming framework.
**Data:** Add collision for ground/apron, sidewalk, store shell and parked cars using simple static shapes. Prevent foliage inside the footprint using existing facilities if available. If missing, review a minimal bounded exclusion hook; do not sweep unrelated scatter architecture into this phase.
**Performance:** One-time height sampling/build; static collision. Rebuilds must not duplicate the site. No continuous terrain queries or world scans. Unload frees site resources and registrations.
**Pass:** First-person walk reaches the lot, stays grounded, steps onto sidewalk and cannot pass through cars/store. No obvious buried/floating corners or vegetation through asphalt. Existing flight/cave/robot behavior preserved; user checks locally.

## PL2 — Nighttime atmosphere

**Objective:** Judge the location under repeatable night conditions.
**Dependencies:** PL1; inspect SkyController, EnvironmentMaster and existing tuning UI before adding hooks.
**Scope/approach:** Several warm street lamps, store entrance/window glow and one irregular flickering lamp. Use real local lights with restrained emissive meshes; choose a limited number of shadow-casting lamps. Flicker should vary intensity with occasional brief outages, not rapid full-screen flashes. Expose base energy, timing/variation, enable and seed. Keep other lamps steady.
**Owning files:** Site art/config and flicker helper; a small existing debug/environment hook only if needed.
**Preserve/out of scope:** Sky3D ownership, normal time progression and existing default environment values. No second sky/fog system or broad weather rework.
**Data:** Provide a documented night hour and pause procedure through existing controls first. If a one-click preview is justified, apply to runtime copies through the existing environment owner and offer explicit restoration; do not overwrite shared masters just to stage the shot.
**Performance:** Static lights receive no repeated writes. Flicker wakes only when enabled/relevant, schedules work only while active and stops/disconnects on disable/unload; restore configured steady/off state. Reuse existing movement/relevance signals where possible; avoid per-lamp distance polling. Document any intentional remaining timer/light/shadow cost.
**Pass:** Lot edges/cars remain readable, store is a focal point, mist remains visible and only selected lamp flickers. Repeat the same paused-night view; disable/re-enable and unload without stale callbacks. User assesses lighting/FPS.

## PL3 — Optional perspective comparison

**Objective:** Compare this exact location/time in first person, third person and angled orthographic top down.
**Dependencies:** PL2; explicit authorization for player/camera work, actual dependency inspection.
**Scope/approach:** Start by inspecting the lit site in existing first person/free flight. If useful, add fixed non-processing third-person-height and top-down comparison viewpoints as a tiny isolated first step. Fixed viewpoints compare composition only; they do not count as playable third-person controls.
For a playable comparison, preserve one player world position and collidable walking body across views, add a simple visible body hidden in first person, and use camera-relative walking for third/top-down. Keep flight available. Reuse existing controller only where this stays clear and bounded; otherwise return a scoped proposal before rewriting it.
**Owning files:** PLAYER camera/config/controller/UI only as necessary; site comparison markers; player/world docs.
**Preserve/out of scope:** Existing F6/button semantics unless deliberately documented, F9 input blocking, mouse capture/focus behavior, movement events, terrain camera registration and dynamic collision coverage. No combat camera, aiming, character customization or full animation framework. No implementation of TD0–TD3.
**Data/performance:** Group mode settings, offsets, pitch, orthographic size and zoom bounds. Use one active Camera3D; register it with Terrain3D after switching. Inspect any fog/weather consumers of camera/player positions. Camera follow runs only while moving/settling; idle pose sleeps; no scene searches per frame.
**Pass:** Switching retains location and staged night; collision/movement remain reliable, body visibility is correct, top-down directions make sense and UI does not move the player. Check terrain coverage with offset cameras. User chooses which perspectives deserve further investment.
**Assistant rationale:** Regular Chat can handle fixed viewpoints and small isolated additions. If playable modes demand a broad shared-controller rewrite, document that boundary and let Ben choose a smaller scope or assistant; no automatic escalation.

## PL4 — Focused polish and handoff

**Objective:** Improve only what the selected views reveal.
**Dependencies:** User feedback on PL2/optional PL3.
**Scope:** Tune lamp spacing, window contrast, painted lines, car silhouettes and a few props (bin, bollards, sign). Improve apron edges/approach if needed. Consider wet asphalt only after the dry baseline works; avoid adding expensive reflections as a default.
**Owning files:** Site assets/config/docs; camera files only for explicitly requested tuning.
**Preserve/out of scope:** No world expansion beyond this location, asset-pack hunt, full interior, vehicle mechanics or new gameplay.
**Performance:** Reuse static materials; keep prop/light counts bounded and dormant behavior intact.
**Pass:** User can walk the approach and compare agreed views at night; record known limitations and next choice without claiming visual/performance acceptance prematurely.

## Validation and delivery for every authorized job

Keep checks brief and proportional per CodePreferences: inspect diff/references, run an initial C# build/basic scene check when practical, and report unavailable tools. Leave extended runtime/visual/FPS work to Ben. Never mark a check passed merely because code was pushed.

Handoff: changed files, one commit SHA, checks actually run, exact deferred local checks, known limitations, wake/sleep triggers, remaining idle work and scaling costs. Update this phase table and owning READMEs. Use Planned → In Progress → Implemented → Awaiting Validation → Accepted accurately; user visual acceptance remains separate from compilation.

## Suggested next instruction to Regular Chat

"Read the root README, CodePreferences and WORLD/PARKING_LOT_IMPLEMENTATION_PLAN.md, then inspect the current owning files. Implement PL0 only: a compact reusable convenience-store exterior/parking-lot scene using simple geometry, 3–4 parked cars, sidewalk and lamp placements. Keep lighting behavior, world integration and camera changes for their later phases. Keep validation brief, update docs, and commit once."

Possible simplifications: authorize PL0–PL1 together if the layout needs real terrain context; complete PL2 before detailed art; use fixed comparison viewpoints before committing to playable camera modes. Review this plan first. No subsequent phase is automatically authorized.
