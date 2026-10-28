# ParkingLot001 — reusable asphalt and parking foundation

**Status: PL0A implemented, awaiting local Godot/C# compilation and visual validation.** This independent prefab is now instanced by ConvenienceStoreSite001, but the assembled site is not placed in the active top-down/FPS world.

### Files and use

- `ParkingLot001.tscn`: complete standalone prefab with `ParkingLotMesh` and authored `ParkingLotMaster.tres`.
- `C/ParkingLotMesh.cs`: `[Tool]` procedural mesh + matching basic static collision generated **once** when the prefab is instanced, then processing disabled.
- `Configuration/ParkingLotSettings.cs`: typed `[GlobalClass]` resource with Inspector-editable dimensions, bay count, curb and colour settings.
- `Configuration/ParkingLotMaster.tres`: independent default master with a 40 × 28m main apron, 8 bays and 6m driveway. No shared master is modified at runtime.
- `Artwork/`: reserved for future decals, painted cracks and imported assets.

The -Z edge is the **future store frontage**; +Z leads to the approach road. Scene origin (0,0,0) is the centre of the main apron at the underside of the asphalt, whose top is `AsphaltThickness` (0.12m) above root Y. The world placement system must later ground the prefab on suitable *level* Terrain3D, reserve foliage/rock/grass clearings for the full rectangle and driveway, and avoid buried or floating edges. Terrain is **not edited here**.

The layout uses one collidable asphalt slab, a matching south driveway stub, two raised concrete sidewalk spans with curbs, and a narrow lower central entrance access. White/cream bay divider paint, head stripe and driveway guide lines are **non-colliding**. Each of the 8 parking spaces has a passive `Marker3D` at `Generated/Markers/ParkingSpots/Bay_01` … `Bay_08`, facing -Z for the existing Car001's local forward axis; other markers are `Generated/Markers/{Entrance,Storefront,RoadConnection}`. No cars, lamps, buildings, enemies, lighting, audio, scripts or runtime spawning are added to the lot or site.

The configured `BayCount` is capped to available asphalt width (2m side margins). The scene generates on `_Ready()` and **does not live-rebuild** when the Inspector resource changes; re-instance/reload after editing. Every instance shares the same static box mesh helper, but creates its own authored material instances on startup only. There is **no per-frame or per-physics idle processing**; no terrain queries, foliage scans or extra light cost.

### How to inspect it

1. Pull branch `implement-td0-td1`, let Godot build C#, open `ParkingLot001.tscn` directly. The `[Tool]` builder should display the apron, 8 spaces, curb access and driveway stub.
2. Edit `Configuration/ParkingLotMaster.tres` for dimensions/count/colour; re-open/reinstance the scene to rebuild.
3. In a temporary test scene with a player on suitably level ground, verify the slab/sidewalk/curb are physically walkable. This prefab is not placed in `WORLD/World.tscn` yet.
4. Confirm `ConvenienceStoreSite001.tscn`, `ConvenienceStore001.tscn` and `StreetLamp001.tscn` remain empty/unplaced. Report any rendering or collision mismatch before PL0B.

See [site-owned PL plan](../../../Sites/ConvenienceStoreSite001/IMPLEMENTATION_PLAN.md) for PL0B–PL4 and integration constraints.

## Asphalt access correction

The 12cm solid apron/driveway had vertical side faces. Godot CharacterBody3D does not provide general automatic stair stepping, so entering from a side could catch the capsule even though a village-only ramp existed at the driveway end. ParkingLotMesh now generates one continuous sloped asphalt shoulder along the full apron+driveway outline (including both concave joins), with matching upward-facing collision triangles. EdgeRampWidth is 0.8m by default (about 8.5 degrees at 12cm thickness); native top slab and bay markers remain unchanged. Generated bounds automatically include shoulders for village grading and scatter exclusions. The central entrance access strip now has a 2mm visual lip rather than a 52mm step; raised sidewalk curbs remain intentional. The redundant village-only driveway ramp is removed, while the store rear-exit ramp remains.

Built once with the prefab; no player-controller step logic, extra timer or per-frame query. Numeric outline/concave-join/slope checks and diff review passed; Godot/.NET unavailable, so compilation and physical traversal remain unverified. After pull/rebuild, reload the scene and F6 WORLD/World.tscn. Walk from dirt/grass onto both driveway sides, its end, both lot sides and front edges; test back off, corners, central store entrance, zombies and FPS. Saved terrain need not be rebaked.
