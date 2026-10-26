// A cached unit cone along local -Z. Shader time animates bands; hidden beams have no CPU callbacks.
using Godot;

public partial class ScanBeam : Node3D
{
    private static readonly CylinderMesh Cone = new() { TopRadius = 0, BottomRadius = 1, Height = 1,
        RadialSegments = 32, Rings = 1, CapTop = false, CapBottom = false };
    private MeshInstance3D _mesh = null!;
    private ShaderMaterial _material = null!;

    // =========================================================
    // Unit cone is centred on its axis, then rotated so its tip starts at the socket and opens towards -Z.
    public override void _Ready()
    {
        _material = new ShaderMaterial { Shader = GD.Load<Shader>("res://ENTITIES/TYPES/Robots/Shared/Scanning/ScanBeam.gdshader") };
        _mesh = new MeshInstance3D { Mesh = Cone, MaterialOverride = _material,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off, Rotation = new Vector3(Mathf.Pi * 0.5f, 0, 0) };
        AddChild(_mesh); Visible = false; SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    // Called only on configuration changes; the full cone angle matches the nonvisual sensor convention.
    public void Configure(float length, float fullAngle, Color color, float opacity, float brightness, float lineSpeed)
    {
        float distance = Mathf.Max(0.1f, length);
        float radius = distance * Mathf.Tan(Mathf.DegToRad(Mathf.Clamp(fullAngle, 1, 150)) * 0.5f);
        _mesh.Position = Vector3.Forward * distance * 0.5f;
        _mesh.Scale = new Vector3(radius, distance, radius);
        _material.SetShaderParameter("beam_color", color);
        _material.SetShaderParameter("opacity", Mathf.Clamp(opacity, 0, 1));
        _material.SetShaderParameter("brightness", Mathf.Max(0, brightness));
        _material.SetShaderParameter("line_speed", lineSpeed);
    }

    // =========================================================
    // No light, shadow, particles, timers or per-frame shader writes.
    public void SetScanning(bool enabled) => Visible = enabled;
}
