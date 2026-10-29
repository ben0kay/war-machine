# Physical doors — DOOR1–DOOR3

Work on `implement-td0-td1`; implement only the owner's current phase. This is separate from IX0–IX8 inventory/pickup work.

| Phase | Goal | State | Suggested executor |
| --- | --- | --- | --- |
| DOOR1 | Upright, constrained, bidirectionally pushable physical rear service door | Foundation fix implemented; awaiting local build/Jolt acceptance | Codex; owner/Copilot runtime validation |
| DOOR2 | Reach-validated hold/grab of either handle for physical push/pull/closing | Planned; not authorized | Codex |
| DOOR3 | Reuse handle interaction for cupboards/wardrobes and constrained sliding drawers | Planned; not authorized | Codex |

## DOOR1 foundation fix

Owner reported no swing from either side and a crooked leaf after commits 4a3fc01 / 1f867b9. Source audit confirmed the joint was configured around local Z while the code assumed Y. Correct the joint frame (not per-frame leaf rotation), preserve centred mass/edge pivot, validate both body paths and source opening clearances. Avoid angular locks/motors or unsupported Jolt soft-limit workarounds.

Implemented: local Z maps to vertical +Y via initial joint X=-90°; closed leaf identity retained. Explicit slab-centred COM, small configurable collision margin, authored damping mode and sleep wake-up. Existing mass/force/damping values remain. Production slide-contact dispatch is extracted into CharacterContactPush for actual-contact regression reuse. F10 reveals contact paths, accepted pushes, torque and alignment only on request. No new persistent callbacks or physics queries.

Preserve Door001 prefab/master/handle anchors, solid current leaf collision, wall fade metadata and AIM1 shape link. Store retains exactly one rear exterior door; main front entry and interior openings stay clear. No interaction key, pickup, inventory or loose-prop framework. Source geometry calculations are documented in README; no speculative opening/terrain changes made.

Acceptance: build, then the supplied DEBUG/Doors/DoorPhysicsTest headless Jolt test and brief World/FPS walk-through. Test actual MoveAndSlide Leaf contacts from handle side inside/outside, upright closed rest, no initial overlap, smooth swing, fixed edge, limits and damping. Confirm solid actor/bullet collision and faded cursor targeting. Source checks alone are not runtime acceptance. No local Godot/.NET available; delivery SHA is in job response.

## DOOR2 — physical handle push/pull (future)

Depends on accepted DOOR1 and a narrow shared reach/obstruction/input arbitration contract. Preserve HandleInside/HandleOutside as passive anchors now. Later, aim at a reachable handle, hold an interaction action, physically drive the constrained leaf toward the hand target, and allow both opening and pulling shut. Share mechanics across FPS and top-down with view-specific input adapters. Validate walls, reach loss, release, menu/death/scene cleanup and multiple targets. Never teleport the leaf or add idle handle raycasts per door. No implementation authorized in this job.

## DOOR3 — cupboards, wardrobes and drawers (future)

Depends on DOOR2's reusable grabbing/physical control rather than a second interaction system. Reuse hinge/handle control for cabinet/wardrobe leaves; use sliding constraints for drawers. Keep prefab geometry/configuration independent of selection/input and constrain travel to avoid detachment. Validate actor clearance, blockers, release and dormant performance. Do not implement cupboards, drawers, inventory or general physics pickups under DOOR1.

## DOOR1.1 — physics-feel tuning (2026-10-28)

**Authorized owner follow-up; source implemented, awaiting local Jolt validation.** Shared `Door001` defaults now use MassKg 7, AngularDamping 0.65 and CharacterContactForce 52, plus authored `ContactSpeedReference=4.0m/s`, `ContactSpeedExponent=1.65` and `MaxContactSpeedMultiplier=2.5`. Actual motion remains a bounded force×delta impulse delivered only by the existing contact dispatcher. The nonlinear speed curve makes soft nudges weak, walks effective and sprints materially stronger, while reduced angular damping preserves motion after release. No per-door update or joint motor; no new Door002/handle input/furniture scope. Extend the headless door regression with deterministic nudge/walk/sprint scaling assertions; local Godot .NET compile, actual contact momentum and both view playtests remain outstanding.
