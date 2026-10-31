// Removable FPS-only debug zombie spawner; no idle work and one physics-safe query per 0 press.
using Godot;
using System.Collections.Generic;

public partial class DebugZombieSpawner : Node
{
    [ExportGroup("Temporary FPS zombie testing")]
    [Export] public bool Enabled { get; set; } = true;
    [Export(PropertyHint.Range, "1,40,1")] public int MaxActiveDebugZombies { get; set; } = 20;
    [Export(PropertyHint.Range, "5,100,1")] public float AimRange { get; set; } = 70f;
    [Export(PropertyHint.Range, "0,1,0.01")] public float RunnerChance { get; set; } = 0.1f;

    private const string NormalPath = "res://ENTITIES/TYPES/Zombies/Zombie.tscn";
    private const string RunnerPath = "res://ENTITIES/TYPES/Zombies/Zombie_002/Zombie_002.tscn";
    private readonly RandomNumberGenerator _rng = new();
    private readonly List<ZombieController> _active = new();
    private PackedScene? _normal, _runner;
    private PlayerController? _player;
    private FirstPersonCamera? _camera;
    private GodotObject? _terrain;
    private Node3D? _group;
    private WorldAudio? _audio;
    private ZombieVisionDebug? _vision;
    private CorpseManager? _corpses;
    private bool _ready, _pending;

    // =========================================================
    public override void _Ready()
    {
        SetProcess(false); SetPhysicsProcess(false);
        SetProcessUnhandledKeyInput(Enabled && OS.IsDebugBuild());
        _rng.Randomize();
    }

    // =========================================================
    // Bind from FPS after WorldController has initialized the normal zombie services.
    public void Initialize(PlayerController player, FirstPersonCamera camera, GodotObject terrain,
        Node3D group, WorldAudio audio, ZombieVisionDebug vision, CorpseManager? corpses)
    {
        if (!Enabled || !OS.IsDebugBuild()) return;
        _player = player; _camera = camera; _terrain = terrain; _group = group;
        _audio = audio; _vision = vision; _corpses = corpses;
        _normal = GD.Load<PackedScene>(NormalPath);
        _runner = GD.Load<PackedScene>(RunnerPath);
        _ready = _normal != null && _runner != null;
        if (!_ready) GD.PushWarning("DBG-Z0: missing zombie scene.");
    }

    // =========================================================
    // GUI handles keys first; never spawn while F1 is open, keyboard focus is in GUI, or mouse is released.
    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (!_ready || !Enabled || @event is not InputEventKey key || !key.Pressed || key.Echo) return;
        if (key.PhysicalKeycode != Key.Key0 && key.PhysicalKeycode != Key.Kp0) return;
        if (_player == null || _player.IsDead || _player.GameplayInputBlocked
            || Input.MouseMode != Input.MouseModeEnum.Captured || !GetWindow().HasFocus()) return;
        _pending = true; SetPhysicsProcess(true);
        GetViewport().SetInputAsHandled();
    }

    // =========================================================
    // Physics query and placement run once at the next physics tick; no polling while idle.
    public override void _PhysicsProcess(double delta)
    {
        SetPhysicsProcess(false);
        if (!_pending || !_ready || !Enabled) return;
        _pending = false; SpawnOne();
    }

    // =========================================================
    private void SpawnOne()
    {
        if (!IsInstanceValid(_player) || !IsInstanceValid(_camera) || !IsInstanceValid(_group)
            || _terrain == null || _audio == null || _vision == null) return;
        for (int i = _active.Count - 1; i >= 0; i--)
            if (!IsInstanceValid(_active[i]) || _active[i].IsQueuedForDeletion()
                || !_active[i].IsActive || _active[i].Health <= 0) _active.RemoveAt(i);
        if (_active.Count >= Mathf.Clamp(MaxActiveDebugZombies, 1, 40))
        { GD.Print("DBG-Z0: maximum active test zombies reached."); return; }

        var space = _player!.GetWorld3D().DirectSpaceState;
        Vector3 origin = _camera!.GlobalPosition, direction = -_camera.GlobalBasis.Z;
        var aim = space.IntersectRay(new PhysicsRayQueryParameters3D {
            From = origin, To = origin + direction * Mathf.Clamp(AimRange, 5f, 100f),
            CollisionMask = 17u, CollideWithAreas = false,
            Exclude = new Godot.Collections.Array<Rid> { _player.GetRid() }
        });
        if (aim.Count == 0) { GD.Print("DBG-Z0: aim at the ground."); return; }
        Vector3 hit = aim["position"].AsVector3();
        float height = _terrain.Call("get_height", hit).AsSingle();
        if (!float.IsFinite(height) || Mathf.Abs(hit.Y - height) > 1.2f)
        { GD.Print("DBG-Z0: point is not terrain ground."); return; }
        Vector2 separation = new(hit.X - _player.GlobalPosition.X, hit.Z - _player.GlobalPosition.Z);
        if (separation.LengthSquared() < 9f)
        { GD.Print("DBG-Z0: aim at least 3m away."); return; }
        var ground = space.IntersectRay(new PhysicsRayQueryParameters3D {
            From = new Vector3(hit.X, height + 2f, hit.Z),
            To = new Vector3(hit.X, height - 1.5f, hit.Z),
            CollisionMask = 1u, CollideWithAreas = false
        });
        if (ground.Count == 0 || ground["normal"].AsVector3().Y < 0.75f)
        { GD.Print("DBG-Z0: no walkable ground here."); return; }

        bool fast = _rng.Randf() < Mathf.Clamp(RunnerChance, 0f, 1f);
        var scene = fast ? _runner : _normal;
        if (scene == null) return;
        var zombie = scene.Instantiate<ZombieController>();
        _group!.AddChild(zombie);
        zombie.GlobalPosition = ground["position"].AsVector3() + Vector3.Up * 0.06f;
        var shape = zombie.GetNode<CollisionShape3D>("CollisionShape3D");
        var overlaps = space.IntersectShape(new PhysicsShapeQueryParameters3D {
            Shape = shape.Shape, Transform = shape.GlobalTransform,
            CollisionMask = zombie.CollisionMask | 17u, CollideWithBodies = true,
            CollideWithAreas = false, Exclude = new Godot.Collections.Array<Rid> { zombie.GetRid() }
        }, 1);
        if (overlaps.Count > 0)
        { GD.Print("DBG-Z0: spawn position blocked."); zombie.QueueFree(); return; }

        zombie.CorpseOwner = _corpses;
        zombie.BindHearing(_audio.SoundEvents.Hearing);
        uint seed = _rng.Randi();
        zombie.InitializeTarget(_player, seed, point => _terrain.Call("get_height", point).AsSingle());
        _vision.Register(zombie);
        _active.Add(zombie);
        GD.Print($"DBG-Z0: spawned {(fast ? "fast" : "normal")} zombie at {zombie.GlobalPosition} ({_active.Count} live tests).");
    }

    // =========================================================
    public override void _ExitTree()
    { _pending = _ready = false; _active.Clear(); }
}
