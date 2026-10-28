# MachineGun_001 configuration

Edit `MachineGunMaster.tres` for the machine gun's shot interval, damage, range, base spread, effect length, pooled tracer count, flash duration, colour, hit mask and optional sound. Uses `EQUIPMENT/Weapons/Shared/FirearmSettings.cs`; all values are copied into runtime settings on equip.

`TracerPoolSize` is intentionally capped at 1–16 shared tracer mesh slots. Changing the master affects new gun instances after reloading, not an already-equipped instance's live settings.

## Damage and independent impact settings (2026-10-28)

`Damage = 25.0`, `BulletImpactStrength = 0.35`, `BulletStaggerSeconds = 0.08` are separate from rifle values. They are fed by the existing generic firearm impact dispatch; the zombie's CM1 reaction gates, per-target resistance and required recovery gap still apply. The shared bullet movement slowdown now scales with effective impact strength, so the MG's low-strength flinch is much less disruptive than the rifle. No extra per-shot queries or idle processing.
