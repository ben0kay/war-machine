> **PH2 extension:** The same smoke now checks stamina initialization/idle sleep, free walking, sprint drain, delay/recovery timing, exact jump costs/rejection, exhaustion threshold, invalid spending, small shot spending, immutable defaults and death shutdown. Recovery is manually stepped on the real stamina node in this synchronous fixture; world integration still needs FPS/top-down checks. Not run here: dotnet/Godot unavailable.

# PH0 standalone smoke tests

`PlayerVitalsTest.tscn` is a development-only scene exercising actual player/vitals/shared-health code. It never runs from World, adds no gameplay hotkey, changes no authored resources and exits with 0 on success or 1 on failure. The output starts `[PH0] PASS` or `[PH0] FAIL`.

Run from repository root after Godot .NET has imported the project:

```sh
dotnet build war-machine.csproj
godot --headless --path . res://DEBUG/TEST/PlayerVitals/PlayerVitalsTest.tscn
```

Use your installed Godot .NET executable name/path if different. Tests cover initial six-region state, pre-initialization hits, independent limbs/vitality, unknown-to-torso damage, invalid inputs, clamped healing, exhausted limb survival, single mitigation with armour/resistance, callback snapshot consistency, latched death, death input gate and unchanged NPC two-rifle-hit behavior. These tests have **not run here**: neither Godot nor dotnet is installed.

Manual PH1: F6 World and FPS. There are no incoming enemy attacks yet, so use a temporary development call/subscriber at the end of world startup to inject e.g. `player.Vitals.ReceiveHit(new DamagePacket(65, DamageType.Physical, null, player.GlobalPosition, Vector3.Zero), BodyRegion.LeftLeg)` and later `player.Vitals.HealRegion(BodyRegion.LeftLeg, 65)`; remove the call afterwards. Check only the player's own left leg changes, no vitality bar, readable severity, no limp, healing restores it, death via torso stops movement/fire/Q/weapon selection, opening/closing inventory never releases the death gate, world keeps running and NPC bars remain unchanged. Try 1280x720 and 640x360; mouse must pass through the status widget. No permanent debug damage controls were added.
