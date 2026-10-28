# ConvenienceStore001 master configuration

`ConvenienceStoreMaster.tres` now uses typed `ConvenienceStoreSettings : StructureSettings`, referenced by both the store generator and the direct shared `StructureVisibility` child. Defaults: 16m width, 12m depth, 4.2m wall height, 4m stockroom depth, 1.45m shelving, furniture enabled and a six-colour wall/trim/accent/floor/roof/shelving palette. Inherited exterior and indoor fade values are kept.

The scene generates geometry only once at `_Ready`; re-instance after changing these authoring values. This master does not affect any other building.
