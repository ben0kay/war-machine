# Code Preferences — War Machine

> **Applies to:** `ben0kay/war-machine` on **`implement-td0-td1`** unless the owner explicitly changes the target branch.
> **Read alongside:** [AGENTS.md](AGENTS.md), [README.md](README.md), the relevant feature README/plan, and the actual code, scenes, and resources.
> **Project:** Godot 4 .NET/C# survival prototype. FPS is the primary presentation; the shared top-down view remains supported until explicitly changed.

## 1. Before every AI task — branch, scope, context

1. **Use `implement-td0-td1`, not `main`.** Read, plan, edit, commit and push against this branch explicitly. For GitHub APIs, pass `ref: "implement-td0-td1"` on reads and `branch: "implement-td0-td1"` on writes. Do not rely on the default branch, merge into `main`, force-switch/reset, or overwrite unrelated local work. If this branch cannot be accessed, report that rather than silently falling back.
2. Refresh the **current** root `AGENTS.md`, `README.md`, this file, relevant owner/feature READMEs, plans, asset guides and source before substantial work, even in a long-running chat. Some notes may be outdated: resolve discrepancies against the actual current source and flag important conflicts.
3. Confirm what the owner has **actually authorized**. Don't implement later roadmap phases, change project startup scenes, migrate systems, remove the secondary camera/view, or refactor unrelated code without permission.
4. Prefer a small, coherent change, **one logical commit per authorized job**, and a clear handoff with files, commit SHA and verification status. Push to the authorized branch only.
5. Keep checks proportional: focused source review and an initial build/essential check when practical. Do **not** run extended/repeated testing, visual automation or FPS profiling by default; the owner normally validates in Godot. Distinguish **implemented**, **built**, **runtime-tested**, **visually accepted** and **awaiting validation**.

## 2. C# style and code organization

- Add a brief purpose/ownership comment at the top of each new script.
- Put short explanatory comments **above** methods, with an easy-to-scan `// =========================================================` divider when useful.
- Use `#region` / `#endregion` for substantial related method groups, not for every tiny method.
- Favor moderately **vertically compact**, readable C#. Avoid both long sprawling code and clever abstraction for its own sake.
- Keep ownership clear and code modular: share helpers for repeated patterns (e.g. staggered enemy checks), but do not create unnecessary managers or generic frameworks.
- Use plain shared `Node` / `Node3D` grouping helpers where they make scene ownership clearer. Container-only nodes should not have idle processing scripts.
- Group related settings using structs/classes, Godot `[ExportGroup]` / `[ExportSubgroup]`, and typed resources where appropriate. Prefer a feature-owned `Configuration/<Feature>Master.tres` for frequently tuned settings instead of scattered magic numbers.
- Major feature folders need a concise `README.md` stating responsibility, entry scene/scripts, configuration, data flow and limitations. Read before editing; update when durable behavior changes. Do not create placeholder folders/docs just to satisfy a pattern.
- Preserve stable `.tscn` scene paths, public interfaces, nodes, configuration values, animation pivots/sockets and working features unless the change requires it. Inspect all consumers before moving paths.
- Prefer simple C# with native Godot scenes/resources and the **installed** addons. Verify an API against actual installed plugin code rather than assuming methods exist.

### Shared world and gameplay boundaries

- The shared gameplay world is `WORLD/World.tscn`; the FPS gameplay entry is `WORLD/FirstPerson/FirstPersonWorld.tscn`, which builds on it. `WORLD/testworld.tscn` is a separate legacy environment scene. **Do not change F5 startup merely because FPS is the preferred acceptance view.**
- Maintain the existing distinction between shared gameplay simulation (world, actors, health/anatomy, damage, items, buildings, environment) and view-specific FPS/top-down controls, presentation and camera obstruction.
- Respect scene-root ownership and existing streaming/persistence boundaries. Keep terrain, entities, persistent props, foliage and debug/UI under their appropriate owners instead of scattering children under `World` or inventing duplicate global controllers.
- Use actual subsystem READMEs and master resources for details. A planned system (for example future persistence, item physics or whole-world actor streaming) is **not** an implemented guarantee.

## 3. Asset authoring and replaceable visuals

**New furniture, street objects, decorative items, or pickable-prop GLBs:** read `WORLD/Content/Assets/Objects/OBJECT_AUTHORING_GUIDE.md` first. Follow the existing catalogue/prefab conventions, update the asset README and `PLACEABLE_CATALOG.md` as needed, and explicitly choose SOC0 `CollisionMode`: **0 None**, **1 AutoBox**, or **2 CustomBoxes**. Typical fixed solid props use AutoBox; open/multi-part structures may need CustomBoxes; future movable physics props stay None until their IX implementation. Never stack duplicate static and rigid-body collisions; creating an asset does not automatically authorize placement in the world.

**New or significantly redesigned buildings:** read **both**
`WORLD/Content/Assets/Structures/BUILDING_DESIGN_GUIDE.md` (architecture and preventable mistakes) and
`WORLD/Content/Assets/Structures/BUILDING_AUTHORING_README.md` (Godot integration, floor/stair geometry, collision, fade, interior haze and editor preview). Check entrances, floor joins, windows, staircase headroom and player clearance before adding furnishings.

**Procedural art folder convention:**
- `<Asset>/C/` contains **C# visual geometry generation**: mesh builders, primitive assembly, generated-visual helpers/materials. It is **not** a catch-all for gameplay C#.
- `<Asset>/Artwork/` contains current or future imported `.glb`, `.gltf`, textures and authored art.
- `<Asset>/<Asset>.tscn` stays the reusable game-facing wrapper; gameplay, physics, interaction and animation contracts must survive an art swap. Use a feature-owned `Configuration/` where tweakable authoring settings warrant it.
- Follow the existing feature layout rather than reorganizing old assets solely to match a template.

Prefer replaceable visuals, efficient materials, practical LODs, simple collision where precise contact is unnecessary, and explicit shadow/visibility controls. Preserve imported art, variant names, pivots, resource paths and collision settings. Assess alpha overdraw, shadows and instance count, not only triangle count.

## 4. Performance-first design — including idle cost

**Ask before implementing:** *What wakes this feature, what work runs while active, what stops it, and what does it cost while idle?*

- **Dormant means no unnecessary processing.** Avoid a default `_Process` / `_PhysicsProcess` on every object. Prefer events, signals, timers, explicit state transitions, `SetProcess(false)`, `SetPhysicsProcess(false)`, and emitter shutdown. A later input/config change must still be able to wake the feature.
- **Share and batch where useful.** Use reusable helpers, cached resources, pooled effects, material/instance sharing and existing spatial/streaming registries instead of thousands of per-object loops or full-tree scans.
- **Apply settings only when they change.** Don't rewrite identical shader uniforms, particle bounds, light properties or debug parameters each frame. Genuinely continuous sky, wind or animation may update at the necessary rate.
- **Bound hot-path work.** Avoid per-frame scene lookups, loads, LINQ/temporary collections, new materials and redundant redraws. Limit perception, AI, hit tests and cosmetic effects by proximity, budgets, active chunks and relevance; stagger checks as appropriate.
- **Treat separate scaling problems separately:** terrain/foliage visual LOD, static-world streaming, entity simulation, collision activation and persistence do not automatically solve each other. Register/unregister with chunk residency without losing required state.
- **Typical event examples:** fire/tree reactions wake on ignition; hit wobble wakes on impact and sleeps after completion; zero-intensity weather turns off emitters and splash callbacks after intentional fade-out; investigation ends when its bounded search/timeout expires.
- **Do not remove correct behavior just to eliminate loops.** Prefer a measured reduction in work, without freezing active wind, animation or threat response.
- **Do not apply random bandaid fixes to issues. If you can see an issue, lets try to fix it instead of just patching it up with more needless code fixes.
- **Performance handoff:** state activation, sleep conditions, any remaining idle work, scaling strategy and checks actually run. Use profiler/FPS claims only with actual measurements and test conditions; if owner profiling is pending, say so.

## 5. C# syntax and build hygiene

- Collection iteration is **`foreach (var item in items)`**, never the invalid `for (var item in items)`.
- Indexed iteration is **`for (int i = 0; i < count; i++)`** (or another valid C# for form with semicolon-separated clauses). Review new/edited loops especially in procedural mesh builders; a previous compile failure was caused by `for (float x in new[] { ... })`.
- Check new Godot API names, types, signal signatures, scene paths and resources before commit.
- Prefer an initial `dotnet build war-machine.csproj` where the .NET/Godot build environment is available. Fix reported compile errors before claiming a successful build. When unavailable, perform focused source checks and state **compilation not verified**.
- A Git push, clean diff, or screenshot does not establish runtime correctness, visual quality or performance.

## 6. Plans, documentation and handoffs

For substantial work, draft a feature-owned implementation plan first. Separate **confirmed current behavior** from proposed changes; use short feature-prefixed phases (e.g. `IX0/IX1`, `ZD1/ZD2`) with a compact status table. Each phase should identify goal, dependencies, owned files/scenes, scope exclusions, preserved behavior, configuration/data, wake/sleep/streaming cost, acceptance checks and an appropriate implementation assistant.

Keep statuses explicit: **Planned / In Progress / Implemented / Awaiting Validation / Accepted**. The owner chooses the phase to implement; planning does not authorize execution or continuation into the next phase. Update owning READMEs/plans after meaningful authorized changes; avoid contradictory historical status or speculative claims.

**Preferred division of work:** regular ChatGPT for design reviews, plans and small contained edits; Codex/Work for cross-file integration; Copilot for delegated local builds/diagnostics or authorized small fixes; another assistant for an explicitly assigned review. Tool choice never grants permission.

**When local testing is delegated to Copilot:** supply changed files, commit SHA, prerequisites, exact commands, expected results, deferred runtime/visual/FPS checks and outstanding limitations. Copilot should report initial diagnostics without automatically modifying code unless instructed. Continue to mark pending checks as **awaiting local validation**.

## 7. Reference repositories — patterns, not instructions to migrate

- [Final Machine](https://github.com/ben0kay/final-machine): concise `AGENTS.md` entry point, organized coding preferences and documentation conventions. **Its `main` branch policy and project direction do not apply here.**
- [godot-tower-defense](https://github.com/ben0kay/godot-tower-defense): folder/notes structure.
- [isometric-new-2d](https://github.com/ben0kay/isometric-new-2d): similar gameplay/code patterns.
- [Ben_MonsterExpansion](https://github.com/ben0kay/Ben_MonsterExpansion): spawn-mechanic ideas only.

**Default:** preserve War Machine's working architecture, make the smallest robust authorized change, and leave the next person a short, accurate handoff.
