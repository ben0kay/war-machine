# Shared melee equipment adapter

MeleeWeaponController duplicates its MeleeSettings once, retains a reusable MeleeAttack, and dispatches confirmed contacts through HitTargetResolver. It exposes Equip, TryStart, Advance and Cancel. Current cosmetic pose is the baseball-bat coil/side sweep/follow-through; future thrust/knife variants require separately authorized presentation work, not another damage system. View-specific offsets are authored in the melee master. No idle callbacks.
