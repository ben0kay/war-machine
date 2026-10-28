# Deer — D-AI1 wandering and fleeing

**Implemented in source; local Godot build and behaviour verification pending.** Deer still inherits `ENTITIES/Shared/Entity.tscn` and uses the same 120-HP `HealthComponent`, `IDamageReceiver`, collision and injured-only health bar as before. The world still spawns **one deer** near player spawn when `WorldMaster.DeerPreviewEnabled=true`.

## Basic behaviour

The world calls `deer.InitializeTarget(player, seed, terrainHeight)` after ground/clearance checks. The deer starts dormant until that explicit injection.

- **Wander** around an anchor in a 9m radius, walking at 1.2m/s and resting randomly 1.5–3.5s.
- **Detect** one registered player at up to 13m using a wide 300° cone, the existing shared `VisibilityResolver` for cover and player head/torso, and an unconditional close-range (4m) proximity alert. Decisions use one low-rate 0.4s one-shot timer.
- **Flee** away from the threat for approximately 18m at 7.5m/s. Up to 12 bounded candidate directions/shorter distances use the shared native-ground + capsule-clearance + chest-height obstruction check to reject obstacles. This is straight-line movement, not navigation around buildings.
- **Re-anchor** at the deer's **actual stopping position**, not its old spawn/home or unreachable target point. After a rest, wander around the new anchor. If the player remains close, flee again at the next decision.
- **Shot response:** any nonlethal damage also causes flight, even if the attacker was outside its normal sight range. Lethal shots still deactivate and remove the deer.

Four lightweight hip pivots now swing during active movement for a visible walk/gallop; there is no idle animation or hearing. Physics runs **only while traveling**, with a terrain height sampler used during motion because camera-centred Terrain3D physics coverage can be limited. No new AI manager or raycasts every render frame.

## Reused mechanics

- `EntityCore` activation/deactivation; `HealthComponent` and `IDamageReceiver` for combat.
- `VisibilityResolver` and `IPerceptionTarget` from zombie/player perception (no robot beam).
- New shared `ENTITIES/Shared/Movement/GroundGoalSampler.cs` extracted from zombie wandering: walkable ground, destination capsule clearance, a strict sweep for zombies or a relaxed obstruction ray for deer, and four bounded random choices. Zombie-specific chasing/investigation stays unchanged.
- Deer implements its **own** wander/flee behaviour and `MoveAndSlide`; the robot's waypoint patrol and zombie chasing are not appropriate for a fleeing animal.

## Config and tests

Edit `Configuration/DeerMaster.tres` for `WanderRadius`, `WanderSpeed`, idle waits, `DetectionRange`, `FleeDistance`, `FleeSpeed`, `VisionAngleDegrees`, `DecisionIntervalSeconds`, `SightBlockerMask`, movement acceleration and turning rate. `StartActive=false` prevents uninitialized AI. The default is now a muted brown coat with a slimmer body/legs and no antlers (optional small antlers remain). Health remains 120 HP.

After pulling, build C# and run `WORLD/World.tscn` (F6). Find the preview deer near spawn; watch wandering, approach it, chase it to a new spot, then watch wandering relocate. Shoot at range and watch it flee and show an injured health bar; a second default rifle hit kills it. Check terrain slopes, obstacles/doorways, FPS, deactivation and scene unload. **No Godot runtime or physics build has been run here.**

[AI_IMPLEMENTATION_PLAN.md](AI_IMPLEMENTATION_PLAN.md) records the later hunting/navigation phases.

## Deer visual polish and stationary-AI follow-up (2026-10-28)

User saw a long sausage-like reindeer model and a stationary deer after D-AI1. The model has a shorter, shallower torso, higher chest, slimmer tapered forward neck, smaller head/muzzle, finer legs/hooves, natural ears and a smaller tail. A more natural brown replaces the bright orange shade. Antlers now default OFF; enabling HasAntlers builds only small stag antlers. The four original leg-pivot names and capsule/120HP pipeline are unchanged.

The AI's shared CastMotion check was a likely cause of rejected walk/flee goals on uneven ground. The new deer-only relaxed goal mode still checks actual terrain support, destination capsule clearance and a chest-height obstacle ray, while real movement remains collision-aware using MoveAndSlide. Zombies retain their original strict full-body sweep. Deer additionally tests shorter nearby wander goals and more balanced escape directions. One-time [Deer] initialization and first-travel messages, plus a bounded blocked-goal warning, help identify an activated but stuck actor. Source-based fix only; actual in-game success is not yet confirmed.

After compiling, F6 World.tscn and watch for 10-15 seconds, walk close, chase it to a new anchor and try a shot from range. Check the Godot Output for [Deer] AI initialized, [Deer] First travel, or [Deer] blocked-goal warnings. Also recheck that zombies still wander normally and deer collision stays grounded.

## Farther world placement — 2026-10-28

WorldMaster now places this same AI prefab at player-relative X/Z `(-18,50)` (about 53m away; world X/Z 494,600), south of the village arrival area. Matching WorldSettings defaults and startup scatter exclusion follow that offset. This separates normal wander observation from immediate player encounters. The prior default was about 18m from spawn, already outside the 13m detection range; proximity is therefore not established as the stationary-deer cause. No AI/movement algorithm changed in this relocation. Local runtime diagnosis and movement verification remain pending; inspect the existing `[Deer]` initialization/first-travel/rejected-goal messages.
