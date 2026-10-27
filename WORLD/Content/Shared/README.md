# Shared content helpers

C/ContentScatterSettings.cs: exported per-category resource. C/ContentMeshBuilder.cs: small coloured mesh primitives. C/ContentScatter.cs: deterministic Terrain3D height/slope placement and nearby MultiMesh batches. No per-frame polling or saved terrain changes.

ContentScatter also exposes InitializeView for the active player and UpdateAnchor for movement events. World-XZ exclusion rectangles expand by conservative mesh radius; optional rock-only static collision is cached by variant and created/freed with chunks. Collision defaults OFF for existing decorative categories. No new frame callbacks; noise is disposed on rebuild/exit.
