# Building design guide — architectural layout and structural quality

**Mandatory first read for every new building.** This document defines **what makes a convincing, walkable and visually clean building**. The accompanying [technical authoring contract](BUILDING_AUTHORING_README.md) defines **how Godot/C# must represent it**: standalone scenes, editor previews, geometry/collision, top-down fades, haze exclusion, metadata, dynamic doors and performance. Both apply; neither substitutes for the other.

**Workflow:** root project README → CodePreferences.md → **this design guide** → [technical contract](BUILDING_AUTHORING_README.md) → the nearest reference building and its actual source. Do not place the prefab in the active world or start furniture without separate authorization.

## Architectural intent — design real spaces, not repeated boxes

- **Purpose before walls.** Choose whether the building is residential, commercial, industrial, public or abandoned; establish why it has its entrances, public rooms, restricted areas and service routes. A larger footprint does not justify multiplying identical rooms.
- **Human circulation first.** Draw an understandable route from the exterior entrance to reception, corridors, key rooms, stairs, upper landing and emergency exits. A gap in a wall is not automatically a usable doorway. Check both walking and side approaches, headroom and door swing.
- **Hierarchy and asymmetry.** Mix large open rooms, medium offices and small service/utility spaces. Use sensible adjacency and intentionally varied room depth. A police station should read as lobby → administration → staff/secure spaces, not a symmetrical grid of boxes.
- **Coordinate windows with rooms.** Decide window locations with the elevations and place wall partitions on solid piers; never let walls or furniture cross the actual glazing.
- **Finish every junction.** An internal wall must meet another wall's *face*, terminate intentionally with a column/return, or contain a genuine framed opening. No dangling hallway ends, projecting nubs, narrow wedges, unintentional slabs or floating landing corners.
- **Architecture in both views.** FPS is the primary gameplay reference; inspect heights and reachability at eye level. Top-down remains supported, so floors and individual walls must be legible when presented/faded correctly. The metadata and camera mechanics live in the technical guide.

## Decide these dimensions before writing mesh code

| Element | Mandatory design decision / floorplan check |
| --- | --- |
| Footprint/site | Exact exterior outline, foundation support, grade, storeys, roof, access and intended connection to surroundings |
| Doors | Real clear openings, frames, intended swing, no blocking rail/step/wall and continuous routes through the building |
| Main paths | Generally ~2 m clear width and ≥2.2 m headroom; test against actual player/body and movement behaviour |
| Stairs | Practical location, lower approach, turn landing, upper arrival, railing edges and floor cutout planned as one connected path |
| Stair approach | At least 2.0 m flat and unobstructed before the bottom tread **along the route**; also 2.0 m usable side approach if entered from the side; full headroom and no rail intrusion |
| Upper floor opening | Cut **only** the real staircase/turn void; fill the remainder with one continuous solid floor; protect exposed rims without blocking the full upper stair exit |
| Windows | Real apertures in geometry/collision; partitions terminate on solid facade piers with a 0.5 m authoring margin from window-opening edges |
| Floor interfaces | Internal walls stop below the next slab; exterior walls remain continuous between storeys and meet the roof |
| Lighting/decor | Choose rooms and ceiling heights first; mount lights and later furniture after geometry is accepted, leaving some rooms intentionally dark |

These are **project authoring defaults**, not legal building-code assertions. If a special building needs another standard, document the reason and prove player traversal.

## PoliceStation001 failure register — problems future designs must prevent

The station was intended to be an early structural stress test, but it exposed recurring problems in the authoring process. Keep this table visible during every new build or later architectural change.

| Order | What went wrong | Prevention gate |
| --- | --- | --- |
| 0 — First editor preview | The building had a scene/Inspector but the generated model was invisible | Tool scripts must generate inside the scene editor, expose Refresh after a C# build and make the generated root inspectable; **technical guide** |
| 1 — First floorplan | Back-corner straight staircase felt misplaced; too many repeated rectangular rooms | Design entrance/reception/stair core and public-to-private hierarchy before adding room walls |
| 2 — First U-stair | Stair flight/railings crossed their own entrance; insufficient flat floor at the first tread | Reserve complete **door → unobstructed flat landing → first step** path in 3D, including lateral clearance |
| 3 — Facade partitions | Structural interior walls visibly intersected windows | Partition exterior wall junctions must land on solid masonry/piers, not window glass |
| 4 — Ugly protruding walls | A partition projected past a perpendicular wall; other ends were isolated | Join walls flush to the receiver's **near face**; do not stop corridor walls in open space |
| 5 — Interior upstairs clipping | Lower-floor wall tops poked through the second-floor slab | Interior partition top goes just below the slab underside, with matching collision |
| 6 — Exterior seam regression | Shortening **all** walls opened a horizontal exterior gap between floors | Treat exterior walls independently: facade stays continuous up to the next facade/roof, while only interior partition tops are shortened |
| 7 — Unsafe upper stair opening | Overlarge upstairs slab cutout left holes beyond the actual flight/landing; no proper barriers | Cut the real stair void, fill the safe apron with collidable floor and guard all exposed edges **except** the clear top exit |
| 8 — Upstairs loose ends | Corridor walls stopped without meeting another wall or stuck beyond their crossing wall | Connect walls to an intended cross-wall/return and cut a genuine doorway wherever the join would block circulation |

**Never claim visual or physics acceptance from a successful C# commit.** Inspect the standalone Godot scene at overhead and eye level, and physically walk the stairs/doors when a permitted playtest is available. Each new revision must re-check affected neighboring walls, window lines, floors and railings.

## Original shell-and-circulation requirements (retained from the former mixed guide)

## Closed stair construction and anchored rails — PS1.7 (2026-10-31)

First-person structural stair inspection must include **side and underside views**: a flight may be walkable but still show daylight between thin treads or floating handrail posts. When steps use one smooth Godot slope collider beneath separate noncolliding step meshes, put each visual **riser at the elevation boundary between successive treads**, add thick visible treads and a continuous noncolliding backing/stringer, and never replace the walking ramp with dozens of abrupt step colliders just to close a visual gap. Preserve clearance at turns and room doorways.

A 3.2m tread extends only 1.6m to each side of the flight centre. Rail/post X coordinates outside that envelope require intentional connecting structural stringers; otherwise posts float beside the stairs. Align rails over the tread/stringer, anchor posts at real stair or landing elevations, and compute their tops against the inclined handrail rather than using a fixed post height independent of rise direction. Verify both positive- and negative-rise flights, including bottom and top landing posts, rail collider contact and accessible stair width.

For below-ground detention corridors, author genuinely traversable central routes and actual cell door **openings** with corresponding *visual and physical* gaps. Do not replace jail fronts with a full invisible collider through a doorway, or end cell partitions short of their barred wall/back wall. Check that cell bars, service-room walls, foundation perimeter and rear hall connect without dangling stubs. Add functional doors, prisoners or furniture only in a separately authorized pass.

## Detention bars and cell front detailing — PS1.9 (2026-10-31)

A detention area built from generic large rectangular primitives can appear like a crude fence rather than a believable set of cells. Give bars an intentional **slender steel profile**, regular rhythm and lightweight horizontal reinforcement without replacing them with a solid invisible collider. Preserve a separate physically walkable **full-width doorway** below its lintel, and confine any barred transom or cell-gate placeholder to geometry *outside* the doorway's clearance volume. A door posed 90 degrees open is **visual-only** until a separate door/interaction phase authorizes its physics and hinge state.

Add small but restrained architectural distinctions (lower wall finish, a vent or grille, small cell designation) rather than duplicating an entire furniture subsystem. Keep static materials shared across cells; batch or simplify if the jail population later grows. Validate bars from first-person corridor eye height, actual capsule passage through every cell doorway and alignment at solid rear walls and dividers. Do not let room trim create new corner nubs or coplanar Z-fighting. See PoliceStation001 PS1.9.

## Open stair railings and structurally supported turns — PS1.8 (2026-10-31)

A thick continuous stair `SideStringer` can unintentionally hide the lower half of every banister even when every post is generated. Keep the backing **thin and beneath the steps**, make balusters evenly spaced on both sides, and embed their ends in the backing/treads and handrail. Inspect from the stair sides and below, not only from the player eye height while climbing. Avoid railing posts extending past the actual first/last step.

Dimension U-turn platforms to the **union of the adjacent stair-flight edges** rather than an arbitrary oversized width. For a landing suspended over the basement, align visible structural supports precisely from basement floor to platform underside; keep supports within the slab perimeter and out of stair traversal volume. Recheck all floor/ceiling interfaces, wall nubs, rail/slope joins, corner posts and collision visually after each geometry change.

## FPS surface readability — PS1.6 material contrast rule (2026-10-31)

When a procedural building uses flat-shaded box meshes, **do not give the floor, underside of slab (ceiling), wall and stair treads nearly the same colour/value**. It can conceal tread depth and floor boundaries even when the collision meshes are correct. Use a deliberate hierarchy: ceiling brightest, walls lighter than floors, finished floors darker, stair treads clearly distinct from paler risers, and a thin coloured step nose on every tread. PoliceStation001 uses **muted dark blue** as an understated nosing accent, not hazard-yellow or emissive stripes.

Prefer **separate configurable material colours** to scattering constants in procedural mesh code. For a box slab where one material covers every face, place an **underside-only, visual-only finish** a small distance below the original physical slab rather than changing the upper walking surface or adding another collider. The underside finish must match all existing **shaft holes**, must inherit the correct upper-deck/roof visual ownership, and must not obstruct light fixtures, stairs or headroom. Likewise, step-edge strips should sit just above the tread, be *visual only*, and leave the existing tread/ramp collider completely untouched. Check from a **descending FPS player's eye height**, not only the overhead editor preview. See PoliceStation001 PS1.6 and root LessonsLearned LL003.

## Lodge layout fixes — door under stairs, tread/readability, furnishing anchors (BL2.3; 2026-10-31)

A door cut into a partition that runs beside a stair may be **visually and physically beneath the climbing flight** even if the door gap is valid in the wall itself. Compare the *actual side-door opening Z range and headroom* against the full stair run/riser envelope; move the real opening and header together ahead of the lower tread or beyond the far landing, never conceal the overlap with a frame or partial wall.

A 22-tread visual stair can look like thin floating boards. Choose a sensible **visual step count and tread thickness** while retaining an intentional, smooth walkable ramp collider; never put tread and riser top faces exactly coplanar, as this produces Z-fighting and poor depth cues. Every change must keep the same top/bottom landing and stairwell aperture. For 4m rise over 5.5m run the 16-step/14cm-tread test case yields 25cm rise and 34cm run; a literal 11-step version would be very steep per step. Validate FPS movement and appearance locally.

Furniture placement must use the **actual solid wall and window bays**, not just approximate room centres. In the rear office, an `OfficeNotice` can overlap book shelves even though neither is a real window; distinguish artwork from a genuine window and relocate it instead of modifying glazing. Use wall-relative shared anchors for solid furniture AND its bedding, books, door panel and decorations; confirm cabinet backs touch near a wall face, furniture does not intrude into protected windows or the doorway, and a bedroom bed is visually centred on a suitable uninterrupted wall bay. Reserve full upstairs window/stair approach bounds before creating collidable fixtures.

## Lodge wall joins, deck ownership and porch collisions — Building002 BL2.2 (2026-10-31)

A wall endpoint measured with a generic 0.25m inset can visibly stop short when its receiving shell is only 0.23m thick; end partitions at the **receiving wall's actual interior face** (half its thickness), with a tiny perpendicular intersection only if needed to hide cracks. Inspect near door returns, side-room joins and shared floor elevations. Do not cover true door/window openings with extra fix-up boxes.

A decorative Terrain3D-supported interior floor and an exterior veranda must not share any opaque coplanar area at identical finished elevation. Stop each deck at a deliberate handoff plane and use a small intentionally raised threshold to bridge the seam. Resize decorative floorboard seams with their owning deck. Do **not** solve coplanar overlap using material z-offset or a second full-size floor.

Upstairs flat walls beneath a gabled roof must follow the *actual* pitched eave/roof underside, not stop at a convenient fixed wall height. Close intentional solid partitions up into the gable with one triangle rather than multiple overlapping gables; keep exterior gable faces as **wall-owned** so roof hiding doesn't expose holes. Chimney/fascia should sit atop the roof plane/ridge, never protrude through an interior ceiling. Ensure the two roof parts have distinct ownership and no duplicated coplanar faces.

If porch railing/bench geometry represents physical blockers in FPS, author corresponding collision for every seat, back, handrail and upright (or an intentional simplified enclosing proxy); don't leave collidable legs supporting ghost seats. Orient seat backs deliberately relative to the intended +Z outward viewing direction and check that they remain clear of walkway access.

## Lodge FPS circulation and finished wall tops — Building002 BL2.1 (2026-10-31)

The Building002 first-person walkthrough reproduced preventable PoliceStation001 issues: **the first tread was too near the external door**, furniture crowded the entrance or blocked window sightlines, and ground-storey partitions did not reach the upper-floor deck. These are structural authoring failures even when the game builds.

Reserve the full entrance-to-stair **flat** approach, not just a minimum distance between two node origins. A relocated straight flight requires the **upper stairwell cutout, railings and landing** to use the same start/end coordinates; check that the final ramp remains walkable and does not run into the receiving rear-room partition. Put real furniture and its visual accessories under *one shared placement coordinate*, then check complete bounds against doors, both front and side window sightlines, and routes. A window must be viewable past a stove, cabinet or locker—not only free of a wall collider. For the lower walls, derive their top from the **actual upper slab's underside** (h minus slab thickness minus a tiny separation), not a fixed independent 2.95m wall height; retain doorway header elevations and don't punch into the upper deck. See Building002 README BL2.1 and root LessonsLearned LL008.

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

## Mandatory architectural authoring order — layout first, decoration last

1. **Plan the footprint and circulation.** Choose floor levels, terrain alignment, entrances/exits, window apertures, stairs, landings and the direct walkable routes joining doors to doors. Draw/declare minimum clear human-width corridors before adding shelves, furniture or art. Keep emergency exit routes and stair headroom clear in both views.
2. **Build the structural shell:** foundations, floor/deck, **separate solid wall spans around genuine door/window holes**, roof and stair geometry, with matching collision geometry. A painted glass texture or invisible blocker is **not** a substitute for an actual opening. Door and window openings need explicit unobstructed vertical as well as horizontal space.
3. **Declare logical wall assemblies:** each physically continuous wall (including sections flanking a doorway, lintel/header, frames and compatible decorations) gets a **single fade group node**. Different perpendicular walls must have separate groups. Doorframes belong to their wall group; future door leaves may join visually but must retain independent transforms/physics for interaction. Moving leaves will need explicit obstruction-proxy refresh when their transform changes.
4. **Reserve clearance volumes** before constructing furnishings: door thresholds and swing/interaction zones, window apertures, the main route through the interior, stair footprints/landings and corridors between exits. A reservation covers the intended full volume, not just floor pixels. Reuse the cached `StructureClearance` helper and persist named reservation markers under `Generated/ClearanceVolumes`.
5. **Add major fixtures only after clearance validation:** counters, shelving, beds, wardrobes, built-in appliances. Reject/reposition any fixture whose complete bounds intersect a reserved volume. Walking paths and door/windows always take priority over aesthetic placement. Test that the player body can traverse the complete sequence of openings.
6. **Add decorative mesh art:** lights, wall pictures/clocks, signage, trim and surface details, then **last** add reusable static GLB Objects and pickup Props, only in safe locations. Do not put any art, collision, hanging pictures, shelves or furniture inside a window/door opening or over a doorway, and keep window sight lines visibly open. Attach wall-mounted art to the correct fade group if it should fade with the wall. Loose physics items must remain outside generated-wall ownership.
7. **Validate from both camera perspectives** at normal zoom and near walls: circulation is unobstructed, openings are clear, each complete wall assembly fades coherently, roofs/whole-building exterior fading still work, and AIM1 visual targeting skips only faded paired colliders. Verify bullets, movement, zombie sight and FPS stay physically correct. Correct the *layout first* rather than adding camera-visibility exceptions for a bad layout.

**Clearance starting recommendations (authoring values, not global game rules):** ~2.0m clear width for primary aisles and most connecting corridors, ≥2.2m vertical clearance through doors and occupied walk paths, and extra space at stair landings and swinging doors. Use the actual player collider/dynamics and prefab dimensions to validate. Smaller/larger building layouts may need explicit alternative values, but must still provide continuous collision-free circulation.

## Mandatory pre-furnishing acceptance checklist

- [ ] The building has a recognizable real-world purpose and coherent room hierarchy; layouts are not a grid copied without architectural reasoning.
- [ ] A continuous, properly clear entrance → public/staff rooms → corridors → stairs/upper levels → exits route exists.
- [ ] Every door and window is a real opening with matching physical clearance; no rail, shelf, partition or collider inside the opening.
- [ ] The first stair tread has a level approach, a turning landing if applicable, a protected upper arrival and safe headroom. Collision and presentation agree.
- [ ] The upper floor is genuinely solid outside the **actual** stair opening; no extra missing strips, overlapping landing slabs or unguarded edges.
- [ ] Every wall junction is clean, attached or deliberately terminated; no short tails, walls crossing windows, or floating partition segments.
- [ ] Interior wall tops do not clip through decks/roofs; exterior walls have **no storey-to-storey horizontal gaps**.
- [ ] Inspector `[Tool]` preview builds reliably and has all relevant floor/roof/exterior inspection states, or another explicit isolated scene inspection path.
- [ ] Only then request the [technical acceptance gate](BUILDING_AUTHORING_README.md) for fading, haze suppression, targeting, collisions and performance. No furniture until architecture is accepted.

## Examples

[PoliceStation001](PoliceStation001/README.md) records the structural fixes behind the failure table; [ConvenienceStore001](ConvenienceStore001/README.md) demonstrates a smaller single-storey shell and [Building004](Building004/README.md) provides a two-storey reference. Their current code is authoritative over historical notes; local acceptance is separate from source implementation.
