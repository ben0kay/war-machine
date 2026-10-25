# PLAYER — Exploration

`Explorer.tscn` and `ExplorationController.cs` provide reusable, collision-free inspection flight. Configure `Configuration/PlayerMaster.tres` for speed, boost, sensitivity, FOV and view distance. World code samples terrain once for safe spawn; the controller knows nothing about terrain generation or weather.

WASD flies relative to the view, QE/Space moves vertically, Shift boosts and Esc toggles the mouse. Flight physics callbacks wake on keyboard input, stop when movement keys release, and sleep when focus is lost. Mouse rotation is event-driven; there is no idle movement polling loop.
