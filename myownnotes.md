move combat, interaction, into SYSTEMS?

What is the VEGETATION folder?
How does it differ from World/Content/Vegetation & my now new ASSETS folder (which i created to import external models)




m getting some bad lag spikes in my world. firstpersonworld.tscn. can you see why ?

10:36 PM


CodePreferences.md


I’ll trace the FPS scene’s active world systems and their per-frame/streaming work, then point to the most likely spike sources with evidence. I’ll keep this diagnostic-only unless you ask me to change something.


Yes. The strongest source-level suspect is chunk generation happening synchronously when you move, not anything unusually complex in FirstPersonWorld.tscn itself. That scene mostly inherits the shared world and swaps in FPS controls/camera.

The player’s movement events feed several systems at once: grass, pines, rocks, and forest zones. When a 32 m chunk boundary is crossed, grass and pine create missing chunks immediately (grass, pine); forest defers its update, but still creates chunks together on the main thread (forest). Chunk creation scatters candidates, samples terrain, builds MultiMeshes, and can add collision shapes. Several systems can do that work on the same crossing—an excellent match for intermittent movement stutters, especially near forest.

There’s also substantial content enabled by default: stylized grass and ForestGrass002, three forest zones, pines, and rocks with collision. Near the village, the world also requests 20 extra stress-test zombies by default, in addition to its regular spawns; those are activated and run physics/AI (default setting, spawn path).

Other cases to distinguish:

Only at startup: the world explicitly builds full terrain collision (WorldController).
When returning to distant buildings: scene resources load threaded, but instantiation/attachment still happens on the main thread; the operation limit doesn’t bound how long one scene’s setup takes (streamer).
I haven’t profiled a live run, so these are strong code-based leads, not a measured diagnosis. The quickest check is to watch Godot’s profiler while crossing a 32 m boundary, then A/B test with the extra zombies disabled and vegetation families disabled one at a time. I made no changes.