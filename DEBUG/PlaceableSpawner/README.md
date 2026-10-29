# F1 Placeable Spawner — automatic canonical-scene discovery

> **Current (2026-10-28): Dynamic F1 catalogue implemented in source; local Godot/.NET import, build and visual tests are pending.** Replaces the original hardcoded Bookcase/Book/Bed/Table/Pine menu entries, numeric selection ranges and source file path switches.

## Automatic menu rules

`PlaceableCatalog.cs` scans **once** when the world creates the F1 debug menu. It searches `res://WORLD/Content/Assets/Objects/Static/` and `Props/` recursively for **canonical** `<AssetID>/<AssetID>.tscn` files. It ignores helper folders such as `Artwork/`, `Configuration/`, `C/` and `Shared/`. It only accepts scene roots deriving from `PlaceableVisual`; **procedural/physics-aware Car001 and StreetLamp001 are excluded** from this visual-only, no-physics preview on purpose. Do not put gameplay colliders in this F1 menu accidentally. A future visual-only wrapper can opt a procedural asset in when explicitly requested.

Menu entries are sorted by category (OBJECT/PROP and subfolder names), asset ID and option. `OptionButton.AddSeparator` inserts visible category headings; actual items use unique IDs referencing a cached `PlaceableEntry`, rather than the index of the menu row. Future canonical visual scene wrappers automatically appear **the next time the world starts**—no manual C# registration or modification to the markdown catalogue required. The catalogue markdown is **documentation**, not a parsed/runtime registry.

### Auto-discovered variants

If the scene root derives from `PlaceableVisual` and contains a **public exported C# enum property**, the scanner generates one entry per enum member, humanising labels: `WarmSand` → `Warm Sand`. Variant values are assigned to a new scene instance **before** `AddChild` and `_Ready`, so existing `BedVisual.BeddingVariant` (four colours), `TableVisual.FinishVariant` (three finishes) and similarly authored future scripts work without spawner changes. Assets without exported enums appear once. If a visual root exports multiple enums, each is offered as an independently selectable variation of the scene defaults (not the cross-product of all possible combinations). Keep one enum per default visual variant family where practical.

### Optional debug preview metadata

Normal placeables require **no preview hints**. Assets with unusual viewing requirements may add metadata to their canonical scene's root:

```ini
metadata/f1_preview_hover = true
metadata/f1_preview_min_distance = 12.0
```

The first applies the existing `Configuration/PlaceableSpawnMaster.tres` `BookHoverHeight` to that preview. The second enforces a minimum horizontal distance, but never overrides a *larger* globally configured `SpawnDistance`. Book001 has only the hover flag; PineTree001 has only the 12m minimum. New beds, hedges, chairs, tables, cupboards etc. need neither. Both flags affect F1 preview placement **only**, not actual world placement or asset size masters.

## Current expected menu

- OBJECT · Furniture / Beds: Bed001, each of Slate Blue / Moss Green / Warm Sand / Dusty Rose.
- OBJECT · Furniture / Bookcases: Bookcase001.
- OBJECT · Furniture / Tables: Table001, each of Weathered Oak / Dark Walnut / Faded Grey.
- OBJECT · Landscaping / Hedges: **Hedge001**, previously missing from the hardcoded F1 list.
- OBJECT · Vegetation / Trees: PineTree001 (12m minimum preview distance).
- PROP · Books: Book001 (0.95m default hover as before).

## Input, spawning and performance

F1/Escape/captured-mouse behavior, `Spawn selected model`, `Clear spawned`, max **24** temporary child instances, terrain height sampling, pivot/rotation and no saved placement remain as before. The scene/model is loaded from its **canonical wrapper**, not a separate GLB or new mesh generator. F1 does not create collision, gravity or inventory functionality; visual-only source scenes remain noncolliding. One startup directory walk and one scene-type/enum inspection per eligible scene are cached for the session. No per-frame menu updates, polling, scanning on repeated F1 opens, or additional spawn-time directory traversal.

## Validation handoff (Godot/.NET not run here)

1. `dotnet build war-machine.csproj`, import current GLBs once, F6 `WORLD/World.tscn`, open F1.
2. Confirm category headings and all **11** options: Bookcase001 (1), Book001 (1), Bed001 (4), Table001 (3), PineTree001 (1), Hedge001 (1). Procedural Car001/StreetLamp001 should not appear.
3. Spawn each existing variation, check materials and canonical size masters. Book001 hovers using `BookHoverHeight`, PineTree001 appears >=12m away, Hedge001 spawns grounded, and the same selected type gets the right preview name/status.
4. Check 24-instance cap, Clear, F1/Escape focus and FPS mouse recapture. No additional physics bodies/terrain placement should appear.
5. Create a dummy standard GLB wrapper `Static/Furniture/Chairs/Chair001/Chair001.tscn` deriving from `PlaceableVisual`; restart world, confirm it automatically appears under Furniture / Chairs, spawns correctly, and requires **zero** edits to `PlaceableSpawnMenu.cs` or `PlaceableCatalog.cs`. For a dummy new enum-variant wrapper, verify each enum member appears and changes the model as expected.

**Source implementation staged, local compile/runtime acceptance pending.** Do not claim tested or accepted until the owner runs the above.

## SOC0 physics preview update (2026-10-28)

The F1 catalogue remains automatic and capped at 24 temporary instances, but the old “visual preview only” limitation is relaxed **per asset**: `PlaceableVisual.CollisionMode` now controls fixed collision. Bookcase001, Table001, Bed001 and Hedge001 default to `AutoBox` and are solid in F1 and when instanced in the world. Book001 and PineTree001 remain noncolliding; Car001/StreetLamp001 remain outside this F1 catalogue. The menu no longer claims all previews lack collision. Clearing previews frees their temporary static bodies; nothing is saved to world placement. The exported collision selector is an **int with an Inspector enum hint**, not a C# enum property, so automatic F1 palette enumeration remains unchanged. A bounding box may also block decorative gaps until manually configured as CustomBoxes. Godot gameplay testing pending.
