// Shared 32m foliage residency/scheduling: one player anchor, fog envelope and bounded main-thread queue.
// Species renderers only build/release their own art. No per-family player-distance streaming in managed mode.
using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public partial class FoliageManager : Node3D
{
    private const int Grass = 0, Pines = 1, Forest = 2, Rocks = 3;
    private const int AllFamilies = 15;
    private readonly Dictionary<Vector2I, ChunkState> _chunks = new();
    private readonly HashSet<Vector2I> _wanted = new();
    private readonly List<Job> _jobs = new();
    private readonly List<Vector2I> _remove = new();
    private PlayerController _player = null!;
    private VegetationVisibilityPolicy _visibility = null!;
    private StylizedGrassPlacement? _grass;
    private PinePlacement? _pines;
    private ForestZonesPlacement? _forest;
    private ContentScatter? _rocks;
    private FoliageSettings _settings = null!;
    private Vector2I _cell = new(int.MinValue, int.MinValue);
    private Vector3 _position;
    private int _worldSize, _cursor;
    private bool _ready;

    private sealed class ChunkState { public int Ready; }
    private readonly struct Job
    {
        public readonly Vector2I Key;
        public readonly int Family;
        public readonly bool Release;
        public readonly float Priority;
        public Job(Vector2I key, int family, bool release, float priority)
        { Key = key; Family = family; Release = release; Priority = priority; }
    }

    public int ActiveChunkCoordinates => _chunks.Count;
    public int PendingOperations => _jobs.Count - _cursor;

    // =========================================================
    public override void _Ready()
    { SetProcess(false); SetPhysicsProcess(false); }

    // =========================================================
    public void Initialize(PlayerController player, int worldSize, FoliageSettings settings,
        VegetationVisibilityPolicy visibility, StylizedGrassPlacement? grass,
        PinePlacement pines, ForestZonesPlacement forest, ContentScatter rocks)
    {
        _player = player; _worldSize = worldSize; _settings = settings; _visibility = visibility;
        _grass = grass; _pines = pines; _forest = forest; _rocks = rocks;
        _player.PositionChanged += OnMoved;
        _visibility.Changed += OnVisibilityChanged;
        if (_grass != null) _grass.ChunksInvalidated += OnGrassInvalidated;
        _pines.ChunksInvalidated += OnPinesInvalidated;
        _forest.ChunksInvalidated += OnForestInvalidated;
        _rocks.ChunksInvalidated += OnRocksInvalidated;
        _ready = true;
        OnVisibilityChanged();
    }

    // =========================================================
    private void OnMoved(Vector3 position)
    {
        if (!_ready) return;
        _position = position;
        int size = _settings.ChunkSize;
        var cell = new Vector2I(Mathf.FloorToInt(position.X / size), Mathf.FloorToInt(position.Z / size));
        if (cell == _cell) return;
        _cell = cell;
        Reconcile();
    }

    // =========================================================
    private void OnVisibilityChanged()
    {
        if (!_ready) return;
        _rocks?.SetManagedVisibility(_visibility.RenderDistance);
        _position = _player.GlobalPosition;
        _cell = new Vector2I(Mathf.FloorToInt(_position.X / _settings.ChunkSize),
            Mathf.FloorToInt(_position.Z / _settings.ChunkSize));
        Reconcile();
    }

    // =========================================================
    // Recompute only on a crossed 32m cell, an applied fog profile or a recipe invalidation.
    private void Reconcile()
    {
        using var perf = WorldPerformanceProbe.Measure("Foliage.Reconcile");
        _wanted.Clear();
        int size = _settings.ChunkSize;
        float limit = _visibility.RenderDistance + size * _settings.LoadChunkMargin;
        float limitSq = limit * limit;
        int radius = Mathf.CeilToInt(limit / size) + 1;
        for (int z = _cell.Y - radius; z <= _cell.Y + radius; z++)
            for (int x = _cell.X - radius; x <= _cell.X + radius; x++)
            {
                if (x < 0 || z < 0 || x * size >= _worldSize || z * size >= _worldSize) continue;
                var key = new Vector2I(x, z);
                if (DistanceSquared(key) > limitSq) continue;
                _wanted.Add(key);
                if (!_chunks.ContainsKey(key)) _chunks.Add(key, new ChunkState());
            }
        BuildJobs();
    }

    // =========================================================
    private float DistanceSquared(Vector2I key)
    {
        float dx = (key.X + 0.5f) * _settings.ChunkSize - _position.X;
        float dz = (key.Y + 0.5f) * _settings.ChunkSize - _position.Z;
        return dx * dx + dz * dz;
    }

    // =========================================================
    private void BuildJobs()
    {
        _jobs.Clear(); _cursor = 0; _remove.Clear();
        float release = _visibility.RenderDistance + _settings.ChunkSize * _settings.UnloadChunkMargin;
        float releaseSq = release * release;
        foreach (var pair in _chunks)
        {
            bool wanted = _wanted.Contains(pair.Key);
            float distance = DistanceSquared(pair.Key);
            if (!wanted && distance <= releaseSq) continue;
            for (int family = 0; family < 4; family++)
            {
                int bit = 1 << family;
                bool ready = (pair.Value.Ready & bit) != 0;
                if ((wanted && ready) || (!wanted && !ready)) continue;
                float priority = wanted ? distance + family * 0.01f : 1000000f - distance + family * 0.01f;
                _jobs.Add(new Job(pair.Key, family, !wanted, priority));
            }
            if (!wanted && pair.Value.Ready == 0) _remove.Add(pair.Key);
        }
        foreach (var key in _remove) _chunks.Remove(key);
        _jobs.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        SetProcess(_jobs.Count > 0);
    }

    // =========================================================
    // One family/chunk job at a time. The budget is soft until expensive individual
    // Grass.CreateChunk operations are split by a later measured optimization.
    public override void _Process(double delta)
    {
        if (!_ready || _cursor >= _jobs.Count) { SetProcess(false); return; }
        using var perf = WorldPerformanceProbe.Measure("Foliage.ProcessQueue");
        long began = Stopwatch.GetTimestamp();
        int limit = Mathf.Clamp(_settings.OperationsPerFrame, 1, 6);
        double budget = Mathf.Clamp(_settings.WorkBudgetMs, 1f, 12f);
        int processed = 0;
        while (_cursor < _jobs.Count && processed < limit)
        {
            Job job = _jobs[_cursor++];
            if (!_chunks.TryGetValue(job.Key, out var state)) continue;
            int mask = 1 << job.Family;
            if (job.Release)
            {
                if ((state.Ready & mask) != 0) Release(job.Family, job.Key);
                state.Ready &= ~mask;
                if (state.Ready == 0 && !_wanted.Contains(job.Key)) _chunks.Remove(job.Key);
            }
            else
            {
                if (!_wanted.Contains(job.Key)) continue;
                Build(job.Family, job.Key);
                state.Ready |= mask;
            }
            processed++;
            if ((Stopwatch.GetTimestamp() - began) * 1000.0 / Stopwatch.Frequency >= budget) break;
        }
        if (_cursor >= _jobs.Count) SetProcess(false);
    }

    private void Build(int family, Vector2I key)
    {
        switch (family)
        {
            case Grass: _grass?.BuildManagedChunk(key, _settings.ChunkSize); break;
            case Pines: _pines?.BuildManagedChunk(key); break;
            case Forest: _forest?.BuildManagedChunk(key); break;
            case Rocks: _rocks?.BuildManagedChunk(key); break;
        }
    }

    private void Release(int family, Vector2I key)
    {
        switch (family)
        {
            case Grass: _grass?.ReleaseManagedChunk(key); break;
            case Pines: _pines?.ReleaseManagedChunk(key); break;
            case Forest: _forest?.ReleaseManagedChunk(key); break;
            case Rocks: _rocks?.ReleaseManagedChunk(key); break;
        }
    }

    // =========================================================
    private void Invalidate(int family)
    {
        if (!_ready) return;
        int mask = ~(1 << family);
        foreach (var state in _chunks.Values) state.Ready &= mask;
        Reconcile();
    }
    private void OnGrassInvalidated() => Invalidate(Grass);
    private void OnPinesInvalidated() => Invalidate(Pines);
    private void OnForestInvalidated() => Invalidate(Forest);
    private void OnRocksInvalidated() => Invalidate(Rocks);

    // =========================================================
    public override void _ExitTree()
    {
        if (!_ready) return;
        _ready = false;
        _player.PositionChanged -= OnMoved;
        _visibility.Changed -= OnVisibilityChanged;
        if (_grass != null) _grass.ChunksInvalidated -= OnGrassInvalidated;
        _pines.ChunksInvalidated -= OnPinesInvalidated;
        _forest.ChunksInvalidated -= OnForestInvalidated;
        _rocks.ChunksInvalidated -= OnRocksInvalidated;
        SetProcess(false);
    }
}
