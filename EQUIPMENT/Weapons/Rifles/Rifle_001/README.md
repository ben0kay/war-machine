# Rifle_001 — temporary usable low-poly rifle

**Implemented, awaiting local validation.** Always equipped in the active top-down
preview, selected by `EQUIPMENT/Configuration/EquipmentMaster.tres`.

- `Rifle_001.tscn`: shared FirearmController root, replaceable Visual and Muzzle.
- `C/RifleMeshBuilder.cs`: original temporary procedural rifle geometry only.
- `Configuration/RifleMaster.tres`: shared FirearmSettings resource, edited per rifle.
- `Artwork/`: eventual imported model/texture destination.

## Defaults and controls

| Setting | Value |
| --- | --- |
| Shot interval | 0.65 seconds (about 92 rounds/minute) |
| Damage | 95 physical |
| Range | 90 metres |
| ACC0 base spread | 0.30° maximum shot deviation (half-angle); 0° restores perfect accuracy |
| Trigger | Click once or hold left mouse for slow repeating fire |
| Hit mask | 5: world layer 1 + zombie layer 3; player layer 2 excluded |
| Feedback | Thin moving tracer (300m/s, 1.7m, 0.014m core), single pooled slot; 0.07s flash and optional sound |

Mouse aims toward a physics surface using the active camera in either projection.
Empty space falls back to a hand-height plane. The player faces the cursor while
walking/crouching, with holding pose/socket settings in TopDownTestMaster.tres.
Shots use the muzzle, not the camera, and check shoulder-to-barrel obstruction.
ACC0 now adds a small randomly sampled angular spread **only when firing**; the
crosshair/aim point does not wobble. The same RifleMaster applies in the inherited
FPS test. Stance and movement accuracy differences (ACC1) are not implemented.
Zombies have editable 100 health: two default-rifle hits remove one. Dead actors
stop their brain/movement and leave the scene; no ragdolls, drops or respawns.
Robot/scenery can block a shot but have no damage response added in this pass.

UI clicks don't shoot. Releasing the mouse, focus loss or hovering the tuning UI
cancels held fire. No ammo, reloads, inventory, switching or ballistic projectiles.
Existing camera/arrow movement/crouch/environment controls remain.

## Reuse / replacement

For another firearm, make another scene using the shared controller and settings,
copy its stats resource, supply its visual and Muzzle marker, then select that
scene in EquipmentMaster. No player or firing-code rewrite is required.
For imported art, replace only Visual, align the grip origin and muzzle facing -Z.
Weapon pose is player-rig configuration, not a rifle-specific animation driver.

## Performance / validation handoff

Unequipped weapons have no polling. Actor/camera/mouse changes queue an aim update
in physics; settled aim sleeps. Holding fire waits on the weapon cooldown timer.
A shot uses a shoulder obstruction ray and, when unobstructed, one hit ray.
Hit reactions use an interface, not world scans. One tracer slot plus a flash
are pre-created and reused; effects stop processing after expiry. Other firearms
can increase `TracerPoolSize` for overlapping shots without changing the rifle's
appearance or cost. Optional audio only plays per shot.

Source/resource paths, wiring and whitespace checked. Godot/.NET unavailable in
this environment, so **compilation and gameplay remain unverified**. Locally:

1. `dotnet build war-machine.csproj`, then open in your Godot .NET editor.
2. F6 `WORLD/World.tscn`; use existing preview terrain.
3. Check gun/arms standing, walking, crouched, turning and aiming uphill/downhill.
4. Click/hold: slow regular shots, correct muzzle/tracer and 2-hit zombie removal.
5. Verify terrain/cover blocks hits, another zombie intercepts the shot, robot is unharmed.
6. Click tuning sliders and alt-tab while firing: no unwanted/stuck shots.
7. Switch orthographic/perspective, zoom/pitch, move with a stationary cursor.
8. Disable EquipmentMaster or unload: no stuck subscriptions/audio/effects.

No terrain rebake or parked FPS scene changes are required.

## Procedural model polish — geometry only

The rifle Visual node now builds a more intentional compact carbine: reusable chamfered and tapered low-poly profiles on the receiver/stock, angled pistol grip, open trigger guard, stepped magazine, thin ejection/bolt details, inset handguard vent slots, upper accessory rail, protective front/rear sight ears, round barrel and short muzzle device. The overall grip-centred outline stays within the original weapon's approximate dimensions, with its `Muzzle` marker still fixed at `(0, 0.055, -0.81)` along local -Z. Top-down and first-person views share the same upgraded art; no FPS-only material variant, shader, textures or robot material import.

Primitive geometry is shared through `EQUIPMENT/Weapons/Shared/WeaponVisualGeometry.cs`. The builder creates the full model one time and stops processing; static meshes/basic existing-tone materials are reused. Controller, equipment slots, shot origin, recoil/hit/tracer/light/audio rules, balancing and FPS offsets are unchanged. **Local build/FPS silhouette checks required**, particularly sight alignment/hand placement in motion.

## Moving tracer pass

RifleMaster now uses `TracerSpeed=300` m/s, `TracerLength=1.7`m, `TracerWidth=0.014`m, `TracerEveryNShots=1`, `EffectDuration=0.35`s max and one pooled slot. A short unshaded bright streak plus subtle amber halo travels down the original hitscan path and clips at the hit point. No ballistic simulation or damage delay, and cooldown, spread, audio, lights, muzzle flash and hit detection remain unchanged. Build and visually test in Godot (not verified here).

## RC0 cumulative recoil configuration (2026-10-28)

The master now authors 2.0° pitch per accepted shot, 3.0° maximum, no horizontal step, 12°/s gameplay recovery after 0.10s. Visual parameters 0.09m backward kick, 5° pitch and 0.22s recovery are **not rendered yet**. The shared `FirearmRecoil` state is ready, but real aim climb/weapon animation is deferred to RC1/RC2; existing shot behavior, 95 damage and 0.30° base random spread are unchanged.

## RC2 FPS recoil (2026-10-28)

The original RC0 master values now drive real ballistic recoil in both top-down (RC1) and FPS (RC2) modes, including the 2.0° accepted-shot climb and 0.09m/5°/0.22s visual rifle kick. One shared `FirearmVisualRecoil` animates only the Visual child, leaving the physical muzzle/cover/hit pipeline unchanged. FPS does not force the camera pitch. Build/visual checks outstanding; older RC0-only notes above are historical.

## FPS camera recoil refinement (2026-10-28)

RifleMaster now has `FpsCameraRecoilFollowDegreesPerSecond=30.0` and `FpsVisualClimbMultiplier=0.55`. FPS accepted rifle shots briefly pitch the physical camera upward using its existing RC0 2°/shot accumulation, while the visual kick still animates independently. Mouse-look can counter it, and recoil recovers after a pause. This is FPS-only; the top-down RC1 camera remains unchanged. Local Godot compilation and feel check pending.

## FPS one-way rifle pitch (2026-10-28)

The rifle also kicks the actual FPS camera upward **2° for every accepted shot**; there is no downward camera recovery after releasing. `FpsMaxCameraRecoilBurstDegrees=3.0`, reset after a 0.45-second gap without accepted shots, so even held automatic rifle fire at 0.65-second intervals adds another 2° each time. Follow speed 30°/s; shared top-down RC1 limit remains 3°. Re-aim manually between shots. The short visual rifle kick still returns cosmetically. Test locally.
