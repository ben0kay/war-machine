# MachineGun_001 — low-poly light machine gun

**Status:** Authored, pending local Godot/.NET build and visual/gameplay validation. A new **independent weapon choice**, now wired into the shared test weapon slots.

- `MachineGun_001.tscn` reuses the same `FirearmController.cs` as Rifle_001.
- `Configuration/MachineGunMaster.tres` uses the common `FirearmSettings` with a faster fire rate, different spread, damage and range.
- `C/MachineGunMeshBuilder.cs` creates original, removable low-poly weapon art once, with heavier silhouette, ammunition box, barrel shroud, carrying handle and folded bipod. A future imported GLB can replace the visual without changing firing.
- `Artwork/` is reserved for replacements and source assets.
- **FPS framing:** `MachineGunMaster.tres` sets `FirstPersonOffset = (0.07, -0.18, -0.23)` and `FirstPersonScale = 0.78`, relative to the default FPS weapon offset. This lowers and reduces the bulky gun in the camera without altering its top-down model, damage or actual muzzle alignment. The rifle retains its original FPS presentation.

## Proposed initial balance, now authored

| Setting | MachineGun_001 | Existing Rifle_001 |
| --- | --- | --- |
| Shot interval | 0.10 s (600 RPM) | 0.65 s (~92 RPM) |
| Automatic | Yes | Yes |
| Damage | 35 physical | 95 physical |
| Range | 85 m | 90 m |
| Base spread | 0.75° | 0.30° |
| Tracer effect lifetime | 0.18 s | 0.07 s |
| Reusable tracer slots | 4 | 1 (default) |
| Flash lifetime | 0.05 s | 0.07 s (default) |
| Sound | Optional, unset | Optional, unset |

Shots share `ShotSpread`, the existing shoulder-to-muzzle obstruction check, hit/damage pipeline, automatic trigger timer and `FirearmShotEffects` pooled tracers. No ammunition count, belt-feed simulation, reload, weapon switching, new input bindings, overheating or special NPC handling has been added.

## How to test without implementing weapon switching

Edit `EQUIPMENT/Configuration/EquipmentMaster.tres` and set `DefaultWeaponScene` to `res://EQUIPMENT/Weapons/MachineGuns/MachineGun_001/MachineGun_001.tscn` in Godot. The default **remains Rifle_001** in source control, with SecondaryWeaponScene now selecting this machine gun. This configuration affects both top-down and the inherited FPS test using `EquipmentMaster`. The current input adapter already supports holding the trigger for automatic fire.

Press F6 in `WORLD/World.tscn`, check the heavier mesh/hand placement, muzzle/tracer start, sustained 600 RPM, damage, spread and nearby-cover blocking. F6 `DEBUG/TEST/FirstPerson/FirstPersonTest.tscn` for the FPS alternative.

**Important:** With multiple live tracers, high-rate shooting can leave several distinct short-lived lines in the world simultaneously. The muzzle flash remains one reused mesh, resetting on every shot. The optional shot audio remains **one** `AudioStreamPlayer3D`, so overlapping machine gun sound/loop polish is a separate future task. No runtime/render test claimed here.

Performance: `TracerPoolSize=4` pre-creates four tracer meshes at weapon initialization. Each shot only cycles an existing slot; no per-shot nodes, physics queries, materials or allocations from the VFX layer. `FirearmShotEffects._Process` runs only while a flash or at least one tracer is active, then sleeps. Stop/unequip hides every slot.

Test slots now implemented: press **1** for rifle, **2** for this machine gun in either view. No need to replace DefaultWeaponScene manually. Shared WeaponSlots handles switching; weapon mechanics and pooled effects are unchanged.

## Procedural model polish — geometry only

The existing light machine gun now uses chamfered/tapered receiver and stock forms, a more integrated cheek rest, open trigger guard, side feed-box with hinges/latch/reinforcing ribs, revised ejection/bolt cover, recessed cooling slots, real round heavy barrel/collar/brake, an open-air carry handle, raised sights and refined folded bipod. These details keep its heavy appearance without increasing the weapon's original envelope. Its `Muzzle` marker remains at `(0, 0.06, -1.20)` along local -Z. The authored FPS offset `(0.07, -0.18, -0.23)` and scale `0.78` are untouched.

Geometry shares `EQUIPMENT/Weapons/Shared/WeaponVisualGeometry.cs` with the rifle; colours remain their original simple palettes with **no first-person material/texture/shader pass**. One-time mesh construction, cached static forms, no animations/process loops, no new collision. Controller, rate of fire, accuracy, shot sound/light and muzzle flash stay unchanged. **Local Godot/.NET build and top-down/FPS visual checks pending**.
