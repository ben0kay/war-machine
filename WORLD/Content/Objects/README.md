# Legacy world objects

Original working `Cars/Car001/` remains here, with existing Godot/C# paths unchanged. **New reusable assets are organized in [../Placeables/](../Placeables/README.md):** `Placeables/Objects/` means cannot pick up (may still interact), `Placeables/Props/` means physically pickup-able. Bookcase001 and Book001 are currently imported-model assets, not integrated prefabs.

Do not put new props under old `Objects/Props/`. Later car migration needs separately approved code/path updates and both-camera validation. Car001 itself remains fixed, with `C/` generator, `Configuration/` and `Artwork/`.
