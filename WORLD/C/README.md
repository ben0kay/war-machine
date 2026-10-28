# World procedural artwork

VillageRoadMesh.cs builds the village's simple dirt connection strips and two collidable store-site access ramps once. C/ is geometry only; layout/reservations remain in WORLD/VillageLayout.cs and terrain editing in WORLD/Generation/VillageTerrain.cs. No geometry helper has a process loop or owns actors, camera, weather or materials outside its own art.

The rear-exit access ramp now uses a solid convex wedge with a buried toe instead of one-sided trimesh collision. Asphalt perimeter ramps are owned by ParkingLotMesh; their convex wedges and clockwise art faces are built there. No player step/teleport behavior is introduced.
