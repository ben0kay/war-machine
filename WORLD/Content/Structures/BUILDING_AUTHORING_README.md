# Building authoring contract — give this to future building-design chats

Use **Building002** as the multi-storey reference. Reuse shared visibility; do not add building-name cases to WorldController or put camera/player code in a mesh generator.

## Prefab and ownership

- Keep `<Building>/<Building>.tscn` as the stable prefab, `C/` for procedural mesh construction, `Artwork/` for later models, and `Configuration/` for the master/settings.
- Derive the building settings from `StructureSettings`. Its inherited exports provide OutsideBuildingOpacity, HiddenUpperOpacity, IndoorRoofOpacity, FloorFadeSeconds and LandingHysteresis. Existing master values remain in each building's `.tres`; the runtime component deep-copies them.
- Add **one direct Node child** using `Shared/StructureVisibility.cs`, with `Settings` referencing that same master. No CameraOccluder nodes are needed in a new compliant prefab: shared code builds disjoint section owners.
- Place it beneath the world's `Structures` subtree (or its descendants). The active top-down camera discovers existing components once and listens for later prefab Ready/Exit events. It does not need a new named case. FPS does not register the context, so it keeps opaque geometry.

## Required metadata

Metadata can be set in the Inspector for authored models, or generated before the parent `_Ready()` finishes. The names below are metadata keys, **not mandatory node paths**.

| Declare on | Metadata | Meaning |
| --- | --- | --- |
| Building/layout Node3D | `structure_occupied_bounds` = Aabb | Occupied volume, in that node's local coordinates; declare multiple boxes for irregular footprints/canopies |
| Each floor parent | `floor_level` = integer; `floor_elevation` = float | Ascending level numbers starting at 0; feet elevation **relative to building root**; no duplicate levels |
| Disjoint visual subtree | `structure_visual_role` = string | One of `walls`, `roof`, `deck`, `contents`, `stairs` |

Keep occupancy/floor declarations above visual groups. Visual role roots **must not overlap/nest**: traversal stops at a role root. Characters/entities must never be children of a visual role group. Keep building root upright, use uniform scale, and keep floor numbers/elevations increasing. Rectangular occupied volumes are a first-pass approximation, not room/portal detection.

Example layout: building -> occupied layout -> Level0/Level1 -> separate Walls, Roof, Deck, Contents; separate connecting Stairs. Each level contains its own visual groups and **separate static collision**. Keep a real opening in upper deck geometry/collision around stairs, a walkable connector, clear landings and doors. Do not fade/remove collision to reveal interiors.

C# declaration helpers (Shared/StructureLayout.cs):

```csharp
StructureLayout.Occupied(layout, new Aabb(new Vector3(-8, -0.5f, -6), new Vector3(16, 8, 12)));
StructureLayout.Level(ground, 0, 0);
StructureLayout.Level(upper, 1, 4);
StructureLayout.Visual(walls, "walls");
StructureLayout.Visual(deck, "deck");
StructureLayout.Visual(furniture, "contents");
StructureLayout.Visual(roof, "roof");
StructureLayout.Visual(stairs, "stairs");
```

## What each role does

- **walls:** individual camera-to-player obstruction fading on the occupied floor; lower-storey walls restore upstairs.
- **roof:** fades indoors when at/above the occupied level. A roof belongs to its supporting floor; a global top roof goes on the top floor.
- **deck / contents:** solid on the occupied/lower floors; upper levels fade and restore near their landing. Furniture never inherits wall/roof camera fading.
- **stairs:** stays readable indoors across levels; only the outside whole-building fade affects it.
- Untagged art gets outside-house fading only. Separate decorative trim with its corresponding wall/roof role if it should follow that fade.

Outside, the entire building fades coherently to its own OutsideBuildingOpacity (default 0.15). Inside/under canopy, sectional/floor presentation takes over. Tag the petrol forecourt/canopy as occupied too so pumps remain solid while underneath. Cosmetic transparency never changes collision, weapon hits or enemy perception.

## Procedural spawning and rebuilding

Generate the same metadata/groups for every seed and finish before visibility's deferred initialization. Newly spawned prefabs auto-register while beneath the active structures root; no recurring scans. For a live geometry rebuild, call `visibility.Rebuild(() => RebuildGeneratedArt())`: it restores/unregisters old owners before the callback and defers rebuilding caches afterward. Use this after an explicit seed/authoring action, **not every frame**. `Refresh()` can rebuild visibility caches after metadata-only edits. Only visibility reuse is implemented here; procedural house layouts, editor placement/snap/seed locking remain future work in NOTES/Ideas/PROCEDURAL_HOUSES_AND_HYBRID_WORLD.md.

Brief checks: compile, spawn/despawn a second instance, walk behind/in/out, ascend/descend each landing, stop during/after fades, confirm occupied furnishings/stairs and physical LOS stay solid, and check FPS stays opaque. No per-frame scans, raycasts or timers per structure; only movement events and active fade transitions. Godot build/visual acceptance for this refactor remains pending locally.
