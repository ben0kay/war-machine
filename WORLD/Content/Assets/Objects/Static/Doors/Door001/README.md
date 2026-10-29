# Door001 — DOOR1 physical service door

**Joint-axis foundation fix implemented in source; awaiting local compilation/Jolt/contact/visual acceptance.** Initial `4a3fc01` and contact follow-up `1f867b9` did not produce a working door in the owner's playtest. The door was crooked and unpushable; this pass fixes the demonstrated source defect rather than increasing force. [DOOR1–DOOR3 roadmap](IMPLEMENTATION_PLAN.md).

## Diagnosed defect and correction

Godot's HingeJoint3D, including the Jolt backend, uses its **joint-local Z axis**. The old code incorrectly assumed +Y and left the hinge at identity. Its permitted axis was therefore horizontal (through the leaf normal). Gravity could rotate/sag the off-centre slab about the wrong axis, while a character's handle push generated vertical torque that the joint constrained. See the official [Jolt hinge implementation](https://github.com/godotengine/godot/blob/master/modules/jolt_physics/joints/jolt_hinge_joint_3d.cpp), which reads `Vector3::AXIS_Z` for both hinge axes.

The joint frame now starts at **X=-90°**, mapping its +Z to door-local +Y. The **leaf remains at its closed identity rotation**. HingeAnchor and Leaf are both explicitly bound and their NodeA/NodeB paths checked after attachment. The leaf origin and custom centre of mass are the slab centre; the joint anchor is at its left edge at the same height. No motor, rigid-body angular axis locks, artificial leaf rotation, animation/tween or per-door update loop. Jolt owns all constrained motion; symmetric ±110° limits avoid single-sided limit-sign ambiguity.

The original DOOR1 baseline used **9kg**, angular damping **2.2** and CharacterContactForce **38**. The owner's later contact-feel pass below replaces these values with a lighter, more responsive default. Angular damping explicitly replaces environmental damping so the authored number is predictable. The sleeping leaf is explicitly woken before a real impulse. A small authored **5mm BoxShape margin** fits the thin 75mm slab. Door geometry, pivot and hinge limits remain unchanged.

## Reusable prefab and geometry

Door001.tscn remains the canonical prefab. Its fixed root is the hinge X/Z at floor Y=0. Runtime construction:

- HingeAnchor: shape-less StaticBody3D, no collision layer/mask.
- Leaf: RigidBody3D at `(width/2, bottom+height/2, 0)`, layer/mask 1; DoorCollision matches Panel.
- HandleInside / HandleOutside and metal plates: unchanged leaf-local anchors for future DOOR2.
- HingeJoint3D: at `(0, bottom+height/2, 0)`, local Z oriented vertically, two-body binding and collision exclusion.

The convenience store still creates **one rear door only** under `Generated/Level_0/OutsideWalls/RearWall/RearServiceDoor001`, hinge X=-0.69m relative to the rear doorway centre. The front entrance and interior openings remain open. No store geometry or site grading change was necessary for the axis correction.

| Closed clearance from authored source | Distance |
| --- | --- |
| 1.52m solid opening minus 1.38m leaf | 70mm each side |
| Decorative jamb inner edge to leaf edge (trim is visual only) | 15mm each side |
| 2.90m header minus 2.78m leaf top | 120mm |
| 0.18m leaf bottom minus 0.12m physical store floor | 60mm (terrain-supported site has at least this gap) |

These are source calculations; the supplied runtime test checks actual overlap. A wide swing can naturally meet the existing wall before reaching the configured limit; the joint is a limit, not a promise of an unobstructed full arc. Root/ancestors must be static, upright and uniformly scaled. Do not move an active physical door by animating its parent.

## Actual character contact, fade and aim

PlayerMovement still uses its existing MoveAndSlide result only while moving. Its extracted CharacterContactPush adapter examines those contacts, accepts one eligible rigid leaf whose parent implements ICharacterContactPushable, checks that motion points into the surface and passes the **actual world contact point** to TryPushFromCharacter. This is the previous contact algorithm, shared with the physics test; no raycast, overlap scan or input action is added. The door applies the same bounded force×dt impulse at the centre-relative contact point. Handle-side contact yields hinge torque; a hinge-edge contact yields less. No handle grabbing, pull-to-close action or DOOR2 behaviour.

**Hold movement against the leaf and press F10** if it still fails locally. Existing contact paths/normals/points identify Leaf versus wall/jamb. An eligible contact adds `DOOR1`: accepted push count, last point, signed torque impulse around the corrected axis, yaw, upright dot, sleep state and resolved A/B paths. `pushes` should rise and torque should be nonzero near the handle. A wall-only contact means the player did not reach the slab; do not raise force to solve it. All diagnostic work is explicit F10 only; no idle logs or polling.

Panel/plates retain DynamicFadeVisualMeta and structure_camera_blocker=false. Rear-wall grouped opacity remains the presentation owner; no stale closed-door obstruction proxy is introduced. FadeAwareAim.Link still pairs DoorCollision to Panel: cursor targeting can skip a faded panel, while movement, NPCs and bullet rays retain the current real leaf collision. The shape-less anchor is not a target. No collision layer/mask was changed.

## Local acceptance / test status

Source/resource/path/axis/geometry/loop/whitespace checks passed. **No dotnet or Godot executable here: compilation and actual contacts/swing are NOT verified.** The headless actual-contact regression is provided at [DEBUG/Doors](../../../../../../../DEBUG/Doors/README.md); it covers both sides, yaw 0/45°, closed overlap, upright/anchor stability, swing/damping and AIM1/bullet compatibility.

Pull/build, then F6 World and FPS. Walk front entrance → rear service passage; confirm front remains open. Walk into the rear leaf near its handle from **inside and outside**. It should start square/closed, swing smoothly without tilt/detachment/jamming, slow and settle, retain solid bullet/zombie/player collision and respect limits. Repeat camera wall fade and AIM1 targeting. If it fails, return F10 output and the first physics/debugger error. No terrain rebake. DOOR2 and DOOR3 remain unimplemented.

## DOOR1.1 — responsive weight, velocity-sensitive contact and longer swing (2026-10-28)

**Owner feedback:** rear convenience-store door feels rigid, needs too much pushing and stops too soon; a sprint should swing it further than a gentle brush. This is a targeted tuning of the existing **shared** `Door001`, not a bespoke store controller or artificial animation.

| Tunable in `DoorMaster.tres` | Old | New default | Reason |
| --- | ---: | ---: | --- |
| `MassKg` | 9 | **7** | Less inertial resistance at the same contact force |
| `AngularDamping` | 2.2 | **0.65** | Preserve angular momentum after release; natural slow-down still occurs through Jolt |
| `CharacterContactForce` | 38 | **52** | Easier ordinary walking contact |
| `ContactSpeedReference` | implicit 5 | **4.0 m/s** | Speed producing ~1× contact scale |
| `ContactSpeedExponent` | linear | **1.65** | Gentle contacts transfer much less; fast collisions give more impulse |
| `MaxContactSpeedMultiplier` | 1.5 | **2.5** | Strong but bounded sprint contact response |

A single `MoveAndSlide` leaf contact still supplies movement direction, world-space contact point, speed and delta, with no new queries or per-frame door code. The actual per-contact impulse remains `Force × clamp(delta,0,0.05) × clamp((speed/reference)^exponent, 0, maxMultiplier)` applied at the true collision point; the near-hinge region produces little torque as before. For example, the default curve produces approximately **0.07× at 0.8m/s**, **1× at 4m/s**, and **2.5× at 7m/s**. Unlike before, there is **no 0.35× floor** that makes tiny nudges disproportionately strong. Angular damping and the physical hinge naturally govern continued motion once the player stops touching the door.

**Scope:** affects the *one existing rear convenience-store service door* and any future structure that instances the same master. No front door installation, handle pulling, locks, player movement, geometry, collision masks or hinge-axis edits. If custom copies later override the master, compare their exported values individually.

**Validation pending locally:** `dotnet build war-machine.csproj`; run `DEBUG/Doors/DoorPhysicsTest.tscn` for contact, hinge and deterministic speed-response checks; play in top-down/FPS and nudge, walk and sprint into the rear door from both sides. Release movement immediately after a firm contact: the leaf should keep swinging and gradually damp, not freeze; a light tap should barely move it. Confirm stable upright hinge, no jamb collision, limits, no accidental oscillation, no desync in FPS and no character shoving through the door. Runtime feel and performance have **not** been measured here.

## DOOR1.2 — lighter, freer swing after another owner playtest (2026-10-28)

**Source tuning implemented; awaiting local Jolt/playtest confirmation.** The owner reported that 7kg still felt too stiff and requested **5kg**. The same shared `DoorMaster.tres` now uses:

| Setting | Previous DOOR1.1 | New DOOR1.2 |
| --- | ---: | ---: |
| `MassKg` | 7 kg | **5 kg** |
| `AngularDamping` | 0.65 | **0.40** |
| `ContactSpeedReference` | 4.0 m/s | **3.5 m/s** |
| `CharacterContactForce` | 52 | 52 (unchanged) |
| `ContactSpeedExponent` | 1.65 | 1.65 (unchanged) |
| `MaxContactSpeedMultiplier` | 2.5 | 2.5 (unchanged) |

Decreasing mass reduces rotational inertia, so the same player contact accelerates the hinged door more quickly. **Lower mass alone does not guarantee longer coasting**; lower angular damping preserves more angular speed after release. The slightly lower reference speed raises ordinary walking contact impulse without adding a baseline kick to gentle nudges. With the existing curve, a 0.8m/s brush has ~0.087× contact scale, 3.5m/s produces 1× and 7m/s reaches the capped 2.5×. These are relative force multipliers, not opening angles.

The hinge, physics bodies, player-contact adapter, scene, handle nodes, collision, swing limits and physics-loop sleeping are otherwise unchanged. This shared preset is used by the store rear service door and any future door instance that uses this master. It does not create new hinged doors in the police station.

**Acceptance:** After building, approach the rear convenience-store leaf at low speed, normal speed and sprint from both sides. Compare opening acceleration and how far it continues to swing after contact ends. A light tap should produce only a small movement. Check it does not accelerate into the jamb, oscillate excessively, sag or clip. Test with the current Godot/Jolt backend; measured/runtime acceptance remains pending.
