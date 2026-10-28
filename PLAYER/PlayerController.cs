// Active 1.8m player: physical walking and one visual gait driver, dormant when grounded/idle.
using Godot;

public partial class PlayerController : CharacterBody3D, IPerceptionTarget, IDamageReceiver
{
    public PlayerVitals Vitals { get; private set; } = null!;
    public bool IsDead => Vitals != null && Vitals.IsDead;
    public bool GameplayInputBlocked { get; private set; }
    public event System.Action<bool>? GameplayInputBlockChanged;

    // =========================================================
    // A live-world menu stops only this actor's controls; gravity and every other world owner keep running.
    public void SetGameplayInputBlocked(bool blocked)
    {
        blocked |= IsDead; // Closing a backpack cannot release the permanent death gate.
        if (GameplayInputBlocked == blocked) return;
        GameplayInputBlocked = blocked; _toggleCrouch = _toggleProne = false;
        if (blocked) _jumpRequested = false;
        if (blocked) Velocity = new Vector3(0, Velocity.Y, 0);
        SetPhysicsProcess(true); GameplayInputBlockChanged?.Invoke(blocked);
    }

    public event System.Action<Vector3>? PositionChanged;
    private PlayerSettings _settings = null!;
    private Node3D _art = null!, _leftArm = null!, _rightArm = null!, _leftLeg = null!, _rightLeg = null!;
    private Node3D _leftFoot = null!, _rightFoot = null!;
    private Node3D _leftKnee = null!, _rightKnee = null!, _upperBody = null!;
    private Node3D _shoulderGirdle = null!, _leftElbow = null!, _rightElbow = null!;
    private CollisionShape3D _collider = null!;
    private CapsuleShape3D _capsule = null!;
    private PhysicsShapeQueryParameters3D _standingQuery = null!, _crouchingQuery = null!;
    public enum MovementStance { Standing, Crouching, Prone }
    public MovementStance CurrentStance { get; private set; } = MovementStance.Standing;
    public bool IsCrouching => CurrentStance == MovementStance.Crouching;
    public bool IsProne => CurrentStance == MovementStance.Prone;
    public event System.Action<bool>? StanceChanged; // Historical crouch-only observer.
    public event System.Action<MovementStance>? MovementStanceChanged;
    public Marker3D WeaponMount { get; private set; } = null!;
    public Vector3 WeaponProbeOrigin => _upperBody.GlobalPosition + new Vector3(
        _settings.WeaponProbeOffset.X,
        Mathf.Lerp(_settings.WeaponProbeOffset.Y, 0.10f, _prone),
        _settings.WeaponProbeOffset.Z);
    public event System.Action? WeaponPoseChanged;
    private bool _weaponPose;
    private Node3D? _shoveWeapon;
    private float _shoveGrip;
    private float _recoilArmAngle, _recoilArmApplied;
    private float _crouch, _prone;
    private bool _toggleCrouch, _toggleProne, _jumpRequested;
    private MovementStance _stanceBeforeProne = MovementStance.Standing;
    private Vector3 _screenRight, _screenForward;
    private Camera3D? _movementCamera;
    private float _phase;
    private bool _initialized, _reportMovementCollision;

    // =========================================================
    // The test world places the player before enabling physics; no spawn-time fall without terrain data.
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); }

    // =========================================================
    // Cache screen-relative movement axes and subscribe to later camera rotation/tilt changes.
    public virtual void Initialize(PlayerSettings settings, Camera3D camera)
    {
        if (_movementCamera is Camera previous && IsInstanceValid(previous))
            previous.SettingsChanged -= RefreshMovementAxes;
        _settings = settings; _movementCamera = camera;
        RefreshMovementAxes();
        if (camera is Camera view) view.SettingsChanged += RefreshMovementAxes;
        FloorSnapLength = 0.5f; FloorMaxAngle = Mathf.DegToRad(46);
        if (!_initialized)
        {
            var mesh = new PlayerMeshBuilder(this, _settings);
            _art = mesh.Art; _upperBody = mesh.UpperBody; _shoulderGirdle = mesh.ShoulderGirdle;
            _leftArm = mesh.LeftArm; _rightArm = mesh.RightArm;
            _leftElbow = mesh.LeftElbow; _rightElbow = mesh.RightElbow;
            _leftLeg = mesh.LeftLeg; _rightLeg = mesh.RightLeg;
            _leftKnee = mesh.LeftKnee; _rightKnee = mesh.RightKnee;
            _leftFoot = mesh.LeftFoot; _rightFoot = mesh.RightFoot; WeaponMount = mesh.WeaponMount;
            _collider = GetNode<CollisionShape3D>("CollisionShape3D");
            _capsule = (CapsuleShape3D)_collider.Shape.Duplicate(); _collider.Shape = _capsule;
            var standing = (CapsuleShape3D)_capsule.Duplicate(); standing.Height = 1.8f;
            var crouching = (CapsuleShape3D)_capsule.Duplicate(); crouching.Height = 1.4f;
            var exclusion = new Godot.Collections.Array<Rid> { GetRid() };
            _standingQuery = new PhysicsShapeQueryParameters3D { Shape = standing, CollisionMask = CollisionMask,
                Exclude = exclusion };
            _crouchingQuery = new PhysicsShapeQueryParameters3D { Shape = crouching, CollisionMask = CollisionMask,
                Exclude = exclusion };
        }
        ResetPhysicsInterpolation(); // Do not blend spawn placement/generated rig from the authored origin.
        if (Vitals == null)
        {
            Vitals = GetNode<PlayerVitals>("Vitals"); Vitals.Initialize(_settings.Vitals);
            Vitals.Died += OnDied;
        }
        _initialized = true; _jumpRequested = _toggleCrouch = _toggleProne = false;
        CurrentStance = MovementStance.Standing; _stanceBeforeProne = MovementStance.Standing;
        _crouch = _prone = 0f; _capsule.Height = 1.8f; _collider.Position = Vector3.Up * 0.9f;
        Velocity = Vector3.Zero; SetPhysicsProcess(true);
    }

    // =========================================================
    // The movement capsule has no anatomy metadata: the coordinator deliberately falls back to Torso.
    public void ReceiveHit(DamagePacket hit)
    { if (_initialized && !IsQueuedForDeletion() && !IsDead) Vitals.ReceiveHit(hit); }

    // =========================================================
    // Keep the actor/camera alive. Existing input-block subscribers cancel both views' attacks immediately.
    private void OnDied()
    { SetGameplayInputBlocked(true); Input.MouseMode = Input.MouseModeEnum.Visible; }

    // =========================================================
    // Refresh only on camera settings changes, not in the player physics loop.
    // Project onto XZ so left/right/up/down stay aligned with the rotated on-screen view.
    private void RefreshMovementAxes()
    {
        if (!IsInstanceValid(_movementCamera)) return;
        _screenRight = _movementCamera!.GlobalBasis.X;
        _screenRight.Y = 0; _screenRight = _screenRight.Normalized();
        _screenForward = -_movementCamera.GlobalBasis.Z;
        _screenForward.Y = 0; _screenForward = _screenForward.Normalized();
    }

    // =========================================================
    // Right-click requests one grounded jump; physics owns the impulse and GUI receives input first.
    // Movement keys still wake the shared physics driver, never an always-on idle callback.
    public override void _UnhandledInput(InputEvent input)
    {
        if (!_initialized || GameplayInputBlocked) return;
        if (input is InputEventMouseButton mouse && mouse.ButtonIndex == MouseButton.Right && mouse.Pressed)
        {
            if (CanAcceptJumpInput() && GetWindow().HasFocus() && IsOnFloor() && CurrentStance == MovementStance.Standing
                && GetViewport().GuiGetHoveredControl() == null && GetViewport().GuiGetFocusOwner() == null)
            {
                _jumpRequested = true; SetPhysicsProcess(true);
                GetViewport().SetInputAsHandled();
            }
            return;
        }
        if (input is not InputEventKey key || key.Echo) return;
        if (key.PhysicalKeycode == Key.F10 && key.Pressed)
        {
            _reportMovementCollision = true; SetPhysicsProcess(true);
            GetViewport().SetInputAsHandled(); return;
        }
        if (key.PhysicalKeycode == Key.Ctrl && key.Location == KeyLocation.Right && key.Pressed)
        {
            _toggleProne = true; _toggleCrouch = false;
            SetPhysicsProcess(true); GetViewport().SetInputAsHandled(); return;
        }
        if ((key.PhysicalKeycode == Key.C ||
             key.PhysicalKeycode == Key.Ctrl && key.Location == KeyLocation.Left) && key.Pressed)
        {
            _toggleCrouch = true; _toggleProne = false;
            SetPhysicsProcess(true); GetViewport().SetInputAsHandled(); return;
        }
        if (IsMovementKey(key.PhysicalKeycode))
            SetPhysicsProcess(true);
    }

    // =========================================================
    // Ground contact sleeps again after focus returns; no held direction is allowed while the window is unfocused.
    public override void _Notification(int what)
    {
        if (!_initialized) return;
        if (what == NotificationApplicationFocusOut) _jumpRequested = false;
        if (what == NotificationApplicationFocusOut || what == NotificationApplicationFocusIn)
            SetPhysicsProcess(true);
    }

    // =========================================================
    // Physics root owns movement. Stance requests and one-time clearance tests run only on input.
    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        if (_toggleProne)
        {
            _toggleProne = false;
            if (IsProne)
            {
                // Return to the original stance if there is headroom; in low spaces try crouch.
                if (!TrySetStance(_stanceBeforeProne) && _stanceBeforeProne == MovementStance.Standing)
                    TrySetStance(MovementStance.Crouching);
            }
            else if (IsOnFloor())
            {
                _stanceBeforeProne = CurrentStance;
                TrySetStance(MovementStance.Prone);
            }
        }
        if (_toggleCrouch)
        {
            _toggleCrouch = false;
            if (IsProne) TrySetStance(MovementStance.Crouching);
            else TrySetStance(IsCrouching ? MovementStance.Standing : MovementStance.Crouching);
        }
        float previousCrouch = _crouch, previousProne = _prone;
        _crouch = Mathf.MoveToward(_crouch, IsCrouching ? 1f : 0f, dt * 6f);
        _prone = Mathf.MoveToward(_prone, IsProne ? 1f : 0f, dt * 5f);
        bool stanceAnimating = _crouch != previousCrouch || _prone != previousProne;
        if (stanceAnimating)
        {
            // A short upright capsule is an initial prone collision proxy; detailed lying
            // hull/hurtboxes belong to a later physical-pose pass.
            _capsule.Height = Mathf.Max(0.65f, 1.8f - 0.4f * _crouch
                - (1.8f - Mathf.Clamp(_settings.ProneCollisionHeight, 0.65f, 1.15f)) * _prone);
            _collider.Position = Vector3.Up * (_capsule.Height * 0.5f);
        }
        // Consume the press once. Upward velocity naturally disables floor snapping; gravity
        // continues while airborne until MoveAndSlide reports landing, then idle physics can sleep.
        bool jumping = _jumpRequested && IsOnFloor() && !GameplayInputBlocked
            && CurrentStance == MovementStance.Standing && GetWindow().HasFocus();
        _jumpRequested = false;
        Vector2 keys = !GameplayInputBlocked && GetWindow().HasFocus() ? ReadMovementInput().Normalized() : Vector2.Zero;
        Vector3 direction = GetMovementDirection(keys);
        float speed = Mathf.Max(0.1f, _settings.WalkSpeed) *
            (IsProne ? Mathf.Clamp(_settings.ProneSpeedMultiplier, 0.05f, 0.4f)
            : IsCrouching ? Mathf.Clamp(_settings.CrouchSpeedMultiplier, 0.1f, 1f)
            : Input.IsPhysicalKeyPressed(Key.Shift) ? Mathf.Max(1f, _settings.RunMultiplier) : 1f);
        Vector3 horizontal = new(Velocity.X, 0, Velocity.Z);
        horizontal = UpdateHorizontalVelocity(horizontal, direction * speed, dt);
        Vector3 before = GlobalPosition;
        float vertical = jumping ? Mathf.Clamp(_settings.JumpVelocity, 0f, 12f)
            : IsOnFloor() ? -0.1f : Velocity.Y - Mathf.Max(1, _settings.Gravity) * dt;
        Velocity = new Vector3(horizontal.X, vertical, horizontal.Z);
        MoveAndSlide();
        if (_reportMovementCollision) ReportMovementCollision();
        Vector3 travel = GlobalPosition - before; travel.Y = 0;
        float actualSpeed = dt > 0 ? travel.Length() / dt : 0;
        if (actualSpeed > 0.05f)
        {
            float yaw = Mathf.Atan2(-travel.X, -travel.Z);
            if (!_weaponPose) _art.Rotation = new Vector3(0, Mathf.LerpAngle(_art.Rotation.Y, yaw, 1 - Mathf.Exp(-14 * dt)), 0);
            float gaitReference = IsProne ? 2f : Mathf.Max(0.1f, _settings.WalkSpeed);
            _phase += dt * Mathf.Tau * Mathf.Max(0.1f, _settings.GaitRate) * actualSpeed / gaitReference;
            // Crawl uses a short, alternating elbow/leg movement instead of a walking stride.
            float liftPhase = Mathf.Cos(_phase);
            float gaitStrength = Mathf.Clamp(actualSpeed / gaitReference, 0f, 1f);
            Pose(Mathf.Sin(_phase) * (IsProne ? 0.13f : 0.5f),
                Mathf.Abs(Mathf.Sin(_phase * 2)) * (IsProne ? 0.008f : 0.025f),
                Mathf.Max(0, liftPhase) * gaitStrength, Mathf.Max(0, -liftPhase) * gaitStrength, gaitStrength);
        }
        else Pose(0, 0);
        if (stanceAnimating) WeaponPoseChanged?.Invoke();
        if (before.DistanceSquaredTo(GlobalPosition) > 0.0000001f) PositionChanged?.Invoke(GlobalPosition);
        if (IsOnFloor() && keys == Vector2.Zero && horizontal.LengthSquared() < 0.0001f && _crouch == (IsCrouching ? 1f : 0f) && _prone == (IsProne ? 1f : 0f))
        { Velocity = Vector3.Zero; _phase = 0; Pose(0, 0); SetPhysicsProcess(false); }
    }

    // =========================================================
    // A short stature is always allowed. Getting taller is gated by a one-time physics
    // clearance query for the destination body height; the actor never stands through ceilings.
    private bool TrySetStance(MovementStance next)
    {
        if (next == CurrentStance) return true;
        if (next == MovementStance.Standing && !HasStanceClearance(_standingQuery, 1.8f)) return false;
        if (next == MovementStance.Crouching && IsProne && !HasStanceClearance(_crouchingQuery, 1.4f)) return false;
        CurrentStance = next;
        StanceChanged?.Invoke(IsCrouching);
        MovementStanceChanged?.Invoke(next);
        return true;
    }

    // =========================================================
    private bool HasStanceClearance(PhysicsShapeQueryParameters3D query, float height)
    {
        query.Transform = GlobalTransform * new Transform3D(Basis.Identity,
            Vector3.Up * (height * 0.5f + 0.03f));
        return GetWorld3D().DirectSpaceState.IntersectShape(query, 1).Count == 0;
    }

    // =========================================================
    // F10 only: read the last MoveAndSlide contacts, no extra ray/shape queries or repeated logging.
    private void ReportMovementCollision()
    {
        _reportMovementCollision = false;
        int count = GetSlideCollisionCount();
        GD.Print($"Movement probe: player={GlobalPosition}, floor={IsOnFloor()}, contacts={count}. Hold movement against the blocker while pressing F10.");
        for (int i = 0; i < count; i++)
        {
            var hit = GetSlideCollision(i);
            string path = hit.GetCollider() is Node node ? node.GetPath().ToString() : "native/non-node collider";
            GD.Print($"Movement blocker: {path}; normal={hit.GetNormal()}; point={hit.GetPosition()}");
        }
    }

    // =========================================================
    // FPS only accepts jumps while its pointer is captured; overhead always accepts world clicks.
    protected virtual bool CanAcceptJumpInput() => true;

    // =========================================================
    // Small input hooks let isolated test views reuse collision, crouch and movement without copying the controller.
    protected virtual Vector3 UpdateHorizontalVelocity(Vector3 current, Vector3 target, float dt)
        => current.MoveToward(target, Mathf.Max(1, _settings.Acceleration) * dt);

    // =========================================================
    protected virtual bool IsMovementKey(Key key) => key == Key.Up || key == Key.Down || key == Key.Left || key == Key.Right || key == Key.Shift;
    protected virtual Vector2 ReadMovementInput() => new(
        (Input.IsPhysicalKeyPressed(Key.Right) ? 1 : 0) - (Input.IsPhysicalKeyPressed(Key.Left) ? 1 : 0),
        (Input.IsPhysicalKeyPressed(Key.Up) ? 1 : 0) - (Input.IsPhysicalKeyPressed(Key.Down) ? 1 : 0));
    protected virtual Vector3 GetMovementDirection(Vector2 keys) => (_screenRight * keys.X + _screenForward * keys.Y).Normalized();
    protected Node3D VisualRoot => _art;
    public float CapsuleHeight => _capsule.Height;
    // Actual transitioning capsule height keeps standing/crouching samples consistent in both camera modes.
    public Vector3 PerceptionHead => GlobalPosition + Vector3.Up * Mathf.Max(0.1f, CapsuleHeight - 0.1f);
    public Vector3 PerceptionTorso => GlobalPosition + Vector3.Up * (CapsuleHeight * 0.6f);

    // =========================================================
    // The actor owns its holding pose and socket; equipment owns everything about firing.
    public void SetWeaponPose(bool enabled) { _weaponPose = enabled; if (_initialized) Pose(0, 0); }

    // =========================================================
    // Face the cursor while walking/crouching; pitch only the held socket, independent of leg gait.
    public void SetWeaponAim(Vector3 target)
    {
        Vector3 horizontal = target - GlobalPosition; horizontal.Y = 0;
        if (horizontal.LengthSquared() < 0.01f) return;
        _art.Rotation = new Vector3(0, Mathf.Atan2(-horizontal.X, -horizontal.Z), 0);
        Vector3 direction = target - WeaponMount.GlobalPosition;
        if (direction.LengthSquared() > 0.0001f && Mathf.Abs(direction.Normalized().Dot(Vector3.Up)) < 0.99f)
            WeaponMount.GlobalBasis = Basis.LookingAt(direction, Vector3.Up);
    }

    // =========================================================
    // RC1: react with shoulders/arms, never with the weapon mount or player collider.
    // Updating the delta avoids accumulating rotation when the player is motionless.
    public void SetWeaponRecoilPose(float normalizedKick)
    {
        if (!_initialized) return;
        float amount = float.IsFinite(normalizedKick) ? Mathf.Clamp(normalizedKick, 0f, 1.5f) : 0f;
        _recoilArmAngle = Mathf.DegToRad(1.8f * amount);
        ApplyWeaponRecoilPose();
    }

    // =========================================================
    // Pose() resets animated joints during movement; it reapplies the most recent recoil once.
    private void ApplyWeaponRecoilPose()
    {
        float current = _weaponPose && _shoveGrip <= 0f ? _recoilArmAngle : 0f;
        float change = current - _recoilArmApplied;
        if (Mathf.Abs(change) < 0.000001f) return;
        _shoulderGirdle.Rotation += new Vector3(change, 0f, 0f);
        _leftArm.Rotation += new Vector3(change * 0.5f, 0f, 0f);
        _rightArm.Rotation += new Vector3(change * 0.5f, 0f, 0f);
        _recoilArmApplied = current;
    }

    // =========================================================
    // The shove owns only its temporary grip targets; locomotion keeps owning the normal rig pose.
    public void SetShoveGrip(Node3D? weapon, float weight)
    {
        _shoveWeapon = weapon; _shoveGrip = Mathf.Clamp(weight, 0, 1);
        if (!_initialized) return;
        _leftArm.RotationDegrees = _settings.LeftWeaponArmDegrees;
        _rightArm.RotationDegrees = _settings.RightWeaponArmDegrees;
        _leftElbow.Rotation = _rightElbow.Rotation = Vector3.Zero;
        ApplyShoveGrips();
    }

    // =========================================================
    // Two-bone arm solve keeps both gloves on the side-on gun without moving the character collider.
    private void ApplyShoveGrips()
    {
        if (_shoveGrip <= 0 || !IsInstanceValid(_shoveWeapon)) return;
        ReachGrip(_rightArm, _rightElbow, _shoveWeapon!.ToGlobal(new Vector3(0, -0.10f, 0.02f)), 1);
        ReachGrip(_leftArm, _leftElbow, _shoveWeapon.ToGlobal(new Vector3(0, -0.10f, -0.40f)), -1);
    }

    // =========================================================
    // Fixed rig lengths match PlayerMeshBuilder; clamp unreachable targets rather than stretching limbs.
    private void ReachGrip(Node3D arm, Node3D elbow, Vector3 target, float side)
    {
        const float upper = 0.32f, lower = 0.166f;
        Vector3 shoulder = arm.GlobalPosition, delta = target - shoulder;
        float distance = Mathf.Clamp(delta.Length(), 0.16f, upper + lower - 0.001f);
        Vector3 direction = delta.Normalized();
        if (direction.LengthSquared() < 0.001f) return;
        Vector3 pole = _art.GlobalBasis * new Vector3(side, -0.4f, 0.3f);
        Vector3 bend = pole - direction * pole.Dot(direction);
        if (bend.LengthSquared() < 0.001f) bend = Vector3.Up.Cross(direction);
        bend = bend.Normalized();
        float along = (upper * upper - lower * lower + distance * distance) / (2 * distance);
        Vector3 joint = shoulder + direction * along + bend * Mathf.Sqrt(Mathf.Max(0, upper * upper - along * along));
        Basis desired = ArmBasis(joint - shoulder, bend);
        arm.GlobalBasis = arm.GlobalBasis.Slerp(desired, _shoveGrip);
        desired = ArmBasis(shoulder + direction * distance - elbow.GlobalPosition, bend);
        elbow.GlobalBasis = elbow.GlobalBasis.Slerp(desired, _shoveGrip);
    }

    // =========================================================
    // Generated sleeves extend along local -Y, with a stable perpendicular bend axis.
    private static Basis ArmBasis(Vector3 direction, Vector3 bend)
    {
        Vector3 y = -direction.Normalized(), x = bend.Cross(y).Normalized();
        if (x.LengthSquared() < 0.001f) x = Vector3.Right;
        return new Basis(x, y, x.Cross(y).Normalized()).Orthonormalized();
    }

    // =========================================================
    // The same stride clock drives subtle trunk counter-rotation, shoulder movement and natural elbow flex.
    // Aiming heavily damps these motions to keep the rifle and cursor aligned.
    private void Pose(float angle, float bob, float leftLift = 0, float rightLift = 0, float gaitStrength = 0)
    {
        float stride = angle * Mathf.Lerp(1, 0.4f, _crouch) * (1f - 0.4f * _prone);
        float motion = gaitStrength * Mathf.Lerp(1f, 0.35f, _crouch) * (1f - 0.4f * _prone);
        float aimDamping = _weaponPose ? 0.25f : 1f;
        float swing = Mathf.Sin(_phase), across = Mathf.Cos(_phase);
        float torsoTwist = Mathf.DegToRad(Mathf.Clamp(_settings.TorsoTwistDegrees, 0, 8)) * motion * aimDamping;
        float torsoRoll = Mathf.DegToRad(Mathf.Clamp(_settings.TorsoRollDegrees, 0, 5)) * motion * aimDamping;
        float shoulderSway = Mathf.DegToRad(Mathf.Clamp(_settings.ShoulderSwayDegrees, 0, 6)) * motion;
        _art.Position = Vector3.Up * (bob * Mathf.Lerp(1, 0.35f, _crouch)
            - 0.72f * (1 - Mathf.Cos(1.15f * _crouch)) - 0.30f * _prone);
        // Lean upper body forward to the ground: the head/arms lead the crawl along local -Z.
        _upperBody.Rotation = new Vector3(0.18f * _crouch - 1.26f * _prone,
            -swing * torsoTwist, across * torsoRoll);
        _shoulderGirdle.Rotation = new Vector3(0, -swing * shoulderSway * 0.3f * aimDamping,
            Mathf.Sin(_phase * 2f) * shoulderSway * 0.5f * (_weaponPose ? 0.45f : 1f));
        _leftArm.Rotation = new Vector3(-stride - 0.4f * _crouch + 1.8f * _prone, 0, 0);
        _rightArm.Rotation = new Vector3(stride - 0.4f * _crouch + 1.8f * _prone, 0, 0);
        if (_weaponPose)
        {
            // Keep hands and equipped rifle forward when the parent torso lies horizontal.
            _leftArm.RotationDegrees = _settings.LeftWeaponArmDegrees + new Vector3(55f * _prone, 0, 0);
            _rightArm.RotationDegrees = _settings.RightWeaponArmDegrees + new Vector3(55f * _prone, 0, 0);
        }
        // Slight forearm lag follows the opposite leg; keep near zero when hands support the rifle.
        float elbowFlex = Mathf.DegToRad(Mathf.Clamp(_settings.ElbowFlexDegrees, 0, 35)) * motion;
        float armFreedom = _weaponPose ? 0.07f : 1f;
        _leftElbow.Rotation = new Vector3(elbowFlex * (0.55f + 0.45f * Mathf.Max(0, -swing)) * armFreedom, 0, 0);
        _rightElbow.Rotation = new Vector3(elbowFlex * (0.55f + 0.45f * Mathf.Max(0, swing)) * armFreedom, 0, 0);
        _leftLeg.Rotation = new Vector3(stride + 1.15f * _crouch - 1.08f * _prone, 0, 0);
        _rightLeg.Rotation = new Vector3(-stride + 1.15f * _crouch - 1.08f * _prone, 0, 0);
        // Crouch retains its existing deep knee bend; walking adds up to ~35 degrees on the airborne leg.
        float kneeBend = Mathf.DegToRad(Mathf.Clamp(_settings.KneeBendDegrees, 0, 60)) * (1f - _crouch) * (1f - _prone);
        _leftKnee.Rotation = new Vector3(-2.3f * _crouch + 0.70f * _prone - kneeBend * leftLift, 0, 0);
        _rightKnee.Rotation = new Vector3(-2.3f * _crouch + 0.70f * _prone - kneeBend * rightLift, 0, 0);
        // Small toe/heel movement while swinging; feet return to their original planted pose at rest.
        float footRoll = Mathf.DegToRad(Mathf.Clamp(_settings.FootRollDegrees, 0, 20)) * (1f - _crouch) * Mathf.Sin(_phase);
        _leftFoot.Rotation = new Vector3(-_leftLeg.Rotation.X - _leftKnee.Rotation.X + footRoll * leftLift, 0, 0);
        _rightFoot.Rotation = new Vector3(-_rightLeg.Rotation.X - _rightKnee.Rotation.X - footRoll * rightLift, 0, 0);
        ApplyShoveGrips();
        _recoilArmApplied = 0f; // Locomotion pose rewrote joint rotations above.
        ApplyWeaponRecoilPose();
    }

    // =========================================================
    // Do not retain a camera settings subscription after the player unloads.
    public override void _ExitTree()
    {
        if (_movementCamera is Camera view && IsInstanceValid(view))
            view.SettingsChanged -= RefreshMovementAxes;
        if (IsInstanceValid(Vitals)) Vitals.Died -= OnDied;
        _movementCamera = null;
    }
}
