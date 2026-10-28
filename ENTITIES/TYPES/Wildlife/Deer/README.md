# Deer — first passive wildlife entity

**Implemented in source; Godot C# compilation and visual/physics checks pending.** `Deer.tscn` inherits the **same** shared `ENTITIES/Shared/Entity.tscn` used by zombies. It is a real `CharacterBody3D` with collision on physics layer 3, shared identity/lifecycle, a **120 HP** `HealthComponent` and `IDamageReceiver`. Rifles and machine guns can hit it using their existing rays and damage pipeline; full-HP health bars stay hidden, and an injured deer uses the generic FEEDBACK overlay automatically. Lethal hits simply deactivate, clear collision, hide and free the actor, as zombies do today.

## Contents

- `Deer.tscn`: inherited `Entity` scene, deer-specific capsule (radius 0.42m, 1.65m tall, centre Y=0.825), `DeerController` and a Health child; the shared `VisualRoot` uses `C/DeerMeshBuilder.cs`.
- `DeerController.cs`: health initialization, attack hit forwarding, event-driven death and cleanup only. It has **no** wander timer, perception/targeting, animation, gravity polling, pathfinding or AI.
- `DeerSettings.cs` / `Configuration/DeerMaster.tres`: typed species master with 120 HP (zero armour/resistances), coat/underside/hoof/antler colours, and a `HasAntlers` toggle.
- `C/DeerMeshBuilder.cs`: `[Tool]` lightweight original low-poly mesh, built once under `VisualRoot/Generated`; dark-brown coat, cream belly/muzzle, four legs and hooves, long neck, two ears, eyes, short white tail and two small branched antlers. Uses a few shared primitives and basic materials. No imported textures, new shaders or limb colliders.
- `Artwork/`: reserved for a future imported realistic deer model or texture set.

## In-world test

`WORLD/WorldController` now has a **single opt-in preview placement enabled by default**. `WORLD/Configuration/WorldMaster.tres`: `DeerPreviewEnabled=true`, `DeerPreviewOffset=(-14,12)` relative to the player's `SpawnXZ`. The active scene has `WorldActorsRoot/PersistentEntitiesRoot/Wildlife`; the world samples Terrain3D native collision and checks capsule clearance once after terrain collision startup. If the site is blocked/unsuitable it **skips with an Output warning** rather than floating the deer or moving terrain. A small vegetation/rock/grass clearing is reserved around that position. Turn `DeerPreviewEnabled=false` to remove the test spawn without deleting the prefab.

The deer stands still at its sampled ground position until killed. This is intentional for a first health and artwork check; it does **not** flee, react visibly to being shot, graze, move, attack, or behave as ambient wildlife yet. The single test spawn is not a population manager, persistent wildlife, loot or crafting feature. For future AI, keep this scene and add its own species-specific behaviour without borrowing zombie pursuit.

## Local acceptance

Pull branch `implement-td0-td1`, run `dotnet build war-machine.csproj`, then F6 `WORLD/World.tscn` (also inspect the inherited FPS debug scene if useful). Look roughly 14m west and 12m south of player spawn; confirm a deer appears on the ground with no large overlaps. Shoot once with the 95-damage rifle: health goes 120 → 25 and the shared health bar should show. A second rifle hit removes the deer. Check a machine gun burst, disabled preview flag, clean scene unload and no idle deer processing. You can open `Deer.tscn` in Godot editor to preview its basic mesh; its builder runs once and does not require imported artwork.

**Unverified here:** compiling the C# scripts, inherited Godot scene loading, mesh appearance, native clearance and health-bar rendering. No new zombie/robot AI, damage maths, terrain rebake or player-health code was changed.
