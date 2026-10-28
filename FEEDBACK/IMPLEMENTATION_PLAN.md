# FB — Status and combat feedback implementation plan

> **Current status (2026-10-28):** Reusable numeric HealthBar and shared NPC overlay already exist in source (local runtime validation pending); PH0 player vitality/body-region state is still planned. **New owner decision:** PH1/FB1 player UI will be a six-region body silhouette, NOT a player HP bar. When PH0 arrives, exclude player HealthComponent from the current overlay's bottom-left numeric bar branch, leaving NPC bars as-is. FB3 defence and FB4 effects remain separately planned.

### Implemented generic health bar — integration and validation

A reusable `Bars/Health/HealthBar.tscn`/`.cs` draws current/max HP. `HealthComponent` now emits `HealthChanged(HealthComponent)` after successful damage and configuration and registers in the `health_components` group. `Bars/EnemyBars/HealthBarOverlay.cs` observes this group and SceneTree additions/removals, handles any shared-health actor, and projects its bar only while `CurrentHealth < MaxHealth`; dead/unloaded/fully healed actors lose their bar. Current overlay code contains a bottom-left numeric player bar path, but **PH0 must disable/exclude the player's registration** as part of the revised BodyCondition design. Only NPC bars should remain; there is still no actual player health. One controller initializes it once for overhead and inherited FPS; no per-zombie UI nodes.

The shared presenter processes only while at least one actor is injured, at an Inspector-tunable max 30Hz with 48 pooled visible bar controls by default. Offscreen bars hide and return on screen reentry; wounded actors offscreen still incur the throttled shared projection check. **No occlusion ray:** visibility through world walls/roofs is a known limitation for later tuning. Bar settings in `Configuration/FeedbackMaster.tres`, visual colours/style in `HealthBar.tscn`.

**Run locally:** `dotnet build war-machine.csproj`. F6 `WORLD/World.tscn` and the FPS test. Confirm fresh zombies (100/100) have no bars; first rifle hit (5/100 remaining) shows a thin red bar; lethal second shot removes it. Confirm runner, 48+ wounded, camera panning/zoom, offscreen return, full-health configure/reset, scene unload and mouse/UI input. Once PH0 is authorized later, verify the player is excluded from the numeric overlay even when injured, while NPC bars still behave normally. PH1 then adds the six-region body silhouette. This change was only statically reviewed; build/Godot visuals remain **awaiting validation**.

> **Implementation state:** FB0 scaffold complete; generic shared HealthBar plus FB2 overlay implemented in source (Godot validation pending), PH1/FB1 **player silhouette not implemented**, FB3/FB4 planned. Follow the authoritative 2026-10-28 revision above; older scaffold-only statements are historical. Work on `implement-td0-td1`.

## Phase overview

| Phase | Objective | Suggested assistant | Status |
| --- | --- | --- | --- |
| **FB0** | Create reusable FEEDBACK folders, owning READMEs, phase plan and cross-links | Regular Chat | **Implemented — docs only** |
| **FB1** | Six-region player BodyCondition silhouette; reuse existing NPC bar separately | UI design + Codex/Work | **Planned — requires PH0** |
| **FB2** | Damage-triggered enemy bars for zombies and future entities | Codex/Work integration; Regular Chat tuning | **Planned — health events already available for zombies** |
| **FB3** | Optional defence/shield bar for actors with **actual depletable shield state** | Codex/Work | **Planned — dependent on a future shield resource** |
| **FB4** | Damage numbers, hit indicators and later status icons | Regular Chat visual plan; Codex/Work for integration | **Planned** |

Each phase is a separate authorization gate. Do not implement another because its prerequisite is complete.

## Confirmed baseline

- Zombie/runner health and death use `ENTITIES/Shared/Combat/HealthComponent.cs`, configured by `ZombieSettings.HealthProfile`. `HealthComponent` has `Damaged`, `Died` and already-existing `HealthChanged` events, `CurrentHealth`, `MaxHealth` and `IsDead`. Its `Damaged` payload reports resolved incoming damage, **not actual HP delta on overkill**; read current health for bar values.
- `PLAYER/PlayerController.cs` currently has **no health receiver**. The existing [PLAYER plan](../PLAYER/IMPLEMENTATION_PLAN.md) defines PH0 player health, healing, hit reception and death; keep player stats in PLAYER, not here.
- `UI/GameplayHUD/Hotbar/Hotbar.tscn` is a static screen-space hotbar in `WORLD/World.tscn`; the FPS test inherits it. Don't edit/replace the hotbar as part of FB0.
- Existing flat armour/resistances in `HealthSettings` mitigate incoming damage; neither is a currently depleting defence pool.
- FB0 adds only `.md` files. There is **no FeedbackMaster.tres yet**, since an exported settings class and corresponding consuming widget do not exist.

## FB1 — player body-condition silhouette (with PH1)

**Objective:** PH1/FB1 presentation of the player's **six-region BodyCondition** instead of a numeric vitality/HP bar. PH0 must supply region values and events before this player-facing UI can be implemented. The generic `HealthBar.tscn` and NPC overlay are already implemented separately and remain NPC-specific.

**Ownership:** `FEEDBACK/Bars/PlayerStatus/BodyConditionWidget.tscn` and visual-only script consume `BodyCondition` read-only snapshots, with presentational colours/size under `FEEDBACK/Configuration/FeedbackMaster.tres`. Compose a single noninteractive widget under `UI/GameplayHUD/Screen` beside hotbar/backpack for both overhead and inherited FPS; no new HP arithmetic in FEEDBACK.

**Initial style:** six-shaped anatomy silhouette, left/right from player's own perspective; healthy muted/neutral, injured amber, severe red, incapacitated dark red and a simple non-colour indicator for accessibility. Full health remains unobtrusive; recent damage can briefly pulse. Real bleeding/fracture icons require implemented conditions, not fake placeholders. No 100/100 bar and no six literal shrinking bars. Future inventory Health tab is separately authorized.

**Integration:** PH0 must exclude the player-owned `HealthComponent` from the existing generic `HealthBarOverlay` player bottom-left branch while preserving automatically registered zombie/deer bars. PH1 binds `ConditionChanged`/death events once, reads all six initial values immediately, updates only on changes and unsubscribes cleanly. `Control.MouseFilter=Ignore`, no persistent polling or overlapping hotbar.

**Pass:** inspect all six individually, healed states, 0 condition, vitality death, compact layout at top-down/FPS resolutions, no duplicate player bar, no interference with NPC health bars, inventory or mouse shooting. Compile and visually verify in Godot. No new health gameplay is authorized by FB1.

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
