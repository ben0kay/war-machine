// Reusable small rectangular light casing/diffuser meshes; C/ owns only one-time procedural geometry.
using Godot;

public static class IndoorCeilingFixtureGeometry
{
    private static readonly BoxMesh SharedCube = new() { Size = Vector3.One };

    // =========================================================
    // Corridor style is a 2.1m slim strip; room style is a compact 0.78m panel.
    // Both hang below the ceiling; art is nonphysical and its emitter is in the owning fixture.
    public static void Build(Node3D parent, bool corridor, IndoorCeilingLightSettings s)
    {
        float width = corridor ? 0.34f : 0.78f, length = corridor ? 2.1f : 0.62f;
        var casing = new StandardMaterial3D
        {
            AlbedoColor = s.FrameColor, Roughness = 0.70f, Metallic = 0.20f
        };
        var diffuser = new StandardMaterial3D
        {
            AlbedoColor = s.LightColor, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            EmissionEnabled = s.Enabled, Emission = s.LightColor,
            EmissionEnergyMultiplier = Mathf.Clamp(s.PanelEmission, 0.2f, 2.5f)
        };
        Box(parent, "Housing", new Vector3(0, 0f, 0),
            new Vector3(width + 0.08f, 0.09f, length + 0.08f), casing);
        Box(parent, "WhiteDiffuser", new Vector3(0, -0.055f, 0),
            new Vector3(width, 0.018f, length), diffuser);
    }

    // =========================================================
    private static void Box(Node3D parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        parent.AddChild(new MeshInstance3D
        {
            Name = name, Mesh = SharedCube, Position = position, Scale = scale,
            MaterialOverride = material, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        });
    }
}
