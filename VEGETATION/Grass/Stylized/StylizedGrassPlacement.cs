// STG1: seeded patches of imported ForestGrass001 meshes, streamed in GPU MultiMesh chunks.
// Reuses the village's grass exclusion rectangles and Terrain3D height data; no particles or per-blade nodes.
using Godot;
using System.Collections.Generic;
using System.Diagnostics;
using System;

public partial class StylizedGrassPlacement : Node3D
{
    public VegetationVisibilityPolicy Visibility { get; set; } = new(new VegetationVisibilitySettings());
    public bool ExternallyManaged { get; set; }
    public event System.Action? ChunksInvalidated;
    [Export] public StylizedGrassSettings Settings { get; set; } = null!;
    [Export] public BillboardGrassSettings FillerSettings { get; set; } = null!;
    // ForestGrass002 replaces the billboard filler by default, with independently authored dense beds.
    public ForestGrass002Settings? ShortGrassSettings { get; set; }
    // All additional plant types use the same ground-cover chunks, masks and fog policy.
    public GroundCoverSettings? GroundCover { get; set; }
    [Export] public EnvironmentConfig EnvironmentSettings { get; set; } = null!;
    public Godot.Collections.Array<Rect2> ExclusionAreas { get; } = new();
    public int ActiveChunks => _active.Count;
    public int ActiveClumps { get; private set; }
    public int ActiveFiller { get; private set; }
    public int ActiveShortGrass { get; private set; }
    public int ActivePlants { get; private set; }

    private readonly Dictionary<Vector2I, GrassChunk> _active = new();
    private readonly List<PlantArt> _plants = new();
    private Mesh[] _near = System.Array.Empty<Mesh>(), _far = System.Array.Empty<Mesh>();
    private Mesh[] _shortNear = System.Array.Empty<Mesh>(), _shortFar = System.Array.Empty<Mesh>();
    private Mesh[] _billboards = System.Array.Empty<Mesh>();
    private ShaderMaterial? _material, _shortMaterial, _billboardMaterial;
    private GodotObject _terrain = null!;
    private Node3D _anchor = null!;
    private Vector2I _center = new(int.MinValue, int.MinValue);
    private int _worldSize;
    private float _focusDistance;
    private bool _initialized, _refreshQueued;
    private PendingGrassBuild? _pending;

    private sealed class GrassChunk
    {
        public readonly List<MultiMeshInstance3D> Nodes = new();
        public int Clumps, Filler, ShortGrass, Plants;
    }

    private sealed class PendingGrassBuild
    {
        public readonly Vector2I Key;
        public readonly GrassChunk Chunk = new();
        public readonly IEnumerator<bool> Steps;
        public PendingGrassBuild(Vector2I key, Func<GrassChunk, IEnumerable<bool>> stages)
        { Key = key; Steps = stages(Chunk).GetEnumerator(); }
    }

    private sealed class PlantArt
    {
        public readonly GroundCoverPlantSettings Settings;
        // Lods[0] is near. Species may declare two or three authored levels.
        public Mesh[][] Lods = System.Array.Empty<Mesh[]>();
        public ShaderMaterial? Material;
        public PlantArt(GroundCoverPlantSettings settings) { Settings = settings; }
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
        CancelPendingManagedChunk();
        foreach (var chunk in _active.Values) ReleaseChunk(chunk);
        _active.Clear(); ActiveClumps = 0; ActiveFiller = 0; ActiveShortGrass = 0; ActivePlants = 0;
        _center = new Vector2I(int.MinValue, int.MinValue);
        _near = _far = _shortNear = _shortFar = _billboards = System.Array.Empty<Mesh>();
        _material = _shortMaterial = _billboardMaterial = null;
        _plants.Clear();
        if (!_initialized) return;
        if (Settings != null && Settings.Enabled && Settings.PatchesPerChunk > 0 && !LoadTemplates())
            _near = _far = System.Array.Empty<Mesh>();
        if (FillerSettings != null && FillerSettings.Enabled && FillerSettings.PatchesPerChunk > 0
            && FillerSettings.PatchChance > 0f && FillerSettings.MaxPerPatch > 0 && !LoadBillboards())
            _billboards = System.Array.Empty<Mesh>();
        if (ShortGrassSettings != null && ShortGrassSettings.Enabled && ShortGrassSettings.PatchesPerChunk > 0
            && ShortGrassSettings.PatchChance > 0f && ShortGrassSettings.MaxPerPatch > 0 && !LoadShortGrass())
            _shortNear = _shortFar = System.Array.Empty<Mesh>();
        // Build each configured plant art set just once; no per-instance scenes.
        if (GroundCover != null)
            foreach (var species in GroundCover.Plants)
            {
                if (species == null) continue;
                var art = new PlantArt(species);
                if (species.Enabled && species.Share > 0f && !LoadPlantTemplates(art))
                    GD.PushWarning("Ground cover: " + species.Label + " could not load its models.");
                _plants.Add(art);
            }
        ApplyWind();
        ChunksInvalidated?.Invoke();
        if (!ExternallyManaged) UpdateAnchor(_anchor.GlobalPosition);
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
    // One material and three small crossed-card meshes shared by every filler MultiMesh chunk.
    // The billboard atlas is different from the stylized clump atlas and must not be mixed.
    private bool LoadBillboards()
    {
        const string root = "res://ASSETS/Vegetation/Grass/ForestGrass001_Billboard/";
        var sourceMaterial = GD.Load<ShaderMaterial>(root + "Materials/ForestGrass001BillboardWind.tres");
        if (sourceMaterial == null)
        { GD.PushWarning("Billboard filler: material missing; filler disabled."); return false; }
        _billboardMaterial = (ShaderMaterial)sourceMaterial.Duplicate();
        var meshes = new Mesh[3];
        for (int i = 0; i < meshes.Length; i++)
        {
            string path = root + "Models/forest_grass_001_billboard_" + (char)('a' + i) + ".gltf";
            var packed = GD.Load<PackedScene>(path);
            if (packed == null)
            { GD.PushWarning("Billboard filler: missing " + path); return false; }
            Node rootNode = packed.Instantiate();
            try
            {
                MeshInstance3D? meshNode = rootNode as MeshInstance3D;
                if (meshNode == null)
                    foreach (Node child in rootNode.FindChildren("*", "MeshInstance3D", true, false))
                        if (child is MeshInstance3D instance) { meshNode = instance; break; }
                if (meshNode?.Mesh is not ArrayMesh source || source.GetSurfaceCount() != 1)
                { GD.PushWarning("Billboard filler: invalid mesh in " + path); return false; }
                var mesh = (ArrayMesh)source.Duplicate();
                mesh.SurfaceSetMaterial(0, _billboardMaterial);
                meshes[i] = mesh;
            }
            finally { rootNode.Free(); }
        }
        _billboards = meshes;
        return true;
    }

    // =========================================================
    // Import ForestGrass002's three compact tuft variants at two mesh LODs.
    // Keep its own wind/atlas material rather than using billboard cutout artwork.
    private bool LoadShortGrass()
    {
        const string root = "res://ASSETS/Vegetation/Grass/ForestGrass002/";
        var material = GD.Load<ShaderMaterial>(root + "Materials/ForestGrass002Wind.tres");
        if (material == null)
        { GD.PushWarning("ForestGrass002: material missing; short grass disabled."); return false; }
        _shortMaterial = (ShaderMaterial)material.Duplicate();
        var near = new Mesh[3]; var far = new Mesh[3];
        for (int variant = 0; variant < 3; variant++) for (int lod = 1; lod <= 2; lod++)
        {
            string path = root + "Models/forest_grass_002_" + (char)('a' + variant)
                + "_LOD" + lod + ".gltf";
            var packed = GD.Load<PackedScene>(path);
            if (packed == null) { GD.PushWarning("ForestGrass002: missing " + path); return false; }
            Node scene = packed.Instantiate();
            try
            {
                MeshInstance3D? item = scene as MeshInstance3D;
                if (item == null)
                    foreach (Node child in scene.FindChildren("*", "MeshInstance3D", true, false))
                        if (child is MeshInstance3D mesh) { item = mesh; break; }
                if (item?.Mesh is not ArrayMesh source || source.GetSurfaceCount() != 1)
                { GD.PushWarning("ForestGrass002: invalid mesh/surface " + path); return false; }
                var copy = (ArrayMesh)source.Duplicate();
                copy.SurfaceSetMaterial(0, _shortMaterial);
                if (lod == 1) near[variant] = copy; else far[variant] = copy;
            }
            finally { scene.Free(); }
        }
        _shortNear = near; _shortFar = far;
        ApplyWind();
        return true;
    }

    // =========================================================
    // Load a species using the same single-surface imported glTF + MultiMesh pipeline.
    // Future plants only need a settings resource and lettered 2- or 3-LOD meshes.
    private bool LoadPlantTemplates(PlantArt art)
    {
        var species = art.Settings;
        var sourceMaterial = GD.Load<ShaderMaterial>(species.MaterialPath);
        if (sourceMaterial == null) { GD.PushWarning("Missing ground-cover material: " + species.MaterialPath); return false; }
        var material = (ShaderMaterial)sourceMaterial.Duplicate();
        int count = Mathf.Clamp(species.VariantCount, 1, 12);
        int lodCount = Mathf.Clamp(species.LodCount, 2, 3);
        var meshes = new Mesh[lodCount][];
        for (int lod = 0; lod < lodCount; lod++) meshes[lod] = new Mesh[count];
        for (int i = 0; i < count; i++) for (int lod = 1; lod <= lodCount; lod++)
        {
            string path = species.ModelDirectory.TrimEnd('/') + "/" + species.ModelPrefix
                + (char)('a' + i) + "_LOD" + lod + ".gltf";
            var packed = GD.Load<PackedScene>(path);
            if (packed == null) { GD.PushWarning("Missing ground-cover model: " + path); return false; }
            Node root = packed.Instantiate();
            try
            {
                MeshInstance3D? item = root as MeshInstance3D;
                if (item == null)
                    foreach (Node child in root.FindChildren("*", "MeshInstance3D", true, false))
                        if (child is MeshInstance3D mesh) { item = mesh; break; }
                if (item?.Mesh is not ArrayMesh source || source.GetSurfaceCount() != 1)
                { GD.PushWarning("Ground-cover model needs one mesh surface: " + path); return false; }
                var meshCopy = (ArrayMesh)source.Duplicate();
                meshCopy.SurfaceSetMaterial(0, material);
                meshes[lod - 1][i] = meshCopy;
            }
            finally { root.Free(); }
        }
        art.Material = material; art.Lods = meshes;
        return true;
    }

    // =========================================================
    // Only on setup or explicit weather-setting changes; the GPU animates the shared foliage shader.
    public void ApplyWind()
    {
        if (EnvironmentSettings == null) return;
        Vector2 dir = EnvironmentSettings.WindDirection;
        dir = dir.LengthSquared() > 0.0001f ? dir.Normalized() : Vector2.Right;
        if (_material != null && Settings != null)
        {
            _material.SetShaderParameter("wind_direction", dir);
            _material.SetShaderParameter("wind_strength", Mathf.Clamp(
                EnvironmentSettings.GrassWindStrength * Settings.WindMultiplier, 0f, 0.2f));
            _material.SetShaderParameter("wind_speed", Mathf.Max(0f, Settings.WindSpeed));
        }
        if (_billboardMaterial != null && FillerSettings != null)
        {
            _billboardMaterial.SetShaderParameter("wind_direction", dir);
            _billboardMaterial.SetShaderParameter("wind_strength", Mathf.Clamp(
                EnvironmentSettings.GrassWindStrength * FillerSettings.WindMultiplier, 0f, 0.2f));
            _billboardMaterial.SetShaderParameter("wind_speed", Mathf.Max(0f, FillerSettings.WindSpeed));
        }
        if (_shortMaterial != null && ShortGrassSettings != null)
        {
            _shortMaterial.SetShaderParameter("wind_direction", dir);
            _shortMaterial.SetShaderParameter("wind_strength", Mathf.Clamp(
                EnvironmentSettings.GrassWindStrength * ShortGrassSettings.WindMultiplier, 0f, 0.2f));
            _shortMaterial.SetShaderParameter("wind_speed", Mathf.Max(0f, ShortGrassSettings.WindSpeed));
        }
        foreach (var art in _plants)
        {
            if (art.Material == null) continue;
            art.Material.SetShaderParameter("wind_direction", dir);
            art.Material.SetShaderParameter("wind_strength", Mathf.Clamp(
                EnvironmentSettings.GrassWindStrength * art.Settings.WindMultiplier, 0f, 0.2f));
            art.Material.SetShaderParameter("wind_speed", Mathf.Max(0f, art.Settings.WindSpeed));
        }
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
        float lod1 = Mathf.Clamp(Visibility.Lod1End + _focusDistance, lod + 2f, far - 2f);
        foreach (var chunk in _active.Values)
            foreach (var instance in chunk.Nodes)
            {
                string name = instance.Name.ToString();
                bool mid = name.EndsWith("_Mid");
                bool far3 = name.EndsWith("_Far3");
                bool distant = name.EndsWith("_Far");
                bool filler = name.StartsWith("BillboardFiller_");
                instance.VisibilityRangeBegin = far3 ? lod1 : distant || mid ? lod : 0f;
                instance.VisibilityRangeEnd = far3 || distant || filler ? far : mid ? lod1 : lod;
                instance.SetMeta(WorldRenderEnvelope.AuthoredEndMetadata, instance.VisibilityRangeEnd);
            }
    }

    // =========================================================
    private bool HasLoadedPlants()
    {
        foreach (var art in _plants)
            if (art.Lods.Length > 0) return true;
        return false;
    }

    // =========================================================
    // Move only on integer 32m chunk crossings. Existing chunks are reused without polling.
    public void UpdateAnchor(Vector3 position)
    {
        if (ExternallyManaged || (_near.Length != 3 && _shortNear.Length != 3 && _billboards.Length != 3 && !HasLoadedPlants())) return;
        int width = Mathf.Clamp(Settings?.ChunkSize ?? 32, 16, 64);
        var next = new Vector2I(Mathf.FloorToInt(position.X / width),
            Mathf.FloorToInt(position.Z / width));
        if (next == _center) return;
        using var perf = WorldPerformanceProbe.Measure("Grass.ReconcileChunks");
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
            if (!wanted.Contains(old.Key)) { ReleaseChunk(old.Value); ActiveClumps -= old.Value.Clumps; ActiveFiller -= old.Value.Filler;
                ActiveShortGrass -= old.Value.ShortGrass; ActivePlants -= old.Value.Plants; remove.Add(old.Key); }
        foreach (var key in remove) _active.Remove(key);
        foreach (var key in wanted) if (!_active.ContainsKey(key)) CreateChunk(key, width);
    }

    // =========================================================
    // Reapply the single fog-derived envelope and add/remove only chunks whose residency changed.
    private void OnVisibilityChanged()
    {
        if (!_initialized) return;
        if (ExternallyManaged) CancelPendingManagedChunk();
        UpdateView(0f, _focusDistance, true);
        if (!ExternallyManaged)
        {
            _center = new Vector2I(int.MinValue, int.MinValue);
            UpdateAnchor(_anchor.GlobalPosition);
        }
    }

    // =========================================================
    // PERF1: run one part of a chunk at a time; the shared manager supplies the deadline.
    // No partially generated grass is rendered or counted as resident.
    public bool StepManagedChunk(Vector2I key, int width, long deadlineTicks)
    {
        if (!ExternallyManaged || _active.ContainsKey(key)) return true;
        if (_near.Length != 3 && _shortNear.Length != 3 && _billboards.Length != 3 && !HasLoadedPlants()) return true;
        if (_pending != null && _pending.Key != key) CancelPendingManagedChunk();
        if (_pending == null)
            _pending = new PendingGrassBuild(key, chunk => CreateChunkStages(key, width, chunk));
        using var perf = WorldPerformanceProbe.Measure("Grass.BuildStep", key);
        for (int step = 0; step < 16; step++)
        {
            if (!_pending.Steps.MoveNext())
            {
                var complete = _pending;
                complete.Steps.Dispose(); _pending = null;
                CommitChunk(key, complete.Chunk);
                return true;
            }
            if (Stopwatch.GetTimestamp() >= deadlineTicks) break;
        }
        return false;
    }

    // =========================================================
    // Discard hidden, unfinished work on player relocation, fog edits or F1 resets.
    public void CancelPendingManagedChunk()
    {
        if (_pending == null) return;
        _pending.Steps.Dispose();
        ReleaseChunk(_pending.Chunk);
        _pending = null;
    }

    public void ReleaseManagedChunk(Vector2I key)
    {
        if (_pending?.Key == key) CancelPendingManagedChunk();
        if (!_active.TryGetValue(key, out var chunk)) return;
        ReleaseChunk(chunk);
        ActiveClumps -= chunk.Clumps; ActiveFiller -= chunk.Filler;
        ActiveShortGrass -= chunk.ShortGrass; ActivePlants -= chunk.Plants;
        _active.Remove(key);
    }

    // =========================================================
    // All three grass families share residency and road/terrain masks; only art and scatter differ.
    // =========================================================
    private void CreateChunk(Vector2I key, int width)
    {
        using var perf = WorldPerformanceProbe.Measure("Grass.CreateChunk", key);
        var chunk = new GrassChunk();
        foreach (var unused in CreateChunkStages(key, width, chunk)) { }
        CommitChunk(key, chunk);
    }

    // =========================================================
    // Legacy sync and new incremental paths consume the same placement generators,
    // preserving RNG samples, terrain masks, density and final MultiMesh transforms.
    private IEnumerable<bool> CreateChunkStages(Vector2I key, int width, GrassChunk chunk)
    {
        float end = Visibility.RenderDistance + _focusDistance;
        if (_near.Length == 3 || HasLoadedPlants())
            foreach (var unused in CreatePatchClumps(key, width, end, chunk)) yield return true;
        if (_shortNear.Length == 3 && ShortGrassSettings != null)
            foreach (var unused in CreateDensePatches(key, width, end, chunk,
                ShortGrassSettings, _shortNear, _shortFar, false)) yield return true;
        if (_billboards.Length == 3 && FillerSettings != null)
            foreach (var unused in CreateDensePatches(key, width, end, chunk,
                FillerSettings, _billboards, System.Array.Empty<Mesh>(), true)) yield return true;
    }

    private void CommitChunk(Vector2I key, GrassChunk chunk)
    {
        _active.Add(key, chunk);
        ActiveClumps += chunk.Clumps; ActiveFiller += chunk.Filler;
        ActiveShortGrass += chunk.ShortGrass; ActivePlants += chunk.Plants;
        foreach (var node in chunk.Nodes) node.Visible = true;
    }

    // =========================================================
    // Preserve existing clustered stylized-grass scatter and its seed.
    private IEnumerable<bool> CreatePatchClumps(Vector2I key, int width, float end, GrassChunk chunk)
    {
        var rng = new RandomNumberGenerator { Seed = Mix(Settings.Seed, key.X, key.Y) };
        // Independent streams leave the original grass positions and yaw intact.
        var chooser = new RandomNumberGenerator { Seed = Mix(Settings.Seed + 26321, key.X, key.Y) };
        var details = new RandomNumberGenerator { Seed = Mix(Settings.Seed + 81359, key.X, key.Y) };
        var bin = new List<Transform3D>[3];
        for (int i = 0; i < 3; i++) bin[i] = new List<Transform3D>();
        var plantBins = new List<Transform3D>[_plants.Count][];
        var positions = new List<Vector2>[_plants.Count];
        for (int p = 0; p < _plants.Count; p++)
        {
            int variants = Mathf.Clamp(_plants[p].Settings.VariantCount, 1, 12);
            plantBins[p] = new List<Transform3D>[variants];
            for (int variant = 0; variant < variants; variant++)
                plantBins[p][variant] = new List<Transform3D>();
            positions[p] = new List<Vector2>();
        }
        int patches = Mathf.Clamp(Settings.PatchesPerChunk, 0, 12);
        int members = Mathf.Clamp(Settings.ClumpsPerPatch, 1, 30);
        float patchSpreadMultiplier = Mathf.Clamp(Settings.PatchSpreadMultiplier, 1f, 5f);
        for (int patch = 0; patch < patches; patch++)
        {
            if (rng.Randf() > Mathf.Clamp(Settings.PatchChance, 0f, 1f)) continue;
            Vector2 center = new((key.X + rng.RandfRange(0.08f, 0.92f)) * width,
                (key.Y + rng.RandfRange(0.08f, 0.92f)) * width);
            // Preserve the same random samples and accepted-count budget while allowing
            // grass clumps to disperse beyond the original small patch circles.
            float radius = rng.RandfRange(Mathf.Clamp(Settings.PatchRadiusMin, 0.6f, 4f),
                Mathf.Clamp(Mathf.Max(Settings.PatchRadiusMin, Settings.PatchRadiusMax), 0.6f, 6f))
                * patchSpreadMultiplier;
            for (int j = 0; j < members; j++)
            {
                // Yield before the next block, even when earlier candidates were rejected.
                if (j > 0 && j % 8 == 0) yield return true;
                float theta = rng.RandfRange(0f, Mathf.Tau);
                float spread = Mathf.Sqrt(rng.Randf()) * radius;
                Vector2 point = center + new Vector2(Mathf.Cos(theta), Mathf.Sin(theta)) * spread;
                float scale = rng.RandfRange(Mathf.Clamp(Settings.ScaleMin, 0.3f, 2f),
                    Mathf.Clamp(Mathf.Max(Settings.ScaleMin, Settings.ScaleMax), 0.3f, 2.5f));
                if (point.X < 2 || point.Y < 2 || point.X >= _worldSize - 2 || point.Y >= _worldSize - 2
                    || IsExcluded(point, Mathf.Max(0.6f, Settings.EdgeClearance) * scale)) continue;
                if (!TryGround(point, Settings.MinimumNormalY, out float height)) continue;
                int variant = rng.RandiRange(0, 2);
                float yaw = rng.RandfRange(0f, Mathf.Tau);
                // Shares partition candidates; disabled plants leave gaps instead of
                // re-rolling grass. The billboard scatter has its own untouched seed.
                float selection = chooser.Randf(), cumulative = 0f;
                int selected = -1;
                for (int p = 0; p < _plants.Count; p++)
                {
                    cumulative += Mathf.Clamp(_plants[p].Settings.Share, 0f, 1f);
                    if (selection < cumulative) { selected = p; break; }
                }
                if (selected < 0)
                {
                    if (_near.Length == 3)
                        bin[variant].Add(new Transform3D(
                            new Basis(Vector3.Up, yaw).Scaled(Vector3.One * scale),
                            new Vector3(point.X, height - 0.035f, point.Y)));
                    continue;
                }
                var art = _plants[selected]; var plant = art.Settings;
                float plantScale = details.RandfRange(
                    Mathf.Clamp(plant.ScaleMin, 0.25f, 2f),
                    Mathf.Clamp(Mathf.Max(plant.ScaleMin, plant.ScaleMax), 0.25f, 2f));
                int plantVariant = details.RandiRange(0, plantBins[selected].Length - 1);
                if (art.Lods.Length == 0 || !plant.Enabled
                    || IsExcluded(point, Mathf.Max(0.5f, plant.EdgeClearance) * plantScale)
                    || !TryGround(point, plant.MinimumNormalY, out float plantHeight)) continue;
                float spacing = Mathf.Clamp(plant.MinimumSpacing, 0f, 5f);
                bool crowded = false;
                foreach (Vector2 earlier in positions[selected])
                    if (earlier.DistanceSquaredTo(point) < spacing * spacing) { crowded = true; break; }
                if (crowded) continue;
                positions[selected].Add(point);
                plantBins[selected][plantVariant].Add(new Transform3D(
                    new Basis(Vector3.Up, yaw).Scaled(Vector3.One * plantScale),
                    new Vector3(point.X, plantHeight - 0.035f, point.Y)));
            }
        }
        float lod = Mathf.Clamp(Visibility.Lod0End + _focusDistance, 8f, end - 4f);
        float lod1 = Mathf.Clamp(Visibility.Lod1End + _focusDistance, lod + 2f, end - 2f);
        Vector3 origin = new((key.X + 0.5f) * width, 0f, (key.Y + 0.5f) * width);
        if (_near.Length == 3)
            for (int i = 0; i < 3; i++)
            {
                if (bin[i].Count == 0) continue;
                foreach (var unused in AddBatchStages(chunk, "StylizedGrass_" + key.X + "_" + key.Y + "_" + i + "_Near",
                    _near[i], bin[i], origin, 0f, lod)) yield return true;
                foreach (var unused in AddBatchStages(chunk, "StylizedGrass_" + key.X + "_" + key.Y + "_" + i + "_Far",
                    _far[i], bin[i], origin, lod, end)) yield return true;
                chunk.Clumps += bin[i].Count;
            }
        for (int p = 0; p < _plants.Count; p++)
        {
            var art = _plants[p];
            if (art.Lods.Length == 0) continue;
            for (int i = 0; i < art.Lods[0].Length; i++)
            {
                if (plantBins[p][i].Count == 0) continue;
                string name = "GroundPlant_" + p + "_" + key.X + "_" + key.Y + "_" + i;
                foreach (var unused in AddBatchStages(chunk, name + "_Near", art.Lods[0][i],
                    plantBins[p][i], origin, 0f, lod)) yield return true;
                if (art.Lods.Length == 2)
                    foreach (var unused in AddBatchStages(chunk, name + "_Far", art.Lods[1][i],
                        plantBins[p][i], origin, lod, end)) yield return true;
                else
                {
                    foreach (var unused in AddBatchStages(chunk, name + "_Mid", art.Lods[1][i],
                        plantBins[p][i], origin, lod, lod1)) yield return true;
                    foreach (var unused in AddBatchStages(chunk, name + "_Far3", art.Lods[2][i],
                        plantBins[p][i], origin, lod1, end)) yield return true;
                }
                chunk.Plants += plantBins[p][i].Count;
            }
        }
    }

    // =========================================================
    // Irregular seeded dense patches for ForestGrass002 or optional legacy billboards.
    // Independent seeds do not touch ForestGrass001 or the plant selection stream.
    // =========================================================
    private IEnumerable<bool> CreateDensePatches(Vector2I key, int width, float end, GrassChunk chunk,
        BillboardGrassSettings config, Mesh[] near, Mesh[] far, bool billboard)
    {
        var rng = new RandomNumberGenerator { Seed = Mix(config.Seed, key.X, key.Y) };
        var bin = new List<Transform3D>[3];
        for (int i = 0; i < 3; i++) bin[i] = new List<Transform3D>();

        int patches = Mathf.Clamp(config.PatchesPerChunk, 0, 16);
        int least = Mathf.Clamp(config.MinPerPatch, 1, 100);
        int most = Mathf.Clamp(Mathf.Max(least, config.MaxPerPatch), 1, 120);
        float minRadius = Mathf.Clamp(config.PatchRadiusMin, 0.8f, 10f);
        float maxRadius = Mathf.Clamp(Mathf.Max(minRadius, config.PatchRadiusMax), minRadius, 12f);
        float minScale = Mathf.Clamp(config.ScaleMin, 0.3f, 2f);
        float maxScale = Mathf.Clamp(Mathf.Max(minScale, config.ScaleMax), minScale, 2.5f);
        float tiltChance = Mathf.Clamp(config.TiltChance, 0f, 1f);
        float tiltDegrees = Mathf.Clamp(config.MaxTiltDegrees, 0f, 8f);

        for (int patch = 0; patch < patches; patch++)
        {
            if (rng.Randf() > Mathf.Clamp(config.PatchChance, 0f, 1f)) continue;
            Vector2 center = new((key.X + rng.RandfRange(0.06f, 0.94f)) * width,
                (key.Y + rng.RandfRange(0.06f, 0.94f)) * width);
            int members = rng.RandiRange(least, most);
            float radius = rng.RandfRange(minRadius, maxRadius);
            float aspect = rng.RandfRange(0.70f, 1.35f);
            float heading = rng.RandfRange(0f, Mathf.Tau);
            float phase = rng.RandfRange(0f, Mathf.Tau);
            float patchScale = rng.RandfRange(0.82f, 1.18f);

            for (int j = 0; j < members; j++)
            {
                // Yield before the next block, even when earlier candidates were rejected.
                if (j > 0 && j % 8 == 0) yield return true;
                float theta = rng.RandfRange(0f, Mathf.Tau);
                float wobble = 1f + 0.16f * Mathf.Sin(theta * 3f + phase)
                    + 0.09f * Mathf.Sin(theta * 5f - phase);
                float spread = Mathf.Sqrt(rng.Randf()) * radius * wobble;
                Vector2 offset = new Vector2(Mathf.Cos(theta) * aspect,
                    Mathf.Sin(theta) / aspect) * spread;
                Vector2 point = center + offset.Rotated(heading);
                float scale = Mathf.Clamp(rng.RandfRange(minScale, maxScale) * patchScale, 0.4f, 2.2f);
                if (point.X < 2 || point.Y < 2 || point.X >= _worldSize - 2 || point.Y >= _worldSize - 2
                    || IsExcluded(point, Mathf.Max(0.5f, config.EdgeClearance) * scale)) continue;
                if (!TryGround(point, config.MinimumNormalY, out float height)) continue;

                Basis basis = new(Vector3.Up, rng.RandfRange(0f, Mathf.Tau));
                if (tiltDegrees > 0f && rng.Randf() < tiltChance)
                {
                    float angle = rng.RandfRange(0f, Mathf.Tau);
                    Vector3 axis = new(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                    basis *= new Basis(axis, Mathf.DegToRad(rng.RandfRange(0f, tiltDegrees)));
                }
                int variant = rng.RandiRange(0, 2);
                bin[variant].Add(new Transform3D(basis.Scaled(Vector3.One * scale),
                    new Vector3(point.X, height - 0.035f, point.Y)));
            }
        }
        float lod = Mathf.Clamp(Visibility.Lod0End + _focusDistance, 8f, end - 4f);
        Vector3 origin = new((key.X + 0.5f) * width, 0f, (key.Y + 0.5f) * width);
        for (int i = 0; i < 3; i++)
        {
            if (bin[i].Count == 0) continue;
            string name = (billboard ? "BillboardFiller_" : "ForestGrass002_")
                + key.X + "_" + key.Y + "_" + i;
            foreach (var unused in AddBatchStages(chunk, name + (billboard ? "" : "_Near"),
                near[i], bin[i], origin, 0f, billboard ? end : lod)) yield return true;
            if (!billboard)
                foreach (var unused in AddBatchStages(chunk, name + "_Far", far[i], bin[i], origin, lod, end))
                    yield return true;
            if (billboard) chunk.Filler += bin[i].Count;
            else chunk.ShortGrass += bin[i].Count;
        }
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
    private bool TryGround(Vector2 p, float minimumNormalY, out float height)
    {
        height = _terrain.Call("get_height", new Vector3(p.X, 0f, p.Y)).AsSingle();
        float east = _terrain.Call("get_height", new Vector3(p.X + 1f, 0f, p.Y)).AsSingle();
        float south = _terrain.Call("get_height", new Vector3(p.X, 0f, p.Y + 1f)).AsSingle();
        if (!float.IsFinite(height) || !float.IsFinite(east) || !float.IsFinite(south)) return false;
        float normalY = 1f / Mathf.Sqrt(1f + (east - height) * (east - height)
            + (south - height) * (south - height));
        return normalY >= Mathf.Clamp(minimumNormalY, 0.5f, 1f);
    }

    // =========================================================
    // MultiMesh uploads are chunked into groups of 64 transforms in managed mode.
    // Unfinished batches remain detached, then join the hidden chunk until commit.
    private IEnumerable<bool> AddBatchStages(GrassChunk chunk, string name, Mesh mesh,
        List<Transform3D> instances, Vector3 anchor, float begin, float end)
    {
        if (!ExternallyManaged)
        {
            chunk.Nodes.Add(BuildBatch(name, mesh, instances, anchor, begin, end));
            yield return true;
            yield break;
        }
        var multi = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, Mesh = mesh };
        multi.InstanceCount = instances.Count;
        for (int i = 0; i < instances.Count; i++)
        {
            var t = instances[i]; t.Origin -= anchor;
            multi.SetInstanceTransform(i, t);
            if ((i & 63) == 63) yield return true;
        }
        chunk.Nodes.Add(AttachBatch(name, multi, anchor, begin, end, false));
        yield return true;
    }

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
        return AttachBatch(name, multi, anchor, begin, end, true);
    }

    private MultiMeshInstance3D AttachBatch(string name, MultiMesh multi, Vector3 anchor,
        float begin, float end, bool visible)
    {
        var node = new MultiMeshInstance3D {
            Name = name, Multimesh = multi, Position = anchor,
            VisibilityRangeBegin = begin, VisibilityRangeEnd = end,
            VisibilityRangeBeginMargin = begin > 0f ? 5f : 0f,
            VisibilityRangeEndMargin = 5f,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            Visible = visible
        };
        node.SetMeta(WorldRenderEnvelope.AuthoredEndMetadata, end);
        AddChild(node);
        return node;
    }

    // =========================================================
    private void ReleaseChunk(GrassChunk chunk)
    {
        using var perf = WorldPerformanceProbe.Measure("Grass.ReleaseChunk");
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
        CancelPendingManagedChunk();
        Visibility.Changed -= OnVisibilityChanged;
        if (_initialized && GodotObject.IsInstanceValid(_terrain))
            _terrain.Disconnect("height_maps_changed", Callable.From(QueueTerrainRefresh));
        _initialized = false;
    }
}
