# ParkingLot001 configuration — PL0A

`ParkingLotMaster.tres` is now a `ParkingLotSettings` resource, attached to `ParkingLot001.tscn`. Its exported groups: asphalt Width/Depth/Thickness, parking BayCount/BayWidth/BayDepth/LineWidth, sidewalk depth/height/entrance gap, driveway width/length and five simple paint colours.

Defaults: main asphalt 40 × 28m, 0.12m thick; 8 × (3.1m by 5.6m) bays, 2.4m sidewalk, 0.15m raised path with 3.2m central low access, 8m × 6m driveway. Bounds are clamped defensively and bay count is reduced if the set would overrun the apron.

This is a **read-only authoring resource** at runtime; the builder reads settings once and does not alter the master. Re-instance to see Inspector changes. No changes to Terrain3D or placement until PL1.

## Optional parking rows (current 2026-10-28)

`ParkingLotSettings.cs` now exports `ParkingRows=1` (default standalone single row), `CentralDriveGap=9.3`m and `InteriorAisleWidth=6`m. The site **does not alter this shared master**: it binds its own `WORLD/Content/Assets/Sites/ConvenienceStoreSite001/Configuration/SiteParkingLotMaster.tres` with `ParkingRows=3`, producing three parking rows and 42 marked bays on the original 56×36m apron. The two added rows automatically clamp/skip when dimensions are insufficient, and preserve the central access from the driveway. Edit the site override for this site, not the reusable preset. Reinstantiate after changes. No per-frame generation.
