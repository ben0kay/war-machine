# World configuration

`WorldMaster.tres` is the saved, Inspector-editable master resource. Its C# resource class lives one folder above. Select the resource in Godot to tune grouped values. Changes apply on the next scene launch; no per-frame config polling.

## Convenience-store zombie stress population (2026-10-28)

The shared `WorldMaster.tres` now exposes a dedicated **Store population stress test — 20 additional zombies** group: `StoreStressZombiesEnabled=true`, `StoreStressSlowOffsets` with 16 village-relative slow candidates, and `StoreStressRunnerOffsets` with four village-relative runner candidates. Both views use the same authored population; `HordeCount` remains zero. Disable the boolean and restart the scene for a baseline FPS comparison. Existing safe terrain, physics-clearance and player-distance checks may skip blocked candidates; Output logs the actual additional spawn count. Only `WORLD/WorldController` owns spawn logic; no separate site or entity manager or timers. No new permanent processing; local build/gameplay validation pending.
