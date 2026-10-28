// Bed001 palette adapter for imported GLB material surfaces; C# does not generate bed geometry.
using Godot;

[Tool]
public partial class BedVisual : PlaceableVisual
{
    public enum BeddingPalette { SlateBlue, MossGreen, WarmSand, DustyRose }
    [Export] public BeddingPalette BeddingVariant { get; set; } = BeddingPalette.SlateBlue;

    // GLB contract: 7 material surfaces; duvet #5 and folded trim #6.
    private const int DuvetSurface = 5, FoldSurface = 6;
    private MeshInstance3D? _bedMesh;

    // Cached shared materials; no material/mesh duplication for colour variants.
    private static readonly StandardMaterial3D[] DuvetPaints =
    {
        Paint(new Color(0.27f, 0.39f, 0.51f)),
        Paint(new Color(0.37f, 0.44f, 0.32f)),
        Paint(new Color(0.67f, 0.58f, 0.44f)),
        Paint(new Color(0.55f, 0.38f, 0.40f))
    };
    private static readonly StandardMaterial3D[] FoldPaints =
    {
        Paint(new Color(0.42f, 0.53f, 0.62f)),
        Paint(new Color(0.51f, 0.58f, 0.43f)),
        Paint(new Color(0.78f, 0.70f, 0.57f)),
        Paint(new Color(0.68f, 0.52f, 0.53f))
    };

    // =========================================================
    // Imported GLB is ready before this parent _Ready: apply scaling and only two material overrides.
    public override void _Ready()
    {
        base._Ready();
        ApplyBedding();
    }

    // =========================================================
    // A deliberate runtime palette change never rebuilds the mesh or affects other beds.
    public void SetBeddingVariant(BeddingPalette variant)
    {
        if (BeddingVariant == variant) return;
        BeddingVariant = variant;
        if (IsInsideTree()) ApplyBedding();
    }

    // =========================================================
    // Cache the imported single mesh; preserve GLB authoring surface order.
    private void ApplyBedding()
    {
        if (!GodotObject.IsInstanceValid(_bedMesh))
            foreach (Node node in GetNode<Node3D>("Model").FindChildren("*", "MeshInstance3D", true, false))
                if (node is MeshInstance3D candidate && candidate.Mesh?.GetSurfaceCount() == 7)
                { _bedMesh = candidate; break; }

        if (!GodotObject.IsInstanceValid(_bedMesh))
        {
            GD.PushWarning("Bed001: expected a 7-surface imported GLB mesh for colour variants.");
            return;
        }

        int choice = Mathf.Clamp((int)BeddingVariant, 0, DuvetPaints.Length - 1);
        _bedMesh!.SetSurfaceOverrideMaterial(DuvetSurface, DuvetPaints[choice]);
        _bedMesh.SetSurfaceOverrideMaterial(FoldSurface, FoldPaints[choice]);
    }

    // =========================================================
    private static StandardMaterial3D Paint(Color color) => new()
    { AlbedoColor = color, Roughness = 0.88f, Metallic = 0 };
}
