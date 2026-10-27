// Top-down lodge presentation: event-driven local-space occupancy, stair transition and composed section fades.
// No gameplay simulation, collision changes, per-building polling, or camera behaviour in the art generator.
using Godot;
using System.Collections.Generic;

public partial class BuildingFloorVisibility : Node3D
{
    public enum Occupancy { Outside, Ground, OnStairs, Upper }
    public Occupancy CurrentFloor { get; private set; } = Occupancy.Outside;
    private Building002Mesh _building = null!;
    private PlayerController _player = null!;
    private Building002Settings _settings = null!;
    private readonly List<CameraOccluder> _upper = new(), _lowerWalls = new();
    private CameraOccluder _roof = null!;
    private float _upperOpacity = 1, _roofOpacity = 1, _upperGoal = 1, _roofGoal = 1;
    private float _width, _depth, _height;
    private bool _inside, _upstairs, _initialized;

    // =========================================================
    // World passes an explicit prefab/player only for the top-down camera; cache the known mesh groups once.
    public void Initialize(Building002Mesh building, PlayerController player)
    {
        _building = building; _player = player;
        _settings = (Building002Settings)building.Settings.Duplicate(true);
        _width = Mathf.Clamp(_settings.Width, 14, 24); _depth = Mathf.Clamp(_settings.Depth, 10, 18);
        _height = building.GetNode<Node3D>("Generated/Level_1").GetMeta("floor_elevation").AsSingle();
        const string ground = "Generated/Level_0/Visuals/", upper = "Generated/Level_1/Visuals/";
        _lowerWalls.Add(Section(ground + "Exterior", true));
        _lowerWalls.Add(Section(ground + "Partitions", true));
        _upper.Add(Section(upper + "FloorDeck", false));
        _upper.Add(Section(upper + "Furnishings", false));
        _upper.Add(Section(upper + "ExteriorWalls", true));
        _upper.Add(Section(upper + "Partitions", true));
        _roof = Section(upper + "RoofVisuals", true);
        SetProcess(false); SetPhysicsProcess(false);
        _player.PositionChanged += OnPlayerMoved; _initialized = true; OnPlayerMoved(player.GlobalPosition);
    }

    // =========================================================
    // Each mesh has exactly one fade owner. Contents/deck have presentation fades but no obstruction proxy.
    private CameraOccluder Section(string path, bool proxy)
    {
        var section = new CameraOccluder { Name = path.Replace('/', '_'),
            ObjectRoot = _building.GetNode<Node3D>(path), CreateObstructionProxy = proxy,
            FadeIndividualMeshes = path.EndsWith("Exterior") || path.EndsWith("ExteriorWalls") || path.EndsWith("Partitions") };
        AddChild(section); section.InitializeBounds(); return section;
    }

    // =========================================================
    // Local feet position respects terrain placement/rotation. Hysteresis avoids boundary/landing oscillation.
    private void OnPlayerMoved(Vector3 position)
    {
        Vector3 local = _building.ToLocal(position);
        float margin = _inside ? 0.15f : -0.05f;
        _inside = Mathf.Abs(local.X) < _width * 0.5f + margin
            && Mathf.Abs(local.Z) < _depth * 0.5f + margin && local.Y > -0.5f && local.Y < _height + 2.5f;
        float hysteresis = Mathf.Clamp(_settings.LandingHysteresis, 0.1f, 0.6f);
        _upstairs = _inside && local.Y >= _height - (_upstairs ? hysteresis : 0.12f);
        CurrentFloor = !_inside ? Occupancy.Outside : _upstairs ? Occupancy.Upper
            : local.Y > 0.3f ? Occupancy.OnStairs : Occupancy.Ground;
        // Keep the upper deck faded until the player approaches the top; the staircase itself is never a fade target.
        float landing = Mathf.SmoothStep(_height - 1.8f, _height - 0.12f, local.Y);
        float hidden = Mathf.Clamp(_settings.HiddenUpperOpacity, 0, 1);
        _upperGoal = !_inside || _upstairs ? 1 : Mathf.Lerp(hidden, 1, landing);
        _roofGoal = _inside ? Mathf.Clamp(_settings.IndoorRoofOpacity, 0, 1) : 1;
        foreach (var wall in _lowerWalls) wall.SetCameraFadeEnabled(!_upstairs);
        if (_upperOpacity != _upperGoal || _roofOpacity != _roofGoal) SetProcess(true);
    }

    // =========================================================
    // Only transitioning presentation runs per frame; composed camera factors cannot restore a hidden storey.
    public override void _Process(double delta)
    {
        float step = (float)delta / Mathf.Max(0.02f, _settings.FloorFadeSeconds);
        _upperOpacity = Mathf.MoveToward(_upperOpacity, _upperGoal, step);
        _roofOpacity = Mathf.MoveToward(_roofOpacity, _roofGoal, step);
        foreach (var section in _upper) section.SetPresentationOpacity(_upperOpacity);
        _roof.SetPresentationOpacity(_roofOpacity);
        if (_upperOpacity == _upperGoal && _roofOpacity == _roofGoal) SetProcess(false);
    }

    // =========================================================
    // Release explicit external subscription, restore presentation on removal, and let proxies unregister normally.
    public override void _ExitTree()
    {
        if (!_initialized) return;
        if (IsInstanceValid(_player)) _player.PositionChanged -= OnPlayerMoved;
        foreach (var section in _upper) if (IsInstanceValid(section)) section.SetPresentationOpacity(1);
        foreach (var section in _lowerWalls) if (IsInstanceValid(section)) section.SetCameraFadeEnabled(true);
        if (IsInstanceValid(_roof)) _roof.SetPresentationOpacity(1);
        _initialized = false;
    }
}
