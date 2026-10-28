# AM0 — One-pass WORLD/Content/Assets migration (Codex handoff)

> **Status: PLANNED — NOT IMPLEMENTED.** User approved creating this one-pass implementation plan; actual file relocation, code edits and commits are reserved for a later explicit Codex execution request.
>
> **Target repository / branch:** `ben0kay/war-machine` / **`implement-td0-td1`**, never `main`. Read the latest root `README.md`, `CodePreferences.md`, `WORLD/Content/README.md`, relevant category READMEs and the *live* files before touching anything. As of the 2026-10-28 audit, the five source directories held **113 tracked files**; this count is informational, not a freeze. Concurrent changes must be preserved.

| Phase | Goal | Assistant | Status |
| --- | --- | --- | --- |
| **AM0 — single atomic migration** | Restructure five existing reusable-asset folders under `WORLD/Content/Assets/`, fix all references/docs, verify both views, make **one commit** | **Codex** | **Planned / awaiting explicit execution** |

## Purpose and definition

Establish exactly one canonical location for **reusable authored world assets**. An **Object/Static** is *not pickup-able* (it may still have doors/switches). An **Object/Props** entry is intended to be pickup-able/throwable when IX physics is implemented; changing the directory does **not** add physics. A **Site** is a composed location referencing independent buildings, infrastructure and objects. **Structures** are building shells, **Infrastructure** reusable site/road/parking components. Preserve existing asset identities, visuals, settings, behavior and world placement.

## Final directory layout (actual existing files shown)

```text
WORLD/
  Content/
    README.md                        # updated category navigation
    ASSETS_MIGRATION_PLAN.md         # this plan: keep here as historical tracking
    Assets/
      README.md                      # new canonical asset navigation/authoring guide
      Sites/
        ConvenienceStoreSite001/     # complete scene composed from other assets
      Structures/
        BUILDING_AUTHORING_README.md # keep contract beside structure assets
        Building001/
        Building002/
        Building003/
        Building004/
        ConvenienceStore001/
        Shared/                       # current shared building/visibility scripts
      Infrastructure/
        ParkingLots/ParkingLot001/    # retain existing internal layout and assets
      Objects/
        README.md                     # category definitions, catalogue link
        PLACEABLE_CATALOG.md         # moved/updated existing catalogue
        Shared/                       # PlaceableVisual, PlaceableSizeSettings
        Static/
          README.md
          Cars/Car001/                # existing working car; move, don't duplicate
          Furniture/
            Bookcases/Bookcase001/
            Beds/Bed001/
          Lighting/StreetLamp001/
        Props/
          README.md
          Books/Book001/
    Caves/                            # unchanged in AM0
    Rocks/                            # unchanged in AM0
    Vegetation/                       # unchanged in AM0
    Shared/                           # unchanged shared procedural scatter utilities
```

The above categories are **folder organization**, not automatic code namespaces, physics modes or world scene-tree node names. Don't create empty example `Roads/`, `Household/` etc. unless a real asset exists. Keep actual files, `Artwork/`, `C/`, `Configuration/`, `.tres`, `.glb`, `.tscn`, `.uid`, `.gd`, textures and their source metadata together. Files under `WORLD/Content/Caves`, `Rocks`, `Vegetation`, and procedural `Shared` are **explicitly excluded** from AM0 (they are terrain/scatter systems, not these five reusable-asset folders); revisit separate `Assets/Nature` grouping later if wanted. Nothing outside those five source folders is to be physically relocated in this job.

## Exact old → new mapping

Apply **specific mappings before generic mappings**; check for path collisions before moving. Use `git mv` (or IDE/Godot-assisted moves preserving Git history and source metadata), not delete/recreate. Keep working tree and unrelated edits intact.

| Existing path prefix | Final path prefix | Notes |
| --- | --- | --- |
| `WORLD/Content/Sites/` | `WORLD/Content/Assets/Sites/` | Move all subtrees including site composition scene and plan. |
| `WORLD/Content/Structures/` | `WORLD/Content/Assets/Structures/` | Include shared visibility code, scripts, settings, building authoring README and every structure. |
| `WORLD/Content/Infrastructure/` | `WORLD/Content/Assets/Infrastructure/` | Parking lot and reusable foundations, unchanged internally. |
| `WORLD/Content/Objects/Cars/` | `WORLD/Content/Assets/Objects/Static/Cars/` | Car001 is now *physically* migrated; **unlike previous Placeables notes, no legacy copy remains**. |
| `WORLD/Content/Placeables/Objects/` | `WORLD/Content/Assets/Objects/Static/` | Furniture and lighting subtrees; merge its README with legacy Objects README so category docs are accurate. |
| `WORLD/Content/Placeables/Props/` | `WORLD/Content/Assets/Objects/Props/` | Includes Book001 scene, master, GLB, docs. |
| `WORLD/Content/Placeables/Shared/` | `WORLD/Content/Assets/Objects/Shared/` | Keep shared PlaceableVisual and PlaceableSizeSettings; all scenes still use them. |
| `WORLD/Content/Placeables/PLACEABLE_CATALOG.md` | `WORLD/Content/Assets/Objects/PLACEABLE_CATALOG.md` | Update every catalogue entry/path. |
| `WORLD/Content/Placeables/OBJECT_AUTHORING_GUIDE.md` | `WORLD/Content/Assets/Objects/OBJECT_AUTHORING_GUIDE.md` | Move the new GLB-first visual authoring guide unchanged except for paths/links; preserve its collision/physics-deferred scope. |
| `WORLD/Content/Placeables/README.md` | `WORLD/Content/Assets/Objects/README.md` | Rewrite as canonical Objects guide, including both Static and Props, remove obsolete legacy car note. |
| `WORLD/Content/Objects/README.md`, `WORLD/Content/Objects/Cars/README.md` | Merge guidance into new `Assets/Objects/README.md`, `Assets/Objects/Static/README.md`, and new Cars README location | Do not leave misleading legacy docs in a now-empty old directory. |

Important: after moving `Placeables/Objects/`, its `README.md` becomes `Assets/Objects/Static/README.md`; after moving `Placeables/Props/`, its README becomes `Assets/Objects/Props/README.md`. Reconcile/replace existing legacy README text as needed rather than creating duplicates.

### Critical references identified during pre-audit

This list is **not exhaustive**; Codex must perform a repository-wide search on the latest branch.

- `WORLD/WorldController.cs`: four explicit `Building001`–`Building004` loads and a `Car001` load; retain offsets, terrain placement and existing room/occlusion behavior.
- `WORLD/World.tscn`: `ConvenienceStoreSite001.tscn` external PackedScene reference; maintain the existing node and transform.
- `WORLD/VillageLayout.cs`: **interpolated dynamic building path** (`res://WORLD/Content/Structures/{pair.Item1}/{pair.Item1}.tscn`); ordinary plain-text search of full filenames is insufficient.
- `WORLD/Content/Sites/ConvenienceStoreSite001/ConvenienceStoreSite001.tscn`: references the store shell, ParkingLot001, Car001, StreetLamp001. All four must resolve to the moved targets; preserve original instance transforms.
- `DEBUG/PlaceableSpawner/PlaceableSpawnMenu.cs`: references Bookcase001, Book001 and Bed001 canonical `.tscn` scenes, their masters/models indirectly; F1 menu must continue to spawn all entries with unchanged appearance/scale/material variants.
- All moved `.tscn`, `.tres`, `.cs` and embedded resource paths: `ext_resource`, scripts, materials and references to `StructureVisibility`, `PlaceableVisual`, imported GLBs, artwork, configurations and relative script dependencies.
- `WORLD/Content/Assets/Structures/BUILDING_AUTHORING_README.md`, `INTERACTION/README.md`, `INTERACTION/IMPLEMENTATION_PLAN.md`, `WORLD/Content/README.md`, root README and asset-local documentation: update hardcoded paths, Markdown relative links and instructions for AI-assisted building creation.
- Other controllers, debug panels, setup/config resources, scene inheritance and tool scripts may still name original folders. **Search the entire repo, not only the asset subtree.**
- Do **not** rename C# classes or `res://WORLD/Content/Rocks`, `Caves`, `Vegetation` and `Shared` paths. Do not rename `WorldActorsRoot/PersistentPropsRoot/Objects` runtime node names; filesystem asset classification is independent of scene-tree hierarchy.

## Codex execution checklist — complete as ONE job, not incremental partial pushes

1. **Preflight:** confirm `git branch --show-current` is `implement-td0-td1`, `git status --short`, latest `git log -1`, and read root `CodePreferences.md`. Preserve pre-existing local/uncommitted work and handle concurrently changed files before editing; never reset/force-push. Inventory source files and target collisions.
2. **Baseline reference scan (before move):** `git grep -n -E 'WORLD/Content/(Sites|Structures|Infrastructure|Objects|Placeables)' -- .` and search **case-insensitive** path strings plus C# runtime constructs (`GD.Load`, `ResourceLoader.Load`, `PackedScene`, `ProjectSettings`, `res://`, dynamic/interpolated segment building). Check nontracked project resource files if present. Save a concise list/count to verify against after.
3. **Atomic relocation:** move/merge files exactly per map. Retain canonical `*.tscn` identities, resource contents, `.uid` sidecars, imported `.glb` source, existing configuration resources and relative relationships. Don't commit Godot's generated `.godot/` import cache. Ensure no new file overwrites an existing different file in the target path.
4. **Rewrite all references:** perform path-prefix replacement in **code and source text**, including the special `Placeables/Objects`→`Assets/Objects/Static`, `Placeables/Props`→`Assets/Objects/Props`, `Placeables/Shared`→`Assets/Objects/Shared`, `Objects/Cars`→`Assets/Objects/Static/Cars`, plus Sites/Structures/Infrastructure. Then update full documentation Markdown links (relative or root-anchored) and human-facing category names. Avoid broad replacing an unrelated runtime path simply because its token says Objects.
5. **Finish library docs:** create `WORLD/Content/Assets/README.md`, update `WORLD/Content/README.md` to distinguish authored `Assets` from existing procedural `Caves/Rocks/Vegetation/Shared`, update `Assets/Objects/README.md` + catalogue and structure authoring guide. Recheck all docs in `WORLD/`, `DEBUG/`, `INTERACTION/` and any implementation-plan notes for stale example paths.
6. **Exhaustive stale-path audit:** repeat `git grep -n -E 'WORLD/Content/(Sites|Structures|Infrastructure|Objects|Placeables)' -- .`. It should have **zero active broken resource references** (historical discussion in the AM0 plan can intentionally mention old paths). Specifically verify there is no leftover old actual directory, no duplicate canonical model/scene and no missing target. Audit references from scene/resource files and C# path-building logic, not only literal absolute matches. Check `git diff --check`, `git status --short`, `git diff --stat`, `git diff --summary`; inspect abnormal deletions/geometry changes.
7. **Lightweight validation (no extended profiling):** reimport with Godot .NET editor **if locally available**, build C# (`dotnet build` using the actual solution/project name), inspect parse/import/missing resource diagnostics. F6 `WORLD/World.tscn`: buildings, village/convenience-store site, original cars, lamps, parking surfaces and spawn offsets load correctly; terrain, shared structure visibility and occlusion work as before. F1: Bookcase001, Book001, Bed001 (including variants) load/scale correctly. F6 `DEBUG/TEST/FirstPerson/FirstPersonTest.tscn`: inherited world and content still load. Spot-check resource UID warnings and GLB/material imports. **No terrain rebake or large test suite.** If Godot/toolchain unavailable, state which steps are deferred; never claim runtime passed.
8. **One delivery commit only:** after resolved references and source checks, commit to `implement-td0-td1` with a message such as `refactor(world): consolidate reusable content under Assets`, push once, and report commit SHA, paths changed, any unresolved references/warnings, tests performed and local validation remaining. **Do not push to main or include unrelated gameplay work.**

## Acceptance criteria

- Exactly one canonical `WORLD/Content/Assets/` hierarchy for the five authored categories; complete **Static + Props** merge and no duplicate old `Objects/Placeables` library.
- Every previously working packed scene still instantiates; GLB artwork, size masters, bed material variation, car collisions/open door, parking lot, store scene and multi-floor structure visibility are preserved.
- Village/WorldController startup paths and FPS inheritance resolve. F1 catalog works. All nested asset scenes/resources and `.uid` references resolve without parse errors after import.
- The old five directory prefixes have no remaining **active runtime loads or resource references**; README instructions/links direct future chats to the new hierarchy.
- Terrain/caves/rocks/vegetation/scatter systems stay unchanged; **zero intended visual/gameplay behavior changes, zero new physics or AI, zero world-placement changes, zero continuous background work**.
- One commit to the development branch with a concise local validation handoff.

## Performance and scope notes

This is a **filesystem/reference-only refactor**: no runtime system is activated by moving assets, and no extra per-frame cost is intended. Preserve the current distinction between dormant visual-only placeables and future IX physics. Migration must not accidentally instantiate duplicate assets or rebuild geometry. Avoid rewriting mesh logic or optimizing materials during this pass. Other phases (procedural sites, cave expansion, interaction IX0–IX8, roads or inventory) remain strictly out of scope.

**Suggested assistant:** Codex. The task spans approximately 113 moved files, numerous cross-file C#/Godot references, imported GLB source and several documentation owners. A single repo-aware change/validation pass is more reliable than piecemeal Regular Chat moves.
