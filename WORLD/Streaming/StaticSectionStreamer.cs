// WS2 static placement residency only. Bounded manifest, threaded resources, main-thread scene operations; sleeps at rest.
using Godot;
using System;
using System.Collections.Generic;

public partial class StaticSectionStreamer : Node
{
    private sealed class Request
    {
        public string Path = "";
        public PackedScene? Scene;
        public bool Failed;
    }
    public event Action? Changed;
    public int ResidentCount => _resident.Count;
    public int InFlightCount => _requests.Count;
    public int DesiredCount => _wanted.Count;
    public int LoadCount { get; private set; }
    public int UnloadCount { get; private set; }
    public bool Working => IsProcessing();
    private readonly Dictionary<string, Node3D> _resident = new();
    private readonly Dictionary<string, Request> _requests = new();
    private readonly List<string> _release = new();
    private readonly Dictionary<string, ulong> _readyAt = new();
    private readonly HashSet<string> _wanted = new(), _failed = new();
    private WorldManifest _manifest = null!;
    private WorldStreamingSettings _settings = null!;
    private Node3D _root = null!;
    private Vector2 _anchor;
    private bool _initialized, _hasAnchor;

    // =========================================================
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); }

    // =========================================================
    // Own an immutable description/settings copy. Reject unsupported overrides rather than ignoring saved data.
    public void Initialize(WorldManifest manifest, WorldStreamingSettings settings, Node3D root)
    {
        if (_initialized) throw new InvalidOperationException("Streamer is already initialized.");
        settings.Validate(); manifest.Validate();
        if (manifest.Placements.Count > settings.MaxPlacements) throw new ArgumentException("WS2 placement limit exceeded.");
        foreach (var item in manifest.Placements)
            if (item.Overrides.Count != 0) throw new ArgumentException("WS2 does not apply placement overrides.");
        _manifest = WorldManifest.FromJson(manifest.ToJson());
        _settings = (WorldStreamingSettings)settings.Duplicate(true); _root = root; _initialized = true;
    }

    // =========================================================
    // Caller supplies movement events. Hysteresis uses actual footprints, so crossing an ownership edge cannot duplicate a site.
    public void UpdateAnchor(Vector3 position, bool force = false)
    {
        if (!_initialized || !float.IsFinite(position.X) || !float.IsFinite(position.Z)) return;
        Vector2 next = new(position.X, position.Z);
        if (_hasAnchor && !force && next.DistanceSquaredTo(_anchor) < _settings.MovementStep * _settings.MovementStep) return;
        _anchor = next; _hasAnchor = true;
        foreach (var item in _manifest.Placements)
        {
            float distance = DistanceToBounds(next, item.Bounds);
            if (distance <= _settings.LoadDistance) _wanted.Add(item.InstanceId);
            else if (distance > _settings.UnloadDistance) _wanted.Remove(item.InstanceId);
        }
        SetProcess(true); Changed?.Invoke();
    }

    // =========================================================
    public static float DistanceToBounds(Vector2 point, Rect2 bounds)
        => point.DistanceTo(point.Clamp(bounds.Position, bounds.End));

    // =========================================================
    public bool IsResident(string id) => _resident.ContainsKey(id);

    // =========================================================
    public bool IsCollisionReady(string id) => _readyAt.TryGetValue(id, out ulong frame) && Engine.GetPhysicsFrames() >= frame;

    // =========================================================
    // Poll at most MaxInFlight jobs. Scene attachment and teardown are atomic operations, bounded by count, not a guaranteed ms budget.
    public override void _Process(double delta)
    {
        if (!_initialized) { SetProcess(false); return; }
        int budget = _settings.OperationsPerFrame;
        foreach (var item in _manifest.Placements)
        {
            if (budget == 0) break;
            if (!_wanted.Contains(item.InstanceId) && _resident.Remove(item.InstanceId, out Node3D? node))
            { _readyAt.Remove(item.InstanceId); node.Free(); budget--; UnloadCount++; }
        }
        foreach (Request request in _requests.Values)
        {
            if (request.Scene != null || request.Failed) continue;
            ResourceLoader.ThreadLoadStatus status = ResourceLoader.LoadThreadedGetStatus(request.Path);
            if (status == ResourceLoader.ThreadLoadStatus.Loaded)
            {
                request.Scene = ResourceLoader.LoadThreadedGet(request.Path) as PackedScene;
                request.Failed = request.Scene == null;
            }
            else if (status == ResourceLoader.ThreadLoadStatus.Failed || status == ResourceLoader.ThreadLoadStatus.InvalidResource)
                request.Failed = true;
        }
        foreach (var item in _manifest.Placements)
        {
            if (budget == 0 || _resident.Count >= _settings.MaxResident) break;
            if (!_wanted.Contains(item.InstanceId) || _resident.ContainsKey(item.InstanceId) || _failed.Contains(item.InstanceId)) continue;
            WorldAssetCatalog.TryResolve(item.AssetId, out string path);
            if (!_requests.TryGetValue(path, out Request? request))
            {
                if (_requests.Count >= _settings.MaxInFlight) continue;
                request = new Request { Path = path }; _requests.Add(path, request);
                if (ResourceLoader.LoadThreadedRequest(path, "PackedScene", false) != Error.Ok) request.Failed = true;
            }
            if (request.Failed) { _failed.Add(item.InstanceId); GD.PushWarning($"WS2 failed resource: {path}"); continue; }
            if (request.Scene == null) continue;
            Node? instance = null;
            try
            {
                instance = request.Scene.Instantiate();
                if (instance is not Node3D visual) throw new InvalidOperationException("Static placement needs a Node3D root.");
                visual.Name = item.InstanceId; visual.SetMeta("world_placement_id", item.InstanceId);
                // Set world placement before Ready, including a transformed residency root.
                visual.Transform = _root.GlobalTransform.AffineInverse() * item.WorldTransform;
                _root.AddChild(visual); _resident.Add(item.InstanceId, visual);
                _readyAt[item.InstanceId] = Engine.GetPhysicsFrames() + 2; LoadCount++;
            }
            catch (Exception error)
            {
                if (IsInstanceValid(instance)) instance!.Free();
                _failed.Add(item.InstanceId); GD.PushWarning($"WS2 placement {item.InstanceId}: {error.Message}");
            }
            budget--;
        }
        // Obsolete threaded requests cannot be canceled by Godot: drain completion and drop references without instantiating.
        _release.Clear();
        foreach (var pair in _requests)
        {
            if (pair.Value.Scene == null && !pair.Value.Failed) continue;
            bool needed = false;
            foreach (var item in _manifest.Placements)
                if (_wanted.Contains(item.InstanceId) && !_resident.ContainsKey(item.InstanceId) && !_failed.Contains(item.InstanceId)
                    && WorldAssetCatalog.TryResolve(item.AssetId, out string path) && path == pair.Key) { needed = true; break; }
            if (!needed || _resident.Count >= _settings.MaxResident) _release.Add(pair.Key);
        }
        foreach (string path in _release) _requests.Remove(path);
        bool remaining = _requests.Count > 0;
        foreach (var item in _manifest.Placements)
            if ((_resident.ContainsKey(item.InstanceId) && !_wanted.Contains(item.InstanceId))
                || (_wanted.Contains(item.InstanceId) && !_resident.ContainsKey(item.InstanceId)
                    && !_failed.Contains(item.InstanceId) && _resident.Count < _settings.MaxResident)) { remaining = true; break; }
        SetProcess(remaining); Changed?.Invoke();
    }

    // =========================================================
    // The residency root belongs to the same scene lifetime. No external player/actor subscriptions are held here.
    public override void _ExitTree()
    {
        SetProcess(false); _initialized = false;
        foreach (Node3D node in _resident.Values) if (IsInstanceValid(node)) node.QueueFree();
        // A scene switch may happen during a threaded read. Drain it outside this scene without attaching obsolete content.
        var pending = new List<string>();
        foreach (var pair in _requests) if (pair.Value.Scene == null && !pair.Value.Failed) pending.Add(pair.Key);
        if (pending.Count > 0)
        {
            var drain = new StaticSectionLoadDrain(); drain.Paths.AddRange(pending);
            Node root = GetTree().Root;
            Callable.From(() => { if (IsInstanceValid(root) && root.IsInsideTree()) root.AddChild(drain); else drain.Free(); }).CallDeferred();
        }
        _resident.Clear(); _readyAt.Clear(); _wanted.Clear(); _requests.Clear(); _failed.Clear(); Changed = null;
    }
}
