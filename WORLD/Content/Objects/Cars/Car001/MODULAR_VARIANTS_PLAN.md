# CAR-MOD — reusable modular damaged-car variants (future work)

**Status: proposed only. DO NOT implement until requested.** Branch: `implement-td0-td1`. The current `Car001` is unchanged in geometry other than the closed default driver-door pose, its original front-left hinge remains usable via `DriverDoorAngleOverride` or `SetDoorAngle(float)`.

## Why one configurable prefab

The model already has a separate physical front-left door, while the remaining passenger door/rear quarter and four wheels are baked into separate generated nodes. A separate car scene for every combination (four doors × four wheels × hood, glazing, etc.) creates combinatorial authoring effort and more resources to maintain. One scene with distinct optional parts and fixed per-instance state is simpler, consistent and usually cheaper; Godot can share mesh resources between instances.

## Proposed layout

```text
Car001 (Node3D, original Car001Mesh, CameraOccluder)
  Generated
    Body / Interior / Roof / StaticCollision
    Doors
      FrontLeft    Pivot + mesh + optional collider (existing first example)
      FrontRight   Pivot + mesh + optional collider (must first extract from cabin)
      RearLeft     Pivot + mesh + optional collider (must first extract from quarter)
      RearRight    Pivot + mesh + optional collider (must first extract from quarter)
    Wheels
      FrontLeft    Mesh/Hub at existing wheel coordinate
      FrontRight
      RearLeft
      RearRight
    Windows
      Front / Rear / Side* optional pane meshes
    OptionalParts
      Hood / Trunk / Bumpers / Mirrors
```

Each door: enum `Closed, Ajar, Open, Missing` or a boolean presence plus continuous hinge angle. Each wheel: `Intact, Missing` initially; `Flat` or `Damaged` later. Each window: `Intact, Shattered, Missing` when art supports real holes. Use a typed per-instance `CarConditionSettings : Resource` with a few grouped exported controls, while `Car001Master.tres` keeps the common dimensions/material defaults. **Duplicate** the variant resource per instance before mutation or store authored values directly on the car root, so variants cannot leak between cars. Keep the existing `SetDoorAngle(float)` as a compatibility forwarding API for front-left.

## Generation and collision

1. Build common chassis and interior just once per instance; cache shared shape/mesh resources where practical.
2. Emit each **present** door/wheel/optional part under its own named node, with collider only where physically necessary. Missing parts should omit or disable the corresponding collision; they shouldn't create invisible blockers.
3. Set fixed open-door rotations at build time. If interactive later, animate only the affected pivot on input, then stop processing; for fixed wrecks there is **no animation or per-frame logic**.
4. For missing wheels, the result must read visually as removed (exposed axle/brake, wheel absent); consider a small static chassis tilt/sag if needed, but don't simulate wheel suspension.
5. Ensure current camera occlusion and world clearing bounds still account for door-open extents. Avoid individual collision for purely decorative glass and small hub details.

## Scope boundaries and checkpoints

- **Now implemented:** front-left door defaults closed, with an independent per-instance angle override and no shared Resource mutation.
- **Future CAR-MOD0:** extract other door panels/windows from fixed cabin into true unique door pivots and optional collider groups, without visual regressions.
- **Future CAR-MOD1:** independent door state/presence controls, wheels absent/present, authored small damaged state presets. No runtime wheel simulation.
- **Future CAR-MOD2:** optional shattered glass, hood/trunk and extra visual deterioration.
- **Future CAR-MOD3:** user-interactive opening/closing and loot/interior mechanics, only if authorized.

**Validation:** first test a completely closed car, then one open front-left door, then independent combinations once CAR-MOD phases are requested. Observe top-down and FPS, collision, backface/winding, static occlusion, and scene generation overhead. Do not make extra duplicated PackedScenes for variants unless a future model genuinely has distinct topology.
