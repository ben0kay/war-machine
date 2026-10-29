> **SOC0 static collision (2026-10-28):** fixed Bookcase001, Bed001, Table001 and Hedge001 now generate an invisible static BoxShape3D once from imported GLB bounds, scaled with the scene's size master. CustomBoxes and None are shared options. F1 can spawn these as temporary solid objects. Book001 and PineTree001 remain visual-only; future movable objects still need IX1+. Source implemented; runtime validation pending. [Implementation details](Shared/README.md).

# Objects — reusable world asset library

**Current status (2026-10-28):** Bookcase001, Book001, Bed001, Table001, PineTree001, Hedge001, WheelieBin001, DumpsterBin001 and Fridge001 are GLB artwork in stable reusable **canonical `.tscn` scenes**, each with its own `.tres` size master. Bookcase001, Bed001, Table001, Hedge001, WheelieBin001, DumpsterBin001 and Fridge001 additionally have SOC0 static AutoBox collision. The F1 menu now discovers eligible canonical visual-wrapper scenes automatically (source implemented; local validation pending). Bed001 has four duvet colours; Table001 has three wood finishes but uses only **one GLB model**. **Movable-prop physics, pickup and permanent placement** are future gameplay tasks; static SOC0 collision is implemented in source.

> **DOOR1 exception:** Non-pickup architectural doors can include a hinged RigidBody3D leaf. `Static/` describes item *ownership/pickup classification*, not the physics body type. See [Door001](Static/Doors/Door001/README.md). No loose-props physics system is active.

> **SOC1A partially implemented (2026-10-29):** The shared asset size master also includes `ShadowMode` (Inherit / Disabled / Enabled). Default Inherit preserves existing imported shadow settings. [SOC1 plan](SOC1_OBJECT_DIAGNOSTICS_PLAN.md) still proposes render-distance controls, F1 debugging, GLB validation and performance budget reports.

## Object versus Prop

- `Static/` are not pickup-able: Bookcase001 and Bed001 are fixed furniture; they can later be interactive without being carried.
- `Props/` are physically pickup-able in a future IX phase: Book001 is a small book that will eventually support carry/drop/throw.
- `WORLD/Content/Assets/Structures/` owns building shells. Future furnishing should instance these **catalogue scenes** under appropriate `contents` visual roles, not bake the same art into procedural building geometry.
- `INTERACTION/` owns targeting, hinges, physics carrying and breakage. `INVENTORY/` and `EQUIPMENT/` own inventory/equipment identities, not reusable art.

AM0 consolidated the former libraries here. [Static](Static/README.md) includes the existing cars and lamps; [Props](Props/README.md) includes future liftable books. No duplicate legacy asset folders remain.

## Current GLB catalogue

- [Fridge001](Static/Appliances/Fridges/Fridge001/README.md): original upright fridge/freezer GLB, separate upper/lower door pivots, **four shared finish variants** via FridgeVisual, size master and SOC0 AutoBox; automatically F1 eligible, not placed in the world.
- [DumpsterBin001](Static/Containers/Dumpsters/DumpsterBin001/README.md): original low-poly **large industrial dark-green steel dumpster** (~3.48m wide, ~2.55m tall) with two black pivoted lids, no wheels, SOC0 AutoBox, standard master and automatically discovered F1 preview; not permanently placed.
- [WheelieBin001](Static/Containers/WheelieBins/WheelieBin001/README.md): 2-wheel dark-green refuse bin, matte black separate lid pivot, original GLB, size master, and SOC0 AutoBox; automatically eligible for F1, not permanently spawned.
- [Bookcase001](Static/Furniture/Bookcases/Bookcase001/README.md): original GLB, a new visual `Bookcase001.tscn`, plus `Configuration/BookcaseMaster.tres`.
- [Book001](Props/Books/Book001/README.md): original GLB, a new visual `Book001.tscn`, plus `Configuration/BookMaster.tres`.
- [Bed001](Static/Furniture/Beds/Bed001/README.md): **converted the original C# geometry to `Artwork/Bed001.glb`**, kept a stable visual `Bed001.tscn` and `Configuration/BedMaster.tres`, plus 4 selectable cloth palettes.
- [Table001](Static/Furniture/Tables/Table001/README.md): 1.82m rectangular wooden dining table GLB, `Table001.tscn`, `TableMaster.tres` and three shared wood finishes. AutoBox static collision; no movable physics.
- [Hedge001](Static/Landscaping/Hedges/Hedge001/README.md): imported 4.8m-wide, ~2.14m-tall clipped ornamental hedge GLB, static manual landscaping rather than procedural vegetation; same size master; AutoBox static collision; its scene is also used by convenience-store landscaping.
- [PineTree001](Static/Vegetation/Trees/PineTree001/README.md): imported 14.4m pine GLB with a size master and F1 visual preview; kept **separate from** the active procedural MultiMesh forest under `VEGETATION/Trees/Pine/`. Higher-detail art prototype, not a forest replacement.

The common [Shared/PlaceableSizeSettings.cs](Shared/PlaceableSizeSettings.cs) exposes `SizeMultiplier = Vector3(1, 1, 1)`: X=width, Y=height/thickness, Z=depth/length, **relative to the GLB's native dimensions**. Change each asset's own master and respawn/reload to update all default instances. Use a duplicated/overridden master for individually sized copies. The common [PlaceableVisual.cs](Shared/PlaceableVisual.cs) applies size once on `_Ready`. The bed's [BedVisual.cs](Static/Furniture/Beds/Bed001/BedVisual.cs) changes materials only, **never rebuilds meshes**.

See [PLACEABLE_CATALOG.md](PLACEABLE_CATALOG.md) for canonical paths, dimensions, variant names and default masters.

## Authoring guidance for future AI chats

**Start with [OBJECT_AUTHORING_GUIDE.md](OBJECT_AUTHORING_GUIDE.md)** for future static-object and prop artwork. It now includes a **mandatory three-way SOC0 choice: None (0), AutoBox (1), CustomBoxes (2)**, plus scene/Inspector instructions, example bins and shelters, scale rules, F1 testing and the separate IX movable-item boundary. AM0 is implemented; this is its canonical location.

1. Prefer optimized imported GLBs under `Artwork/` for furniture, small props and appliances. Use a stable scene wrapper and a `.tres` per asset for frequently tweaked sizes; do not duplicate GLB source files merely for colour variants.
2. Use `C/` only when generating visible geometry in C#. A small interaction/colour-setting script belongs alongside its scene or `Shared/`. Preserve pivot, dimensions and material surface naming/order on re-export.
3. Share meshes/materials across instances; avoid per-frame work or physics on decorative clutter. The size helper is event/ready-driven and dormant at rest.
4. For **fixed Static** assets, explicitly choose `CollisionMode = 1` AutoBox (ordinary closed solid) or `2` CustomBoxes (usable gaps), or `0` None for decoration. Current bookcase/bed/table/hedge are AutoBox; their full boxes block decorative gaps. Future movable Book001 still needs IX1–IX2 rigid physics, not SOC0.
5. Reuse IX0–IX8 in `INTERACTION/IMPLEMENTATION_PLAN.md`; do not place/migrate assets in active world or inventory as part of visual art authoring. The F1 menu now automatically discovers valid `PlaceableVisual` scenes by canonical folder/scene name, with public exported enum variant choices; do not hardcode F1 indices or paths.
6. [Car001](Static/Cars/Car001/README.md) now lives under `Static/Cars/`; it retains its procedural geometry, original settings and existing collision. Its master is not a GLB size master.

## F1 object previews (SOC0 collision when enabled)

Run `WORLD/World.tscn` or the FPS debug scene with F6, press **F1**, then select any automatically listed canonical GLB visual wrapper, including Hedge001, the four Bed001 palettes, the three Table001 wood finishes, Bookcase001, Book001 or PineTree001, using the [Placeable Spawner](../../../../DEBUG/PlaceableSpawner/README.md). All previews use the **canonical scene and corresponding `.tres`**. Bed and bookcase are grounded by their Y=0 source pivot; book hovers to allow close inspection. These are temporary previews; **SOC0-enabled fixed objects collide** while none-mode props/previews don't. There is still no gravity-driven prop simulation, pickup or saved placement. Godot import/build/visual acceptance remains pending locally.
