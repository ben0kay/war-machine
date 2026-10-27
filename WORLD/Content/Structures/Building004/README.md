# Building004 — two-storey abandoned field office

**Status:** New standalone procedurally authored 2-storey comparison prefab, placed in the active `WORLD/World.tscn` by shared `WorldController`. C# compilation, terrain physics, visuals and floor transitions await local Godot validation.

## What makes it different from the lodge

- **Style:** urban/municipal concrete-and-weathered-brick field office, flat metal-parapet roof and rooftop ventilation equipment (rather than Building002's timber/gable roof). Reuses `Shared/C/StructureGeometry.cs` from the Building002/003 authoring toolchain.
- **Footprint:** default 18 × 14 metres. Front entrance faces **+Z**; the lower ground floor is Terrain3D-supported, with thin non-solid decorative slab. Second floor elevation is **3.35m**, upper walls 3.0m tall.
- **Ground:** central open front entrance, actual service exit at back-right, real window openings with separate solid glazing, dispatch counter, records cabinet, security room with internal doorway.
- **Upper:** **genuinely collidable** deck divided into four rectangles leaving an honest stairwell aperture. A 21-tread crosswise staircase rises toward **+X** on an invisible continuous slope (<46°). The upper floor has archive/office furnishings, an internal partition doorway and a walk-out balcony above the entrance. Both the balcony floor and handrails have collision.
- **Roof:** separate solid flat roof with parapet and ventilation box. `ShowRoof`/`SetRoofVisible` hide only mesh visuals, not collision.
- **Visual roles:** `StructureLayout.Level` metadata for Level_0/Level_1; disjoint `walls`, `deck`, `contents`, `stairs`, `roof` groups; occupied main body + extra upper balcony zone. Dedicated child `StructureVisibility` reuses the exact shared camera/floor/outside fading logic already used by Building002/003. No building-specific visibility code.
- **Physics:** all walls, glazing, stairs ramp, upper decks, balcony guard, roof and solid furniture use static layer-1 colliders; NPCs and firearms use existing collision/LOS. No door interaction, lights, procedural-seed generator, AI navigation, persistent world state or extra tick loops.

## Sources

`Building004.tscn` is the reusable prefab. `C/Building004Mesh.cs` builds geometry once in `_Ready()`, `Configuration/Building004Settings.cs` and `Configuration/Building004Master.tres` own the independently adjustable authoring values, and `Artwork/` reserves future replacement meshes. The reusable building authoring contract is [../BUILDING_AUTHORING_README.md](../BUILDING_AUTHORING_README.md).

## Location and tests

The shared world places Building004 at `WorldMaster.Building004Offset = (-34, -70)` relative to player spawn—about 26m south of Building002, with a clear outdoor approach between them. Both the top-down and inherited first-person world use the same placement, as they do for the other buildings. Its generated mesh bounds are passed through the normal one-time pine/rock/grass clearing logic; **no Terrain3D bake or terrain alteration**.

**Local smoke tests:** build C#; F6 `WORLD/World.tscn` and visit the lodge north-west of spawn, then head further north (negative Z) to Building004. Enter centre front, walk west and use the crosswise stairs, confirm solid landings/no whole-floor collision above the stairwell and reach the upstairs balcony. Walk behind/inside/under upper floor, ascend and descend, check upper sections and roof fade without floor/physics disappearing; test rifle LOS and F7, FPS comparison and original three buildings. There is no formal scene render/gameplay validation from this connector; visual acceptance remains with the owner.
