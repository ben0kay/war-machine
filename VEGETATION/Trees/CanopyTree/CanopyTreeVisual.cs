// DF1: standalone preview wrapper for procedural canopy art; forest placement must use mesh builder directly.
// Intentionally has no collision, gameplay, wind poll, per-tree physics or permanent world integration.
using Godot;

[Tool]
public partial class CanopyTreeVisual : Node3D
{
    [Export] public CanopyTreeSettings Settings { get; set; } = null!;
    [Export(PropertyHint.Range, "0,2,1")] public int Variant { get; set; } = 0;
    [Export] public bool PreviewFarMesh { get; set; }

    // =========================================================
    // Build once at editor/runtime ready. DF2 will instead cache meshes in MultiMesh batches.
    public override void _Ready()
    {
        SetProcess(false); SetPhysicsProcess(false);
        if (Settings == null) { GD.PushWarning("CanopyTreeVisual requires CanopyTreeMaster."); return; }
        var bark = new StandardMaterial3D { VertexColorUseAsAlbedo = true, Roughness = 1f };
        var foliage = new StandardMaterial3D {
            VertexColorUseAsAlbedo = true, Roughness = 0.95f,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled
        };
        var geometry = new MeshInstance3D {
            Name = "CanopyGeometry",
            Mesh = CanopyTreeMeshBuilder.Create(Settings, Mathf.Clamp(Variant, 0, 2),
                bark, foliage, PreviewFarMesh),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.On
        };
        AddChild(geometry);
    }
}
