// World-local native render ranges for static props and mesh-based vegetation; no object simulation or frame loop.
using Godot;
using System.Collections.Generic;

public partial class WorldRenderCulling : Node
{
    private sealed class Entry
    {
        public VisualInstance3D Node = null!;
        public float End, Begin, Length, Shadow;
        public bool LightEnabled;
    }
    public WorldRenderSettings Settings { get; private set; } = null!;
    public int RegisteredCount => _entries.Count;
    private readonly Dictionary<VisualInstance3D, Entry> _entries = new();
    private readonly HashSet<VisualInstance3D> _pending = new();
    private Node[] _roots = System.Array.Empty<Node>();
    private PlayerDistanceFog _fog = null!;
    private Camera _camera = null!;
    private PlayerController _player = null!;
    private bool _initialized, _queued, _force, _viewSubscribed;
    private float _cameraY = float.NaN, _horizontal = float.NaN;

    // =========================================================
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); }

    // =========================================================
    public void Initialize(PlayerDistanceFog fog, Camera camera, PlayerController player, WorldRenderSettings settings, params Node[] roots)
    {
        if (_initialized) return;
        _fog = fog; _camera = camera; _player = player; Settings = settings; _roots = roots; _initialized = true;
        GetTree().NodeAdded += OnAdded; GetTree().NodeRemoved += OnRemoved;
        _fog.SettingsApplied += ApplySettings; _camera.SettingsChanged += ApplySettings;
        foreach (Node root in roots) Discover(root);
        ApplySettings();
    }

    // =========================================================
    // Reuse explicit setting events. Fog OFF/translucent restores authored ranges and detaches movement notifications.
    public void ApplySettings()
    {
        if (!_initialized) return;
        bool active = Settings.Enabled && _fog.FullyOpaque && !_fog.CameraMetric;
        if (active != _viewSubscribed)
        {
            if (active) _camera.ViewChanged += OnViewChanged; else _camera.ViewChanged -= OnViewChanged;
            _viewSubscribed = active;
        }
        _force = true; QueueRefresh();
    }

    // =========================================================
    // This fast signal guard runs only during existing camera activity. Native culling itself follows every frame.
    private void OnViewChanged()
    {
        float horizontal = new Vector2(_camera.GlobalPosition.X - _player.GlobalPosition.X,
            _camera.GlobalPosition.Z - _player.GlobalPosition.Z).Length();
        float step = WorldRenderEnvelope.Safe(Settings.CameraRefreshStep, 2, 1, 8);
        if (!float.IsFinite(_cameraY) || Mathf.Abs(_cameraY - _camera.GlobalPosition.Y) >= step
            || Mathf.Abs(_horizontal - horizontal) >= step) QueueRefresh();
    }

    // =========================================================
    private void Discover(Node node)
    { OnAdded(node); foreach (Node child in node.GetChildren()) Discover(child); }

    // =========================================================
    private bool Eligible(Node node)
    {
        bool underRoot = false;
        foreach (Node root in _roots) if (IsInstanceValid(root) && (root == node || root.IsAncestorOf(node))) { underRoot = true; break; }
        if (!underRoot) return false;
        for (Node? at = node; at != null; at = at.GetParent())
            if (at.HasMeta(WorldRenderEnvelope.ExemptMetadata) && at.GetMeta(WorldRenderEnvelope.ExemptMetadata).AsBool()) return false;
        return node is MeshInstance3D or MultiMeshInstance3D or OmniLight3D or SpotLight3D;
    }

    // =========================================================
    // A deferred batch observes finished GLB/procedural Ready callbacks and final placement transforms.
    private void OnAdded(Node node)
    {
        if (!_initialized || node is not VisualInstance3D visual || !Eligible(node)) return;
        _pending.Add(visual); QueueRefresh();
    }

    // =========================================================
    private void QueueRefresh()
    { if (_queued || !_initialized) return; _queued = true; CallDeferred(nameof(Flush)); }

    // =========================================================
    // Apply explicit runtime transform/material/geometry edits without a repeated scene-tree scan.
    public void Refresh() { _force = true; QueueRefresh(); }

    // =========================================================
    public void RefreshSubtree(Node root) { Discover(root); Refresh(); }

    // =========================================================
    public void Flush()
    {
        _queued = false; if (!_initialized) return;
        foreach (VisualInstance3D node in _pending)
        {
            if (!IsInstanceValid(node) || !node.IsInsideTree() || node.IsQueuedForDeletion() || !Eligible(node) || _entries.ContainsKey(node)) continue;
            var entry = new Entry { Node = node };
            if (node is GeometryInstance3D geometry) entry.End = AuthoredEnd(geometry, geometry.VisibilityRangeEnd);
            else if (node is Light3D light)
            { entry.LightEnabled = light.DistanceFadeEnabled; entry.Begin = light.DistanceFadeBegin; entry.Length = light.DistanceFadeLength; entry.Shadow = light.DistanceFadeShadow; }
            _entries.Add(node, entry);
            if (!_force) Apply(entry);
        }
        _pending.Clear();
        float horizontal = new Vector2(_camera.GlobalPosition.X - _player.GlobalPosition.X,
            _camera.GlobalPosition.Z - _player.GlobalPosition.Z).Length();
        bool changed = _force || (Settings.Enabled && _fog.FullyOpaque &&
            (!float.IsFinite(_cameraY) || !Mathf.IsEqualApprox(_cameraY, _camera.GlobalPosition.Y)
            || !Mathf.IsEqualApprox(_horizontal, horizontal)));
        if (changed) foreach (Entry entry in _entries.Values) if (IsInstanceValid(entry.Node)) Apply(entry);
        _cameraY = _camera.GlobalPosition.Y; _horizontal = horizontal; _force = false;
    }

    // =========================================================
    private static float AuthoredEnd(GeometryInstance3D geometry, float fallback)
        => geometry.HasMeta(WorldRenderEnvelope.AuthoredEndMetadata) ? geometry.GetMeta(WorldRenderEnvelope.AuthoredEndMetadata).AsSingle() : fallback;

    // =========================================================
    private void Apply(Entry entry)
    {
        bool active = Settings.Enabled && _fog.FullyOpaque && Eligible(entry.Node);
        float step = WorldRenderEnvelope.Safe(Settings.CameraRefreshStep, 2, 1, 8);
        float buffer = WorldRenderEnvelope.Safe(Settings.HiddenBuffer, 8, 4, 64) + step * 2;
        float shadow = WorldRenderEnvelope.Safe(Settings.ShadowBuffer, 48, 0, 160);
        if (entry.Node is GeometryInstance3D geometry)
        {
            float authored = AuthoredEnd(geometry, entry.End);
            float end = authored;
            if (active && _fog.OpaqueBackdrop)
            {
                Aabb bounds = WorldRenderEnvelope.WorldBounds(geometry,
                    WorldRenderEnvelope.Safe(Settings.DeformationPadding, 2, 0, 8));
                float safety = buffer + Mathf.Max(0, geometry.VisibilityRangeEndMargin)
                    + (geometry.CastShadow == GeometryInstance3D.ShadowCastingSetting.Off ? 0 : shadow);
                float policy = WorldRenderEnvelope.End(bounds, _camera.GlobalPosition, _player.GlobalPosition,
                    _fog.OpaqueRadius, _fog.CameraMetric, safety);
                if (float.IsFinite(policy) && policy > 0) end = WorldRenderEnvelope.Cap(authored, policy);
            }
            if (!Mathf.IsEqualApprox(geometry.VisibilityRangeEnd, end)) geometry.VisibilityRangeEnd = end;
        }
        else if (entry.Node is Light3D light)
        {
            if (entry.LightEnabled) return; // This policy never owns an already-authored fade curve.
            if (!active || !Settings.FadeLocalLights)
            { Restore(entry); return; }
            float radius = light is OmniLight3D omni ? omni.OmniRange : ((SpotLight3D)light).SpotRange;
            Vector3 scale = light.GlobalBasis.Scale;
            radius *= Mathf.Max(Mathf.Abs(scale.X), Mathf.Max(Mathf.Abs(scale.Y), Mathf.Abs(scale.Z)));
            Aabb influence = new(light.GlobalPosition - Vector3.One * radius, Vector3.One * radius * 2);
            float length = WorldRenderEnvelope.Safe(Settings.LightFadeLength, 8, 1, 32);
            float end = WorldRenderEnvelope.End(influence, _camera.GlobalPosition, _player.GlobalPosition,
                _fog.OpaqueRadius, _fog.CameraMetric, buffer + shadow + length);
            if (!float.IsFinite(end)) { Restore(entry); return; }
            // Existing authored fades own their curve (restored above). Never alter Visible, energy or fixture state.
            float begin = Mathf.Max(0, end - length);
            light.DistanceFadeBegin = begin; light.DistanceFadeLength = length;
            light.DistanceFadeShadow = end;
            light.DistanceFadeEnabled = true;
        }
    }

    // =========================================================
    private static void Restore(Entry entry)
    {
        if (!IsInstanceValid(entry.Node)) return;
        if (entry.Node is GeometryInstance3D geometry) geometry.VisibilityRangeEnd = AuthoredEnd(geometry, entry.End);
        else if (entry.Node is Light3D light && !entry.LightEnabled)
        { light.DistanceFadeBegin = entry.Begin; light.DistanceFadeLength = entry.Length; light.DistanceFadeShadow = entry.Shadow; light.DistanceFadeEnabled = entry.LightEnabled; }
    }

    // =========================================================
    private void OnRemoved(Node node)
    {
        if (node is not VisualInstance3D visual) return;
        _pending.Remove(visual);
        if (_entries.Remove(visual, out Entry? entry)) Restore(entry);
    }

    // =========================================================
    public override void _ExitTree()
    {
        if (!_initialized) return;
        _initialized = false;
        GetTree().NodeAdded -= OnAdded; GetTree().NodeRemoved -= OnRemoved;
        if (IsInstanceValid(_fog)) _fog.SettingsApplied -= ApplySettings;
        if (IsInstanceValid(_camera))
        { _camera.SettingsChanged -= ApplySettings; if (_viewSubscribed) _camera.ViewChanged -= OnViewChanged; }
        foreach (Entry entry in _entries.Values) Restore(entry);
        _entries.Clear(); _pending.Clear(); _roots = System.Array.Empty<Node>();
    }
}
