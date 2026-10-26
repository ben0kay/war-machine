# PLAYER — Exploration

`Explorer.tscn` and `ExplorationController.cs` provide reusable, collision-free inspection flight. Configure `Configuration/PlayerMaster.tres` for speed, boost, sensitivity, FOV and view distance. World code samples terrain once for safe spawn; the controller knows nothing about terrain generation or weather.

WASD **or arrow keys** fly relative to the view. Hold **Ctrl** to descend and **RMB** (right mouse button) to rise. QE/Space remain vertical aliases, Shift boosts and Esc toggles the mouse. Movement requires captured mouse mode. Flight physics callbacks wake on keyboard **or mouse-button** input, stop when movement inputs release/cancel, and sleep when focus is lost. W+Up and other duplicate aliases do not double speed. Mouse rotation is event-driven; there is no idle movement polling loop.

## First-person mode

Explorer is now a CharacterBody3D with a hidden capsule. F6 or the top-right camera button switches between free flight and collidable first-person walking. Press Esc to release the mouse for clicking the button. Walking: WASD/arrows, Shift sprint, Space jump. Ctrl/RMB/Q/E vertical flight aliases apply only in free mode. Free flight remains collision-free.

On entering walking mode, raycast below the camera finds terrain or cave ground, then checks capsule headroom. Terrain3D height sampling provides a fallback when flying outside its dynamic collision radius. Switching back preserves eye position; no player body mesh is drawn. PlayerMaster exposes walking/sprint/gravity/jump/capsule/eye settings alongside existing flight/view settings. Terrain3D uses Dynamic/Game collision, and cave floor/walls/roof use static trimeshes. Decorative vegetation and scattered rocks/logs still have no individual physics colliders.

Gravity/movement run while airborne or walking; settled idle sleeps until input wakes it. Camera mode UI uses signals only. F9 blocks player controls and resumes walking physics when closed. No new player render mesh or gameplay systems. Source checks only this revision: Godot/.NET unavailable; compilation and local movement/cave validation deferred to user.
