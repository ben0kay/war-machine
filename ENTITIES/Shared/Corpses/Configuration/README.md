# Corpse configuration

CorpseMaster.tres is the shared ZD0 fall default, consumed by WorldController/CorpseManager. Edit its enabled flag, simplified proxy size/pivot/mass, 0.8–2.5s fall/stability defaults, damping, push/spin and travel limit. Runtime copies protect authored values; invalid settings disable handoff rather than starting uncontrolled physics.

This profile is species-independent; a future adapter may supply a validated per-spawn copy. Current initial consumers are zombie 001 and runner 002. No TTL, sinking, pile collision or slowdown setting is implemented before ZD1/ZD2. See the parent README for lifecycle and pending tests.
