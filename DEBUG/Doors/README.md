# DOOR1 actual-contact regression

**Provided, not executed here:** no Godot or dotnet executable in the implementation environment.

```sh
dotnet build war-machine.csproj
godot --headless --path . res://DEBUG/Doors/DoorPhysicsTest.tscn
```

Use Godot .NET with this project's Jolt backend. Expected `[DOOR1] PASS` / exit 0; a failed assertion or exception exits 1. The fixture matches the rear opening (1.52m), floor top (.12m), wall thickness (.23m) and header (2.90m). Four independent cases push from both sides with world yaw 0° and 45° and translated parents. A **real 0.3m-radius, 1.8m capsule CharacterBody3D** walks near the handle; actual MoveAndSlide contacts pass through the same production CharacterContactPush adapter. This does not substitute an arbitrary direct impulse for contact testing.

Checks: resolved hinge bodies, joint Z vertical, no rigid angular locks, closed upright rest/no static overlap, AIM1/dynamic fade metadata, bullet solidity while faded, cursor skipping while faded, detected Leaf contacts/accepted pushes/nonzero vertical torque, expected swing direction, fixed edge/no tilt, angular limits, damping and no per-door gameplay process. The isolated fixture does not prove the full store has no unexpected extra collider or that both cameras look correct.

After build, F6 **World.tscn and FirstPersonTest.tscn**. Enter through the front (still open), approach the rear door from both sides and walk near the handle. Verify smooth swinging without sag/jam; player/zombie/bullets still collide at the moving leaf. Repeat faded rear wall and mouse targeting checks. No interaction button/grabbing, DOOR2 or DOOR3 in this pass. No terrain rebake.

If it fails, hold movement against the door and press **F10**. Return the movement collider path/normal/point plus the DOOR1 line: pushes count, hinge axis, last hinge torque impulse, yaw, upright dot, sleeping and A/B paths. Expected A=HingeAnchor, B=Leaf, axis upward, up-dot near 1 and rising pushes/nonzero torque near handle. If only a jamb/wall appears, it is a contact/clearance issue, not evidence for increasing force. No scene scan, extra production ray or always-on logger was added.

## DOOR1.1 contact-feel regression (2026-10-28)

The regression now checks that the shared DoorMaster loads, a 0.8m/s nudge generates less than 20% of a 4m/s walking contact's scale, a 7m/s sprint transfers at least 1.5 times the walking scale, and no movement transfers zero impulse. It checks reduced angular damping before the existing real MoveAndSlide contact tests, which still validate both push directions, yaw angles, upright hinge, limits and settling. These curve assertions do not replace a real runtime feel test in the convenience store; run them locally.
