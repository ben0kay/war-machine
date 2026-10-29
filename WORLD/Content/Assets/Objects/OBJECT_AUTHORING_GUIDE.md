> **SOC0 static collider integration (2026-10-28):** `PlaceableVisual` supports runtime `CollisionMode=0 None, 1 AutoBox, 2 CustomBoxes`. Current Bookcase001, Table001, Bed001 and Hedge001 scenes enable AutoBox; Book001/PineTree001 remain None. For **new fixed static objects**, set `CollisionMode=1` unless traversable openings require multiple `StaticCollisionBox` resources and mode 2. Use native unscaled model-local coordinates for custom boxes, matching visual `SizeMultiplier` on instantiation. No automatic physics for Props/rigid/pickup, and no added permanent world spawns. [Shared helper](Shared/README.md).

# Object authoring guide — GLB-first catalogue (SOC0 static collision available)

**Status:** Active guidance for future AI asset-authoring chats. **SOC0 static collision is now available:** new fixed furniture/landscaping objects can opt into a shared AutoBox or CustomBoxes static collider. Movable props, pickup, loot, breakage, sleeping interactions and permanent world placement still require separate authorization.

**Current location:** `WORLD/Content/Assets/Objects/OBJECT_AUTHORING_GUIDE.md`.
**AM0 implemented:** this is the canonical guide. The [migration record](../../ASSETS_MIGRATION_PLAN.md) tracks local validation. Never create duplicate catalogues or asset copies in the old locations.

> **Future only — not implemented:** [SOC1 Object Diagnostics & Performance Plan](SOC1_OBJECT_DIAGNOSTICS_PLAN.md) proposes optional per-object shadows/render caps, F1 bounds/collider inspection, offline GLB validation and cost reports. Deferred phases cover LOD, placement footprint and camera fade. **Use this plan for later authoring/tool work, not as permission to alter the game now.**

## Start here for every new model

1. Read root `README.md`, `CodePreferences.md`, this guide, the current Objects README and catalogue, `DEBUG/PlaceableSpawner/README.md`, and one comparable asset's scene/master/README. **Work on `implement-td0-td1`, not `main`.**
2. Confirm the requested asset ID and role. **Static / Object** means **not liftable** (it might become interactable later); **Prop** means **intended to be liftable/throwable eventually**, but **not yet physically implemented**. Don't confuse folder classification with the physics-body type: SOC0 adds optional **StaticBody3D** to fixed objects; loose/throwable Props require future IX RigidBody3D.
3. Create one original, Godot-importable **`.glb`** with plausible real-world dimensions. Prefer GLB for furniture, clutter, tools, containers and appliances. Use procedural geometry C# under `C/` only if **explicitly requested** or clearly essential for dynamic geometry; C# can still own a tiny palette selector or scene settings without constructing vertices.
4. Wrap the GLB in a **canonical `<AssetID>.tscn`** and add a **per-asset `Configuration/<AssetName>Master.tres`** for size, reusing the common `PlaceableVisual` and `PlaceableSizeSettings` already in `Shared/`. Don't invent duplicate copies of shared helpers for each object.
5. Record the new asset in the catalogue and its own README. **F1 automatically discovers canonical `<AssetID>/<AssetID>.tscn` visual wrappers deriving from `PlaceableVisual` under `Static/` or `Props/`; no new F1 registration or spawner code is required.** Don't add it to the active world without permission.

## SOC0 — required collision decision when authoring an object or item

**Before creating the canonical prefab, decide whether it is fixed scenery, decoration or intended to become a movable prop.** Every new asset README/catalogue entry must state its **collision mode and rationale**. The shared `PlaceableVisual` C# default is **0/None**, so new assets do not become solid unless the mode is **explicitly selected on their scene root**.

| Inspector selection | Exact `.tscn` value | Use cases | Important consequence |
| --- | --- | --- | --- |
| **None** | `CollisionMode = 0` | Decorative art, Book001 visual-only future movable prop, imported PineTree001 comparison, wall picture, nonblocking clutter | No `StaticBody3D`; player/zombie pass through |
| **AutoBox** | `CollisionMode = 1` | **Default for fixed solid objects:** closed wheelie bin/dustbin, fridge, bookcase, table, bed, hedge, solid cupboard frame | One box around **all imported visual bounds**; blocks gaps beneath tables and inside shelves |
| **CustomBoxes** | `CollisionMode = 2` | **Walk-through/complex fixed objects:** bus shelter roof/posts/side walls, covered walkway, open shelving, crawl-under table | Manually specify a few solid boxes, leaving valid openings physically free |

**Selection rule:** choose **AutoBox** for a typical stationary object unless its visible holes need to stay usable; then choose **CustomBoxes**. Choose **None** only when no static obstruction is intended. A wheelie bin that should later be **pushed, tipped, carried or thrown** belongs to the separately planned `INTERACTION/` IX1–IX2 rigid-body work, **not** a static collider pretending to move. Classifying a model under `Static/` versus `Props/` describes gameplay ownership, not an automatic collision rule.

### Exactly how to author it (reuse SOC0; don't duplicate scripts)

1. Use the existing `Shared/PlaceableVisual.cs` on `<AssetID>.tscn` root, or an existing derived presentation script such as `BedVisual` / `TableVisual`. Keep `Model/Artwork` and `Configuration/<AssetName>Master.tres` `SizeMultiplier`.
2. The root Inspector shows **Static collision (SOC0) → CollisionMode** as a dropdown; it is an exported **int with `PropertyHint.Enum`**, not a C# enum that F1 would mistake for a visual variant. In a scene file the property is simply `CollisionMode = 0`, `1`, or `2`.
3. **AutoBox:** set `CollisionMode = 1`. **Do not manually add `StaticBody3D` or `CollisionShape3D` to the prefab**. At runtime `Shared/StaticObjectCollision.cs` walks visual meshes once, combines transformed AABBs, and creates one static `BoxShape3D` in the correct scaled position.
4. **CustomBoxes:** set `CollisionMode = 2`; add `StaticCollisionBox` Resource entries to root `CollisionBoxes` in the Inspector. Each has **Center** and **Size**, both `Vector3` values in the **unscaled GLB's Model-local metres**. For a bus stop, make thin boxes for **roof, supporting posts, rear/side panels and bench** only—**no entrance-spanning box**. For open shelves use back/uprights/shelf boards, not a single block. Empty/invalid lists warn and generate no collision.
5. **None:** set `CollisionMode = 0` for no collider; leave future movable Books/Buckets/Brooms as art-only until the IX physics phase. Never mix SOC0 static collision with later active `RigidBody3D` on the same item.
6. SOC0 uses one world-layer-1 `StaticBody3D` named `StaticCollision` with lightweight `BoxShape3D` children. The physics body is a **sibling** of `Model`, not a child of the nonuniformly scaled visual; the common size master scales boxes appropriately at creation. Collision may block movement, shots and sight rays **where those systems include world layer 1**. A bus shelter's transparent panels still count as **solid occluders** if covered by boxes; keep openings actually clear.
7. Collision exists **after runtime scene instantiation**, not as a permanent node visible inside the editor's packed prefab. It is generated **once**, without per-object `_Process`, `_PhysicsProcess`, triangle-mesh physics or per-frame bounds scans. To change mode/size/custom boxes, edit the scene/master and respawn/reload. No live recalculation is implemented.

Minimal `<AssetID>.tscn` root example for a **fixed Bin001** (keep actual resource IDs from its new scene):

```ini
[node name="Bin001" type="Node3D"]
script = ExtResource("1")
Settings = ExtResource("2")
CollisionMode = 1
```

For `CustomBoxes`, create the entries via the Inspector rather than inventing per-asset physics C#. The canonical examples already using **AutoBox** are `Bookcase001`, `Bed001`, `Table001` and `Hedge001`. `Book001` and the imported `PineTree001` visual preview remain **None**. `Car001`, `StreetLamp001` and `Door001` have their own existing gameplay collision and must **not** get duplicate SOC0 bodies.

### Required AI handoff / acceptance checks

- Record asset ID, `.glb`, `.tscn`, master, dimensions, pivot, variants, **collision mode and blocked/accessible gaps** in its own README and `PLACEABLE_CATALOG.md`.
- In `WORLD/World.tscn` or the inherited FPS view, use **F1** to spawn a temporary instance. It now has **real collision if the canonical scene is AutoBox/CustomBoxes**, and passes through if None. F1 is not a persistent world-placement tool. Walk against the shape; test a rotated instance and modified `SizeMultiplier`, and check any intended entrance/underside opening. Use Remote scene inspection/visible collision shapes; physics colliders are not constructed in the editor hint preview.
- Check weapons/LOS for solid blockers, especially cases with decorative gaps or transparent glazing; don't claim bullet-blocking or zombie navigation is correct without a local test.
- Keep imported meshes/materials shared, collider count low, and no idle per-object callbacks. No automatic movable-item physics, loot, inventory, player interaction, AI pathfinding or saving is part of SOC0. See [shared implementation](Shared/README.md) and [IX gameplay plan](../../../../INTERACTION/IMPLEMENTATION_PLAN.md).

## Directory examples

**Canonical layout:**

```text
WORLD/Content/Assets/Objects/
  OBJECT_AUTHORING_GUIDE.md
  PLACEABLE_CATALOG.md
  Shared/PlaceableVisual.cs
  Shared/PlaceableSizeSettings.cs
  Static/Furniture/Chairs/Chair001/
    Chair001.tscn
    README.md
    Artwork/Chair001.glb
    Configuration/ChairMaster.tres
  Props/Containers/Bottle001/
    Bottle001.tscn
    README.md
    Artwork/Bottle001.glb
    Configuration/BottleMaster.tres
```

These are **illustrative future IDs**, not claims that the example models exist. Use `Static/` for non-liftable objects and `Props/` for future carryable items. The catalogue, guide and shared files sit directly under `Assets/Objects/`.

A particular asset needs only the files it actually uses. Optional per-asset colour scripts live beside the `.tscn`; **`C/` is for C# that actually generates geometry**, not a catch-all.

## Model/art rules — consistent and inexpensive

| Property | Contract |
| --- | --- |
| Coordinates | Godot metres, **+Y up**. Author a sensible, consistent forward direction (normally **+Z front** for furniture unless there is a documented reason otherwise). |
| Ground pivot | Floor-standing items: root pivot at ground **Y=0**. Small loose props: an intuitive rest-on-surface pivot; document orientation, e.g. a book lies flat. |
| Dimensions | Use credible real-world proportions. Record native GLB **width × height × depth/length in metres**; don't fix an out-of-scale source model by piling on mystery scene transforms. |
| Mesh | Low-poly/stylized with convincing silhouette. Remove hidden/internal faces when safe; avoid dozens of separate nodes or unnecessary material surfaces. No fixed triangle limit: give actual triangle/surface counts and justify unusual detail. |
| Materials | Prefer restrained rough/PBR materials, muted colours and shared resources. Avoid expensive transparency, unique huge textures, bespoke shaders or duplicated GLBs for simple recolours. **Do not rely solely on glTF `COLOR_0` vertex colours for the intended base appearance: set a sensible opaque PBR `baseColorFactor` fallback, and verify actual material import in Godot.** For a small number of visibly distinct colour regions, a few named material surfaces are acceptable; record the added draw-call cost. |
| Composition | One asset per canonical model. Books, cups, cans, pillows or tools intended as independently placeable objects should **not** be permanently baked into other asset models. Built-in details such as mattress/pillows of Bed001 can stay as a single furniture asset for now. |
| Changes | Preserve the asset ID, root pivot and usable dimensions when refining a model. For any GLB re-export, verify names/material groups used by variant scripts remain compatible. |

A detailed model does not automatically perform badly. **Draw calls/material surfaces, visible instance count, shadows and texture memory** matter too; geometry generation method alone does not determine runtime performance. Do not claim measured FPS benefits without profiling.

## The standard placeable scene and size master

Follow existing **Bookcase001**, **Book001** and **Bed001**; their GLBs live under `Artwork/`. The canonical scene has this logical hierarchy:

```text
<AssetID>.tscn
  <AssetID>       (Node3D + shared PlaceableVisual or a small derived visual script)
    Model         (Node3D scaled via master)
      Artwork     (instance of Artwork/<AssetID>.glb)
```

The `.tres` resource uses the **existing** `PlaceableSizeSettings` class. Default:

```ini
SizeMultiplier = Vector3(1, 1, 1)
```

- **X = width, Y = height/thickness, Z = length/depth** multipliers, **not metres**. Example `Vector3(1.2, 1, 1)` increases model width by 20%.
- Editing the shared asset master affects **all default instances after reload/reinstancing**. For a differently sized individual copy, use an instance-specific duplicated/overridden resource.
- Scale only the visual `Model` child, preserving stable asset root placement. For scene roots with `CollisionMode = 1` (AutoBox), SOC0 automatically generates and scales one static collider from the imported bounds **at runtime readiness**. `CollisionMode = 2` uses authored `CollisionBoxes` resources in unscaled model-local metres; `CollisionMode = 0` generates no collider.
- Reuse the existing helpers and master format instead of generating a new per-object C# `Settings` class solely for width.

**Other settings are optional, not implemented globally yet.** A future common `MaterialVariant` selector or `ConditionVariant` (clean/used/dirty/damaged) could be worthwhile, but **do not export pretend settings that have no visual effect**. Add targeted, asset-specific visual configuration only when the actual art supports it.

## Variants: reuse one GLB

**Bed001 is the working reference.** Its one `Bed001.glb` has separate duvet and folded-trim material surfaces. `BedVisual.cs` selects SlateBlue, MossGreen, WarmSand or DustyRose by overriding only those surfaces **on that instance**, with shared palette materials. Frame, mattress, pillows and geometry stay the same.

For new variant-ready assets:

1. Identify which parts really vary (fabric, painted metal, labels, dirt or wear), and keep these surfaces separable without multiplying material slots unnecessarily.
2. Give material groups clear stable names; record exact group/surface contract in the asset README. **Do not blindly reuse Bed001's surface indices**—they are specific to that GLB.
3. Use **shared material resources** and per-instance material overrides; don't modify shared imported source materials so one item's colour accidentally changes every instance.
4. Prefer 2–4 meaningful palettes where they add value. Do not create `Chair_Blue.glb`, `Chair_Green.glb`, etc. just to tint the same mesh.
5. A real shape difference (broken leg, missing door, crumpled sheet) may warrant a separate mesh/geometry variant **later**. Don't imply a colour tint represents geometry damage.
6. Random/deterministic world-seeded selection is **future placement-system work**. For now a user/scene chooses the variant explicitly; no per-frame randomization.

## How to add it to the catalogue and F1 menu

- Update `PLACEABLE_CATALOG.md` with **asset ID**, Static/Prop classification, canonical scene and GLB, native dimensions/pivot/front, triangles/surfaces, variants, size master and **actual SOC0 mode** (`None`, `AutoBox`, `CustomBoxes`). Never label an active AutoBox prefab as visual-only.
- The asset's README must explain appearance, intended placement, scale, orientation, safe colour surfaces, **chosen collision mode with its limitations**, and future interaction intent without claiming it exists.
- **F1 discovery is automatic** for standard `PlaceableVisual` scenes at `Static/.../<AssetID>/<AssetID>.tscn` or `Props/.../<AssetID>/<AssetID>.tscn`. The F1 scanner uses actual scene files, not catalogue markdown. It rejects roots that don't derive from `PlaceableVisual`; Car001/StreetLamp001 remain excluded. **SOC0 collision on eligible placeables is active in runtime F1 previews**. New scenes appear after world reload, with **no F1 C# edits**. For colour/finish variations, expose a **public `[Export]` enum property** on a `PlaceableVisual`-derived script; the menu enumerates choices and sets the property before `_Ready`. Optional per-scene root metadata `f1_preview_hover=true` or `f1_preview_min_distance=12.0` adjusts only unusual preview placement. See [PlaceableSpawner README](../../../../DEBUG/PlaceableSpawner/README.md).
- The F1 debug preview is temporary: **AutoBox/CustomBoxes are solid** when authored on the canonical scene, while None remains pass-through. Do not use world controller/building generators just to preview art or silently add furniture to houses.

## GLB authoring, collision and validation handoff

Keep checks proportional:

1. Verify the actual `.glb` file exists, is structurally valid/importable and has the claimed dimensions/material groups. If generated by code outside Godot, ensure export is real GLB binary, not a renamed text file; inspect mesh normals, face winding and bottom pivot.
2. Verify `.tscn` references to GLB, script and master exist; ensure the scene wraps the imported model at `Model/Artwork`.
3. Where practical, `dotnet build war-machine.csproj`; otherwise clearly mark **not compiled**. Run Godot GLB import and visual test locally if available. Preview via **F6 `WORLD/World.tscn` → F1**; compare variants and edit/reload `SizeMultiplier`. Check FPS debug preview if relevant.
4. Check that the chosen `CollisionMode` is actually written on the scene root and works in F1 at runtime (or None stays noncolliding). Test source scale/rotation and open spaces. Confirm no per-instance idle `_Process`/`_PhysicsProcess`, accidentally duplicated bodies or unintended permanent placements.
5. Update catalogue/asset README and report changed files, branch, one commit SHA, what was tested and what still needs local validation. Respect unrelated edits and the active branch; don't push to main.

**Done for this asset-authoring phase** means: genuine GLB, correct scene/master, **explicit documented collision choice**, catalogue/README updates, proper F1 discovery and a truthful validation handoff. Solidity requires configured/valid SOC0 at runtime; it **does not** imply pickup, interaction, inventory or deployment.

## Reserved for later: dynamic physics and interactions

Keep object identity and scene roots stable so a later IX phase can add **dynamic item physics** without recreating artwork. Remaining future needs:

- **Static furniture:** SOC0 is now available via root `CollisionMode = 1` (one bounding box) or `2` (authored `StaticCollisionBox` resources). Prefer custom box sets for meaningful empty shelving or table/bed clearance. AutoBox is an intentionally inexpensive initial blocker, not a traversable shelf/under-table simulation.
- **Liftable props:** appropriately sized `RigidBody3D` shapes, mass/friction/sleep, carry/drop/throw through the shared interaction owner, and sensible lifecycle/persistence.
- **Scale awareness:** scene visual scaling must agree with collider geometry, particularly for nonuniform multipliers.
- **Gameplay boundaries:** collision, interaction, damage, loot, container storage, audio and visibility belong to their respective owners; never simulate physics for every decorative item by default.

**SOC0 only is implemented:** collision for static canonical GLB wrappers. Loose-prop rigid physics, item interaction/loot and persistent world placement remain future, separately authorized work.
