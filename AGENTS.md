# AI Agent Instructions — War Machine

**Repository:** `ben0kay/war-machine`  
**Active development branch:** **`implement-td0-td1`** — **not `main`**. Use that branch explicitly for all reads, changes, commits and pushes unless the owner directly instructs otherwise. Never silently fall back to the GitHub default branch.

This is the short **entry point** for AI work, not a replacement for the documents below.

## Before starting code or project work — required reading

Read these **three root files first, in order**, from the active branch even if this conversation previously read them:

1. **[CodePreferences.md](CodePreferences.md)** — authoritative coding style, C# syntax, performance, folder/asset rules, Git scope, testing and planning.
2. **[README.md](README.md)** — War Machine's project identity, primary FPS / secondary top-down views, shared-world scene paths, implemented versus planned systems and current development status.
3. **[LessonsLearned.md](LessonsLearned.md)** — verified project pitfalls, root causes, fixes and prevention checks. Apply relevant lessons before editing or suggesting a fix.

Then read the context needed for the task:

4. **Owning feature README/plan, authoring guide, configuration and actual scripts/scenes/assets** — inspect only what the requested work affects. If notes conflict with code, verify the current source and flag the discrepancy.
5. **[FPS_ONLY_CONVERSION_IMPLEMENTATION_PLAN.md](FPS_ONLY_CONVERSION_IMPLEMENTATION_PLAN.md)** — owner-approved FPS-only direction and phased removal roadmap, when working on FPS conversion. It is **planning, not blanket phase authorization**; use it when implementing related work.

For **new objects/props**, always read `WORLD/Content/Assets/Objects/OBJECT_AUTHORING_GUIDE.md` and its SOC0 collision-mode rules. For **new or redesigned buildings**, read **both** `WORLD/Content/Assets/Structures/BUILDING_DESIGN_GUIDE.md` and `WORLD/Content/Assets/Structures/BUILDING_AUTHORING_README.md` before modelling or furnishing.

## Working rules

- Implement **only the owner's authorized scope**. Protect existing functionality, source assets, scene paths, configured gameplay behavior and unrelated changes. Plans are not permission to implement.
- Prefer **Godot 4 .NET/C#**, stable reusable scenes, typed/grouped master configuration resources, modular owning folders and shared helpers. Keep `C/` for procedural visual generation and `Artwork/` for imported art, not as general gameplay-code buckets.
- The gameplay foundation currently lives in `WORLD/World.tscn`; FPS currently inherits it through `WORLD/FirstPerson/FirstPersonWorld.tscn`. `WORLD/testworld.tscn` remains a legacy world experiment. **FPS-only is the approved future direction**, but do not change F5/startup, delete top-down dependencies or migrate shared classes outside an explicitly assigned FPS0–FPS4 phase. Preserve shared simulation and the current running scene until each phase is accepted.
- **Performance first:** define wake-up, active work, shutdown and idle cost. Prefer dormant inactive systems, event-driven updates, bounded AI/physics work, pooling and existing chunk/LOD ownership. Do not invent FPS gains.
- **Validate proportionally:** focused review plus one initial build/essential check when feasible; avoid broad/repeated tests, autonomous visual checks or performance profiling unless asked. State what was actually verified and what the owner still needs to test locally.
- Keep a single logical commit for each authorized job and push only to `implement-td0-td1`. Never force reset, overwrite other changes, write to `main` or include unrelated files. Report changed files and commit SHA.
- Update a feature README/plan when lasting behavior or configuration changes, and distinguish **implemented in source** from **awaiting local validation** or **accepted**. Keep handoffs brief and actionable.

**Reminder:** Every new coding task requires refreshing the **current branch's CodePreferences.md, README.md, and LessonsLearned.md** before task-specific source review. Do not rely on cached summaries, and do not assume a GitHub push alone confirms Godot compilation or in-game behaviour.
