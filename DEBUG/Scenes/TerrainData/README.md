# BuildingTerrainTest-only Terrain3D data

This directory is deliberately separate from the world terrain directories.

The first **F6** run of `DEBUG/Scenes/BuildingTerrainTest.tscn` writes one native
`terrain3d_*.res` region here, containing flat Y=0 terrain and a real 48x32m
hole beneath the PoliceStation001 basement. It saves **only if no .res region
exists**, so subsequent terrain sculpt/holes painted in the editor survive.

Never copy or move the main world's `WORLD/Terrain/Data` or
`WORLD/Terrain/Gameplay` files here, or vice versa.
You can commit this generated .res later if you want a permanently saved test
region in the repository; this authoring change does not invent its binary bytes.
