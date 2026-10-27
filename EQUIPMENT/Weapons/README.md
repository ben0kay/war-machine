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
