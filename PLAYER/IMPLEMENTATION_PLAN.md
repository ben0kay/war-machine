# PLAYER — Health, Body Condition, Vitals and Survival Implementation Plan

> **PH0/PH1 implemented in source (2026-10-28), awaiting local build/runtime/visual validation.** The player is not a traditional on-screen 100 HP bar. PH0 will establish hidden overall **Vitality** (generic life/death) plus independent **six-region BodyCondition** (head, torso, both arms, both legs). PH1 will display a compact body silhouette/condition readout, not the generic player HP bar. Damageable regions are foundation now; actual limping, impaired aiming, bleeding, fractures and medical treatment are later, individually authorized features. This decision supersedes all older HP-bar/temporary-debug-label PH0/PH1 prose anywhere in this repo.
>
> **Current source reality:** PlayerVitals, BodyCondition and the event-driven six-region BodyConditionWidget now exist. Shared HealthComponent supplies validated healing and a resolved-damage entry point; NPC ApplyDamage still uses the same resolver. The generic overlay excludes player ownership. See the implementation handoff below. The numbered phase contracts remain design/acceptance criteria; future phases are not authorized.
>
> Work only on `implement-td0-td1`. Follow `CodePreferences.md`, read the current actual code, and implement only the specifically requested phase. Keep code modular and dormant at idle. `PLAYER/Stats/BODY_CONDITION_DESIGN.md` is the detailed anatomy/damage contract.

## Phases at a glance

| Phase | Goal | Suggested executor | State |
| --- | --- | --- | --- |
| **PH0** | Hidden vitality + six body-region condition values, shared damage/healing, death, events; compatible player receiver; no normal player HP bar | Codex / Work | **Implemented; Awaiting Validation** |
| **PH1** | Compact six-region body silhouette and injury-state feedback in shared GameplayHUD; no new health calculations | UI design + Codex / Work | **Implemented; Awaiting Validation** |
| **PH-HIT** | Accurate optional anatomy hurtboxes and region mapping for bullets/melee, collision-safe, separately approved | Codex / Work | **Future / not authorized** |
| **PH2** | Stamina/exertion; future body-condition modifiers may consume this API | Codex / Work | **Planned** |
| **PH3** | Hunger, thirst and slow survival consequences | Codex / Work | **Planned** |
| **PH4** | Fatigue, sleep and recovery | Codex / Work | **Planned** |
| **PH5** | Later individual injuries, bleeding, fractures, environmental exposure and treatment hooks | Codex / Work | **Planned, subfeatures separately gated** |

## PSR0 — Small player-script structure refactor (proposed only)

**Status: Planned; not authorized for implementation.** Independent of PH0–PH5. The active `PLAYER/PlayerController.cs` is ~454 lines and mixes input, movement physics, stance/capsule transitions, animation and weapon posing. Refactor ownership for readability **without adding mechanics or changing behaviour**:

- Keep `PlayerController.cs` as the thin `CharacterBody3D` coordinator: initialization, physics/input callback dispatch, player identity, vitals/death gate, existing public methods/events and cleanup.
- `Input/PlayerInput.cs`: keyboard/mouse bindings, input-blocking, movement reads and one-shot requests. Preserve **/ prone, Right Ctrl crouch, right-mouse jump**, current movement keys, menus and FPS mouse-capture rules.
- `Movement/PlayerMovement.cs`: camera-relative movement, acceleration/braking, sprint, gravity, jump impulse, `MoveAndSlide()`, ground/idle wake-sleep and collision diagnostics.
- `Movement/PlayerStance.cs`: standing/crouching/prone state, clearance queries, capsule resizing, stance transitions and notifications.
- `Visuals/PlayerAnimation.cs`: existing walk/crawl gait and rig posing, with no independent idle animation loop.
- `Visuals/PlayerWeaponPose.cs`: aim/weapon socket, recoil arm response, shove grips and arm solving. Keep actual firing/reload/melee ownership in existing `Equipment/` scripts; `C/` remains procedural mesh generation only.

**Implementation guardrails:** Prefer small plain C# collaborators, cached references and a single player physics callback rather than extra scene nodes or ticking components. Preserve public APIs/signals consumed by HUD, enemies and equipment, the `DEBUG/TEST/FirstPerson/PLAYER/FirstPersonPlayer.cs` inheritance/virtual hooks, camera rotation refresh, headroom checks, and the no-work-while-idle policy. Keep `PlayerSettings.cs`/`Configuration/PlayerMaster.tres` authoritative for current tuning; add no duplicate configuration. Add short README ownership notes for new folders.

**Acceptance when separately approved:** `dotnet build war-machine.csproj` plus brief top-down/FPS smoke tests covering movement/camera rotation, sprint/brake, crouch/prone and ceiling clearance, RMB jump, idle sleep, weapon aim/recoil/shove, health/death and input-blocked menus. One commit to `implement-td0-td1`. **This section is documentation only; no refactor has been performed.**

## Verified starting point and ownership

- The active game is `WORLD/World.tscn`; the FPS debug test inherits shared world/player plumbing. `PLAYER/Player.tscn` and `PlayerController.cs` currently handle movement, stance and equipment, **not health**. `PLAYER/PlayerSettings.cs` and `Configuration/PlayerMaster.tres` own exported defaults. The old LegacyFirstPerson player is parked.
- `ENTITIES/Shared/Combat/` owns `DamagePacket`, `DamageResult`, `DamageResolver`, `HealthSettings`, `HealthComponent` and `IDamageReceiver`. Zombies already receive hits this way. **Never replace this damage calculation with a player-specific formula.**
- The generic enemy feedback overlay and `HealthBar.tscn` already exist. It subscribes to `HealthComponent.HealthChanged` and projects red bars for damaged actors. It also currently has code to show the *player* at lower-left; that **must be changed by PH0** to exclude the player, not reused as the new player health UI. NPC bar presentation remains intact.
- `UI/GameplayHUD` composes Hotbar and Backpack/Inventory. `FEEDBACK/Bars/PlayerStatus` will own the PH1 body-condition widget; place one instance beside (not on top of) the hotbar, shared between overhead/FPS. A future inventory Health tab is outside PH1.
- `PLAYER/Stats/` owns player vitals, body-region state, their typed master settings and change notifications. `PLAYER/Survival/` owns later time-based health effects. WorldController composes/initializes only, not health or injury rules.
- Avoid `C/` for data/controllers; reserve it for procedural art. No save schema, singleton, per-hit scene searches or always-running stats process.

## Planned folders and scene wiring

~~~text
PLAYER/
  Player.tscn                           existing CharacterBody3D
    Vitals                               Node / PlayerVitals (PH0)
      Health                             existing HealthComponent (PH0)
      BodyCondition                      new lightweight Node (PH0)
  PlayerController.cs                    implement IDamageReceiver, death/controls gate
  PlayerSettings.cs                      exported player vitals settings
  Configuration/PlayerMaster.tres        single typed master, nested runtime defaults
  Stats/
    PlayerVitals.cs                      orchestrates shared health and body regions
    PlayerVitalsSettings.cs              vitality + six condition defaults
    BodyCondition.cs                     fixed six entries; no separate node per limb
    BodyRegion.cs                        enum Head/Torso/LeftArm/RightArm/LeftLeg/RightLeg
    BODY_CONDITION_DESIGN.md             data, routing, semantics, hitbox future plan
  Survival/                              PH3/PH5, not PH0
FEEDBACK/Bars/PlayerStatus/              PH1 silhouette/widget, no gameplay state
UI/GameplayHUD/                           one screen-space HUD owner
ENTITIES/Shared/Combat/                   unchanged general-purpose resolver/health
~~~

This is a **design**, not a claim that the scene nodes or code exist. Group the health and body nodes as shown; do not create six child health components, six tickers or duplicate NPC health bars.

## Core rule: two different types of condition

1. **Vitality**: hidden player survival/death value, initially 100/100, kept on the existing generic `HealthComponent`. It is **not** rendered as the player's primary HP bar. Only approved critical damage (initially head/torso), or later explicitly authorized severe survival hazards, lowers vitality. Vitality reaching zero latches death. The internal 100 is a practical initial tuning value, **not a claim that the player is six separate health bars**.
2. **BodyCondition**: six independent normalized condition values, initially 100 each, owned by one small `BodyCondition` instance. Losing arm/leg condition **does not directly kill the player** in PH0. Zero on a limb means its region is fully incapacitated in the data model; consequences come later. Zero head/torso condition does not independently implement a second death mechanic in PH0; vitality remains authoritative until explicit critical-injury rules exist.
3. A hit is **mitigated exactly once** using shared `DamageResolver` and existing armour/resistances. Apply the resolved amount to the selected region. Head/torso additionally apply the appropriate resolved vital loss **once** through `HealthComponent`. To avoid double mitigation, PH0 may add a narrowly scoped `HealthComponent.ApplyResolvedDamage(...)` API that retains `Damaged`, `HealthChanged`, `Died` semantics, while existing `ApplyDamage` continues to resolve as before for zombies. Never call `ApplyDamage` again on an already mitigated hit.
4. `Unknown`/unspecified body region falls back deterministically to **Torso** for current capsule hits; never randomly choose limbs. Keep existing `DamagePacket` constructors/callers compatible: prefer an optional region parameter or a narrow player region routing adapter. Actual precision head/arm/leg ray-hit geometry is deferred to **PH-HIT**. PH0 tests every region by explicit injected packets/region selection, not by pretending the existing capsule can locate limbs.
5. Region healing and overall vitality healing are distinct. Treating an arm must not restore other regions or automatically resurrect the player. Body-condition and vitality change events carry read-only snapshots and do not mutate `.tres` masters.
6. PH0 does **not** implement limb penalties, pain, bleeding, fractures, medical items, dismemberment, extra player armour hitboxes or medical UI. Plan these against the stable values later without rewriting damage contracts.

## PH0 — Vitality and BodyCondition foundation

**Goal:** A living player with internal vitality and six independent body conditions, able to receive damage, be explicitly healed in tests, and die once. Existing combat and enemy health remain unchanged.

**Required sequence for Codex (PH0 authorization ONLY):**

1. Read `PLAYER/Stats/BODY_CONDITION_DESIGN.md`, this phase, `PLAYER/Stats/README.md`, current player/weapon controllers (top-down and FPS), `ENTITIES/Shared/Combat/README.md` and actual FEEDBACK overlay implementation. Resolve any contradictions in older READMEs in favor of this plan.
2. Add typed `PlayerVitalsSettings` and six-region configuration to the existing `PlayerMaster.tres`; PH0 default vitality 100 and region condition 100 each; zero armour/resistances initially. Deep-copy runtime configuration. Values in master are **defaults**, never live health state.
3. Create a minimal `BodyCondition` with a fixed six-entry store, getters, bounded region damage/healing, per-region `ConditionChanged` event, no per-frame processing or timers. Use a typed `BodyRegion` enum with an explicit Unknown sentinel. No gameplay consequences yet.
4. Add `PlayerVitals` as the single coordination point: resolve each incoming packet once via shared rules, update the correct region, apply vital loss only for head/torso or explicitly approved life hazards, clamp all values, emit coherent events. Preserve the generic `HealthComponent` as authoritative vitality/death with its existing `HealthChanged` event; implement validated finite-positive healing safely. Preserve zombie and deer contracts and semantics.
5. `PlayerController` implements `IDamageReceiver`, delegates to `PlayerVitals` and rejects pre-initialization, dead and queued-for-deletion hits. Wire Vitals/Health/BodyCondition in `Player.tscn` and initialize from runtime player settings **before** HUD subscriptions. Keep actor node on death for camera, scene ownership and future respawn.
6. On death latch once: stop player-controlled movement, weapon fire/aim, and Q-melee (including top-down and FPS handlers) while leaving camera/world rendering/input alive. Existing alive movement and hotbar/backpack must not regress.
7. **Feedback compatibility is mandatory:** change the existing `HealthBarOverlay` registration/refresh policy to skip the player-owned HealthComponent; leave automatic NPC registration and NPC red bars exactly as before. **Do not** create a new bottom-left health bar or an always-present diagnostic `Health: 100/100` label. For PH0 smoke tests, read direct events/Inspector or add strictly temporary development-only diagnostics with no permanent gameplay HUD. PH1 adds the body condition presentation.
8. Brief source/build/F6 verification and documentation only. Do not build real hitboxes, aim penalties, injury effects, medicine, stamina, hunger, crafting or enemy attack AI. One commit to required dev branch for an authorized PH0 job.

**Acceptance examples:**

- Initial state: vitality 100/100; Head, Torso, LeftArm, RightArm, LeftLeg and RightLeg each 100; no traditional player HP bar on the HUD. Generic zombie/deer bars still appear on injury.
- Inject **25 physical damage to LeftLeg** (no protection) -> LeftLeg 75, vitality remains 100, other five regions remain 100. Repeat to LeftArm -> only that arm changes. No automatic limp/aim penalty yet.
- Inject **25 physical damage to Torso** -> torso 75 and vitality 75. Another critical hit can kill via vitality reaching 0; death fires only once. Unspecified hit -> Torso. NPC damage still follows their existing rules.
- Healing `HealRegion(LeftLeg, 10)` restores only that region, clamped to 100; `HealVitality(10)` restores only vitality. Reject NaN, negative, infinity and healing dead. No unsolicited regeneration or per-frame polling.
- Restart/unload cleans events, no control input after death, FPS and overhead both boot. Run `dotnet build war-machine.csproj` and scene smoke checks where available; report unavailable tooling honestly.

**Explicit exclusions:** limb effects, instant death simply because a limb reaches 0, precision anatomical hitboxes, bleed/shock/temperature, stamina/survival, medicine, revive/respawn, save/load and automated enemy attacks.

## PH1 — Six-region body-condition HUD

**Goal:** Represent player health by a **compact body silhouette** instead of a 100 HP bar. This is PH1 presentation, coordinated with existing FEEDBACK FB1 visual ownership. It adds **no health damage mechanics**.

**Required scene/presentation:**

- Under `FEEDBACK/Bars/PlayerStatus/` create one lightweight `BodyConditionWidget.tscn` and visual-only script; compose it **once** under `UI/GameplayHUD/Screen` as a sibling of the hotbar/backpack in overhead and inherited FPS. Preserve the existing generic enemy overlay.
- Body silhouette has six interactive-free visual regions (head, torso, L/R arms and legs), visually distinguishing healthy / minor / severe / incapacitated using event-driven colours plus readable shapes/icons/labels, not colour alone. Defaults as initial style suggestion: healthy muted green/neutral, minor amber, severe red, 0 dark/critical. Future tuning lives in `FeedbackMaster.tres` (presentation only), never duplicated HP stats.
- Compact bottom-left presentation, away from the hotbar. Fully healthy state remains unobtrusive (either muted silhouette or a small neutral health icon); wounded regions become prominent. Optional short damage flash/tween runs only when changed and stops. No always-running HUD polling or input interception (`MouseFilter.Ignore`).
- Bind once to `BodyCondition.ConditionChanged` and `PlayerVitals` life/death events; initialize all six regions from current values immediately. Death may tint/dim the whole silhouette or display a restrained message, not respawn/revive logic. Unsubscribe on rebind/exit.
- Region condition is available for an eventual Inventory Health tab with detailed injuries/medical treatment, but **that tab is not implemented under PH1**. No numeric vitality bar, six literal HP bars, hover tooltip system, fracture icons without real fractures or animation/skeletal effects.
- Existing `HealthBar.tscn` and `HealthBarOverlay.cs` remain for **NPCs**. Do not repurpose their health math or special-case the player back into the old lower-left bar.

**PH1 passes:** six regions display correct initial and damaged states; injured arm/leg colours change individually, healthy siblings stay neutral; a healed region returns to normal; no duplicate player bar; zombie health bar unaffected; hotbar and backpack remain usable; top-down/FPS layouts fit 16:9 plus smaller viewport, no phantom mouse clicks; zero idle UI work. Validate Godot scene and build locally if possible.

**Separate future PH-HIT (not authorized by PH0/PH1):** attach region-specific nonblocking physics hurtboxes/markers to player head/torso/arms/legs, account for current single movement capsule and weapons' raycast collision masks, and forward `BodyRegion` to the existing hit dispatcher. Avoid duplicate collisions/damage for one hit, ghost through-wall hits, wrong left/right assignments, or breaking melee/AI sight. Keep separate from physical movement collision and test overhead/FPS. Until then unspecified gameplay ray hits map to torso; development injections verify all six independently.

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

**Scope / ownership:** `PLAYER/Survival/` contains slow update logic and thresholds; `PLAYER/Stats/` stores current values and emits changed events. Add grouped maximums, decay rates and threshold rules to the player master. Provide explicit `Eat(amount)` and `Drink(amount)` entry points for future items; at severe thresholds, route a non-anatomical vitality hazard through PlayerVitals and the existing shared damage resolver, rather than setting vitality directly or inventing a torso wound.

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

**Scope / ownership:** Stable conditions are owned by `ENVIRONMENT/`; exposure/threshold effects live under `PLAYER/Survival/`; current player status remains under `PLAYER/Stats/`. Extend `DamageType` and matching resistances only when a new type has gameplay use. Damage-over-time should use an explicit PlayerVitals non-anatomical hazard route and the shared resolver to lower vitality, and change BodyCondition only for an actual region-specific injury; never misclassify weather exposure as a torso shot. Add configuration only for effects being implemented.

**Out of scope:** Massive effect registry, global condition manager, all possible injuries, robot damage, advanced medicine.

**Preserve:** Weather owners, shared resolver for all actors, other combat results, zero-cost idle components.

**Performance:** Subscribe to weather/exposure changes or region events; activate one bounded low-rate effect tick only while exposure/condition is active; deregister on chunk/scene exit. Do not scan every player/world object every frame.

**Pass:** One chosen exposure can start, apply properly typed/mitigated consequences and stop fully when conditions clear, without continuing work or leaving duplicated listeners.

**Assistant:** Regular Chat for a narrow hazard design; Codex / Work for cross-environment integration; Copilot for scene/runtime checks.

## Phasing, decisions and validation handoff

1. **Implement PH0 first**, without automatically authorizing PH1, PH-HIT, stamina or new attacks. It supplies hidden vitality and six body-region data from day one. PH1 is the silhouette/presentation job once that state works.
2. **Keep the generic damage/HP primitives reusable.** Existing `HealthComponent.HealthChanged` works already; do not duplicate it. Narrow healing and resolved-damage support must preserve zombie/deer/NPC behaviour and make mitigation happen once.
3. **Keep independent body condition separate from death.** A severely injured leg is not automatically a fatal loss of hidden vitality; later bleeding, shock and organ injury can drive vitality through explicit approved mechanics. Body and life events do not need idle processing.
4. **Maintain feedback coexistence.** Player generic HP bars are excluded in PH0; NPC overlays remain working; one silhouette binds after PH1. Later inventory Health tab and precise hitboxes are independent authorizations.
5. **No save system or detailed injury effects yet.** `PlayerMaster.tres` contains authored defaults, and eventual SAVE stores current runtime condition only after a save contract exists.
6. Every implemented phase requires one commit to `implement-td0-td1` and a brief truthful build/scene verification report. **This edit is documentation only**; PH0–PH5 and PH-HIT remain planned until separately requested.

**Current verification:** Plan and existing code reviewed only. No player stats scripts, scene nodes, combat routing or HUD art were created by this planning revision. No Godot runtime build is claimed.

## Combat / melee dependency (separate plan)

[COMBAT/IMPLEMENTATION_PLAN.md](../COMBAT/IMPLEMENTATION_PLAN.md) defines CM0–CM5 for generic impact delivery, bullet stagger, gun shove, bat, knife and eventual zombie melee. **PH0 is required before CM5 zombies can damage the player.** CM0–CM4 can be planned or implemented independently of PH0, since the current zombies already have health. PH2 stamina is an optional later melee cost, not a dependency for the first prototype. No combat action is authorized or implemented by adding this reference.

## PH0–PH1 implementation handoff — 2026-10-28

Implemented the authorized model and silhouette together. Player master embeds hidden 100 vitality and six 100-condition maxima. Player root forwards compatible packets; unknown damage resolves Torso. PlayerVitals resolves mitigation exactly once, commits body state, applies critical vitality loss through HealthComponent.ApplyResolvedDamage, then publishes region changes. Life and body callbacks see committed values; reentrant damage/healing is guarded. NPC raw-damage semantics are preserved; shared Heal is finite-positive, clamped and cannot resurrect.

Death keeps the actor/camera in scene and uses existing input-block subscriptions in overhead/FPS to cancel attacks and aim. The block cannot be cleared after death by closing inventory; inventory close also cannot recapture the cursor after death. The existing NPC overlay rejects player-owned HealthComponents. World initializes one silhouette before the backpack, on the existing shared CanvasLayer. The silhouette has no normal vitality readout, supports six anatomical sections and textual severity, and updates only on events. FeedbackMaster owns presentation thresholds/colours; PlayerMaster owns runtime defaults. No per-limb nodes, stats timers or HUD polling.

PH-HIT, limb penalties, medical items, detailed injuries, stamina, needs, respawn, save and enemy attacks remain unimplemented. Current gameplay capsule hits always map Torso; use the explicit development region route to verify other sections. No UI pulse is added (optional polish). Layout anchors above the hotbar at bottom-left; local visual acceptance is pending.

Source/resource wiring and whitespace checks performed. Neither Godot nor dotnet is available here: **compilation, the new actual-code smoke test and World/FPS visual/input tests are not verified**. Run `dotnet build war-machine.csproj`, then `godot --headless --path . res://DEBUG/TEST/PlayerVitals/PlayerVitalsTest.tscn`, then the brief F6 checks in [test README](../DEBUG/TEST/PlayerVitals/README.md). Delivery commit SHA is reported in the job response; this record is included in that commit. Do not mark Accepted until local checks pass.
