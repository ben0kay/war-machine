# Fridge001 — four-finish upright refrigerator/freezer

**Status (2026-10-28):** Original, reusable Godot GLB + canonical C# prefab + shared size master + SOC0 AutoBox + four F1-finish variants authored. **Godot import, local .NET build, and runtime appearance/physics still require local acceptance.** No permanent world placement.

## Design and asset

A slightly rounded, realistically proportioned upright domestic refrigerator with **a shorter upper freezer door and a tall lower refrigerator door**. Details include two slightly protruding door panels with dark perimeter gaskets and a horizontal parting gap, silver vertical pull handles, side hinges, recessed toe-kick, four small feet, subtle enamel badge and rear compressor grille. No branding/text, external textures, animation loops or audio.

- [Artwork/Fridge001.glb](Artwork/Fridge001.glb): original **glTF 2.0 binary**, **796 triangles**, **11 grouped mesh nodes**, **7 opaque PBR material definitions**, approx. **55 KB**, no external resources.
- Native bounds measured from authored nodes: approximately **0.83 m wide × 1.87 m tall × 0.91 m deep** including handles (housing is about 0.74 m deep). Y=0 floor pivot; +Y up; **+Z faces the doors**, -Z rear with compressor grille.
- **Two separate future-ready pivots:** `UpperFreezerHinge` and `LowerFridgeHinge` at the right edges; door enamel, rubber gasket and individual handle meshes each descend from their respective hinge. These transform nodes can be adapted for a future actual opening mechanic, but **both doors are closed/inert now**. Do not claim they open or replace the single current static collider while they are animated.

## One GLB, four real material finishes

`FridgeVisual.cs` derives from `PlaceableVisual` and exports `FridgeFinish FinishVariant`. F1 discovers the enum automatically and offers **Warm White, Brushed Silver, Dark Graphite, Seafoam Green** as separate entries. Each choice changes only the imported single-surface mesh nodes `FridgePaint_Shell`, `FridgePaint_UpperDoor`, `FridgePaint_LowerDoor` through **shared static material override resources**, with separate body and door tint/metallic/roughness. It does **not** modify or duplicate the imported GLB, mutate a source material, or perform per-frame work. Handles/gaskets/vent mesh groups keep their original metallic/dark PBR colours for every finish.

| Finish | Surface character |
| --- | --- |
| WarmWhite | slightly creamy enamel, default |
| BrushedSilver | cool silver-metal body and brighter metal doors |
| DarkGraphite | dark anthracite satin/graphite enamel |
| SeafoamGreen | desaturated vintage mint-green enamel |

## Scene, collision and testing

- [Fridge001.tscn](Fridge001.tscn) wraps the shared GLB at `Model/Artwork` and uses [FridgeVisual.cs](FridgeVisual.cs). [Configuration/FridgeMaster.tres](Configuration/FridgeMaster.tres) has `SizeMultiplier = Vector3(1,1,1)` (relative X width, Y height, Z depth); overrides need reinstantiate/reload for collision updates.
- **Collision mode = 1, SOC0 AutoBox** on the canonical scene root, using existing one-shot `StaticObjectCollision.cs` to generate one `StaticBody3D/BoxShape3D` on world layer 1 at runtime. This is a **fixed closed solid**, not pickup-able or currently openable. The AutoBox includes handles/door protrusion and deliberately fills any decorative gaps. Future door-opening needs adjusted static/door colliders and the separate IX interaction owner.
- The F1 spawner finds the canonical scene automatically under **OBJECT · Appliances / Fridges → Fridge001 · [Finish]**. No F1 code changes. The asset is **not permanently added** to houses, shops, terrain, or site generation.

**Acceptance:** pull `implement-td0-td1`, allow Godot to import `Fridge001.glb` and open the scene to inspect panel gaps, normals, feet, colour/metallic finish and handles. Run `dotnet build war-machine.csproj`, open F6 `WORLD/World.tscn` → F1 and try all four finishes, collision, orientation and scaling. Check FPS inherited view, player collision, shot/LOS masking and clearance before putting it in a building. Confirm each selected F1 finish changes body + doors only, rather than every fridge instance. Godot/editor runtime wasn't available during remote authoring.
