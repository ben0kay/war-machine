// Event-driven scenery obstruction checks (8Hz maximum); interpolation sleeps when fades settle.
using Godot;
using System.Collections.Generic;

public partial class CameraOcclusion : Node
{
    private Camera _camera = null!;
    private PlayerController _player = null!;
    private CameraSettings _settings = null!;
    private Timer _check = null!;
    private readonly CapsuleShape3D _shape = new();
    private PhysicsShapeQueryParameters3D _query = null!;
    private readonly List<ICameraOcclusionProvider> _providers = new();
    private readonly HashSet<ICameraFadeTarget> _hits = new();
    private sealed class Fade
    { public float Current = 1, Goal = 1; }
    private readonly Dictionary<ICameraFadeTarget, Fade> _fades = new();
    private readonly List<ICameraFadeTarget> _remove = new();
    private bool _initialized;

    // =========================================================
    // Cached references and explicit movement signals; no global object searches or stationary query timer.
    public void Initialize(Camera camera, PlayerController player, CameraSettings settings)
    {
        _camera = camera; _player = player; _settings = settings;
        _query = new PhysicsShapeQueryParameters3D { Shape = _shape, CollisionMask = CameraOccluder.ObstructionLayer,
            CollideWithAreas = true, CollideWithBodies = false };
        _check = new Timer { OneShot = true, ProcessCallback = Timer.TimerProcessCallback.Physics };
        AddChild(_check); _check.Timeout += CheckObstructions;
        camera.ViewChanged += Invalidate; player.PositionChanged += OnPlayerMoved; player.StanceChanged += OnStanceChanged;
        CameraOccluder.RegistryChanged += Invalidate;
        SetProcess(false); SetPhysicsProcess(false); _initialized = true; Invalidate();
    }

    // =========================================================
    // Instanced scenery supplies its own cheap spatial lookup without per-tree physics bodies.
    public void RegisterProvider(ICameraOcclusionProvider provider)
    { _providers.Add(provider); provider.OcclusionChanged += Invalidate; Invalidate(); }

    private void OnPlayerMoved(Vector3 position) => Invalidate();
    private void OnStanceChanged(bool crouching) => Invalidate();

    // =========================================================
    // Repeated movement coalesces into one bounded physics query; no timer runs while everything is stationary.
    public void Invalidate()
    {
        if (_initialized && _settings.ObstructionFadeEnabled && _check.IsStopped()) _check.Start(1.0 / Mathf.Clamp(_settings.ObstructionChecksPerSecond, 2, 15));
    }

    // =========================================================
    // Capture all scenery along a narrow camera/upper-body corridor, not just the first hit; entities are excluded by type/layer.
    private void CheckObstructions()
    {
        if (!_settings.ObstructionFadeEnabled) { RestoreAll(); return; }
        _hits.Clear();
        Vector3 to = _player.GlobalPosition + Vector3.Up * (_player.IsCrouching ? 0.8f : 1.1f);
        // In orthographic mode the screen ray can be parallel and offset from the camera's position.
        Vector2 screen = _camera.UnprojectPosition(to);
        Vector3 from = _camera.ProjectRayOrigin(screen);
        Vector3 direction = to - from;
        float length = direction.Length(), radius = Mathf.Clamp(_settings.ObstructionRadius, 0.1f, 1);
        if (length < 0.01f) return;
        direction /= length;
        Vector3 x = direction.Cross(Vector3.Up);
        if (x.LengthSquared() < 0.0001f) x = Vector3.Right; else x = x.Normalized();
        _shape.Radius = radius; _shape.Height = length + radius * 2;
        _query.Transform = new Transform3D(new Basis(x, direction, x.Cross(direction).Normalized()), (from + to) * 0.5f);
        foreach (var hit in _player.GetWorld3D().DirectSpaceState.IntersectShape(_query, 64))
        {
            var collider = hit["collider"].AsGodotObject();
            if (collider is Area3D area && area.HasMeta("camera_occluder")
                && area.GetMeta("camera_occluder").AsGodotObject() is CameraOccluder target && target.IsValid) _hits.Add(target);
        }
        foreach (var provider in _providers) provider.CollectObstructions(from, to, radius, _hits);
        foreach (var pair in _fades) pair.Value.Goal = _hits.Contains(pair.Key) ? Mathf.Clamp(_settings.ObstructedOpacity, 0, 1) : 1;
        foreach (var target in _hits)
            if (!_fades.ContainsKey(target)) _fades.Add(target, new Fade { Goal = Mathf.Clamp(_settings.ObstructedOpacity, 0, 1) });
        bool transitioning = false;
        foreach (var pair in _fades)
            if (!pair.Key.IsValid || pair.Value.Current != pair.Value.Goal) { transitioning = true; break; }
        SetProcess(transitioning);
    }

    // =========================================================
    // Only transitioning objects receive writes; settled faded objects stay tracked without frame callbacks.
    public override void _Process(double delta)
    {
        bool moving = false; _remove.Clear();
        float step = (float)delta / Mathf.Max(0.02f, _settings.ObstructionFadeSeconds);
        foreach (var pair in _fades)
        {
            if (!pair.Key.IsValid) { _remove.Add(pair.Key); continue; }
            var fade = pair.Value;
            float next = Mathf.MoveToward(fade.Current, fade.Goal, step);
            if (next != fade.Current) { fade.Current = next; pair.Key.SetCameraOpacity(next); }
            if (fade.Current != fade.Goal) moving = true;
            else if (fade.Goal == 1) _remove.Add(pair.Key);
        }
        foreach (var target in _remove) _fades.Remove(target);
        if (!moving) SetProcess(false);
    }

    // =========================================================
    // Explicit OFF/settings changes can wake a recheck without adding resource polling.
    public void ApplySettings() { if (_settings.ObstructionFadeEnabled) Invalidate(); else { _check.Stop(); RestoreAll(); } }

    private void RestoreAll()
    {
        foreach (var target in _fades.Keys) if (target.IsValid) target.SetCameraOpacity(1);
        _fades.Clear(); _hits.Clear(); SetProcess(false);
    }

    // =========================================================
    // Restore visible scenery and release every external callback when changing/unloading the world.
    public override void _ExitTree()
    {
        if (!_initialized) return;
        RestoreAll(); _check.Stop();
        if (IsInstanceValid(_camera)) _camera.ViewChanged -= Invalidate;
        if (IsInstanceValid(_player)) { _player.PositionChanged -= OnPlayerMoved; _player.StanceChanged -= OnStanceChanged; }
        CameraOccluder.RegistryChanged -= Invalidate;
        foreach (var provider in _providers) provider.OcclusionChanged -= Invalidate;
        _providers.Clear(); _initialized = false;
    }
}
