# Winding cave preview

Integrated directly into TestWorld, about 48m east and 28m north of spawn (default entrance x=1072, z=1212). Look for the CAVE marker. The open entrance faces south; walk north into the passage. CaveMaster controls location, length, width, height, bend, vertical variation, sections and crystal light colour/energy.

Based on the demo's approach in demo/components/Tunnel.tscn and demo/src/CaveEntrance.gd: separate collidable tunnel geometry and coloured crystal OmniLights. Original demo unchanged. This version generates a 90m winding arched tunnel with a flat sloping floor, raised/lowered sections and a sealed far end. A textured rocky shell covers it. This is an added rock-outcrop cave above the saved heightfield, rather than terrain carving; no hole-map or saved region edits occur. It is a single passage, not a branching cave network. No area-based ambient-light change is included yet.

Two static trimesh colliders block the floor/walls/roof. Three static emissive crystal clusters supply shadowed blue lights, distance-limited. The cave builds once after saved terrain loads, with no process callbacks. No foliage exclusion volume yet: grass/props may intersect the entrance or low parts of the passage; visual/terrain fit needs user testing. Terrain and cave are collidable; decorative MultiMesh trees/plants/rocks remain decorative without individual colliders.

Source/resource checks only. Godot/.NET tooling is unavailable in this session; compile and test first-person switching, cave entry/exit, floor slopes, jump/headroom, walls/roof collision and light appearance locally. No runtime/performance acceptance claimed.
