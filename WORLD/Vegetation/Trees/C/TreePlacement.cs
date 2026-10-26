// Event-driven Terrain3D tree placement: seeded forest chunks, shared MultiMeshes, no per-tree nodes or idle polling.
using Godot;
using System.Collections.Generic;

public partial class TreePlacement : Node3D
{
    [Export] public TreeSettings Settings { get; set; } = null!;
    [Export] public EnvironmentConfig EnvironmentSettings { get; set; } = null!;

    private GodotObject _data = null!;
    private ExplorationController _explorer = null!;
    private WorldConfig _world = null!;
    private FastNoiseLite _forest = null!;
    private Mesh[] _meshes = System.Array.Empty<Mesh>(), _farMeshes = System.Array.Empty<Mesh>();
    private sealed class TreeChunk
    {
        public readonly List<MultiMeshInstance3D> Nodes = new();
        public int TreeCount;
    }
    private readonly Dictionary<Vector2I, TreeChunk> _active = new();
    private Vector2I _center = new(int.MinValue, int.MinValue);
    private int _chunkSize, _chunkCount;
    private bool _initialized, _refreshQueued;
    public int ActiveChunks => _active.Count;
    public int ActiveTrees { get; private set; }

    // =========================================================
    // Use the already-loaded, saved Terrain3D region and the same movement signal as the grass system.
    public void Initialize(Node3D terrain, ExplorationController explorer, WorldConfig world)
    {
        if (_initialized) DisconnectSignals();
        _data = terrain.Get("data").AsGodotObject(); _explorer = explorer; _world = world;
        _explorer.ViewPositionChanged += OnViewMoved;
        _data.Connect("height_maps_changed", Callable.From(QueueTerrainRefresh));
        _initialized = true;
        SetProcess(false); SetPhysicsProcess(false);
        ApplySettings();
    }

    // =========================================================
    // Rebuild only when explicitly requested or the terrain height map changes; OFF frees all instances.
    public void ApplySettings()
    {
        foreach (var chunk in _active.Values)
            foreach (var instance in chunk.Nodes) { RemoveChild(instance); instance.QueueFree(); }
        _active.Clear(); _center = new Vector2I(int.MinValue, int.MinValue); ActiveTrees = 0;
        _meshes = _farMeshes = System.Array.Empty<Mesh>();
        if (!_initialized || Settings == null || !Settings.Enabled || Settings.Density <= 0) return;

        _chunkSize = Mathf.Clamp(Settings.ChunkSize, 32, 192);
        _chunkCount = Mathf.CeilToInt((float)_world.Size / _chunkSize);
        _forest = new FastNoiseLite { Seed = Settings.Seed + 17, Frequency = Mathf.Max(0.0001f, Settings.ForestNoiseFrequency) };
        var imported = new List<Mesh>();
        foreach (Mesh candidate in Settings.ImportedMeshes)
            if (candidate != null) imported.Add(candidate);
        if (imported.Count > 0) _meshes = imported.ToArray();
        else
        {
            var bark = new StandardMaterial3D { VertexColorUseAsAlbedo = true, Roughness = 1f };
            var foliage = new ShaderMaterial { Shader = GD.Load<Shader>("res://WORLD/Vegetation/Trees/Shaders/LeafWind.gdshader") };
            Vector2 wind = EnvironmentSettings.WindDirection;
            wind = wind.LengthSquared() > 0.0001f ? wind.Normalized() : Vector2.Right;
            foliage.SetShaderParameter("wind_direction", wind);
            foliage.SetShaderParameter("wind_strength", Mathf.Max(0f, Settings.LeafWindStrength)
                * Mathf.Clamp(EnvironmentSettings.GrassWindStrength, 0f, 2f));
            foliage.SetShaderParameter("wind_speed", Mathf.Max(0f, Settings.LeafWindSpeed));
            foliage.SetShaderParameter("flutter_strength", Mathf.Max(0f, Settings.LeafFlutter));
            int count = Mathf.Clamp(Settings.Variants, 1, 12);
            _meshes = new Mesh[count];
            if (Settings.UseGeneratedLod) _farMeshes = new Mesh[count];
            for (int i = 0; i < count; i++)
            {
                _meshes[i] = TreeGenerator.CreateOak(Settings, i, bark, foliage);
                if (_farMeshes.Length > 0)
                    _farMeshes[i] = TreeGenerator.CreateOak(Settings, i, bark, foliage, Mathf.Clamp(Settings.FarLeafStride, 2, 8));
            }
        }
        OnViewMoved(_explorer.GlobalPosition);
    }

    // =========================================================
    // Crossing a chunk boundary loads/evicts only nearby tree MultiMeshes; standing still does nothing.
    private void OnViewMoved(Vector3 position)
    {
        if (_meshes.Length == 0) return;
        var next = new Vector2I(Mathf.FloorToInt(position.X / _chunkSize), Mathf.FloorToInt(position.Z / _chunkSize));
        if (next == _center) return;
        _center = next;
        float distance = Mathf.Clamp(Settings.RenderDistance, 64f, 512f);
        int radius = Mathf.CeilToInt((distance + _chunkSize * 1.5f) / _chunkSize);
        var wanted = new HashSet<Vector2I>();
        var chunkCenter = new Vector2((next.X + 0.5f) * _chunkSize, (next.Y + 0.5f) * _chunkSize);
        for (int x = next.X - radius; x <= next.X + radius; x++)
            for (int z = next.Y - radius; z <= next.Y + radius; z++)
            {
                if (x < 0 || z < 0 || x >= _chunkCount || z >= _chunkCount) continue;
                var key = new Vector2I(x, z);
                if (chunkCenter.DistanceTo(new Vector2((x + 0.5f) * _chunkSize, (z + 0.5f) * _chunkSize))
                    <= distance + _chunkSize * 1.5f) wanted.Add(key);
            }
        var remove = new List<Vector2I>();
        // Retain one extra cache ring so nudging across a boundary does not rebuild the same trees.
        foreach (var key in _active.Keys)
            if (!wanted.Contains(key) && chunkCenter.DistanceTo(new Vector2((key.X + 0.5f) * _chunkSize, (key.Y + 0.5f) * _chunkSize))
                > distance + _chunkSize * 2.5f) remove.Add(key);
        foreach (var key in remove)
        {
            foreach (var instance in _active[key].Nodes)
            {
                RemoveChild(instance); instance.QueueFree();
            }
            ActiveTrees -= _active[key].TreeCount;
            _active.Remove(key);
        }
        foreach (var key in wanted)
            if (!_active.ContainsKey(key)) CreateChunk(key);
    }

    // =========================================================
    // Sample Terrain3D heights and local slopes once per candidate, with forest noise and a spawn clearing.
    private void CreateChunk(Vector2I key)
    {
        var bins = new List<Transform3D>[_meshes.Length];
        for (int i = 0; i < bins.Length; i++) bins[i] = new List<Transform3D>();
        var random = new RandomNumberGenerator { Seed = Mix(Settings.Seed, key.X, key.Y) };
        float spacing = Mathf.Clamp(Settings.Spacing, 8f, 40f);
        int cells = Mathf.Max(1, Mathf.CeilToInt(_chunkSize / spacing));
        float step = (float)_chunkSize / cells;
        float clearing = Mathf.Max(0, Settings.SpawnClearingRadius);
        float minScale = Mathf.Max(0.1f, Mathf.Min(Settings.MinimumScale, Settings.MaximumScale));
        float maxScale = Mathf.Max(minScale, Mathf.Max(Settings.MinimumScale, Settings.MaximumScale));
        for (int a = 0; a < cells; a++)
            for (int b = 0; b < cells; b++)
            {
                if (random.Randf() > Mathf.Clamp(Settings.Density, 0, 1)) continue;
                float x = key.X * _chunkSize + (a + 0.5f + random.RandfRange(-0.35f, 0.35f)) * step;
                float z = key.Y * _chunkSize + (b + 0.5f + random.RandfRange(-0.35f, 0.35f)) * step;
                if (x < 3 || z < 3 || x >= _world.Size - 3 || z >= _world.Size - 3) continue;
                if (new Vector2(x, z).DistanceSquaredTo(_world.SpawnXZ) < clearing * clearing) continue;
                if (_forest.GetNoise2D(x, z) < Settings.ForestThreshold) continue;
                float height = SampleHeight(x, z);
                if (!float.IsFinite(height)) continue;
                float dx = SampleHeight(x + 2, z), dz = SampleHeight(x, z + 2);
                if (!float.IsFinite(dx) || !float.IsFinite(dz)) continue;
                float slope = new Vector2(dx - height, dz - height).Length() / 2f;
                if (slope > Settings.MaximumSlope) continue;
                float scale = random.RandfRange(minScale, maxScale);
                int variant = random.RandiRange(0, _meshes.Length - 1);
                var basis = new Basis(Vector3.Up, random.RandfRange(0, Mathf.Tau)).Scaled(Vector3.One * scale);
                bins[variant].Add(new Transform3D(basis, new Vector3(x, height - 0.1f, z)));
            }

        var chunk = new TreeChunk();
        for (int i = 0; i < bins.Length; i++)
        {
            if (bins[i].Count == 0) continue;
            float distance = Mathf.Clamp(Settings.RenderDistance, 64, 512);
            float margin = Mathf.Min(distance * 0.25f, Mathf.Clamp(Settings.DistanceFadeMargin, 4, 48));
            float lod = Mathf.Clamp(Settings.LodDistance, 32, distance - margin);
            bool hasLod = _farMeshes.Length == _meshes.Length;
            float averageHeight = 0;
            foreach (var transform in bins[i]) averageHeight += transform.Origin.Y;
            var anchor = new Vector3((key.X + 0.5f) * _chunkSize, averageHeight / bins[i].Count, (key.Y + 0.5f) * _chunkSize);
            chunk.Nodes.Add(CreateBatch($"Oak_{key.X}_{key.Y}_{i}_Near", _meshes[i], bins[i], anchor, 0, hasLod ? lod : distance, margin, Settings.CastShadows));
            if (hasLod)
                chunk.Nodes.Add(CreateBatch($"Oak_{key.X}_{key.Y}_{i}_Far", _farMeshes[i], bins[i], anchor, lod, distance, margin,
                    Settings.CastShadows && Settings.FarCastShadows));
            chunk.TreeCount += bins[i].Count;
        }
        ActiveTrees += chunk.TreeCount;
        _active.Add(key, chunk);
    }

    // =========================================================
    // Native Forward+ range fades/LOD and culling need no C# frame callbacks; roots/transforms are shared by both levels.
    private MultiMeshInstance3D CreateBatch(string name, Mesh mesh, List<Transform3D> transforms, Vector3 anchor, float begin, float end, float margin, bool shadows)
    {
        var multi = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, Mesh = mesh };
        multi.InstanceCount = transforms.Count;
        for (int j = 0; j < transforms.Count; j++)
        {
            var transform = transforms[j]; transform.Origin -= anchor;
            multi.SetInstanceTransform(j, transform);
        }
        var node = new MultiMeshInstance3D
        {
            Name = name, Position = anchor, Multimesh = multi, ExtraCullMargin = 1f,
            VisibilityRangeBegin = begin, VisibilityRangeEnd = end,
            VisibilityRangeBeginMargin = begin > 0 ? margin : 0, VisibilityRangeEndMargin = margin,
            VisibilityRangeFadeMode = GeometryInstance3D.VisibilityRangeFadeModeEnum.Self,
            CastShadow = shadows ? GeometryInstance3D.ShadowCastingSetting.On : GeometryInstance3D.ShadowCastingSetting.Off
        };
        AddChild(node);
        return node;
    }

    // =========================================================
    // Terrain3D's saved height-map API is authoritative for tree roots; do not generate terrain here.
    private float SampleHeight(float x, float z)
        => (float)_data.Call("get_height", new Vector3(x, 0, z)).AsDouble();

    // =========================================================
    // Coalesce editor height changes, then resample roots without touching saved terrain.
    private void QueueTerrainRefresh()
    {
        if (_refreshQueued) return;
        _refreshQueued = true; CallDeferred(nameof(RefreshTerrain));
    }

    // =========================================================
    // Process the queued refresh once, never every frame.
    public void RefreshTerrain()
    {
        _refreshQueued = false;
        if (_initialized) ApplySettings();
    }

    // =========================================================
    // Stable chunk-local seed means tree layout survives camera movement and reloads.
    private static ulong Mix(int seed, int x, int z)
    {
        unchecked
        {
            uint hash = (uint)seed ^ ((uint)x * 0x9E3779B9u) ^ ((uint)z * 0x85EBCA6Bu);
            hash ^= hash >> 16; hash *= 0x7FEB352Du; hash ^= hash >> 15; hash *= 0x846CA68Bu; hash ^= hash >> 16;
            return hash;
        }
    }

    // =========================================================
    // Clean up movement/height callbacks on world unload; Godot frees the active GPU instances.
    private void DisconnectSignals()
    {
        if (IsInstanceValid(_explorer)) _explorer.ViewPositionChanged -= OnViewMoved;
        if (IsInstanceValid(_data)) _data.Disconnect("height_maps_changed", Callable.From(QueueTerrainRefresh));
    }

    // =========================================================
    // No lingering signal subscriptions when testworld is closed.
    public override void _ExitTree()
    {
        if (_initialized) DisconnectSignals();
        _initialized = false;
    }
}
