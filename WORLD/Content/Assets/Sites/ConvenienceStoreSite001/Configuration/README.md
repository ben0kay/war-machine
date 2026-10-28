# ConvenienceStoreSite001 configuration

PL0B is authored directly with editable scene-child transforms in `../ConvenienceStoreSite001.tscn`. `SiteMaster.tres` remains an inert generic `Resource` placeholder and `ConvenienceStoreSiteSettings.cs` intentionally remains blank; neither is assigned to the scene. This avoids two competing sources for positions and an idle layout-update loop.

Future PL1 may use these files for **site-to-world location, allowable terrain slope and vegetation clearings**, but those mechanics are not implemented here. Edit existing prefab masters separately: ParkingLotMaster for bay geometry, ConvenienceStoreMaster for building size, Car001Master for appearance, StreetLampMaster for physical lamp dimensions, and `ENVIRONMENT/Lighting/Fixtures/StreetLamp/Configuration/StreetLampLightingMaster.tres` for actual illumination. If dimensions change, manually realign authored scene transforms.

## Convenience-store hedge-row visual size master

[HedgeRowMaster.tres](HedgeRowMaster.tres) is an **optional site-level override** of the existing shared `PlaceableSizeSettings`. The eight `Hedge001.tscn` instances in `../ConvenienceStoreSite001.tscn` all reference it directly:

`SizeMultiplier = Vector3(1, 1.125, 0.43)` → each is ~4.81m long, **~2.41m high (2.5× the first-pass 0.96m)**, ~0.55m deep. These are visual-only; the globally reused hedge's own `HedgeMaster.tres` is unchanged. The hedge group is at site Y=-0.12 and all eight instance roots at Y=0.27, landing on the raised sidewalk top at site Y=0.15. They run along Z=-15.7 with a ~4.2m central opening, and leave a store-facing pedestrian path ~1.77m deep.

Changing this master affects all eight only on scene reload/reinstance. The count and X/Z locations remain manually authored in the site scene and won't adjust automatically if ParkingLot001 dimensions are later changed. No new C# logic, collision, terrain grading or per-frame work was added.

## Site-only parking rows — current 2026-10-28

`SiteParkingLotMaster.tres` is a site-specific `ParkingLotSettings` resource assigned to `../ConvenienceStoreSite001.tscn/ ParkingLot`. It matches existing 56×36m asphalt, 14 bays in the original row, 3.1×5.6m stalls, 8×6m road stub and 2.4m raised store sidewalk, but enables **ParkingRows=3**, **CentralDriveGap=9.3m**, **InteriorAisleWidth=6m**. Two additional split-bank rows produce 28 more marked bays (42 total), leaving a through centre access route. Change this site resource to tune the arrangement without modifying standalone `ParkingLot001/Configuration/ParkingLotMaster.tres`. Parking lines/markers are generated at scene startup only. Four new interior StreetLamp001 instances and four simple concrete pads are directly authored in the site's `.tscn`, not in ParkingLot001. Visual/build acceptance pending.
