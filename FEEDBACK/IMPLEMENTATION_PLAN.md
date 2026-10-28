# FB — Status and combat feedback implementation plan

> **Status:** **FB0 Implemented (directory + docs only)**; **FB1–FB4 Planned**. No HUD bars or effects are currently functional as a result of this scaffold. Implement only a phase explicitly authorized by the owner. Work on `implement-td0-td1`.

## Phase overview

| Phase | Objective | Suggested assistant | Status |
| --- | --- | --- | --- |
| **FB0** | Create reusable FEEDBACK folders, owning READMEs, phase plan and cross-links | Regular Chat | **Implemented — docs only** |
| **FB1** | Reusable health bar + player health HUD widget | Regular Chat layout; Codex/Work integration | **Planned — requires PH0** |
| **FB2** | Damage-triggered enemy bars for zombies and future entities | Codex/Work integration; Regular Chat tuning | **Planned — health events already available for zombies** |
| **FB3** | Optional defence/shield bar for actors with **actual depletable shield state** | Codex/Work | **Planned — dependent on a future shield resource** |
| **FB4** | Damage numbers, hit indicators and later status icons | Regular Chat visual plan; Codex/Work for integration | **Planned** |

Each phase is a separate authorization gate. Do not implement another because its prerequisite is complete.

## Confirmed baseline

- Zombie/runner health and death use `ENTITIES/Shared/Combat/HealthComponent.cs`, configured by `ZombieSettings.HealthProfile`. `HealthComponent` has `Damaged` and `Died` events, `CurrentHealth`, `MaxHealth` and `IsDead`. Its `Damaged` payload reports resolved incoming damage, **not actual HP delta on overkill**; read current health for bar values.
- `PLAYER/PlayerController.cs` currently has **no health receiver**. The existing [PLAYER plan](../PLAYER/IMPLEMENTATION_PLAN.md) defines PH0 player health, healing, hit reception and death; keep player stats in PLAYER, not here.
- `UI/GameplayHUD/Hotbar/Hotbar.tscn` is a static screen-space hotbar in `WORLD/World.tscn`; the FPS test inherits it. Don't edit/replace the hotbar as part of FB0.
- Existing flat armour/resistances in `HealthSettings` mitigate incoming damage; neither is a currently depleting defence pool.
- FB0 adds only `.md` files. There is **no FeedbackMaster.tres yet**, since an exported settings class and corresponding consuming widget do not exist.

## FB1 — player health bar and reusable core

**Objective:** After PH0 supplies real player HP events, display a small, clean screen-space health bar in the gameplay HUD; provide a reusable health-bar scene/style for future other actors.

**Dependencies:** PH0 implemented and locally validated; existing `UI/GameplayHUD` composition, player health current/max and change/death events. **PH0's minimal diagnostic text readout is temporary and not an FB1 bar.**

**Ownership/files (proposed, confirm against actual scene):**
- `FEEDBACK/Bars/Health/`: `HealthBar.tscn` / `HealthBar.cs`, visual-only value binding, no HP arithmetic.
- `FEEDBACK/Bars/PlayerStatus/`: player-facing status presentation, optional script that binds to player health at startup and disconnects on exit.
- `FEEDBACK/Configuration/`: a proper `FeedbackSettings.cs` exported class and `FeedbackMaster.tres` only when tuning fields are actually consumed; e.g. colours, thickness, labels, hit flash/animation duration.
- `UI/GameplayHUD/` and `WORLD/World.tscn` or `WorldController` only for **one** correct instance/binding shared by top-down + inherited FPS.
- Update owning READMEs, not the damage pipeline except if a narrow missing event API is confirmed.

**Implementation:** Bind after player's PH0 initialization; set initial current/max once; subscribe to health change and death; clamp displayed percentage, handle zero/max/overheal safely. Preserve hotbar. Noninteractive Control mouse filter Ignore. A hit pulse tween may run only while active and stop afterwards. Rebind/scene exit must unsubscribe.

**Out of scope:** Damage/healing logic, enemy bars, actual shield mechanics, armour formula, inventory, respawn.

**Performance/pass:** Zero idle updates, no per-frame health polling, one shared world HUD instance; on F6 world and FPS (inheritance) see player bar 100/100, one update per applied hit/heal, clamped full/empty, no ghost subscriptions after reload, no input obstruction or hotbar overlap. `dotnet build war-machine.csproj`, both F6 views, proportional visual check.

## FB2 — temporary overhead enemy bars

**Objective:** Show simple health bars for damaged zombies/runners and later any actor exposing the required HP signals, in the primary angled top-down view.

**Dependencies:** Existing zombie HealthComponent + notification contract. Can proceed independently of FB1; reuse FB1 bar style if available but don't require player HP.

**Ownership/files:** `FEEDBACK/Bars/EnemyBars/` presenter/manager, reusable `Bars/Health` widget when present, per-actor optional head-height marker (or validated height offset), `WORLD/` one-time registration composition only. FPS can reuse the same component selectively but should not get an independent duplicated manager.

**Implementation:** Bind nearby/relevant actors when spawned/registered; react to `Damaged` to reveal/update with *current/max* HP; hide after a configurable short idle period (suggest start 2.5s), when actor exits view/LOS, dies, deactivates or streams out. For top-down, project a point above the head into screen coordinates; clamp/hide offscreen rather than showing detached bars. Avoid leaking information through buildings: use a bounded camera occlusion policy when visible, preferably reusing existing building/visibility constraints. Test house/roof fade interaction. Do **not** allocate a permanent Control/physics loop for every world zombie. Reuse/pool a small capped set of active bars; iterate only active visible candidates.

**Out of scope:** Enemy HP calculations, damage system rewrites, always-on enemy nameplates, full mob UI, per-zombie Camera3D/CanvasLayer nodes.

**Performance/pass:** At idle all enemy bars hidden with no per-entity feedback processing; near-hit reveals exactly one correct bar, repeated hits update, death/unload clears, 30+ zombies do not generate 30 permanent UI ticks; overhead and FPS display/camera registration tested separately. Compile, F6 World and FPS, wall/cover/screen edge checks.

## FB3 — defence/shield visuals

**Objective:** Display an optional additional meter **only for a real consumable defensive resource** (e.g. shields, guard, barrier), driven by its own max/current changed events.

**Dependencies:** A separately planned and approved shield/guard stat; `HealthSettings.Armour` and resistance percentages are not a depletable pool and must not be mapped directly to a bar. FB1/FB2 style reuse optional.

**Ownership:** `FEEDBACK/Bars/Defence/` generic visual widget, consuming actor-owned defence change API; config defaults under `FEEDBACK/Configuration/`. No shield simulation inside FEEDBACK.

**Performance/pass:** Optional bar remains absent when actor has no shield, event-driven update and deterministic hide at zero/full according to approved UX, no event leaks or idle work. Compile and relevant scene test.

## FB4 — optional combat effects

**Objective:** Readable, restrained hit confirmation and status feedback in `FEEDBACK/Effects/`.

**Ownership:** `DamageNumbers` (optional aggregated floaters), `HitIndicators` (player hit direction/damage cue), `StatusEffects` (status icons only when real mechanics exist). Reuse existing contact/damage events; visual effects never decide damage or hit eligibility.

**Scope/performance:** Choose subfeature per separate approval. Pool/cap active objects, respect off-screen/occlusion, stop/return to pool on completion and despawn. For FPS, consider screen-space direction indicator; for overhead, readability at camera zoom first. No all-entity polling, no always-on animation effects.

## Handoff and acceptance

Start by reading root `README.md`, `CodePreferences.md`, `FEEDBACK/README.md`, this plan, `PLAYER/IMPLEMENTATION_PLAN.md` for PH0 boundaries, and actual current source/scene. Codex should implement **only the approved FB phase**, document files/tuning, make one commit to `implement-td0-td1`, then report static/build/run checks separately. Owner accepts visual styling after local Godot testing. FB0 needs no C# build because it introduces docs only.
