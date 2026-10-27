# Shared firearms

`C/FirearmController.cs` can be the root script of any firearm scene. Supply a
`FirearmSettings` resource and exported `Muzzle` Marker3D, facing local -Z.
The item's visual child can be procedural C# or an imported scene. Neither the
controller nor the player input adapter knows which firearm is equipped.

An actor calls `Equip(CollisionObject3D)` after the firearm enters the tree.
Call `TryFire(aimPoint, shoulderProbeOrigin)` **from physics**. It enforces its
cooldown, checks the shoulder-to-muzzle segment for cover, casts to the range
limit, and forwards damage to the nearest ancestor implementing `IDamageReceiver`.
Non-damageable collision still blocks shots. `ReadyToFire` lets an adapter request
another held-trigger shot without polling. `Unequip()` shuts down cooldown,
feedback and audio. Timing/resources are per instance, not mutable shared masters.

One reused flash/tracer pair per weapon; no per-shot nodes or permanent loop.
`ShotSound` is an optional AudioStream resource. Default rifle has no audio asset.
Stats, effect sizes/duration/color and hit mask are in each item's `.tres`.
