# PLAYER — Health, Vitals and Survival Implementation Plan

> **Plan only — PH0–PH5 are Planned.** No player-health, survival or HUD code is implemented by this document. Implement only the phase expressly authorized by the owner, on the active `implement-td0-td1` branch unless instructed otherwise.
>
> **Reviewed:** 2026-10-27 against the active branch's root `README.md`, `CodePreferences.md`, `PLAYER/README.md`, player scene/controller/settings, `WORLD/WorldController.cs` and existing shared damage pipeline. The parked first-person world must remain unchanged.

## Phases at a glance

| Phase | Goal | Suggested assistant | Status |
| --- | --- | --- | --- |
| **PH0** | Player health (100 HP), shared damage reception, healing, death, simple HP readout | Codex / ChatGPT Work | **Planned** |
| **PH1** | Better health HUD and hit/death feedback, no new damage mechanics | Regular Chat for UI plan; Codex / Work for integration | Planned |
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
  Stats/                            # PH0 onwards (player-owned state and adapter)
    PlayerVitals.cs                 # new: player health wrapper, future vitals signals
    PlayerVitalsSettings.cs         # new: future-extensible profile containing HealthSettings
    README.md                       # new when PH0 implemented
  Survival/                         # PH3+ only (rates, exposures, consequences)
    README.md                       # create with first survival implementation
  Equipment/                        # existing: weapon adapter; reacts to player death
  LegacyFirstPerson/                # parked: no change
  IMPLEMENTATION_PLAN.md            # this plan

ENTITIES/Shared/Combat/             # existing: damage packets, resolver, health, receiver
WORLD/World.tscn                    # active scene: wires player and minimal visible readout
WORLD/WorldController.cs            # composition/event binding only, not damage formulas
UI/                                  # optional later PH1 reusable HUD, only if justified
```

**Proposed scene outline:** `Player (CharacterBody3D / PlayerController, IDamageReceiver)` -> `Vitals (Node / PlayerVitals)` -> `Health (Node / existing HealthComponent)`. Keep the two small components as a grouped child rather than adding several managers to the scene tree. The root receives hits because attacks may resolve `IDamageReceiver` on the collider/ancestor; it simply forwards them. The shared damage resolver must not know about PlayerController, zombies or UI.

**Configuration:** `PlayerSettings` should expose an exported, clearly grouped `PlayerVitalsSettings` profile, authored as a subresource in the single `PLAYER/Configuration/PlayerMaster.tres`. It initially contains `HealthSettings` with **MaxHealth = 100**, armour and resistances defaulting to zero. Runtime systems duplicate resource data; they never overwrite the master. Don't put current HP into `.tres`: it is runtime state, eventually serialized via SAVE.

## PH0 — First player health pass

**Objective:** A genuinely damageable/healable active player, with 100/100 initial HP, existing mitigation rules, one-time death handling and a minimal visible readout. This establishes the API for future injuries and starvation without implementing those mechanics.

**Dependencies:** Existing `HealthComponent`, `DamageResolver`, `HealthSettings`, `IDamageReceiver`; active `PlayerController.Initialize`; `PlayerMaster.tres`; current equipment adapter; existing `Controls` CanvasLayer in `WORLD/World.tscn`.

**Scope and owning files:**
1. **Reusable health extension:** Add a bounded, finite-positive `Heal(amount)` (or equivalently named) method and an HP-change notification to `ENTITIES/Shared/Combat/HealthComponent.cs`. No healing of dead actors: resurrection/reset must be a distinct, explicit later operation. Emit change only if HP actually changed. Preserve current damage/death semantics and old `Damaged`/`Died` events, and keep existing zombie behavior unchanged.
2. **Player stats:** Add `PLAYER/Stats/PlayerVitals.cs` plus `PlayerVitalsSettings.cs` and `Stats/README.md`. Configure the existing `HealthComponent` with an isolated copy of the player health profile during player initialization; expose read-only `CurrentHealth`, `MaxHealth`, `IsDead`, `Heal(amount)` and HP/death events. No separate damage formula.
3. **Root hit receiver:** Make `PLAYER/PlayerController.cs` implement `IDamageReceiver` and forward `ReceiveHit(DamagePacket)` to its player-owned vitals. Reject hits until initialized and once dead. Wire death to input/movement shutdown and inform the weapon adapter so held fire cannot continue; keep player node in scene for later respawn. Decide the smallest visual death reaction (e.g. stop and clearly indicate dead), **not** a ragdoll.
4. **Scene/master wiring:** Attach a `Vitals/Health` group under `PLAYER/Player.tscn`; add the exported vitals profile under `PlayerSettings`, saved as nested resources in `PLAYER/Configuration/PlayerMaster.tres`. Keep `WorldController` as initialization/composition glue; do not change spawn, camera, terrain, robot or zombie systems.
5. **Tiny readout:** In the active world's existing `Controls` CanvasLayer, show e.g. `Health: 100 / 100`, updating via an HP-changed event, not per-frame polling. PH1 owns visual styling. Provide a minimal deliberate test path (lightweight local harness/debug-only damage call if necessary), without a permanent gameplay damage hotkey.

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

**Scope / ownership:** Replace or restyle the temporary world readout with a reusable health bar (prefer `UI/` only when actually needed); percentage/current+max, hit pulse and optional simple directional indicator. Subscribe to HP/death events; keep logic in player vitals, presentation in the HUD. Player death message or overlay may be added; respawn remains separate unless later authorized.

**Out of scope:** New combat/status types, inventory, enemy attacks, save system, elaborate animations.

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

**Current verification for this plan:** Repository source and documentation reviewed; no code changed, no Godot build/scene execution. PH0–PH5 remain **Planned** pending explicit authorization.
