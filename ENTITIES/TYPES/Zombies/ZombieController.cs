// Reusable zombie: shared sight, health, roaming and chase with overridable artwork/gait for future variants.
using Godot;

public partial class ZombieController : EntityCore, IDamageReceiver, IImpactReceiver
{
    public CorpseManager? CorpseOwner { get; set; }
    private uint _corpseSeed;
    private bool _deathHandled;
    private AnatomyHitboxes _anatomy = null!;
    private ZombieAnatomy _anatomyVisual = null!;
    public AnatomyComponent? Anatomy { get; private set; }
    private bool _receivingHit;
    private ZombieLocomotion _locomotion = null!;
    private Node3D _anatomyHead = null!;
    private bool _mobilityDirty, _reportedMobilityBlock;
    public ZombieMobility Mobility => _locomotion?.Mode ?? ZombieMobility.Walk;
    public bool LowBodyActive => _locomotion?.IsLowBody == true;
    public Vector3 SightOrigin => LowBodyActive ? _anatomyHead.GlobalPosition : GlobalPosition + Vector3.Up * _config.EyeHeight;
    public BodyRegion LastHitRegion { get; private set; } = BodyRegion.Unknown;
    public uint AnatomicalHitCount { get; private set; }
    public float Health => _health == null ? 0 : _health.CurrentHealth;
    private HealthComponent _health = null!;
    public bool IsChasing { get; private set; }
    private Node3D? _target;
    private IPerceptionTarget? _sightTarget;
    private VisibilityResolver? _visibility;
    private MeshInstance3D? _visionCone;
    private bool _showVisionCone;
    public float VisionRange => (IsChasing || IsInvestigating) ? Mathf.Max(_config.DetectionRange, _config.LoseRange) : _config.DetectionRange;
    private ZombieSettings _config = null!;
    private Timer _brain = null!;
    private readonly RandomNumberGenerator _random = new();
    private System.Func<Vector3, float>? _groundHeight;
    private PhysicsRayQueryParameters3D _groundRay = null!;
    private PhysicsShapeQueryParameters3D _recoveryQuery = null!;
    private CollisionShape3D _bodyShape = null!;
    private bool _reportedGroundFailure, _awaitingSpawnGround, _reportedSpawnGround;
    private Vector3 _home, _goal;
    private bool _hasGoal, _configured;
    private ulong _restUntil;
    private float _phase, _stuck;
    private HitReaction _reaction;
    private bool _shoveStagger, _meleeBackstep;
    private float _recoveryRemaining;
    private Vector3 _reactionPose;
    protected Node3D _leftArm = null!, _rightArm = null!, _leftLeg = null!, _rightLeg = null!;
    protected Node3D _leftKnee = null!, _rightKnee = null!;
    // =========================================================
    // Art and one decision timer are built once. The inherited scene stays dormant until placement supplies a target.
    public override void _Ready()
    {
        if (_brain == null)
        {
            BuildArt();
            UpdatePose(0);
            BuildAnatomy();
            _health = new HealthComponent { Name = "Health" }; AddChild(_health);
            _health.Died += OnKilled;
            _brain = new Timer { OneShot = true, ProcessCallback = Timer.TimerProcessCallback.Physics };
            AddChild(_brain); _brain.Timeout += Think;
        }
        FloorSnapLength = 0.5f;
        _bodyShape = GetNode<CollisionShape3D>("CollisionShape3D");
        _groundRay = new PhysicsRayQueryParameters3D { CollisionMask = 1,
            Exclude = new Godot.Collections.Array<Rid> { GetRid() } };
        _recoveryQuery = new PhysicsShapeQueryParameters3D { Shape = _bodyShape.Shape, CollisionMask = CollisionMask,
            Exclude = new Godot.Collections.Array<Rid> { GetRid() } };
        _wanderQuery = new PhysicsShapeQueryParameters3D { Shape = _bodyShape.Shape,
            CollisionMask = CollisionMask | 17u, CollideWithBodies = true, CollideWithAreas = false,
            Exclude = new Godot.Collections.Array<Rid> { GetRid() } };
        base._Ready();
        _config = (ZombieSettings)RuntimeSettings;
        _investigation ??= new InvestigationMemory(_config.CreateInvestigationSettings());
        _locomotion ??= new ZombieLocomotion(this, _bodyShape, VisualRoot, _anatomyHead,
            _config.LocomotionProfile ?? new ZombieLocomotionSettings(),
            _leftArm, _rightArm, _leftLeg, _rightLeg, _leftKnee, _rightKnee);
    }

    // =========================================================
    // Cache eight pose-following boxes: torso/head, arms, thighs and knee/shins (six labelled regions).
    private void BuildAnatomy()
    {
        _anatomy = new AnatomyHitboxes { Name = "AnatomyHitboxes" }; AddChild(_anatomy);
        _anatomy.Initialize(this, (Settings as ZombieSettings)?.AnatomyProfile
            ?? GD.Load<AnatomySettings>("res://ENTITIES/Shared/Anatomy/Configuration/AnatomyMaster.tres"));
        Node3D head = _anatomyHead = VisualRoot.GetNode<Node3D>("Head");
        var pivots = new[] { VisualRoot, head, _leftArm, _rightArm, _leftLeg, _rightLeg, _leftKnee, _rightKnee };
        var regions = new[] { BodyRegion.Torso, BodyRegion.Head, BodyRegion.LeftArm, BodyRegion.RightArm,
            BodyRegion.LeftLeg, BodyRegion.RightLeg, BodyRegion.LeftLeg, BodyRegion.RightLeg };
        var boundaries = new System.Collections.Generic.HashSet<Node3D>(pivots);
        for (int i = 0; i < pivots.Length; i++) _anatomy.AddRegion(pivots[i], regions[i], boundaries);
        if (_anatomy.RequiresPoseSync) _anatomy.SyncPose();
        _anatomyVisual = new ZombieAnatomy(_leftArm, _rightArm, _leftLeg, _rightLeg);
    }

    // =========================================================
    // Species-specific injury effects stay at this adapter boundary; shared state never knows mesh names.
    private void OnAnatomyChanged(BodyRegion region, LimbState state)
    {
        if (state == LimbState.Severed) _anatomy.DisableRegion(region);
        AnatomySnapshot snapshot = Anatomy!.Capture();
        _anatomyVisual.Apply(snapshot);
        ZombieMobility previous = Mobility;
        _locomotion.Refresh(snapshot); _mobilityDirty = true; _reportedMobilityBlock = false;
        if (previous != Mobility && _anatomy.RuntimeSettings.LogRegionHits)
            GD.Print($"[ELD2] {GetPath()} mode={Mobility}");
        if (IsActive && !_awaitingSpawnGround) SetPhysicsProcess(true);
        if (_anatomy.RuntimeSettings.LogRegionHits)
            GD.Print($"[ELD1] {GetPath()} region={region} state={state} condition={Anatomy.Condition(region):0.##}");
    }

    // =========================================================
    // Debug builds only: demonstrate surviving multi-limb injuries without changing damage or vitality settings.
    public void DebugInjure(BodyRegion region, LimbState state)
    {
        if (!OS.IsDebugBuild() || _receivingHit || !IsActive || !_configured || _health.IsDead) return;
        _receivingHit = true;
        try { Anatomy!.DebugSetState(region, state); }
        finally { _receivingHit = false; }
    }

    // =========================================================
    // Preserve virtual species pose hooks; synchronize even when AI changes pose outside the movement tick.
    private void UpdatePose(float step, float leftLift = 0, float rightLift = 0)
    { PoseWithMobility(step, leftLift, rightLift); if (_anatomy?.RequiresPoseSync == true) _anatomy.SyncPose(); }

    // =========================================================
    // Healthy virtual poses remain species-owned; injury presentation overlays them for both variants.
    private void PoseWithMobility(float step, float leftLift = 0, float rightLift = 0)
    {
        VisualRoot.Rotation = new Vector3(0, VisualRoot.Rotation.Y, 0);
        Pose(step, leftLift, rightLift);
        _locomotion?.ApplyPose(step);
    }

    // =========================================================
    private void TryMobilityBody()
    {
        if (!_locomotion.BodyPending) return;
        if (!_locomotion.TryBody(VisualRoot.Rotation.Y))
        {
            if (!_reportedMobilityBlock && _anatomy.RuntimeSettings.LogRegionHits)
                GD.Print($"[ELD2] {GetPath()} waiting for body clearance; mode={Mobility}");
            _reportedMobilityBlock = true; return;
        }
        _reportedMobilityBlock = false;
        if (_anatomy.RuntimeSettings.LogRegionHits) GD.Print($"[ELD2] {GetPath()} low_body={LowBodyActive}");
        _recoveryQuery.Shape = _wanderQuery.Shape = _bodyShape.Shape;
        UpdatePose(0); ResetPhysicsInterpolation();
        if (IsActive && !_awaitingSpawnGround) SetPhysicsProcess(true);
    }

    // =========================================================
    // Long crawler colliders must turn with their visual body, including slow investigation-look turns.
    private void SetMobilityFacing(float yaw)
    {
        yaw = _locomotion.TryTurn(yaw);
        VisualRoot.Rotation = new Vector3(VisualRoot.Rotation.X, yaw, VisualRoot.Rotation.Z);
    }

    // =========================================================
    // Subtypes only supply art and optional motion tuning; detection, health and AI stay shared.
    protected virtual void BuildArt()
    {
        var mesh = new ZombieMeshBuilder(VisualRoot);
        BindArtJoints(mesh.LeftArm, mesh.RightArm, mesh.LeftLeg, mesh.RightLeg, mesh.LeftKnee, mesh.RightKnee);
    }

    // =========================================================
    // Share the existing six-pivot shamble contract across independent procedural mesh builders.
    protected void BindArtJoints(Node3D leftArm, Node3D rightArm, Node3D leftLeg, Node3D rightLeg,
        Node3D leftKnee, Node3D rightKnee)
    {
        _leftArm = leftArm; _rightArm = rightArm; _leftLeg = leftLeg; _rightLeg = rightLeg;
        _leftKnee = leftKnee; _rightKnee = rightKnee;
    }

    // =========================================================
    // Only species animation/movement feel is customizable; all current zombie timings remain unchanged.
    protected virtual float MovementAcceleration => 5f;
    protected virtual float GaitPhasePerMeter => 5f;

    // =========================================================
    // World owns placement and target references; the entity has no dependency on PlayerController or Terrain3D.
    public void InitializeTarget(Node3D player, uint seed, System.Func<Vector3, float>? groundHeight = null)
    {
        Deactivate(); ClearReaction(); ClearInvestigation(); _groundHeight = groundHeight; _reportedGroundFailure = false; _target = player; _home = GlobalPosition; _random.Seed = seed;
        _corpseSeed = seed; _deathHandled = false;
        // ZMAT0: spawn-seeded cached material assignment; does not advance the movement RNG.
        ZombieAppearance.Apply(VisualRoot, seed, _config.AppearanceVariationEnabled, _config.BloodChance);
        _sightTarget = player as IPerceptionTarget;
        _visibility = player is CollisionObject3D body && _sightTarget != null
            ? new VisibilityResolver(this, body, _config.SightBlockerMask) : null;
        if (_visibility == null) GD.PushWarning("Zombie target must provide body samples and a physics body; eyesight disabled.");
        if (Anatomy != null) Anatomy.StateChanged -= OnAnatomyChanged;
        Anatomy = new AnatomyComponent(_config.AnatomyProfile ?? _anatomy.RuntimeSettings);
        Anatomy.StateChanged += OnAnatomyChanged;
        AnatomySnapshot reset = Anatomy.Capture();
        _anatomyVisual.Apply(reset); _locomotion.Refresh(reset); _mobilityDirty = true;
        _anatomy.RestoreRegions(CollisionLayer);
        LastHitRegion = BodyRegion.Unknown; AnatomicalHitCount = 0;
        _health.Configure(_config.HealthProfile);
        _awaitingSpawnGround = groundHeight != null; _reportedSpawnGround = false;
        ResetPhysicsInterpolation();
        _configured = true; _hasGoal = false; IsChasing = false; _restUntil = 0; Activate();
    }

    // =========================================================
    // Shared health owns all damage maths. Species code only decides whether hits are accepted and how death looks.
    public void ReceiveHit(DamagePacket hit)
    {
        if (_receivingHit || !IsActive || !_configured || _health.IsDead) return;
        _receivingHit = true;
        try
        {
            DamageResult resolved = DamageResolver.Resolve(hit, _health.RuntimeSettings);
            if (resolved.Amount <= 0) return;
            // Commit injury and visual removal before the synchronous HealthComponent.Died callback.
            Anatomy!.ApplyResolved(resolved);
            LastHitRegion = hit.Region;
            if (hit.Region != BodyRegion.Unknown)
            {
                AnatomicalHitCount++;
                if (_anatomy.RuntimeSettings.LogRegionHits)
                    GD.Print($"[ELD0] {GetPath()} region={hit.Region} incoming={hit.Amount:0.##} point={hit.Position}");
            }
            _health.ApplyResolvedDamage(resolved);
        }
        finally { _receivingHit = false; }
    }

    // =========================================================
    // Damage has already resolved when dispatched together. CM2-B2 shoves add collision-aware backward movement,
    // while CM1 bullets retain their original damped-velocity stagger and pose.
    public void ReceiveImpact(in CombatImpact impact)
    {
        if (!IsActive || !_configured || _health.IsDead || IsQueuedForDeletion() || _awaitingSpawnGround) return;
        if (impact.Kind == CombatImpactKind.Bullet)
        {
            InvestigateBulletHit(impact.Source);
            if (!_config.BulletReactionsEnabled
                || !_reaction.TryStart(impact, _config.ImpactResistance, _config.FlinchThreshold,
                    _config.MaxStaggerSeconds, _config.MinFlinchInterval, _config.MinRecoverySeconds,
                    Time.GetTicksMsec())) return;
            _shoveStagger = _meleeBackstep = false;
            // Weaker weapon impact retains more momentum; strength 1 keeps the original rifle response.
            float power = Mathf.Clamp(_reaction.Strength, 0f, 1f);
            float keep = Mathf.Lerp(1f, Mathf.Clamp(_config.ImpactVelocityRetention, 0f, 1f), power);
            Velocity = new Vector3(Velocity.X * keep, Velocity.Y, Velocity.Z * keep);
        }
        else if (impact.Kind == CombatImpactKind.Shove || impact.Kind == CombatImpactKind.Melee)
        {
            // Damaging bat swings reuse collision-aware shove travel, but with their own reaction window.
            bool melee = impact.Kind == CombatImpactKind.Melee;
            float maxDuration = melee ? Mathf.Clamp(_config.MeleeMaxStaggerSeconds, 0.1f, 1f)
                : _config.ShoveMaxStaggerSeconds;
            if (!_config.ShoveReactionsEnabled
                || !_reaction.TryStart(impact, _config.ShoveResistance, _config.ShoveFlinchThreshold,
                    maxDuration, _config.MinFlinchInterval, _config.MinRecoverySeconds,
                    Time.GetTicksMsec())) return;
            _shoveStagger = true; _meleeBackstep = melee;
            // The original MoveAndSlide resolves walls and other entities; no teleport or upward lift.
            float pushSpeed = Mathf.Min(SafeShoveSetting(_config.ShoveMaxPushSpeed, 20f),
                _reaction.Strength * SafeShoveSetting(_config.ShovePushSpeed, 20f));
            Vector3 push = _reaction.Direction * pushSpeed;
            Velocity = new Vector3(push.X, Velocity.Y, push.Z);
        }
        else return;
        _recoveryRemaining = 0; _stuck = 0; SetPhysicsProcess(true);
    }

    // =========================================================
    // Guard configurable shove speeds/acceleration against non-finite authoring values.
    private static float SafeShoveSetting(float value, float max)
    { return float.IsFinite(value) ? Mathf.Clamp(value, 0f, max) : 0f; }

    // =========================================================
    // Remove only the additive hit pose, preserving the species' normal lean/yaw/gait.
    private void RemoveReactionPose()
    {
        if (_reactionPose == Vector3.Zero) return;
        VisualRoot.Rotation -= _reactionPose; _reactionPose = Vector3.Zero;
    }

    // =========================================================
    private void ClearReaction()
    { RemoveReactionPose(); _reaction.Reset(); _shoveStagger = _meleeBackstep = false; _recoveryRemaining = 0; }

    // =========================================================
    private void ApplyReactionPose()
    {
        if (!_reaction.IsActive || (_shoveStagger && !_meleeBackstep)) return; // Q unchanged; bat adds torso recoil.
        float pulse = Mathf.Sin(_reaction.Progress * Mathf.Pi) * Mathf.Min(1, _reaction.Strength);
        float angle = Mathf.DegToRad(Mathf.Clamp(_config.FlinchDegrees * (_meleeBackstep ? 1.8f : 1f), 0, 25)) * pulse;
        float side = Mathf.Clamp(_reaction.Direction.Dot(VisualRoot.GlobalBasis.X), -1, 1);
        _reactionPose = new Vector3(angle, 0, -side * angle * 0.4f);
        VisualRoot.Rotation += _reactionPose;
    }

    // =========================================================
    // The actor owns its death reaction; the resolver has no zombie/robot/deer-specific behaviour.
    private void OnKilled(DamagePacket hit)
    {
        if (_deathHandled) return;
        _deathHandled = true;
        _anatomy.Disable();
        Node3D visual = VisualRoot;
        Transform3D at = visual.GlobalTransform;
        Vector3 velocity = Velocity;
        var joints = new System.Collections.Generic.List<Node3D> { _leftArm, _rightArm, _leftLeg, _rightLeg, _leftKnee, _rightKnee };
        var head = visual.GetNodeOrNull<Node3D>("Head"); if (head != null) joints.Add(head);
        AnatomySnapshot injuries = Anatomy!.Capture();
        var pose = new ZombieCorpsePose(_corpseSeed, injuries, joints.ToArray());
        bool alignCorpsePivot = LowBodyActive;
        var owner = CorpseOwner;
        Deactivate(); _configured = false; CollisionLayer = CollisionMask = 0;
        _anatomyVisual.Apply(injuries); // Explicitly preserve the final snapshot after Pose(0)/reaction reset.
        Hide();
        // Keep the disabled actor until the deferred visual transfer finishes; never leave corpse physics under it.
        Callable.From(() =>
        {
            if (!IsInstanceValid(this) || !IsInsideTree()) return;
            try
            {
                if (IsInstanceValid(owner) && owner!.IsInsideTree())
                    owner.TrySpawn(visual, at, pose, hit.Direction, velocity, _corpseSeed, alignPivotToVisual: alignCorpsePivot);
            }
            finally { QueueFree(); }
        }).CallDeferred();
    }

    // =========================================================
    protected override void OnActivated()
    { if (_configured) { _brain.Start(_random.RandfRange(0.05f, Mathf.Max(0.05f, _config.SightIntervalSeconds))); SetPhysicsProcess(true); } UpdateVisionDebug(); }

    // =========================================================
    protected override void OnDeactivated()
    { ClearReaction(); ClearInvestigation(); _brain?.Stop(); _hasGoal = false; IsChasing = false; if (_leftArm != null) UpdatePose(0); UpdateVisionDebug(); }

    // =========================================================
    // Initial frontal acquisition, then angle-independent alerted tracking with mandatory LOS.
    // Hidden target positions never become confirmed chase goals.
    private void Think()
    {
        if (!IsActive || !_configured) return;
        if (_awaitingSpawnGround && !ConfirmSpawnGround())
        { _brain.Start(Mathf.Max(0.1f, _config.SightIntervalSeconds)); return; }
        TryMobilityBody();
        CheckGroundPenetration();
        bool wasChasing = IsChasing;
        bool targetValid = IsInstanceValid(_target) && _target!.IsInsideTree() && !_target.IsQueuedForDeletion();
        bool chasing = false;
        if (targetValid && _visibility != null && _sightTarget != null)
        {
            Vector3 offset = _target.GlobalPosition - GlobalPosition; offset.Y = 0;
            float squared = offset.LengthSquared(), range = Mathf.Max(0, VisionRange);
            if (squared <= range * range)
            {
                // During an ongoing chase or PER2 search, don't forget an unobstructed
                // target simply because it ran behind the zombie's visual facing.
                bool inCone = IsChasing || IsInvestigating;
                if (!inCone)
                {
                    Vector3 forward = Vector3.Forward.Rotated(Vector3.Up, VisualRoot.Rotation.Y);
                    float threshold = Mathf.Cos(Mathf.DegToRad(Mathf.Clamp(_config.VisionAngleDegrees, 1, 180)) * 0.5f);
                    inCone = squared < 0.000001f || forward.Dot(offset / Mathf.Sqrt(squared)) >= threshold;
                }
                if (inCone)
                    chasing = _visibility.CanSee(GetWorld3D().DirectSpaceState,
                        SightOrigin, _sightTarget);
            }
        }
        if (chasing != IsChasing)
        { IsChasing = chasing; _hasGoal = false; if (!_reaction.IsActive) { RemoveReactionPose(); UpdatePose(0); } }
        ulong now = Time.GetTicksMsec();
        UpdateInvestigation(wasChasing, now, targetValid);
        if (_locomotion.CanDrive && !IsChasing && !IsInvestigating && !_anchorPending && !_hasGoal && now >= _restUntil)
        {
            if (!TryChooseWanderGoal()) Rest();
        }
        if ((_hasGoal && _locomotion.CanDrive) || _anchorPending || !IsOnFloor()) SetPhysicsProcess(true);
        UpdateVisionDebug();
        _brain.Start(Mathf.Max(0.1f, _config.SightIntervalSeconds));
    }

    // =========================================================
    // Debug owns mesh construction/input; species owns visibility lifecycle. No debug frame callback.
    public void AttachVisionDebug(MeshInstance3D cone)
    { cone.Reparent(this, true); _visionCone = cone; _showVisionCone = _config.ShowVisionCone; UpdateVisionDebug(); }

    // =========================================================
    public void SetVisionDebugVisible(bool visible)
    { _showVisionCone = visible; UpdateVisionDebug(); }

    // =========================================================
    private void UpdateVisionDebug()
    {
        if (_visionCone == null) return;
        bool visible = IsActive && _showVisionCone;
        if (_visionCone.Visible != visible) _visionCone.Visible = visible;
        if (visible)
        {
            Vector3 position = ToLocal(SightOrigin);
            Vector3 rotation = new(0, VisualRoot.Rotation.Y, 0);
            if (_visionCone.Position != position) _visionCone.Position = position;
            if (_visionCone.Rotation != rotation) _visionCone.Rotation = rotation;
        }
        float range = Mathf.Max(0, VisionRange);
        var scale = new Vector3(range, 1, range);
        if (_visionCone.Scale != scale) _visionCone.Scale = scale;
    }

    // =========================================================
    // Confirm native spawn collision on the existing staggered physics timer before enabling gravity.
    private bool ConfirmSpawnGround()
    {
        float height = _groundHeight!(GlobalPosition);
        bool confirmed = false;
        if (float.IsFinite(height))
        {
            _groundRay.From = new Vector3(GlobalPosition.X, height + 0.6f, GlobalPosition.Z);
            _groundRay.To = new Vector3(GlobalPosition.X, height - 0.6f, GlobalPosition.Z);
            var hit = GetWorld3D().DirectSpaceState.IntersectRay(_groundRay);
            confirmed = hit.Count > 0 && hit["normal"].AsVector3().Y >= Mathf.Cos(FloorMaxAngle);
        }
        if (confirmed) { _awaitingSpawnGround = false; SetPhysicsProcess(true); return true; }
        Velocity = Vector3.Zero; SetPhysicsProcess(false);
        if (!_reportedSpawnGround)
        {
            GD.PushWarning($"Zombie spawn waiting for solid terrain: position={GlobalPosition}, terrain_height={height}, mask={CollisionMask}. No gravity until confirmed; check Terrain3D collision coverage.");
            _reportedSpawnGround = true;
        }
        return false;
    }

    // =========================================================
    // Use the existing staggered physics timer only while falling. The world optionally supplies open-terrain height.
    private void CheckGroundPenetration()
    {
        if (!_config.GroundRecoveryEnabled || _groundHeight == null || IsOnFloor() || Velocity.Y >= -0.1f) return;
        float height = _groundHeight(GlobalPosition);
        // Holes/missing terrain return NaN; ordinary cliff falls remain ABOVE the lower ground and are untouched.
        if (!float.IsFinite(height) || GlobalPosition.Y >= height - Mathf.Max(0.25f, _config.GroundRecoveryDepth)) return;
        Vector3 before = GlobalPosition;
        _groundRay.From = new Vector3(before.X, height + 0.6f, before.Z);
        _groundRay.To = new Vector3(before.X, height - 0.6f, before.Z);
        var space = GetWorld3D().DirectSpaceState;
        var hit = space.IntersectRay(_groundRay);
        string result = "no solid ground detected above sunk zombie";
        if (hit.Count > 0 && hit["normal"].AsVector3().Y >= Mathf.Cos(FloorMaxAngle))
        {
            Vector3 position = hit["position"].AsVector3() + Vector3.Up * 0.12f;
            Transform3D shapeTransform = _bodyShape.GlobalTransform;
            shapeTransform.Origin += position - before; _recoveryQuery.Transform = shapeTransform;
            if (space.IntersectShape(_recoveryQuery, 1).Count == 0)
            {
                GlobalPosition = position; Velocity = Vector3.Zero; ResetPhysicsInterpolation(); SetPhysicsProcess(true);
                result = $"recovered onto confirmed solid surface at {position}";
            }
            else result = "recovery blocked by another collider";
        }
        if (!_reportedGroundFailure)
        {
            GD.PushWarning($"Zombie ground penetration: position={before}, terrain_height={height}, mask={CollisionMask}; {result}.");
            _reportedGroundFailure = true;
        }
    }

    // =========================================================
    // Only moving/falling zombies run physics; waiting or stopped zombies retain just their low-rate decision timer.
    public override void _PhysicsProcess(double delta)
    {
        // A failed resource/Ready or a not-yet-configured spawn must never cascade into per-tick exceptions.
        if (!_configured || !IsActive) { SetPhysicsProcess(false); return; }
        if (_awaitingSpawnGround) { SetPhysicsProcess(false); return; }
        float dt = (float)delta;
        RemoveReactionPose();
        if (_mobilityDirty) { TryMobilityBody(); UpdatePose(0); _mobilityDirty = false; }
        bool reacting = _reaction.IsActive;
        bool shovePushing = reacting && _shoveStagger;
        if (reacting)
        {
            _reaction.Advance(dt, Time.GetTicksMsec());
            if (!_reaction.IsActive)
            {
                _shoveStagger = _meleeBackstep = false;
                _recoveryRemaining = Mathf.Clamp(_config.RecoverySeconds, 0, 1);
            }
        }
        bool recovering = !reacting && _recoveryRemaining > 0;
        if (recovering) _recoveryRemaining = Mathf.Max(0, _recoveryRemaining - dt);
        Vector3 offset = _goal - GlobalPosition; offset.Y = 0;
        float stop = IsChasing ? Mathf.Max(0.7f, _config.StopDistance)
            : _investigation.Phase == InvestigationPhase.Travel ? SafePolicy(_config.InvestigationArrivalDistance, 0.2f, 1.5f) : 0.4f;
        bool walking = _locomotion.CanDrive && _hasGoal && offset.LengthSquared() > stop * stop;
        if (_hasGoal && _locomotion.CanDrive && !walking && !IsChasing && !shovePushing)
        { if (_investigation.Phase == InvestigationPhase.Travel) InvestigationArrived(); else Rest(); }
        Vector3 direction = walking ? offset.Normalized() : Vector3.Zero;
        float speed = _locomotion.Speed(Mathf.Max(0, IsPursuing ? _config.ChaseSpeed : _config.WanderSpeed));
        // Scale movement slowdown with accepted bullet strength. Do not alter shove displacement.
        if (reacting && !_shoveStagger)
            speed *= Mathf.Lerp(1f, Mathf.Clamp(_config.StaggerMoveMultiplier, 0f, 1f),
                Mathf.Clamp(_reaction.Strength, 0f, 1f));
        float acceleration = reacting ? Mathf.Max(1, _config.StaggerBrakingAcceleration)
            : recovering ? Mathf.Max(0.1f, _config.RecoveryAcceleration) : MovementAcceleration * _locomotion.AccelerationScale;
        // Chase AI cannot cancel shove travel. Use the ordinary MoveAndSlide collision path.
        Vector3 currentHorizontal = new(Velocity.X, 0, Velocity.Z);
        Vector3 horizontal = shovePushing
            ? currentHorizontal.MoveToward(Vector3.Zero, dt * SafeShoveSetting(_config.ShoveBrakingAcceleration, 40f))
            : currentHorizontal.MoveToward(direction * speed, dt * Mathf.Max(0.1f, acceleration));
        if (!_locomotion.CanDrive && !shovePushing) horizontal = Vector3.Zero;
        Vector3 before = GlobalPosition;
        Velocity = new Vector3(horizontal.X, IsOnFloor() ? -0.1f : Velocity.Y - 20 * dt, horizontal.Z);
        MoveAndSlide();
        Vector3 travel = GlobalPosition - before; travel.Y = 0;
        bool moved = travel.LengthSquared() > 0.000001f;
        if (moved)
        {
            if (shovePushing)
            {
                // Q still slides. Damaging melee has a brief backward stepping gait while recoiling.
                if (_meleeBackstep)
                {
                    _phase += travel.Length() * Mathf.Max(0.1f, GaitPhasePerMeter) * 1.2f;
                    float stride = Mathf.Cos(_phase);
                    PoseWithMobility(-Mathf.Sin(_phase) * 0.8f, Mathf.Max(0f, stride), Mathf.Max(0f, -stride));
                }
                else PoseWithMobility(0);
            }
            else
            {
                float yaw = Mathf.Atan2(-travel.X, -travel.Z);
                SetMobilityFacing(Mathf.LerpAngle(VisualRoot.Rotation.Y, yaw, 1 - Mathf.Exp(-4 * dt)));
                _phase += travel.Length() * Mathf.Max(0.1f, GaitPhasePerMeter * _locomotion.GaitScale);
                // Flex the airborne knee slightly, using the existing shamble clock; no extra animation loop.
                float swing = Mathf.Cos(_phase);
                PoseWithMobility(Mathf.Sin(_phase), Mathf.Max(0, swing), Mathf.Max(0, -swing));
            }
            _stuck = 0;
        }
        else { PoseWithMobility(0); if (walking && !reacting && !recovering) _stuck += dt; }
        ApplyReactionPose();
        if (_anatomy.RequiresPoseSync) _anatomy.SyncPose();
        UpdateVisionDebug();
        if (_stuck > 2)
        { if (IsInvestigating) FinishInvestigation(); else Rest(); _stuck = 0; }
        if (!reacting && !recovering && (!walking || !_hasGoal) && horizontal.LengthSquared() < 0.0001f && IsOnFloor())
        {
            Velocity = Vector3.Zero;
            if (_anchorPending) { _home = GlobalPosition; _anchorPending = false; }
            SetPhysicsProcess(false);
        }
    }

    // =========================================================
    // Bounded pauses also recover wandering from a blocked direction; pursuit reacquires on the next decision.
    private void Rest()
    { _hasGoal = false; _restUntil = Time.GetTicksMsec() + (ulong)_random.RandiRange(1000, 3000); }

    // =========================================================
    // Arms extend toward the player during pursuit, including the stop-near-player pose.
    protected virtual void Pose(float step, float leftLift = 0, float rightLift = 0)
    {
        VisualRoot.Position = Vector3.Up * (Mathf.Abs(step) * 0.015f);
        _leftArm.Rotation = new Vector3((IsPursuing ? 1.31f : 0.12f) - step * 0.10f, 0, -0.11f);
        _rightArm.Rotation = new Vector3((IsPursuing ? 1.40f : 0.20f) + step * 0.07f, 0, 0.07f);
        _leftLeg.Rotation = new Vector3(step * 0.22f, 0.02f, -0.025f);
        _rightLeg.Rotation = new Vector3(-step * 0.19f, -0.02f, 0.018f);
        _leftKnee.Rotation = new Vector3(-0.10f - leftLift * 0.30f, 0, 0);
        _rightKnee.Rotation = new Vector3(-rightLift * 0.22f, 0, 0);
    }

}
