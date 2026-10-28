# ConvenienceStore001 artwork

The building remains procedurally generated once in `../C/ConvenienceStoreMesh.cs`, with no GLB, glass, signage PNG or decals.

**WT1 wall-material experiment:** see [Materials/README.md](Materials/README.md) for two authored `NoiseTexture2D` resources (mottled plaster albedo and fine-grain normal). They now decorate the existing exterior walls and interior stockroom partition using opaque StandardMaterial3D, with configurable repeat/roughness/bump and a switch to revert to plain colour. Other building art/materials are unchanged.

Future asset replacement must preserve physically open door/window apertures, existing collision and the wall/roof/contents role metadata needed for `StructureVisibility`.
