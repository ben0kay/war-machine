Read the main README.md in the project root for an overall flavour of the game/coding structure. This is C#.
I prefer comments above functions/methods. along with a commented line of ========================================================= so i can easily see when editing code in godot
I prefer some comment at top of each script to explain what the script is doing.
I prefer a .md file inside each major folder to explain whats inside this folder.
I prefer a masterconfig file inside each major capslock folder to group easily frequent tweakable settings
I prefer you organizing functions/methods into regions when appropriate.
I prefer to organize my scripts for ease of finding in future etc.
I do like you to vertically compress code where possible, so it's a little easier to read on screen.
I also prefer my code to be not overly complex, more like simplicity, while still achieving the gameplay I desire + performance friendly
I like re-using code when possible, I.E we use a helper function for staggered checks for enemies and things, similar to my Macros i had in my previous gamemaker vector game etc.
Please keep in mind I like optimization where possible too. Performance oriented.
I like keeping things containted and modular for easy expansion and easy tweaking of things without having to rewrite other things.
I like Node helpers as well so that the scene trees to not become cluttered. we should group common things in a shared plain node.
I like grouping similar variables in structs / export groups for inspector so it shows sub categories etc.
Check out my isometric 3d and isometric 2d github repositories for code/folder structure references.
If an .md file is available in the project for a section/feature/mechanic we are editing, refer to the .md as sometimes it will have extra context. Some might be outdated. Update md files appropriately when completing work.
Only do one commit for github per job instead of multiple commits.


Refer to https://github.com/ben0kay/godot-tower-defense for folder / notes structure
Refer to https://github.com/ben0kay/isometric-new-2d for similar game mechanics
Refer to https://github.com/ben0kay/Ben_MonsterExpansion for Grim Dawns spawn mechanics just for ideas

## Performance-first rules for every new mechanic

**Design for low idle cost from the beginning, not just functional correctness.** Before writing a feature, identify what activates it, what work happens while active, and exactly what stops that work afterward. Avoid introducing an always-running `_Process` or `_PhysicsProcess` by default.

- **Inactive means dormant:** If an effect is off, intensity is zero, or its animation has finished, it should not keep simulating, checking objects, redrawing, or rewriting GPU properties. Use `SetProcess(false)`, `SetPhysicsProcess(false)`, emitter shutdown, and explicit activation where suitable. Keep a way for UI/config changes to wake it without idle polling.
- **Prefer triggers and signals:** A tree should process fire only after `TryIgnite()` triggers ignition; an axe-hit wobble should animate only after a hit and stop afterward. Do not give every idle tree a fire-checking loop.
- **Rain and snowfall example:** Zero intensity means OFF, not invisible particles still updating. Suspend emission, splash work and unnecessary callbacks; allow intentional fade-out/remaining effects to finish, then sleep. Changing the slider or weather state must reactivate the effect directly.
- **Use shared systems:** Prefer one manager for many objects, pooled effects, shared GPU particles/materials, and cached data instead of thousands of per-instance idle loops.
- **Only update changed data:** Don't assign the same shader uniforms, settings, particle bounds or light properties every frame. Apply configuration when changed, and throttle legitimately continuous cosmetic changes if it preserves the appearance.
- **Respect streaming and visibility:** Limit queries to nearby or active chunks; register/unregister on spawn and unload. Avoid full-world/group scans every frame. Suspend off-screen cosmetic work without breaking persistent simulation.
- **Keep hot paths allocation-free where practical:** Avoid repeated scene-tree searches, resource loads, temporary lists/arrays, LINQ, material creation, or redundant `QueueRedraw()` in frequent callbacks.
- **Do not over-optimize away necessary animation:** Clock, sunlight and active wind may be genuinely continuous. Justify their update frequency and avoid duplicate calculations; don't freeze functioning visuals.
- **Verify idle and active performance:** Test never-active, activation, deactivation, settled zero state, reactivation, many instances, and chunk unload/reload. Use Godot profiler/frame traces to verify idle callbacks/draws and allocations are absent or intentionally minimal. Do not claim measured improvements without measurements.
- **Include a performance handoff:** When implementing, describe the wake-up trigger, sleep/shutdown condition, any remaining idle per-frame work, scaling strategy, and what was actually tested.

**Default design question:** What makes this mechanic run, what makes it stop, and what is its cost while nothing is happening?

These requirements apply alongside the existing modular, data-driven, C# and documentation preferences.

## Optional local testing handoff — when I request it

To conserve Codex usage, I may explicitly ask for compilation and some or all runtime/performance testing to be performed locally by GitHub Copilot in VS Code. When requested, Codex should implement and review the changes, commit/push when authorized, and hand over the deferred checks instead of running them here. This is optional per job; otherwise follow the normal validation requirements above.

Provide the commit SHA, changed files, prerequisites, exact build/test commands, expected results and any manual Godot visual/performance checks. Clearly state which checks were run and which are deferred, and mark the implementation as **awaiting local validation** until results are available. Pushing code does not mean it passed testing. Copilot should initially run the checks without modifying code and return commands, exit codes and relevant failures for follow-up.

## Implementation planning and AI task allocation

Regular Chat is the preferred assistant for drafting and reviewing implementation plans before coding. First read the root README, this file, the owning feature README, relevant plans, and actual current scripts/scenes/configs. Create an implementation-ready Markdown plan in the feature's owning folder; distinguish confirmed current behavior from proposed changes.

Use short numbered phases with feature-specific prefixes (for example C0/C1/C2 or ES0/ES1/ES2). At the top, include a compact phase table showing identifier, goal, suggested assistant and current status. For each phase, specify objective, current dependencies, scope and out-of-scope, owning files/folders, implementation approach, existing behavior to preserve, data/configuration considerations, performance (activation, idle cost, shutdown, streaming), validation/pass criteria, and recommended assistant with rationale. Keep phases independently reviewable and avoid speculative complexity.

Explicitly identify work Regular Chat can safely handle to conserve Codex usage: documentation, planning, design review, simple isolated configuration or code changes. Recommend Codex/ChatGPT Work for major multi-file implementation, shared architecture, complex integrations and performance-sensitive changes; Copilot for local testing/diagnostics and authorized small fixes; Claude for optional second opinions and assigned isolated work. These are suggestions, never automatic permission to edit.

After drafting, explain phases in plain language and proactively suggest possible improvements, simplifications, alternatives, reordering, splitting or deferral. Give the user the opportunity to review and refine the plan before implementation. Never automatically execute a plan or proceed to subsequent phases: implement only the phase/task explicitly authorized by the user.

Maintain clear phase states: Planned, In Progress, Implemented, Awaiting Validation, Accepted. Update the plan and owning README after authorized work; distinguish code completion from automated checks and user visual/performance acceptance. Record commit SHA, known limitations, deferred tests and next handoff. Keep current guidance prominent; relocate lengthy superseded history to validation/archive notes instead of mixing contradictory instructions into active phases. Continue to follow one GitHub commit per job.

Preferred workflow: Regular Chat investigates and drafts → user reviews/improves → user chooses phase and assistant → authorized implementation → validation → documentation update → next phase only on request.
