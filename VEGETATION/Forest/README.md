# Dense canopy forest — DF0 site foundation

**Status: DF0 + DF1 implemented in source, awaiting local Godot validation. DF2–DF5 remain planned only.**

The current stage defines an optional **96 × 96m** test footprint east of the village at absolute X/Z **(667, 567)**, opposite the western robot patrol. It has not been proven safe in a running scene; a bounded startup survey inspects terrain/slopes/approach and the existing village/patrol/building/zombie/deer reservation rectangles before accepting it.

## Files and operation

- [FOREST_IMPLEMENTATION_PLAN.md](FOREST_IMPLEMENTATION_PLAN.md): DF0–DF5 planning and local handoff.
- `ForestZone.cs`: **static, startup-only** survey + optional debug border. No process loop or terrain writing.
- `Configuration/ForestSettings.cs`, `Configuration/ForestMaster.tres`: sole settings owner, linked from `WORLD/Configuration/WorldMaster.tres → DenseForest`.
- World startup surveys after grading and before the existing pine batches are spawned; only an enabled/approved footprint is added to their exclusions. No pine generator fork and no new tree art.

**Default is OFF:** the world and existing pines are unchanged. To inspect, enable `ShowDebugOutline` in `ForestMaster.tres`, restart the FPS scene (F6) and walk east; look for the green terrain-following border and `DF0 forest site` Output. `Enabled=true` on a passing site clears **only existing pine placements** from that square for a future canopy forest, and OFF restores the original deterministic pines on restart. A rejection does not clear pines.

**Important:** DF0 does **not** yet create a tall forest, adjust global pine density, block sunshine, change grass/rock scatter, or create wildlife/zombie behaviour. Location remains **awaiting actual in-game approval**. Next phase DF1 will create sample canopy tree artwork, then DF2 will use the confirmed site for chunked MultiMesh distribution.

**Performance:** OFF: no DF0 iteration at all. ON/preview: bounded terrain survey once and optional static outline, with no idle callback, physics shape, per-tree Node or world-scanning loop. Use `dotnet build war-machine.csproj` and F6 World/FPS for compilation and acceptance.

## DF1 canopy tree source

[Standalone CanopyTree prototype](../Trees/CanopyTree/README.md) now provides a 17–22m procedural broadleaf tree, three seeded crown silhouettes, a smaller filler example, lightweight far geometry and its own `CanopyTreePreview.tscn` visual scene. This is **art-only**, not a live forest. Existing DF0 zone toggle and pine clearings are unchanged. The user must approve the tree silhouette and DF0 footprint before DF2 world placement.
