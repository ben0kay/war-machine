# Contact pushing for physical hinges

`ICharacterContactPushable` is a tiny, optional physics contact adapter shared by future doors and cupboard hinges. `PlayerController` checks `MoveAndSlide()` contacts **only while moving** and invokes one matching hinged owner's method per physics step. The door applies a real contact-point impulse to its `RigidBody3D`; Godot's `HingeJoint3D` supplies rotation constraints. The contact is directional: walking *into* the leaf from either side pushes it, while grazing past or standing still does not.

This is **not** IX0 targeting, grabbing, inventory or universal prop physics. It does not register global entities, query/raycast the physics world, add a fixed update loop to doors, or override bullet/movement collision. Subsequent cupboard-door interactions may implement the same interface. Test with the rear service door in `ConvenienceStore001`, especially pushing near its handle edge. See `WORLD/Content/Assets/Objects/Static/Doors/Door001/README.md`.

## DOOR1 joint/contact diagnosis follow-up

The door hinge now explicitly aligns joint-local Z to vertical Y; the previous identity joint constrained handle-side torque about the wrong axis. CharacterContactPush.TryApply contains the former player loop unchanged, allowing DEBUG/Doors/DoorPhysicsTest to drive the same adapter using actual MoveAndSlide contacts. PlayerMovement invokes it from the original place after movement, before F10/visual notifications. No extra production raycasts, per-door process or broader prop support.

ICharacterContactPushable's optional default GetContactPushDiagnostics is read by F10 on a matching contact only. Door reports accepted pushes, signed hinge torque, axis/alignment/sleep and body paths. Mass/force values remain unchanged; do not infer runtime success from source completion. See the Door001 README and DOOR1–DOOR3 plan; build and real-world tests remain local.
