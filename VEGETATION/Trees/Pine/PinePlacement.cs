// Event-driven Terrain3D tree placement: seeded forest chunks, shared MultiMeshes, no per-tree nodes or idle polling.
using Godot;
using System.Collections.Generic;

public partial class PinePlacement : Node3D, ICameraOcclusionProvider
{
    // One shared visibility owner for every active vegetation family. No per-pine distances.
    public VegetationVisibilityPolicy Visibility { get; set; } = new(new VegetationVisibilitySettings());
    public const uint TrunkLayer = 1u << 4; // Physics layer 5: small actors/weapon rays; giant robot ignores it.
    public Godot.Collections.Array<Rect2> ExclusionAreas { get; } = new();
    public event System.Action? OcclusionChanged;
    [Export] public PineSettings Settings { get; set; } = null!;
    [Export] public EnvironmentConfig EnvironmentSettings { get; set; } = null!;

    private ShaderMaterial? _foliage, _stylizedFoliage;
    private ShaderMaterial[] _glbFoliage = System.Array.Empty<ShaderMaterial>();
    private bool _usingPineGlb, _usingStylizedPine;
    private GodotObject _data = null!;
    private ExplorationController _explorer = null!;
    private Node3D _anchor = null!;
    private int _worldSize;
    private Vector2 _spawn;
    private float _focusDistance;
    private FastNoiseLite _forest = null!;
    private Mesh[] _meshes = System.Array.Empty<Mesh>(), _farMeshes = System.Array.Empty<Mesh>(),
        _thirdMeshes = System.Array.Empty<Mesh>();
    private sealed class TreeChunk
    {
        public readonly List<MultiMeshInstance3D> Nodes = new();
        public int TreeCount;
        public StaticBody3D? Trunks;
        public readonly List<PineFadeTarget> Occluders = new();
        public Aabb Bounds;
        public bool HasBounds;
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
        _explorer = explorer;
        _explorer.ViewPositionChanged += UpdateAnchor;
        InitializeView(terrain, explorer, world.Size, world.SpawnXZ);
    }

    // =========================================================
    // Active-world placement depends on terrain and an explicit anchor, not the legacy explorer/world config.
    public void InitializeView(Node3D terrain, Node3D anchor, int worldSize, Vector2 spawn)
    {
        _data = terrain.Get("data").AsGodotObject(); _anchor = anchor; _worldSize = worldSize; _spawn = spawn;
        _data.Connect("height_maps_changed", Callable.From(QueueTerrainRefresh));
        Visibility.Changed += OnVisibilityChanged;
        _initialized = true;
        SetProcess(false); SetPhysicsProcess(false);
        ApplySettings();
    }

    // =========================================================
    // Rebuild only when explicitly requested or the terrain height map changes; OFF frees all instances.
    public void ApplySettings()
    {
        foreach (var chunk in _active.Values) ReleaseChunk(chunk);
        OcclusionChanged?.Invoke();
        _active.Clear(); _center = new Vector2I(int.MinValue, int.MinValue); ActiveTrees = 0;
        _meshes = _farMeshes = _thirdMeshes = System.Array.Empty<Mesh>();
        _foliage = _stylizedFoliage = null;
        _glbFoliage = System.Array.Empty<ShaderMaterial>();
        _usingPineGlb = _usingStylizedPine = false;
        if (!_initialized || Settings == null || !Settings.Enabled || Settings.Density <= 0) return;

        _chunkSize = Mathf.Clamp(Settings.ChunkSize, 32, 192);
        _chunkCount = Mathf.CeilToInt((float)_worldSize / _chunkSize);
        _forest?.Dispose();
        _forest = new FastNoiseLite { Seed = Settings.Seed + 17, Frequency = Mathf.Max(0.0001f, Settings.ForestNoiseFrequency) };
        // The GLB is read once when changing modes/building the forest, never once per tree.
        // The seed/terrain/exclusion/streaming path below stays identical for both sources.
        bool stylized = Settings.UseStylizedPine && LoadStylizedPineMeshes();
        Mesh? glbMesh = !stylized && Settings.UsePineGlb ? LoadPineGlbMesh() : null;
        var imported = new List<Mesh>();
        foreach (Mesh candidate in Settings.ImportedMeshes)
            if (candidate != null) imported.Add(candidate);
        if (stylized) { /* Three stylized LODs were loaded as shared mesh templates. */ }
        else if (glbMesh != null) { _meshes = new Mesh[] { glbMesh }; _usingPineGlb = true; }
        else if (imported.Count > 0) _meshes = imported.ToArray();
        else
        {
            var bark = new StandardMaterial3D { VertexColorUseAsAlbedo = true, Roughness = 1f };
            var foliage = new ShaderMaterial { Shader = GD.Load<Shader>("res://VEGETATION/Trees/Pine/Shaders/LeafWind.gdshader") };
            _foliage = foliage;
            Vector2 wind = EnvironmentSettings.WindDirection;
            wind = wind.LengthSquared() > 0.0001f ? wind.Normalized() : Vector2.Right;
            foliage.SetShaderParameter("wind_direction", wind);
            foliage.SetShaderParameter("use_camera_occlusion", true);
            foliage.SetShaderParameter("wind_strength", Mathf.Max(0f, Settings.LeafWindStrength)
                * Mathf.Clamp(EnvironmentSettings.GrassWindStrength, 0f, 2f));
            foliage.SetShaderParameter("wind_speed", Mathf.Max(0f, Settings.LeafWindSpeed));
            foliage.SetShaderParameter("flutter_strength", Mathf.Max(0f, Settings.LeafFlutter));
            int count = Mathf.Clamp(Settings.Variants, 1, 12);
            _meshes = new Mesh[count];
            if (Settings.UseGeneratedLod) _farMeshes = new Mesh[count];
            for (int i = 0; i < count; i++)
            {
                _meshes[i] = PineMeshBuilder.CreatePine(Settings, i, bark, foliage);
                if (_farMeshes.Length > 0)
                    _farMeshes[i] = PineMeshBuilder.CreatePine(Settings, i, bark, foliage, Mathf.Clamp(Settings.FarLeafStride, 2, 8));
            }
        }
        ApplyWind();
        UpdateAnchor(_anchor.GlobalPosition);
    }

    // =========================================================
    // Extract the three authored stylized glTF LODs once per forest rebuild, not once per tree.
    // Retain the original vertex colours and material surface order; no preview assets are modified.
    private bool LoadStylizedPineMeshes()
    {
        const string root = "res://ASSETS/Vegetation/Trees/PineTree001/";
        var bark = GD.Load<Material>(root + "Materials/PineTree001Bark.tres");
        var palette = GD.Load<ShaderMaterial>(root + "Materials/PineTree001Foliage.tres");
        var shader = GD.Load<Shader>("res://VEGETATION/Trees/Pine/Shaders/StylizedPineFoliage.gdshader");
        if (bark == null || palette == null || shader == null)
        { GD.PushWarning("Stylized pine materials unavailable; falling back to original pines."); return false; }

        var foliage = new ShaderMaterial { Shader = shader };
        foreach (string parameter in new[] { "palette_response", "foliage_roughness" })
            foliage.SetShaderParameter(parameter, palette.GetShaderParameter(parameter));
        foliage.SetShaderParameter("use_camera_occlusion", true);

        var lods = new Mesh[3];
        for (int lod = 1; lod <= 3; lod++)
        {
            string path = root + $"Models/pine_tree_001_a_LOD{lod}.gltf";
            var scene = GD.Load<PackedScene>(path);
            if (scene == null)
            { GD.PushWarning($"Stylized pine missing {path}; falling back to original pines."); return false; }
            Node instance = scene.Instantiate();
            try
            {
                MeshInstance3D? visual = instance as MeshInstance3D;
                if (visual == null)
                    foreach (Node child in instance.FindChildren("*", "MeshInstance3D", true, false))
                        if (child is MeshInstance3D mesh) { visual = mesh; break; }
                if (visual?.Mesh is not ArrayMesh source || source.GetSurfaceCount() != 2)
                {
                    GD.PushWarning($"Stylized pine LOD{lod} must contain one mesh with two surfaces (bark, foliage); reverting to original pines.");
                    return false;
                }
                var copy = (ArrayMesh)source.Duplicate();
                copy.SurfaceSetMaterial(0, bark);
                copy.SurfaceSetMaterial(1, foliage);
                lods[lod - 1] = copy;
            }
            finally { instance.Free(); }
        }
        _meshes = new[] { lods[0] }; _farMeshes = new[] { lods[1] }; _thirdMeshes = new[] { lods[2] };
        _stylizedFoliage = foliage; _usingStylizedPine = true;
        return true;
    }

    // =========================================================
    // Extract one combined mesh from PineTree001.glb, then discard its temporary scene tree.
    // Duplicate the ArrayMesh once before overriding foliage materials: F1/catalogue art is untouched.
    // The three foliage surfaces retain their original PBR colours, but now support camera fade and wind.
    private Mesh? LoadPineGlbMesh()
    {
        if (Settings.PineGlbScene == null)
        { GD.PushWarning("Pine forest GLB enabled but PineGlbScene is missing; using legacy pine source."); return null; }
        Node root = Settings.PineGlbScene.Instantiate();
        try
        {
            MeshInstance3D? visual = root as MeshInstance3D;
            if (visual == null)
                foreach (Node child in root.FindChildren("*", "MeshInstance3D", true, false))
                    if (child is MeshInstance3D mesh) { visual = mesh; break; }
            if (visual?.Mesh is not ArrayMesh source || source.GetSurfaceCount() != 5)
            {
                GD.PushWarning("PineTree001 GLB needs one ArrayMesh with 5 material surfaces; using legacy pine source.");
                return null;
            }

            var copy = (ArrayMesh)source.Duplicate();
            var shader = GD.Load<Shader>("res://VEGETATION/Trees/Pine/Shaders/ImportedPineFoliage.gdshader");
            var foliage = new ShaderMaterial[3];
            for (int i = 2; i < 5; i++)
            {
                Material? original = visual.GetActiveMaterial(i) ?? source.SurfaceGetMaterial(i);
                Color tint = original is BaseMaterial3D pbr ? pbr.AlbedoColor : new Color(0.10f, 0.24f, 0.13f);
                var material = new ShaderMaterial { Shader = shader };
                material.SetShaderParameter("foliage_tint", tint);
                material.SetShaderParameter("use_camera_occlusion", true);
                copy.SurfaceSetMaterial(i, material);
                foliage[i - 2] = material;
            }
            _glbFoliage = foliage;
            return copy;
        }
        finally { root.Free(); }
    }

    // =========================================================
    // Update the shared GPU material directly; wind sliders never regenerate geometry.
    public void ApplyWind()
    {
        if (_foliage == null && _stylizedFoliage == null && _glbFoliage.Length == 0) return;
        Vector2 direction = EnvironmentSettings.WindDirection.Normalized();
        if (direction.LengthSquared() < 0.0001f) direction = Vector2.Right;
        float strength = Settings.LeafWindStrength * EnvironmentSettings.GrassWindStrength;
        float speed = Settings.LeafWindSpeed * EnvironmentSettings.GrassWindSpeed / 0.025f;
        if (_foliage != null)
        {
            _foliage.SetShaderParameter("wind_direction", direction);
            _foliage.SetShaderParameter("wind_strength", strength);
            _foliage.SetShaderParameter("wind_speed", speed);
        }
        if (_stylizedFoliage != null)
        {
            _stylizedFoliage.SetShaderParameter("wind_direction", direction);
            _stylizedFoliage.SetShaderParameter("wind_strength", strength);
            _stylizedFoliage.SetShaderParameter("wind_speed", speed);
        }
        foreach (ShaderMaterial material in _glbFoliage)
        {
            material.SetShaderParameter("wind_direction", direction);
            material.SetShaderParameter("wind_strength", strength);
            material.SetShaderParameter("wind_speed", speed);
        }
    }

    // =========================================================
    // Every pine source (stylized, generated, imported GLB) shares the same fog-derived
    // global LOD boundaries. Sources with fewer meshes simply skip unsupported levels.
    private void GetDrawRanges(out float nearEnd, out float midEnd, out float end, out float margin)
    {
        end = Mathf.Max(24f, Visibility.RenderDistance + _focusDistance);
        margin = Visibility.TransitionMargin;
        nearEnd = Mathf.Clamp(Visibility.Lod0End + _focusDistance, 8f, end - 12f);
        midEnd = Mathf.Clamp(Visibility.Lod1End + _focusDistance, nearEnd + 4f, end - 4f);
    }

    // =========================================================
    // Native distance ranges include the elevated camera offset; zoom edits update batches, not geometry.
    public void UpdateView(float focusDistance, bool force = false)
    {
        if (!force && Mathf.IsEqualApprox(_focusDistance, focusDistance)) return;
        _focusDistance = Mathf.Max(0, focusDistance);
        GetDrawRanges(out float nearCut, out float midCut, out float end, out _);
        bool triple = _thirdMeshes.Length == _meshes.Length;
        bool doubleLod = _farMeshes.Length == _meshes.Length;
        foreach (var chunk in _active.Values)
            foreach (var node in chunk.Nodes)
            {
                bool far = node.Name.ToString().EndsWith("_Far");
                bool mid = node.Name.ToString().EndsWith("_Mid");
                node.VisibilityRangeBegin = triple ? (far ? midCut : mid ? nearCut : 0f) : (far ? nearCut : 0f);
                node.VisibilityRangeEnd = triple ? (far ? end : mid ? midCut : nearCut)
                    : far || !doubleLod ? end : nearCut;
                node.SetMeta(WorldRenderEnvelope.AuthoredEndMetadata, node.VisibilityRangeEnd);
            }
    }

    // =========================================================
    // Crossing a chunk boundary loads/evicts only nearby tree MultiMeshes; standing still does nothing.
    public void UpdateAnchor(Vector3 position)
    {
        if (_meshes.Length == 0) return;
        var next = new Vector2I(Mathf.FloorToInt(position.X / _chunkSize), Mathf.FloorToInt(position.Z / _chunkSize));
        if (next == _center) return;
        _center = next;
        float distance = Visibility.RenderDistance + _focusDistance;
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
            ReleaseChunk(_active[key]);
            ActiveTrees -= _active[key].TreeCount;
            _active.Remove(key);
        }
        foreach (var key in wanted)
            if (!_active.ContainsKey(key)) CreateChunk(key);
        OcclusionChanged?.Invoke();
    }

    // =========================================================
    // Fog distance changes update active native ranges, then reconcile streamed chunk residency.
    // Geometry generation is never repeated for chunks that remain in the active dictionary.
    private void OnVisibilityChanged()
    {
        if (!_initialized) return;
        UpdateView(_focusDistance, true);
        _center = new Vector2I(int.MinValue, int.MinValue);
        UpdateAnchor(_anchor.GlobalPosition);
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
                if (x < 3 || z < 3 || x >= _worldSize - 3 || z >= _worldSize - 3) continue;
                bool excluded = false;
                foreach (var area in ExclusionAreas) if (area.HasPoint(new Vector2(x, z))) { excluded = true; break; }
                if (excluded) continue;
                if (new Vector2(x, z).DistanceSquaredTo(_spawn) < clearing * clearing) continue;
                if (_forest.GetNoise2D(x, z) < Settings.ForestThreshold) continue;
                float height = SampleHeight(x, z);
                if (!float.IsFinite(height)) continue;
                float dx = SampleHeight(x + 2, z), dz = SampleHeight(x, z + 2);
                if (!float.IsFinite(dx) || !float.IsFinite(dz)) continue;
                float slope = new Vector2(dx - height, dz - height).Length() / 2f;
                if (slope > Settings.MaximumSlope) continue;
                float scale = random.RandfRange(minScale, maxScale);
                // Consume the same variant roll as the original 3-variant forest. This keeps
                // positions, rotations and size RNG identical when switching GLB/mesh art.
                int variantRoll = random.RandiRange(0, _usingPineGlb || _usingStylizedPine
                    ? Mathf.Clamp(Settings.Variants, 1, 12) - 1 : _meshes.Length - 1);
                int variant = _usingPineGlb || _usingStylizedPine ? 0 : variantRoll;
                float modelScale = _usingStylizedPine
                    ? Mathf.Clamp(Settings.StylizedBaseScale, 0.25f, 2f)
                    : _usingPineGlb ? Mathf.Max(0.1f, Settings.PineGlbBaseScale * Settings.HeightMultiplier) : 1f;
                var basis = new Basis(Vector3.Up, random.RandfRange(0, Mathf.Tau))
                    .Scaled(Vector3.One * scale * modelScale);
                bins[variant].Add(new Transform3D(basis, new Vector3(x, height - 0.1f, z)));
            }

        var chunk = new TreeChunk();
        for (int i = 0; i < bins.Length; i++)
        {
            if (bins[i].Count == 0) continue;
            if (Settings.TrunkCollisionEnabled && chunk.Trunks == null)
            { chunk.Trunks = new StaticBody3D { Name = $"Trunks_{key.X}_{key.Y}", CollisionLayer = TrunkLayer, CollisionMask = 0 }; AddChild(chunk.Trunks); }
            GetDrawRanges(out float nearCut, out float midCut, out float distance, out float margin);
            bool hasLod = _farMeshes.Length == _meshes.Length;
            bool hasThird = _thirdMeshes.Length == _meshes.Length;
            float averageHeight = 0;
            foreach (var transform in bins[i]) averageHeight += transform.Origin.Y;
            var anchor = new Vector3((key.X + 0.5f) * _chunkSize, averageHeight / bins[i].Count, (key.Y + 0.5f) * _chunkSize);
            // The imported three-level pine now fades between LODs instead of snapping.
            // Keep culling and range centres identical for all three mesh variants.
            bool fade = _usingStylizedPine && hasLod && hasThird;
            Aabb sharedBounds = fade ? BuildLodBounds(i, bins[i], anchor) : default;
            var near = CreateBatch($"Pine_{key.X}_{key.Y}_{i}_Near", _meshes[i], bins[i], anchor,
                0, hasLod ? nearCut : distance, margin, Settings.CastShadows, fade, sharedBounds);
            chunk.Nodes.Add(near);
            MultiMeshInstance3D? far = null, third = null;
            if (hasLod)
            {
                far = CreateBatch($"Pine_{key.X}_{key.Y}_{i}_{(hasThird ? "Mid" : "Far")}", _farMeshes[i], bins[i],
                    anchor, nearCut, hasThird ? midCut : distance, margin,
                    Settings.CastShadows && Settings.FarCastShadows, fade, sharedBounds);
                chunk.Nodes.Add(far);
            }
            if (hasThird)
            {
                third = CreateBatch($"Pine_{key.X}_{key.Y}_{i}_Far", _thirdMeshes[i], bins[i],
                    anchor, midCut, distance, margin, false, fade, sharedBounds);
                chunk.Nodes.Add(third);
            }
            // Both generated foliage and our imported GLB adapter share per-instance canopy fade data.
            // Generic ImportedMeshes without the adapter retain their existing non-fading behaviour.
            if (_foliage != null || _stylizedFoliage != null || _glbFoliage.Length > 0)
            {
                Aabb localCanopy = _meshes[i].GetAabb();
                if (hasLod) localCanopy = localCanopy.Merge(_farMeshes[i].GetAabb());
                if (hasThird) localCanopy = localCanopy.Merge(_thirdMeshes[i].GetAabb());
                localCanopy.Position += Vector3.Up * localCanopy.Size.Y * 0.18f;
                localCanopy.Size = new Vector3(localCanopy.Size.X, localCanopy.Size.Y * 0.82f, localCanopy.Size.Z);
                for (int j = 0; j < bins[i].Count; j++)
                {
                    Aabb bounds = CameraOcclusionMath.TransformBounds(localCanopy, GlobalTransform * bins[i][j]);
                    chunk.Occluders.Add(new PineFadeTarget(near.Multimesh, far?.Multimesh, third?.Multimesh, j, bounds));
                    chunk.Bounds = chunk.HasBounds ? chunk.Bounds.Merge(bounds) : bounds; chunk.HasBounds = true;
                }
            }
            if (chunk.Trunks != null)
                foreach (var transform in bins[i])
                {
                    float scale = transform.Basis.X.Length();
                    float radius = (_usingStylizedPine ? Mathf.Clamp(Settings.StylizedTrunkRadius, 0.1f, 1.5f)
                        : _usingPineGlb ? Mathf.Clamp(Settings.PineGlbTrunkRadius, 0.1f, 1.5f)
                        : PineMeshBuilder.GetTrunkRadius(Settings, i)) * scale;
                    float height = Mathf.Max(radius * 2, Settings.TrunkCollisionHeight * scale);
                    chunk.Trunks.AddChild(new CollisionShape3D { Position = transform.Origin + Vector3.Up * (height * 0.5f),
                        Shape = new CapsuleShape3D { Radius = radius, Height = height } });
                }
            chunk.TreeCount += bins[i].Count;
        }
        ActiveTrees += chunk.TreeCount;
        _active.Add(key, chunk);
    }

    // =========================================================
    // One body per active chunk, one simple shape per trunk; free collision alongside its visual batch on OFF/eviction.
    private void ReleaseChunk(TreeChunk chunk)
    {
        foreach (var target in chunk.Occluders) target.Alive = false;
        foreach (var instance in chunk.Nodes) { RemoveChild(instance); instance.QueueFree(); }
        if (GodotObject.IsInstanceValid(chunk.Trunks))
        { chunk.Trunks!.CollisionLayer = 0; RemoveChild(chunk.Trunks); chunk.Trunks.QueueFree(); }
    }

    // =========================================================
    // =========================================================
    // Compute matching near/mid/far bounds only when a streamed chunk is generated.
    // Different authored LOD extents no longer move the native distance/culling centre.
    private Aabb BuildLodBounds(int variant, List<Transform3D> transforms, Vector3 anchor)
    {
        var meshBounds = _meshes[variant].GetAabb()
            .Merge(_farMeshes[variant].GetAabb()).Merge(_thirdMeshes[variant].GetAabb());
        Aabb result = default;
        bool first = true;
        foreach (var transform in transforms)
        {
            var local = transform;
            local.Origin -= anchor;
            var bounds = CameraOcclusionMath.TransformBounds(meshBounds, local);
            result = first ? bounds : result.Merge(bounds);
            first = false;
        }
        return result.Grow(2f); // modest wind/frustum margin, still chunk-cullable
    }

    // =========================================================
    // Native Forward+ alpha fades between stylized pine LOD batches, no per-frame C#.
    private MultiMeshInstance3D CreateBatch(string name, Mesh mesh, List<Transform3D> transforms,
        Vector3 anchor, float begin, float end, float margin, bool shadows, bool fade, Aabb sharedBounds)
    {
        var multi = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, UseCustomData = true, Mesh = mesh };
        multi.InstanceCount = transforms.Count;
        for (int j = 0; j < transforms.Count; j++)
        {
            var transform = transforms[j]; transform.Origin -= anchor;
            multi.SetInstanceTransform(j, transform);
            multi.SetInstanceCustomData(j, new Color(1, 0, 0, 0));
        }
        var node = new MultiMeshInstance3D
        {
            Name = name, Position = anchor, Multimesh = multi, ExtraCullMargin = 1f,
            CustomAabb = fade ? sharedBounds : default,
            VisibilityRangeBegin = begin, VisibilityRangeEnd = end,
            VisibilityRangeBeginMargin = begin > 0 ? margin : 0, VisibilityRangeEndMargin = margin,
            VisibilityRangeFadeMode = fade ? GeometryInstance3D.VisibilityRangeFadeModeEnum.Self
                : GeometryInstance3D.VisibilityRangeFadeModeEnum.Disabled,
            CastShadow = shadows ? GeometryInstance3D.ShadowCastingSetting.On : GeometryInstance3D.ShadowCastingSetting.Off
        };
        node.SetMeta(WorldRenderEnvelope.AuthoredEndMetadata, end);
        AddChild(node);
        return node;
    }

    // =========================================================
    // Reject entire chunks first, then test individual cached canopy bounds. No per-tree physics bodies or scene searches.
    public void CollectObstructions(Vector3 from, Vector3 to, float radius, HashSet<ICameraFadeTarget> results)
    {
        foreach (var chunk in _active.Values)
        {
            if (!chunk.HasBounds || !CameraOcclusionMath.Intersects(from, to, chunk.Bounds, radius)) continue;
            foreach (var target in chunk.Occluders)
                if (target.IsValid && CameraOcclusionMath.Intersects(from, to, target.Bounds, radius)) results.Add(target);
        }
    }

    private sealed class PineFadeTarget : ICameraFadeTarget
    {
        private readonly MultiMesh _near;
        private readonly MultiMesh? _far, _third;
        private readonly int _index;
        public readonly Aabb Bounds;
        public bool Alive = true;
        public bool IsValid => Alive && GodotObject.IsInstanceValid(_near);
        public PineFadeTarget(MultiMesh near, MultiMesh? far, MultiMesh? third, int index, Aabb bounds)
        { _near = near; _far = far; _third = third; _index = index; Bounds = bounds; }

        // =========================================================
        // Update one tree in both LOD batches; bark uses an opaque material and ignores this channel.
        public void SetCameraOpacity(float opacity)
        {
            if (!IsValid) return;
            var value = new Color(opacity, 0, 0, 0);
            _near.SetInstanceCustomData(_index, value);
            if (GodotObject.IsInstanceValid(_far)) _far!.SetInstanceCustomData(_index, value);
            if (GodotObject.IsInstanceValid(_third)) _third!.SetInstanceCustomData(_index, value);
        }
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
        if (IsInstanceValid(_explorer)) _explorer.ViewPositionChanged -= UpdateAnchor;
        if (IsInstanceValid(_data)) _data.Disconnect("height_maps_changed", Callable.From(QueueTerrainRefresh));
    }

    // =========================================================
    // No lingering signal subscriptions when testworld is closed.
    public override void _ExitTree()
    {
        Visibility.Changed -= OnVisibilityChanged;
        if (_initialized) DisconnectSignals();
        _initialized = false;
        foreach (var chunk in _active.Values) foreach (var target in chunk.Occluders) target.Alive = false;
        OcclusionChanged?.Invoke();
        _forest?.Dispose(); _forest = null!;
    }
}
