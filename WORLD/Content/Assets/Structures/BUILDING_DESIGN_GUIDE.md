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

## FPS surface readability — PS1.6 material contrast rule (2026-10-31)

When a procedural building uses flat-shaded box meshes, **do not give the floor, underside of slab (ceiling), wall and stair treads nearly the same colour/value**. It can conceal tread depth and floor boundaries even when the collision meshes are correct. Use a deliberate hierarchy: ceiling brightest, walls lighter than floors, finished floors darker, stair treads clearly distinct from paler risers, and a thin coloured step nose on every tread. PoliceStation001 uses **muted dark blue** as an understated nosing accent, not hazard-yellow or emissive stripes.

Prefer **separate configurable material colours** to scattering constants in procedural mesh code. For a box slab where one material covers every face, place an **underside-only, visual-only finish** a small distance below the original physical slab rather than changing the upper walking surface or adding another collider. The underside finish must match all existing **shaft holes**, must inherit the correct upper-deck/roof visual ownership, and must not obstruct light fixtures, stairs or headroom. Likewise, step-edge strips should sit just above the tread, be *visual only*, and leave the existing tread/ramp collider completely untouched. Check from a **descending FPS player's eye height**, not only the overhead editor preview. See PoliceStation001 PS1.6 and root LessonsLearned LL003.

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
