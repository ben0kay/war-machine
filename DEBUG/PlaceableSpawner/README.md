# F1 Placeable Spawner — automatic canonical-scene discovery

> **Current (2026-10-28): Dynamic F1 catalogue implemented in source; local Godot/.NET import, build and visual tests are pending.** Replaces the original hardcoded Bookcase/Book/Bed/Table/Pine menu entries, numeric selection ranges and source file path switches.

## Automatic menu rules

`PlaceableCatalog.cs` scans **once** when the world creates the F1 debug menu. It searches `res://WORLD/Content/Assets/Objects/Static/` and `Props/` recursively for **canonical** `<AssetID>/<AssetID>.tscn` files. It ignores helper folders such as `Artwork/`, `Configuration/`, `C/` and `Shared/`. It only accepts scene roots deriving from `PlaceableVisual`; **procedural/physics-aware Car001 and StreetLamp001 are excluded** because their roots don't inherit `PlaceableVisual`. **Eligible SOC0-enabled fixed objects do have collision in F1 runtime previews**, as authored on the canonical scene; no duplicate physics is added by the menu. A future visual-only wrapper can opt a procedural asset in when explicitly requested.

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

- OBJECT · Appliances / Fridges: **Fridge001** (Warm White, Brushed Silver, Dark Graphite, Seafoam Green).
- OBJECT · Containers / Dumpsters: **DumpsterBin001** (static AutoBox, 7m preview minimum).
- OBJECT · Containers / WheelieBins: WheelieBin001 (static AutoBox).
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
2. Confirm category headings and all **17** options (including WheelieBin001 and DumpsterBin001): Bookcase001 (1), Book001 (1), Bed001 (4), Table001 (3), PineTree001 (1), Hedge001 (1). Procedural Car001/StreetLamp001 should not appear.
3. Spawn each existing variation, check materials and canonical size masters. Book001 hovers using `BookHoverHeight`, PineTree001 appears >=12m away, Hedge001 spawns grounded, and the same selected type gets the right preview name/status.
4. Check 24-instance cap, Clear, F1/Escape focus and FPS mouse recapture. No additional physics bodies/terrain placement should appear.
5. Create a dummy standard GLB wrapper `Static/Furniture/Chairs/Chair001/Chair001.tscn` deriving from `PlaceableVisual`; restart world, confirm it automatically appears under Furniture / Chairs, spawns correctly, and requires **zero** edits to `PlaceableSpawnMenu.cs` or `PlaceableCatalog.cs`. For a dummy new enum-variant wrapper, verify each enum member appears and changes the model as expected.

**Source implementation staged, local compile/runtime acceptance pending.** Do not claim tested or accepted until the owner runs the above.

## SOC0 physics preview update (2026-10-28)

The F1 catalogue remains automatic and capped at 24 temporary instances, but the old “visual preview only” limitation is relaxed **per asset**: `PlaceableVisual.CollisionMode` now controls fixed collision. Bookcase001, Table001, Bed001 and Hedge001 default to `AutoBox` and are solid in F1 and when instanced in the world. Book001 and PineTree001 remain noncolliding; Car001/StreetLamp001 remain outside this F1 catalogue. The menu no longer claims all previews lack collision. Clearing previews frees their temporary static bodies; nothing is saved to world placement. The exported collision selector is an **int with an Inspector enum hint**, not a C# enum property, so automatic F1 palette enumeration remains unchanged. A bounding box may also block decorative gaps until manually configured as CustomBoxes. Godot gameplay testing pending.

**Future asset authors:** See the [canonical object/prop authoring guide](../../WORLD/Content/Assets/Objects/OBJECT_AUTHORING_GUIDE.md) for the mandatory None/AutoBox/CustomBoxes selection, custom-box examples, and when to defer rigid-body physics. F1 does not create or replace colliders: it instantiates the actual canonical prefab with its authored SOC0 mode.

**WheelieBin001:** now automatically discovered under `OBJECT · Containers / WheelieBins` with no spawner code changes. Solid SOC0 AutoBox scene; please validate appearance and collision locally. F1 remains temporary-only.

**DumpsterBin001 (2026-10-28):** A new canonical static object `Static/Containers/Dumpsters/DumpsterBin001/DumpsterBin001.tscn` auto-registers under `OBJECT · Containers / Dumpsters`. Its scene metadata requests a **7m minimum** preview distance because its dimensions are ~3.48 × 2.55 × 1.91m; no changes to the global F1 3m setting, its scanner, or other assets. Uses one SOC0 AutoBox; lids stay closed. Model import, visual appearance and contact must be validated locally.

**Fridge001 (2026-10-28):** the canonical `Static/Appliances/Fridges/Fridge001/Fridge001.tscn` is automatically discovered with the four exported `FridgeVisual.FridgeFinish` options. Each prefab instance uses shared GLB/materials with per-instance paint overrides and SOC0 static AutoBox. No spawner code edits or permanent scene placement; import/finish appearance and physics must be verified in Godot.
