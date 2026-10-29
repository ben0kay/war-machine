// World-local bounded corpse registry: one coarse aging timer, active-only sinking and no global scans.
using Godot;
using System;
using System.Collections.Generic;

public partial class CorpseManager : Node
{
    private sealed class Entry
    {
        public CorpseBody Body = null!;
        public CorpseSettings Settings = null!;
        public ulong DeathAt, SinkAt;
        public Vector3 SinkOrigin;
    }
    private readonly List<Entry> _bodies = new();
    private Node3D _root = null!;
    private CorpseSettings _profile = null!;
    private Timer _aging = null!;
    private bool _exiting;
    public Node3D? Focus { get; set; }
    public int SpawnedCount { get; private set; }
    public int BudgetRetiredCount { get; private set; }
    public int StaticFallbackCount { get; private set; }
    public int RetainedCount => _bodies.Count;
    public int ActiveFallCount
    {
        get
        {
            int count = 0;
            foreach (Entry entry in _bodies)
                if (entry.Body.IsPhysicalFall && !entry.Body.IsSettled && !entry.Body.IsSinking && !entry.Body.IsQueuedForDeletion()) count++;
            return count;
        }
    }

    // =========================================================
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); }

    // =========================================================
    public void Initialize(Node3D root, CorpseSettings settings)
    {
        settings.Validate(); _root = root; _profile = (CorpseSettings)settings.Duplicate(true);
        _aging = new Timer { WaitTime = 1, ProcessCallback = Timer.TimerProcessCallback.Physics };
        AddChild(_aging); _aging.Timeout += Age;
    }

    // =========================================================
    // Oldest settled first under pressure. No backlog; only nearby deaths within the fall budget simulate.
    public bool TrySpawn(Node3D visual, Transform3D at, ICorpsePoseAdapter pose, Vector3 direction, Vector3 velocity,
        uint seed, CorpseSettings? profile = null, bool alignPivotToVisual = false, ulong deathAt = 0)
    {
        if (_exiting || !IsInstanceValid(_root) || !_root.IsInsideTree() || !IsInstanceValid(visual) || visual.IsQueuedForDeletion()) return false;
        var settings = (CorpseSettings)(profile ?? _profile).Duplicate(true); settings.Validate();
        if (!settings.Enabled || !at.Origin.IsFinite() || !at.Basis.X.IsFinite() || !at.Basis.Y.IsFinite() || !at.Basis.Z.IsFinite()) return false;
        while (_bodies.Count >= _profile.MaximumCorpses)
        {
            int oldest = 0;
            for (int i = 0; i < _bodies.Count; i++)
                if (_bodies[i].Body.IsSettled) { oldest = i; break; }
            CorpseBody retired = _bodies[oldest].Body;
            BudgetRetiredCount++;
            _bodies.RemoveAt(oldest); retired.Removed -= Remove; retired.Dismiss();
        }
        bool nearby = !IsInstanceValid(Focus) || at.Origin.DistanceSquaredTo(Focus!.GlobalPosition)
            <= _profile.PhysicalFallDistance * _profile.PhysicalFallDistance;
        bool simulate = nearby && ActiveFallCount < _profile.MaximumActiveFalls;
        if (!simulate) StaticFallbackCount++;
        var body = new CorpseBody { Name = $"Corpse{++SpawnedCount}" };
        _root.AddChild(body);
        var entry = new Entry { Body = body, Settings = settings, DeathAt = deathAt == 0 ? Time.GetTicksMsec() : deathAt };
        _bodies.Add(entry); body.Removed += Remove;
        try
        {
            body.Initialize(visual, at, pose, direction, velocity, seed, settings, alignPivotToVisual, simulate);
            if (_aging.IsStopped()) _aging.Start();
            return true;
        }
        catch (Exception error)
        { GD.PushWarning($"Corpse handoff failed: {error.Message}"); body.Dismiss(); return false; }
    }

    // =========================================================
    // One shared timer ages all registered bodies, even offscreen. No per-body TTL callback.
    private void Age()
    {
        ulong now = Time.GetTicksMsec();
        foreach (Entry entry in _bodies)
        {
            if (entry.Body.IsQueuedForDeletion() || entry.Body.IsSinking) continue;
            if (now - entry.DeathAt < (ulong)(entry.Settings.LifetimeSeconds * 1000)) continue;
            entry.SinkAt = now; entry.SinkOrigin = entry.Body.GlobalPosition;
            entry.Body.BeginSink(); SetProcess(true);
        }
    }

    // =========================================================
    // Only sinking visuals animate. Registry is capped; settled/empty worlds have no manager frame callback.
    public override void _Process(double delta)
    {
        bool sinking = false; ulong now = Time.GetTicksMsec();
        foreach (Entry entry in _bodies)
        {
            if (!entry.Body.IsSinking || entry.Body.IsQueuedForDeletion() || entry.SinkAt == 0) continue;
            float fraction = Mathf.Clamp((now - entry.SinkAt) / (entry.Settings.SinkSeconds * 1000f), 0, 1);
            entry.Body.GlobalPosition = entry.SinkOrigin - Vector3.Up * (entry.Settings.SinkDepth * fraction);
            if (fraction >= 1) entry.Body.QueueFree(); else sinking = true;
        }
        if (!sinking) SetProcess(false);
    }

    // =========================================================
    public void PrintDiagnostics()
    {
        int sinking = 0, settled = 0;
        foreach (Entry entry in _bodies)
        { if (entry.Body.IsSinking) sinking++; else if (entry.Body.IsSettled) settled++; }
        GD.Print($"[ZD3] retained={RetainedCount}/{_profile.MaximumCorpses} active_falls={ActiveFallCount}/{_profile.MaximumActiveFalls} settled={settled} sinking={sinking} static_fallbacks={StaticFallbackCount} budget_retired={BudgetRetiredCount}");
    }

    // =========================================================
    private void Remove(CorpseBody body)
    {
        body.Removed -= Remove;
        for (int i = _bodies.Count - 1; i >= 0; i--) if (_bodies[i].Body == body) _bodies.RemoveAt(i);
        if (!_exiting && _bodies.Count == 0) { _aging.Stop(); SetProcess(false); }
    }

    // =========================================================
    public override void _ExitTree()
    {
        _exiting = true;
        foreach (Entry entry in _bodies)
        { entry.Body.Removed -= Remove; if (IsInstanceValid(entry.Body)) entry.Body.Dismiss(); }
        _bodies.Clear(); Focus = null; _aging?.Stop();
    }
}
