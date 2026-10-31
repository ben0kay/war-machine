# Building Test — standalone FPS structure inspection

**Scene:** `res://DEBUG/Scenes/BuildingTest.tscn` (open and **F6**, not F5).

This is a disposable, deliberately minimal building inspection scene. It **does not** inherit `WORLD/World.tscn`, load Terrain3D, instantiate zombies, equipment, audio, inventory or the real Player, or alter the FPS-only conversion roadmap.

## Swap the building

The root owns a **`BuildingSlot`** Node3D with one instance of `PoliceStation001.tscn` by default. To inspect a different building, remove that child and drag another `*.tscn` into `BuildingSlot` in the Godot scene editor. Keep the building's own root scene (and its procedural `[Tool]` generator) intact. The scene is initially set up for a **48 × 64 m station**, so adjust the external test pads and test-player position if a different building footprint needs it.

The **`BuildingTestPlayer`** is an independent reusable debug prefab, `DEBUG/Scenes/BuildingTestPlayer.tscn`, with a separate `BuildingTestPlayer.cs` controller. It is **not** a `PLAYER/Player.tscn` instance, does not derive from `PlayerController`, and never initializes the shared combat, stance, equipment, audio, inventory or world systems. It uses one Godot `CharacterBody3D` capsule (radius 0.3m / height 1.8m) and its own FPS camera. No script initializes or controls the building. You can reposition the `BuildingTestPlayer` node to spawn in the lobby, basement or outside. A separate `PlayerSpawn` marker marks the initial position but deliberately does not auto-teleport the player at runtime, avoiding additional scene/controller complexity. When moving the spawn, update the test player transform as well.

**Controls:** WASD or arrow keys move, mouse looks, Shift sprints **only while standing**, right mouse button jumps **only while standing**, **C / Left Ctrl / Right Ctrl toggle crouch**, **Slash (/) toggles prone**, Escape releases the mouse, and left mouse click recaptures it. Vertical mouse look is **inverted by default**, like the current FPS master profile; toggle `BuildingTestPlayer → InvertMouseY` in the Inspector to switch direction.

The dedicated test player matches the core gameplay stance geometry and movement multipliers without importing the gameplay player: standing capsule **1.8m** and eye ~**1.65m**, crouching capsule **1.4m** and eye ~**1.25m** at **0.45× walking speed**, prone capsule **0.75m** and eye ~**0.60m** at **0.17× walking speed**. A single capsule/eye transition runs inside the player's existing physics callback; destination headroom queries are only made on requests to grow taller, so blocked ceilings prevent standing and unsafe prone-to-crouch transitions. The prone proxy is a short **upright capsule**, like the existing gameplay prototype, not a physical lying hitbox. The user returns from prone to the previous stance when clearance permits, or crouching if standing is obstructed.

**Documentation discrepancy:** The current real `PLAYER/Input/PlayerInput.cs` handles **Right Ctrl = crouch** and **Slash = prone**. Some older gameplay READMEs describe Right Ctrl as prone. This debug scene follows the **actual code** while adding Left Ctrl / C aliases for easy crouch testing; it does not alter the gameplay bindings.

The movement script runs only for this one test actor; the static test stage and building add no per-frame processing. There are intentionally no weapons, health, HUD gameplay systems, zombie perception, leaning or world interactions — this is a collision/clearance/walkthrough harness, not a gameplay test.

## Exterior floor and basement

The four collidable `TestGround` pads are **outside** the default police station footprint (X ±24, Z ±32), leaving the entire area under the building open. The station's own authored deck, stairs and basement colliders supply the interior walking surfaces. There is **no Terrain3D** in this scene, so the PS1.5 basement is exposed and can be walked through without terrain clipping. The pads are only a neutral outdoor approach, not a terrain-hole implementation.

The test scene also has a static directional sun, simple ambient environment and lightweight instructions in the top-left. The police station's existing fixture scene can illuminate the basement. This stage is for editor preview and isolated walking checks; it does not establish actual in-world terrain-holes, terrain grading, streaming, atmosphere/fog, top-down fading or runtime FPS budgets.

## Validation checklist

1. Build the Godot 4 C# project; open this scene directly and press F6.
2. Walk from the front pad through the police station entrance, climb/descend the main U-stair, and walk down into the basement from the ground level.
3. Test collision at railings, level transitions, both landings, basement doors, floor edges and room wall joins; verify Escape/mouse capture and stairs from both directions.
4. Swap `BuildingSlot`'s child for a different building and update the player location/pads if necessary.
5. Check lighting and rendered materials locally. A source-only commit does **not** prove compilation, visual appearance or walkability.

**Boundary:** The police station source/prefab, normal World/FPS scenes, project startup, gameplay player, terrain assets and project input settings are not modified by this test harness.

## Building-test stance and vertical look (2026-10-31)

The local `BuildingTestPlayer.cs` now supports configurable inverted Y look and stance-changing capsules so inspectors can test 1.8m/1.4m/0.75m clearances and low doors or spaces in any swapped building. This is **not** a new dependency on `PlayerController`. Mouse inversion defaults to true in the inspector, and height/speed values can be edited on the test actor without touching gameplay. The scene's top-left control help lists both actions. C# compilation and physical transitions through basement ceiling/rail geometry require local Godot verification.
