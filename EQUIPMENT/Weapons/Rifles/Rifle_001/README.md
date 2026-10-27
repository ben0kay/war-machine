# Rifle_001 — planned temporary low-poly rifle

**Status: documentation/folder scaffold only. Not implemented, not equipped, and not a working firearm.** This folder provides Codex with a specific home for a first simple rifle for the angled 3D top-down War Machine test.

## Intended files (create when implementing)

```text
Rifle_001/
├── Rifle_001.tscn               # Reusable root Node3D scene, muzzle / grip markers
├── README.md
├── C/
│   ├── RifleMeshBuilder.cs      # IMPORTANT: generate the temporary low-poly 3D rifle in C#
│   ├── RifleController.cs       # Rifle state, firing/cooldown / simple shot logic
│   ├── RifleSettings.cs         # Typed editable settings Resource
│   └── README.md
├── Configuration/
│   ├── RifleMaster.tres         # Default rifle stats/mesh settings when code exists
│   └── README.md
└── Artwork/
    └── README.md                # Later imported model/texture assets; no art required now
```

## Art / code separation — owner preference

**`C/` is the procedural C# mesh folder**: Godot C# builds boxes/cylinders/custom surfaces to form the low-poly rifle for the current prototype. It is not merely a folder for generic controllers. Keep model construction in `RifleMeshBuilder.cs` rather than the player script.

**`Artwork/` is where the owner may eventually place an actual `.glb` model** and other authored files. Make replacing the generated mesh with a future art scene straightforward; keep the same rifle root, grip and muzzle conventions.

## Future attachment and behaviour

- Proposed `Rifle_001.tscn` root as `Node3D`, with an **equippable grip origin** and a **Muzzle** `Marker3D` facing Godot forward (`-Z`). The procedural mesh and later imported art should use compatible local coordinates/scale.
- The current test player is a procedural humanoid in `WORLD/TopDownWorldTest/C/TopDownPlayer.cs`. Codex can introduce an explicit `WeaponMount` parented to the correct upper body/hand visual pivot and attach a single rifle instance there. The gun follows character animation; do not attach it to the physics root without aiming/pose consideration.
- Prototype only: character can aim toward mouse/world terrain, hold the rifle, left-click to fire a simple hitscan, show brief muzzle flash/tracer, and have inspector/master tweakable damage, cooldown and range. **No ammunition economy, inventory, reload system, weapon switching, projectile simulation or damage integration to the robot is required by this scaffold.**
- Avoid altering the parked `WORLD/testworld.tscn`; integrate only into the **active top-down scene** when specifically requested. Reuse shared helpers only when helpful; don't turn a first weapon into a large framework.
- Performance first: event-driven firing, small one-shot effects and no idle per-frame firearm processing. Character aiming may update only while the aim/cursor or actor changes, with no unrelated scene-tree searches.

## Codex handoff (future, not automatic)

Read root `README.md`, `CodePreferences.md`, `EQUIPMENT/README.md`, this file, current `TopDownPlayer.cs` and top-down test scene. Implement a **temporary usable Rifle_001** following the proposed contracts, explicitly keeping *C# procedural mesh generation in `C/`* and imported model destination in `Artwork/`. Hook into the top-down player's `WeaponMount` and leave the existing robot and first-person world unchanged unless separately requested. Build/check briefly; one commit per authorized job.
