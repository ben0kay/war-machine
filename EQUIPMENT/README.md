# EQUIPMENT — Weapons and handheld tools

**Status: folder/documentation scaffold only. No rifle, projectile, shooting, inventory or equipment system has been implemented.** This is intended as an independent, reusable asset/behaviour home for the fully 3D top-down War Machine game.

## Folder conventions (important)

- **`C/`** is where we write **Godot C# procedural mesh generation** for a weapon/tool. It can also hold that item's C# behaviour/controller and configuration class. Example: `RifleMeshBuilder.cs` constructs the first temporary low-poly rifle *using Godot mesh primitives from C#*. This is intentional and not an exported art asset.
- **`Artwork/`** is reserved for **future authored/imported models**, such as a `.glb`/`.gltf` rifle and associated texture/material files. Replacing the procedural C# art with a model should not require rewriting weapon mechanics.
- **`Configuration/`** holds the **per-item editable master resource** (e.g. `RifleMaster.tres`). Keep the associated typed C# settings class under the item's `C/` folder to follow the C# convention. Duplicate configuration at runtime before changing it.
- **`.tscn`** lives at the individual item root (e.g. `Weapons/Rifles/Rifle_001/Rifle_001.tscn`) once the item is actually implemented.
- **`README.md`** describes what a folder owns, intended files, and eventual scene integration.
- Don't create C# runtime systems, blank `.tres` files or a nonfunctional scene merely to fill a directory. Create real source/resources as Codex implements the first rifle.

## Current structure

```text
EQUIPMENT/
├── README.md
├── Shared/README.md                     # Shared equipment interface/equip logic when needed
├── Weapons/
│   ├── README.md
│   ├── Shared/README.md                 # Reusable firearm mechanics when a second weapon needs them
│   └── Rifles/
│       ├── README.md
│       └── Rifle_001/
│           ├── README.md
│           ├── C/README.md             # Future C# procedural model + rifle behaviour
│           ├── Configuration/README.md # Future RifleMaster.tres
│           └── Artwork/README.md       # Future imported model/textures
└── Tools/README.md                      # Future axes, pickaxes, repair/build tools
```

Further categories (e.g. `Weapons/Pistols/`, `Weapons/Shotguns/`, `Weapons/Melee/` and concrete `Tools/Axe_001/`) are **future possible paths**, not empty folders to precreate.

## Ownership and integration

- **EQUIPMENT** owns the weapon/tool scene, visual construction, individual settings and firing/using behaviour.
- **PLAYER** / the current `WORLD/TopDownWorldTest/C/TopDownPlayer.cs` owns movement, pose/aim input and a proposed **hand/weapon attachment point**; it must not embed the complete rifle implementation.
- **WORLD/TopDownWorldTest** only instances/connects a test weapon when explicitly authorized. The parked first-person world should remain untouched.
- **ENTITIES** owns robots/zombies/other actors, not every object they might equip. Reusable equipment should be able to attach to different actors later without duplicating classes.

**Next Codex step if authorized:** implement a *single temporary low-poly Rifle_001* using C# procedural mesh generation in `C/`, held via a player `WeaponMount`. Add a small, configurable single-shot hitscan firearm, simple muzzle flash/tracer and basic character aiming. Keep it modular and dormant while not firing; defer ammo economy, inventory, multiple weapons, tool use and full combat systems. Read [the rifle's contract](Weapons/Rifles/Rifle_001/README.md) first.

Read root `README.md`, `CodePreferences.md` and this README before doing any implementation. Keep one commit per authorized job and validation brief as requested.
