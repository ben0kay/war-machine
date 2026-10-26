# Terrain-embedded winding cave

Near spawn: 48m east and 28m north by default (x=1072,z=1212). Entrance faces south; look for CAVE. CaveMaster owns position, length, width, height, bend, vertical variation, sections and crystal colour/energy.

The Terrain3D demo uses a separate collidable tunnel, rocks around entrances, crystal OmniLights and an ambient-light entrance trigger (demo/components/Tunnel.tscn, demo/src/CaveEntrance.gd). Terrain3D heightfields provide the surrounding land; tunnel geometry supplies the interior. The original demo remains unchanged.

The old exposed concrete-like outer tunnel shell has been removed. A bounded, one-time pass now raises the actual loaded Terrain3D heightfield into a broad ridge above the 90m winding passage, feathers the sides into existing terrain and makes a small control-map hole at the entrance. Irregular collidable rocks dress the opening. Height/control images update once and dynamic terrain collision is rebuilt. The collidable inner passage retains its sloped floor, roof, walls and crystal lights.

Edits affect runtime height/control maps only: no saved .res region is overwritten and no rebake is required. Each fresh launch reapplies the same ridge to the saved starting data. This intentionally changes the landscape in the running game, including terrain-based placement atop it. There is no live cave rebuild control; calling Initialize repeatedly on already modified data is unsupported. No global terrain regeneration or voxel excavation.

WeatherBridge suppresses local precipitation/mist/motes while the camera is within the passage; no per-frame cave callbacks. The cave remains a single tunnel with a sealed end, not a branching network. Local compilation, entrance fit, terrain seams, hidden roof, walking collision, weather shelter and lighting need user validation; Godot/.NET unavailable here. No ambient-light trigger is implemented yet.
