# Reusable world sites (assemblies)

A site composes independent prefabs: buildings, parking surfaces, cars, lamps and markers. It owns relative transforms and the complete placement footprint, not the child assets' geometry, visibility or interactions.

[ConvenienceStoreSite001](ConvenienceStoreSite001/README.md) is assembled and placed in the active village: store, parking lot with 14 bays, seven Car001 instances and four lamps. It is inherited by the FPS test.

Reusable components live in sibling `../Infrastructure`, `../Structures` and `../Objects/Static` (including cars and lamps). AM0 moves these assets without changing placement. Future site integration must still check terrain across the entire footprint, clear vegetation/rocks and register only the building with structure visibility.
