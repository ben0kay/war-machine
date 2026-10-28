// Read-only startup bridge for five existing static placements. Never reparent, regenerate or duplicate them.
using Godot;

public static class WorldManifestCapture
{
    private static readonly (string Node, string Instance, string Asset)[] Entries = {
        ("ConvenienceStoreSite001", "village.store-site.001", "site.convenience-store.001"),
        ("Building001", "village.building.001", "structure.building.001"),
        ("Building002", "village.building.002", "structure.building.002"),
        ("Building003", "village.building.003", "structure.building.003"),
        ("Building004", "village.building.004", "structure.building.004") };

    // =========================================================
    public static WorldManifest Capture(Node3D structures, WorldSectionSettings settings, int seed)
    {
        WorldManifest manifest = WorldManifest.Create(settings, seed);
        for (int i = 0; i < Entries.Length; i++)
        {
            var entry = Entries[i]; var node = structures.GetNodeOrNull<Node3D>(entry.Node);
            if (node == null || node.IsQueuedForDeletion()) continue;
            manifest.Placements.Add(WorldPlacementRecord.Capture(entry.Instance, entry.Asset, node,
                VillageLayout.Bounds(node), manifest, seed));
        }
        // Exercise the same serialized contract used by later authoring, not Godot's temporary instance IDs.
        return WorldManifest.FromJson(manifest.ToJson());
    }
}
