// Shared minimal cut-surface geometry; gameplay adapters only attach/show it at existing limb pivots.
using Godot;

public static class ZombieStumpGeometry
{
    private static readonly Material Surface = new StandardMaterial3D
    { AlbedoColor = new Color(0.27f, 0.13f, 0.12f), Roughness = 1 };
    private static readonly Mesh Cap = new CylinderMesh
    { TopRadius = 0.07f, BottomRadius = 0.065f, Height = 0.045f, RadialSegments = 8 };

    // =========================================================
    public static MeshInstance3D Create()
        => new() { Name = "AnatomyStump", Mesh = Cap, MaterialOverride = Surface, Visible = false };
}
