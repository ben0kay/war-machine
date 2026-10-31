// DF2 multi-zone owner: survey authored areas once; reuse existing chunk/LOD forest placement.
using Godot;
using System;
using System.Collections.Generic;

public partial class ForestZonesPlacement : Node3D, ICameraOcclusionProvider
{
    public VegetationVisibilityPolicy Visibility { get; set; } = new(new VegetationVisibilitySettings());
    public event Action? OcclusionChanged;
    public int ApprovedCount => _approved.Count;
    public IReadOnlyList<Rect2> ApprovedAreas => _areas;

    private const int MaxZones = 12;
    private sealed class ApprovedZone
    {
        public readonly ForestSettings Settings;
        public readonly Rect2 Area;
        public readonly Godot.Collections.Array<Rect2> Reservations;
        public ApprovedZone(ForestSettings settings, Rect2 area, Godot.Collections.Array<Rect2> reservations)
        { Settings = settings; Area = area; Reservations = reservations; }
    }
    private readonly List<ApprovedZone> _approved = new();
    private readonly List<Rect2> _areas = new();
    private readonly List<ForestPlacement> _placements = new();

    // =========================================================
    // All regions use original structure/road/actor reservations, never another forest's
    // full exclusion rectangle. The latter is used ONLY to prevent overlapping regions.
    public void PrepareZones(GodotObject data, WorldSettings world, Godot.Collections.Array<Rect2> pineExclusions)
    {
        _approved.Clear(); _areas.Clear();
        var original = new Godot.Collections.Array<Rect2>();
        foreach (Rect2 area in pineExclusions) original.Add(area);
        var configured = world.ForestZones?.Zones;
        int count = configured != null && configured.Count > 0 ? configured.Count :
            (world.DenseForest != null ? 1 : 0);
        if (count > MaxZones)
            GD.PushWarning($"DF2: only the first {MaxZones} forest zones of {count} are supported.");
        for (int i = 0; i < Math.Min(count, MaxZones); i++)
        {
            ForestSettings? source = configured != null && configured.Count > 0
                ? configured[i] : world.DenseForest;
            if (source == null || (!source.Enabled && !source.ShowDebugOutline)) continue;
            var settings = (ForestSettings)source.Duplicate(true);
            if (!ForestZone.TrySurvey(data, settings, world, original, out Rect2 area, out string report))
            {
                GD.PushWarning($"DF2 zone {i + 1} rejected (pines and rocks preserved): {report}");
                continue;
            }
            bool overlaps = false;
            foreach (Rect2 accepted in _areas)
                if (accepted.Grow(2f).Intersects(area.Grow(2f))) { overlaps = true; break; }
            if (overlaps)
            {
                GD.PushWarning($"DF2 zone {i + 1} overlaps an approved zone; skipped.");
                continue;
            }
            GD.Print($"DF2 zone {i + 1} approved: {report}");
            if (settings.ShowDebugOutline) ForestZone.ShowFootprint(this, data, area);
            if (!settings.Enabled) continue;
            var reservations = new Godot.Collections.Array<Rect2>();
            foreach (Rect2 blocked in original) reservations.Add(blocked);
            _approved.Add(new ApprovedZone(settings, area, reservations));
            _areas.Add(area);
            pineExclusions.Add(area);
        }
        GD.Print($"DF2 forest zones: {_approved.Count} active / {count} configured.");
    }

    // =========================================================
    // Each existing DF2 instance owns its own deterministic layout and resident MultiMesh chunks.
    // The manager itself has no idle process and every instance shares one fog-driven LOD policy.
    public void InitializeView(Node3D terrain, Node3D player)
    {
        SetProcess(false); SetPhysicsProcess(false);
        for (int i = 0; i < _approved.Count; i++)
        {
            var zone = _approved[i];
            var placement = new ForestPlacement { Name = $"DenseForestZone{i + 1}", Visibility = Visibility };
            AddChild(placement);
            placement.OcclusionChanged += OnChildOcclusionChanged;
            _placements.Add(placement);
            placement.InitializeView(terrain, player, zone.Settings, zone.Area, zone.Reservations);
        }
    }

    // =========================================================
    public void UpdateAnchor(Vector3 position)
    { foreach (var placement in _placements) placement.UpdateAnchor(position); }

    public void UpdateView(float viewOffset)
    { foreach (var placement in _placements) placement.UpdateView(viewOffset); }

    public void ApplySharedTint(Color tint, float strength)
    { foreach (var placement in _placements) placement.ApplySharedTint(tint, strength); }

    public void CollectObstructions(Vector3 from, Vector3 to, float radius,
        HashSet<ICameraFadeTarget> results)
    { foreach (var placement in _placements) placement.CollectObstructions(from, to, radius, results); }

    private void OnChildOcclusionChanged() => OcclusionChanged?.Invoke();

    // =========================================================
    public override void _ExitTree()
    {
        foreach (var placement in _placements)
            if (IsInstanceValid(placement)) placement.OcclusionChanged -= OnChildOcclusionChanged;
    }
}
