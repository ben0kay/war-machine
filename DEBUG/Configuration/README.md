# Debug configuration

Select `DebugMaster.tres` in Godot's Inspector to enable/disable the FPS overlay or change its refresh interval. Changes apply on the next world launch, or immediately through `FpsCounter.ApplySettings()`. Hidden overlays stop their timers. No config polling loop is added.


**Active World/FPS use:** `World.tscn` now instantiates `FpsCounter.cs` above the left DebugOverlay, and `FirstPersonTest.tscn` inherits that one label. Its font turns progressively from green (60+ FPS) through yellow/orange to red (20 FPS or lower). The existing `ShowFps` and `FpsUpdateSeconds` master settings remain authoritative; defaults stay enabled/0.5 s. The legacy TestWorld counter shares this updated colour behaviour.
