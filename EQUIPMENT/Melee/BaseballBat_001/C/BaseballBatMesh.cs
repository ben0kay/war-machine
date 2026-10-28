// Replaceable wood bat placeholder, extending along local -Z from a two-hand grip. No collision or gameplay logic.
using Godot;

public partial class BaseballBatMesh : Node3D
{
    // =========================================================
    public override void _Ready()
    {
        var wood = new StandardMaterial3D { AlbedoColor = new Color(0.49f, 0.29f, 0.13f), Roughness = 0.8f };
        var tape = new StandardMaterial3D { AlbedoColor = new Color(0.08f, 0.085f, 0.09f), Roughness = 1 };
        AddPart("Handle", 0.018f, 0.023f, 0.30f, -0.10f, tape);
        AddPart("Taper", 0.023f, 0.045f, 0.30f, -0.40f, wood);
        AddPart("Barrel", 0.045f, 0.047f, 0.38f, -0.74f, wood);
        var hands = new Node3D { Name = "Hands", Visible = false }; AddChild(hands);
        foreach (float z in new[] { 0.01f, -0.13f })
            hands.AddChild(new MeshInstance3D { Mesh = new SphereMesh { Radius = 0.046f, Height = 0.10f,
                RadialSegments = 8, Rings = 4 }, MaterialOverride = tape, Position = new Vector3(0, -0.01f, z) });
        SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    private void AddPart(string name, float nearRadius, float farRadius, float length, float z, Material material)
    {
        AddChild(new MeshInstance3D { Name = name, Mesh = new CylinderMesh { TopRadius = nearRadius,
            BottomRadius = farRadius, Height = length, RadialSegments = 10 }, MaterialOverride = material,
            Position = new Vector3(0, 0, z), Rotation = new Vector3(Mathf.Pi * 0.5f, 0, 0) });
    }
}
