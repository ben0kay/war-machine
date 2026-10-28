# ELD-PERF1 — deferred anatomical hit detection

**Implemented in source; awaiting local compilation, correctness and performance acceptance (2026-10-28).** Scope is the shared anatomy owner, accepted firearm queries and the existing bat sweep. No AI/movement/vision/health balance or corpse changes. No new test scenes.

## Work shifted from movement to attacks

Default `DeferredHitDetection=true`: one native broad sphere per participating actor, plus cached joint references/local boxes (eight boxes for the current zombies). No per-limb native physics shapes, no pose-transform reads/writes by anatomy while walking, and no independently processing anatomy node. The sphere follows the character root through the existing scene transform hierarchy. It does not follow limbs or change with stance; its conservative envelope covers the authored healthy/extended-arm/low-crawl poses.

Only an attack query reaching a broad candidate reads current joint transforms and tests the existing local boxes mathematically. A region is identified **before** normal damage dispatch. Disabled/severed-region detection flags are updated on anatomy events, and severed boxes are skipped. Reset restores those flags. Death disables the broad area; exit removes its existing registry entry. Original clothing, injury states, severing and missing-limb corpse visuals are unchanged.

The broad sphere is not a damage target. Firearm shoulder-to-muzzle obstruction and ballistic segments both use AnatomyRaycaster. It refines each broad candidate, temporarily excludes that examined area, and keeps searching from the original origin. The nearest precise hit clips subsequent native rays, while real cover/body hits at or before it take priority. Original query exclusions/end/inside flags are restored in finally. Overlapping broad areas, an origin inside an envelope, a gap between limbs, or a severed limb's empty space do not become an automatic hit or stop the bullet. A mathematical miss continues to farther actors/world. Ordinary deer/robot/world hits retain their normal native collision path. Cursor AIM1, camera fading, Q shove and movement capsules are unchanged.

The bat still uses its bounded sphere broad query and angular/height/range active-sector filters. It now examines each candidate's virtual part centres and refines contact/cover rays using the same shared raycaster. Reused per-step candidate IDs prevent duplicate area-shape work; existing per-swing IDs still ensure one damage application per actor and the existing authored target cap. Removed regions cannot generate bat contacts. Existing aggressive FPS pose/backstep tuning is untouched.

## Bounds and budgets

ZombieAnatomyMaster.tres (shared by both variants) and the shared fallback AnatomyMaster.tres author:

- DeferredHitDetection=true.
- BroadRadius=1.6m, BroadCentre=(0,0.8,0), relative to the actor root. These cover the current biped poses including crawler arms and dragging legs. Future rigs/scales/longer limbs must author a conservative envelope; a region extending outside the broad volume cannot be discovered by this candidate path.
- Padding=0.005m remains on cached anatomical boxes. These are the same conservative art boxes, not triangle-accurate skin. Mathematical slabs do not reproduce engine-specific box collision margins exactly; inspect grazing boundaries in the comparison.

All candidates are gathered through native spatial queries, not a per-shot world/group scan. The existing registered capsule RID cache remains versioned at spawn/unload. Temporary exclusion storage is reused and copied only after the first deferred broad candidate. Native query result dictionaries still incur engine allocations; this pass does not claim allocation-free Godot raycasts.

Firearm segments permit at most 32 native queries each. A bat active step retains 64 broad results and at most **16 total native refinement/cover queries**, including nested broad-candidate checks. If a budget runs out, stop conservatively without applying an unchecked broad hit as damage; `budget_stops` records this. Dense saturated crowds may therefore omit a hit instead of bypassing cover. No unbounded recast, epsilon stepping through limbs or full-world per-shot search. The native broad areas are non-monitoring/non-monitorable as before, with unchanged actor collision layer and mask zero.

## In-world A/B comparison

After pulling, build `dotnet build war-machine.csproj`, then F6 `WORLD/World.tscn` and `WORLD/FirstPerson/FirstPersonWorld.tscn`.

Existing left panel: **Debug → Anatomy performance** contains Reset counters and Print counters. These buttons have no polling. Output `[ELD-PERF1]` reports registered deferred/legacy owners, configured native anatomy shape count, executed pose-shape writes, virtual box tests, native firearm/bat-refinement rays and conservative budget stops. Counters do not measure milliseconds, engine broadphase cost or FPS.

1. Default mode: reset, let the same crowd move without attacking, then print. Expected `pose_shape_writes=0`, `virtual_box_tests=0`. One configured anatomy sphere per registered actor. Existing character motion, animation, LOS and crawler-turn clearance queries still run normally.
2. Shoot/fire continuously, use the bat, then print: virtual tests/refinement rays should increase, while pose-shape writes remain zero. Check budget_stops; it should normally remain zero. Turn LogRegionHits=false in ZombieAnatomyMaster for timing comparisons so Output logging is not the benchmark.
3. Compare physical mode: change **ZombieAnatomyMaster.tres DeferredHitDetection=false**, restart the scene (not a live toggle). It recreates the original eight native boxes and enables pose synchronization, skipping severed boxes. Move the same healthy crowd, reset/print and compare profiler frame/physics time at the same camera location and with the same debug/render settings. Then restore true/restart. Both variants share this profile; fallback is only for actors without a species profile.
4. Measure no-fire crowd movement separately from sustained gunfire/bat use. Deferred mode removes continuous native shape work but spends extra work on attacks; it is not a promise that every scene or firing pattern is faster. Character movement/crowd contacts, rendering and sight checks remain possible bottlenecks.

## Correctness acceptance

In both views and on both variants: hit head/torso/each arm/leg while walking, reacting, hopping and crawling; inspect ELD logs and limb loss. Shoot an exposed arm outside the movement capsule. Fire through an empty gap/severed limb toward another zombie or a wall: hit the actual farther object or miss, never the broad envelope. Place a wall/closed door inside a zombie's broad envelope: real cover must block. Test overlapping zombies, muzzle/probe near cover and a ray starting inside a broad sphere. Check bat side-by-side multi-target hits, once-per-swing damage, no phantom limb hits, cover and unchanged Q. Finish a dismembered crawler and verify corpse handoff. Reload both worlds repeatedly and check stale RID/errors.

Source/loop/resource guards and independent segment-math/order model cases were run here (axis/parallel/inside/boundary/reverse segments, transformed/rotated/nonuniform-scale fractions, cover-before-limb, overlapping candidates and gaps). These are **not** compilation or native Godot runtime tests. Godot/.NET are unavailable here; actual broad-envelope coverage, physics ordering/shape indices, shooting/bat regression, memory and before/after FPS remain unverified. Owner performs the World/FPS acceptance; no extra test scene is required.
