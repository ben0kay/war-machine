# ConvenienceStore001 — future reusable building shell

**Status: scaffold only, not modeled or placed.**

`ConvenienceStore001.tscn` is an empty Node3D; `C/ConvenienceStoreMesh.cs` and `Configuration/ConvenienceStoreSettings.cs` are intentionally empty. `Configuration/ConvenienceStoreMaster.tres` is an untyped, unused placeholder.

When PL0B is authorized, build an independent ~12–16m roadside store shell with a readable entrance, windows, optional counter/front sign and physical walls/door openings. The convenience store must be independently placeable without the asphalt lot; the *site* instances them side by side.

**Critical:** follow [BUILDING_AUTHORING_README.md](../BUILDING_AUTHORING_README.md): derive settings from `StructureSettings`, use the shared `StructureVisibility` component, occupied volume and floor metadata, disjoint `walls/roof/deck/contents` visual roles and separate actual collision. The generic world camera should fade only the building when obstructing the player; the asphalt lot and streetlamps must remain unaffected.

No lighting, convenience-store interior gameplay, inventory, door interactions or standalone placement are part of this scaffold.
