// One reusable flash/tracer per equipped gun; runs only for the short visible shot lifetime.
using Godot;

public partial class FirearmShotEffects : Node3D
{
    private MeshInstance3D _tracer = null!, _flash = null!;
    private float _remaining;
    private FirearmSettings _settings = null!;

    // =========================================================
    // Build once in world space so the trail does not follow a moving shooter after firing.
    public void Initialize(FirearmSettings settings)
    {
        _settings = settings; TopLevel = true;
        var material = new StandardMaterial3D { ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = settings.ShotColor, NoDepthTest = false };
        _tracer = new MeshInstance3D { Name = "Tracer", Mesh = new BoxMesh { Size = Vector3.One },
            MaterialOverride = material, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off };
        float radius = Mathf.Clamp(settings.FlashRadius, 0.01f, 0.5f);
        _flash = new MeshInstance3D { Name = "MuzzleFlash", Mesh = new SphereMesh { Radius = radius, Height = radius * 2, RadialSegments = 8, Rings = 4 },
            MaterialOverride = material, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off };
        AddChild(_tracer); AddChild(_flash); Visible = false; SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    // Reuse the same two instances, with no tweens, per-shot nodes or persistent particles.
    public void ShowShot(Vector3 start, Vector3 end)
    {
        Vector3 direction = end - start;
        float length = direction.Length();
        _tracer.Visible = length > 0.001f;
        if (_tracer.Visible)
        {
            float width = Mathf.Clamp(_settings.TracerWidth, 0.005f, 0.2f);
            Vector3 up = Mathf.Abs(direction.Normalized().Dot(Vector3.Up)) > 0.99f ? Vector3.Right : Vector3.Up;
            var facing = Basis.LookingAt(direction, up);
            _tracer.GlobalTransform = new Transform3D(new Basis(facing.X * width, facing.Y * width, facing.Z * length), (start + end) * 0.5f);
        }
        _flash.GlobalPosition = start;
        _remaining = Mathf.Clamp(_settings.EffectDuration, 0.01f, 0.25f); Visible = true; SetProcess(true);
    }

    // =========================================================
    // Hide and genuinely stop once the flash/tracer expires.
    public override void _Process(double delta)
    { _remaining -= (float)delta; if (_remaining <= 0) { Visible = false; SetProcess(false); } }

    // =========================================================
    public void Stop() { Visible = false; SetProcess(false); }
}
