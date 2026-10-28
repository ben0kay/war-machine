// Shared depth-overlay fog: default overhead radius, optional true camera distance for FPS. No simulation callbacks.
using Godot;

public partial class PlayerDistanceFog : Node
{
    public event System.Action? SettingsApplied;
    public bool FullyOpaque { get; private set; }
    public float OpaqueRadius { get; private set; }
    public bool CameraMetric => _cameraDistance;
    public bool OpaqueBackdrop { get; private set; }
    private Camera3D _camera = null!;
    private PlayerController _player = null!;
    private EnvironmentConfig _settings = null!, _atmosphere = null!;
    private bool _atmosphereSubscribed;
    private ShaderMaterial? _material;
    private MeshInstance3D? _overlay;
    private bool _subscribed, _cameraDistance;

    // =========================================================
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); }

    // =========================================================
    public void Initialize(Camera3D camera, PlayerController player, EnvironmentConfig settings, bool cameraDistance = false, EnvironmentConfig? atmosphere = null)
    { _camera = camera; _player = player; _settings = settings; _atmosphere = atmosphere ?? settings; _cameraDistance = cameraDistance; ApplySettings(); }

    // =========================================================
    // Zero/disabled means no draw and no movement subscription; sliders explicitly reactivate it.
    public void ApplySettings()
    {
        float opacity = float.IsFinite(_settings.DistanceFogOpacity) ? Mathf.Clamp(_settings.DistanceFogOpacity, 0, 1) : 0;
        bool enabled = _settings.DistanceFogEnabled && opacity > 0;
        FullyOpaque = enabled && opacity >= 1;
        if (enabled && _overlay == null)
        {
            _material = new ShaderMaterial { Shader = GD.Load<Shader>("res://ENVIRONMENT/Fog/Shaders/PlayerDistanceFog.gdshader"), RenderPriority = 127 };
            _overlay = new MeshInstance3D { Name = "DistanceFogOverlay", Position = new Vector3(0, 0, -1),
                Mesh = new QuadMesh { Size = Vector2.One * 2 }, MaterialOverride = _material,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off, ExtraCullMargin = 16384,
                IgnoreOcclusionCulling = true };
            _camera.AddChild(_overlay); _overlay.SetProcess(false); _overlay.SetPhysicsProcess(false);
        }
        if (IsInstanceValid(_overlay)) _overlay!.Visible = enabled;
        if (enabled && !_cameraDistance && !_subscribed) { _player.PositionChanged += UpdateCenter; _subscribed = true; }
        else if ((!enabled || _cameraDistance) && _subscribed) { _player.PositionChanged -= UpdateCenter; _subscribed = false; }
        if (enabled && !_atmosphereSubscribed) { _atmosphere.AtmosphereChanged += ApplyAtmosphere; _atmosphereSubscribed = true; }
        else if (!enabled && _atmosphereSubscribed) { _atmosphere.AtmosphereChanged -= ApplyAtmosphere; _atmosphereSubscribed = false; }
        if (!enabled) { OpaqueRadius = 0; SettingsApplied?.Invoke(); return; }
        _material!.SetShaderParameter("camera_distance", _cameraDistance);
        float start = float.IsFinite(_settings.DistanceFogStart) ? Mathf.Max(1, _settings.DistanceFogStart) : 24;
        float end = float.IsFinite(_settings.DistanceFogEnd) ? Mathf.Max(start + 1, _settings.DistanceFogEnd) : start + 24;
        float variation = float.IsFinite(_settings.DistanceFogEdgeVariation)
            ? Mathf.Clamp(_settings.DistanceFogEdgeVariation, 0, Mathf.Min(8, start * 0.25f)) : 0;
        OpaqueRadius = end + variation;
        _material!.SetShaderParameter("clear_radius", start);
        _material.SetShaderParameter("fog_radius", end);
        _material.SetShaderParameter("opacity", opacity);
        ApplyAtmosphere();
        _material.SetShaderParameter("edge_variation", variation);
        UpdateCenter(_player.GlobalPosition);
        SettingsApplied?.Invoke();
    }

    // =========================================================
    // Shared solar state tints each view's own daytime fog colour without changing its distance/culling settings.
    private void ApplyAtmosphere()
    {
        if (_material == null) return;
        float day = _atmosphere.Daylight;
        Color tint = !_atmosphere.TimeResponsiveFog ? _settings.DistanceFogColor :
            day < 0.5f ? _atmosphere.NightFogColor.Lerp(_atmosphere.DuskFogColor, day * 2) :
            _atmosphere.DuskFogColor.Lerp(_settings.DistanceFogColor, day * 2 - 1);
        _material.SetShaderParameter("fog_color", tint);
        float skyOpacity = _cameraDistance ? Mathf.Clamp(_atmosphere.FpsSkyFogOpacity, 0, 1) : 1f;
        _material.SetShaderParameter("sky_opacity", skyOpacity);
        // Removing opaque geometry must not reveal a clearer sky where a solid fog silhouette used to be.
        bool opaqueBackdrop = skyOpacity >= 1;
        if (OpaqueBackdrop != opaqueBackdrop) { OpaqueBackdrop = opaqueBackdrop; SettingsApplied?.Invoke(); }
    }

    // =========================================================
    // The player position signal is silent at rest; zoom/projection matrices are supplied directly by the renderer.
    private void UpdateCenter(Vector3 position)
        => _material!.SetShaderParameter("player_xz", new Vector2(position.X, position.Z));

    // =========================================================
    public override void _ExitTree()
    {
        if (_atmosphereSubscribed) _atmosphere.AtmosphereChanged -= ApplyAtmosphere;
        if (_subscribed && IsInstanceValid(_player)) _player.PositionChanged -= UpdateCenter;
        if (IsInstanceValid(_overlay)) _overlay!.QueueFree();
        _subscribed = false;
    }
}
