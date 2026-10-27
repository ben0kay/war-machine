// Reusable hitscan firearm: cooldown, obstruction, hit dispatch and feedback, with no player/rifle dependency.
using Godot;

public partial class FirearmController : Node3D
{
    public event System.Action? ReadyToFire;
    [Export] public FirearmSettings Settings { get; set; } = null!;
    [Export] public Marker3D Muzzle { get; set; } = null!;
    public FirearmSettings RuntimeSettings { get; private set; } = null!;
    private Timer _cooldown = null!;
    private FirearmShotEffects _effects = null!;
    private AudioStreamPlayer3D _audio = null!;
    private PhysicsRayQueryParameters3D _query = null!;
    private CollisionObject3D? _holder;
    private bool _equipped;

    // =========================================================
    // Instantiate shared components once; idle guns never run physics/process callbacks.
    public override void _Ready()
    {
        SetProcess(false); SetPhysicsProcess(false);
        RuntimeSettings = Settings != null ? (FirearmSettings)Settings.Duplicate(true) : new FirearmSettings();
        _cooldown = new Timer { Name = "Cooldown", OneShot = true, ProcessCallback = Timer.TimerProcessCallback.Physics };
        AddChild(_cooldown); _cooldown.Timeout += OnCooldown;
        _effects = new FirearmShotEffects { Name = "ShotEffects" }; AddChild(_effects); _effects.Initialize(RuntimeSettings);
        _audio = new AudioStreamPlayer3D { Name = "ShotAudio", Stream = RuntimeSettings.ShotSound, VolumeDb = RuntimeSettings.ShotVolumeDb };
        AddChild(_audio);
        _query = new PhysicsRayQueryParameters3D { CollisionMask = RuntimeSettings.HitMask, HitFromInside = true };
        if (Muzzle == null) GD.PushError("Firearm scene requires a Muzzle marker.");
    }

    // =========================================================
    // Any actor can equip this scene; its own collider is excluded from the cached query.
    public void Equip(CollisionObject3D holder)
    { _holder = holder; _query.Exclude = new Godot.Collections.Array<Rid> { holder.GetRid() }; _equipped = true; }

    // =========================================================
    // Caller supplies aim and shoulder/probe origin during physics, independent of movement/input implementation.
    public bool TryFire(Vector3 aimPoint, Vector3 probeOrigin)
    {
        if (!_equipped || !IsInstanceValid(_holder) || Muzzle == null || !_cooldown.IsStopped()) return false;
        Vector3 start = Muzzle.GlobalPosition, direction = aimPoint - start;
        if (direction.LengthSquared() < 0.0001f) return false;
        direction = direction.Normalized();
        _query.CollisionMask = RuntimeSettings.HitMask;
        // Prevent a barrel sticking through cover from firing out of the other side.
        _query.From = probeOrigin; _query.To = start;
        var space = GetWorld3D().DirectSpaceState;
        var hit = space.IntersectRay(_query);
        if (hit.Count > 0) start = probeOrigin;
        else
        {
            _query.From = start; _query.To = start + direction * Mathf.Clamp(RuntimeSettings.Range, 0.1f, 1000);
            hit = space.IntersectRay(_query);
        }
        Vector3 end = hit.Count > 0 ? hit["position"].AsVector3() : _query.To;
        if (hit.Count > 0)
        {
            for (Node? node = hit["collider"].AsGodotObject() as Node; node != null; node = node.GetParent())
                if (node is IDamageReceiver receiver)
                { receiver.ReceiveHit(new DamagePacket(RuntimeSettings.Damage, RuntimeSettings.DamageKind, _holder, end, direction)); break; }
        }
        _effects.ShowShot(start, end);
        if (_audio.Stream != null) _audio.Play();
        _cooldown.Start(Mathf.Max(RuntimeSettings.ShotInterval, 0.03f));
        return true;
    }

    // =========================================================
    // A held-trigger adapter may request its next shot; no weapon-side target/input polling.
    private void OnCooldown() { if (_equipped) ReadyToFire?.Invoke(); }

    // =========================================================
    // Unequip, focus cancellation and scene teardown cannot leave feedback/audio ticking.
    public void CancelFeedback() { _effects.Stop(); _audio.Stop(); }

    // =========================================================
    public void Unequip() { _equipped = false; _cooldown.Stop(); CancelFeedback(); _holder = null; }
}
