// STG1: seeded patches of imported ForestGrass001 meshes, streamed in GPU MultiMesh chunks.
// Reuses the village's grass exclusion rectangles and Terrain3D height data; no particles or per-blade nodes.
using Godot;
using System.Collections.Generic;

public partial class StylizedGrassPlacement : Node3D
{
    public VegetationVisibilityPolicy Visibility { get; set; } = new(new VegetationVisibilitySettings());
    [Export] public StylizedGrassSettings Settings { get; set; } = null!;
    [Export] public BillboardGrassSettings FillerSettings { get; set; } = null!;
    // All additional plant types use the same ground-cover chunks, masks and fog policy.
    public GroundCoverSettings? GroundCover { get; set; }
    [Export] public EnvironmentConfig EnvironmentSettings { get; set; } = null!;
    public Godot.Collections.Array<Rect2> ExclusionAreas { get; } = new();
    public int ActiveChunks => _active.Count;
    public int ActiveClumps { get; private set; }
    public int ActiveFiller { get; private set; }
    public int ActivePlants { get; private set; }

    private readonly Dictionary<Vector2I, GrassChunk> _active = new();
    private readonly List<PlantArt> _plants = new();
    private Mesh[] _near = System.Array.Empty<Mesh>(), _far = System.Array.Empty<Mesh>();
    private Mesh[] _billboards = System.Array.Empty<Mesh>();
    private ShaderMaterial? _material, _billboardMaterial;
    private GodotObject _terrain = null!;
    private Node3D _anchor = null!;
    private Vector2I _center = new(int.MinValue, int.MinValue);
    private int _worldSize;
    private float _focusDistance;
    private bool _initialized, _refreshQueued;

    private sealed class GrassChunk
    {
        public readonly List<MultiMeshInstance3D> Nodes = new();
        public int Clumps, Filler, Plants;
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
        foreach (var chunk in _active.Values) ReleaseChunk(chunk);
        _active.Clear(); ActiveClumps = 0; ActiveFiller = 0; ActivePlants = 0;
        _center = new Vector2I(int.MinValue, int.MinValue);
        _near = _far = _billboards = System.Array.Empty<Mesh>(); _material = _billboardMaterial = null;
        _plants.Clear();
        if (!_initialized) return;
        if (Settings != null && Settings.Enabled && Settings.PatchesPerChunk > 0 && !LoadTemplates())
            _near = _far = System.Array.Empty<Mesh>();
        if (FillerSettings != null && FillerSettings.Enabled && FillerSettings.PatchesPerChunk > 0
            && FillerSettings.PatchChance > 0f && FillerSettings.MaxPerPatch > 0 && !LoadBillboards())
            _billboards = System.Array.Empty<Mesh>();
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
        if (_near.Length != 3 && _billboards.Length != 3 && !HasLoadedPlants()) return;
        int width = Mathf.Clamp(Settings?.ChunkSize ?? 32, 16, 64);
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
            if (!wanted.Contains(old.Key)) { ReleaseChunk(old.Value); ActiveClumps -= old.Value.Clumps; ActiveFiller -= old.Value.Filler;
                ActivePlants -= old.Value.Plants; remove.Add(old.Key); }
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
    // The original stylized patches and widespread billboard filler share residency and masks.
    // =========================================================
    private void CreateChunk(Vector2I key, int width)
    {
        var chunk = new GrassChunk();
        float end = Visibility.RenderDistance + _focusDistance;
        if (_near.Length == 3 || HasLoadedPlants()) CreatePatchClumps(key, width, end, chunk);
        if (_billboards.Length == 3) CreateFiller(key, width, end, chunk);
        _active.Add(key, chunk);
        ActiveClumps += chunk.Clumps; ActiveFiller += chunk.Filler; ActivePlants += chunk.Plants;
    }

    // =========================================================
    // Preserve existing clustered stylized-grass scatter and its seed.
    private void CreatePatchClumps(Vector2I key, int width, float end, GrassChunk chunk)
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
                chunk.Nodes.Add(BuildBatch("StylizedGrass_" + key.X + "_" + key.Y + "_" + i + "_Near",
                    _near[i], bin[i], origin, 0f, lod));
                chunk.Nodes.Add(BuildBatch("StylizedGrass_" + key.X + "_" + key.Y + "_" + i + "_Far",
                    _far[i], bin[i], origin, lod, end));
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
                chunk.Nodes.Add(BuildBatch(name + "_Near", art.Lods[0][i],
                    plantBins[p][i], origin, 0f, lod));
                if (art.Lods.Length == 2)
                    chunk.Nodes.Add(BuildBatch(name + "_Far", art.Lods[1][i],
                        plantBins[p][i], origin, lod, end));
                else
                {
                    chunk.Nodes.Add(BuildBatch(name + "_Mid", art.Lods[1][i],
                        plantBins[p][i], origin, lod, lod1));
                    chunk.Nodes.Add(BuildBatch(name + "_Far3", art.Lods[2][i],
                        plantBins[p][i], origin, lod1, end));
                }
                chunk.Plants += plantBins[p][i].Count;
            }
        }
    }

    // =========================================================
    // Irregular seeded billboard patches: many models per patch, variable footprint and scale.
    // A separate seed keeps the original stylized-grass positions completely unchanged.
    // =========================================================
    private void CreateFiller(Vector2I key, int width, float end, GrassChunk chunk)
    {
        var rng = new RandomNumberGenerator { Seed = Mix(FillerSettings.Seed, key.X, key.Y) };
        var bin = new List<Transform3D>[3];
        for (int i = 0; i < 3; i++) bin[i] = new List<Transform3D>();

        int patches = Mathf.Clamp(FillerSettings.PatchesPerChunk, 0, 16);
        int least = Mathf.Clamp(FillerSettings.MinPerPatch, 1, 100);
        int most = Mathf.Clamp(Mathf.Max(least, FillerSettings.MaxPerPatch), 1, 120);
        float minRadius = Mathf.Clamp(FillerSettings.PatchRadiusMin, 0.8f, 10f);
        float maxRadius = Mathf.Clamp(Mathf.Max(minRadius, FillerSettings.PatchRadiusMax), minRadius, 12f);
        float minScale = Mathf.Clamp(FillerSettings.ScaleMin, 0.3f, 2f);
        float maxScale = Mathf.Clamp(Mathf.Max(minScale, FillerSettings.ScaleMax), minScale, 2.5f);
        float tiltChance = Mathf.Clamp(FillerSettings.TiltChance, 0f, 1f);
        float tiltDegrees = Mathf.Clamp(FillerSettings.MaxTiltDegrees, 0f, 8f);

        for (int patch = 0; patch < patches; patch++)
        {
            if (rng.Randf() > Mathf.Clamp(FillerSettings.PatchChance, 0f, 1f)) continue;
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
                float theta = rng.RandfRange(0f, Mathf.Tau);
                float wobble = 1f + 0.16f * Mathf.Sin(theta * 3f + phase)
                    + 0.09f * Mathf.Sin(theta * 5f - phase);
                float spread = Mathf.Sqrt(rng.Randf()) * radius * wobble;
                Vector2 offset = new Vector2(Mathf.Cos(theta) * aspect,
                    Mathf.Sin(theta) / aspect) * spread;
                Vector2 point = center + offset.Rotated(heading);
                float scale = Mathf.Clamp(rng.RandfRange(minScale, maxScale) * patchScale, 0.4f, 2.2f);
                if (point.X < 2 || point.Y < 2 || point.X >= _worldSize - 2 || point.Y >= _worldSize - 2
                    || IsExcluded(point, Mathf.Max(0.5f, FillerSettings.EdgeClearance) * scale)) continue;
                if (!TryGround(point, FillerSettings.MinimumNormalY, out float height)) continue;

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
        for (int i = 0; i < 3; i++)
        {
            if (bin[i].Count == 0) continue;
            Vector3 origin = new((key.X + 0.5f) * width, 0f, (key.Y + 0.5f) * width);
            var node = BuildBatch("BillboardFiller_" + key.X + "_" + key.Y + "_" + i,
                _billboards[i], bin[i], origin, 0f, end);
            chunk.Nodes.Add(node);
            chunk.Filler += bin[i].Count;
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
