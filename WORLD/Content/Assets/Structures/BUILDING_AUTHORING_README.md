# Building authoring — technical Godot/C# contract

**Read together with [BUILDING_DESIGN_GUIDE.md](BUILDING_DESIGN_GUIDE.md).** The design guide is the first-phase floorplan and structural-quality authority (including all previous PoliceStation001 failures); this document is the second-phase engineering authority for prefab scenes, editor generation, physical geometry, visibility, fog/haze, streaming, performance and testing. **Both are mandatory for any new building.**

## Two independent gates

1. **Architectural acceptance:** planned circulation and functional rooms, genuine openings, ≥2 m safe stair approaches, clean window/partition joins, no wall nubs, correct interior/exterior floor seams, collidable upper landing, guarded stair rim and clear walking exit. See [design rules and failure register](BUILDING_DESIGN_GUIDE.md). A successful script compilation cannot establish this.
2. **Technical acceptance:** scene works standalone in editor; visual/physical collision correspond; floor/roof/wall semantic groups, occupied footprint, camera fading, AIM1 and haze support are correct; no per-frame generation and no unexpected GPU/physics costs. All behavior must be locally checked in Godot before marked accepted.

## GH1 — haze exclusion / exact occupied footprint

The project reads `structure_occupied_bounds` metadata from buildings beneath `WorldActorsRoot/PersistentPropsRoot/Structures` through `GroundHaze.SetBuildingExclusions(...)`. Declare `StructureLayout.Occupied(node, Aabb)` on the **actual covered footprint** of every enclosed building or roofed open-air canopy that should suppress drifting ground haze. Do not tag the full parking/site root: roads and open car parks must keep their haze. A complex building can use multiple overlapping rectangle bounds for its wings and canopies, with local XZ rotation supported. The default soft fade extends **1.25 m** beyond the boundary. This affects `GroundWisps.gdshader` haze only, not Sky3D baseline/distance fog, collision or LOS. After dynamically creating/removing buildings, explicitly refresh `GroundHaze.SetBuildingExclusions`: the startup capture does not track live structural changes automatically. Keep separate any broader streaming/activation volumes from roof-covered ground zones.

## Procedural prefab editor preview — required for inspection before placement

Use a stable `<Building>/<Building>.tscn`, `C/` for mesh construction, `Artwork/` for future imported GLBs, `Configuration/` for typed `StructureSettings` master resources, and a building-local README. Add a direct `StructureVisibility` child referencing the same settings master. A procedural authoring generator should use `[Tool]` if its design needs to be visible in Godot's scene editor; generate once on load and provide a meaningful **Build / Refresh Preview** Inspector control after a C# hot-reload. The generated root should be visible/inspectable, but large transient art/collision descendants should not be unknowingly serialized into the `.tscn`. Rebuild/refresh must be idempotent and remove old generated children, not double-place geometry. GroundFloor / UpperFloor / Exterior previews should hide the corresponding visual groups without changing the runtime physical contract. Never run `_Process` or `_PhysicsProcess` merely to regenerate static walls.

## Enforce architectural constraints in mesh AND collision code

- `StructureGeometry.WallX/WallZ` or equivalent must split actual wall **spans** around doors/windows; the visible hole and collider hole must match.
- `StructureClearance.Reserve` holds named local authoring volumes and `Allows(...)` rejects **participating furniture** by full bounds, **not** bad structural stairs/walls. Separately validate stair-door proximity, window/partition intersections and connected deck/landing dimensions **before generating the shell**. Reject invalid geometry with a clear logged error, not a disguised warning.
- Trim side/corridor partitions to the **recipient wall's inside face** (rather than the centreline). Example current police-station half widths: exterior 0.115m, partition 0.09m; derive from actual source dimensions when those change.
- If upper finished floor is Y=`H` with slab thickness `T`, **interior** lower walls stop approximately at `H-T-0.02m`; **exterior** lower walls continue to the upper facade starting at `H`. Roof level follows the same distinction: interior partition heights versus uninterrupted exterior skin. Match collision and visuals; do not create an outside horizontal band while fixing internal clipping.
- Upper floor is split only around the **actual flight and turn**, and the safe approach is one continuous collidable slab up to the top step. Guard exposed edges with physical railings **except** the full-width stair exit; put upper rails in upper floor ownership. Don't make an overlapping second slab for a landing after filling the original hole.
- Structural acceptance includes the neighboring window, wall end, corridor route and slab after **every** moved partition. The fixed facade-window bands in `PoliceStation001` are a project-specific example, not a generic validation algorithm for all irregular buildings.

## Terrain3D placement contract — collidable ground floors, cutouts and basements

An isolated project test revealed repeatable integration mistakes: see root [LessonsLearned.md](../../../../LessonsLearned.md) **LL002**. A procedural building's finished Y=0 collidable ground slab must not be co-planar with an uncut Terrain3D surface: the two z-fight even when both meshes and materials are valid. Where a structure owns its floor collision, remove overlapping terrain beneath the **actual building floor coverage**, not merely the basement area, or use a deliberately offset and validated grade without leaving visible or colliding terrain inside rooms. A basement cutout alone is not sufficient when other ground-floor rooms share the terrain's elevation.

Terrain3D's hole paint operates on height/control-map **vertices**, with terrain triangles discarded when any vertex of the triangle is marked. Account for this expansion at the *outer* building footprint rather than putting a hole bit on the outermost wall vertex and cutting one extra cell outside the facade. Verify the surface meets the actual facade/window/door threshold and that exterior approaches are collidable and free of Z-fighting. Site-owned entrance thresholds may bridge the required small join while remaining separate from the building's reusable shell.

Respect saved terrain state. A one-time generator with `*.res` data **does not retroactively apply new hole bounds** after an old region is saved; choose an explicitly new versioned test directory or ask for controlled migration. Do not rewrite main world terrain to correct a standalone test scene. Validate **ground corridors**, **front/rear entrances**, **full perimeter**, **terrain collision** and **basement traversal**, not only the hole centre.

## Technical reference sections (retained)

## DOOR1 — author real swinging doors without compromising openings

`Assets/Objects/Static/Doors/Door001/` is the reusable physical door reference. `ConvenienceStore001` uses one at the **rear exterior exit only**; the front entrance and interior doorways remain physically open. Use an actual `HingeJoint3D` to a static anchor and a `RigidBody3D` door leaf (never rotate a static wall collider as if it were a hinge). Hinge root remains fixed relative to the building; the leaf collider and visible slab rotate together. The door's opening size, hinge position, physical dimensions, side clearance and sweep radius must be checked during the **shell and opening** stage before fixtures or GLBs. Treat the open and closed door leaf as part of the circulation test; fixed threshold and door swing reservations must leave room for motion. Keep door frames in the wall fade group; the leaf is nested in the same group for visual opacity but its physics body remains independent.

The door marks the moving leaf panel/handles with `CameraOccluder.DynamicFadeVisualMeta` so the wall group includes their opacity without caching a **stale static obstruction proxy**. Mark moving panels `structure_camera_blocker=false` for exterior cached triangle checks. Pair the door's collision shape with the live moving mesh through `FadeAwareAim.Link`: camera fade and mouse selection never disable physical bullet/player contact. Changing hinge positions during gameplay must not require per-frame mesh scanning; future interaction/DOOR2 can grab the two `HandleOutside/Inside` Marker3D nodes by physics reach and swing forces, not by directly teleporting the leaf. Door leaf's contact physics runs through Godot; no player movement changes are needed.

**First-phase local checks:** push the door from both sides while walking, verify hinge limits and sleeping, inspect rear-wall fade/AIM1 and door collider, ensure navigation from front to rear remains possible, and check no joint errors. The reference shop uses a 1.52m wide rear opening within a 2.6m service corridor; the main storefront door remains an empty aperture. No cupboards, sliding drawers, inventories, locks or mouse dragging yet.

## OV-W1 grouped-fade ownership — don't merge all meshes

A single continuous wall is a **logical unit**, not necessarily one triangle mesh. Keep independent `CollisionShape3D` boxes for lintels, piers, window sills and doors; geometry/collision remains correct through openings. Under a level's `structure_visual_role="walls"` subtree, create one `Node3D` per complete wall and call `StructureLayout.FadeGroup(group)` (sets `structure_wall_fade_group=true` metadata). Put all its wall sections, frames, future door leaf and wall-attached ornaments under it. One obstruction of *any* child affects every mesh belonging to that group. Adjacent/perpendicular walls get separate groups; small ungrouped details retain legacy per-mesh behavior.

Shared `CameraOccluder` creates a **separate precise obstruction proxy per child mesh**, but reuses one `CameraGroupFadeTarget` for every proxy in the group. This prevents an oversized single group bounds proxy fading from empty space while synchronizing the displayed opacity of all members. No names are hardcoded, no whole-building mesh merges, no extra fade timer/physics loop. Floor-level owner, roof role, outside whole-building reveal and the existing AIM1 collider-to-mesh links still apply. A closed door inside a wall group should fade with that wall but must keep its own collider; opening it must not be treated as solid cover across the doorway. Moving door geometry requires an explicit occlusion-proxy refresh/rebuild when it changes position (not a per-frame scan). **New imported meshes require real material/mesh ownership and explicit collision links**, not an opaque collision block over the full facade.

```csharp
var walls = StructureGeometry.Group(level0, "Walls");
StructureLayout.Visual(walls, "walls");
var north = StructureGeometry.Group(walls, "NorthWall");
StructureLayout.FadeGroup(north);
StructureGeometry.WallX(north, collision, "LeftPier", ...);
StructureGeometry.WallX(north, collision, "DoorHead", ...);
StructureGeometry.WallX(north, collision, "RightPier", ...);
// Put doorframe / optionally movable door and wall decorations in north's visual group.
```

**BW1 convenience store reference:** `ConvenienceStore001/C/ConvenienceStoreMesh.cs` creates FrontWall, RearWall, WestWall, EastWall and the StockroomPartition group. It preserves the real apertures and existing building physics; only the fade ownership changes. Its `StructureClearance` authoring phase reserves a front entrance, interior stockroom door, rear exit, front/side window apertures and three connecting path volumes; collidable shelf/counter/crate placements are guarded by full-bounds checks. Future AI-driven GLB placement should consult `Generated/ClearanceVolumes` through `StructureClearance.AllowsPlacement(clearanceRoot, candidateBounds)` before instancing an object (candidate bounds in building-local coordinates). Reservations describe protected openings/routes and are not physical blockers; do not copy them into collision geometry.


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

- **walls:** authored groups fade as complete walls; ungrouped legacy wall meshes still fade individually. Lower-storey walls restore upstairs.
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

Future AI can furnish buildings using stable asset prefabs rather than building furniture into each procedural structure's `C/` mesh generator. Put fixed furniture in the appropriate floor furnishing/`contents` presentation subtree **after** reserving window/door openings, stairs and exit-to-exit routes; use full-bounds clearance checks, not visual guesswork. Loose physics props need authored spawn anchors and an independent authoritative prop owner/state so building art regeneration and top-down fading do not own/destroy physical movement. A faded item never loses its collider. Don't generate or place furniture automatically until requested.

## OV1 — accurate exterior blockers

Exterior whole-building fade now requires five camera-projected player samples to hit actual cached opaque walls/roof/deck triangles; bounds/radius alone do not trigger it. Current outside opacity and all indoor presentation stay unchanged. Collision is not consulted or changed. Keep these visual roles accurate, static and separate from contents/stairs. Plain opaque native materials work automatically, including visual-only gables and roof slabs. Current coloured window panes are opaque and therefore count; actual open door/window geometry remains clear.

Split transparent/cutout/custom shader surfaces away from opaque shell meshes: mixed meshes containing a nonopaque surface are conservatively skipped; unknown shaders/no faces warn and fail open. Optional `structure_camera_blocker=false` on mesh/ancestor excludes decoration. This does not make contents into blockers. Cosmetic camera transparency does not affect the cached decision; explicit hidden geometry is skipped. Refresh/Rebuild after material/geometry/relative transform edits. GPU deformation/alpha pixel coverage is not evaluated. Complex art exceeding the 16,384 triangle-test budget per check stays solid with a once-only warning; use simpler static structural geometry. See CAMERA/Occlusion/BUILDING_FADE_IMPROVEMENT.md for the audit and local acceptance. OV2 actor hiding is separate.

## AIM1 collision-to-visual mapping for mouse aiming

The existing `StructureGeometry.Box(visual, body, ...)` automatically pairs its `CollisionShape3D` with the created `MeshInstance3D` using `FadeAwareAim.Link(shape, mesh)`; Building001's private Box helper does the same. `CameraOccluder` exposes whether that **exact visual** is currently faded (camera obstruction, floor/roof presentation, or outside whole-building opacity). The top-down cursor ray skips only the paired faded physical shape. Physical collision never changes: guns can hit the faded wall; opaque walls sharing the same StaticBody3D still block mouse aiming.

**For imported/custom future buildings:** if collision is built separately, pair every intended faded colliding part at authoring/spawn time with its specific visual using `FadeAwareAim.Link(collisionShape, matchingMeshInstance)`, and place the visual inside the correct `structure_visual_role` group so a `CameraOccluder` owns its fade. Avoid one entire building collision shape for many independently faded walls: individual mapping is required for accurate targeting. An unpaired collider remains targetable/solid to the mouse, by design. Do not change collision layers or bullet ray masks to achieve visual-only aiming.

## CS2: expanded reference-store authoring quick check (2026-10-28)

Before copying this technique to other buildings, use `ConvenienceStore001` as a **two-rear-room, 24×16m** reference alongside the existing single-storey BW1 grouping rules. It demonstrates **one storefront door, one exterior rear door, three interior openings, a full-length central escape corridor, a front checkout near unobstructed large window apertures, and six retail shelf rows**.

- Physically split walls into real spans around every opening; do **not** join window-spanning invisible collision or put frames, signs, clocks or furnishings in the holes.
- Group each continuous wall (including door lintels and frames) for top-down fade. The retail-to-rear partition is one logical wall group, while the two corridor dividing walls are separate groups. Do not globally fade the whole level on a single wall obstruction. Preserve AIM1 collision shape links.
- Reserve **every** exit route and opening before furniture: now 14 building-local `StructureClearance` volumes, including the route front door → central rear corridor → rear exterior door and separately reachable west/east rear rooms. Check complete fixture bounds; here all 12 authored major placement candidates pass the default-size intersection test. This is an authoring check, not a navigation/pathfinding guarantee.
- Keep the structural/building prefab independent from the assembled **Site**. Changing the default building dimensions may require moving the **site Store instance and markers**, extending a graded terrain platform, and moving nearby spawn positions. A bigger structure must not swallow fixed hedge/parking geometry; a visual-only flush approach can bridge the sidewalk gap if native terrain already supplies collision.
- **Finish** by testing roof/exterior/individual wall group fade, FPS opaque behavior, physical doorway traversal, opening sightlines, AIM1 cursor targeting versus real bullets, and terrain support. Only after those tests add nonessential GLBs and cosmetic clutter. Do not infer these runtime outcomes from a source-only check.
