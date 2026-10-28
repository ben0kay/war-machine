// Stable prototype asset IDs resolve to canonical scene paths without loading their scenes or artwork.
using System.Collections.Generic;

public static class WorldAssetCatalog
{
    private static readonly Dictionary<string, string> Paths = new() {
        ["site.convenience-store.001"] = "res://WORLD/Content/Assets/Sites/ConvenienceStoreSite001/ConvenienceStoreSite001.tscn",
        ["structure.building.001"] = "res://WORLD/Content/Assets/Structures/Building001/Building001.tscn",
        ["structure.building.002"] = "res://WORLD/Content/Assets/Structures/Building002/Building002.tscn",
        ["structure.building.003"] = "res://WORLD/Content/Assets/Structures/Building003/Building003.tscn",
        ["structure.building.004"] = "res://WORLD/Content/Assets/Structures/Building004/Building004.tscn" };

    // =========================================================
    public static bool TryResolve(string assetId, out string path) => Paths.TryGetValue(assetId, out path!);
}
