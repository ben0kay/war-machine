# World procedural artwork

VillageRoadMesh.cs builds simple dirt-road visuals above the graded native ground, once at startup. It creates no collision, ramps, wedges or processing callbacks. C/ is geometry only; layout/reservations live in WORLD/VillageLayout.cs and native terrain editing lives in WORLD/Generation/VillageTerrain.cs. Terrain3D owns walking support. Other physical props/building walls keep their own native collision.
