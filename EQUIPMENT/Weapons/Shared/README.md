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
for **RC0 shared recoil state now implemented (awaiting local build validation)**;
RC1 top-down and RC2 FPS view-specific aim/visual kick are still planned. The existing `ShotSpread.Apply` continues to supply only
random dispersion, not accumulated aim climb.

## Ownership cleanup

FirearmController.cs, FirearmSettings.cs and FirearmShotEffects.cs live directly here; C/ is reserved for mesh builders, such as Rifle_001/C/RifleMeshBuilder.cs.

## Shared shape-only weapon art polish

`WeaponVisualGeometry.cs` provides one cached flat-shaded chamfered box profile, front-/rear-tapered profiles, one 12-sided round tube and reusable box mesh for gun model builders. The rifle and machine gun now reuse these primitives instead of constructing a unique box mesh for every detail. Existing colour-only materials remain; no new textures, shader files, material variants or FPS renderer were added. Visual nodes generate their meshes once on load and sleep. Firing mechanics and weapon marker positions remain in the original scenes. Inspect both models from the first-person camera and at overhead distances, check arm/hand alignment and shot muzzle effects. Compiler/editor/runtime verification remains for local Godot.

## Solid-model face winding correction

The rifle and machine gun both use `WeaponVisualGeometry.Profile` for the receiver, stock and other chamfered/tapered parts. Their triangles were originally submitted in a winding order that Godot backface-culls from **outside**, exposing the inner faces as if the gun were cut open in first-person view. The shared `Face` helper now emits vertices in **Godot's clockwise front-face order** while preserving its analytically correct outward-facing normals. This fixes both guns without touching their separate mesh builders, sizes, authored flat colours, scene mount transforms, Muzzle markers, collision or firing systems. Keep default backface culling; do not hide reversed winding with a two-sided material.

Minimal check: triangle orientation of the 48 side and 16 cap faces was verified by a numeric cross-product test; source patches were reviewed. **Godot render/compilation not tested locally.** Pull the branch and inspect rifle (1) / machine gun (2) from FPS beside nearby geometry and then in top-down; their outer upper receiver and stock panels should be solid, with no cutaway void.

## RC0 recoil integration (2026-10-28)

`FirearmRecoil.cs` stores cumulative pitch/yaw independently for each equipped firearm; `FirearmController` applies one accepted-shot impulse after firing, then sleeps its temporary recovery callback when settled. A `ShotAccepted` event and `RecoilOffsetDegrees` snapshot are available for later animation adapters. `TryFire(aimPoint, shoulderProbeOrigin, recoilScreenUp)` can use the camera's up axis to adjust the actual ballistic direction **before** ACC0 random spread, with the same obstruction/hitscan and tracer ray. The current two-argument call still keeps legacy aim, so RC0 itself does not cause visible recoil or gameplay aim climb; those arrive in RC1/RC2. `CancelFeedback`, swapping, unequipping and Q shove clear recoil; no stale state on re-equip. Values are per-gun `.tres` fields, not part of spread or impact stagger.

## RC1 top-down integration (2026-10-28)

The primary top-down `PlayerWeaponController` now uses the third-argument recoil-view-up overload with the current camera's `GlobalBasis.Y`, so the stored pitch/yaw actually changes bullet/tracer direction before the existing random spread. `FirearmController` still has the same authoritative muzzle/cover/hit tests. The `ShotAccepted` event drives the new player-owned `TopDownWeaponRecoil` animation, which affects only each gun's `Visual` child and a slight shoulder/arm pose, not the physical `Muzzle` marker or weapon root. The two-argument overload remains used by FPS until RC2. No NPC-specific branch or weapon-specific recoil code.

## RC2: one visual kick helper shared across camera modes (2026-10-28)

`FirearmVisualRecoil.cs` now owns the temporary model Visual-child pitch/back-translation and exact pose restoration in both views. RC1's `TopDownWeaponRecoil` remains a wrapper for the extra player shoulder/arm response; RC2 FPS calls the same visual helper directly. Weapon root and Muzzle are not animated, avoiding double displacement of the shared ballistic ray. FPS passes `FirstPersonCamera.GlobalBasis.Y` to `FirearmController.TryFire` for physically higher 3D bullet trajectories before ACC0 spread. The `ShotAccepted` event drives model feedback, and each adapter sleeps its visual process on settle. No camera shake was introduced. See [recoil implementation plan](../RECOIL_IMPLEMENTATION_PLAN.md); FPS and top-down Godot validation are pending.

## FPS-only camera-driven sustained recoil refinement (2026-10-28)

The shared ballistic `FirearmRecoil` still accumulates independently of `ShotSpread`. The FPS adapter now applies that state to the physical FPS camera and calls the **two-argument** `TryFire` (instead of also rotating the 3D shot ray), preventing double recoil; the top-down view keeps the third-argument recoil direction and its current camera behavior. `FirearmVisualRecoil` gained optional `SetSustainedRise` for the FPS gun model only. The model's real sibling Muzzle marker remains unchanged. Per-gun FPS climb smoothing and model-rise multipliers are in each `.tres`. Full handoff: [recoil plan](../RECOIL_IMPLEMENTATION_PLAN.md).

## FPS one-way camera recoil (2026-10-28)

`FirstPersonWeapon` now uses a shot-accepted FPS **per-burst** camera-rise budget and positive-only camera impulses. It **does not** drive camera rotation from the falling `FirearmRecoil.RecoilOffsetDegrees`; RC0's recoil recovery remains relevant to top-down and transient gun Visual animation only. The FPS camera keeps its new mouse-look pitch after each shot. The original two-argument FPS `TryFire` continues to avoid double ballistic recoil, while top-down RC1 continues using the three-argument camera-up overload. Settings `FpsMaxCameraRecoilBurstDegrees` and `FpsRecoilBurstResetSeconds` are FPS-only; top-down mastery cap is untouched.

## HE0 accepted-shot emission

Optional `FirearmSettings.ShotEmission` adds shared sound loudness and hearing radius. Current rifle/MG masters opt in; actual ShotCue remains the playback definition and may have no Stream. Existing world/FPS injection passes WorldAudio through WeaponSlots. An accepted shot routes once through `WorldAudio.SoundEvents` after starting cooldown; it publishes a muzzle-position snapshot and requests optional pooled playback. Rejected shots produce neither. Audio capping/muting never gates gameplay noise. Custom callers must supply their own world service in Equip; no global fallback/search exists. Custom masters without the profile remain playback-only. HE1 listeners/reactions and footstep emission are not implemented. See [shared sound API](../../../AUDIO/Shared/README.md).
