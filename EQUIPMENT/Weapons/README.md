# Weapons

Weapon scenes and individual weapon types live here. Start with [Rifles/Rifle_001](Rifles/Rifle_001/README.md). A weapon should be an instantiable item rather than code hardcoded into `TopDownPlayer.cs`.

[Shared](Shared/README.md) is reserved for common firearm behaviour when duplication justifies it. Future `Pistols/`, `Shotguns/` and `Melee/` folders can be created **when those items are requested**, not now.

Each concrete weapon uses `C/` for **Godot C# procedural meshes and runtime code**, `Artwork/` for later model imports, `Configuration/` for item master settings, and a root `.tscn` scene once implemented. No weapons currently exist.
