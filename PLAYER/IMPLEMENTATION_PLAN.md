> **CM5 source implemented / awaiting local validation:** Standing zombie strikes now reach the existing PH0/PH1 receiver; see [combat handoff](../COMBAT/CM5_README.md). No player health formula, input or camera changes.

> **ELD0 source implementation (2026-10-28):** [Shared region detection](../ENTITIES/Shared/Anatomy/README.md) now routes firearm shape hits for both zombie variants in both views. BodyRegion moved to ENTITIES/Shared/Anatomy with values unchanged. Existing damage balance/player rules and melee stay unchanged; no injuries, severing or new attacks. Local build/runtime validation pending.

> **Anatomy/combat/corpse direction (2026-10-28):** [Overall master plan](../ENTITIES/TYPES/Zombies/ANATOMY_COMBAT_CORPSE_PLAN.md). PH0/PH1 and player vitality/healing rules remain intact. Future PH-HIT should reuse the ELD0 region-detection foundation when separately authorized; no player injury penalties, severing or state migration are authorized by this link.

# PLAYER — Health, Body Condition, Vitals and Survival Implementation Plan

> **PH0/PH1 implemented in source (2026-10-28), awaiting local build/runtime/visual validation.** The player is not a traditional on-screen 100 HP bar. PH0 will establish hidden overall **Vitality** (generic life/death) plus independent **six-region BodyCondition** (head, torso, both arms, both legs). PH1 will display a compact body silhouette/condition readout, not the generic player HP bar. Damageable regions are foundation now; actual limping, impaired aiming, bleeding, fractures and medical treatment are later, individually authorized features. This decision supersedes all older HP-bar/temporary-debug-label PH0/PH1 prose anywhere in this repo.
>
> **Current source reality:** PlayerVitals, BodyCondition and the event-driven six-region BodyConditionWidget now exist. Shared HealthComponent supplies validated healing and a resolved-damage entry point; NPC ApplyDamage still uses the same resolver. The generic overlay excludes player ownership. See the implementation handoff below. The numbered phase contracts remain design/acceptance criteria; future phases are not authorized.
>
> Work only on `implement-td0-td1`. Follow `CodePreferences.md`, read the current actual code, and implement only the specifically requested phase. Keep code modular and dormant at idle. `PLAYER/Stats/BODY_CONDITION_DESIGN.md` is the detailed anatomy/damage contract.

## Phases at a glance

| Phase | Goal | Suggested executor | State |
| --- | --- | --- | --- |
| **PSR0** | Behaviour-preserving player script ownership split | Codex / Work | **Implemented; Awaiting Validation** |
| **PH0** | Hidden vitality + six body-region condition values, shared damage/healing, death, events; compatible player receiver; no normal player HP bar | Codex / Work | **Implemented; Awaiting Validation** |
| **PH1** | Compact six-region body silhouette and injury-state feedback in shared GameplayHUD; no new health calculations | UI design + Codex / Work | **Implemented; Awaiting Validation** |
| **PH-HIT** | Accurate optional anatomy hurtboxes and region mapping for bullets/melee, collision-safe, separately approved | Codex / Work | **Future / not authorized** |
| **PH2** | Short-term stamina: sprint, jump, small firing costs, recovery and temporary bottom-right HUD | Codex / Work | **Foundation implemented; Awaiting Validation** |
| **PH3** | Fullness, hidden satiation, thirst and later survival consequences | Codex / Work | **Planned** |
| **PH4** | Fatigue, sleep and recovery | Codex / Work | **Planned** |
| **PH5** | Later individual injuries, bleeding, fractures, environmental exposure and treatment hooks | Codex / Work | **Planned, subfeatures separately gated** |

## PSR0 — Player-script structure refactor

**Status: Implemented; Awaiting Validation (2026-10-28).** Owner explicitly authorized the code refactor. Independent of PH0–PH5. PlayerController shrank from 476 lines (including the latest door-contact hook) to approximately 120 lines. No new gameplay, input mapping, tuning, scene nodes or public APIs were introduced.

| Owner | Responsibility |
| --- | --- |
| `PlayerController.cs` | CharacterBody3D identity, initialization, vitals/death/menu gate, public properties/events, engine callback dispatch, existing virtual FPS hooks and cleanup |
| `Input/PlayerInput.cs` | Physical bindings, movement reads, queued stance/jump requests, GUI/focus guards and wake-up |
| `Movement/PlayerMovement.cs` | Camera axes/subscription, acceleration/sprint/gravity/jump, MoveAndSlide, existing contact-driven door push/F10, movement notifications and idle sleep |
| `Movement/PlayerStance.cs` | Three stance states, capsule/clearance caches, one-time transitions and height interpolation |
| `Visuals/PlayerAnimation.cs` | One-time rig cache, gait/facing and unchanged walk/crouch/crawl posing |
| `Visuals/PlayerWeaponPose.cs` | Aim/socket, recoil arm deltas, shove grips and original two-bone arm solve |

**Implementation choice:** helpers are private sealed plain C# classes nested in the existing partial PlayerController, split across the planned folders. Partial declarations let them call the existing protected FPS hooks without widening the public/internal actor API. They own their respective state and have no Godot node inheritance, independent callbacks or timers. PlayerSettings/Configuration/PlayerMaster remain authoritative; Equipment still owns firing/reload/melee, C/ still owns mesh generation. No scene/script/UID path changes or legacy-player edits.

**Preserved order:** stance requests/clearance and capsule interpolation → one-shot jump/input → virtual movement/acceleration → MoveAndSlide → existing door push and F10 → actual-travel gait → socket/position events → grounded idle sleep. Pose still reapplies shove grips before recoil. Reinitialize replaces the same camera subscription, resets requests/stance without duplicate rig generation, and exit detaches callbacks. Existing PlayerVitals ownership and latched death gate remain.

**Bindings from current source:** arrows move, Shift sprints, Right Ctrl crouches, slash `/` goes prone, RMB jumps and F10 diagnoses contact. FPS still gates movement/jump through mouse capture. Historical Left Ctrl/C or Right Ctrl prone descriptions elsewhere are not new bindings introduced by PSR0.

**Verification performed:** source comparison normalized helper qualification and confirmed 15 extracted method bodies plus the reconstructed full physics sequence match the pre-refactor baseline. Existing seven virtual hooks and public gameplay/event surface retained; source/whitespace/resource and loop review completed. No Godot/.NET executable is available here: compilation and runtime are **unverified**.

**Local acceptance:** build `dotnet build war-machine.csproj`; run the existing `DEBUG/TEST/PlayerVitals/PlayerVitalsTest.tscn` headlessly, then briefly F6 World and FPS. Check arrows while rotating the camera, sprint/brake/reverse, all stance transitions/headroom, RMB press/hold/landing, contact-driven doors, idle sleep/focus wake, aiming/recoil/Q shove/reload, inventory blocking and latched death. Reinitialize/reload and verify one rig/one camera subscription. No terrain rebake. One commit to `implement-td0-td1`; delivery SHA is in the job response. Mark Accepted only after local checks.

## Verified starting point and ownership

- The primary gameplay entry is `WORLD/FirstPerson/FirstPersonWorld.tscn`, inheriting `WORLD/World.tscn` shared composition and secondary top-down view. `PLAYER/Player.tscn` / `PlayerController.cs` own shared movement, stance, equipment and implemented PH0/PH1 vitals/death plumbing. `PLAYER/PlayerSettings.cs` and `Configuration/PlayerMaster.tres` own exported defaults. The old LegacyFirstPerson player is parked.
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

**Foundation implemented in source (2026-10-29); awaiting local compilation, movement/weapon/HUD acceptance.** The owner authorized the start of PH2 and a small temporary bottom-right GUI. Hunger, satiation, thirst, fatigue and injury consequences remain later phases.

**Objective:** Short-term capacity for vigorous actions, separate from vitality, body condition and long-term fatigue.

**Ownership:** PlayerVitals creates one Stats/PlayerStamina node at initialization. PlayerVitalsSettings embeds a typed PlayerStaminaSettings profile inside PlayerMaster; current reserve is runtime-only and initialization does not refill an existing player. PlayerMovement gates sprint/jump and reports collision-resolved ground sprint effort. Both existing weapon view adapters charge their shared accepted-shot event once, using FirearmSettings.StaminaCostPerShot. No NPC firearm behavior or damage formula is changed.

| Initial setting | Default |
| --- | ---: |
| Maximum | 100 |
| Sprint drain | 10/s |
| Jump cost | 15 |
| Firearm cost | 0.5 per accepted shot, separately tunable per weapon |
| Recovery delay | 1.5s after the latest actual exertion |
| Recovery rate | 15/s |
| Sprint unlock after depletion | 20% of maximum |

**Rules:** Walking, crouching and crawling spend nothing and permit recovery. Standing sprint requires movement input, Shift, available stamina and a run multiplier above one; drain requires actual horizontal travel >0.1m/s and ground contact before/after movement. Shift alone, pushing immovably into a wall, airborne travel and blocked/menu input do not drain sprint stamina. A jump spends its exact cost only after standing/ground/focus/input-capture/positive-jump-velocity eligibility passes; insufficient stamina consumes neither the action nor reserve. Costs never change walking/stance speeds or bypass collision. Sprint exhaustion removes the run multiplier through existing acceleration and stays latched until the recovery threshold; holding Shift allows walking recovery and automatic sprint resumption afterward.

Firing spends up to the available reserve, so zero stamina never blocks firing. Empty/reloading/rejected attempts cost nothing. No accuracy/recoil/handling penalties yet. Jump and sprint costs exceed the initial per-shot cost. Sprint and shots may both contribute while simultaneous; accepted jumps replace ground-sprint drain on their launch tick. Failed actions do not restart the recovery delay; actual accepted shots at zero do restart it.

**API:** TrySpend rejects invalid/nonfinite/negative or unaffordable exact costs; TryJump wraps the configured jump cost; SpendPartial handles accepted-shot/continuous costs; RecordSprint is the movement adapter; CanSprint/Exhausted and Changed supply read-only gating/feedback. Values are bounded; disabled stamina restores unrestricted existing sprint/jump and hides its meter. Death stops spending/recovery. No normal healing/respawn/save semantics are added.

**Performance/time:** Drain uses the existing player movement callback. Only depleted non-sprinting stamina enables its own recovery physics callback (including the bounded recovery delay), stopping at full, death or unload. No timer, world search, polling while full, extra movement callback or stat per limb. Recovery can run while the player's movement sleeps. Physics delta uses gameplay seconds, not the accelerated day/night clock or wall time; tree pause stops it. Live-world menus/uncaptured FPS/focus release stop exertion and permit recovery. UI subscribes only to changes and viewport resize.

**GUI:** One shared UI/GameplayHUD/Stamina card at bottom-right, numeric reserve/bar/status, always visible for now, exported corner margin and narrow-window relocation. Mouse-through, no health arithmetic or smoothing loop. A later layout move is separate.

**Out of scope:** Hunger/satiation/thirst/fatigue, metabolism, weight, stamina costs for Q/bat/knife/dodge/climb, fitness, injury penalties, exhausted-firearm inaccuracy, healing, saved reserve and sleep.

**Validation:** Source/diff/resource/loop review completed. Existing DEBUG/TEST/PlayerVitals smoke now checks actual stamina code for idle sleep, sprint/walking, delay crossing, recovery clamps, jump affordability, exhaustion hysteresis, invalid costs, shot spending, master immutability and death shutdown. It manually steps recovery synchronously; it does not prove real movement/weapon integration. No new test scene. dotnet/Godot are unavailable here, so compilation and smoke/runtime/visual/performance checks remain unverified.

Locally run `dotnet build war-machine.csproj`, then `godot --headless --path . res://DEBUG/TEST/PlayerVitals/PlayerVitalsTest.tscn` using Godot .NET. In FirstPersonWorld, compare full-to-empty sprint (~10s of grounded effort), walking recovery, held-Shift resumption, stationary wall contact, jump press/hold/airborne/unaffordable cases, rifle/MG accepted shots and rejected empty/reload attempts, inventory/Escape/focus, death/reload and bottom-right layout at narrow/normal resolutions. Check secondary World and idle profiler behavior. No measured result is claimed.

**Next:** Tune the foundation only after local acceptance. Later stamina modifiers remain explicit later work. One commit to implement-td0-td1; see Git history for delivery SHA.

## PH3 — Hunger and thirst

**Objective:** Add visible food fullness, a hidden satiation buffer consumed before fullness falls, and thirst. Eating restores fullness/satiation; foods may sustain the player differently. Time and activity consume reserves. Later explicit consequences can affect accuracy, running or stamina recovery; avoid uncontrolled stacking.

**Dependencies:** PH0 (and preferably PH2 so penalties can target stamina). A single authoritative in-game time source must be chosen when rates are introduced.

**Scope / ownership:** `PLAYER/Survival/` contains slow update logic and thresholds; `PLAYER/Stats/` stores current values and emits changed events. Add grouped maximums, decay rates and threshold rules to the player master. Provide explicit `Eat(amount)` and `Drink(amount)` entry points for future items; at severe thresholds, route a non-anatomical vitality hazard through PlayerVitals and the existing shared damage resolver, rather than setting vitality directly or inventing a torso wound.

**Out of scope:** Food items, recipes, stomach simulation, diseases, offline progression, complex metabolism.

**Preserve:** Player health/damage rules; no background work in unrelated actors.

**Performance:** One low-rate shared survival tick, not one timer per statistic; sleep when disabled, stop at death/unload, no world searches; reactivation handles elapsed game time deterministically within reasonable bounds. Do not implicitly use wall-clock/offline decay until save/time semantics exist.

**Pass:** Values decrease at configured in-game rates, eating/drinking clamp safely, threshold effects activate once/when appropriate, HP damage uses shared pipeline, disabled simulation is dormant.

**Assistant:** Codex / Work for time integration; Regular Chat can design rate/threshold tables; Copilot for accelerated time checks.

## PH4 — Fatigue and rest

**Objective:** Model longer-term fatigue (higher means more tired), distinct from short-term stamina. Sleep is the main recovery method; food may provide modest bounded relief but must not replace sleep. Eligible minor-injury sleep healing requires its own PH5 rules.

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
