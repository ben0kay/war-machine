// Shared HUD overlay: current/future HealthComponents automatically show while below full HP.
// Only injured actors are projected; UI controls are pooled and capped.
using Godot;
using System.Collections.Generic;

public partial class HealthBarOverlay : Control
{
    private sealed class Entry
    {
        public readonly HealthComponent Health;
        public readonly Node3D Actor;
        public HealthBar? Bar;
        public bool Wounded;
        public Entry(HealthComponent health, Node3D actor) { Health = health; Actor = actor; }
    }

    private readonly Dictionary<HealthComponent, Entry> _registered = new();
    private readonly List<Entry> _wounded = new();
    private readonly List<HealthBar> _pool = new();
    private FeedbackSettings _settings = null!;
    private PackedScene _barPrefab = null!;
    private Camera3D _camera = null!;
    private PlayerController _player = null!;
    private float _accumulator;
    private bool _initialized;

    // =========================================================
    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;
        SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    // Register existing actors once, then observe new health components on scene-tree additions.
    public void Initialize(PlayerController player, Camera3D camera)
    {
        if (_initialized) return;
        _player = player; _camera = camera;
        _settings = (FeedbackSettings)GD.Load<FeedbackSettings>("res://FEEDBACK/Configuration/FeedbackMaster.tres").Duplicate(true);
        _barPrefab = GD.Load<PackedScene>("res://FEEDBACK/Bars/Health/HealthBar.tscn");
        _initialized = true;
        GetTree().NodeAdded += OnNodeAdded;
        GetTree().NodeRemoved += OnNodeRemoved;
        foreach (Node node in GetTree().GetNodesInGroup(HealthComponent.FeedbackGroup))
            if (node is HealthComponent health) Register(health);
    }

    // =========================================================
    private void OnNodeAdded(Node node)
    { if (node is HealthComponent health) Register(health); }

    // =========================================================
    private void OnNodeRemoved(Node node)
    { if (node is HealthComponent health) Unregister(health); }

    // =========================================================
    private void Register(HealthComponent health)
    {
        if (!_initialized || _registered.ContainsKey(health) || !IsInstanceValid(health)) return;
        Node3D? actor = FindOwner(health);
        if (actor == null) return;
        var entry = new Entry(health, actor);
        _registered.Add(health, entry);
        health.HealthChanged += OnHealthChanged;
        Refresh(entry);
    }

    // =========================================================
    // Prefer the owning collision actor; support Health nested in future Player/Stats.
    private static Node3D? FindOwner(Node health)
    {
        Node3D? fallback = null;
        for (Node? node = health.GetParent(); node != null; node = node.GetParent())
        {
            if (node is CollisionObject3D body) return body;
            if (node is Node3D spatial && fallback == null) fallback = spatial;
        }
        return fallback;
    }

    // =========================================================
    private void OnHealthChanged(HealthComponent health)
    { if (_registered.TryGetValue(health, out Entry? entry)) Refresh(entry); }

    // =========================================================
    // The exact rule: visible-eligible while alive AND current HP is below max HP.
    private void Refresh(Entry entry)
    {
        bool wounded = IsInstanceValid(entry.Health) && !entry.Health.IsDead
            && entry.Health.MaxHealth > 0f && entry.Health.CurrentHealth < entry.Health.MaxHealth;
        if (wounded)
        {
            if (!entry.Wounded) { entry.Wounded = true; _wounded.Add(entry); SetProcess(true); }
            entry.Bar?.SetHealth(entry.Health.CurrentHealth, entry.Health.MaxHealth);
        }
        else if (entry.Wounded)
        {
            entry.Wounded = false; _wounded.Remove(entry); ReleaseBar(entry);
            if (_wounded.Count == 0) { SetProcess(false); _accumulator = 0; }
        }
    }

    // =========================================================
    // Project at bounded frequency only while any actor is injured. Full HP means zero callbacks.
    public override void _Process(double delta)
    {
        if (!_initialized || _wounded.Count == 0) { SetProcess(false); return; }
        _accumulator += (float)delta;
        float interval = 1f / Mathf.Clamp(_settings.ProjectionUpdatesPerSecond, 5f, 60f);
        if (_accumulator < interval) return;
        _accumulator = 0f;
        Vector2 viewport = GetViewport().GetVisibleRect().Size;
        int capacity = Mathf.Clamp(_settings.MaxVisibleBars, 1, 128), shown = 0;

        for (int i = _wounded.Count - 1; i >= 0; i--)
        {
            Entry entry = _wounded[i];
            if (!IsInstanceValid(entry.Health) || !IsInstanceValid(entry.Actor) || entry.Actor.IsQueuedForDeletion()
                || entry.Health.IsDead || entry.Health.CurrentHealth >= entry.Health.MaxHealth)
            { Refresh(entry); continue; }

            bool playerBar = entry.Actor == _player;
            float width = Mathf.Clamp(playerBar ? _settings.PlayerBarWidth : _settings.WorldBarWidth, 24f, 320f);
            float height = Mathf.Clamp(playerBar ? _settings.PlayerBarHeight : _settings.WorldBarHeight, 4f, 28f);
            Vector2 pos;
            bool inView = true;
            if (playerBar)
            {
                float margin = Mathf.Clamp(_settings.PlayerMargin, 0f, 120f);
                pos = new Vector2(margin, viewport.Y - height - margin);
            }
            else
            {
                Vector3 anchor = entry.Actor.GlobalPosition + Vector3.Up * Mathf.Clamp(_settings.WorldAnchorHeight, 0.5f, 5f);
                inView = entry.Actor.IsVisibleInTree() && !_camera.IsPositionBehind(anchor);
                pos = inView ? _camera.UnprojectPosition(anchor) - new Vector2(width * 0.5f, height) : Vector2.Zero;
                inView = inView && pos.X >= 0f && pos.Y >= 0f
                    && pos.X + width <= viewport.X && pos.Y + height <= viewport.Y;
            }

            if (!inView || shown >= capacity) { ReleaseBar(entry); continue; }
            if (entry.Bar == null)
            {
                entry.Bar = AcquireBar();
                entry.Bar.SetHealth(entry.Health.CurrentHealth, entry.Health.MaxHealth);
            }
            entry.Bar.Size = new Vector2(width, height);
            entry.Bar.Position = pos;
            entry.Bar.Visible = true;
            shown++;
        }
    }

    // =========================================================
    // Recycle controls: at most MaxVisibleBars instances, even during a large horde fight.
    private HealthBar AcquireBar()
    {
        if (_pool.Count > 0)
        {
            int last = _pool.Count - 1;
            HealthBar bar = _pool[last]; _pool.RemoveAt(last);
            return bar;
        }
        HealthBar created = _barPrefab.Instantiate<HealthBar>();
        AddChild(created); return created;
    }

    // =========================================================
    private void ReleaseBar(Entry entry)
    {
        if (entry.Bar == null) return;
        entry.Bar.Visible = false;
        _pool.Add(entry.Bar);
        entry.Bar = null;
    }

    // =========================================================
    private void Unregister(HealthComponent health)
    {
        if (!_registered.Remove(health, out Entry? entry)) return;
        if (IsInstanceValid(health)) health.HealthChanged -= OnHealthChanged;
        if (entry.Wounded) _wounded.Remove(entry);
        ReleaseBar(entry);
        if (_wounded.Count == 0) { SetProcess(false); _accumulator = 0; }
    }

    // =========================================================
    // Release shared tree subscriptions and actor events on unload/replacing the view.
    public override void _ExitTree()
    {
        if (!_initialized) return;
        GetTree().NodeAdded -= OnNodeAdded;
        GetTree().NodeRemoved -= OnNodeRemoved;
        foreach (HealthComponent health in _registered.Keys)
            if (IsInstanceValid(health)) health.HealthChanged -= OnHealthChanged;
        _registered.Clear(); _wounded.Clear(); _pool.Clear(); _initialized = false;
        SetProcess(false);
    }
}
