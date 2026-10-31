# Building Test — standalone FPS structure inspection

**Scene:** `res://DEBUG/Scenes/BuildingTest.tscn` (open and **F6**, not F5).

This is a disposable, deliberately minimal building inspection scene. It **does not** inherit `WORLD/World.tscn`, load Terrain3D, instantiate zombies, equipment, audio, inventory or the real Player, or alter the FPS-only conversion roadmap.

## Swap the building

The root owns a **`BuildingSlot`** Node3D with one instance of `PoliceStation001.tscn` by default. To inspect a different building, remove that child and drag another `*.tscn` into `BuildingSlot` in the Godot scene editor. Keep the building's own root scene (and its procedural `[Tool]` generator) intact. The scene is initially set up for a **48 × 64 m station**, so adjust the external test pads and test-player position if a different building footprint needs it.

The **`BuildingTestPlayer`** is an independent reusable debug prefab, `DEBUG/Scenes/BuildingTestPlayer.tscn`, with a separate `BuildingTestPlayer.cs` controller. It is **not** a `PLAYER/Player.tscn` instance, does not derive from `PlayerController`, and never initializes the shared combat, stance, equipment, audio, inventory or world systems. It uses one Godot `CharacterBody3D` capsule (radius 0.3m / height 1.8m) and its own FPS camera. No script initializes or controls the building. You can reposition the `BuildingTestPlayer` node to spawn in the lobby, basement or outside. A separate `PlayerSpawn` marker marks the initial position but deliberately does not auto-teleport the player at runtime, avoiding additional scene/controller complexity. When moving the spawn, update the test player transform as well.

**Controls:** WASD or arrow keys move, mouse looks, Shift sprints, right mouse button jumps, Escape releases the mouse, and left mouse click recaptures it. The movement script runs only for the one test actor; the static test stage and building add no per-frame processing. There is intentionally no crouch, weapon, health, HUD, zombie perception, leaning or game interaction — this is a collision/clearance/walkthrough harness, not a gameplay test.

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
