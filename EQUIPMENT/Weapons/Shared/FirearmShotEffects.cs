// Reusable fixed-size shot tracer pool and muzzle flash; only ticks while feedback is visible.
using Godot;

public partial class FirearmShotEffects : Node3D
{
    private MeshInstance3D[] _tracers = System.Array.Empty<MeshInstance3D>();
    private float[] _tracerTime = System.Array.Empty<float>();
    private MeshInstance3D _flash = null!;
    private FirearmSettings _settings = null!;
    private int _nextTracer;
    private float _flashTime;

    // =========================================================
    // Create a capped number of reusable tracers once at equip time, not during automatic fire.
    // TopLevel keeps the fired streak at its world-space position as the shooter moves.
    public void Initialize(FirearmSettings settings)
    {
        _settings = settings; TopLevel = true;
        var material = new StandardMaterial3D { ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = settings.ShotColor, NoDepthTest = false };
        var box = new BoxMesh { Size = Vector3.One };
        int count = Mathf.Clamp(settings.TracerPoolSize, 1, 16);
        _tracers = new MeshInstance3D[count];
        _tracerTime = new float[count];
        for (int i = 0; i < count; i++)
        {
            var tracer = new MeshInstance3D { Name = $"Tracer_{i + 1:00}", Mesh = box,
                MaterialOverride = material, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                Visible = false };
            AddChild(tracer); _tracers[i] = tracer;
        }
        float radius = Mathf.Clamp(settings.FlashRadius, 0.01f, 0.5f);
        _flash = new MeshInstance3D { Name = "MuzzleFlash",
            Mesh = new SphereMesh { Radius = radius, Height = radius * 2, RadialSegments = 8, Rings = 4 },
            MaterialOverride = material, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            Visible = false };
        AddChild(_flash);
        Visible = false; SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    // Round-robin reuse: overlapping tracer visuals persist until their own lifetime expires.
    // If all slots are still occupied, the oldest next slot is replaced rather than allocating.
    public void ShowShot(Vector3 start, Vector3 end)
    {
        float duration = Mathf.Clamp(_settings.EffectDuration, 0.01f, 0.5f);
        int slot = _nextTracer; _nextTracer = (_nextTracer + 1) % _tracers.Length;
        var tracer = _tracers[slot];
        Vector3 direction = end - start;
        float length = direction.Length();
        if (length > 0.001f)
        {
            float width = Mathf.Clamp(_settings.TracerWidth, 0.005f, 0.2f);
            Vector3 up = Mathf.Abs(direction.Normalized().Dot(Vector3.Up)) > 0.99f ? Vector3.Right : Vector3.Up;
            var facing = Basis.LookingAt(direction, up);
            tracer.GlobalTransform = new Transform3D(
                new Basis(facing.X * width, facing.Y * width, facing.Z * length),
                (start + end) * 0.5f);
            tracer.Visible = true; _tracerTime[slot] = duration;
        }
        else { tracer.Visible = false; _tracerTime[slot] = 0; }

        _flash.GlobalPosition = start;
        _flashTime = Mathf.Clamp(_settings.FlashDuration, 0.01f, 0.25f);
        _flash.Visible = true;
        Visible = true; SetProcess(true);
    }

    // =========================================================
    // Only the few active slots are timed; sleep immediately after the final streak/flash expires.
    public override void _Process(double delta)
    {
        float dt = (float)delta;
        bool active = false;
        if (_flashTime > 0)
        {
            _flashTime = Mathf.Max(0f, _flashTime - dt);
            if (_flashTime <= 0) _flash.Visible = false; else active = true;
        }
        for (int i = 0; i < _tracerTime.Length; i++)
        {
            if (_tracerTime[i] <= 0f) continue;
            _tracerTime[i] = Mathf.Max(0f, _tracerTime[i] - dt);
            if (_tracerTime[i] <= 0f) _tracers[i].Visible = false;
            else active = true;
        }
        if (!active) { Visible = false; SetProcess(false); }
    }

    // =========================================================
    // Focus loss, unequip and scene unload cancel every visual, not just the newest shot.
    public void Stop()
    {
        _flashTime = 0; _flash.Visible = false;
        for (int i = 0; i < _tracers.Length; i++)
        { _tracerTime[i] = 0; _tracers[i].Visible = false; }
        Visible = false; SetProcess(false);
    }
}
