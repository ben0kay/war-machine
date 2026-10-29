# Trees

Pine/ owns reusable pine placement, settings, procedural C/ meshes, shaders and future Artwork/ models. New tree types should get their own type folder rather than live under a particular world scene.

## Planned broadleaf canopy trees

See [dense forest test-biome plan](../Forest/FOREST_IMPLEMENTATION_PLAN.md): DF1 proposes a tall broadleaf canopy tree type under `VEGETATION/Trees/CanopyTree/` with its own art, configuration and README, but **no new asset has been made yet**. The existing pine source and placement are unchanged.

**DF1 implemented in source (2026-10-29):** [CanopyTree/README.md](CanopyTree/README.md) documents a three-variant tall broadleaf procedural tree with a scaled smaller filler preview, configurable appearance and near/far mesh builder. It has **not** been added to the active forest; DF2 placement is separate. The original pines are unchanged.
