# Object authoring guide — GLB-first catalogue (visual phase)

**Status:** Active guidance for future AI asset-authoring chats. **Visual-only phase**: new furniture and small props can be created now; do **not** add colliders, physics, pickup, loot, breakage, sleep/use interactions or persistent world placement without a separate request.

**Current location:** `WORLD/Content/Assets/Objects/OBJECT_AUTHORING_GUIDE.md`.
**AM0 implemented:** this is the canonical guide. The [migration record](../../ASSETS_MIGRATION_PLAN.md) tracks local validation. Never create duplicate catalogues or asset copies in the old locations.

## Start here for every new model

1. Read root `README.md`, `CodePreferences.md`, this guide, the current Objects README and catalogue, `DEBUG/PlaceableSpawner/README.md`, and one comparable asset's scene/master/README. **Work on `implement-td0-td1`, not `main`.**
2. Confirm the requested asset ID and role. **Static / Object** means **not liftable** (it might become interactable later); **Prop** means **intended to be liftable/throwable eventually**, but **not yet physically implemented**. Don't confuse this folder classification with Godot `StaticBody3D`/`RigidBody3D`—new catalogue entries are visual-only.
3. Create one original, Godot-importable **`.glb`** with plausible real-world dimensions. Prefer GLB for furniture, clutter, tools, containers and appliances. Use procedural geometry C# under `C/` only if **explicitly requested** or clearly essential for dynamic geometry; C# can still own a tiny palette selector or scene settings without constructing vertices.
4. Wrap the GLB in a **canonical `<AssetID>.tscn`** and add a **per-asset `Configuration/<AssetName>Master.tres`** for size, reusing the common `PlaceableVisual` and `PlaceableSizeSettings` already in `Shared/`. Don't invent duplicate copies of shared helpers for each object.
5. Record the new asset in the catalogue and its own README. **F1 automatically discovers canonical `<AssetID>/<AssetID>.tscn` visual wrappers deriving from `PlaceableVisual` under `Static/` or `Props/`; no new F1 registration or spawner code is required.** Don't add it to the active world without permission.

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

## The standard visual scene and size master

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
- Scale only the visual `Model` child, preserving stable asset root placement. This size setting does **not** automatically build or resize physical collision; physics is deferred.
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

- Update the current `PLACEABLE_CATALOG.md` with **asset ID**, Object/Prop classification, canonical scene + GLB path, native dimensions/pivot/front, triangle and material/surface counts, available variants, own `.tres` path and **visual-only status**.
- The asset's own `README.md` should explain appearance, where it fits the world, scale defaults, orientation, which surfaces are safe to swap, and the **future** collision/interaction intent without claiming those are implemented.
- **F1 discovery is automatic** for standard `PlaceableVisual` scenes at `Static/.../<AssetID>/<AssetID>.tscn` or `Props/.../<AssetID>/<AssetID>.tscn`. The F1 scanner uses actual scene files, not catalogue markdown. It rejects procedural/physics scene roots that don't derive from `PlaceableVisual`; Car001/StreetLamp001 are intentionally excluded from visual-only previews. New scenes appear after world reload, with **no F1 C# edits**. For colour/finish variations, expose a **public `[Export]` enum property** on a `PlaceableVisual`-derived script; the menu enumerates choices and sets the property before `_Ready`. Optional per-scene root metadata `f1_preview_hover=true` or `f1_preview_min_distance=12.0` adjusts only unusual preview placement. See [PlaceableSpawner README](../../../../DEBUG/PlaceableSpawner/README.md).
- The debug preview is temporary/noncolliding. Don't use world controller, building-generator paths or gameplay spawns to test simple artwork. Do not silently add furniture to houses or trigger a permanent terrain rebuild.

## Visual-phase validation and AI handoff

Keep checks proportional:

1. Verify the actual `.glb` file exists, is structurally valid/importable and has the claimed dimensions/material groups. If generated by code outside Godot, ensure export is real GLB binary, not a renamed text file; inspect mesh normals, face winding and bottom pivot.
2. Verify `.tscn` references to GLB, script and master exist; ensure the scene wraps the imported model at `Model/Artwork`.
3. Where practical, `dotnet build war-machine.csproj`; otherwise clearly mark **not compiled**. Run Godot GLB import and visual test locally if available. Preview via **F6 `WORLD/World.tscn` → F1**; compare variants and edit/reload `SizeMultiplier`. Check FPS debug preview if relevant.
4. Confirm there is no per-instance idle `_Process`/`_PhysicsProcess`, no active rigid bodies, no bogus collider or unintended world placement. Verify old catalogue entries and F1 options are not broken.
5. Update catalogue/asset README and report changed files, branch, one commit SHA, what was tested and what still needs local validation. Respect unrelated edits and the active branch; don't push to main.

**Done for this phase** means: genuine GLB, correct visual scene/master, correct documentation, optional working variants/F1 entry and an honest visual-validation handoff. **It does not mean the model is physically solid, pick-up-able, interactable or deployed in the game world.**

## Reserved for later: collision and physics integration

Keep object identity and scene roots stable so a later separately authorized IX task can add collision without recreating artwork. Likely needs:

- **Static furniture:** fitted `StaticBody3D` shapes for relevant solid portions, not one giant box filling bookcase shelves/under-bed gaps.
- **Liftable props:** appropriately sized `RigidBody3D` shapes, mass/friction/sleep, carry/drop/throw through the shared interaction owner, and sensible lifecycle/persistence.
- **Scale awareness:** scene visual scaling must agree with collider geometry, particularly for nonuniform multipliers.
- **Gameplay boundaries:** collision, interaction, damage, loot, container storage, audio and visibility belong to their respective owners; never simulate physics for every decorative item by default.

Keep this section as **future considerations only**, not permission to implement or claim these capabilities today.
