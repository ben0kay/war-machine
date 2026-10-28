# ParkingLot001 configuration — PL0A

`ParkingLotMaster.tres` is now a `ParkingLotSettings` resource, attached to `ParkingLot001.tscn`. Its exported groups: asphalt Width/Depth/Thickness, parking BayCount/BayWidth/BayDepth/LineWidth, sidewalk depth/height/entrance gap, driveway width/length and five simple paint colours.

Defaults: main asphalt 40 × 28m, 0.12m thick; 8 × (3.1m by 5.6m) bays, 2.4m sidewalk, 0.15m raised path with 3.2m central low access, 8m × 6m driveway. Bounds are clamped defensively and bay count is reduced if the set would overrun the apron.

This is a **read-only authoring resource** at runtime; the builder reads settings once and does not alter the master. Re-instance to see Inspector changes. No changes to Terrain3D or placement until PL1.
