// WS2 active-world bridge: adopt four static village buildings; persistent services, site and actors keep their owners.
using Godot;
using System;
using System.Collections.Generic;

public partial class WorldStaticStreaming : Node
{
    public StaticSectionStreamer Streamer { get; private set; } = null!;
    public float LoadDistance { get; private set; }
    public float UnloadDistance { get; private set; }
    private PlayerController _player = null!;
    private Camera _camera = null!;
    private Node _actors = null!;
    private WorldStreamingSettings _settings = null!;
    private readonly HashSet<CharacterBody3D> _bodies = new();
    private bool _initialized;

    // =========================================================
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); }

    // =========================================================
    // Capture is supplied before any unload. Validate every selected instance before transferring ownership.
    public void Initialize(PlayerController player, Camera camera, Node3D structures, Node actors,
        WorldManifest captured, WorldStreamingSettings settings)
    {
        settings.Validate();
        var manifest = WorldManifest.FromJson(captured.ToJson());
        manifest.Placements.RemoveAll(item => !item.InstanceId.StartsWith("village.building.", StringComparison.Ordinal));
        if (manifest.Placements.Count != 4 || settings.MaxResident < 4)
            throw new ArgumentException("WS2 world bridge requires four startup buildings and four resident slots.");
        var nodes = new List<Node3D>();
        foreach (var item in manifest.Placements)
        {
            string name = "Building" + item.InstanceId.Substring(item.InstanceId.LastIndexOf('.') + 1);
            var node = structures.GetNodeOrNull<Node3D>(name);
            if (node == null || node.IsQueuedForDeletion()) throw new ArgumentException($"WS2 missing startup {name}.");
            nodes.Add(node);
        }
        _player = player; _camera = camera; _actors = actors;
        _settings = (WorldStreamingSettings)settings.Duplicate(true);
        Streamer = new StaticSectionStreamer { Name = "StaticSections" }; AddChild(Streamer);
        Streamer.Initialize(manifest, _settings, structures);
        for (int i = 0; i < nodes.Count; i++) Streamer.Adopt(manifest.Placements[i].InstanceId, nodes[i]);
        foreach (Node node in actors.FindChildren("*", "CharacterBody3D", true, false)) OnAdded(node);
        GetTree().NodeAdded += OnAdded; GetTree().NodeRemoved += OnRemoved;
        Streamer.CanUnload = CanUnload;
        _player.PositionChanged += OnMoved;
        _camera.SettingsChanged += UpdateEnvelope; _camera.RenderRangeChanged += UpdateEnvelope;
        _initialized = true; UpdateEnvelope();
        GD.Print($"[WS2] Adopted four village buildings. Preload {LoadDistance:0}m / unload {UnloadDistance:0}m. Store/terrain/actors remain resident.");
    }

    // =========================================================
    // Match the full camera clip even when fog is disabled; round upward to avoid range changes during ordinary zoom/orbit.
    public static Vector2 Distances(WorldStreamingSettings settings, float far, float orbit)
    {
        float reach = Mathf.Ceil((far + orbit + 32) / 16) * 16;
        float load = Mathf.Max(settings.LoadDistance, reach);
        float band = Mathf.Max(settings.UnloadDistance - settings.LoadDistance, settings.MovementStep * 4 + 1);
        return new Vector2(load, Mathf.Max(settings.UnloadDistance, load + band));
    }

    // =========================================================
    private void UpdateEnvelope()
    {
        if (!_initialized) return;
        Vector2 distances = Distances(_settings, _camera.Far, _camera.MaximumRenderOffset);
        if (Mathf.IsEqualApprox(LoadDistance, distances.X) && Mathf.IsEqualApprox(UnloadDistance, distances.Y)) return;
        LoadDistance = distances.X; UnloadDistance = distances.Y;
        Streamer.SetDistances(LoadDistance, UnloadDistance, _player.GlobalPosition);
    }

    // =========================================================
    private void OnMoved(Vector3 position) => Streamer.UpdateAnchor(position);

    // =========================================================
    // Cache actor membership from lifecycle events, not world searches at every camera/movement update.
    private void OnAdded(Node node)
    { if (node is CharacterBody3D body && _actors.IsAncestorOf(body)) _bodies.Add(body); }

    // =========================================================
    private void OnRemoved(Node node)
    { if (node is CharacterBody3D body) _bodies.Remove(body); }

    // =========================================================
    // Conservative actor pin; re-evaluated on the next player movement batch. No distant actor is deactivated or freed.
    private bool CanUnload(WorldPlacementRecord item)
    {
        Rect2 protectedBounds = item.Bounds.Grow(8);
        foreach (var body in _bodies)
        {
            if (!IsInstanceValid(body) || body.IsQueuedForDeletion() || !body.IsInsideTree()) continue;
            Vector3 position = body.GlobalPosition;
            if (protectedBounds.HasPoint(new Vector2(position.X, position.Z))) return false;
        }
        return true;
    }

    // =========================================================
    public override void _ExitTree()
    {
        if (!_initialized) return;
        _initialized = false; GetTree().NodeAdded -= OnAdded; GetTree().NodeRemoved -= OnRemoved;
        if (IsInstanceValid(_player)) _player.PositionChanged -= OnMoved;
        if (IsInstanceValid(_camera)) { _camera.SettingsChanged -= UpdateEnvelope; _camera.RenderRangeChanged -= UpdateEnvelope; }
        if (IsInstanceValid(Streamer)) Streamer.CanUnload = null;
        _bodies.Clear();
    }
}
