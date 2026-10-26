// Optional terrain-anchored volumetric haze. Camera height never lifts it away from the ground.
using Godot;
using System.Collections.Generic;

public partial class GroundHaze : Node3D
{
    private EnvironmentConfig _settings = null!;
    private Godot.Environment _environment = null!;
    private GodotObject _data = null!;
    private ExplorationController _explorer = null!;
    private WildernessCave _cave = null!;
    private readonly List<FogVolume> _volumes = new();
    private Vector2I _tile = new(int.MinValue, int.MinValue);
    private bool _initialized, _ownsFog, _oldEnabled, _sheltered;
    private float _oldDensity;
    private const float TileWidth = 24;

    // =========================================================
    // Attach to Sky3D's existing Environment resource without replacing its sky, lights or distance fog.
    public void Initialize(Node3D terrain, ExplorationController explorer, EnvironmentConfig settings, Node sky, WildernessCave cave)
    {
        _data = terrain.Get("data").AsGodotObject(); _explorer = explorer; _settings = settings; _cave = cave;
        _environment = (Godot.Environment)sky.GetNode("SkyDome").Get("environment").AsGodotObject();
        _explorer.ViewPositionChanged += OnMoved; _initialized = true;
        SetProcess(false); SetPhysicsProcess(false); ApplySettings();
    }

    // =========================================================
    // Toggle/slider events update a handful of shared GPU volumes; OFF frees them and restores engine fog settings.
    public void ApplySettings()
    {
        if (!_initialized) return;
        foreach (var volume in _volumes) { RemoveChild(volume); volume.QueueFree(); } _volumes.Clear();
        _tile = new Vector2I(int.MinValue, int.MinValue);
        if (!_settings.GroundHazeEnabled || _settings.GroundHazeDensity <= 0)
        { RestoreFog(); return; }
        string renderer = ProjectSettings.GetSetting("rendering/renderer/rendering_method", "forward_plus").AsString();
        if (renderer != "forward_plus") { GD.PushWarning("Ground haze requires Forward+."); RestoreFog(); return; }
        if (!_ownsFog)
        {
            _oldEnabled = _environment.VolumetricFogEnabled; _oldDensity = _environment.VolumetricFogDensity; _ownsFog = true;
        }
        _environment.VolumetricFogEnabled = true;
        _environment.VolumetricFogDensity = 0; // Local volumes only; no additional global fog blanket.
        var material = new FogMaterial { Density = _settings.GroundHazeDensity, Albedo = new Color(0.62f, 0.69f, 0.72f), HeightFalloff = 0, EdgeFade = 0.25f };
        for (int i = 0; i < 9; i++)
        {
            var volume = new FogVolume { Shape = RenderingServer.FogVolumeShape.Box, Size = new Vector3(TileWidth + 1, _settings.GroundHazeHeight, TileWidth + 1), Material = material };
            AddChild(volume); _volumes.Add(volume);
        }
        OnMoved(_explorer.GlobalPosition);
    }

    // =========================================================
    // Small local tiles sample saved/runtime Terrain3D height only on tile crossings. Shelter updates only on movement.
    private void OnMoved(Vector3 position)
    {
        if (_volumes.Count == 0) return;
        bool sheltered = _cave.IsSheltered(position + Vector3.Up * (_explorer.FirstPerson ? _explorer.Settings.EyeHeight : 0));
        var tile = new Vector2I(Mathf.FloorToInt(position.X / TileWidth), Mathf.FloorToInt(position.Z / TileWidth));
        bool changed = tile != _tile;
        if (!changed && sheltered == _sheltered) return;
        _tile = tile; _sheltered = sheltered;
        int i = 0;
        for (int x = -1; x <= 1; x++) for (int z = -1; z <= 1; z++)
        {
            var volume = _volumes[i++];
            if (changed)
            {
                var point = new Vector3((tile.X + x + 0.5f) * TileWidth, 0, (tile.Y + z + 0.5f) * TileWidth);
                float height = _data.Call("get_height", point).AsSingle();
                volume.SetMeta("valid_ground", float.IsFinite(height));
                if (float.IsFinite(height)) { point.Y = height + _settings.GroundHazeHeight * 0.5f - 0.15f; volume.GlobalPosition = point; }
            }
            bool visible = !_sheltered && volume.GetMeta("valid_ground", false).AsBool();
            if (volume.Visible != visible) volume.Visible = visible;
        }
    }

    // =========================================================
    private void RestoreFog()
    {
        if (!_ownsFog) return;
        _environment.VolumetricFogEnabled = _oldEnabled; _environment.VolumetricFogDensity = _oldDensity; _ownsFog = false;
    }
    // =========================================================
    public override void _ExitTree()
    {
        if (!_initialized) return;
        if (IsInstanceValid(_explorer)) _explorer.ViewPositionChanged -= OnMoved;
        if (IsInstanceValid(_environment)) RestoreFog();
    }
}
