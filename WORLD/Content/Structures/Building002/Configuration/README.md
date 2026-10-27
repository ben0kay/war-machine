# Building002 configuration

`Building002Settings.cs` defines Godot inspector exports and `Building002Master.tres` holds the default lodge footprint, wall height, roof rise and material colours. The mesh builder reads these values once when loaded; reopen the scene to regenerate after edits. Settings classes live in `Configuration/`, not mesh-only `C/`.
