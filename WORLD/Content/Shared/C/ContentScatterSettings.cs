// Editable preview art and distribution settings shared by world content categories.
using Godot;

[GlobalClass]
public partial class ContentScatterSettings : Resource
{
    public enum ContentKind { Flowers, Bushes, FallenLogs, Rocks }
    [ExportGroup("Distribution")]
    [Export] public ContentKind Kind { get; set; }
    [Export] public bool Enabled { get; set; } = true;
    [Export] public int Seed { get; set; } = 4711;
    [Export(PropertyHint.Range, "0,1,0.01")] public float Density { get; set; } = 0.45f;
    [Export] public float Spacing { get; set; } = 7f;
    [Export] public int ChunkSize { get; set; } = 32;
    [Export] public float RenderDistance { get; set; } = 85f;
    [Export] public float MaximumSlope { get; set; } = 0.65f;
    [Export] public float PatchFrequency { get; set; } = 0.025f;
    [Export] public float PatchThreshold { get; set; } = -0.1f;
    [Export] public bool CastShadows { get; set; }
    [ExportGroup("Collision")]
    [Export] public bool CollisionEnabled { get; set; }
    [Export(PropertyHint.Layers3DPhysics)] public uint CollisionLayer { get; set; } = 1;
    [ExportGroup("Generated Artwork")]
    [Export] public float MinimumScale { get; set; } = 0.8f;
    [Export] public float MaximumScale { get; set; } = 1.3f;
    [Export] public float Size { get; set; } = 1f;
    [Export] public int Detail { get; set; } = 32;
    [Export] public Color PrimaryColor { get; set; } = new(0.12f, 0.28f, 0.1f);
    [Export] public Color AccentColor { get; set; } = new(0.7f, 0.5f, 0.85f);
    [Export] public float WindStrength { get; set; } = 0.15f;
    [ExportGroup("Rock Geometry")]
    // Mesh-only controls: generated once per silhouette/rebuild; don't alter placement RNG.
    [Export(PropertyHint.Range, "0,1,0.05")] public float RockCrownFlatness { get; set; } = 0.65f;
    [Export(PropertyHint.Range, "0,1,0.05")] public float RockAngularVariation { get; set; } = 0.55f;
    [ExportGroup("Rock Cover Boulders")]
    // These two additional silhouettes share MultiMesh batching and cached convex hulls.
    [Export] public bool RockCoverEnabled { get; set; } = false;
    [Export(PropertyHint.Range, "0,0.5,0.01")] public float RockCoverChance { get; set; } = 0.22f;
    [Export(PropertyHint.Range, "0.7,1.6,0.05")] public float RockCoverHeightMultiplier { get; set; } = 1f;

    [ExportGroup("Rock Surface")]
    [Export] public Material? SurfaceMaterial { get; set; }
    [Export(PropertyHint.Range, "0.1,5,0.05")] public float RockTextureScale { get; set; } = 0.8f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float RockNormalStrength { get; set; } = 0.65f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float RockMossAmount { get; set; } = 0.35f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float RockWetness { get; set; } = 0.15f;
    [Export] public Godot.Collections.Array<Mesh> ImportedMeshes { get; set; } = new();
}
