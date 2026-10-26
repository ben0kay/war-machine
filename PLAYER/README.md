# PLAYER — Exploration

`Explorer.tscn` and `ExplorationController.cs` provide reusable, collision-free inspection flight. Configure `Configuration/PlayerMaster.tres` for speed, boost, sensitivity, FOV and view distance. World code samples terrain once for safe spawn; the controller knows nothing about terrain generation or weather.

WASD **or arrow keys** fly relative to the view. Hold **Ctrl** to descend and **RMB** (right mouse button) to rise. QE/Space remain vertical aliases, Shift boosts and Esc toggles the mouse. Movement requires captured mouse mode. Flight physics callbacks wake on keyboard **or mouse-button** input, stop when movement inputs release/cancel, and sleep when focus is lost. W+Up and other duplicate aliases do not double speed. Mouse rotation is event-driven; there is no idle movement polling loop.
