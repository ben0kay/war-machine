# Zombie debug visibility

`ZombieVisionDebug` owns existing zombie registration, F7 sight-cone presentation and explicit nearest-target lookup for injury tests. `ZombieRenderDebug` is a passive visibility-only registry used by the World/FPS debug panel.

## Render-cost comparison

Under the left panel **Debug → Hide zombie visuals + corpses**, toggle while keeping camera position/direction fixed. FPS: Escape releases the pointer to use the panel. Start with no firing/active falls and compare stable FPS before/after. Hiding living art and corpses isolates their geometry/shadow drawing cost; AI, physics, movement, anatomy, animation and corpse simulation continue. If FPS barely changes, look at other visible scenery/rendering costs. This is diagnostic, not an optimization or a measured CPU/GPU attribution.

The registry stores each zombie's existing VisualRoot once. ZD0 transfers that same root into the corpse owner, so both old corpses and deaths while hidden stay covered without scanning the corpse scene or modifying shared corpse code. Newly registered zombies inherit the hide flag. Only root visibility changes; child visibility (severed limbs, stumps) is preserved. Restoring also preserves roots that were already individually hidden. Sight cones are temporarily hidden with the art and return to their selected F7/checkbox preference afterward. Clearing the world-local registry restores hidden valid roots and releases references.

No per-frame callback/timer, global static registry, extra raycast, process-mode change, scene unload or collision modification. Registration and checkbox changes are the only work triggers; dead references prune on checkbox changes. Toggle defaults off at world startup. Available in the shared debug panel for both views, debug builds only.

Source/lifecycle/whitespace checks only here; Godot/.NET are unavailable. Pull, rebuild, test both World and FPS: hide/show with living and settled dead zombies, kill one while hidden, check new corpse stays hidden, restore with missing limbs still missing, verify AI still pursues and colliders remain, then compare FPS from the same view. Actual compilation/runtime and rendering measurements remain local.

## Prior notes

# Zombie vision debug

ZombieVisionDebug is installed by WorldController under DebugUIRoot in both gameplay views. F7 toggles faint red horizontal eye-height cone sectors attached to zombie VisualRoots. No process/physics callback or extra LOS checks. The `ShowZombieSightConesOnStart = false` export on `WORLD/World.tscn` controls initial visibility; `ShowVisionCone` in zombie settings still determines whether each species is eligible to display its cone. Potential FOV is shown through scenery intentionally; actual cover can still hide a target inside the fan. Arms-out pursuit indicates acquisition. The sector uses acquire/retain range and current visual facing. Deactivation hides it; actor deletion frees its mesh, and world unload frees the input owner. Disable with F7 to remove debug draws. Local visual validation pending.

## Top-down checkbox and F7 synchronization

The overhead tuning panel now has a top-level **Debug** disclosure section with **Show zombie sight cones**, default **OFF** as configured by `WORLD/World.tscn`. `WorldController` creates `ZombieVisionDebug` once before initializing UI so the checkbox and F7 both operate on the same manager. `SetConesVisible(bool)` hides/shows existing cone meshes for all registered live zombies and publishes `VisibilityChanged` once per actual toggle. The panel synchronizes silently when F7 is pressed and disconnects on teardown. `Register()` applies the current visibility state to any future zombie, still respecting its per-species `ShowVisionCone` setting. F7 remains available in FPS; no additional FPS checkbox is added.

Only visual meshes are affected; zombie field of view, line of sight, chase logic, spawning, damage, and config master files are untouched. No `_Process`/`_PhysicsProcess`, LOS scan, extra mesh allocation or polling was added; one bounded registered-zombie iteration occurs only on toggles. Zombie death cleanup still happens when the manager iterates, as before.

**Awaiting local validation:** build C#; F6 `WORLD/World.tscn`, verify no cones show at startup and the Debug checkbox is unchecked, then enable it and see cones on eligible zombies; press F7 and check the checkbox syncs. Briefly F6 inherited FPS scene: default cones hidden, F7 still toggles. Actual eyesight/chase remain unchanged. No rebake required.
