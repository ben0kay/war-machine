// Generic level/roof/wall presentation from cached structure metadata; no building-specific paths or dimensions.
using Godot;
using System.Collections.Generic;

public partial class BuildingFloorVisibility : Node3D
{
    public bool IsInside { get; private set; }
    public int CurrentLevel { get; private set; } = -1;
    public bool OnStairs { get; private set; }
    private sealed class Part
    { public CameraOccluder Owner = null!; public StructureLayout.Part Definition = null!; public float Current = 1, Goal = 1; }
    private readonly List<Part> _parts = new();
    private Node3D _building = null!;
    private PlayerController _player = null!;
    private StructureSettings _settings = null!;
    private StructureLayout _layout = null!;
    private bool _initialized;

    // =========================================================
    public void Initialize(Node3D building, PlayerController player, StructureSettings settings, StructureLayout layout)
    {
        _building = building; _player = player; _settings = settings; _layout = layout;
        foreach (var definition in layout.Parts)
        {
            bool wall = definition.Role == "walls", roof = definition.Role == "roof";
            var owner = new CameraOccluder { Name = $"Section_{_parts.Count}", ObjectRoot = definition.Root,
                CreateObstructionProxy = wall || roof, FadeIndividualMeshes = wall };
            AddChild(owner); owner.InitializeBounds();
            _parts.Add(new Part { Owner = owner, Definition = definition });
        }
        SetProcess(false); SetPhysicsProcess(false);
        _initialized = true; _player.PositionChanged += OnPlayerMoved; OnPlayerMoved(player.GlobalPosition);
    }

    // =========================================================
    // Local feet + authored elevations support multiple storeys, landing hysteresis and separate canopy volumes.
    private void OnPlayerMoved(Vector3 position)
    {
        Vector3 local = _building.ToLocal(position); bool inside = false;
        foreach (var zone in _layout.Zones)
            if (zone.Grow(IsInside ? 0.15f : -0.05f).HasPoint(local)) { inside = true; break; }
        int previous = CurrentLevel; IsInside = inside; CurrentLevel = inside ? _layout.Floors[0].Level : -1;
        float elevation = _layout.Floors[0].Elevation;
        if (inside)
            foreach (var floor in _layout.Floors)
            {
                float margin = previous >= floor.Level ? Mathf.Clamp(_settings.LandingHysteresis, 0.1f, 0.6f) : 0.12f;
                if (local.Y >= floor.Elevation - margin) { CurrentLevel = floor.Level; elevation = floor.Elevation; }
            }
        OnStairs = inside && local.Y > elevation + 0.3f;
        bool moving = false;
        foreach (var part in _parts)
        {
            var d = part.Definition; float goal = 1;
            if (inside && d.Role != "stairs")
            {
                if (d.Role == "roof" && d.Level >= CurrentLevel) goal = Mathf.Clamp(_settings.IndoorRoofOpacity, 0, 1);
                else if (d.Level > CurrentLevel)
                {
                    float targetElevation = elevation;
                    foreach (var floor in _layout.Floors) if (floor.Level == d.Level) { targetElevation = floor.Elevation; break; }
                    float blend = Mathf.SmoothStep(targetElevation - 1.8f, targetElevation - 0.12f, local.Y);
                    goal = Mathf.Lerp(Mathf.Clamp(_settings.HiddenUpperOpacity, 0, 1), 1, blend);
                }
            }
            part.Goal = goal;
            part.Owner.SetCameraFadeEnabled(!inside || d.Level >= CurrentLevel);
            if (part.Current != goal) moving = true;
        }
        SetProcess(moving);
    }

    // =========================================================
    public override void _Process(double delta)
    {
        bool moving = false; float step = (float)delta / Mathf.Max(0.02f, _settings.FloorFadeSeconds);
        foreach (var part in _parts)
        {
            part.Current = Mathf.MoveToward(part.Current, part.Goal, step);
            part.Owner.SetPresentationOpacity(part.Current);
            if (part.Current != part.Goal) moving = true;
        }
        if (!moving) SetProcess(false);
    }

    // =========================================================
    public void Shutdown()
    {
        if (!_initialized) return;
        if (IsInstanceValid(_player)) _player.PositionChanged -= OnPlayerMoved;
        foreach (var part in _parts) if (IsInstanceValid(part.Owner))
        { part.Owner.SetPresentationOpacity(1); part.Owner.SetCameraFadeEnabled(true); }
        SetProcess(false); _initialized = false;
    }

    // =========================================================
    public override void _ExitTree() => Shutdown();
}
