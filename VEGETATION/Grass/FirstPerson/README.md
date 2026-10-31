> **Current ownership:** Production FPS adapter/profile in `VEGETATION/Grass/FirstPerson`. [View composition and settings](../../../WORLD/FirstPerson/README.md). Older test-folder statements below are historical; current paths and behaviour are defined by the promoted source.

# VEGETATION

`GrassMaster.tres` is the FPS-only grass profile: 32m radius, narrower blades, four sections and 30Hz GPU simulation. It reuses VEGETATION/Grass without duplicating shaders or placement. Shared overhead grass settings remain unchanged.

**STG1 replacement (2026-10-30):** `GrassMaster.tres → Enabled=false`; old FPS GPU particle tiles are now OFF. New FPS (and top-down) stylized grass is streamed by `VEGETATION/Grass/Stylized/StylizedGrassPlacement.cs` from `ASSETS/Vegetation/Grass/ForestGrass001`, configured in `VEGETATION/Grass/Stylized/Configuration/StylizedGrassMaster.tres` (Enabled=true). The legacy FPS grass settings remain only for reversible A/B tests; F1 still targets the old controller. See [replacement handoff](../Stylized/README.md).
