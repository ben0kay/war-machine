// DF2: seeded canopy trees in bounded 32m streamed MultiMesh chunks, with trunk-only native collision.
// No per-tree scripts, idle callbacks, or terrain changes. A DF0-approved zone is required.
using Godot;
using System.Collections.Generic;

public partial class ForestPlacement : Node3D, ICameraOcclusionProvider
{
    public VegetationVisibilityPolicy Visibility { get; set; } = new(new VegetationVisibilitySettings());
    public FoliageTintSettings FoliageTint { get; set; } = null!;
    public event System.Action? OcclusionChanged;
    public int TotalTrees { get; private set; }
    public int ActiveTrees { get; private set; }
    public int ActiveChunks => _chunks.Count;
    private ForestSettings _settings = null!;
    private GodotObject _terrain = null!;
    private Node3D _player = null!;
    private Rect2 _area;
    // Six slots per style: three tall variants and three young fillers using far art.
    private const int VariantsPerStyle = 6;
    private Mesh[] _near = System.Array.Empty<Mesh>(), _far = System.Array.Empty<Mesh>();
    private StandardMaterial3D? _safeLeafMaterial;
    private Color _baseLeafColor;
    private readonly Dictionary<Vector2I, List<Tree>> _layout = new();
    private readonly Dictionary<Vector2I, Chunk> _chunks = new();
    private readonly List<(Vector2 Point, float Gap)> _accepted = new();
    private readonly List<Rect2> _reserved = new();
    private Vector2I _current = new(int.MinValue, int.MinValue), _requested;
    private bool _ready, _queued, _refreshQueued;
    private float _viewOffset;

    private readonly struct Tree
    {
        public readonly Vector3 Origin;
        public readonly float Scale, Yaw, TiltAxis, TiltRadians;
        public readonly int Variant;
        public Tree(Vector3 position, float scale, float yaw, float tiltAxis,
            float tiltRadians, int variant)
        { Origin = position; Scale = scale; Yaw = yaw;
            TiltAxis = tiltAxis; TiltRadians = tiltRadians; Variant = variant; }
    }
    private sealed class Chunk
    {
        public readonly List<MultiMeshInstance3D> Batches = new();
        public readonly List<CanopyFade> FadeTargets = new();
        public StaticBody3D? Trunks;
        public Aabb Bounds;
        public bool HasBounds;
        public int Count;
    }

    // =========================================================
    // Generate near and far meshes once for each of three DF1 silhouettes; scaled young trees share far art.
    public void InitializeView(Node3D terrain, Node3D player, ForestSettings settings, Rect2 area,
        Godot.Collections.Array<Rect2> reservations)
    {
        _terrain = terrain.Get("data").AsGodotObject(); _player = player;
        _settings = settings; _area = area;
        // Only startup candidate generation reads these; no per-frame area checks.
        _reserved.Clear();
        foreach (Rect2 reservation in reservations)
            if (reservation.Intersects(area.Grow(2f))) _reserved.Add(reservation.Grow(2f));
        SetProcess(false); SetPhysicsProcess(false);
        var art = (CanopyTreeSettings)GD.Load<CanopyTreeSettings>(
            "res://VEGETATION/Trees/CanopyTree/Configuration/CanopyTreeMaster.tres").Duplicate(true);
        // DF2 white-tree recovery: a known-good native palette is the default until we
        // can verify the custom shaders on the player's GPU. Do not let shader errors
        // silently display white bark/leaves again.
        Material bark, foliage;
        _safeLeafMaterial = null;
        _baseLeafColor = art.LeafColor.Lerp(art.LeafAccentColor, 0.35f);
        if (_settings.UseSafePaletteMaterials)
        {
            bark = new StandardMaterial3D {
                AlbedoColor = art.BarkColor.Lerp(art.BarkScarColor, 0.13f),
                VertexColorUseAsAlbedo = false, Roughness = 1f
            };
            _safeLeafMaterial = new StandardMaterial3D {
                AlbedoColor = _baseLeafColor,
                VertexColorUseAsAlbedo = false, Roughness = 0.95f,
                CullMode = BaseMaterial3D.CullModeEnum.Disabled
            };
            foliage = _safeLeafMaterial;
            // Use the CPU-owned runtime tint, never a render-thread-synchronizing global GET.
            ApplySharedTint(FoliageTint.Tint, FoliageTint.Strength);
        }
        else
        {
            var shaderBark = new ShaderMaterial {
                Shader = GD.Load<Shader>("res://VEGETATION/Forest/Shaders/CanopyBark.gdshader")
            };
            shaderBark.SetShaderParameter("bark_fallback_color", art.BarkColor);
            var shaderFoliage = new ShaderMaterial {
                Shader = GD.Load<Shader>("res://VEGETATION/Forest/Shaders/CanopyFoliage.gdshader")
            };
            shaderFoliage.SetShaderParameter("leaf_fallback_color", art.LeafColor);
            bark = shaderBark; foliage = shaderFoliage;
        }
        // DF2.6: duplicate the original authored tree family, keeping its exact source
        // config intact and adding a slightly leafier high-crown/trunk art resource.
        var leafier = (CanopyTreeSettings)GD.Load<CanopyTreeSettings>(
            "res://VEGETATION/Trees/CanopyTree/Configuration/CanopyTreeLeafierMaster.tres").Duplicate(true);
        _near = new Mesh[VariantsPerStyle * 2]; _far = new Mesh[VariantsPerStyle * 2];
        for (int style = 0; style < 2; style++)
        {
            var source = style == 0 ? art : leafier;
            int offset = style * VariantsPerStyle;
            for (int i = 0; i < 3; i++)
            {
                _near[offset + i] = CanopyTreeMeshBuilder.Create(source, i, bark, foliage);
                _far[offset + i] = CanopyTreeMeshBuilder.Create(source, i, bark, foliage, true);
                // Young fill trees stay cheaper and share their own style's far mesh.
                _near[offset + i + 3] = _far[offset + i];
                _far[offset + i + 3] = _far[offset + i];
            }
        }
        // One log at startup, not per chunk: proves whether both surface materials exist.
        LogMaterialCheck((ArrayMesh)_near[0]);
        BuildLayout();
        _terrain.Connect("height_maps_changed", Callable.From(QueueTerrainRefresh));
        Visibility.Changed += OnVisibilityChanged;
        _ready = true;
        UpdateAnchor(_player.GlobalPosition);
    }

    // =========================================================
    // Safe-mode palette follows the same F1 global tint controls used by stylized grass and pines.
    // Native StandardMaterial3D cannot read a shader global directly; update one shared material.
    public void ApplySharedTint(Color tint, float strength)
    {
        if (_safeLeafMaterial == null) return;
        float luma = _baseLeafColor.R * 0.2126f + _baseLeafColor.G * 0.7152f + _baseLeafColor.B * 0.0722f;
        float target = Mathf.Max(0.05f, tint.R * 0.2126f + tint.G * 0.7152f + tint.B * 0.0722f);
        float ratio = luma / target;
        var matched = new Color(Mathf.Clamp(tint.R * ratio, 0f, 1f),
            Mathf.Clamp(tint.G * ratio, 0f, 1f),
            Mathf.Clamp(tint.B * ratio, 0f, 1f), 1f);
        _safeLeafMaterial.AlbedoColor = _baseLeafColor.Lerp(matched, Mathf.Clamp(strength * 0.85f, 0f, 1f));
    }

    // =========================================================
    // Material diagnostics to distinguish missing shader/surface bindings from art colour.
    private void LogMaterialCheck(ArrayMesh mesh)
    {
        if (mesh.GetSurfaceCount() != 2)
        {
            GD.PushError($"DF2 COLOR CHECK: expected 2 surfaces, got {mesh.GetSurfaceCount()}.");
            return;
        }
        var bark = mesh.SurfaceGetMaterial(0);
        var leaves = mesh.SurfaceGetMaterial(1);
        string mode = _settings.UseSafePaletteMaterials ? "SAFE" : "SHADER";
        GD.Print($"DF2 COLOR CHECK [{mode}]: bark={bark?.GetType().Name ?? "MISSING"}, " +
            $"leaves={leaves?.GetType().Name ?? "MISSING"}, " +
            $"base leaf={_baseLeafColor}, chunks remain deterministic.");
        if (bark == null || leaves == null)
            GD.PushError("DF2 COLOR CHECK: surface material missing; check CanopyTreeMeshBuilder.Create.");
    }

    // =========================================================
    // Candidate cells are global to the zone, independent of future chunk loads and camera motion.
    private void BuildLayout()
    {
        _layout.Clear(); _accepted.Clear(); TotalTrees = 0;
        using var noise = new FastNoiseLite { Seed = _settings.Seed + 313, Frequency = 0.021f };
        PlaceLayer(false, noise); PlaceLayer(true, noise);
        int leafierCount = 0;
        foreach (var pair in _layout)
            foreach (var tree in pair.Value)
                if (tree.Variant >= VariantsPerStyle) leafierCount++;
        GD.Print($"DF2 forest: {TotalTrees} seeded trees, {_layout.Count} occupied chunks at {_area}; "
            + $"DF2.6 canopy: {TotalTrees - leafierCount} original / {leafierCount} leafier.");
    }

    // =========================================================
    // Mature trees first; place smaller fillers into remaining safe gaps with a separate seeded grid.
    private void PlaceLayer(bool young, FastNoiseLite noise)
    {
        float spacing = Mathf.Clamp(young ? _settings.YoungSpacing : _settings.MatureSpacing, 5f, 16f);
        float density = Mathf.Clamp(young ? _settings.YoungDensity : _settings.MatureDensity, 0f, 1f);
        float gap = Mathf.Clamp(young ? _settings.YoungMinSeparation : _settings.MatureMinSeparation, 2f, spacing);
        int countX = Mathf.Max(1, Mathf.CeilToInt(_area.Size.X / spacing));
        int countZ = Mathf.Max(1, Mathf.CeilToInt(_area.Size.Y / spacing));
        float dx = _area.Size.X / countX, dz = _area.Size.Y / countZ;
        for (int z = 0; z < countZ; z++) for (int x = 0; x < countX; x++)
        {
            var rng = new RandomNumberGenerator { Seed = Mix(_settings.Seed + (young ? 1907 : 0), x, z) };
            var p = _area.Position + new Vector2(
                (x + 0.5f + rng.RandfRange(-0.34f, 0.34f)) * dx,
                (z + 0.5f + rng.RandfRange(-0.34f, 0.34f)) * dz);
            if (!_area.HasPoint(p) || IsClearing(p) || IsReserved(p)) continue;
            float noiseValue = Mathf.Clamp(noise.GetNoise2D(p.X, p.Y) * 0.5f + 0.5f, 0f, 1f);
            if (rng.Randf() > Mathf.Min(1f, density * Mathf.Lerp(0.72f, 1.24f, noiseValue))) continue;
            bool farEnough = true;
            foreach (var other in _accepted)
            {
                float minGap = (gap + other.Gap) * 0.5f;
                if (p.DistanceSquaredTo(other.Point) < minGap * minGap) { farEnough = false; break; }
            }
            if (!farEnough || !TryGround(p, out float height)) continue;
            int variant = rng.RandiRange(0, 2);
            float scale = young
                ? rng.RandfRange(Mathf.Clamp(_settings.YoungScaleMin, 0.4f, 0.85f),
                    Mathf.Clamp(Mathf.Max(_settings.YoungScaleMin, _settings.YoungScaleMax), 0.4f, 0.9f))
                : rng.RandfRange(Mathf.Clamp(_settings.MatureScaleMin, 0.7f, 1.3f),
                    Mathf.Clamp(Mathf.Max(_settings.MatureScaleMin, _settings.MatureScaleMax), 0.7f, 1.4f));
            float yaw = rng.RandfRange(0f, Mathf.Tau);
            // Independent hash: changing the 50/50 visual mix does not move any trees,
            // consume the placement RNG, or change trunk collisions/chunk membership.
            float selection = (Mix(_settings.Seed + 4171, x, z) & 0xffffUL) / 65535f;
            int style = selection < Mathf.Clamp(_settings.LeafierShare, 0f, 1f) ? 1 : 0;
            // Independent seeded tilt: no change to tree positions, size, yaw or style
            // selection when this art-only setting is edited (0 = perfectly upright).
            ulong lean = Mix(_settings.Seed + (young ? 9029 : 9011), x, z);
            float maxTilt = Mathf.DegToRad(Mathf.Clamp(_settings.MaxTiltDegrees, 0f, 2f));
            float tiltAxis = (lean & 0xffffUL) * (Mathf.Tau / 65535f);
            float tiltRadians = ((lean >> 16) & 0xffffUL) * (maxTilt / 65535f);
            var tree = new Tree(new Vector3(p.X, height - 0.1f, p.Y), scale,
                yaw, tiltAxis, tiltRadians, variant + (young ? 3 : 0) + style * VariantsPerStyle);
            int chunk = Mathf.Clamp(_settings.ChunkSize, 32, 64);
            var key = new Vector2I(Mathf.FloorToInt(p.X / chunk), Mathf.FloorToInt(p.Y / chunk));
            if (!_layout.TryGetValue(key, out var list)) { list = new List<Tree>(); _layout.Add(key, list); }
            list.Add(tree); _accepted.Add((p, gap)); TotalTrees++;
        }
    }

    // =========================================================
    // DF2.5: retain the southern road-to-forest entry and add a gently meandering
    // north-south trail parallel to the village, with two natural pocket clearings.
    private bool IsClearing(Vector2 p)
    {
        float width = Mathf.Clamp(_settings.TrailWidth, 4f, 10f);
        float entryZ = Mathf.Clamp(_settings.TrailEntryZ,
            _area.Position.Y + 8f, _area.End.Y - 8f);
        float across = Mathf.Clamp((p.X - _area.Position.X) / _area.Size.X, 0f, 1f);
        float entryTrailZ = entryZ + Mathf.Sin(across * Mathf.Pi * 1.7f) * 5.5f;
        if (Mathf.Abs(p.Y - entryTrailZ) < width * 0.5f) return true;
        float along = (p.Y - entryZ) / Mathf.Max(_area.Size.Y, 1f);
        float spineX = _area.GetCenter().X + Mathf.Sin(along * Mathf.Pi * 2.2f) * 6f;
        if (Mathf.Abs(p.X - spineX) < width * 0.38f) return true;
        var a = new Vector2(_area.GetCenter().X - 17f, entryZ + 16f);
        var b = new Vector2(_area.GetCenter().X + 23f,
            _area.Position.Y + _area.Size.Y * 0.30f);
        return p.DistanceSquaredTo(a) < 30.25f || p.DistanceSquaredTo(b) < 20.25f;
    }

    // =========================================================
    // Extra safety for an extended rectangular forest alongside existing village clearings.
    private bool IsReserved(Vector2 point)
    {
        foreach (Rect2 area in _reserved)
            if (area.HasPoint(point)) return true;
        return false;
    }

    // =========================================================
    // Reject missing/steep Terrain3D samples before allocating any tree transform or collider.
    private bool TryGround(Vector2 p, out float height)
    {
        height = _terrain.Call("get_height", new Vector3(p.X, 0, p.Y)).AsSingle();
        float east = _terrain.Call("get_height", new Vector3(p.X + 2f, 0, p.Y)).AsSingle();
        float south = _terrain.Call("get_height", new Vector3(p.X, 0, p.Y + 2f)).AsSingle();
        return float.IsFinite(height) && float.IsFinite(east) && float.IsFinite(south)
            && new Vector2(east - height, south - height).Length() * 0.5f
                <= Mathf.Clamp(_settings.MaximumSlope, 0.1f, 1.5f);
    }

    // =========================================================
    // View/zoom changes update native ranges only; no re-randomization or per-frame LOD work.
    public void UpdateView(float viewOffset)
    {
        _viewOffset = Mathf.Max(0f, viewOffset);
        foreach (var chunk in _chunks.Values)
            foreach (var batch in chunk.Batches) SetRange(batch, batch.Name.ToString().EndsWith("_Far"));
    }

    // =========================================================
    // Player movement queues a single deferred sync on chunk boundary (safe outside physics flush).
    public void UpdateAnchor(Vector3 position)
    {
        if (!_ready) return;
        int size = Mathf.Clamp(_settings.ChunkSize, 32, 64);
        var next = new Vector2I(Mathf.FloorToInt(position.X / size), Mathf.FloorToInt(position.Z / size));
        if (next == _current && !_queued) return;
        _requested = next;
        if (_queued) return;
        _queued = true; CallDeferred(nameof(SyncChunks));
    }

    // =========================================================
    // Iterate only occupied zone chunks; no all-world chunk grids or idle loops.
    public void SyncChunks()
    {
        _queued = false;
        if (!_ready || _requested == _current) return;
        _current = _requested;
        int size = Mathf.Clamp(_settings.ChunkSize, 32, 64);
        var viewer = new Vector2((_current.X + 0.5f) * size, (_current.Y + 0.5f) * size);
        float reach = Visibility.RenderDistance + _viewOffset + size * 1.4f;
        var wanted = new HashSet<Vector2I>();
        foreach (var pair in _layout)
        {
            var centre = new Vector2((pair.Key.X + 0.5f) * size, (pair.Key.Y + 0.5f) * size);
            if (viewer.DistanceSquaredTo(centre) <= reach * reach) wanted.Add(pair.Key);
        }
        var removed = new List<Vector2I>();
        foreach (var pair in _chunks)
            if (!wanted.Contains(pair.Key)) { ReleaseChunk(pair.Value); removed.Add(pair.Key); ActiveTrees -= pair.Value.Count; }
        foreach (var key in removed) _chunks.Remove(key);
        bool added = false;
        foreach (var key in wanted)
            if (!_chunks.ContainsKey(key)) { CreateChunk(key, _layout[key]); added = true; }
        if (added || removed.Count > 0) OcclusionChanged?.Invoke();
    }

    // =========================================================
    // One body per chunk, per-trunk capsules on pine physics layer 5, with near/far mesh batches.
    private void CreateChunk(Vector2I key, List<Tree> trees)
    {
        var group = new Chunk { Count = trees.Count };
        var bins = new List<Transform3D>[_near.Length];
        for (int i = 0; i < bins.Length; i++) bins[i] = new List<Transform3D>();
        StaticBody3D? body = _settings.TrunkCollisionEnabled ? new StaticBody3D {
            Name = $"CanopyTrunks_{key.X}_{key.Y}", CollisionLayer = PinePlacement.TrunkLayer, CollisionMask = 0
        } : null;
        foreach (var tree in trees)
        {
            // Apply one tiny rigid lean from the root: all LOD meshes share the transform.
            // The same rotation is used below for the short physical trunk capsule.
            var axis = new Vector3(Mathf.Cos(tree.TiltAxis), 0f, Mathf.Sin(tree.TiltAxis));
            var tilt = new Basis(new Quaternion(axis, tree.TiltRadians));
            bins[tree.Variant].Add(new Transform3D(
                (tilt * new Basis(Vector3.Up, tree.Yaw)).Scaled(Vector3.One * tree.Scale), tree.Origin));
            if (body == null) continue;
            float radius = 0.39f * Mathf.Clamp(_settings.TrunkRadiusMultiplier, 0.6f, 1.8f) * tree.Scale;
            float height = Mathf.Max(radius * 2.2f, _settings.TrunkCollisionHeight * tree.Scale);
            body.AddChild(new CollisionShape3D {
                Transform = new Transform3D(tilt,
                    tree.Origin + tilt * (Vector3.Up * (height * 0.5f))),
                Shape = new CapsuleShape3D { Radius = radius, Height = height }
            });
        }
        if (body != null) { AddChild(body); group.Trunks = body; }
        int size = Mathf.Clamp(_settings.ChunkSize, 32, 64);
        float end = Visibility.RenderDistance + _viewOffset;
        float lod = Mathf.Clamp(Visibility.Lod0End + _viewOffset, 8f, end - 4f);
        float margin = Visibility.TransitionMargin;
        for (int i = 0; i < bins.Length; i++)
        {
            if (bins[i].Count == 0) continue;
            float meanY = 0f;
            foreach (var pose in bins[i]) meanY += pose.Origin.Y;
            var anchor = new Vector3((key.X + 0.5f) * size, meanY / bins[i].Count, (key.Y + 0.5f) * size);
            var near = Batch($"Canopy_{key.X}_{key.Y}_{i}_Near", _near[i], bins[i], anchor,
                0f, lod, margin, _settings.NearCastShadows);
            var far = Batch($"Canopy_{key.X}_{key.Y}_{i}_Far", _far[i], bins[i], anchor,
                lod, end, margin, _settings.FarCastShadows);
            group.Batches.Add(near); group.Batches.Add(far);
            Aabb crown = _near[i].GetAabb().Merge(_far[i].GetAabb());
            crown.Position += Vector3.Up * crown.Size.Y * 0.36f;
            crown.Size = new Vector3(crown.Size.X, crown.Size.Y * 0.64f, crown.Size.Z);
            for (int j = 0; j < bins[i].Count; j++)
            {
                Aabb bound = CameraOcclusionMath.TransformBounds(crown, GlobalTransform * bins[i][j]);
                group.FadeTargets.Add(new CanopyFade(near.Multimesh, far.Multimesh, j, bound));
                group.Bounds = group.HasBounds ? group.Bounds.Merge(bound) : bound;
                group.HasBounds = true;
            }
        }
        _chunks.Add(key, group); ActiveTrees += trees.Count;
    }

    // =========================================================
    // Only two shared material surfaces; one custom-opacity channel for native MultiMesh instances.
    private MultiMeshInstance3D Batch(string name, Mesh mesh, List<Transform3D> trees, Vector3 anchor,
        float begin, float end, float margin, bool shadows)
    {
        var multimesh = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            UseColors = true, UseCustomData = true, Mesh = mesh };
        multimesh.InstanceCount = trees.Count;
        for (int i = 0; i < trees.Count; i++)
        {
            var pose = trees[i]; pose.Origin -= anchor;
            multimesh.SetInstanceTransform(i, pose);
            multimesh.SetInstanceColor(i, Colors.White); // Preserve geometry vertex colours without instance tint.
            multimesh.SetInstanceCustomData(i, new Color(1f, 0f, 0f, 0f));
        }
        var node = new MultiMeshInstance3D {
            Name = name, Position = anchor, Multimesh = multimesh,
            VisibilityRangeBegin = begin, VisibilityRangeEnd = end,
            VisibilityRangeBeginMargin = begin > 0f ? margin : 0f,
            VisibilityRangeEndMargin = margin,
            CastShadow = shadows ? GeometryInstance3D.ShadowCastingSetting.On
                : GeometryInstance3D.ShadowCastingSetting.Off
        };
        node.SetMeta(WorldRenderEnvelope.AuthoredEndMetadata, end);
        AddChild(node);
        return node;
    }

    // =========================================================
    private void SetRange(MultiMeshInstance3D node, bool far)
    {
        float end = Visibility.RenderDistance + _viewOffset;
        float lod = Mathf.Clamp(Visibility.Lod0End + _viewOffset, 8f, end - 4f);
        node.VisibilityRangeBegin = far ? lod : 0f;
        node.VisibilityRangeEnd = far ? end : lod;
        node.SetMeta(WorldRenderEnvelope.AuthoredEndMetadata, node.VisibilityRangeEnd);
    }

    // =========================================================
    // The common fog profile drives all native LOD ranges and loaded zone chunks.
    private void OnVisibilityChanged()
    {
        if (!_ready) return;
        UpdateView(_viewOffset);
        _current = new Vector2I(int.MinValue, int.MinValue);
        UpdateAnchor(_player.GlobalPosition);
    }

    // =========================================================
    // Existing top-down CameraOcclusion provider fades crowns; FPS intentionally retains full foliage.
    public void CollectObstructions(Vector3 from, Vector3 to, float radius, HashSet<ICameraFadeTarget> results)
    {
        // StandardMaterial3D has no per-instance dither hook; do not perform wasted fade work.
        if (_settings.UseSafePaletteMaterials) return;
        foreach (var chunk in _chunks.Values)
        {
            if (!chunk.HasBounds || !CameraOcclusionMath.Intersects(from, to, chunk.Bounds, radius)) continue;
            foreach (var target in chunk.FadeTargets)
                if (target.IsValid && CameraOcclusionMath.Intersects(from, to, target.Bounds, radius)) results.Add(target);
        }
    }

    private sealed class CanopyFade : ICameraFadeTarget
    {
        private readonly MultiMesh _near, _far;
        private readonly int _index;
        public readonly Aabb Bounds;
        public bool Alive = true;
        public bool IsValid => Alive && GodotObject.IsInstanceValid(_near);
        public CanopyFade(MultiMesh near, MultiMesh far, int index, Aabb bounds)
        { _near = near; _far = far; _index = index; Bounds = bounds; }

        // =========================================================
        public void SetCameraOpacity(float opacity)
        {
            if (!IsValid) return;
            var data = new Color(Mathf.Clamp(opacity, 0f, 1f), 0f, 0f, 0f);
            _near.SetInstanceCustomData(_index, data);
            if (GodotObject.IsInstanceValid(_far)) _far.SetInstanceCustomData(_index, data);
        }
    }

    // =========================================================
    private void ReleaseChunk(Chunk chunk)
    {
        foreach (var target in chunk.FadeTargets) target.Alive = false;
        foreach (var batch in chunk.Batches) { RemoveChild(batch); batch.QueueFree(); }
        if (GodotObject.IsInstanceValid(chunk.Trunks))
        { chunk.Trunks!.CollisionLayer = 0; RemoveChild(chunk.Trunks); chunk.Trunks.QueueFree(); }
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
        if (!_ready) return;
        foreach (var chunk in _chunks.Values) ReleaseChunk(chunk);
        _chunks.Clear(); ActiveTrees = 0; _current = new(int.MinValue, int.MinValue);
        BuildLayout(); UpdateAnchor(_player.GlobalPosition);
        OcclusionChanged?.Invoke();
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
        if (_ready && GodotObject.IsInstanceValid(_terrain))
            _terrain.Disconnect("height_maps_changed", Callable.From(QueueTerrainRefresh));
        _ready = false;
        foreach (var chunk in _chunks.Values) foreach (var target in chunk.FadeTargets) target.Alive = false;
        OcclusionChanged?.Invoke();
    }
}
