# Reusable world assets

This is the canonical authored asset library after **AM0**. Migration is implemented; local Godot import/build and visual acceptance remain pending. [Migration record](../ASSETS_MIGRATION_PLAN.md).

| Category | Owner |
| --- | --- |
| [Sites](Sites/README.md) | Locations composed from reusable buildings, ground assemblies and objects |
| [Structures](Structures/README.md) | Building shells and shared visibility; [building authoring contract](Structures/BUILDING_AUTHORING_README.md) |
| [Infrastructure](Infrastructure/README.md) | Parking lots and reusable ground/site components |
| [Objects/Static](Objects/Static/README.md) | Non-liftable cars, furniture and lamps; may still support interactions |
| [Objects/Props](Objects/Props/README.md) | Small assets intended for future carrying/throwing; folder membership does not implement physics |

Start new furniture/prop artwork with the [object authoring guide](Objects/OBJECT_AUTHORING_GUIDE.md) and [catalogue](Objects/PLACEABLE_CATALOG.md). Each asset keeps its stable scene, configuration, Artwork and optional procedural C folder together. Never copy a canonical model merely to reference it from a site. Existing procedural car settings remain distinct from GLB visual size masters.

Terrain/scatter content stays in sibling `../Caves`, `../Rocks`, `../Vegetation` and `../Shared`. Assets does not own those systems or add runtime work. Filesystem categories do not change world node names, placement, physics or visibility behavior.
