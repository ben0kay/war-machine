# Door001 — DOOR1 physical service door

**Joint-axis foundation fix implemented in source; awaiting local compilation/Jolt/contact/visual acceptance.** Initial `4a3fc01` and contact follow-up `1f867b9` did not produce a working door in the owner's playtest. The door was crooked and unpushable; this pass fixes the demonstrated source defect rather than increasing force. [DOOR1–DOOR3 roadmap](IMPLEMENTATION_PLAN.md).

## Diagnosed defect and correction

Godot's HingeJoint3D, including the Jolt backend, uses its **joint-local Z axis**. The old code incorrectly assumed +Y and left the hinge at identity. Its permitted axis was therefore horizontal (through the leaf normal). Gravity could rotate/sag the off-centre slab about the wrong axis, while a character's handle push generated vertical torque that the joint constrained. See the official [Jolt hinge implementation](https://github.com/godotengine/godot/blob/master/modules/jolt_physics/joints/jolt_hinge_joint_3d.cpp), which reads `Vector3::AXIS_Z` for both hinge axes.

The joint frame now starts at **X=-90°**, mapping its +Z to door-local +Y. The **leaf remains at its closed identity rotation**. HingeAnchor and Leaf are both explicitly bound and their NodeA/NodeB paths checked after attachment. The leaf origin and custom centre of mass are the slab centre; the joint anchor is at its left edge at the same height. No motor, rigid-body angular axis locks, artificial leaf rotation, animation/tween or per-door update loop. Jolt owns all constrained motion; symmetric ±110° limits avoid single-sided limit-sign ambiguity.

Mass remains **9kg**, angular damping **2.2** and CharacterContactForce **38**. Angular damping explicitly replaces environmental damping so the authored number is predictable. The sleeping leaf is explicitly woken before a real impulse. A small authored **5mm BoxShape margin** fits the thin 75mm slab. No speculative force increase or unrelated collision changes.

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
