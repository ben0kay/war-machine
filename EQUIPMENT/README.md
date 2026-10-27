# EQUIPMENT — reusable weapons and handheld tools

The first usable firearm is implemented and integrated into the active top-down scene.
**Awaiting local compilation and visual/physics validation.** Read root CodePreferences.md.

- `Configuration/EquipmentMaster.tres`: enable equipment and choose the default weapon scene.
- `Shared/C/EquipmentConfig.cs`: small composition resource, independent of the player.
- `Weapons/Shared/C/FirearmController.cs`: shared firing, cadence, hitscan, obstruction and damage dispatch.
- `Weapons/Shared/C/FirearmSettings.cs`: typed, reusable firearm stats resource.
- `Weapons/Shared/C/FirearmShotEffects.cs`: reused shot feedback, dormant after expiry.
- `Weapons/Rifles/Rifle_001/`: the first scene, editable stats and temporary C# visual.
- `PLAYER/Equipment/`: mouse/camera input adapter; actor pose remains in TopDownPlayer.
- `ENTITIES/Shared/Combat/IDamageReceiver.cs`: shared hit contract, currently supported by zombies.
- `Tools/`: reserved for later tools; no inventory, reload, ammo economy or switching yet.

`C/` contains procedural C# mesh generation and scripts. `Artwork/` is for later
imported art. Item `Configuration/` holds `.tres` definitions. Keep firing code
shared from the first weapon onward: do not create a rifle-specific controller or
copy stat classes for a pistol. Future projectile/melee/tool behaviour should be
added deliberately when requested; hitscan is not a universal projectile simulator.

Rifle is always equipped while the master is enabled. Mouse aims; left click or
hold fires slowly. Arrows, Shift, right Ctrl crouch and camera controls remain.
UI clicks are excluded; focus loss cancels held fire. Robot and parked FPS world
are unchanged. See the rifle README for controls, stats and local check steps.

Performance: input, actor/camera changes wake one physics aim update; no idle aim
loop. Held fire sleeps on a one-shot physics cooldown timer, then requests its next
shot. Queries exclude the holder. Feedback reuses two mesh instances and sleeps
after its brief lifetime. No projectile nodes, target scans or per-zombie hit polls.
Audio is optional via the stats resource; no sound asset is included by default.
