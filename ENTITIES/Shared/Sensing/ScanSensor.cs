// Registered candidates only. A capped timer checks range/cone before optional bounded line-of-sight rays.
using Godot;
using System;
using System.Collections.Generic;

public partial class ScanSensor : Node
{
    public event Action<Node3D>? TargetEntered;
    public event Action<Node3D>? TargetSeen;
    public event Action<Node3D>? TargetExited;
    private readonly List<Node3D> _candidates = new();
    private readonly HashSet<Node3D> _seen = new();
    private Node3D _socket = null!;
    private EntityCore _actor = null!;
    private ScanSensorSettings _settings = null!;
    private Timer _timer = null!;
    private PhysicsRayQueryParameters3D _ray = null!;
    private bool _scanning;
    private int _cursor;
    public bool IsScanning => _scanning;

    // =========================================================
    // No frame callbacks or scene-wide target searches.
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); }

    // =========================================================
    // Explicit references isolate sensing from visual mesh and world scene paths.
    public void Initialize(EntityCore actor, Node3D socket, ScanSensorSettings settings)
    {
        if (_timer != null) { StopScan(); _actor.ActiveChanged -= OnActorActiveChanged; }
        _candidates.Clear(); _cursor = 0;
        _actor = actor; _socket = socket; _settings = (ScanSensorSettings)settings.Duplicate(true);
        _ray = new PhysicsRayQueryParameters3D { CollisionMask = _settings.CollisionMask,
            Exclude = new Godot.Collections.Array<Rid> { actor.GetRid() } };
        if (_timer == null)
        {
            _timer = new Timer { ProcessCallback = Timer.TimerProcessCallback.Physics };
            AddChild(_timer);
        }
        _timer.Timeout -= Tick; _timer.Timeout += Tick;
        _actor.ActiveChanged += OnActorActiveChanged;
    }

    // =========================================================
    // Registration may wake an already-requested scan, but does not begin scanning by itself.
    public void RegisterCandidate(Node3D candidate)
    {
        if (_candidates.Contains(candidate)) return;
        _candidates.Add(candidate);
        if (_scanning && _timer != null && _timer.IsStopped()) _timer.Start(Mathf.Max(0.1f, _settings.IntervalSeconds));
    }

    // =========================================================
    // Unregister on target unload; no-target scans leave the timer dormant.
    public void UnregisterCandidate(Node3D candidate)
    {
        _candidates.Remove(candidate); Forget(candidate);
        if (_candidates.Count == 0) _timer.Stop();
    }

    // =========================================================
    // A visual-only robot can leave this component disabled or register no candidates.
    public void StartScan()
    {
        if (_scanning || !_actor.IsActive || !_settings.Enabled) return;
        _scanning = true;
        if (_candidates.Count > 0) _timer.Start(Mathf.Max(0.1f, _settings.IntervalSeconds));
    }

    // =========================================================
    // Stop drops all seen-state and the timer; repeated stop calls emit no duplicate exits.
    public void StopScan()
    {
        _scanning = false; _timer?.Stop();
        foreach (var target in _candidates) Forget(target);
        _seen.Clear();
    }

    // =========================================================
    // Check every candidate's cheap cone test; round-robin LOS prevents one candidate monopolizing rays.
    private void Tick()
    {
        if (!_scanning || !_actor.IsActive) { StopScan(); return; }
        for (int i = _candidates.Count - 1; i >= 0; i--)
        {
            var target = _candidates[i];
            if (GodotObject.IsInstanceValid(target) && target.IsInsideTree()) continue;
            Forget(target); _candidates.RemoveAt(i);
        }
        int count = _candidates.Count, rays = 0;
        if (count == 0) { _timer.Stop(); return; }
        int start = _cursor % count;
        Vector3 origin = _socket.GlobalPosition, forward = -_socket.GlobalBasis.Z.Normalized();
        float rangeSquared = Mathf.Max(0, _settings.Range) * Mathf.Max(0, _settings.Range);
        float threshold = Mathf.Cos(Mathf.DegToRad(Mathf.Clamp(_settings.FullAngleDegrees, 1, 170)) * 0.5f);
        int next = (start + 1) % count;
        for (int n = 0; n < count; n++)
        {
            var candidate = _candidates[(start + n) % count];
            if (!GodotObject.IsInstanceValid(candidate) || !candidate.IsInsideTree()) { Forget(candidate); continue; }
            Vector3 offset = candidate.GlobalPosition - origin;
            float squared = offset.LengthSquared();
            if (squared > rangeSquared || (squared > 0.001f && forward.Dot(offset / Mathf.Sqrt(squared)) < threshold))
            { Forget(candidate); continue; }
            if (_settings.RequireLineOfSight)
            {
                if (rays >= Mathf.Max(1, _settings.RaysPerTick)) continue;
                rays++; next = ((start + n) + 1) % count;
                _ray.From = origin; _ray.To = candidate.GlobalPosition;
                var hit = _socket.GetWorld3D().DirectSpaceState.IntersectRay(_ray);
                if (hit.Count > 0)
                {
                    var collider = hit["collider"].AsGodotObject() as Node;
                    if (collider != candidate && (collider == null || !candidate.IsAncestorOf(collider) && !collider.IsAncestorOf(candidate)))
                    { Forget(candidate); continue; }
                }
            }
            if (_seen.Add(candidate)) TargetEntered?.Invoke(candidate);
            TargetSeen?.Invoke(candidate);
        }
        _cursor = next;
    }

    // =========================================================
    // Removing a seen candidate reports once; invalid freed objects are not passed to event listeners.
    private void Forget(Node3D candidate)
    { if (_seen.Remove(candidate) && GodotObject.IsInstanceValid(candidate)) TargetExited?.Invoke(candidate); }

    // =========================================================
    // Logical scanning always stops when actor sleeps, independently of the visual effect.
    private void OnActorActiveChanged(bool active) { if (!active) StopScan(); }

    // =========================================================
    // Explicit subscription cleanup handles unload even when a scan was never started.
    public override void _ExitTree()
    {
        StopScan(); _candidates.Clear();
        if (_actor != null) _actor.ActiveChanged -= OnActorActiveChanged;
        if (_timer != null) _timer.Timeout -= Tick;
    }
}
