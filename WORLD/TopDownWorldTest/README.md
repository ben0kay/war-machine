# Top-down World Test

**Status: Plan only.** The preview scene has not been implemented.

[Implementation plan](IMPLEMENTATION_PLAN.md) — phases TD0–TD3 for a lightweight standalone orthographic-camera prototype at `WORLD/TopDownWorldTest/topdownWorldTest.tscn`.

Intended content: gently rolling Terrain3D with one hill; a very simple low-poly animated human moved by camera-relative WASD; an angled orthographic follow camera with wheel zoom; and one instance of the existing Robot_001 performing its normal walk/stop/cyan scan loop.

Intentionally excluded: grass, trees, rocks, caves, weather, haze, combat and new gameplay systems. No changes to the main `WORLD/testworld.tscn` or saved main-world terrain. When implemented, open the preview scene in Godot and use **Run Current Scene (F6)**.
