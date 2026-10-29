> **GH1 authoring rule:** Every solid building or roofed open-air canopy that should suppress local drifting ground haze must supply explicit `StructureLayout.Occupied(node, Aabb)` metadata for its actual covered ground footprint. The startup haze capture finds these zones automatically inside `WorldActorsRoot/PersistentPropsRoot/Structures`; **do not** tag an entire shopping-site root that includes car parks/roads. Separate rectangles may overlap for wings, pump canopies or covered walkways. Local XZ rotation is supported; fade 1.25m outside each rectangle by default. Future dynamically constructed buildings must explicitly refresh `GroundHaze.SetBuildingExclusions`. This concerns only `GroundWisps.gdshader`, not Sky3D baseline/distance fog or building collision.

# Building authoring contract — give this to future building-design chats

> **BW1 / CS2 reference standard (implemented in source, local Godot validation pending):** start with `ConvenienceStore001` for **logical wall fade assemblies, aperture/walkway clearance and the staged furnishing process**. `Building002` remains the multi-floor reference; migrate other structures later, only after the store has been visually accepted. Existing unmarked walls keep per-mesh fading and do not need immediate rebuilding.

## DOOR1 — author real swinging doors without compromising openings

`Assets/Objects/Static/Doors/Door001/` is the reusable physical door reference. `ConvenienceStore001` uses one at the **rear exterior exit only**; the front entrance and interior doorways remain physically open. Use an actual `HingeJoint3D` to a static anchor and a `RigidBody3D` door leaf (never rotate a static wall collider as if it were a hinge). Hinge root remains fixed relative to the building; the leaf collider and visible slab rotate together. The door's opening size, hinge position, physical dimensions, side clearance and sweep radius must be checked during the **shell and opening** stage before fixtures or GLBs. Treat the open and closed door leaf as part of the circulation test; fixed threshold and door swing reservations must leave room for motion. Keep door frames in the wall fade group; the leaf is nested in the same group for visual opacity but its physics body remains independent.

The door marks the moving leaf panel/handles with `CameraOccluder.DynamicFadeVisualMeta` so the wall group includes their opacity without caching a **stale static obstruction proxy**. Mark moving panels `structure_camera_blocker=false` for exterior cached triangle checks. Pair the door's collision shape with the live moving mesh through `FadeAwareAim.Link`: camera fade and mouse selection never disable physical bullet/player contact. Changing hinge positions during gameplay must not require per-frame mesh scanning; future interaction/DOOR2 can grab the two `HandleOutside/Inside` Marker3D nodes by physics reach and swing forces, not by directly teleporting the leaf. Door leaf's contact physics runs through Godot; no player movement changes are needed.

**First-phase local checks:** push the door from both sides while walking, verify hinge limits and sleeping, inspect rear-wall fade/AIM1 and door collider, ensure navigation from front to rear remains possible, and check no joint errors. The reference shop uses a 1.52m wide rear opening within a 2.6m service corridor; the main storefront door remains an empty aperture. No cupboards, sliding drawers, inventories, locks or mouse dragging yet.

## Mandatory collision-free shell checks — stair approaches and window junctions (PS1 correction)

**Hard rules for every future building, including editor-preview-only prefabs, not optional decoration advice.** The police-station PS1 visual review exposed two authoring failures: a stair flight/railing obstructed its own entrance and an internal partition met an exterior window instead of a solid pier. These **must be prevented before furnishing**.

1. **Stairs must have a real landing/approach, not just an opening.** Reserve at least **2.0m of flat, unobstructed floor** immediately before the **first tread** (measured along the direction of approach), plus at least 2.0m of usable lateral approach if entering the flight from a side doorway. Include handrails, posts, doorframes, doors in both open/closed positions, and the actual player collision envelope. The first riser, underside of the second flight and any railing cannot overlap either approach volume. These are project baseline authoring minima, not a universal building-code claim.
2. **Doors feeding a staircase must not open directly into the step, ramp or a guardrail.** Provide a dedicated level vestibule or sufficiently deep landing, protect the entire door-swing footprint and headroom, and make the walk from door → flat approach → stair → turn landing → upper landing continuous. A 2m straight-line separation to a door centre alone does not replace a collision-free 3D traversal test.
3. **L/U stairs need independently open floor shafts.** Split the upper deck VISUAL AND COLLISION into rectangles around the complete shaft, not a noncolliding painted cutout; ensure the turn landing is wide and deep enough for the player, meets both slopes, and the upper landing joins a walkable upper deck. Guard the *actual* exposed upper-floor void edges without blocking the landings. Guardrails belong with the correct upper floor visibility group so ground-floor/editor top-down visibility is not misleading.
4. **No wall may terminate inside a window aperture** or intersect window glass, frames, or reserved sightline clearance. Before building a partition that joins an exterior wall, compare its junction against the **actual** window opening on that level. Junction must land on a solid structural pier, with at least **0.5m from the aperture edge** as the default authoring margin (increase for larger frames/thicker partitions). This applies to ground and upper floors, all front/rear/side facades, and future procedurally varied layouts. Where it conflicts, move the partition or window during layout; never hide the collision/mesh overlap.
5. **Validate structural features against one another**, not just furniture: window-to-partition, door-to-stair, rail-to-door, stair-to-floor, landing-to-floor, wall-to-door and door-swing-to-wall. `StructureClearance.Allows(...)` currently rejects participating furniture placements; its nonphysical metadata **does not automatically reject walls or stairs**. Each builder must actively check its authored shell geometry before construction or reject invalid layout with a clear error. After changes, inspect the Godot scene in **both** GroundFloor and UpperFloor views at door/window height, not just from overhead.
6. **Keep named reservations** in `Generated/ClearanceVolumes` for full-height doors/windows, floor-level bottom stair aprons, turn landings, both upper exits and exit-to-exit walkways. Reservations protect later props, but are **not substitutes for the structural checks above**. Show the actual protected minima in each structure's README.

**PoliceStation001 implementation example:** `ValidateStairApproach` enforces the 2m side/front geometry; `InteriorPerimeterPartitionX` and `EnsureClearPerimeterWindowJunction` reject side-wall partition locations that hit the five known 3.6m window bands. That is a local first-pass guard, **not** a general spatial solver for all arbitrary buildings: new authoring code must check every relevant facade/level and all geometry rather than relying on those five positions.

## Mandatory multi-storey staircase exit floors and connected partitions (PS1.4)

- **Cut only the true stair opening, not the whole rectangular construction bay.** For U/L flights, calculate the upper-floor deck around the real upper risers and turn-landing void. **Fill the full, flat arrival apron with matching visual slab AND collision**, meeting the last stair tread at exactly the same walking elevation. Never retain an overlapping/coplanar second landing slab on top of a newly extended upper deck. Inspect open shafts in UpperFloor AND GroundFloor preview. Don't bridge the actual step/turn clearance below the floor with a solid patch.
- **Guard the rim but never the walking exit.** Add colliding guardrails/walls on every exposed upper-floor hole edge. A stair exit cut through the rim must remain at least the full clear stair width (including rail offsets), with a continuous safe transition onto the deck. Build each side as its own guard segment, and explicitly check no rail occupies the top tread or its landing approach. Avoid a guardrail across a doorway or camera-visible walkway.
- **No upper corridor/free partition tails.** A hallway wall must terminate at an exterior/room wall or deliberate structural return, trimmed to the recipient's near face. Avoid ending it 0.4m past a cross wall or in empty floor space. When closing a loose corridor end using another wall, cut a genuinely open, sufficiently wide door if it separates a stair arrival route or room; validate the entire connected circulation path rather than sealing it accidentally. Structural body collision must match the openings and the visual arrangement.
- **Authoring check:** list the upper-level cutout bounds, tread-to-slab junction and protected exit width in the building-local README; encode deterministic assertions/throw on invalid parameters before creating geometry. Recheck doors, windows, stairs, safety barriers and walkable floor together after changing any hallway partition.

## Mandatory floor interfaces and clean wall junctions — PS1.2 correction

**Visual and collision geometry must terminate at the adjacent solid's near face.** Do not stop two intersecting walls at their *centrelines*: even when their collision overlaps safely, decorative geometry will show unwanted projecting wall stubs. Trim horizontal partitions to the side facade's interior face and to the hall partition's near face; likewise trim perpendicular wall runs to the receiving wall's near face. Check every wall-to-wall and wall-to-facade junction on **both storeys**. Keep true apertures separate: never solve a wall nub by adding a new wall span that blocks a doorway or a window. Avoid overlapping collinear spans and free ends that stop a few centimetres short of a junction.

**Differentiate INTERIOR partition tops from the EXTERIOR facade.** If the next deck walking surface is `Y=H` with thickness `T`, its underside is `H-T`: **interior room partitions** and their colliders should end just below that underside (e.g. `H-T-0.02m`), preventing tops from poking through the upper floor. **Exterior walls must continue to H**, where the upper facade starts, or a full-height continuous facade/spandrel must deliberately bridge the deck band. The upper floor slab meets the **inside face** of the exterior shell, but it does not visually replace exterior wall cladding. Likewise, top-floor **interior partitions** may stop just below the roof underside, while the **exterior facade should meet the roof** with no visible band or hole. Apply the same rules to paired collision. **Never shorten exterior walls to cure interior clipping:** that caused the police-station PS1.2 horizontal facade gap. Do not move the second-storey walking elevation or disable floor collision. Inspect all sides at eye height in Exterior mode, plus the floor plate in GroundFloor/UpperFloor preview. Keep stairwell openings and guardrails separate; permit only intended structural intersections.

**Interior lighting pass:** reuse a standalone small fixture with one native downward `SpotLight3D`, shared master settings and lightweight white-emissive panel instead of placing outdoor streetlamp scenes in rooms. Mount fixtures just below the real slab/roof underside, group each fixture with its floor's semantic `contents` role, use fewer long corridor fixtures spaced along the hall, and selectively light rooms. Avoid dynamic shadows, outdoor cones, per-light ticks and excessive room light density in large performance-test buildings. Verify actual light spill and budgets in Godot before adding further fixtures.

## Mandatory authoring sequence — layout first, decoration last

1. **Plan the footprint and circulation.** Choose floor levels, terrain alignment, entrances/exits, window apertures, stairs, landings and the direct walkable routes joining doors to doors. Draw/declare minimum clear human-width corridors before adding shelves, furniture or art. Keep emergency exit routes and stair headroom clear in both views.
2. **Build the structural shell:** foundations, floor/deck, **separate solid wall spans around genuine door/window holes**, roof and stair geometry, with matching collision geometry. A painted glass texture or invisible blocker is **not** a substitute for an actual opening. Door and window openings need explicit unobstructed vertical as well as horizontal space.
3. **Declare logical wall assemblies:** each physically continuous wall (including sections flanking a doorway, lintel/header, frames and compatible decorations) gets a **single fade group node**. Different perpendicular walls must have separate groups. Doorframes belong to their wall group; future door leaves may join visually but must retain independent transforms/physics for interaction. Moving leaves will need explicit obstruction-proxy refresh when their transform changes.
4. **Reserve clearance volumes** before constructing furnishings: door thresholds and swing/interaction zones, window apertures, the main route through the interior, stair footprints/landings and corridors between exits. A reservation covers the intended full volume, not just floor pixels. Reuse the cached `StructureClearance` helper and persist named reservation markers under `Generated/ClearanceVolumes`.
5. **Add major fixtures only after clearance validation:** counters, shelving, beds, wardrobes, built-in appliances. Reject/reposition any fixture whose complete bounds intersect a reserved volume. Walking paths and door/windows always take priority over aesthetic placement. Test that the player body can traverse the complete sequence of openings.
6. **Add decorative mesh art:** lights, wall pictures/clocks, signage, trim and surface details, then **last** add reusable static GLB Objects and pickup Props, only in safe locations. Do not put any art, collision, hanging pictures, shelves or furniture inside a window/door opening or over a doorway, and keep window sight lines visibly open. Attach wall-mounted art to the correct fade group if it should fade with the wall. Loose physics items must remain outside generated-wall ownership.
7. **Validate from both camera perspectives** at normal zoom and near walls: circulation is unobstructed, openings are clear, each complete wall assembly fades coherently, roofs/whole-building exterior fading still work, and AIM1 visual targeting skips only faded paired colliders. Verify bullets, movement, zombie sight and FPS stay physically correct. Correct the *layout first* rather than adding camera-visibility exceptions for a bad layout.

**Clearance starting recommendations (authoring values, not global game rules):** ~2.0m clear width for primary aisles and most connecting corridors, ≥2.2m vertical clearance through doors and occupied walk paths, and extra space at stair landings and swinging doors. Use the actual player collider/dynamics and prefab dimensions to validate. Smaller/larger building layouts may need explicit alternative values, but must still provide continuous collision-free circulation.

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
