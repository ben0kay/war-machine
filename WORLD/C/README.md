# World procedural artwork

VillageRoadMesh.cs builds the village's simple dirt connection strips and two collidable store-site access ramps once. C/ is geometry only; layout/reservations remain in WORLD/VillageLayout.cs and terrain editing in WORLD/Generation/VillageTerrain.cs. No geometry helper has a process loop or owns actors, camera, weather or materials outside its own art.
