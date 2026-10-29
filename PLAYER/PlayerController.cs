// Player identity and lifecycle coordinator; input, locomotion, stance and rig posing are plain private collaborators.
using Godot;

public partial class PlayerController : CharacterBody3D, IPerceptionTarget, IDamageReceiver, ICombatLifeState, ICorpseSlowable
{
    private readonly CorpseMovementModifiers _corpseMovement = new();
    public bool CorpseSlowdownEnabled => _settings != null && _settings.CorpseSlowdownEnabled && !IsDead;
    // =========================================================
    public void SetCorpseSlowdown(ulong id, float multiplier, float floor) => _corpseMovement.Set(id, multiplier, floor);
    // =========================================================
    public void RemoveCorpseSlowdown(ulong id) => _corpseMovement.Remove(id);
    public enum MovementStance { Standing, Crouching, Prone }
    public PlayerVitals Vitals { get; private set; } = null!;
    public bool IsDead => Vitals != null && Vitals.IsDead;
    public bool GameplayInputBlocked { get; private set; }
    public PlayerInteraction? Interaction { get; internal set; }
    public bool InteractionModeActive { get; private set; }
    public event System.Action<bool>? InteractionModeChanged;

    // =========================================================
    // Interaction focus cancels weapon triggers without blocking movement or camera look.
    internal void SetInteractionMode(bool active)
    {
        if (InteractionModeActive == active) return;
        InteractionModeActive = active; InteractionModeChanged?.Invoke(active);
    }
    public event System.Action<bool>? GameplayInputBlockChanged;
    public event System.Action<Vector3>? PositionChanged;
    public event System.Action<bool>? StanceChanged;
    public event System.Action<MovementStance>? MovementStanceChanged;
    public event System.Action? WeaponPoseChanged;
    public MovementStance CurrentStance => _stance.Current;
    public bool IsCrouching => CurrentStance == MovementStance.Crouching;
    public bool IsProne => CurrentStance == MovementStance.Prone;
    public float LeanAmount => _lean.Amount; // -1 left, +1 right; presentation only.
    public Marker3D WeaponMount { get; private set; } = null!;
    public float CapsuleHeight => _stance._capsule.Height;
    public Vector3 PerceptionHead => GlobalPosition + Vector3.Up * Mathf.Max(0.1f, CapsuleHeight - 0.1f);
    public Vector3 PerceptionTorso => GlobalPosition + Vector3.Up * (CapsuleHeight * 0.6f);
    public Vector3 WeaponProbeOrigin => _animation._upperBody.GlobalPosition + new Vector3(
        _settings.WeaponProbeOffset.X, Mathf.Lerp(_settings.WeaponProbeOffset.Y, 0.10f, _stance._prone), _settings.WeaponProbeOffset.Z);
    protected Node3D VisualRoot => _animation._art;
    private PlayerSettings _settings = null!;
    private bool _initialized;
    private readonly PlayerInput _input;
    private readonly PlayerMovement _movement;
    private readonly PlayerStance _stance;
    private readonly PlayerLean _lean;
    private readonly PlayerAnimation _animation;
    private readonly PlayerWeaponPose _weapon;

    // =========================================================
    // Constructors allocate only plain helpers; scene access remains in the explicit initialization path.
    public PlayerController()
    {
        _input = new PlayerInput(this); _movement = new PlayerMovement(this); _stance = new PlayerStance(this);
        _lean = new PlayerLean(this);
        _animation = new PlayerAnimation(this); _weapon = new PlayerWeaponPose(this);
    }

    // =========================================================
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); }

    // =========================================================
    // Preserve one-time rig/capsule/vitals creation and the original reset/reinitialization order.
    public virtual void Initialize(PlayerSettings settings, Camera3D camera)
    {
        _settings = settings; _movement.BindCamera(camera);
        FloorSnapLength = 0.5f; FloorMaxAngle = Mathf.DegToRad(46);
        if (!_initialized) { _animation.Initialize(); _stance.Initialize(); }
        ResetPhysicsInterpolation();
        if (Vitals == null)
        {
            Vitals = GetNode<PlayerVitals>("Vitals"); Vitals.Initialize(_settings.Vitals);
            Vitals.Died += OnDied;
        }
        _initialized = true; _input.ClearRequests(); _stance.Reset(); _lean.Reset();
        Velocity = Vector3.Zero; SetPhysicsProcess(true);
    }

    // =========================================================
    // Live-world menus/death gate the actor; the rest of the world keeps running.
    public void SetGameplayInputBlocked(bool blocked)
    {
        blocked |= IsDead;
        if (GameplayInputBlocked == blocked) return;
        GameplayInputBlocked = blocked; _input._toggleCrouch = _input._toggleProne = false;
        if (blocked) { _input._jumpRequested = false; _lean.ClearHeld(); }
        if (blocked && Vitals != null) Vitals.Stamina.RecordSprint(false, 0);
        if (blocked) Velocity = new Vector3(0, Velocity.Y, 0);
        SetPhysicsProcess(true); GameplayInputBlockChanged?.Invoke(blocked);
    }

    // =========================================================
    public void ReceiveHit(DamagePacket hit)
    { if (_initialized && !IsQueuedForDeletion() && !IsDead) Vitals.ReceiveHit(hit); }

    // =========================================================
    private void OnDied()
    { SetGameplayInputBlocked(true); Input.MouseMode = Input.MouseModeEnum.Visible; }

    // =========================================================
    // Releases must be observed even when the GUI handles the key-up event.
    public override void _Input(InputEvent input) => _input.HandleLeanRelease(input);

    // =========================================================
    public override void _UnhandledInput(InputEvent input) => _input.Handle(input);

    // =========================================================
    public override void _Notification(int what)
    { if (_initialized) _input.Notify(what); }

    // =========================================================
    // The only shared physics driver; FPS still records its pose after this callback returns.
    public override void _PhysicsProcess(double delta) => _movement.Advance(delta);

    // =========================================================
    // Keep all virtual hooks on the player so existing FPS overrides dispatch exactly as before.
    protected virtual bool CanAcceptJumpInput() => true;
    protected virtual bool CanAcceptLeanInput() => true;
    protected virtual bool IsMovementKey(Key key) => PlayerInput.MovementKey(key);
    protected virtual Vector2 ReadMovementInput() => PlayerInput.MovementInput();
    protected virtual Vector3 GetMovementDirection(Vector2 keys) => _movement.Direction(keys);
    protected virtual Vector3 UpdateHorizontalVelocity(Vector3 current, Vector3 target, float dt)
        => _movement.HorizontalVelocity(current, target, dt);

    // =========================================================
    // Existing equipment API stays on the actor; helpers own only presentation state.
    public void SetWeaponPose(bool enabled) => _weapon.SetWeaponPose(enabled);
    public void SetWeaponAim(Vector3 target) => _weapon.SetWeaponAim(target);
    public void SetWeaponRecoilPose(float normalizedKick) => _weapon.SetWeaponRecoilPose(normalizedKick);
    public void SetShoveGrip(Node3D? weapon, float weight) => _weapon.SetShoveGrip(weapon, weight);
    // Top-down visual-only bat integration. FPS keeps its separate camera-mounted hands and swing.
    public void SetBatWeapon(Node3D? bat) => _weapon.SetBatWeapon(bat);
    public void SetBatSwingPose(float amount) => _weapon.SetBatSwing(amount);

    // =========================================================
    // Events are raised by the coordinator, preserving their existing order and public identity.
    private void RaisePositionChanged() => PositionChanged?.Invoke(GlobalPosition);
    private void RaiseWeaponPoseChanged() => WeaponPoseChanged?.Invoke();
    private void RaiseStanceChanged(MovementStance next)
    { StanceChanged?.Invoke(IsCrouching); MovementStanceChanged?.Invoke(next); }

    // =========================================================
    public override void _ExitTree()
    {
        _movement.Shutdown();
        if (IsInstanceValid(Vitals)) Vitals.Died -= OnDied;
    }
}
