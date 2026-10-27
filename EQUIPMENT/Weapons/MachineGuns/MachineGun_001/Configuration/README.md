# MachineGun_001 configuration

Edit `MachineGunMaster.tres` for the machine gun's shot interval, damage, range, base spread, effect length, pooled tracer count, flash duration, colour, hit mask and optional sound. Uses `EQUIPMENT/Weapons/Shared/FirearmSettings.cs`; all values are copied into runtime settings on equip.

`TracerPoolSize` is intentionally capped at 1–16 shared tracer mesh slots. Changing the master affects new gun instances after reloading, not an already-equipped instance's live settings.
