# CM — Melee, shove and hit-reaction implementation plan

> **Current status: CM0/CM1 implemented; CM2-A shared melee detection/profile implemented in source, awaiting local validation; CM2-B/C/D and CM3–CM5 remain Planned.** See COMBAT/README.md for exact contracts, tuning, no-knockback scope and validation handoff. Read [root README](../README.md), [CodePreferences](../CodePreferences.md), [COMBAT ownership](README.md) and the actual branch source before authorizing a phase. **Do not automatically implement the next phase.** Work on `implement-td0-td1` unless the owner requests another branch.

## Phase overview

| Phase | Deliverable | Suggested assistant | Status |
| --- | --- | --- | --- |
| **CM0** | Shared impact/attack contract and zero-damage reaction path; preserve current damage | Codex / ChatGPT Work; Regular Chat reviews contracts | **Awaiting Validation** |
| **CM1** | Bullets briefly stagger/slow living zombies, then pursuit resumes | Codex / Work; Copilot checks locally | **Awaiting Validation** |
| **CM2** | Universal quick shove in **top-down and FPS**, with view-specific aim but shared melee query/impact | Regular Chat for bounded implementation; Codex / Work for difficult physics bugs | **Planned** |
| **CM3** | Left-click equipped bat primary attack: configurable blunt arc/multi-target; quick shove remains available | Codex / Work; Regular Chat tunes feel | **Planned** |
| **CM4** | Knife primary attack + reusable shape/profile and animation semantics for future swords, grip modes and dual-wield | Codex / Work; Regular Chat tunes feel | **Planned** |
| **CM5** | Actor-neutral melee reuse for zombies striking the player (**after PH0**); bear/other creatures use species adapters later | Codex / Work; Copilot checks locally | **Planned** |

States: Planned → In Progress → Implemented → Awaiting Validation → Accepted. These are **independent authorization gates**, not permission to build the entire combat system in one pass.

## Updated melee design contract — 2026-10-27 (owner direction, planning only)

**Two independent actions, regardless of held weapon:** A dedicated **Quick Melee** button (provisional `V`) performs a low-damage, primarily defensive shove/punch/body-check. With a firearm, the pose is a gun-butt/weapon push; with a bat, sword or knife, it is a quick push with the held weapon, or an unarmed shove if appropriate. **Primary Attack** (left mouse) continues firing guns; with a melee weapon equipped, it performs that weapon's full damaging swing/stab. Both actions are different attack profiles with separate cooldowns, timing, damage and animation IDs; never implement quick shove as a firearm-only feature. No forced animation asset or dual-wield implementation in CM2.

**Reusable actor-neutral contact executor:** The same `COMBAT/Melee` execution path must work for future player, zombie, bear and other creature attacks. Actor-specific code supplies source/owner, orientation/aim, target filtering, profile and pose hook; the shared executor never reads player input, weapon slot indices, camera or zombie AI directly. Avoid a huge inheritance tree or global combat manager. Existing CM0 `HitTargetResolver`, `CombatImpact`, `IDamageReceiver` and `IImpactReceiver` remain the sole damage/impact dispatch paths.

**Invisible detection volume, only while attacking:** Conceptually each strike uses a short-lived invisible hitbox in front of the attacker. Prefer **bounded physics queries/swept shapes** (narrow capsule/box or arc sectors; not a permanently monitoring `Area3D` per actor), sampled on attack's active ticks. Shape/reach/width/angle, contact timing, target cap and LOS are data in the attack profile. A sword might use a wider sweep hitting several targets; a bat a narrower arc with stronger stagger; a knife a narrow single-target thrust; a bear a frontal paw arc; a zombie a short lunge/bite. Broad-phase shape overlap is **not** automatic damage: for each potential target, verify forward arc, distance, eligibility, occlusion against world solids (walls/doors/terrain) and per-swing deduplication before dispatching exactly one contact. A query must never apply damage through a wall just because its volume overlaps a collider behind the wall. Motion/sample the strike over a short bounded window to avoid tunnelling at low frame rates; put a hard cap on physics candidates and per-attack hits.

**Profiles separate hit feel from animation:** Proposed reusable `MeleeSettings` fields: action kind (`QuickShove`, `PrimarySwing`, `PrimaryThrust`, `Claw`, `Bite`), shape mode (short forward capsule/box, sweep arc, thrust), range, arc angle/width, vertical coverage, target cap, hit mask/filter, optional per-secondary-target falloff, damage/type, impact strength/push, stagger, windup/active/recovery/cooldown, and animation cue (`Shove`, `BluntSwing`, `WideSlash`, `Stab`, `Claw`, etc.). **Do not add every field to CM2 prematurely:** build one shove profile and the extension seams, then add sweep/target cap in CM3 and grip/advanced profiles only when real weapons need them. The contact executor must not know whether a weapon is held in one hand or two. Future one-/two-handed grips and dual wield can select different profiles/animation cues, including different damage/stagger/speed, without rewriting hit detection.

**Lifecycle/performance:** Button or AI decision starts a bounded windup → contact window → recovery; only active attacks request physics checks. Attack completion, target/source death, unequip, focus/menu interruption, deactivation or unload clears pending contacts and stops queries/pose work. Exactly zero per-actor melee polling while idle. Existing controller movement/collision still owns physical knockback; no teleporting targets, no extra health pipeline, no per-frame global scene scans.

**Both camera modes required from CM2 (owner decision):** Implement Quick Melee input and aim adaptation in **both** `PLAYER/Equipment/PlayerWeaponController.cs` (overhead) and `DEBUG/TEST/FirstPerson/EQUIPMENT/FirstPersonWeapon.cs` (FPS). The shared melee executor receives world-space origin, attack orientation and profile, never a Camera reference. **Top-down:** derive horizontal XZ direction from existing player/mouse-facing aim; test reach and horizontal attack arc with a generous but **bounded vertical target-height interval**, sufficient for ordinary standing/crouched zombies and modest slopes, never targets on unrelated floors. Camera height/pitch must not make top-down swipes miss. **FPS:** derive 3D attack orientation from the centre-screen camera ray (yaw **and pitch**); evaluate a short 3D swung/swept volume with finite angular/vertical thickness, **not** an infinitely thin sight ray. Looking significantly above/below a nearby enemy should miss; a sensible near-centre aim should hit. Both modes still check occluding world geometry and target eligibility; matching horizontal facing alone never guarantees contact. Attack origin should be based on the character/weapon reach rather than cast directly from the distant overhead camera or through nearby walls. Use the **same** damage, reaction, cooldown and hit deduplication code. View-specific input/animation adapters are thin and cancel safely; FPS should not need an alternate health or melee implementation.

**Phase boundaries:** CM2 establishes the actor-neutral attack/query seam and *one-target* Quick Melee for the overhead player even if a firearm is equipped. CM3 proves primary attack + configurable **multi-target blunt sweep** and limited melee equipping. CM4 adds narrow knife primary attacks and generalizes attack shape/animation cues for eventual wide sword attacks and grip-specific profiles; actual sword/dual-wield weapons are **future separately authorized work**, not bundled into CM4. CM5 makes a zombie an attacker after PH0 and proves no player-only assumptions; bears and other wildlife become later species-specific adoption, not an implicit CM5 deliverable. FPS may reuse core logic but receives view-specific pose/input only when explicitly authorized.

**Minimal Codex work strategy:** Regular Chat can document/profile defaults, prepare isolated settings/input/pose adapters, and review existing input/equipment contracts. Reserve Codex for physics overlap/occlusion, target deduplication, knockback integration into `CharacterBody3D` and the small shared execution boundary. No imported animations required: start with an optional procedural weapon/body shove cue. Keep CM2 small; do not prebuild sword, bat, bear, dual-wield or generalized animation frameworks.

## 1. Source snapshot at planning — 2026-10-27 (CM0/CM1 changes now described in README)

- **Active main game:** `WORLD/World.tscn` uses `WORLD/WorldController.cs` and the overhead view; `DEBUG/TEST/FirstPerson/FirstPersonTest.tscn` inherits the world and substitutes view-specific controls. The main game is top-down. FPS is a useful debug comparison, not the feature priority.
- **Existing damage:** `ENTITIES/Shared/Combat/DamagePacket.cs` has `Amount`, `Type`, `Source`, world `Position` and `Direction`; `DamageResolver` performs physical armour/resistance calculations. `IDamageReceiver.ReceiveHit(DamagePacket)` is generic but currently returns `void`. `HealthComponent.ApplyDamage` notifies `Damaged(DamageResult)` and `Died(DamagePacket)` and has no always-active processing.
- **Shooting:** `EQUIPMENT/Weapons/Shared/FirearmController.cs` performs cooldown, one-shot cover/hitscan queries and walks up the struck collider's parent chain for `IDamageReceiver`. `PLAYER/Equipment/PlayerWeaponController.cs` handles mouse aim/trigger in top-down; FPS has `DEBUG/TEST/FirstPerson/EQUIPMENT/FirstPersonWeapon.cs`. Firearm settings are per-weapon `FirearmSettings` and `.tres`. The rifle is **95 physical damage** and the machine gun is **35 physical damage, 0.10s between shots**.
- **Zombies:** `ENTITIES/TYPES/Zombies/ZombieController.cs` forwards hits to `HealthComponent`; it subscribes to **`Died` only** and deactivates/frees on death. There is **no nonlethal flinch, stun, stagger or knockback**. It chooses wander/chase on a staggered one-shot sight timer (nominally 0.4s), and its `CharacterBody3D._PhysicsProcess` currently accelerates toward a goal. Both Zombie_001 and Zombie_002 use this shared controller; the runner is fast (8.3m/s chase) and should visibly react to interrupts.
- **Equipment and animations:** `EQUIPMENT/Shared/WeaponSlots.cs` supports two **firearm-only** test slots (keys 1/2); it cannot simply accept a bat or knife scene today. `PLAYER/PlayerController.cs` owns upper-body/weapon pose; zombie gait/arms are currently procedural. No melee animation, collision sweep, stamina cost or generic hand-held item/quickbar binding exists.
- **Player health:** `PLAYER/IMPLEMENTATION_PLAN.md` phases **PH0–PH5 remain planned** in the reviewed source; PH0 would make the player an `IDamageReceiver`. Zombies currently **cannot damage the player**. PH2 may introduce stamina; no stamina dependency for first melee prototype.
- **No prior implemented melee/stagger phase** or related stand-alone roadmap was found in the relevant combat/equipment/source and plan files. `PLAYER/IMPLEMENTATION_PLAN.md` mentions future melee stamina and existing damage README mentions melee as a possible caller only.

### Key requirement: damage and impact are different

A gun-butt shove can push a zombie **without reducing HP**. The present `HealthComponent` ignores nonpositive damage and therefore will not emit a `Damaged` event for a zero-damage shove. **Do not fake damage** to activate knockback. Design an independent, optional **impact/reaction path** that can accompany a damaging hit or occur alone. Impact resistance/immunity is a *target policy*; damage armour/resistances remain in `DamageResolver`. A lethal hit must win over the reaction.

## 2. Target ownership and narrowly scoped interfaces

```text
COMBAT/                                      # new cross-actor owner; this plan exists now
  README.md
  IMPLEMENTATION_PLAN.md
  Shared/                                    # CM0+, new small impact/dispatch primitives
    CombatImpact.cs                          # proposed readonly value payload: location, horizontal direction, strength, reaction kind
    IImpactReceiver.cs                       # proposed optional interface, not HP dependent
    HitTargetResolver.cs                     # proposed bounded collider→ancestor resolver, reuse once per contact
  Reactions/                                 # CM1+, reusable policy/timer primitives only if needed
    HitReaction.cs                            # proposed small event-driven stagger/push helper
  Melee/                                     # CM2+, active attack window / multi-target contact
    MeleeAttack.cs                            # proposed reusable executor; no player-specific input
    MeleeSettings.cs                          # proposed attack profile Resource
  Configuration/                             # create with first actual global tuning need
    CombatMaster.tres                         # proposed optional combat-wide clamps/defaults
ENTITIES/Shared/Combat/                      # CURRENT damage implementation; do not duplicate
  DamagePacket.cs, IDamageReceiver.cs, DamageResolver.cs,
  HealthComponent.cs, HealthSettings.cs
EQUIPMENT/
  Weapons/Shared/FirearmController.cs        # existing hitscan, future impact handoff
  Weapons/Shared/FirearmSettings.cs          # existing per-gun data, future small impact profile
  Melee/                                     # CM3/CM4 only; item prefabs + weapon config
    BaseballBat_001/                         # proposed scene, .tres, C/ procedural art, Artwork/
    Knife_001/                               # proposed scene, .tres, C/ procedural art, Artwork/
PLAYER/Equipment/PlayerWeaponController.cs    # existing overhead attack/input adapter
DEBUG/TEST/FirstPerson/EQUIPMENT/            # optional FPS view adapter, explicitly scoped
ENTITIES/TYPES/Zombies/ZombieController.cs  # recipient motion/pose, future attacker
```

These are **proposed ownership targets, not created empty directories or mandatory exact class names**. Prefer the smallest working contracts and review any new class against existing shared helpers. **`C/` means procedural mesh generation only**; gameplay scripts belong directly in their owning folders. Reuse the current `IDamageReceiver` pipeline and `HealthComponent`; do not create a separate `COMBAT/DamageResolver`. When the game truly needs a move from `ENTITIES/Shared/Combat` to `COMBAT/Shared/Damage`, schedule it as an **explicit optional cleanup**, updating `.tres` script references and Godot `.uid` files together; do not mix that path migration into the first melee implementation.

### Event contract (recommended starting design)

1. An attack supplies one **contact** (hit point, normalized XZ push direction, source, optional positive `DamagePacket`, impact kind/strength/stagger duration). Source direction is taken from the attacker toward the target or the hit, with a safe fallback for coincident positions. Do not rely on a projectile's raw vertical ray direction for physical pushback.
2. A bounded target resolver finds the same target root once from the contacted collision shape, excludes the owner/self and stops at the first relevant supported receiver. Send positive damage through current `IDamageReceiver`; send optional impact through `IImpactReceiver` (exact names can be finalized in CM0). A zero-damage shove sends impact **without** calling health. The recipient rejects impacts when dead/inactive, outside allowed types or immune.
3. A `CharacterBody3D` recipient owns how its existing motion is interrupted, slowed and recovered. Reactions must not change the shared damage formula or have the attacker directly move/teleport enemy transforms. **Death has priority**: lethal hit queues the existing removal and never reactivates/staggers the corpse.
4. For bullets, deliver the configured optional impact along the existing accepted firearm hit, not through another raycast. For melee, detect contacts only during a defined swing's active frames/physics ticks, resolve each actor **once per swing**, and pass the same contact contract to each eligible actor.
5. Generic attack/reaction code knows only interfaces, physics contacts and profiles—not ZombieController, PlayerController, camera mode, weather, Terrain3D or robot internals. Owner-specific animations, stamina or AI reaction strength belong in adapters/profiles.

## 3. Design rules and starting balance proposals (not committed values)

| Attack | Damage idea | Reach/targets | Reaction idea |
| --- | --- | --- | --- |
| Rifle bullet | Keep current **95** initially | Hitscan, one target | Nonlethal flinch ~0.15–0.25s; small/no knockback |
| Machine gun bullet | Keep current **35** initially | Hitscan, one per bullet | Brief soft stagger ~0.10–0.18s, with cooldown/anti-stun-lock |
| Gun-butt shove | **0–5** physical HP | ~1.5m, narrow arc, **one** target | Noticeable push ~0.5–1.0m + ~0.3–0.5s stagger |
| Baseball bat | ~20–35 physical HP | ~2.0m arc, max **three** distinct targets | Moderate flinch, stronger first/closest-hit push |
| Knife | ~25–40 physical HP | ~1.1–1.4m, **one** target | Faster contact, little push, more precise hit |

These are **design-test starting points**, not implemented stats or decisions about final weapon balance. The current 95-damage rifle leaves only 5 HP on a default 100-HP zombie: stagger will be visible only until its **second shot kills**. Tune damage/reaction separately after checking the feel rather than silently nerfing the rifle in CM1.

**Avoid perpetual stun-lock:** stagger duration cap, per-target minimum re-flinch interval, optional severity threshold, and diminishing effect for rapid bullets (important for the 600 RPM machine gun and future hordes). Small gun strikes should be an emergency spacing tool—not the strongest weapon or free permanent crowd-control. Armoured robots may ignore light pushes entirely.

### Melee detection principles

- Character faces the existing top-down mouse aim; attacks project a short reach/arc in front of the weapon or actor, not a giant radial zone around the player. Keep the hittable volume close enough that a bat feels like a swinging object.
- Prefer bounded physics shape queries or a short swept shape on **active swing ticks** over a persistent enabled `Area3D` that can repeat hits indefinitely; whichever is used must honour physics collision masks, obstacles and attack timing.
- Deduplicate targets with a small **per-swing hit set** (instance ID/RID) and enforce a configured maximum (bat three, knife one, gun-butt one). No single target receives repeated damage from overlapping bodies or multiple frames of the same swing.
- Reject hits through solid walls/closed doors using collision or bounded line-of-sight/occlusion checks; exclude self and allies/neutral actors where appropriate. The logic must work against 1, 2, 3 and 20+ tightly packed zombies without full-world scans.
- Apply contact only during attack active windows with windup, active contact, recovery and cooldown; a missed swing still consumes its attack cooldown. Gameplay window and pose may start as event-driven placeholder motions; later bat/knife animations must swap art/animation hooks without changing collision logic.
- Equip/unequip, death/deactivation, focus loss, menu interaction and scene unload cancel pending attack windows, hit sets, VFX and timers. Return to idle with **zero attack-processing cost**.

## CM0 — Minimal impact/reaction foundation

**Objective:** Make damage-independent impact possible without breaking the current damage/health API. It must be usable by a firearm, future melee, zombies and the player when PH0 exists.

**Dependencies:** Current `DamagePacket`, `IDamageReceiver`, `HealthComponent`, existing FirearmController's collider-parent lookup, ZombieController lifecycle; nothing from future stamina/inventory.

**Implementation scope:** Add the smallest readonly impact payload (`CombatImpact` or similar), optional `IImpactReceiver`, and a minimal reusable contact-to-recipient resolver/dispatcher only if it reduces duplication. Direction, source, type/strength, optional interruption time are explicit. Support `0 HP + impact` and `positive HP + impact`; do not add fake damage. Preserve the legacy damage-only path for receivers with no new interface. Define ordering so death wins. Design for configurable target immunity/resistance and capped durations, but defer a large status-effect framework.

**Owning files:** new `COMBAT/Shared/` primitives and README; narrowly updated `EQUIPMENT/Weapons/Shared/FirearmController.cs` only if dispatcher adoption is part of this authorized phase. Create `COMBAT/Configuration/CombatSettings.cs` / `CombatMaster.tres` only if a real global setting is used. Update `ENTITIES/Shared/Combat/README.md` and this plan. Avoid renaming or moving existing health scripts.

**Out of scope:** Visible stagger, physical push, melee input/animation, item slots, player health, status DoT or robot reactions.

**Preserve:** Rifle/MG ray count, cover checks, 95/35 damage, armour/resistance/death semantics, on-hit and miss behaviour, per-weapon profiles, no new permanent loops. One resolved actor per contacted ray/collider.

**Performance / lifecycle:** Value structs, bounded parent walk and direct callback on contact; idle zero callbacks; no global actor registry and no per-hit Node creation. Recipients detach on despawn.

**Pass / owner handoff:** Basic build; 95-damage default zombie still dies on shot two, hits on cover never pass through, non-damageable scenery remains a blocker, zero-damage impact route can be invoked without reducing HP, inactive/dead receiver ignores it, no duplicate damage. Visual impact not expected yet. **Codex / Work** for touching combat contracts and hitscan; **Regular Chat** for review/tuning, **Copilot** for local compile.

## CM1 — Zombie bullet-hit stagger

**Objective:** Rifle/MG nonlethal hits visibly interrupt or slow a chasing zombie briefly; normal chase resumes.

**Dependencies:** CM0; existing ZombieController and its physics gait/brain timer, `HealthComponent.Damaged` event, per-gun `FirearmSettings` and zombie `ZombieSettings`.

**Implementation scope:** Add a small reaction state/helper, **actor-owned** application and expiry. Each gun supplies configurable impact severity; zombie master defines flinch threshold, resistance, minimum re-trigger interval, maximum consecutive interruption time and recovery acceleration. An accepted nonlethal hit may damp horizontal velocity immediately, interrupt attack/chase movement and play a brief shoulder/body flinch; after expiry the existing pursuit/wander goal resumes. The brain's ~0.4s sensing timer can continue independently, but its latest goal must not override the active reaction movement. Death must cancel the reaction. Both shambler and runner inherit the logic, with independently tunable severity.

**Owning files:** `COMBAT/Reactions/` if generic helper is justified; `ENTITIES/TYPES/Zombies/ZombieController.cs`, `ZombieSettings.cs`, `Configuration/ZombieMaster.tres`, runner master where distinct; `EQUIPMENT/Weapons/Shared/FirearmSettings.cs` and firearm masters; relevant READMEs. Use existing visual rig pivots or a simple event-driven pose, not a new procedural animation subsystem.

**Out of scope:** Melee strikes, outgoing zombie damage, ragdoll, permanent stun, new navigation/perception model, changing default gun damage.

**Preserve:** Zombie vision cone/LOS/awareness, normal wandering/chasing and acceleration outside reactions; death dispatch, cover, hit masks, rifle/MG cooldown, top-down aim and FPS firing. One shot only produces one impact for one collided actor.

**Performance / lifecycle:** Impact wakes the existing movement physics and/or one-shot reaction timer only while active; bounded timer expiry then normal movement/sleep. No every-frame scans, duplicated zombie decision timers or per-entity reaction loops at rest. Stop reaction on death, deactivate and unload.

**Pass:** Rifle and MG both cause a short recoverable interruption on living zombie and runner; no reaction on miss/solid cover/dead target; run/turn and stationary cases; fast MG bursts cannot lock one zombie forever; dead zombie still disappears once; horde of ~20 causes bounded work. **Codex / Work** integration; **Copilot** build/F6 visuals; **Regular Chat** balance review.

## CM2 — Reusable quick-melee shove (revised scope)

> **Partial progress (CM2-A, 2026-10-27):** Shared `MeleeSettings`, `MeleeAttack` and `QuickShoveMaster.tres` implement an actor-neutral, attack-only one-target contact query, cooldown/timing, wall filtering and horizontal/top-down vs pitch-sensitive FPS aim. Awaiting local validation. **CM2-B zombie knockback, CM2-C player inputs/contact dispatch and CM2-D poses remain planned.** No actual shove is available in-game yet. See `COMBAT/Melee/README.md`.



**Objective:** Dedicated Quick Melee button performs a short defensive push independent of currently equipped weapon. With a gun it looks like a gun-butt strike; with a bat/knife/sword later it is a held-weapon push, while left-click is reserved for that equipped weapon's main attack. Implement the player's quick shove in **both overhead and first-person views** against one zombie/runner, with an **actor-neutral attack interface** future NPCs can call.

**Dependencies:** CM0/CM1, player aiming/input, equipped-item reference (no inventory rewrite), existing zombie movement/reactions. Reuse CM0's hit/impact dispatch rather than a new damage system.

**Implementation scope:** Add the minimal shared `COMBAT/Melee` contact executor/profile and a dedicated input action (provisionally `V`). One short-lived, narrow invisible query, ~1.5m, max one nearest eligible target, wall/door/terrain occlusion and self exclusion. Top-down uses an XZ-facing arc plus bounded height tolerance; FPS uses the camera's yaw/pitch for a finite-thickness 3D strike that can miss vertically, no damage by default (optional 0–5 HP). Bounded windup/active/recovery, dedicated cooldown, strong but modest backwards physical push/stagger handled by the victim's existing movement code, never direct teleport. No bullet, ammo usage or firearm shot cooldown. Optional simple procedural shove cue on existing player/held-weapon rig, including weapon in hand; avoid requiring authored animation files. Different grips/held items can later swap pose cue without affecting contacts. Inputs remain separate: **Quick Melee = shove; left-click gun = fire; left-click melee weapon = primary attack in CM3+.**

**Owning files:** Small shared `COMBAT/Melee/` helper + profile/resource if necessary, **both** `PLAYER/Equipment/PlayerWeaponController.cs` and `DEBUG/TEST/FirstPerson/EQUIPMENT/FirstPersonWeapon.cs` (thin input/aim adapters), existing `ENTITIES/TYPES/Zombies/ZombieController.cs` and reaction helper for actual knockback, minimal item/pose adapter, applicable configurations/docs. Do not create a separate shove-only damage pipeline or persistent `Area3D`.

**Out of scope:** Bat/sword/knife primary attack, multi-target sweep, one-/two-hand grip or dual-wield, bear/zombie attacks, stamina, inventory expansion, fancy imported animation, polished FPS view-specific animations (unless later approved); **functional FPS input/aim/detection is required**.

**Preserve:** Firearm 1/2 switching and shooting, UI focus prevention, camera-independent world-space aim, existing bullet flinch and death, stable rig pivots, obstacle collisions and top-down primary input.

**Performance/lifecycle:** Work begins only on attack input; fixed upper bounds for candidates, contact ticks and one hit. Finish/cancel on cooldown completion, focus loss, equip swap, death or unload. No active melee node/polling between attacks.

**Pass / handoff:** With firearm equipped, V shoves one nearby zombie without ammo/HP change; misses, cover and out-of-range are safe. Left-click still shoots. A melee item can eventually invoke exactly the same Quick Melee action, while its left-click primary has a distinct profile. Knockback collides naturally, recovers chase, doesn't stun-lock and doesn't affect dead actors. Build + quick **top-down and FPS melee acceptance** locally: standing target, near-centre strike, deliberately high/low FPS aim miss, overhead attack with correct horizontal facing but modest height difference, intervening wall, different elevation/floor, input focus/weapon swap. **Regular Chat** for isolated setup/pose, **Regular Chat** may implement end-to-end in small source-reviewed edits with brief validation; use **Codex / Work** only if complex collision, movement integration or runtime debugging exceeds that scope, **Copilot** for local validation.

## CM3 — Baseball bat sweep (multi-target)

**Objective:** When a baseball bat is equipped, **left-click** performs its configured blunt primary attack hitting up to three unobstructed zombies, while **Quick Melee** still shoves with the held bat on its separate button. This proves the dual-action contract.

**Dependencies:** CM0, CM1, shared melee contact/window from CM2. Current firearm-only `WeaponSlots` needs a **narrow equipment capability/adapter extension** before it can accept a non-firearm scene; avoid converting the nine-slot **visual-only** HUD into an inventory here.

**Implementation scope:** Bat scene + editable **primary attack** profile + procedural placeholder bat mesh (if useful), hand/socket attachment, input for windup/active/recovery, bounded wedge/capsule sweep physics query, up to three **distinct** eligible targets per attack, per-target occlusion. Each receives damage via the existing receiver and optional blunt impact. One hit per zombie per swing; optionally moderate falloff for secondary targets. Expose reach, arc, damage, target cap, cooldown, windup/active/recovery and impact strength; default proposal above. One replaceable animation trigger/weapon socket contract for later imported bat swings.

**Owning files:** `COMBAT/Melee/`, `EQUIPMENT/Melee/BaseballBat_001/…` with `.tscn`, `Configuration/*.tres`, optional `C/` and `Artwork/README.md`, one shared equip/input seam in `EQUIPMENT/Shared/` and `PLAYER/Equipment/`; update docs. No alternate bat-only damage maths.

**Out of scope:** Item drops/looting, inventory slots 3–9, damage-over-time, environmental destructibles, heavy combos, full FPS weapon animation implementation.

**Preserve:** Both existing guns work as before; their scenes and hitscan are not reimplemented. Player can stop attacking on focus loss, weapon swap or menu input; camera-relative movement/aim still function.

**Performance / lifecycle:** Bounded collision query results, per-swing dedup set reused/cleared (no unbounded allocations), no world scan, zero processing between swings; cap contact checks and visible swing VFX. Targets already deleted are ignored safely.

**Pass:** One swing hits exactly 1/2/3 lined-up in-arc zombies and never more than the cap; each loses HP once; misses and behind-wall zombies take none; bat does not hit the holder; fast movement, camera yaw and tight clusters work; switching to gun restores normal firing. **Codex / Work** for equipment and animation hooks, **Copilot** for in-Godot test, **Regular Chat** for balance.

## CM4 — Knife and animation variants

**Objective:** One reusable melee execution supports knife left-click thrusts and future blade/sword profiles with different arc widths, target caps and animation cues, not a second damage system. Maintain Quick Melee for every held melee weapon.

**Dependencies:** CM3 shared equipment and hit query; existing procedural player rig, future imported rig compatibility.

**Implementation scope:** Knife_001 scene and its `.tres` melee profile, very short reach/narrow arc, **one target**, faster attack/cooldown, little/no push, different visual hit reaction/attack pose from bat. Keep gameplay hit windows independent of finished art. Introduce a minimal animation action identifier/animation hook (e.g., `Shove`, `BluntSwing`, `Stab`) that player model can implement now procedurally and later via imported AnimationPlayer/AnimationTree. Cancel/restore the arm/socket cleanly. Reuse one sender and receiver pipeline for all three melee forms.

**Owning files:** `EQUIPMENT/Melee/Knife_001/`, existing `COMBAT/Melee/`, existing player pose adapter and optional shared art hook; per-item `Configuration` + README. No new all-purpose animation framework.

**Out of scope:** Stealth takedowns, bleeding, dismemberment, assassination AI, extensive combos or NPC combat logic; actual wide-sweep sword item, one-/two-hand balance and dual-wield mechanics remain future separately scoped jobs.

**Preserve:** Bat multi-hit, gun shove, bullet impacts and all current equipment controls unaffected. No camera-dependent melee maths.

**Performance / lifecycle:** Same bounded attack-only queries, transient pose/timer while active, no animation callback at rest, cleanup on cancel/death/unequip.

**Pass:** Knife consistently strikes at close range and only one target; bat still hits up to three; distinct placeholder swing vs stab vs shove are legible; target reaction profiles differ; test weapon swaps/interruptions and FPS regression. **Codex / Work** implementation; **Regular Chat** pose and tuning; **Copilot** local test.

## CM5 — Zombies attack and hit-reactive player (later)

**Objective:** Living zombies can deliver telegraphed short melee hits to a damageable player using the **same actor-neutral contact/damage system**; the executor must accept AI-controlled callers without player input. Bears and other creatures may adopt that executor later.

**Dependencies:** **PH0 must be implemented and validated first** (player `IDamageReceiver` and HP/death). CM0 shared contract and at least a basic melee executor from CM2/CM3. PH2 stamina is **optional**, not a blocker; advanced hearing/navigation is optional.

**Implementation scope:** Existing zombie chase/stop radius triggers a bounded melee attempt with windup/contact/recovery/cooldown while in reach and facing the player. Reuse same impact/damage receiver contracts and world collision/cover checks. Target player health through PH0, optional brief player recoil without disrupting locomotion permanently. Apply target immunity/death gating, respect activated state and prevent horde attacks from every zombie at once via an optional **small concurrency budget** (only if playtesting demonstrates necessity). Weapon and zombie attack schedulers stay actor-owned; no global always-on melee manager.

**Owning files:** `ENTITIES/TYPES/Zombies/ZombieController.cs` and `ZombieSettings.cs`/masters, `COMBAT/Melee/` shared executor, `PLAYER/Stats/` PH0 receiver, player animation/hit feedback as appropriate. Update both relevant plans.

**Out of scope:** Infection, grapples, biting status effects, complex combos, stamina drain, crowd navigation, death ragdolls, robot attacks, long-range AI or boss behaviour.

**Preserve:** Zombie sight/cover/acquisition, player health mitigation and death latch, current combat impacts, camera/input, collision and single-action lifecycles.

**Performance / lifecycle:** Attempt one-shot timer/attack window only for zombies already pursuing and within reach; no full population scan/continuous contact checks while wandering or out of range. Cancel on death, deactivation, target invalidation, leaving range, reload and chunk unload; horde work bounded.

**Pass:** A zombie within reach telegraphs and strikes a living player, HP decreases through PH0 exactly once per attack; dodging/leaving reach or cover avoids hits; no damage while outside active window; multiple zombies have bounded actions; lethal hit stops player per PH0; save/respawn not required. **Codex / Work** for AI + player-health integration; **Copilot** build/F6 physics and combat testing; **Regular Chat** attack-timing review.

## 4. Order, simplifications and later decisions

**Recommended order:** CM0 → CM1 for immediately visible bullet stagger → CM2 gun shove → CM3 bat multi-hit → CM4 knife; implement **PH0** before CM5 (it can be done independently at any earlier time). Review each phase's feel in the main top-down world before expanding.

**Simplifications:** Do not add a global combat manager, fixed-tick collision checks for every idle weapon, a large ability/state-machine framework, a second resolver or an entire inventory just for a bat. One reusable short attack window and target interface should cover shove/bat/knife; weapon profiles supply the differences. Keep the existing two firearm slots until an authorized equipment-capability extension is necessary (CM3), and keep `UI/GameplayHUD/Hotbar` decorative until a separate hotbar/inventory phase.

**Proposed decisions the owner can adjust before coding:** quick-melee always available independently of held equipment (owner direction); default quick-melee key (`V` suggested); how far and forceful a shove should feel; whether a very high-damage rifle should flinch on every nonlethal hit; bat second/third-target damage falloff; stagger immunity of large mechanical enemies; and when FPS should receive equivalent view animations. No question blocks plan creation.

**Longer-term option:** When both player and zombies use damage broadly, moving the existing `ENTITIES/Shared/Combat` files into `COMBAT/Shared/Damage/` would make folder boundaries cleaner. It is optional, should be one separately authorized migration with Godot script resource and `.uid` verification, and does not improve the first melee prototype's behaviour by itself. Avoid splitting generic health calculations across two locations.

**Validation baseline for every implementation phase:** focused `dotnet build war-machine.csproj` if available; F6 `WORLD/World.tscn`; retained rifle/MG shots, damage and death checks; first-person regression when shared code changes; short tests of hit timing, wall occlusion, closely clustered enemies, target removal, focus loss and idle CPU. Keep validation proportional and leave visual/performance acceptance with the owner. **This plan itself has no build or runtime test to perform.**

**Work assignment:** Regular Chat can own this plan, documentation, tuning discussions and small isolated config reviews. Codex/ChatGPT Work should handle cross-file combat physics, target resolution, equipment/scene integration and animations. Copilot can run local C# and Godot debugging if asked. Re-read source before each phase, update this plan/owning READMEs and use one GitHub commit per authorized job. **Do not auto-implement later phases.**

## CM0 / CM1 implementation handoff

Implemented contracts, dispatcher, recipient reaction state, gun/zombie masters and docs only. No CM2+ or basement work. Damage/health files are unchanged. Actual values and the mandatory recovery-gap policy are in README; this supersedes the planning snapshot's statement that nonlethal hits cannot interrupt movement. Source and numeric checks only: local .NET build and both-view F6 acceptance pending. No permanent idle reaction callback; existing physics wakes on impact and settles normally. Future phases require separate authorization.
