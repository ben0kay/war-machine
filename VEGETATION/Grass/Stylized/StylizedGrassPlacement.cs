// STG1: seeded patches of imported ForestGrass001 meshes, streamed in GPU MultiMesh chunks.
// Reuses the village's grass exclusion rectangles and Terrain3D height data; no particles or per-blade nodes.
using Godot;
using System.Collections.Generic;

public partial class StylizedGrassPlacement : Node3D
{
    public VegetationVisibilityPolicy Visibility { get; set; } = new(new VegetationVisibilitySettings());
    [Export] public StylizedGrassSettings Settings { get; set; } = null!;
    [Export] public EnvironmentConfig EnvironmentSettings { get; set; } = null!;
    public Godot.Collections.Array<Rect2> ExclusionAreas { get; } = new();
    public int ActiveChunks => _active.Count;
    public int ActiveClumps { get; private set; }

    private readonly Dictionary<Vector2I, GrassChunk> _active = new();
    private Mesh[] _near = System.Array.Empty<Mesh>(), _far = System.Array.Empty<Mesh>();
    private ShaderMaterial? _material;
    private GodotObject _terrain = null!;
    private Node3D _anchor = null!;
    private Vector2I _center = new(int.MinValue, int.MinValue);
    private int _worldSize;
    private float _focusDistance;
    private bool _initialized, _refreshQueued;

    private sealed class GrassChunk
    {
        public readonly List<MultiMeshInstance3D> Nodes = new();
        public int Clumps;
    }

    // =========================================================
    // Initialize once after roads, structures and Terrain3D are finalized.
    public void InitializeView(Node3D terrain, Node3D anchor, int worldSize)
    {
        _terrain = terrain.Get("data").AsGodotObject(); _anchor = anchor; _worldSize = worldSize;
        _terrain.Connect("height_maps_changed", Callable.From(QueueTerrainRefresh));
        Visibility.Changed += OnVisibilityChanged;
        _initialized = true; SetProcess(false); SetPhysicsProcess(false);
        ApplySettings();
    }

    // =========================================================
    // Rebuild on explicit setting changes, not every frame. Disabled means zero GPU batches.
    public void ApplySettings()
    {
        foreach (var chunk in _active.Values) ReleaseChunk(chunk);
        _active.Clear(); ActiveClumps = 0;
        _center = new Vector2I(int.MinValue, int.MinValue);
        _near = _far = System.Array.Empty<Mesh>(); _material = null;
        if (!_initialized || Settings == null || !Settings.Enabled || Settings.PatchesPerChunk <= 0) return;
        if (!LoadTemplates()) return;
        UpdateAnchor(_anchor.GlobalPosition);
    }

    // =========================================================
    // Import 3 variants * 2 authored LODs once and share all meshes/materials across all chunks.
    private bool LoadTemplates()
    {
        const string root = "res://ASSETS/Vegetation/Grass/ForestGrass001/";
        var original = GD.Load<ShaderMaterial>(root + "Materials/ForestGrass001Wind.tres");
        var shader = GD.Load<Shader>("res://VEGETATION/Grass/Stylized/Shaders/StylizedGrass.gdshader");
        if (original == null || shader == null)
        { GD.PushWarning("Stylized grass: missing shared material/shader; no grass spawned."); return false; }
        Variant atlas = original.GetShaderParameter("grass_atlas");
        _material = (ShaderMaterial)original.Duplicate();
        _material.Shader = shader;
        _material.SetShaderParameter("grass_atlas", atlas);
        _near = new Mesh[3]; _far = new Mesh[3];
        for (int variant = 0; variant < 3; variant++) for (int lod = 1; lod <= 2; lod++)
        {
            string letter = (char)('a' + variant) + "";
            string path = root + "Models/forest_grass_001_" + letter + "_LOD" + lod + ".gltf";
            var packed = GD.Load<PackedScene>(path);
            if (packed == null)
            { GD.PushWarning("Stylized grass missing " + path); return false; }
            Node rootNode = packed.Instantiate();
            try
            {
                MeshInstance3D? meshNode = rootNode as MeshInstance3D;
                if (meshNode == null)
                    foreach (Node child in rootNode.FindChildren("*", "MeshInstance3D", true, false))
                        if (child is MeshInstance3D instance) { meshNode = instance; break; }
                if (meshNode?.Mesh is not ArrayMesh source || source.GetSurfaceCount() != 1)
                { GD.PushWarning("Stylized grass invalid mesh/surface count in " + path); return false; }
                var mesh = (ArrayMesh)source.Duplicate();
                mesh.SurfaceSetMaterial(0, _material);
                if (lod == 1) _near[variant] = mesh; else _far[variant] = mesh;
            }
            finally { rootNode.Free(); }
        }
        ApplyWind();
        return true;
    }

    // =========================================================
    // Only on setup or explicit weather-setting changes; the GPU animates the shared foliage shader.
    public void ApplyWind()
    {
        if (_material == null || EnvironmentSettings == null) return;
        Vector2 dir = EnvironmentSettings.WindDirection;
        dir = dir.LengthSquared() > 0.0001f ? dir.Normalized() : Vector2.Right;
        _material.SetShaderParameter("wind_direction", dir);
        _material.SetShaderParameter("wind_strength", Mathf.Clamp(
            EnvironmentSettings.GrassWindStrength * Settings.WindMultiplier, 0f, 0.2f));
        _material.SetShaderParameter("wind_speed", Mathf.Max(0f, Settings.WindSpeed));
    }

    // =========================================================
    // Camera/zoom range changes are native renderer properties, not re-scattered grass.
    public void UpdateView(float viewHeight, float focusDistance, bool force = false)
    {
        float next = Mathf.Max(0f, focusDistance);
        if (!force && Mathf.IsEqualApprox(next, _focusDistance)) return;
        _focusDistance = next;
        float far = Visibility.RenderDistance + _focusDistance;
        float lod = Mathf.Clamp(Visibility.Lod0End + _focusDistance, 8f, far - 4f);
        foreach (var chunk in _active.Values)
            foreach (var instance in chunk.Nodes)
            {
                bool distant = instance.Name.ToString().EndsWith("_Far");
                instance.VisibilityRangeBegin = distant ? lod : 0f;
                instance.VisibilityRangeEnd = distant ? far : lod;
                instance.SetMeta(WorldRenderEnvelope.AuthoredEndMetadata, instance.VisibilityRangeEnd);
            }
    }

    // =========================================================
    // Move only on integer 32m chunk crossings. Existing chunks are reused without polling.
    public void UpdateAnchor(Vector3 position)
    {
        if (_near.Length != 3 || Settings == null || !Settings.Enabled) return;
        int width = Mathf.Clamp(Settings.ChunkSize, 16, 64);
        var next = new Vector2I(Mathf.FloorToInt(position.X / width),
            Mathf.FloorToInt(position.Z / width));
        if (next == _center) return;
        _center = next;
        float render = Visibility.RenderDistance + _focusDistance;
        int range = Mathf.CeilToInt((render + width * 1.5f) / width);
        var wanted = new HashSet<Vector2I>();
        Vector2 eye = new((next.X + 0.5f) * width, (next.Y + 0.5f) * width);
        for (int z = next.Y - range; z <= next.Y + range; z++) for (int x = next.X - range; x <= next.X + range; x++)
        {
            if (x < 0 || z < 0 || x * width >= _worldSize || z * width >= _worldSize) continue;
            var candidate = new Vector2((x + 0.5f) * width, (z + 0.5f) * width);
            if (eye.DistanceSquaredTo(candidate) <= (render + width * 1.5f) * (render + width * 1.5f))
                wanted.Add(new Vector2I(x, z));
        }
        var remove = new List<Vector2I>();
        foreach (var old in _active)
            if (!wanted.Contains(old.Key)) { ReleaseChunk(old.Value); ActiveClumps -= old.Value.Clumps; remove.Add(old.Key); }
        foreach (var key in remove) _active.Remove(key);
        foreach (var key in wanted) if (!_active.ContainsKey(key)) CreateChunk(key, width);
    }

    // =========================================================
    // Reapply the single fog-derived envelope and add/remove only chunks whose residency changed.
    private void OnVisibilityChanged()
    {
        if (!_initialized) return;
        UpdateView(0f, _focusDistance, true);
        _center = new Vector2I(int.MinValue, int.MinValue);
        UpdateAnchor(_anchor.GlobalPosition);
    }

    // =========================================================
    // Deterministic patch centres, then small irregular groups of original imported grass clumps.
    // Every individual root is terrain-tested and checked against roads/footprints with edge clearance.
    private void CreateChunk(Vector2I key, int width)
    {
        var rng = new RandomNumberGenerator { Seed = Mix(Settings.Seed, key.X, key.Y) };
        var bin = new List<Transform3D>[3];
        for (int i = 0; i < 3; i++) bin[i] = new List<Transform3D>();
        int patches = Mathf.Clamp(Settings.PatchesPerChunk, 0, 12);
        int members = Mathf.Clamp(Settings.ClumpsPerPatch, 1, 30);
        for (int patch = 0; patch < patches; patch++)
        {
            if (rng.Randf() > Mathf.Clamp(Settings.PatchChance, 0f, 1f)) continue;
            Vector2 center = new((key.X + rng.RandfRange(0.08f, 0.92f)) * width,
                (key.Y + rng.RandfRange(0.08f, 0.92f)) * width);
            float radius = rng.RandfRange(Mathf.Clamp(Settings.PatchRadiusMin, 0.6f, 4f),
                Mathf.Clamp(Mathf.Max(Settings.PatchRadiusMin, Settings.PatchRadiusMax), 0.6f, 6f));
            for (int j = 0; j < members; j++)
            {
                float theta = rng.RandfRange(0f, Mathf.Tau);
                float spread = Mathf.Sqrt(rng.Randf()) * radius;
                Vector2 point = center + new Vector2(Mathf.Cos(theta), Mathf.Sin(theta)) * spread;
                float scale = rng.RandfRange(Mathf.Clamp(Settings.ScaleMin, 0.3f, 2f),
                    Mathf.Clamp(Mathf.Max(Settings.ScaleMin, Settings.ScaleMax), 0.3f, 2.5f));
                if (point.X < 2 || point.Y < 2 || point.X >= _worldSize - 2 || point.Y >= _worldSize - 2
                    || IsExcluded(point, Mathf.Max(0.6f, Settings.EdgeClearance) * scale)) continue;
                if (!TryGround(point, out float height)) continue;
                int variant = rng.RandiRange(0, 2);
                bin[variant].Add(new Transform3D(
                    new Basis(Vector3.Up, rng.RandfRange(0f, Mathf.Tau)).Scaled(Vector3.One * scale),
                    new Vector3(point.X, height - 0.035f, point.Y)));
            }
        }

        var chunk = new GrassChunk();
        float end = Visibility.RenderDistance + _focusDistance;
        float lod = Mathf.Clamp(Visibility.Lod0End + _focusDistance, 8f, end - 4f);
        for (int i = 0; i < 3; i++)
        {
            if (bin[i].Count == 0) continue;
            Vector3 origin = new((key.X + 0.5f) * width, 0f, (key.Y + 0.5f) * width);
            var near = BuildBatch("StylizedGrass_" + key.X + "_" + key.Y + "_" + i + "_Near",
                _near[i], bin[i], origin, 0f, lod);
            var far = BuildBatch("StylizedGrass_" + key.X + "_" + key.Y + "_" + i + "_Far",
                _far[i], bin[i], origin, lod, end);
            chunk.Nodes.Add(near); chunk.Nodes.Add(far);
            chunk.Clumps += bin[i].Count;
        }
        _active.Add(key, chunk); ActiveClumps += chunk.Clumps;
    }

    // =========================================================
    // World-space road/structure masks are shared with old grass; allow for half-width of each model.
    private bool IsExcluded(Vector2 p, float margin)
    {
        foreach (Rect2 rectangle in ExclusionAreas)
            if (rectangle.Grow(margin).HasPoint(p)) return true;
        return false;
    }

    // =========================================================
    // Saved Terrain3D is authoritative. Steep/missing areas become naturally bare ground.
    private bool TryGround(Vector2 p, out float height)
    {
        height = _terrain.Call("get_height", new Vector3(p.X, 0f, p.Y)).AsSingle();
        float east = _terrain.Call("get_height", new Vector3(p.X + 1f, 0f, p.Y)).AsSingle();
        float south = _terrain.Call("get_height", new Vector3(p.X, 0f, p.Y + 1f)).AsSingle();
        if (!float.IsFinite(height) || !float.IsFinite(east) || !float.IsFinite(south)) return false;
        float normalY = 1f / Mathf.Sqrt(1f + (east - height) * (east - height)
            + (south - height) * (south - height));
        return normalY >= Mathf.Clamp(Settings.MinimumNormalY, 0.5f, 1f);
    }

    // =========================================================
    // One near and one far GPU batch for each present grass art variant, no individual scenes.
    private MultiMeshInstance3D BuildBatch(string name, Mesh mesh, List<Transform3D> instances,
        Vector3 anchor, float begin, float end)
    {
        var multi = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, Mesh = mesh };
        multi.InstanceCount = instances.Count;
        for (int i = 0; i < instances.Count; i++)
        {
            var t = instances[i]; t.Origin -= anchor;
            multi.SetInstanceTransform(i, t);
        }
        var node = new MultiMeshInstance3D {
            Name = name, Multimesh = multi, Position = anchor,
            VisibilityRangeBegin = begin, VisibilityRangeEnd = end,
            VisibilityRangeBeginMargin = begin > 0f ? 5f : 0f,
            VisibilityRangeEndMargin = 5f,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        };
        node.SetMeta(WorldRenderEnvelope.AuthoredEndMetadata, end);
        AddChild(node);
        return node;
    }

    // =========================================================
    private void ReleaseChunk(GrassChunk chunk)
    {
        foreach (var node in chunk.Nodes) { RemoveChild(node); node.QueueFree(); }
    }

    // =========================================================
    private void QueueTerrainRefresh()
    {
        if (_refreshQueued) return;
        _refreshQueued = true; CallDeferred(nameof(RefreshTerrain));
    }

    // =========================================================
    public void RefreshTerrain()
    {
        _refreshQueued = false;
        if (_initialized) ApplySettings();
    }

    // =========================================================
    private static ulong Mix(int seed, int x, int z)
    {
        unchecked
        {
            uint h = (uint)seed ^ ((uint)x * 0x9E3779B9u) ^ ((uint)z * 0x85EBCA6Bu);
            h ^= h >> 16; h *= 0x7FEB352Du; h ^= h >> 15; h *= 0x846CA68Bu; h ^= h >> 16;
            return h;
        }
    }

    // =========================================================
    public override void _ExitTree()
    {
        Visibility.Changed -= OnVisibilityChanged;
        if (_initialized && GodotObject.IsInstanceValid(_terrain))
            _terrain.Disconnect("height_maps_changed", Callable.From(QueueTerrainRefresh));
        _initialized = false;
    }
}
