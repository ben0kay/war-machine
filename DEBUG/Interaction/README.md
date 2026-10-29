# IX0 interaction validation fixture

InteractionTestSwitch.tscn is a synthetic static debug post with a single box collider on solid-world layer 1, label and orange/green indicator. It implements IInteractable and changes only its own toggle state. No idle callbacks, physics simulation, real lights, item identity or asset catalogue registration. Runtime-only placement near the player is controlled by InteractionMaster/SpawnTestSwitch and safely skipped if all four candidates lack support/clearance.

InteractionTargetingTest.tscn is a small actual-code/native-physics smoke for perspective/orthographic selection, target-owned toggle action, player-distance rejection, faded solid-wall rejection and upper-floor rejection. It is provided but not run here; dotnet/Godot are unavailable.

```sh
dotnet build war-machine.csproj
godot --headless --path . res://DEBUG/Interaction/InteractionTargetingTest.tscn
```

Use Godot .NET. Expected [IX0] PASS / exit 0. Then F6 FirstPersonWorld/World for hold E + LMB, GUI priority, fresh trigger after release, held automatic fire cancellation, slot switching, capture/focus/menu/death cleanup and idle query counts. The smoke does not simulate input capture or prove in-world visual acceptance.
