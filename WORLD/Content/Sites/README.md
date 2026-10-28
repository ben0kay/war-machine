# Reusable world sites (assemblies)

**Scaffold only.** A *site* is a composition of independent prefabs at one location: parking lot, building, street lamps, cars, signs and future props. It is not a building type or a new terrain/lighting manager.

- [ConvenienceStoreSite001](ConvenienceStoreSite001/README.md) — future roadside store and car park, currently an inert empty scene.
- `Infrastructure/` owns reusable asphalt/curb/road pieces independently of the site.
- `Structures/` owns reusable buildings and their shared top-down visibility.
- `Placeables/Objects/` owns fixed objects such as lamps.
- Existing `WORLD/Content/Objects/Cars/Car001` remains untouched.

Do not place or auto-generate a site in `WorldController` until a dedicated world integration phase. Content placement must respect Terrain3D height, foliage clearings, camera visibility registration and the site's complete footprint.
