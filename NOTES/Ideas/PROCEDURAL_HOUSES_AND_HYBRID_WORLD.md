# Idea — Editor-placed procedural buildings and a hybrid open world

> **STATUS: IDEA / REFERENCE ONLY.** Not an approved feature plan or implementation task. Do not build any of this until explicitly requested. This records the October 2026 discussion about War Machine's future world-creation workflow. The current small sandbox remains the place to test terrain, foliage, rocks, structures, movement and visibility first.

## The goal

Make it possible to **fly around a large Terrain3D landscape in Godot's 3D editor**, find a good place for a structure, and **drag an editable `ProceduralHouse.tscn` into the scene** instead of building each house by hand.

Desired authoring experience:

1. Explore the landscape in the Godot editor, rather than only running around in-game.
2. Drag/place a reusable house or other procedural-structure scene at an authored location.
3. Snap the scene to saved Terrain3D height and check the footprint/slope.
4. Immediately preview the generated building in the editor.
5. Adjust a **seed**, building style, footprint, number of floors, porch/garage, window pattern, furnishing density, etc., and press **Regenerate** until a pleasing variant appears.
6. **Lock the chosen design** and save its location, rotation, settings, generator version and seed. The same layout must load every time in the game; don't re-randomize it on startup.
7. Optionally **bake** important procedural buildings to editable/static scenes later to preserve hand-authored tweaks and avoid changes when the generator evolves.

This workflow is potentially useful for rural cabins, houses, ranger lodges, farmhouses, gas stations, garages, military outposts, fantasy cottages, taverns, ruins and other architectural families.

## Important distinction: procedural modelling vs procedural layout

`WORLD/Content/Assets/Structures/Building002/` already makes provisional low-poly meshes and collision using C#; the current two-storey layout is **mostly fixed**. A reusable procedural house generator would go further:

- Choose an architectural family and footprint (rectangle, L-shaped, narrow/wide).
- Select a valid room plan, then guarantee door/hall connectivity and useful access.
- Reserve staircase shafts, landings and multi-floor openings **before** placing partitions or furniture.
- Generate real wall/window/door holes, upper decks, stairs, roof, supports and collision.
- Furnish rooms by purpose (bedroom, kitchen, reception, storage, workshop, etc.).
- Vary exterior material palette, damage/weathering, trim, décor and prop arrangement.
- Reuse existing Building002 ground/upper **Level_0 / Level_1** visual-vs-collision grouping and floor metadata. Future automatic camera floor fading must not disable collision.
- Generation must be **seeded and deterministic**, using independent per-instance settings; changing one house must not unexpectedly change every copy.

**Critical:** random walls and furniture with no validation can create inaccessible or broken interiors. Use simple layout rules first, then more complex procedural arrangements once doorway/stair traversal works.

## Conceptual Godot code and folders (illustrative, not approved file creations)

```text
WORLD/Content/Assets/Structures/
  Shared/C/                 # reusable wall, room, floor, roof, stair geometry helpers
  Building002/              # existing ranger-lodge reference/future source of reusable pieces
  ProceduralHouse/
    ProceduralHouse.tscn
    C/ProceduralHouse.cs    # [Tool], editor-time preview + deterministic runtime generation
    Configuration/HouseMaster.tres
    Artwork/                # replace generated placeholders with authored/imported assets later
```

For a minimal MVP, use Godot C# `[Tool]` on the house, exported per-instance settings, and an Inspector **Regenerate** button (Godot `[ExportToolButton]` if supported by the repo's Godot/.NET version). Regenerate *only that instance's generated subtree*, safely in the editor; do not accidentally delete user-authored children. The editor preview must handle C# builds, resource changes and scene reloads without duplicating meshes/colliders or leaving stale nodes. Avoid a per-frame editor polling loop.

The **editor-placed instance's design recipe** is the saved source of truth. Optionally bake a design into a `.tscn`/resource when needing hand-edited special locations or a version-locked shape. For large worlds, don't serialize enormous throwaway generated hierarchies into one giant `World.tscn` unless explicitly intended.

Example stored recipe (illustrative):

```text
House_014
  World transform: hand placed
  Family: Farmhouse
  Seed: 84721
  Generator version: 1
  Floors: 2
  Porch: True
  Furnishing density: Medium
  Locked: True
  Hand-authored overrides: upstairs bedroom / special loot room (future)
```

## Terrain snapping and environment integration

Start with normal editor scene placement and Godot's built-in **Snap Object to Floor** where appropriate. An optional **Snap to Terrain** Inspector button can query *saved Terrain3D* height at the desired X/Z. Terrain must be available in the editor, not only configured dynamically at runtime.

- Sample multiple footprint positions, not just the structure centre, to reject steep slopes or cliff overhangs.
- Optionally warn for excessively uneven footprints; don't auto-rebake or destroy original terrain.
- Plan ahead for flattening/foundations only as a separate, deliberate terrain-editing feature.
- Register building envelopes, entrances, access paths and porches as exclusions for procedural trees, grass, large rocks and enemy spawn points.
- Preserve meaningful paths to doors, not just a footprint-sized clearing.

An external Godot editor asset placer could eventually make placement faster, but **do not require a placement plugin to prove the basic drag-and-drop workflow**.

## How this should fit War Machine's active world

At the time this idea was captured, `WORLD/WorldController.cs` procedurally instantiates Building001, Building002 and Building003 at fixed offsets on startup. `WORLD/World.tscn` uses Terrain3D, and its saved data directory is assigned at runtime. Before the desired editor experience works:

- Make the **existing saved Terrain3D** available to editor authoring, without rebaking or overwriting it.
- Introduce an authored structures container (e.g., `WorldActorsRoot/PersistentPropsRoot/Structures/AuthoredBuildings`) for placed instances, distinct from runtime-generated structures.
- Ensure runtime world initialization **does not create duplicate buildings** at the same site.
- Keep both the top-down view and inherited `WORLD/FirstPerson/FirstPersonWorld.tscn` using the same authored building assets.
- Decide how separately authored regional scenes are loaded/unloaded later, rather than putting every detailed object in one huge permanently active scene.

## Larger world direction: open-world/hybrid

**No final world structure or scale has been decided.** Inspirations discussed: Far Cry's freedom and authored landmarks, with some of Grim Dawn's carefully designed routes/regions, but **less linear exploration**.

Preferred exploratory idea: **hybrid world authoring** instead of choosing either 100% handcrafted or 100% random generation.

| Content | Likely source |
| --- | --- |
| Important towns, robot facilities, story areas, major terrain features and roads | Handcrafted/artist-placed |
| Cabin/farm/house/gas-station locations and special encounters | Authored placement with procedural layout and dressing |
| Trees, grass, rocks, loose debris, minor discoveries | Seeded biome-/slope-/exclusion-aware procedural scatter |
| Secondary settlements or distant wilderness regions | Could later combine authored templates with procedural distribution |

Divide a potentially gigantic world into **regions or streaming cells**. Keep large, recognisable places and general geography under creative control while using deterministic generation for details. Terrain3D capacity is not the same as CPU/physics/AI/visibility/streaming capacity. Explore streaming only once prototypes are solid; do not commit to a huge size prematurely.

An especially appealing compromise: **hand-place 20 recognizable houses in a small town, but procedurally generate most interiors and clutter**, optionally locking a few rooms/plots for quests or hand-authored storytelling.

This architectural system could later be reused for a fantasy project with new room rules and art sets, but War Machine's implementation shouldn't assume fantasy requirements today.

## A possible small sandbox prototype (ideas, not authorization)

| Idea stage | Proof |
| --- | --- |
| HP0 | Bind/load saved Terrain3D in the editor for placement, preserving existing data |
| HP1 | Hand-place an existing Building002 instance, save it, and suppress duplicate runtime placement |
| HP2 | Seeded per-instance variation; editor Regenerate button updates live preview |
| HP3 | Reliable Snap to Terrain, lock and optional bake/persist chosen variation |
| HP4 | Several genuinely valid footprints and room arrangements, including stair/floor rules |
| HP5 | Try several authored plots, a small road/region and later streaming/biome placement |

**First practical success condition:** drag **five** procedural houses into the existing sandbox, place them at different terrain spots, change each seed, obtain five usable variations, then save/reload and find them exactly where and how they were authored. Check collisions, level opacity and doors in both camera modes. Do not start by building a huge map or a complicated editor plugin.

## Guardrails

- Keep this under `NOTES/Ideas/` as reference, **not** an instruction for Codex to implement.
- Read root `README.md`, `CodePreferences.md` and actual updated architecture whenever this is promoted to a real plan.
- Preserve building collision, terrain data, existing zombies/robots, FPS inheritance, and floor-visibility mechanics.
- Editor regeneration and runtime generation should be deterministic, dormant when idle, and avoid per-instance always-running loops or duplicate geometry.
- Save compatibility, seed versioning and stable structure IDs will matter once houses contain gameplay state/loot.

**Open design questions for later:** final world size; contiguous vs connected regions; degree of handcrafted roads/settlements; slope preparation/foundations; procedural exterior vs interior priorities; how much per-room manual overriding is allowed; bake vs regenerate thresholds.

## Reusable visibility foundation now available

Future procedural generators should follow WORLD/Content/Assets/Structures/BUILDING_AUTHORING_README.md: prefab StructureVisibility, StructureSettings master, local occupied bounds, floor levels/elevations, and disjoint walls/roof/deck/contents/stairs roles. Metadata changes with the generated layout; visibility has an explicit Rebuild callback and auto-registration for later spawns. This implements visibility reuse only, not this idea's procedural house generator, terrain snapping, editor seed previews or world streaming.
