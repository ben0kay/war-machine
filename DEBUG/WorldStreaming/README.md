# WS0 section diagnostics

**Top-centre heading:** current player X/Z section, updated on boundary crossing. WorldMaster → Sections controls ShowHeading, ShowGrid, SectionSize, Origin, LogicalBounds and WorldId. Default 128m cells put player spawn at (4,4). Heading reserves the top row; existing world help moves down rather than overlapping it.

**F3:** toggle the read-only X/Z grid (positive X right, positive Z down). The centre square is the player's section, not an exact moving player marker. Nine cells show IDs/owned-placement counts; gold outlines show clipped footprint overlap. A building may touch several cells but contributes one owner count. This grid does not report loaded/unloaded state because WS0 does not stream anything. It does not capture mouse or pause gameplay. F1/F7/F8/F9 keep their current actions.

`WorldSectionDiagnostics` binds existing player movement signals, does not poll the player and disconnects at exit. `SectionMap` redraws only at section changes or when shown. Disable both heading/grid in the master for no movement subscription. FPS uses the same shared composition. Settings/manifest validation failure warns and disables only this diagnostic, leaving world startup running.

## Local contract smoke test

Neither Godot nor dotnet is installed in the implementation environment. These actual-code tests are **provided but not run**:

```sh
dotnet build war-machine.csproj
godot --headless --path . res://DEBUG/WorldStreaming/SectionManifestTest.tscn
```

Use your installed Godot .NET executable path/name. Expected `[WS0] PASS` and exit 0; exceptions produce FAIL and exit 1. Tests cover negative/exact-edge/offset coordinates, half-open finite bounds, four dependency references with one owned placement, schema/world identity, transforms/seeds/overrides round-trip, duplicate IDs, unknown assets and invalid owner/version. No terrain or reusable asset scene is loaded by this test.

F6 World and FPS: heading shows (4,4) at default spawn; cross X=512 or Z=512 to see cell changes; F3 shows five records in village mode and closes cleanly. Confirm buildings/store stay present and retain collision/fading. No duplicate cars/lamps or changed actor activity. Inspect a footprint crossing a line: gold spans cells but owner count is one. Resize a normal desktop viewport (e.g. 1280x720 and 640x360), verify top heading/help, then run with both display flags false. The optional map scales down on viewport resize; the heading remains anchored. Stop/restart and check IDs/record count are stable. There is no disk save, async loading, fog change, actor lifecycle change or WS1 implementation in this job.
