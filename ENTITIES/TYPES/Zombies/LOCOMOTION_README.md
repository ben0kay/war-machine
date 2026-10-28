> **Startup resource correction (2026-10-28):** Godot rejected leading-dot floats in the locomotion masters (`.65`, `.45`, `.75`). They now use `0.65`, `0.45`, `0.75` without changing values. This fixes the first resource parse error that prevented ZombieMaster/runner settings loading and caused subsequent EntitySettings casts/null references. Unconfigured/inactive zombies also disable their physics callback immediately. Source/token checks passed; local Godot resource loading and spawn acceptance remain pending. Pull, rebuild, restart the World/FPS scene and clear old error history before retesting.

# ELD2 — living injury locomotion

**Implemented in source, awaiting local compilation/physics/visual acceptance (2026-10-28).** Both standard and runner use the same ZombieLocomotion helper and existing ZombieController AI. No CM5 attacks, ELD3 attack filtering, detached debris or ZD1–ZD3 work is included. The master remains [ANATOMY_COMBAT_CORPSE_PLAN.md](ANATOMY_COMBAT_CORPSE_PLAN.md).

## Behaviour and ownership

| Available limbs | Mode |
| --- | --- |
| Both legs functional | Original walk/run |
| Both legs usable but either damaged | Limp |
| One leg disabled or severed | Supported one-leg shuffle/hop |
| Both legs disabled/severed, at least one usable arm | Crawl |
| Both legs disabled/severed and no usable arms | Immobilized |

Damaged arms remain usable; disabled/severed arms do not propel a crawler. One-arm crawling runs at 55% of the two-arm crawl speed. Arm loss alone does not prevent healthy-legged walking. Immobilized zombies remain alive, can perceive, and can be physically shoved/fall under gravity; they do not propel themselves. Disabled visible limbs stop their normal animated function. Head/torso damage does not select a movement mode or bypass overall health.

ZombieController still owns destinations, pursuit, PER2A alerted LOS, last-seen search, wander, reaction and MoveAndSlide. ZombieLocomotion caches the injury snapshot on state changes, chooses speed/capabilities, overlays the existing variant poses and handles low-body clearance. It does not own a second AI/physics loop. Runner and standard healthy motion remain on their original virtual Pose/speed hooks.

Regular and runner masters select separate LocomotionProfile resources. Default limp multiplier=0.60, hop multiplier=0.35; hop caps regular 0.65m/s, runner 1.4m/s. Crawl caps regular 0.45m/s, runner 0.75m/s; capped by the current ordinary speed and reduced for one-arm crawling. Injured acceleration/gait reduce using the existing callback. HP, weapon damage, armour, sever thresholds and current healthy speed/sight tuning are unchanged.

## Collider, pose and sensing

Crawlers stay CharacterBody3D. A cached horizontal capsule replaces the standing one: radius 0.30m, length 1.85m, centre 0.34m above feet. The footprint covers the forward head/torso and dragging disabled legs; it is a proxy, not per-limb physics. The procedural body tips forward around its existing yaw, with the head raised and usable arms pulling. The one-leg mode is a supported shuffle with a small cosmetic hop, not repeated physical jumps. No new rig, raycast or articulated ragdoll.

Before changing shape, one overlap query checks floor/world/actors with the existing movement mask, excluding self and ignoring areas. If the larger footprint cannot fit, retain the current collider/pose, stop self-propulsion, and retry on the existing staggered AI timer. **A blocked transition may temporarily leave a legless zombie upright** until space clears or it is shoved clear; it never grows a collider through a wall or teleports out. Standing reset/reuse also requires clearance. Long-body turns are limited to 0.14 radians per update and check midpoint/endpoint; blocked turns retain their last safe heading. This is bounded sampled rotational clearance, not exact continuous rotational CCD. Native MoveAndSlide remains responsible for movement/slopes/contacts.

Ground recovery and wander queries switch to the actual body shape. Crawl sight rays originate at the lowered head, and acquisition facing uses yaw rather than the pitched art's vertical forward axis. Existing alerted-target retention still requires real LOS. The debug fan stays horizontal at the actual sight origin and is not transferred into corpse art. Anatomical hit shapes sync to the final living pose as before.

The existing corpse boundary gains an optional species-independent alignPivotToVisual flag. Living low bodies use it to position ZD0's one-body proxy along the lying visual's up axis rather than lifting the corpse upright above the floor. Standing deaths keep the exact default path. Original visuals, missing limbs and final pose snapshot are still transferred; no new corpse framework/lifetime logic.

## In-world debug testing

No test scenes were added. In either World or FPS, open the existing left panel **Debug → Zombie injury tests**. FPS: Escape releases the mouse first.

1. Approach a zombie within 12m and press **Select nearest zombie**. The selection remains that zombie while you move away.
2. **Damage left leg** demonstrates limp. **Sever left leg** demonstrates shuffle. **Sever both legs** demonstrates crawl. **Disable both arms** after leg loss demonstrates immobilization. State changes are monotonic; select another zombie/restart for a fresh test.
3. These explicit debug-build buttons change anatomy only, leaving HP and normal weapon balance untouched. They are absent in release builds; ordinary hits still use the real damage pipeline. They use the existing debug registration list only on clicks, no periodic lookup. Logs show `[ELD2] mode=...`, successful low_body and a one-time clearance-wait message when blocked.

Build `dotnet build war-machine.csproj`, then F6 `WORLD/World.tscn` and `WORLD/FirstPerson/FirstPersonWorld.tscn`. Test regular/runner pursuit and loss of sight; injured acceleration/poses; crawler turns beside walls/doors/other actors; floor/slopes and low obstructions; pending transition and Q shove clearance; one-arm crawling; immobilized idle and gravity. Shoot the moving/low body to check ELD region hits and absence of standing phantom blockers. Kill a crawler and confirm ZD0 fall/settle preserves missing limbs without lifting the visual upright. Test repeated world reload and healthy zombies, guns, bat and player regression. The bat remains a horizontal strike: a standing chest-height swing can miss a low crawler; use lower stance or firearm aim as appropriate.

## Performance and checks

Activation: anatomy transitions wake the existing physics callback once. Mode derivation is event-driven; no per-limb/state polling. Pose updates piggyback on current movement/reaction work. A low-body change needs one overlap query; retries use existing low-rate AI decisions. Turning a low collider costs at most two overlap queries per changed heading, only while moving or investigating. Normal standing actors have no new clearance queries. Immobilized/blocked grounded actors sleep physics after reactions settle; existing low-rate sensing remains. Debug actions are click-only and reuse the existing population registry.

Source/resource/whitespace and decision/geometry checks only here. Godot/.NET are unavailable: compilation, actual horizontal-capsule transition/turn contacts, gait appearance, low corpse settle and crowd profiling remain **unverified**. No measured performance claim. Runtime acceptance stays with the owner before moving to CM5/ELD3.
