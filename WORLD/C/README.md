# World procedural artwork

VillageRoadMesh.cs builds simple dirt-road visuals above the graded native ground, once at startup. It creates no collision, ramps, wedges or processing callbacks. C/ is geometry only; layout/reservations live in WORLD/VillageLayout.cs and native terrain editing lives in WORLD/Generation/VillageTerrain.cs. Terrain3D owns walking support. Other physical props/building walls keep their own native collision.


## VG4 — Sidewalk and reusable roadside lamps (2026-11-01)

The startup-only `VillageRoadMesh.Build(parent, layout, settings)` now renders the existing dirt road plus **two extra merged, static visual surfaces** for the east-side 2.25m concrete sidewalk and narrow edge. `VillageLayout` generates paved sections without covering eastbound road junctions and includes them in the same flat terrain support/grass clearings. Do not add an overlapping sidewalk collider: Terrain3D is the walking surface.

Outboard lamp points use the original `StreetLamp001.tscn`, including collision, geometry and existing global night schedule; the lamps are stored under `PersistentPropsRoot/VillageStreetLamps`. Default interval 28m, skipping ends/junctions/building overlaps. Settings and enabled state live under the world master. No runtime ticking or terrain file save. **Source-only; Godot build and night FPS validation pending.** See `WORLD/README.md` VG4.
