// Reusable moving bullet tracers and muzzle flash for hitscan firearms.
// Damage resolves immediately in FirearmController; this is visual travel along the resolved shot path.
// All meshes/materials/slots are pooled once at equip and _Process sleeps when feedback ends.
using Godot;

public partial class FirearmShotEffects : Node3D
{
    // Each visual slot retains its shot path; no meshes or data containers are allocated per bullet.
    private sealed class TracerSlot
    {
        public MeshInstance3D Core = null!;
        public MeshInstance3D Glow = null!;
        public Vector3 Start;
        public Vector3 Direction;
        public Basis Facing;
        public float Distance;
        public float Travelled;
        public float Elapsed;
        public bool Active;
    }

    private TracerSlot[] _tracers = System.Array.Empty<TracerSlot>();
    private MeshInstance3D _flash = null!;
    private FirearmSettings _settings = null!;
    private int _nextTracer;
    private int _shotCount;
    private float _flashTime;
    private MuzzleFlashLight? _light;
    private Marker3D _muzzle = null!;

    // =========================================================
    // One pool per equipped weapon. The small bright core and wider translucent halo share meshes
    // and materials between all of this weapon's tracers, without creating a Light3D for each shot.
    public void Initialize(FirearmSettings settings, Marker3D muzzle)
    {
        _settings = settings; _muzzle = muzzle; TopLevel = true;
        PhysicsInterpolationMode = PhysicsInterpolationModeEnum.Off; // Prevent recycled world-space streaks smearing across shots.
        var coreMaterial = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = settings.ShotColor.Lerp(Colors.White, 0.42f),
            NoDepthTest = false
        };
        var haloMaterial = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            AlbedoColor = new Color(settings.ShotColor.R, settings.ShotColor.G, settings.ShotColor.B, 0.24f),
            NoDepthTest = false
        };
        var unitBox = new BoxMesh { Size = Vector3.One };
        int count = Mathf.Clamp(settings.TracerPoolSize, 1, 16);
        _tracers = new TracerSlot[count];
        for (int i = 0; i < count; i++)
        {
            var glow = new MeshInstance3D
            {
                Name = $"TracerGlow_{i + 1:00}", Mesh = unitBox, MaterialOverride = haloMaterial,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off, Visible = false
            };
            var core = new MeshInstance3D
            {
                Name = $"TracerCore_{i + 1:00}", Mesh = unitBox, MaterialOverride = coreMaterial,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off, Visible = false
            };
            AddChild(glow); AddChild(core);
            _tracers[i] = new TracerSlot { Core = core, Glow = glow };
        }
        float radius = Mathf.Clamp(settings.FlashRadius, 0.01f, 0.5f);
        _flash = new MeshInstance3D
        {
            Name = "MuzzleFlash",
            Mesh = new SphereMesh { Radius = radius, Height = radius * 2, RadialSegments = 8, Rings = 4 },
            MaterialOverride = coreMaterial, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            Visible = false
        };
        AddChild(_flash);
        if (settings.EmitMuzzleLight)
        {
            _light = new MuzzleFlashLight { Name = "MuzzleLight" }; AddChild(_light);
            _light.Initialize(GD.Load<LightingSettings>("res://ENVIRONMENT/Lighting/Configuration/LightingMaster.tres"));
        }
        Visible = false; SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    // Gameplay hit/damage has already happened. Only a short visual streak is launched.
    // TracerEveryNShots can omit *visuals* on rapid fire without omitting hits, flash, light or audio.
    public void ShowShot(Vector3 start, Vector3 end)
    {
        int every = Mathf.Clamp(_settings.TracerEveryNShots, 1, 8);
        if (_shotCount++ % every == 0) LaunchTracer(start, end);

        _flash.GlobalPosition = _muzzle.GlobalPosition;
        _light?.Trigger(_muzzle.GlobalPosition);
        _flashTime = Mathf.Clamp(_settings.FlashDuration, 0.01f, 0.25f);
        _flash.Visible = true;
        Visible = true; SetProcess(true);
    }

    // =========================================================
    // Round-robin slot reuse: the oldest slot is replaced if all visuals are in flight.
    // The fixed shot direction and hit distance are computed once, not every rendered frame.
    private void LaunchTracer(Vector3 start, Vector3 end)
    {
        TracerSlot slot = _tracers[_nextTracer];
        _nextTracer = (_nextTracer + 1) % _tracers.Length;

        Vector3 path = end - start;
        float distance = path.Length();
        if (distance <= 0.005f)
        {
            HideTracer(slot); return;
        }
        slot.Start = start;
        slot.Direction = path / distance;
        slot.Distance = distance;
        slot.Elapsed = 0f;
        slot.Active = true;
        Vector3 up = Mathf.Abs(slot.Direction.Dot(Vector3.Up)) > 0.99f ? Vector3.Right : Vector3.Up;
        slot.Facing = Basis.LookingAt(slot.Direction, up);

        // Display a tiny section at the muzzle immediately, even before the next render frame.
        float trail = Mathf.Clamp(_settings.TracerLength, 0.2f, 3f);
        slot.Travelled = Mathf.Min(distance, trail * 0.25f);
        DrawTracer(slot);
        slot.Core.ResetPhysicsInterpolation();
        slot.Glow.ResetPhysicsInterpolation();
    }

    // =========================================================
    // Draw only the last TracerLength metres of the traveled path; never stretch to target.
    // The short segment is clipped at the muzzle and impact so it cannot overshoot a wall.
    private void DrawTracer(TracerSlot slot)
    {
        float segment = Mathf.Min(slot.Travelled, Mathf.Clamp(_settings.TracerLength, 0.2f, 3f));
        float centerAlongPath = slot.Travelled - segment * 0.5f;
        Vector3 center = slot.Start + slot.Direction * centerAlongPath;
        float width = Mathf.Clamp(_settings.TracerWidth, 0.005f, 0.08f);
        Basis facing = slot.Facing;
        slot.Glow.GlobalTransform = new Transform3D(
            new Basis(facing.X * (width * 3.2f), facing.Y * (width * 3.2f), facing.Z * segment), center);
        slot.Core.GlobalTransform = new Transform3D(
            new Basis(facing.X * width, facing.Y * width, facing.Z * segment), center);
        slot.Glow.Visible = true;
        slot.Core.Visible = true;
    }

    // =========================================================
    // Hide both parts of one pooled streak and return it to the dormant state.
    private static void HideTracer(TracerSlot slot)
    {
        slot.Active = false;
        slot.Core.Visible = false;
        slot.Glow.Visible = false;
    }

    // =========================================================
    // Only advances active visuals and muzzle feedback. At impact, show the final clipped
    // segment for one render, then hide it on the next frame; otherwise stop at EffectDuration.
    public override void _Process(double delta)
    {
        float dt = (float)delta;
        bool active = false;
        Vector3 renderedMuzzle = IsInstanceValid(_muzzle)
            ? _muzzle.GetGlobalTransformInterpolated().Origin : GlobalPosition;
        _light?.Advance(dt, renderedMuzzle);
        active |= _light?.IsLit == true;

        if (_flashTime > 0f)
        {
            _flashTime = Mathf.Max(0f, _flashTime - dt);
            if (_flashTime <= 0f) _flash.Visible = false;
            else
            {
                if (IsInstanceValid(_muzzle)) _flash.GlobalPosition = renderedMuzzle;
                active = true;
            }
        }
        float speed = Mathf.Clamp(_settings.TracerSpeed, 80f, 1000f);
        float lifetime = Mathf.Clamp(_settings.EffectDuration, 0.03f, 0.6f);
        foreach (var slot in _tracers)
        {
            if (!slot.Active) continue;
            if (slot.Travelled >= slot.Distance || slot.Elapsed >= lifetime)
            { HideTracer(slot); continue; }

            slot.Elapsed += dt;
            slot.Travelled = Mathf.Min(slot.Distance, slot.Travelled + speed * dt);
            if (slot.Elapsed >= lifetime && slot.Travelled < slot.Distance)
            { HideTracer(slot); continue; }
            DrawTracer(slot);
            active = true;
        }
        if (!active) { Visible = false; SetProcess(false); }
    }

    // =========================================================
    // Unequip, focus loss or scene shutdown clears every moving tracer and muzzle effect immediately.
    public void Stop()
    {
        _light?.StopLight();
        _flashTime = 0f; _flash.Visible = false;
        foreach (var slot in _tracers) HideTracer(slot);
        _shotCount = 0;
        Visible = false; SetProcess(false);
    }
}
