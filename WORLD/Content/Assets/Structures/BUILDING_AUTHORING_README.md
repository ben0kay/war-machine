# Building authoring contract — give this to future building-design chats

Use **Building002** as the multi-storey reference. Reuse shared visibility; do not add building-name cases to WorldController or put camera/player code in a mesh generator.

## Prefab and ownership

- Keep `<Building>/<Building>.tscn` as the stable prefab, `C/` for procedural mesh construction, `Artwork/` for later models, and `Configuration/` for the master/settings.
- Derive the building settings from `StructureSettings`. Its inherited exports provide OutsideBuildingOpacity, OutsideObstructionRadius, HiddenUpperOpacity, IndoorRoofOpacity, FloorFadeSeconds and LandingHysteresis. Existing master values remain in each building's `.tres`; the runtime component deep-copies them.
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

Outside, the entire building fades coherently to its own OutsideBuildingOpacity (default 0.15). The optional per-building OutsideObstructionRadius (default 0.18m) narrows camera-to-player obstruction checks near corners independently of CameraMaster's larger foliage radius; it must remain small enough to avoid premature whole-building fading, but large enough to catch genuine cover. Inside/under canopy, sectional/floor presentation takes over. Tag the petrol forecourt/canopy as occupied too so pumps remain solid while underneath. Cosmetic transparency never changes collision, weapon hits or enemy perception.

## Procedural spawning and rebuilding

Generate the same metadata/groups for every seed and finish before visibility's deferred initialization. Newly spawned prefabs auto-register while beneath the active structures root; no recurring scans. For a live geometry rebuild, call `visibility.Rebuild(() => RebuildGeneratedArt())`: it restores/unregisters old owners before the callback and defers rebuilding caches afterward. Use this after an explicit seed/authoring action, **not every frame**. `Refresh()` can rebuild visibility caches after metadata-only edits. Only visibility reuse is implemented here; procedural house layouts, editor placement/snap/seed locking remain future work in NOTES/Ideas/PROCEDURAL_HOUSES_AND_HYBRID_WORLD.md.

Brief checks: compile, spawn/despawn a second instance, walk behind/in/out, ascend/descend each landing, stop during/after fades, confirm occupied furnishings/stairs and physical LOS stay solid, and check FPS stays opaque. No per-frame scans, raycasts or timers per structure; only movement events and active fade transitions. Godot build/visual acceptance for this refactor remains pending locally.

## Reusable Placeables for later AI furnishing (2026-10-27)

New [Placeables library](../Objects/README.md) and [catalogue](../Objects/PLACEABLE_CATALOG.md) hold reusable non-pickup **Objects** and future physics-liftable **Props**, with original model-only Bookcase001 and Book001 GLBs. These **are not yet collision-ready scenes**.

Future AI can furnish buildings using stable asset prefabs rather than building furniture into each procedural structure's `C/` mesh generator. Put fixed furniture in the appropriate floor furnishing/`contents` presentation subtree, without blocking entrances and stairs. Loose physics props need authored spawn anchors and an independent authoritative prop owner/state so building art regeneration and top-down fading do not own/destroy physical movement. A faded item never loses its collider. Don't generate or place furniture automatically until requested.

## OV1 — accurate exterior blockers

Exterior whole-building fade now requires five camera-projected player samples to hit actual cached opaque walls/roof/deck triangles; bounds/radius alone do not trigger it. Current outside opacity and all indoor presentation stay unchanged. Collision is not consulted or changed. Keep these visual roles accurate, static and separate from contents/stairs. Plain opaque native materials work automatically, including visual-only gables and roof slabs. Current coloured window panes are opaque and therefore count; actual open door/window geometry remains clear.

Split transparent/cutout/custom shader surfaces away from opaque shell meshes: mixed meshes containing a nonopaque surface are conservatively skipped; unknown shaders/no faces warn and fail open. Optional `structure_camera_blocker=false` on mesh/ancestor excludes decoration. This does not make contents into blockers. Cosmetic camera transparency does not affect the cached decision; explicit hidden geometry is skipped. Refresh/Rebuild after material/geometry/relative transform edits. GPU deformation/alpha pixel coverage is not evaluated. Complex art exceeding the 16,384 triangle-test budget per check stays solid with a once-only warning; use simpler static structural geometry. See CAMERA/Occlusion/BUILDING_FADE_IMPROVEMENT.md for the audit and local acceptance. OV2 actor hiding is separate.
