// Shared depth-overlay fog: default overhead radius, optional true camera distance for FPS. No simulation callbacks.
using Godot;

public partial class PlayerDistanceFog : Node
{
    private Camera3D _camera = null!;
    private PlayerController _player = null!;
    private EnvironmentConfig _settings = null!;
    private ShaderMaterial? _material;
    private MeshInstance3D? _overlay;
    private bool _subscribed, _cameraDistance;

    // =========================================================
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); }

    // =========================================================
    public void Initialize(Camera3D camera, PlayerController player, EnvironmentConfig settings, bool cameraDistance = false)
    { _camera = camera; _player = player; _settings = settings; _cameraDistance = cameraDistance; ApplySettings(); }

    // =========================================================
    // Zero/disabled means no draw and no movement subscription; sliders explicitly reactivate it.
    public void ApplySettings()
    {
        bool enabled = _settings.DistanceFogEnabled && _settings.DistanceFogOpacity > 0;
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
        if (!enabled) return;
        _material!.SetShaderParameter("camera_distance", _cameraDistance);
        float start = Mathf.Max(1, _settings.DistanceFogStart);
        _material!.SetShaderParameter("clear_radius", start);
        _material.SetShaderParameter("fog_radius", Mathf.Max(start + 1, _settings.DistanceFogEnd));
        _material.SetShaderParameter("opacity", Mathf.Clamp(_settings.DistanceFogOpacity, 0, 1));
        _material.SetShaderParameter("fog_color", _settings.DistanceFogColor);
        _material.SetShaderParameter("edge_variation", Mathf.Clamp(_settings.DistanceFogEdgeVariation, 0, Mathf.Min(8, start * 0.25f)));
        UpdateCenter(_player.GlobalPosition);
    }

    // =========================================================
    // The player position signal is silent at rest; zoom/projection matrices are supplied directly by the renderer.
    private void UpdateCenter(Vector3 position)
        => _material!.SetShaderParameter("player_xz", new Vector2(position.X, position.Z));

    // =========================================================
    public override void _ExitTree()
    {
        if (_subscribed && IsInstanceValid(_player)) _player.PositionChanged -= UpdateCenter;
        if (IsInstanceValid(_overlay)) _overlay!.QueueFree();
        _subscribed = false;
    }
}
