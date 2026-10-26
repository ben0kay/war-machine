// Close ground wisps: nine terrain-tangent FogVolumes, shader animation only, event-driven placement/shelter.
using Godot;
using System.Collections.Generic;

public partial class GroundHaze : Node3D
{
    private EnvironmentConfig _settings = null!;
    private Godot.Environment _environment = null!;
    private GodotObject _data = null!;
    private ExplorationController _explorer = null!;
    private Camera3D _camera = null!;
    private WildernessCave _cave = null!;
    private readonly List<FogVolume> _volumes = new();
    private readonly List<ShaderMaterial> _materials = new();
    private Vector2I _tile = new(int.MinValue, int.MinValue);
    private Vector3 _lastPosition = new(float.PositiveInfinity, 0, 0);
    private bool _initialized, _ownsFog, _oldEnabled;
    private float _oldDensity, _oldLength;
    private const float TileWidth = 6;

    // =========================================================
    // Reuse Sky3D's Environment; no competing sky/distance-fog owner or camera-following floating layer.
    public void Initialize(Node3D terrain, ExplorationController explorer, EnvironmentConfig settings, Node sky, WildernessCave cave)
    {
        if (_initialized) Disconnect();
        _data = terrain.Get("data").AsGodotObject(); _explorer = explorer; _settings = settings; _cave = cave;
        _camera = explorer.GetNode<Camera3D>("Camera3D");
        _environment = (Godot.Environment)sky.GetNode("SkyDome").Get("environment").AsGodotObject();
        _explorer.ViewPositionChanged += OnMoved; _explorer.CameraModeChanged += OnCameraMode;
        _initialized = true; SetProcess(false); SetPhysicsProcess(false); ApplySettings();
    }

    // =========================================================
    // Sliders update only on change; zero/OFF releases all volumes and restores previous volumetric settings.
    public void ApplySettings()
    {
        if (!_initialized) return;
        if (!_settings.GroundHazeEnabled || _settings.GroundHazeDensity <= 0)
        { ClearVolumes(); RestoreFog(); return; }
        string renderer = ProjectSettings.GetSetting("rendering/renderer/rendering_method", "forward_plus").AsString();
        if (renderer != "forward_plus") { GD.PushWarning("Ground wisps require Forward+."); ClearVolumes(); RestoreFog(); return; }
        if (_volumes.Count == 0)
        {
            var shader = GD.Load<Shader>("res://ENVIRONMENT/Weather/Shaders/GroundWisps.gdshader");
            for (int i = 0; i < 9; i++)
            {
                var material = new ShaderMaterial { Shader = shader };
                var volume = new FogVolume { Shape = RenderingServer.FogVolumeShape.Box, Material = material };
                AddChild(volume); _volumes.Add(volume); _materials.Add(material);
            }
        }
        Vector2 wind = _settings.WindDirection.LengthSquared() > 0.001f ? _settings.WindDirection.Normalized() : Vector2.Right;
        for (int i = 0; i < _volumes.Count; i++)
        {
            _volumes[i].Size = new Vector3(TileWidth + 2, Mathf.Clamp(_settings.GroundHazeHeight, 0.25f, 8) + 4, TileWidth + 2);
            var material = _materials[i];
            material.SetShaderParameter("density", Mathf.Clamp(_settings.GroundHazeDensity, 0, 0.5f));
            material.SetShaderParameter("layer_height", Mathf.Clamp(_settings.GroundHazeHeight, 0.25f, 8));
            material.SetShaderParameter("noise_scale", Mathf.Clamp(_settings.GroundHazeNoiseScale, 0.05f, 1));
            material.SetShaderParameter("drift_speed", Mathf.Clamp(_settings.GroundHazeDriftSpeed, 0, 2));
            material.SetShaderParameter("contrast", Mathf.Clamp(_settings.GroundHazeContrast, 0, 1));
            material.SetShaderParameter("wind_direction", wind);
        }
        _tile = new Vector2I(int.MinValue, int.MinValue); ForceRefresh();
    }

    // =========================================================
    // Camera mode changes affect eye-height shelter; wake explicitly rather than polling input mode.
    private void OnCameraMode(bool firstPerson) => ForceRefresh();

    // =========================================================
    // Meaningful UI/mode events bypass the small movement threshold directly.
    private void ForceRefresh()
    { _lastPosition = new Vector3(float.PositiveInfinity, 0, 0); OnMoved(_explorer.GlobalPosition); }

    // =========================================================
    // Sample only on six-metre tile crossings; shelter/altitude checks are limited to 1.5m movement increments.
    private void OnMoved(Vector3 position)
    {
        if (_volumes.Count == 0 || position.DistanceSquaredTo(_lastPosition) < 2.25f) return;
        _lastPosition = position;
        float viewerGround = _data.Call("get_height", position).AsSingle();
        float heightAbove = _camera.GlobalPosition.Y - viewerGround;
        bool visible = float.IsFinite(viewerGround) && heightAbove > -0.5f && heightAbove < 35
            && !_cave.IsSheltered(_camera.GlobalPosition);
        var tile = new Vector2I(Mathf.FloorToInt(position.X / TileWidth), Mathf.FloorToInt(position.Z / TileWidth));
        bool changed = tile != _tile;
        if (changed)
        {
            _tile = tile; int i = 0;
            for (int x = -1; x <= 1; x++) for (int z = -1; z <= 1; z++)
            {
                Vector3 point = new((tile.X + x + 0.5f) * TileWidth, 0, (tile.Y + z + 0.5f) * TileWidth);
                float height = _data.Call("get_height", point).AsSingle();
                bool valid = float.IsFinite(height); var volume = _volumes[i];
                volume.SetMeta("valid_ground", valid);
                if (valid)
                {
                    float hx = _data.Call("get_height", point + Vector3.Right).AsSingle();
                    float hz = _data.Call("get_height", point + Vector3.Back).AsSingle();
                    Vector2 slope = new(float.IsFinite(hx) ? hx - height : 0, float.IsFinite(hz) ? hz - height : 0);
                    if (slope.LengthSquared() > 0.25f) { valid = false; volume.SetMeta("valid_ground", false); }
                    point.Y = height + _settings.GroundHazeHeight * 0.5f; volume.GlobalPosition = point;
                    _materials[i].SetShaderParameter("ground_height", height);
                    _materials[i].SetShaderParameter("ground_slope", slope);
                }
                i++;
            }
        }
        bool anyVisible = false;
        foreach (var volume in _volumes)
        {
            bool show = visible && volume.GetMeta("valid_ground", false).AsBool();
            if (volume.Visible != show) volume.Visible = show;
            anyVisible |= show;
        }
        if (anyVisible) EnableFog(); else RestoreFog();
    }

    // =========================================================
    // A shorter froxel range favours close detail; global density stays zero so only local wisps contribute.
    private void EnableFog()
    {
        if (!_ownsFog)
        {
            _oldEnabled = _environment.VolumetricFogEnabled; _oldDensity = _environment.VolumetricFogDensity;
            _oldLength = _environment.VolumetricFogLength; _ownsFog = true;
            _environment.VolumetricFogEnabled = true; _environment.VolumetricFogDensity = 0; _environment.VolumetricFogLength = 48;
        }
    }

    // =========================================================
    // No extra global fog pass is left requested by this feature when disabled, sheltered or far above the ground.
    private void RestoreFog()
    {
        if (!_ownsFog) return;
        _environment.VolumetricFogEnabled = _oldEnabled; _environment.VolumetricFogDensity = _oldDensity;
        _environment.VolumetricFogLength = _oldLength; _ownsFog = false;
    }

    // =========================================================
    // Cached nodes persist through shelter transitions; OFF removes them completely.
    private void ClearVolumes()
    {
        foreach (var volume in _volumes) { RemoveChild(volume); volume.QueueFree(); }
        _volumes.Clear(); _materials.Clear(); _tile = new Vector2I(int.MinValue, int.MinValue);
    }

    // =========================================================
    // Reinitialization and unload release both movement subscriptions and temporary fog ownership.
    private void Disconnect()
    {
        if (IsInstanceValid(_explorer)) { _explorer.ViewPositionChanged -= OnMoved; _explorer.CameraModeChanged -= OnCameraMode; }
        RestoreFog(); ClearVolumes(); _initialized = false;
    }

    // =========================================================
    public override void _ExitTree()
    {
        if (!_initialized) return;
        if (IsInstanceValid(_explorer)) { _explorer.ViewPositionChanged -= OnMoved; _explorer.CameraModeChanged -= OnCameraMode; }
        if (IsInstanceValid(_environment)) RestoreFog();
        // Children are already exiting here; let their parent free them rather than removing them mid-exit.
        _volumes.Clear(); _materials.Clear(); _initialized = false;
    }
}
