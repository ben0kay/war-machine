# Shared firearms

`C/FirearmController.cs` can be the root script of any firearm scene. Supply a
`FirearmSettings` resource and exported `Muzzle` Marker3D, facing local -Z.
The item's visual child can be procedural C# or an imported scene. Neither the
controller nor the player input adapter knows which firearm is equipped.

An actor calls `Equip(CollisionObject3D)` after the firearm enters the tree.
Call `TryFire(aimPoint, shoulderProbeOrigin)` **from physics**. It enforces its
cooldown, applies the configured per-shot **BaseSpreadDegrees** half-angle to the
muzzle aim (ACC0), checks the shoulder-to-muzzle segment for cover, casts to
the range limit, and sends a typed DamagePacket to the nearest ancestor
implementing `IDamageReceiver`. Set spread to 0 for previous perfect accuracy.
The target health component/resolver owns armour, resistances and health loss.
Non-damageable collision still blocks shots. `ReadyToFire` lets an adapter request
another held-trigger shot without polling. `Unequip()` shuts down cooldown,
feedback and audio. Timing/resources are per instance, not mutable shared masters.

**Pooled tracers:** `FirearmShotEffects.cs` pre-creates `TracerPoolSize` (1–16) separate
tracer meshes and one reused muzzle flash per equipped weapon. Each shot rotates to
the next slot, preserving existing streaks until their individual `EffectDuration`
expires. Exhausting the pool recycles the next slot rather than allocating a new
mesh. The flash has its own `FlashDuration` and restarts per shot. Only the
tracers/flash that are alive are visible; once all end, the effects node disables
`_Process()`. `Stop()` hides all active slots on unequip or focus loss.
The rifle defaults to **1 tracer**, while MachineGun_001 uses **4**.
No per-shot nodes, new raycasts, physics loops, materials or permanent VFX loop.

`ShotSound` is an optional AudioStream resource. The default rifle and machine
gun have no audio asset. Audio still uses one reusable player per firearm, so
overlapping rapid-fire audio/loops would be a separate polish pass.
Stats, effect sizes/duration/color and hit mask are in each item's `.tres`.
`ShotSpread.cs` uses two random samples only when a shot passes cooldown and equip checks;
there are no extra raycasts or callbacks. It samples a disk-shaped angular cone
uniformly in area and correctly handles near-vertical aims. A longer shot has
more world-space deviation from the target centre. The current top-down and FPS
adapters automatically share this firearm logic.

See [../ACCURACY_IMPLEMENTATION_PLAN.md](../ACCURACY_IMPLEMENTATION_PLAN.md)
for planned ACC1 player crouch/movement multipliers and optional ACC2 polish.
See [../RECOIL_IMPLEMENTATION_PLAN.md](../RECOIL_IMPLEMENTATION_PLAN.md)
for the **separate, not-yet-implemented** RC0/RC1/RC2 cumulative gameplay recoil
and visual weapon kick. The existing `ShotSpread.Apply` continues to supply only
random dispersion, not accumulated aim climb.

## Ownership cleanup

FirearmController.cs, FirearmSettings.cs and FirearmShotEffects.cs live directly here; C/ is reserved for mesh builders, such as Rifle_001/C/RifleMeshBuilder.cs.

## Shared shape-only weapon art polish

`WeaponVisualGeometry.cs` provides one cached flat-shaded chamfered box profile, front-/rear-tapered profiles, one 12-sided round tube and reusable box mesh for gun model builders. The rifle and machine gun now reuse these primitives instead of constructing a unique box mesh for every detail. Existing colour-only materials remain; no new textures, shader files, material variants or FPS renderer were added. Visual nodes generate their meshes once on load and sleep. Firing mechanics and weapon marker positions remain in the original scenes. Inspect both models from the first-person camera and at overhead distances, check arm/hand alignment and shot muzzle effects. Compiler/editor/runtime verification remains for local Godot.

## Solid-model face winding correction

The rifle and machine gun both use `WeaponVisualGeometry.Profile` for the receiver, stock and other chamfered/tapered parts. Their triangles were originally submitted in a winding order that Godot backface-culls from **outside**, exposing the inner faces as if the gun were cut open in first-person view. The shared `Face` helper now emits vertices in **Godot's clockwise front-face order** while preserving its analytically correct outward-facing normals. This fixes both guns without touching their separate mesh builders, sizes, authored flat colours, scene mount transforms, Muzzle markers, collision or firing systems. Keep default backface culling; do not hide reversed winding with a two-sided material.

Minimal check: triangle orientation of the 48 side and 16 cap faces was verified by a numeric cross-product test; source patches were reviewed. **Godot render/compilation not tested locally.** Pull the branch and inspect rifle (1) / machine gun (2) from FPS beside nearby geometry and then in top-down; their outer upper receiver and stock panels should be solid, with no cutaway void.
