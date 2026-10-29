# Corpse configuration

CorpseMaster.tres configures shared ZD0–ZD4: original fall proxy/mass/damping, pile speed 0.8x with 0.65x overlap floor, lifetime 300s from death, sink 8s/depth 2.2m, retained cap 96, physical-fall cap 8 and player-distance limit 45m. Manager caps remain global even with per-body profile overrides. Runtime copies/validation protect authoring. Temporary short TTL/small budgets are useful for local acceptance; restore production values afterward.

Both zombie types opt into pile slowdown. Player slowdown is separately off by default in PLAYER/Configuration/PlayerMaster.tres. See the parent README for collision layers, wake/sleep, cleanup and outstanding World/FPS checks. Species anatomy stays outside this resource.


ArticulatedEnabled=true selects the procedural adapter's connected-body death within existing distance/count limits; false restores the one-box fall. Articulated damping uses RagdollLinearDamping=0.25 and RagdollAngularDamping=0.45; legacy proxy damping stays separate. Shared settling defaults are now MinimumFallSeconds=1.4, StableSeconds=0.6 and MaximumFallSeconds=4.0 (validation permits up to 8s). Settling preserves the final physical limb pose and retains static compound pile shapes. Rig proportions, mass weights and anatomical joint limits belong to ZombieCorpsePose, not this shared resource. These defaults require local visual/performance acceptance.
