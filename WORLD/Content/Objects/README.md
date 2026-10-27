# World objects

Small and medium **stationary environmental props** belong here, independent of buildings, entities and equipment.

- `Cars/Car001/`: original abandoned compact car, made procedurally in Godot C#, with a front-left door **already swung open**.

Like other world content, `C/` holds temporary C# mesh generation, `Artwork/` stores future imported models and `Configuration/` holds master settings. Prop scenes are reusable and are not automatically placed in WORLD/World.tscn.

## Planned physics props / interactive interiors (not implemented)

[INTERACTION/IMPLEMENTATION_PLAN.md](../../../INTERACTION/IMPLEMENTATION_PLAN.md) defines IX0–IX8 for shared physics manipulation and scavenging. IX1 proposes `WORLD/Content/Objects/Props/` with reusable `RigidBody3D` books, buckets, broomsticks and boxes; their prefabs would live here, while interaction targeting/grab/hinges stay in `INTERACTION/` and item ownership under future `INVENTORY/`. Current Car001 and other stationary props remain untouched. Nothing was enabled by creating that plan.
