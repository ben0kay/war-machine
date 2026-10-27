# Weapons

Shared/ holds the reusable hitscan controller, settings, shot spread and a
bounded reusable tracer pool.
Rifles/Rifle_001 is the starter firearm; item art and stats remain local.
[MachineGuns/MachineGun_001](MachineGuns/MachineGun_001/README.md) is a separately
authored 600 RPM light machine gun using the same controller and a four-slot
tracer pool. The rifle remains the configured starter gun; weapon switching is
**not** part of this change.
Other firearm scenes can reuse the same mechanics and typed Resource.
[ACCURACY_IMPLEMENTATION_PLAN.md](ACCURACY_IMPLEMENTATION_PLAN.md) tracks the shot-spread
foundation (ACC0) and proposed player-specific stance/movement accuracy (ACC1).
Projectile weapons, melee, ammo/reload and inventory remain future tasks.

## Interpolated shot presentation — awaiting local validation

Shared effects disable automatic interpolation of recycled world-space tracers and reset each reused transform. The flash follows the muzzle's interpolated position during the existing active render callback, including when cover makes the gameplay ray start at the shoulder. Damage/cover queries and fixed shot segments remain unchanged. No idle loop, allocations or extra physics queries. Applies to rifle, machine gun and FPS. Source/loop review only; compilation not verified (Godot/.NET unavailable). Build and check moving/turning automatic fire, cover, switching and FPS; feedback must expire/sleep and cancel on unequip.

## Shared light and rifle audio foundation

FirearmSettings.EmitMuzzleLight opts each weapon into a cached warm, shadowless real light driven by existing FirearmShotEffects lifetime; both current masters explicitly enable it. LightingMaster owns light colour/energy/range/duration and the active cap. ShotCue opts a firearm into the pooled world audio service supplied through WeaponSlots; RifleMaster uses AUDIO/OneShots/Weapons/RifleShot.tres with an intentionally empty Stream. Assign a WAV/OGG there to hear it. Machine gun has no sound assigned and needs its own cue. Legacy ShotSound only runs without ShotCue, preventing double playback. Accepted-shot damage/spread/cover/cadence remain unchanged. See AUDIO/README.md and ENVIRONMENT/Lighting/README.md for APIs, settings, limits and brief local checks. Compilation/visual/listening acceptance pending locally.
