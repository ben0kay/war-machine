# Configuration — Building001

`Building001Master.tres` stores the editable footprint, wall height, roof rise and muted siding/roof/trim/concrete/window/accent colours. `Building001Settings.cs` defines its typed `Resource` class here, **outside `C/`** because this script defines settings, not a mesh builder.

The prefab uses the master on load; changing its dimensions/colours requires reopening the prefab or re-instancing for the one-shot generation to run again. Roof visibility is an exported property directly on the prefab root.
