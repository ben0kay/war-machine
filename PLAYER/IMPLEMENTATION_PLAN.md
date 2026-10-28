# PLAYER — Health, Vitals and Survival Implementation Plan

> **PH0–PH5 remain planned. New dependency note (2026-10-28):** A working generic health-bar UI and `HealthComponent.HealthChanged(HealthComponent)` notification already exist under FEEDBACK. **PH0 Codex must reuse those, not build a second diagnostic label, health bar or changed-event API.** Once PH0 configures the player's HealthComponent, the shared GameplayHUD overlay will display the player's bar at bottom-left only while HP is below max; full health intentionally shows no bar. Current PH0 sections describing a temporary label or missing HP-changed event are superseded by this note. There is still **no player health gameplay implementation**.

> **Plan only — PH0–PH5 are Planned.** No player-health, survival or HUD mechanics are implemented by this document. A separate [FEEDBACK foundation](../FEEDBACK/README.md) now exists as **folders and documentation only**; its bars are not functional. Implement only the phase expressly authorized by the owner, on `implement-td0-td1`.
>
> **Baseline reviewed:** 2026-10-27 against root `README.md`, `CodePreferences.md`, player scene/controller/settings, `WORLD/WorldController.cs` and shared damage pipeline. Updated 2026-10-28 for the new FEEDBACK folder plan; FPS inherits the active world and should remain compatible, while the parked `WORLD/testworld.tscn` remains unchanged.

## Phases at a glance

| Phase | Goal | Suggested assistant | Status |
| --- | --- | --- | --- |
| **PH0** | Player health (100 HP), shared damage reception, healing, death, simple HP readout | Codex / ChatGPT Work | **Planned** |
| **PH1** | Coordinate health HUD and hit/death presentation through FEEDBACK FB1/FB4, without new health mechanics | Regular Chat layout; Codex / Work for integration | Planned |
| **PH2** | Stamina, sprint cost and regeneration | Codex / Work | Planned |
| **PH3** | Hunger and thirst, slow survival updates and initial consequences | Codex / Work | Planned |
| **PH4** | Fatigue, rest/sleep and recovery | Codex / Work | Planned |
| **PH5** | Temperature, bleeding, injuries and environmental survival effects | Regular Chat for design; Codex / Work for systems | Planned |

**Guiding rule:** Keep player-specific state under `PLAYER/`; reuse combat primitives under `ENTITIES/Shared/Combat/`. Movement remains in `PlayerController`. The world composes scenes; it does not own health calculations. Add only the files needed by the authorized phase, not an empty speculative framework.

## Confirmed code and boundaries (not proposed features)

- **Active scene:** `WORLD/World.tscn` (run with **F6 / Run Current Scene**). `WORLD/WorldController.cs` initializes `PLAYER/Player.tscn` and passes a deep-copied `PlayerSettings` to `PlayerController.Initialize(settings, camera)`. The old `WORLD/testworld.tscn` and `PLAYER/LegacyFirstPerson/` are parked.
- **Player:** `PLAYER/PlayerController.cs` is a `CharacterBody3D` responsible for movement, crouch, gait and weapon pose. `PLAYER/PlayerSettings.cs` and `PLAYER/Configuration/PlayerMaster.tres` are its Inspector-editable master settings. `PLAYER/Equipment/PlayerWeaponController.cs` handles player aim/trigger input.
- **Damage foundation:** `ENTITIES/Shared/Combat/DamagePacket.cs`, `DamageResolver.cs`, `HealthSettings.cs`, `HealthComponent.cs`, and `IDamageReceiver.cs` already implement packet-based damage. `HealthComponent` duplicates its authored settings, clamps HP, has `Damaged`/`Died` events and **no idle update loop**. It currently has **no healing method or general HP-changed event**.
- **Existing example:** `ENTITIES/TYPES/Zombies/ZombieController.cs` implements `IDamageReceiver`, configures a `HealthComponent`, and decides how to react to death. The existing rifle deals 95 physical damage; the default zombie profile has 100 HP. Zombies presently wander/chase; **they do not attack the player**.
- **Not present yet:** Player health, player damage intake, player death/respawn, hunger/thirst/fatigue, stamina or survival UI. Robots do not deal damage in the reviewed code.
- **Folder convention:** `C/` is **reserved for procedural mesh builders**, NOT ordinary scripts. New stats/controllers belong directly in their feature folders.

## Intended folder ownership

```text
PLAYER/
  PlayerController.cs               # existing: movement, stance, animation, weapon pose
  PlayerSettings.cs                 # existing: editable player master fields
  Player.tscn                       # existing: player root + collision
  Configuration/
    PlayerMaster.tres               # existing: one player master, add grouped vitals profile
  Stats/                            # README scaffold exists; PH0 later adds actual state and adapter
    PlayerVitals.cs                 # new: player health wrapper, future vitals signals
    PlayerVitalsSettings.cs         # new: future-extensible profile containing HealthSettings
    README.md                       # existing scaffold; PH0 updates it
  Survival/                         # PH3+ only (rates, exposures, consequences)
    README.md                       # create with first survival implementation
  Equipment/                        # existing: weapon adapter; reacts to player death
  LegacyFirstPerson/                # parked: no change
  IMPLEMENTATION_PLAN.md            # this plan

ENTITIES/Shared/Combat/             # existing: damage packets, resolver, health, receiver
WORLD/World.tscn                    # active scene: wires player and minimal visible readout
WORLD/WorldController.cs            # composition/event binding only, not damage formulas
FEEDBACK/                            # FB0 folders/docs exist; actual bars and effects in approved FB1+ phases
UI/GameplayHUD/                      # existing hotbar; owns screen-space arrangement
```

**Proposed scene outline:** `Player (CharacterBody3D / PlayerController, IDamageReceiver)` -> `Vitals (Node / PlayerVitals)` -> `Health (Node / existing HealthComponent)`. Keep the two small components as a grouped child rather than adding several managers to the scene tree. The root receives hits because attacks may resolve `IDamageReceiver` on the collider/ancestor; it simply forwards them. The shared damage resolver must not know about PlayerController, zombies or UI.

**Configuration:** `PlayerSettings` should expose an exported, clearly grouped `PlayerVitalsSettings` profile, authored as a subresource in the single `PLAYER/Configuration/PlayerMaster.tres`. It initially contains `HealthSettings` with **MaxHealth = 100**, armour and resistances defaulting to zero. Runtime systems duplicate resource data; they never overwrite the master. Don't put current HP into `.tres`: it is runtime state, eventually serialized via SAVE.

## PH0 — First player health pass

### PH0 Codex implementation contract (updated 2026-10-28)

Read this PH0 section, [Stats ownership](Stats/README.md), [shared health](../ENTITIES/Shared/Combat/README.md), [FEEDBACK plan](../FEEDBACK/IMPLEMENTATION_PLAN.md), the actual player/FPS equipment source and `CodePreferences.md` before editing.

**Non-negotiable implementation sequence (PH0 authorization only):**

1. **Model and master:** add the minimum `Stats/PlayerVitals` + `PlayerVitalsSettings` pair described below (or the documented narrower direct-health-profile alternative after checking real source); group a real `HealthSettings` profile under `PlayerSettings`, with default 100 HP, zero armour/resistances in `PlayerMaster.tres`. Player runtime duplicates master settings as it already does; don't mutate shared `.tres`.
2. **Existing health component:** reuse `HealthComponent.Configure`, `ApplyDamage`, `Damaged`, `Died`. For healing and HP UI, expose an event with the *new* current/max snapshot (a `Damaged` payload alone is insufficient because `DamageResult.Amount` may include overkill); clamp heal, ignore nonfinite/negative inputs and forbid healing dead actors. Don't change the zombie damage/death contract.
3. **Player root receiver:** implement `IDamageReceiver` at the root `PlayerController`, gate unconfigured/dead/queued state, and forward through vitals, not custom subtraction. Initialize exactly once after player settings/scene are ready; get initial snapshot before binding HUD. Keep `Player.tscn` node alive on death for camera/scene references.
4. **Death and controls:** one latched death transition; stop player-controlled movement, aim/shoot and Q melee cleanly in **both** the top-down `PlayerWeaponController` and FPS `FirstPersonWeapon` adapters, without pausing the entire scene or breaking camera/render/UI. Don't interfere with equipment 1/2 or Q while alive. Review `_Input`, `_UnhandledInput` and active physics callbacks for post-death action gates.
5. **Minimal validation UI:** a temporary read-only `Health: current / max` label via existing world debug Controls (shared to inherited FPS where practical), not a styled health bar and not `FEEDBACK/Bars` code. FB1 handles presentation later. No permanent debug damage hotkey.
6. **Shutdown and evidence:** unsubscribe all health/weapon/HUD event bindings on scene removal; do not add idle stats callbacks. Run `dotnet build war-machine.csproj` if available, then F6 `WORLD/World.tscn` and `DEBUG/TEST/FirstPerson/FirstPersonTest.tscn`. Test temporary packet damage/healing/mitigation, single death, post-death controls and zombie shooting regression. Report what actually ran, pending checks and one commit to the required branch.

**PH0 does NOT authorize** zombie attack AI (CM5), FB1/FB2 bars, shield resources, animation polish, stamina, respawn, save system or global gameplay manager. Future FB phases depend on the HP API but are separately approved.


**Objective:** A genuinely damageable/healable active player, with 100/100 initial HP, existing mitigation rules, one-time death handling and a minimal visible readout. This establishes the API for future injuries and starvation without implementing those mechanics.

**Dependencies:** Existing `HealthComponent`, `DamageResolver`, `HealthSettings`, `IDamageReceiver`; active `PlayerController.Initialize`; `PlayerMaster.tres`; current equipment adapter; existing `Controls` CanvasLayer in `WORLD/World.tscn`.

**Scope and owning files:**
1. **Reusable health extension:** Add a bounded, finite-positive `Heal(amount)` (or equivalently named) method and an HP-change notification to `ENTITIES/Shared/Combat/HealthComponent.cs`. No healing of dead actors: resurrection/reset must be a distinct, explicit later operation. Emit change only if HP actually changed. Preserve current damage/death semantics and old `Damaged`/`Died` events, and keep existing zombie behavior unchanged.
2. **Player stats:** Add `PLAYER/Stats/PlayerVitals.cs` plus `PlayerVitalsSettings.cs` and `Stats/README.md`. Configure the existing `HealthComponent` with an isolated copy of the player health profile during player initialization; expose read-only `CurrentHealth`, `MaxHealth`, `IsDead`, `Heal(amount)` and HP/death events. No separate damage formula.
3. **Root hit receiver:** Make `PLAYER/PlayerController.cs` implement `IDamageReceiver` and forward `ReceiveHit(DamagePacket)` to its player-owned vitals. Reject hits until initialized and once dead. Wire death to input/movement shutdown and inform the weapon adapter so held fire cannot continue; keep player node in scene for later respawn. Decide the smallest visual death reaction (e.g. stop and clearly indicate dead), **not** a ragdoll.
4. **Scene/master wiring:** Attach a `Vitals/Health` group under `PLAYER/Player.tscn`; add the exported vitals profile under `PlayerSettings`, saved as nested resources in `PLAYER/Configuration/PlayerMaster.tres`. Keep `WorldController` as initialization/composition glue; do not change spawn, camera, terrain, robot or zombie systems.
5. **Tiny PH0 readout only:** In the active world's existing `Controls` CanvasLayer, show e.g. `Health: 100 / 100`, updating on an HP-changed event, never per-frame polling. Keep this temporary diagnostic text small; **do not pre-implement FEEDBACK FB1's styled bar or duplicate the hotbar**. Document how to replace/remove it when FB1 is approved. Provide a deliberate local damage/heal test path (temporary harness if necessary), without a permanent gameplay damage hotkey.

**Out of scope:** Zombie contact/melee attacks, robot beams, death animation/ragdoll, respawn, save/load, healing items, automatic HP regeneration, shield/armour equipment, status effects, stamina, food, water, weather damage, and changes to the legacy first-person player.

**Required behavior to preserve:** Existing walk/run/crouch/aim/shoot mechanics while alive; rifle-versus-zombie 95/100 HP behavior; zombie damage/death pipeline; camera/robot/terrain/weather; no global health singleton or per-frame player-health loop.

**Performance contract:** Hit -> direct `ReceiveHit` -> shared resolver -> event; heal -> direct method -> event. **Idle: zero `_Process`/`_PhysicsProcess` callbacks and zero timer work from health/vitals/readout.** Initialization binds events once; player death stops relevant player input/weapon activity; scene exit unsubscribes event handlers. No global collision scan, searching the tree each hit, or per-hit allocated state object.

**Pass criteria / brief validation:**
- `dotnet build war-machine.csproj` and `WORLD/World.tscn` F6 startup; readout begins at 100/100.
- Applied 25 physical damage with no defence -> 75 HP, then healing 10 -> 85 HP; healing above max clamps at 100; negative/NaN/infinite input cannot alter HP.
- Physical armour/resistance and nonphysical resistance follow the shared resolver exactly; input after death does not move/fire; lethal hit fires death once; later hits/heals do not resurrect.
- Existing zombies still take 95 damage per default rifle hit and die on the second; movement, equipment and parked FPS remain unchanged.
- Event subscriptions are released cleanly on scene exit/restart; no duplicated UI events or active polling.
- Brief source review and build/basic launch only; owner performs longer visuals and performance checks. If local tooling unavailable, mark **Awaiting Validation**, do not claim a passing build.

**Recommended assistant:** Codex / ChatGPT Work for coordinated scene, resource, events and weapon-input integration; Regular Chat can review the plan and isolated profile values; Copilot can run deferred local build/F6 checks.

## PH1 — HUD and hit/death feedback

**Objective:** Improve readability and player feedback, using PH0 events rather than altering damage rules.

**Dependencies:** PH0 accepted (or at least implemented and validated).

**Scope / ownership:** PH1 coordinates presentation with [FEEDBACK FB1/FB4](../FEEDBACK/IMPLEMENTATION_PLAN.md); implement the reusable health bar under `FEEDBACK/Bars/Health/`, player presentation under `FEEDBACK/Bars/PlayerStatus/`, and compose it through `UI/GameplayHUD/` rather than creating a second health UI. Replace the PH0 diagnostic readout; show percentage/current+max, hit pulse and optional separately authorized directional indicator. Subscribe to HP/death events; keep logic in player vitals, presentation in the HUD. Player death message or overlay may be added; respawn remains separate unless later authorized.

**Out of scope:** New combat/status types, inventory, enemy attacks, save system, elaborate animations, enemy bars (FB2) or shield mechanics (FB3).

**Preserve:** PH0 event API, active camera composition and existing world controls; no duplicate health arithmetic.

**Configuration / cost:** Inspector settings for display, not duplicate player HP values. Update only when HP changes; a cosmetic tween runs only until finished, then sleeps. Free/disconnect on scene exit.

**Pass:** Initial HUD is correct, repeated damage updates once, 0 HP visibly indicates death, no periodic redraw/polling at rest.

**Assistant:** Regular Chat for layout/spec; Codex / Work for signal/UI integration; Copilot for visual confirmation.

## PH2 — Stamina and exertion

**Objective:** Add stamina with running cost and controlled regeneration, later usable for melee, dodging and climbing.

**Dependencies:** PH0 stats container; current `PlayerController` running/crouching behavior.

**Scope / ownership:** Add stamina current/max and exhaustion state to `PLAYER/Stats/`, with grouped stamina defaults in `PlayerVitalsSettings`. Expose narrow APIs such as `TrySpendStamina`, `StartExertion`, `StopExertion`; integrate run gating into movement without making vitals control player movement directly. Regen delay and rate configurable. UI can subscribe to changes.

**Out of scope:** Food/thirst/fatigue penalties, inventory weight, combat stamina costs, deep fitness skill trees.

**Preserve:** Camera-relative motion/animation, crouch and acceleration; run speed when stamina is available.

**Performance:** Run/drain wakes only during exertion; regen wakes when below max and eligible, then turns off at full. Prefer one owned low-frequency timer or explicitly bounded movement-linked update, not perpetual per-frame idle work. When terrain/scene unloads, stop and unsubscribe.

**Pass:** Run consumes stamina; depletion stops sprint, recovery permits sprint again; no drain while not exerting, no update timer at full rest; movement remains smooth.

**Assistant:** Codex / Work for player-physics integration; Regular Chat can adjust exported tuning; Copilot for local movement feel.

## PH3 — Hunger and thirst

**Objective:** Add slow world-time-aware nutrition/hydration values and modest configurable consequences.

**Dependencies:** PH0 (and preferably PH2 so penalties can target stamina). A single authoritative in-game time source must be chosen when rates are introduced.

**Scope / ownership:** `PLAYER/Survival/` contains slow update logic and thresholds; `PLAYER/Stats/` stores current values and emits changed events. Add grouped maximums, decay rates and threshold rules to the player master. Provide explicit `Eat(amount)` and `Drink(amount)` entry points for future items; at severe thresholds, send a normal damage packet to the player rather than setting HP directly.

**Out of scope:** Food items, recipes, stomach simulation, diseases, offline progression, complex metabolism.

**Preserve:** Player health/damage rules; no background work in unrelated actors.

**Performance:** One low-rate shared survival tick, not one timer per statistic; sleep when disabled, stop at death/unload, no world searches; reactivation handles elapsed game time deterministically within reasonable bounds. Do not implicitly use wall-clock/offline decay until save/time semantics exist.

**Pass:** Values decrease at configured in-game rates, eating/drinking clamp safely, threshold effects activate once/when appropriate, HP damage uses shared pipeline, disabled simulation is dormant.

**Assistant:** Codex / Work for time integration; Regular Chat can design rate/threshold tables; Copilot for accelerated time checks.

## PH4 — Fatigue and rest

**Objective:** Model tiredness, rest and sleep recovery without building a full sleeping/base system.

**Dependencies:** PH3 shared survival update clock; PH2 stamina for optional tiredness effects.

**Scope / ownership:** Extend stats/settings with fatigue and recovery. Keep sleep initiation/action in a small player interaction or survival controller; advance recovery through the same survival time service. Hook exhaustion to stamina or movement through explicit modifiers, not direct writes into unrelated resource masters.

**Out of scope:** Beds/buildings, dreaming, sleeping animations, full fast-forward UI, save-game time skips.

**Preserve:** Same movement/health ownership, deterministic timing, existing player control modes until an explicit sleep action.

**Performance:** Reuse PH3 timer; dormant while time effects are disabled; event-driven enter/exit sleep, avoid extra per-frame loops.

**Pass:** Fatigue rises under configured activity/time, rest/sleep decreases it, consequences clamp and clear, sleep/death/unload shuts down callbacks.

**Assistant:** Regular Chat to refine sleep UX/rules; Codex / Work for gameplay integration; Copilot for time-rate checks.

## PH5 — Environment and injury hooks

**Objective:** Build small extensible survival effects only as concrete gameplay hazards arrive: body temperature, exposure, bleeding, injuries or radiation.

**Dependencies:** PH0 shared health API; PH3 shared clock; actual environment/world signals for chosen hazards. Implement each hazard separately when content exists; PH5 does **not** require all hazards at once.

**Scope / ownership:** Stable conditions are owned by `ENVIRONMENT/`; exposure/threshold effects live under `PLAYER/Survival/`; current player status remains under `PLAYER/Stats/`. Extend `DamageType` and matching resistances only when a new type has gameplay use. Damage-over-time should create resolved packets via the shared pipeline rather than directly subtracting HP. Add configuration only for effects being implemented.

**Out of scope:** Massive effect registry, global condition manager, all possible injuries, robot damage, advanced medicine.

**Preserve:** Weather owners, shared resolver for all actors, other combat results, zero-cost idle components.

**Performance:** Subscribe to weather/exposure changes or region events; activate one bounded low-rate effect tick only while exposure/condition is active; deregister on chunk/scene exit. Do not scan every player/world object every frame.

**Pass:** One chosen exposure can start, apply properly typed/mitigated consequences and stop fully when conditions clear, without continuing work or leaving duplicated listeners.

**Assistant:** Regular Chat for a narrow hazard design; Codex / Work for cross-environment integration; Copilot for scene/runtime checks.

## Phasing, decisions and validation handoff

1. **Recommended next implementation is PH0 only.** It is self-contained and makes all later phases cheaper. Do not add hunger, thirst, stamina or enemy attacks simply because the health code now exists.
2. **Keep the reusable health primitive generic.** Extend `HealthComponent` once for healing and change notifications; don't introduce another player-only damage resolver or a singleton.
3. **Delay save architecture.** Keep runtime player values distinct from `.tres` defaults. When persistent save profiles arrive, serialize current stats under the eventual `SAVE/` contract; neither PH0 nor this plan invents one.
4. **Small optional alternative:** If `PlayerVitalsSettings` feels redundant during PH0, `PlayerSettings` can directly export `HealthSettings` first, then promote to a nested vitals profile as PH2 approaches. The nested profile is preferred for your eventual many-stat player.
5. **No work beyond planning is authorized by this document.** Each approved phase should produce one GitHub commit, update this plan and `PLAYER/README.md` with state/known limitations, and give short validation results. State **Implemented**, **Awaiting Validation** and **Accepted** separately, not as synonyms.

**Current verification for this plan:** Existing code and documentation reviewed; no player-health code, assets or scenes changed by the FEEDBACK scaffold. PH0–PH5 remain **Planned** pending explicit authorization. No Godot/C# build or scene execution required for this docs-only change.

## Combat / melee dependency (separate plan)

[COMBAT/IMPLEMENTATION_PLAN.md](../COMBAT/IMPLEMENTATION_PLAN.md) defines CM0–CM5 for generic impact delivery, bullet stagger, gun shove, bat, knife and eventual zombie melee. **PH0 is required before CM5 zombies can damage the player.** CM0–CM4 can be planned or implemented independently of PH0, since the current zombies already have health. PH2 stamina is an optional later melee cost, not a dependency for the first prototype. No combat action is authorized or implemented by adding this reference.
