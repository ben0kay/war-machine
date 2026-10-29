> **SOC0 collision enabled (2026-10-28):** Bookcase001.tscn now sets `CollisionMode = 1` (AutoBox), providing one inexpensive StaticBody3D+BoxShape3D from the imported GLB bounds on runtime instantiation, including F1. Its GLB and master remain unchanged; scale is reflected in collision. **This first-pass full box also blocks the decorative shelf cavities**, so switch to `CustomBoxes` later before allowing books/loot to physically occupy shelf openings. No placement changes; gameplay testing pending.

# Bookcase001 — weathered wooden bookcase

**Original imported GLB retained unchanged.** [Bookcase001.tscn](Bookcase001.tscn) is a new lightweight visual wrapper for [Artwork/Bookcase001.glb](Artwork/Bookcase001.glb), with per-asset size master [Configuration/BookcaseMaster.tres](Configuration/BookcaseMaster.tres). **No physics/interactions yet; local validation pending.**

The imported model remains an old walnut-toned case with backing, shelf boards, feet and brass accents: **1,484 triangles**, seven matte PBR colours, approx. **1.215m W × 1.93m H × 0.415m D**, Y=0 ground pivot, front +Z. The original GLB is not rebuilt or duplicated.

**Size tuning:** `SizeMultiplier = Vector3(1, 1, 1)` in `BookcaseMaster.tres` uses X=width, Y=height, Z=depth multipliers. A changed master affects all future default instances/F1 previews when respawned; override/duplicate settings to vary a single bookcase.

**Policy:** Non-pickup **Object**, fixed/noninteractive design. `Bookcase001.tscn` is visual-only so actors pass through it. Later extend the scene with carefully fitted shelf, upright, and back collision; **never** fill the shelves with one solid collider, or a carried Book001 cannot fit. Keep the original GLB and reusable catalogue location, and attach furnishings to the appropriate structure `contents` role.
