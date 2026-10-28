# ConvenienceStoreSite001 configuration

PL0B is authored directly with editable scene-child transforms in `../ConvenienceStoreSite001.tscn`. `SiteMaster.tres` remains an inert generic `Resource` placeholder and `ConvenienceStoreSiteSettings.cs` intentionally remains blank; neither is assigned to the scene. This avoids two competing sources for positions and an idle layout-update loop.

Future PL1 may use these files for **site-to-world location, allowable terrain slope and vegetation clearings**, but those mechanics are not implemented here. Edit existing prefab masters separately: ParkingLotMaster for bay geometry, ConvenienceStoreMaster for building size, Car001Master for appearance, StreetLampMaster for physical lamp dimensions, and `ENVIRONMENT/Lighting/Fixtures/StreetLamp/Configuration/StreetLampLightingMaster.tres` for actual illumination. If dimensions change, manually realign authored scene transforms.
