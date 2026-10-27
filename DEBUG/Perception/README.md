# Zombie vision debug

ZombieVisionDebug is installed by WorldController under DebugUIRoot in both gameplay views. F7 toggles faint red horizontal eye-height cone sectors attached to zombie VisualRoots. No process/physics callback or extra LOS checks. ShowVisionCone in ZombieMaster controls initial display. Potential FOV is shown through scenery intentionally; actual cover can still hide a target inside the fan. Arms-out pursuit indicates acquisition. The sector uses acquire/retain range and current visual facing. Deactivation hides it; actor deletion frees its mesh, and world unload frees the input owner. Disable with F7 to remove debug draws. Local visual validation pending.

## Top-down checkbox and F7 synchronization

The overhead tuning panel now has a top-level **Debug** disclosure section with **Show zombie sight cones**, default ON. `WorldController` creates `ZombieVisionDebug` once before initializing UI so the checkbox and F7 both operate on the same manager. `SetConesVisible(bool)` hides/shows existing cone meshes for all registered live zombies and publishes `VisibilityChanged` once per actual toggle. The panel synchronizes silently when F7 is pressed and disconnects on teardown. `Register()` applies the current visibility state to any future zombie, still respecting its per-species `ShowVisionCone` setting. F7 remains available in FPS; no additional FPS checkbox is added.

Only visual meshes are affected; zombie field of view, line of sight, chase logic, spawning, damage, and config master files are untouched. No `_Process`/`_PhysicsProcess`, LOS scan, extra mesh allocation or polling was added; one bounded registered-zombie iteration occurs only on toggles. Zombie death cleanup still happens when the manager iterates, as before.

**Awaiting local validation:** build C#; F6 `WORLD/World.tscn`, expand Debug, disable cones, check existing zombies (slow and runner) hide cones but still pursue; re-enable and check drawing; press F7 and observe checkbox update. Briefly F6 FPS to check F7 and startup unchanged. No rebake required.
