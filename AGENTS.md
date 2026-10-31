# AI Agent Instructions — War Machine

**Repository:** `ben0kay/war-machine`  
**Active development branch:** **`implement-td0-td1`** — **not `main`**. Use that branch explicitly for all reads, changes, commits and pushes unless the owner directly instructs otherwise. Never silently fall back to the GitHub default branch.

This is the short **entry point** for AI work, not a replacement for the documents below.

## Start each substantial task

Refresh the current repository sources even if this conversation read them earlier:

1. **[CodePreferences.md](CodePreferences.md)** — authoritative coding style, C# syntax, performance, folder/asset rules, Git scope, testing and planning.
2. **[README.md](README.md)** — War Machine's actual project identity, primary FPS / secondary top-down views, shared-world scene paths, existing versus planned systems and current development status.
3. **Owning feature README/plan, authoring guide, configuration and actual scripts/scenes/assets** — inspect only what the requested work affects. If notes conflict with code, check the source and flag the discrepancy.
4. **[FPS_ONLY_CONVERSION_IMPLEMENTATION_PLAN.md](FPS_ONLY_CONVERSION_IMPLEMENTATION_PLAN.md)** — owner-approved FPS-only direction and phased removal roadmap. It is **planning, not blanket phase authorization**; use it when implementing related work.

For **new objects/props**, always read `WORLD/Content/Assets/Objects/OBJECT_AUTHORING_GUIDE.md` and its SOC0 collision-mode rules. For **new or redesigned buildings**, read **both** `WORLD/Content/Assets/Structures/BUILDING_DESIGN_GUIDE.md` and `WORLD/Content/Assets/Structures/BUILDING_AUTHORING_README.md` before modelling or furnishing.

## Working rules

- Implement **only the owner's authorized scope**. Protect existing functionality, source assets, scene paths, configured gameplay behavior and unrelated changes. Plans are not permission to implement.
- Prefer **Godot 4 .NET/C#**, stable reusable scenes, typed/grouped master configuration resources, modular owning folders and shared helpers. Keep `C/` for procedural visual generation and `Artwork/` for imported art, not as general gameplay-code buckets.
- The gameplay foundation currently lives in `WORLD/World.tscn`; FPS currently inherits it through `WORLD/FirstPerson/FirstPersonWorld.tscn`. `WORLD/testworld.tscn` remains a legacy world experiment. **FPS-only is the approved future direction**, but do not change F5/startup, delete top-down dependencies or migrate shared classes outside an explicitly assigned FPS0–FPS4 phase. Preserve shared simulation and the current running scene until each phase is accepted.
- **Performance first:** define wake-up, active work, shutdown and idle cost. Prefer dormant inactive systems, event-driven updates, bounded AI/physics work, pooling and existing chunk/LOD ownership. Do not invent FPS gains.
- **Validate proportionally:** focused review plus one initial build/essential check when feasible; avoid broad/repeated tests, autonomous visual checks or performance profiling unless asked. State what was actually verified and what the owner still needs to test locally.
- Keep a single logical commit for each authorized job and push only to `implement-td0-td1`. Never force reset, overwrite other changes, write to `main` or include unrelated files. Report changed files and commit SHA.
- Update a feature README/plan when lasting behavior or configuration changes, and distinguish **implemented in source** from **awaiting local validation** or **accepted**. Keep handoffs brief and actionable.

**Reminder:** `AGENTS.md` being read in a long conversation means rereading the linked *current branch files* before new substantive work — not relying on cached summaries.
