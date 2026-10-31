# WH — Weapon mass, handling and player-condition integration plan

**Repository:** `ben0kay/war-machine`  
**Branch:** `implement-td0-td1` (not `main`)  
**Created:** 2026-10-31  
**Status:** **Planning only — WH0–WH5 not implemented or authorized.** Codex should implement only a separately requested phase; this document does not authorize a bulk rollout.  
**Scope:** Future FPS firearm handling, with shared player/stat ownership and compatible top-down/shared firing. Do **not** change the project startup scene, FPS-only conversion roadmap, existing terrain, or unrelated player survival systems.

## Goal

Turn today's independent per-gun knobs into **one coherent, explainable handling calculation**. Physical equipped-weapon mass/balance should influence carrying movement, ADS raise/lower speed and physical sway. Current player state (stamina, movement, stance and eventually arm condition) should influence steadiness and handling. Later fatigue, skills, injury consequences and equipment changes should feed the **same calculation** when their own gameplay systems exist. Preserve the existing ballistics, input, camera recoil, muzzle obstruction and animation owners; do not add a parallel weapon simulator.

A deliberately heavy firearm should *generally* feel more cumbersome, but mass **must not be the only handling property**. Balance, ergonomics, stock/support and the user's physical condition can affect how quickly it settles or how much it drifts; a physically heavy but well-braced rifle is not necessarily less stable than a lightweight awkward one. Gameplay baselines stay editable.

## Verified foundation at plan creation

| Existing capability | Authoritative files/state | Current behaviour / limitation |
| --- | --- | --- |
| Weapon resources and shots | `EQUIPMENT/Weapons/Shared/FirearmSettings.cs`, `FirearmController.cs`, per-gun `Configuration/*Master.tres` | Per-gun firing, damage, cooldown, one random `ShotSpread.Apply` sample, ammo and recoil; the controller is shared by views. No mass field or unified handling profile. |
| Equipped slowdown | `FirstPersonWeapon.cs` -> `PLAYER/FirstPerson/FirstPersonPlayer.cs` | `EquippedMovementMultiplier`: Rifle **0.90×**, MachineGun **0.97×** while FPS-equipped. Authored penalty, **not computed from physical mass**. |
| ADS slowdown and time | Same FPS weapon/player + `WORLD/FirstPerson/Configuration/FirstPersonSettings.cs` | `FpsAdsMovementMultiplier`: rifle **0.70×**, MG **0.85×**, multiplied by equipped penalty. ADS transition uses common 0.20 s divided by per-weapon animation speed (rifle 1.10×, MG 0.90×). Toggle on MMB, not hold. |
| Real physical sway | `EQUIPMENT/Weapons/FirstPerson/FirstPersonAimTarget.cs`, `FirstPersonWeapon.cs` | Source-implemented independent angle oscillator: rifle **0.18° / 0.80 Hz / 0.65× ADS**, MG **0.25° / 0.95 Hz / 0.70× ADS**. Sway changes visible gun root and the **real shot target**; no player-state or mass input yet. |
| Bullet inaccuracy | `ShotSpread.cs` and FPS shot adapter | Rifle base **0.30°** + FPS hip extra **0.55°** (0.85° total hip, 0.40 radial exponent); MG base **0.75°** + hip extra **0.20°** (0.95° hip, 0.50 radial exponent). ADS omits only hip extra. These angles are **maximum deviations**, not recoil or sway. |
| Recoil | Shared `FirearmRecoil` and FPS camera/weapon adapter | FPS accepted shots raise the actual camera persistently; model-only kick is separate. **Never apply camera recoil again to the bullet direction**. |
| Stamina | `PLAYER/Stats/PlayerVitals.cs` -> `PlayerStamina.cs` | PH2 reserve, sprint/jump spending, accepted gunshot `StaminaCostPerShot`, recovery, `Changed` event. **Currently no low-stamina aim penalty.** Disabled stamina should act neutral. |
| Limb condition | `PLAYER/Stats/BodyCondition.cs` | Six stored body regions with `ConditionChanged`; arms are available for future handling penalties, but **normal player capsule hits still resolve to Torso** without a separate player-anatomy hitbox phase. No current arm-derived sway penalty. |
| Stance/motion | `PlayerController.cs`, `PLAYER/Movement/PlayerMovement.cs`, `FirstPersonPlayer.cs` | Standing/crouching/prone and actual `Velocity`; sprint is processed by existing movement/stamina code. Do not infer movement solely from keys. |
| Debug | `DEBUG/Overlay/DebugOverlay.cs`, `DEBUG/Overlay/FirstPerson/`, FPS weapon/debug aim line | F1 overlay is **shared** and event-driven. Existing cyan line follows swayed **pre-random-spread** aim; `FirstPersonWeapon.SetPhysicalSwayEnabled(bool)` exists, but **no Weapons F1 category** yet. |
| Future survival | `PLAYER/IMPLEMENTATION_PLAN.md` PH3–PH5 | Hunger/thirst, fatigue/sleep, medical consequences remain **planned**. Do not treat them as implemented stats. |

**Important placeholder:** `MachineGun_001` is also being considered as a future **SMG** visual/role. The authored movement values **cannot** be used to infer a realistic gun mass. Choose/approve each firearm's intended class and mass before tuning, or use safe legacy fallback values.

## Architecture and ownership contract

- **Weapon definition:** Existing `FirearmSettings` and each current gun's `.tres` own intrinsic mass (kg), balance/ergonomics and baseline handling properties. Do not create a second authoritative firearm resource, change the shared weapon slot contract or rewrite shooting. The existing FPS-specific fields remain until each one is explicitly superseded.
- **Player state:** `PlayerVitals/PlayerStamina/BodyCondition` remain authoritative for current condition. `PlayerController`/shared movement remain authoritative for actual stance, sprint and velocity. Weapon code reads snapshots, **never mutates stats to simulate weight**.
- **One evaluator:** A small, pure calculation helper (proposed `EQUIPMENT/Weapons/Shared/WeaponHandling.cs` or similarly feature-owned name) takes a **weapon profile + read-only player context** and returns a bounded **effective handling snapshot**. It does not own scene nodes, persistent timers, GUI, or damage. A small `WeaponHandlingSettings` resource/master is appropriate **only** for global curves/tuning shared between guns.
- **View application:** `FirstPersonWeapon` remains the sole FPS ADS/sway/firing adapter; `FirstPersonPlayer` remains the sole owner of FPS movement modifiers; `FirstPersonCamera` remains the sole writer of FPS look/recoil/FOV; `FirearmController` remains sole owner of final shot dispersion and collision/damage. Top-down retains current factors until an **explicitly authorized** cross-view follow-up.
- **Future equipment/inventory:** Equipped weapon mass is not automatically the full carried inventory weight. When inventory burden arrives, define **one** source of total load and make sure the selected gun is not counted twice. A weapon's handling burden and pack encumbrance are different concepts.
- **Authoring safety:** Runtime settings are deep copies or computed snapshots; debug changes never silently rewrite master `.tres` files. No world singleton/manager/duplicate aim driver for one player's gun.

### Proposed data contract (names indicative; Codex should verify usage before adding types)

```text
FirearmSettings (existing)
  PhysicalMassKg?                 # authored real-world mass; legacy/unconfigured remains safe
  Balance/ErgonomicsFactor?        # weapon-specific handling separate from mass
  Existing ADS / sway / recoil / spread fields kept until migrated

PlayerHandlingContext (read-only snapshot, new plain struct)
  NormalizedStamina, StaminaEnabled
  Stance, HorizontalSpeed, IsSprinting
  NormalizedLeftArm, NormalizedRightArm
  FutureFatigueNeutral, FutureSkillNeutral (do NOT instantiate fake stats)

EffectiveWeaponHandling (computed, new plain struct)
  EquippedMovementMultiplier
  AdsMovementMultiplier
  AdsTransitionSpeedMultiplier
  PhysicalSwayAmplitudeDegrees, SwayFrequencyHz
  OptionalSpreadMultiplier, RecoilMultiplier (later phase only)
  Reason/breakdown values for debug (no production string allocations per frame)
```

**Prefer reusing existing names/contracts** (e.g. `SetEquippedWeaponMovementMultiplier`, `SetAdsMovementMultiplier`, `FirstPersonAimTarget.SamplePhysicalSway`, current FPS shot overload) over introducing parallel setter chains. A new field only exists if it has one owner and an actual reader.

### Combining factors without double-counting

Suggested mental model, **not final balance numbers**:

```text
base handling  = current authored per-weapon value or calibrated mass/balance curve
player factor  = stamina × arm stability × stance × movement (each effect used once)
effective     = clamp(base handling × applicable player factor, safe bounds)

movement speed: shared movement computes stance/sprint/etc × ONE equipped factor × ONE ADS factor
ADS duration:  existing base duration / effective ADS animation speed
sway:          existing oscillator × effective amplitude × ADS blend
shot spread:   existing base + hip extra, then OPTIONAL future handling factor, sampled ONCE
recoil:        preserve FPS camera's one-way actual pitch and existing shot/visual kick ownership
```

**Critical:** Existing stance/sprint/lean/corpse slowdown is already applied by shared movement. Do **not** multiply those penalties again inside the new handling calculation. Similarly, the current gun's fixed `EquippedMovementMultiplier` must be **replaced or used as legacy fallback**, not multiplied on top of a new mass-derived factor. Keep camera rise and random dispersion as independent quantities.

## Phases — implement independently, request authorization per phase

| Phase | Purpose | Status | Suggested owner |
| --- | --- | --- | --- |
| **WH0 — Compatibility + handling profile contract** | Baseline audit, typed input/output, mass/balance authoring, exact legacy fallback | **Planned** | Codex |
| **WH1 — Weight affects movement + ADS handling** | Connect weapon mass/profile to existing FPS equipped/ADS speed and transition interfaces | **Planned** | Codex |
| **WH2 — Player state influences physical sway** | Connect existing stamina, stance, actual movement and arm condition to the **same** real-aim oscillator | **Planned** | Codex |
| **WH3 — F1 Weapons debug and A/B toggles** | Diagnostic toggles/breakdown for weight, sway, spread, recoil, player-condition effects | **Planned** | Codex |
| **WH4 — Optional accuracy/recoil handling integration** | Reuse ACC1/ADS2 interfaces for condition-based spread or recoil only if separately approved | **Planned** | Codex |
| **WH5 — Future survival, skills, attachments and encumbrance** | Add available PH4/PH5/future progression/inventory inputs through same context when those systems ship | **Planned / dependent** | Codex |

### WH0 — Compatibility + handling profile contract

**Goal:** Introduce an inspectable source of physical gun mass and a single handling result **without changing current feel**.

**Dependencies:** None beyond existing source; read `ADS_IMPLEMENTATION_PLAN.md`, `ACCURACY_IMPLEMENTATION_PLAN.md`, `RECOIL_IMPLEMENTATION_PLAN.md`, `PLAYER/IMPLEMENTATION_PLAN.md` and current feature READMEs first. Confirm whether recent unvalidated sway/debug code builds before changing it.

**Scope:** Audit every consumer of `EquippedMovementMultiplier`, `FpsAdsMovementMultiplier`, `FpsAdsAnimationSpeedMultiplier`, `FpsSwayAmplitudeDegrees` and shot spread; document a neutral fallback for unconfigured/custom weapons and melee. Add one compact, typed evaluator and only the required settings, keeping all existing master resources and values intact until the intended mass/SMG class is confirmed. Establish one explicit **legacy-vs-derived handling mode** or migration rule, not implicit two-layer multiplication. Decide how weapon mass units and balance bias are authored.

**Exclusions:** No new gameplay penalties, endurance stat, inventory mass, GUI, gun models, projectile system or extra physics/process callback. Do not alter top-down behaviour.

**Performance/acceptance:** Evaluator is pure, deterministic, nonallocating and dormant when unused; neutral inputs reproduce rifle 0.90× carried / 0.70× ADS / 1.10× ADS animation, MG 0.97× / 0.85× / 0.90×, and their current sway values. Disabled/missing fields remain backward compatible. Initial build + small source regression; one logical commit.

### WH1 — Weight affects FPS walking, ADS slowdown and ADS raise/lower speed

**Goal:** Equip a heavier or less ergonomic gun and visibly feel the burden in the **existing** movement/ADS paths.

**Dependencies:** WH0 baseline approved. Owner confirms intended rifle vs MG/SMG roles and either sample physical masses or a suitable game-balance tier; do **not** guess from the old slowdown percentages.

**Scope:** Calibrate a configurable mass/balance curve with hard limits and explicit neutral/reference values; feed its computed results to the existing FPS `SetEquippedWeaponMovementMultiplier` and `SetAdsMovementMultiplier`, and the existing ADS transition-speed calculation. Avoid adding a second ADS animation. Only emit movement changes on equip, successful swap, ADS edge or relevant profile change, not per frame. Existing stance, sprint, lean, fatigue-independent movement rules and carried multiplier must remain composed **once**.

**Exclusions:** No total backpack encumbrance, new stamina drain or player skills. Sway, accuracy and recoil are unchanged in this phase.

**Acceptance:** Rifle/MG slot switches do not stack penalties; no stuck speed after unequip, bat, Q, Esc, focus/UI interruption or ADS reversals. Tune the initial effective values to match old gameplay first, then test one deliberate heavier/lighter override; F6 FPS and brief F6 top-down regression. Inactive weapon profiles consume no updates.

### WH2 — Stamina, stance, movement and arms affect **physical** sway

**Goal:** The actual aim point (and cyan debug line) reflects player steadiness, not just a preset per-weapon sine wave.

**Dependencies:** WH1, live PH2 stamina and PH0 body condition. PH-HIT is **not** required for the underlying modifiers, but ordinary incoming hits will not exercise arms until player-anatomy routing exists.

**Scope:** Capture normalized stamina and both arm-region conditions from their authoritative nodes; cache event-driven changes from `PlayerStamina.Changed` and `BodyCondition.ConditionChanged`. Sample actual horizontal velocity and stance through existing player movement/physics when active. Standing still should be the baseline; crouching/prone may stabilize; movement/sprinting may worsen; depleted stamina and damaged arms may worsen sway **within clear bounds**. Use an editable mapping resource/fields; do not hardcode severity in the FPS camera. Feed a single effective amplitude/frequency into the **existing** `FirstPersonAimTarget.SamplePhysicalSway`/muzzle-target calculation—**no second sway oscillator**.

**Exclusions:** Do not infer pain, fractures, hunger, fatigue, experience levels or player weapon proficiency; those stats are not implemented. No new anatomy colliders or medical effects. No additional random bullet spread in WH2.

**Performance/acceptance:** While aiming, camera/recoil, gun root and bullet direction remain consistent; cyan debug line shows **swayed pre-random-spread** aim. No increased query count. With stamina disabled/full and arms healthy, preserve baseline settings. Verify low stamina, crouch, prone, moving, sprinting and an explicitly damaged arm via existing test route. Remove subscriptions on unload and sleep with disabled sway/unselected bat/mouse uncaptured. No additional permanent stat polling loop.

### WH3 — One F1 Weapons panel for controlled experiments

**Goal:** Diagnose each mechanic independently and compare real shooting without editing .tres values between tests.

**Dependencies:** WH1–WH2 so effective factors are meaningful. Reuse existing `DEBUG/Overlay/DebugOverlay.cs` and small `DEBUG/Overlay/FirstPerson/` UI helpers. One overlay only.

**Controls proposed:** `Enable weight effects`, `Enable physical sway`, `Enable random spread`, `Enable recoil`, `Enable player-condition influence`, and `Show cyan real-aim debug line`. Optionally show a read-only per-gun breakdown: authored mass, effective equipped/ADS movement, ADS time, effective sway amplitude, base/hip spread, stamina, stance, and active arm penalties. Use checkbuttons; optional tuners only where they help balancing. Debug switches are **runtime-only**, affect the currently selected gun or an explicitly chosen global testing override, and have a clearly documented scope/persistence rule.

**Isolation rules:**
- Sway off removes **both** real-aim drift and associated gun model drift, but retains random spread/recoil.
- Random spread off supplies **zero spread to the same shared shot sample**, without changing camera aim or bypassing collision/damage.
- Recoil off disables the entire matching FPS gameplay+visual recoil feedback path in a coordinated way, **not** only the model and not by silently changing permanently achieved camera pitch. Existing accepted-shot stamina/ammo still applies.
- Weight off uses neutral handling-derived burden while leaving normal stance/sprint movement intact.
- Player-condition off uses **neutral player handling factors**, not infinite stamina or healed limbs.
- Debug line tracks **pre-random-spread** physical direction and can show a separate stationary camera-centre reference if explicitly requested; avoid extra per-frame raycasts when toggled off.

**Performance/acceptance:** UI creates no per-frame polling. Toggle handlers update runtime values/event subscriptions, restore normal baselines cleanly and don't rewrite authored masters. No duplicate controls in top-down; hide/disable view-inapplicable controls. Confirm F1 hide/capture/GUI focus, slot switches, Escape, focus loss, and all-on/all-off A/B cases. Existing debug line should be off-able without unnecessary ray queries.

### WH4 — Optional handling effects on accuracy and recoil

**Goal:** If balance tests show a need, make posture/condition/weapon class influence accuracy and recoil through the **existing** shot and camera pipelines.

**Dependencies:** WH2/WH3. Review `ACCURACY_IMPLEMENTATION_PLAN.md` **ACC1** (stance/movement spread) and ADS2 before coding. Choose **one** coherent accuracy owner and update the older plans to point here rather than implementing duplicate ACC1 code.

**Scope choices (separate approvals):** A bounded factor on total existing `BaseSpreadDegrees + FpsHipFireExtraSpreadDegrees`; optional configurable ADS benefit; and/or a controlled strength/condition modifier on existing accepted-shot camera recoil. For a direct physical-sway effect, avoid simultaneously applying an identical movement penalty to random spread without tuning. Use the **one** existing `ShotSpread.Apply` invocation. Camera recoil stays one-way and requires actual mouse compensation; never add an additive recoil offset to FPS bullet direction a second time.

**Acceptance:** Toggle each mechanic on/off in WH3 and verify its isolated influence. ADS vs hip, single shots vs bursts, muzzle obstruction, traces, ammo/reload, near-wall firing and F6 top-down remain correct. Shot-only spread calculations have no idle cost.

### WH5 — Later fatigue, skills, injuries, attachments and total load

**Goal:** Feed newly implemented systems into the same handling context rather than adding new ad hoc firearm modifiers.

**Dependencies:** Their own explicitly approved/implemented player and inventory phases. PH4 fatigue is **not** the current stamina reserve. PH5 detailed injuries, weapon skill, gear mods, optics and backpack burden do not currently exist as authoritative gameplay values.

**Potential additions:** Fatigue/tiredness makes settling more difficult; strength/weapon skill improves heavy-weapon control within caps; arm injuries affect stability/support-hand ability; attachments influence mass/balance, optics and ADS, and stamina expenditure; overall carried load influences endurance/mobility independently of equipped-gun handling. The evaluator receives **neutral values until a real source exists**. Decide when equipment weight is included in inventory encumbrance so it is never charged twice.

**Exclusions:** Do not author the entire survival/progression/attachment system inside `FirearmController` or WH1. Each new stat needs its own player/gear feature approval, UX, persistence plan and validation.

## Suggested defaults and tuning principles (illustrative, not gameplay values)

- Baseline full-stamina, healthy arms, standing-still should evaluate to **1.0× player factor**; preserve each weapon's existing hand-authored handling baseline during migration.
- Aiming should reduce, **not eliminate**, physical sway; current 0.65× rifle / 0.70× MG ADS reductions make useful starting references.
- Lower stamina should **gradually** worsen steadiness (not an instant cliff at 0); a disabled stamina feature means neutral modifier. Do not deduct stamina merely for steady ADS until specifically approved.
- A damaged arm should affect weapon handling rather than simply duplicating leg movement penalties. Distinguish support and dominant hands only when a handedness contract actually exists.
- Cap each effective stat and the combined output. Avoid unrealistic multiplicative extremes (e.g. simultaneous sprint + zero stamina + arm injuries yielding unplayable sway or animation).
- Keep mass, balance and handling curves **inspectable** in resource settings. Do not embed uneditable magic mappings or derive masses by reversing the current speed percentages.
- Continuous physical sway justifies an active gun render update; inactive/unselected/disabled guns must sleep. Recompute expensive condition coefficients **on input/events or only while genuinely moving**, not through a new always-active observer.

## Cross-system integration checklist

1. **Before code:** Re-read current `AGENTS.md`, `CodePreferences.md`, `README.md`, `LessonsLearned.md`; current branch and the ADS/accuracy/recoil/player/overlay READMEs and sources. Identify any changes since this plan.
2. **One authority per stat:** `FirearmSettings` for weapon traits, `PlayerVitals` for bodily stats, `PlayerController` for movement/stance; handling helper calculates; existing adapter applies.
3. **No backwards regression:** Original scoped FPS movement penalties, ADS FOV and timing, iron sights, sway, gun slot/bat flow, reloading, accepted-shot stamina, line visual, gun root/muzzle offsets, camera recoil and shared damage/collision remain functional.
4. **No two random shots:** Sway is continuous and physical; spread remains one random sampling event per accepted bullet; recoil remains true FPS camera rise. Keep each separately disable-able for testing.
5. **Runtime lifecycle:** Equip/swap/reload, ADS toggle mid-transition, sprint, crouch/prone, severe injury changes, empty stamina/recovery, Q shove, F1 panel, Escape, lost focus, death and scene exit clear/restore state appropriately.
6. **Verification:** One initial `dotnet build war-machine.csproj` if available; F6 `WORLD/FirstPerson/FirstPersonWorld.tscn` for rifle/MG/bat and aim debug; brief F6 `WORLD/World.tscn` regression. Record **source implemented vs build tested vs gameplay accepted** accurately. No broad autonomous testing by default.
7. **Handoff:** Implement **one authorized phase per logical commit** on `implement-td0-td1`, push without force, update the owning docs/phase table, and report settings, changed paths, wake/sleep consequences and pending in-engine checks.

## Suggested first Codex assignment

> Implement **WH0 only** from `EQUIPMENT/Weapons/WEAPON_HANDLING_IMPLEMENTATION_PLAN.md` on `implement-td0-td1`. Read AGENTS/CodePreferences/root README/LessonsLearned and the current weapon/player plans and code. Create the typed mass/balance handling contract and safe legacy baseline with **no visible gameplay changes**, no new loops and no duplicate weapon/accuracy system. Do not start WH1–WH5. One commit/push, update docs, and report build/validation status. If rifle vs MachineGun/SMG physical masses are not specified, keep legacy fallback; do not invent masses.
