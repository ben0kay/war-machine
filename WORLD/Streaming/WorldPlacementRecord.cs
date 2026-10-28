// Serializable authored-instance description, independent of live Godot node IDs and scene paths.
using Godot;
using System.Collections.Generic;
using System.Text.Json.Serialization;

public sealed class WorldPlacementRecord
{
    public string InstanceId { get; set; } = "";
    public string AssetId { get; set; } = "";
    public SectionCoordinate Owner { get; set; }
    public int Seed { get; set; }
    // Basis columns then origin preserve rotation, scale and placement without Euler decomposition.
    public float[] Transform { get; set; } = System.Array.Empty<float>();
    public float[] Footprint { get; set; } = System.Array.Empty<float>();
    public Dictionary<string, string> Overrides { get; set; } = new();
    [JsonIgnore] public Transform3D WorldTransform => new(new Basis(
        new Vector3(Transform[0], Transform[1], Transform[2]), new Vector3(Transform[3], Transform[4], Transform[5]),
        new Vector3(Transform[6], Transform[7], Transform[8])), new Vector3(Transform[9], Transform[10], Transform[11]));
    [JsonIgnore] public Rect2 Bounds => new(Footprint[0], Footprint[1], Footprint[2], Footprint[3]);

    // =========================================================
    public static WorldPlacementRecord Capture(string id, string assetId, Node3D node, Rect2 bounds, WorldManifest manifest, int seed)
    {
        Transform3D t = node.GlobalTransform;
        return new WorldPlacementRecord { InstanceId = id, AssetId = assetId, Seed = seed,
            Owner = manifest.SectionAt(new Vector2(t.Origin.X, t.Origin.Z)),
            Transform = new[] { t.Basis.X.X, t.Basis.X.Y, t.Basis.X.Z, t.Basis.Y.X, t.Basis.Y.Y, t.Basis.Y.Z,
                t.Basis.Z.X, t.Basis.Z.Y, t.Basis.Z.Z, t.Origin.X, t.Origin.Y, t.Origin.Z },
            Footprint = new[] { bounds.Position.X, bounds.Position.Y, bounds.Size.X, bounds.Size.Y } };
    }
}
