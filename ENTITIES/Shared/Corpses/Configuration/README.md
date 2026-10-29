# Corpse configuration

CorpseMaster.tres configures shared ZD0–ZD3: original fall proxy/mass/damping, pile speed 0.8x with 0.65x overlap floor, lifetime 300s from death, sink 8s/depth 2.2m, retained cap 96, physical-fall cap 8 and player-distance limit 45m. Manager caps remain global even with per-body profile overrides. Runtime copies/validation protect authoring. Temporary short TTL/small budgets are useful for local acceptance; restore production values afterward.

Both zombie types opt into pile slowdown. Player slowdown is separately off by default in PLAYER/Configuration/PlayerMaster.tres. See the parent README for collision layers, wake/sleep, cleanup and outstanding World/FPS checks. Species anatomy stays outside this resource.
