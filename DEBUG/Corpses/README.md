# ZD0 fall validation

Provided, **not run in the implementation environment** (no Godot/.NET):

```sh
dotnet build war-machine.csproj
godot --headless --path . res://DEBUG/Corpses/CorpseFallTest.tscn
```

Use your Godot .NET executable. Expected `[ZD0] PASS` / exit 0. The smoke test creates flat static ground, kills one normal zombie and one runner with duplicate lethal hits, and checks one corpse per actor, original visual identity under an independent owner, bounded flat-ground settle, disabled collision/frame callbacks and unchanged settled world transform. It also checks invalid-direction fallback and dormant manager. A fallback dismissal on this flat-ground test is a failure requiring tuning, not a test pass.

Brief local F6 World and inherited FPS checks:

- Kill standing, walking and chasing zombies of both variants from front/back/sides. Verify hit direction/limb variation, retained clothing/blood, a short collapse then no twitching/gait. This is one-body ragdoll-like presentation, not an articulated ragdoll.
- Walk/shove/shoot through the dead position: no upright invisible capsule, corpse hit receiver, sight blocker or hard pile wall should remain. Dead actors/health bars/vision cones disappear; original materials stay on the corpse.
- Try a slope, wall, doorway, stair and edge. Proxy contact should keep the torso out of solid terrain. Unsupported/failed falls may be dismissed within the cap. Watch for body/limb clipping, excessive spin, hovering or roof snapping; one simplified proxy cannot guarantee every limb contact.
- Inspect Remote `PersistentEntitiesRoot/Corpses`: active RigidBody falls freeze with disabled shapes, zero collision and no processing. No per-corpse idle timer. Stop/restart while falling and settled; no orphan bodies or callback errors.
- Piles, movement slowdown, 300s expiry/sink and crowd caps remain ZD1–ZD3. ZD0 settled visuals remain until scene unload. Profile briefly with several deaths after shaders settle; physics correctness and visual acceptance remain local.
