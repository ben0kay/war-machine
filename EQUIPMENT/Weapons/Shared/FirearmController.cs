// Reusable hitscan firearm: cooldown, obstruction, hit dispatch and feedback, with no player/rifle dependency.
using Godot;

public partial class FirearmController : Node3D
{
    public event System.Action? ReadyToFire;
    // RC1/RC2 view adapters subscribe for the same accepted-shot visual recoil event.
    public event System.Action? ShotAccepted;
    [Export] public FirearmSettings Settings { get; set; } = null!;
    [Export] public Marker3D Muzzle { get; set; } = null!;
    public FirearmSettings RuntimeSettings { get; private set; } = null!;
    private Timer _cooldown = null!;
    private FirearmShotEffects _effects = null!;
    private AudioStreamPlayer3D? _legacyAudio;
    private WorldAudio? _audioService;
    private PhysicsRayQueryParameters3D _query = null!;
    private CollisionObject3D? _holder;
    private readonly RandomNumberGenerator _shotRandom = new();
    private bool _equipped;
    private FirearmRecoil _recoil;
    public Vector2 RecoilOffsetDegrees => _recoil.OffsetDegrees;

    // =========================================================
    // Instantiate shared components once; idle guns never run physics/process callbacks.
    public override void _Ready()
    {
        SetProcess(false); SetPhysicsProcess(false);
        RuntimeSettings = Settings != null ? (FirearmSettings)Settings.Duplicate(true) : new FirearmSettings();
        _shotRandom.Randomize();
        _cooldown = new Timer { Name = "Cooldown", OneShot = true, ProcessCallback = Timer.TimerProcessCallback.Physics };
        AddChild(_cooldown); _cooldown.Timeout += OnCooldown;
        _effects = new FirearmShotEffects { Name = "ShotEffects" }; AddChild(_effects); _effects.Initialize(RuntimeSettings, Muzzle);
        if (RuntimeSettings.ShotCue == null && RuntimeSettings.ShotSound != null)
        {
            _legacyAudio = new AudioStreamPlayer3D { Name = "LegacyShotAudio", Stream = RuntimeSettings.ShotSound, VolumeDb = RuntimeSettings.ShotVolumeDb };
            AddChild(_legacyAudio);
        }
        _query = new PhysicsRayQueryParameters3D { CollisionMask = RuntimeSettings.HitMask, HitFromInside = true };
        if (Muzzle == null) GD.PushError("Firearm scene requires a Muzzle marker.");
    }

    // =========================================================
    // Any actor can equip this scene; its own collider is excluded from the cached query.
    public void Equip(CollisionObject3D holder, WorldAudio? audio = null)
    { ResetRecoil(); _audioService = audio; _holder = holder; _query.Exclude = new Godot.Collections.Array<Rid> { holder.GetRid() }; _equipped = true; }

    // =========================================================
    // Caller supplies aim and shoulder/probe origin during physics, independent of movement/input implementation.
    public bool TryFire(Vector3 aimPoint, Vector3 probeOrigin)
    { return TryFire(aimPoint, probeOrigin, Vector3.Zero); }

    // =========================================================
    // Third argument is camera-space up for RC1/RC2 real 3D shot climb; legacy calls remain unchanged.
    public bool TryFire(Vector3 aimPoint, Vector3 probeOrigin, Vector3 recoilScreenUp)
    {
        if (!_equipped || !IsInstanceValid(_holder) || Muzzle == null || !_cooldown.IsStopped()) return false;
        Vector3 start = Muzzle.GlobalPosition, direction = aimPoint - start;
        if (!float.IsFinite(direction.X) || !float.IsFinite(direction.Y) || !float.IsFinite(direction.Z)
            || direction.LengthSquared() < 0.0001f) return false;
        // =========================================================
        // Previous accepted shots' accumulated 3D recoil precedes ACC0 random spread.
        // Both top-down and FPS supply view-up to preserve true world-space bullet intersection.
        direction = _recoil.Apply(direction, recoilScreenUp);
        direction = ShotSpread.Apply(direction, RuntimeSettings.BaseSpreadDegrees, _shotRandom);
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
            var packet = new DamagePacket(RuntimeSettings.Damage, RuntimeSettings.DamageKind, _holder, end, direction);
            var impact = new CombatImpact(_holder, end, end - _holder!.GlobalPosition,
                CombatImpactKind.Bullet, RuntimeSettings.BulletImpactStrength, RuntimeSettings.BulletStaggerSeconds);
            HitTargetResolver.Dispatch(hit["collider"].AsGodotObject() as Node, _holder, packet, impact);
        }
        _effects.ShowShot(start, end);
        if (RuntimeSettings.ShotCue != null)
        {
            if (IsInstanceValid(_audioService)) _audioService!.PlayOneShot(RuntimeSettings.ShotCue, Muzzle.GlobalPosition, GetInstanceId(), RuntimeSettings.ShotVolumeDb);
        }
        else _legacyAudio?.Play();
        _cooldown.Start(Mathf.Max(RuntimeSettings.ShotInterval, 0.03f));
        // Current shot is already resolved; this impulse affects subsequent accepted shots.
        _recoil.OnAcceptedShot(RuntimeSettings);
        if (_recoil.IsActive) SetProcess(true);
        ShotAccepted?.Invoke();
        return true;
    }

    // =========================================================
    // Only wake for accumulated recoil/recovery; the firearm still has no idle frame loop.
    public override void _Process(double delta)
    {
        if (!_equipped) { ResetRecoil(); return; }
        _recoil.Advance((float)delta, RuntimeSettings);
        if (!_recoil.IsActive) SetProcess(false);
    }

    // =========================================================
    // RC0 recoil state is a separate gameplay accumulator, not a visual muzzle transform.
    public void ResetRecoil() { _recoil.Reset(); SetProcess(false); }

    // =========================================================
    // A held-trigger adapter may request its next shot; no weapon-side target/input polling.
    private void OnCooldown() { if (_equipped) ReadyToFire?.Invoke(); }

    // =========================================================
    // Unequip, focus cancellation and scene teardown cannot leave feedback/audio ticking.
    public void CancelFeedback()
    { ResetRecoil(); _effects.Stop(); _legacyAudio?.Stop(); if (IsInstanceValid(_audioService)) _audioService!.StopEmitter(GetInstanceId()); }

    // =========================================================
    public void Unequip() { _equipped = false; _cooldown.Stop(); CancelFeedback(); _holder = null; }
}
