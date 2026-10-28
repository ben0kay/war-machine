# Table001 — rectangular six-seat dining table

**Status:** original GLB artwork + reusable visual-only Godot scene, source implemented; **local Godot import, .NET build and in-game visual validation pending**. Classified as a **Static Object**, meaning it will not be liftable. This does **not** add a physics `StaticBody3D` yet.

## Appearance and use

- Reusable worn wooden dining table for houses, ranger lodges, dining rooms, abandoned cabins and roadside interiors.
- **1.82m X wide × 0.76m Y tall × 0.944m Z deep**, ground placement reference Y=0; tabletop long axis X. No individually movable chairs or clutter baked into the model.
- Five beveled wooden boards with delicate gaps, tapered legs, apron joinery and subtle metal fasteners. Designed to be reasonably legible from top-down and FPS without oversized details.
- **One** [Table001.glb](Artwork/Table001.glb): **532 triangles, 4 PBR material surfaces**; no texture files or duplicate models.

## Files and configuration

- `Table001.tscn`: canonical scene wrapping `Artwork/Table001.glb` under `Model/Artwork`; the root uses `TableVisual.cs`, derived from the shared `PlaceableVisual`.
- `Configuration/TableMaster.tres`: `SizeMultiplier = Vector3(1, 1, 1)` relative to native GLB; X width, Y height, Z depth. Changes apply to default instances on reload; duplicate/override the resource for a unique sized table.
- `FinishVariant`: **WeatheredOak**, **DarkWalnut**, **FadedGrey**. One shared mesh and three sets of cached wood material colours; only surfaces 0–2 are overridden, preserving the muted hardware details. Colour changes never regenerate geometry.
- `Artwork/README.md` documents the GLB primitive/material order for future re-export.

## Use and deferred gameplay

Open `Table001.tscn` in Godot or spawn via the **F1 Placeable Spawner** in `WORLD/World.tscn` to compare the three finishes. Previews are session-only, grounded, and **not collidable**; actors may walk through the table. Never auto-place into active world scenes unless requested.

**Future physics task (not implemented):** fitted `StaticBody3D` collision for tabletop, legs and aprons so there is no giant solid block occupying the under-table gap. Respect configurable nonuniform size when generating/positioning collider shapes. Future interactive objects (chairs, cups, food) remain separate catalogue assets.

## Local checks

Run `dotnet build war-machine.csproj` and import the GLB in Godot .NET. F6 `WORLD/World.tscn` → F1 → spawn all Table001 finishes, check wood tone, normals, table top/leg proportions, floor placement and size master. Confirm existing Bed001, Book001 and Bookcase001 entries still work, and that no physics or new idle callbacks were introduced. Run FPS debug preview optionally.

**Validation status:** GLB structure checked remotely. C# compilation, Godot import, visual appearance and FPS interaction remain deferred to local validation.
