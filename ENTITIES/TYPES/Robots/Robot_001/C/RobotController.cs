// Thin robot coordinator: shared patrol, shared logical sensing, independent cone VFX and optional gait.
using Godot;

public partial class RobotController : EntityCore
{
    [Export] public RobotSettings RobotMaster { get; set; } = null!;
    public RobotSettings RuntimeRobotSettings { get; private set; } = null!;
    public Node3D HeadPivot { get; private set; } = null!;
    public Node3D SensorSocket { get; private set; } = null!;
    private MechanicalLegs? _legs;
    private RobotAudio _audio = null!;
    private VisibleOnScreenNotifier3D _visibility = null!;
    public event System.Action<string>? StageChanged;
    public string Stage { get; private set; } = "Idle";
    public PatrolController Patrol => _patrol;
    private PatrolController _patrol = null!;
    private ScanSensor _sensor = null!;
    private ScanBeam _beam = null!;
    private Timer _stageTimer = null!;
    private bool _encounter, _hiddenWasActive, _lastAnimateLegs = true;
    private float _scanElapsed;
    private float _motionSpeed;
    private bool _preview, _relevant = true, _built;

    // =========================================================
    // Assemble art and cache all sockets once before base Ready activates optional animation.
    public override void _Ready()
    {
        if (!_built)
        {
            RuntimeRobotSettings = RobotMaster != null ? (RobotSettings)RobotMaster.Duplicate(true) : new RobotSettings();
            var s = RuntimeRobotSettings;
            VisualRoot.Scale = Vector3.One * Mathf.Max(0.25f, s.ModelScale);
            Node3D body;
            if (s.ArtOverride != null)
            {
                var art = s.ArtOverride.Instantiate<Node3D>(); VisualRoot.AddChild(art);
                body = art.GetNode<Node3D>("BodyRoot");
            }
            else body = RobotMeshBuilder.Build(VisualRoot, s);
            HeadPivot = body.GetNode<Node3D>("HeadPivot");
            SensorSocket = HeadPivot.GetNode<Node3D>("SensorSocket");
            var hips = new Node3D[4]; var offsets = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                hips[i] = body.GetNode<Node3D>(RobotMeshBuilder.LegNames[i] + "Hip");
                offsets[i] = hips[i].Position;
            }
            _legs = new MechanicalLegs(VisualRoot, body, hips, offsets, s.HipHeight,
                Mathf.Max(0.1f, s.UpperLegLength), Mathf.Max(0.1f, s.LowerLegLength), s.StanceWidth)
            {
                StrideLength = s.StrideLength, CyclesPerSecond = s.GaitSpeed, Lift = s.LegLift,
                StanceFraction = s.StanceFraction, Bob = s.BodyBob, PitchDegrees = s.BodyPitchDegrees, SettleSeconds = s.SettleSeconds
            };
            _audio = new RobotAudio(this);
            _audio.Configure(s.AudioEnabled, s.AudioVolumeDb);
            var collision = GetNode<CollisionShape3D>("CollisionShape3D");
            float scale = Mathf.Max(0.25f, s.ModelScale);
            float height = Mathf.Max(2, s.HipHeight + s.BodySize.Y + 2) * scale;
            collision.Shape = new CapsuleShape3D { Radius = 1.8f * scale, Height = Mathf.Max(height, 3.6f * scale) };
            collision.Position = Vector3.Up * height * 0.5f;
            _visibility = new VisibleOnScreenNotifier3D { Aabb = new Aabb(new Vector3(-7, 0, -7) * scale, new Vector3(14, 13, 14) * scale) };
            AddChild(_visibility);
            _patrol = new PatrolController { Name = "Patrol" }; AddChild(_patrol);
            _sensor = new ScanSensor { Name = "ScanSensor" }; AddChild(_sensor);
            _beam = GD.Load<PackedScene>("res://ENTITIES/TYPES/Robots/Shared/Scanning/ScanBeam.tscn").Instantiate<ScanBeam>();
            SensorSocket.AddChild(_beam);
            _beam.Scale = Vector3.One / Mathf.Max(0.001f, SensorSocket.GlobalBasis.X.Length());
            _beam.Configure(s.Sensor.Range, s.Sensor.FullAngleDegrees, s.SensorColor, s.BeamOpacity, s.BeamBrightness, s.LineSpeed, s.BandContrast);
            _stageTimer = new Timer { Name = "EncounterStage", OneShot = true }; AddChild(_stageTimer);
            _preview = s.PreviewWalking; _lastAnimateLegs = s.AnimateLegs; _built = true;
        }
        _visibility.ScreenEntered += OnScreenEntered; _visibility.ScreenExited += OnScreenExited;
        VisibilityChanged += OnVisibilityChanged;
        _patrol.SpeedChanged += SetMotionSpeed;
        _patrol.WalkingStarted += OnWalking;
        _patrol.Arrived += OnArrived;
        _patrol.ReadyToDepart += BeginScan;
        _stageTimer.Timeout += OnStageTimeout;
        _legs!.FootPlanted += OnFootPlanted;
        base._Ready();
    }

    // =========================================================
    // Inject the route, optional height sampler and explicit target; scene paths stay in the world owner.
    public void InitializeEncounter(Vector3[] route, System.Func<Vector3, Vector3>? groundTarget, Node3D? candidate)
    {
        _preview = false;
        _legs!.ResolveFootTarget = groundTarget; _legs.ResetPose();
        _patrol.Initialize(this, RuntimeRobotSettings.Patrol, route, groundTarget);
        _sensor.Initialize(this, SensorSocket, RuntimeRobotSettings.Sensor);
        if (candidate != null) _sensor.RegisterCandidate(candidate);
        _encounter = true;
        if (IsActive && RuntimeRobotSettings.EncounterEnabled) _patrol.StartPatrol();
    }

    // =========================================================
    // Inspector/F9 updates are event-driven; no saved master is mutated and no route is restarted.
    public void ApplyRuntimeSettings()
    {
        var s = RuntimeRobotSettings;
        _patrol.ApplySettings(s.Patrol); _sensor.ApplySettings(s.Sensor);
        if (IsActive && Stage == "Scanning")
        {
            _sensor.StartScan();
            _stageTimer.Start(Mathf.Max(0.01f, s.ScanSeconds - _scanElapsed));
        }
        _beam.Configure(s.Sensor.Range, s.Sensor.FullAngleDegrees, s.SensorColor, s.BeamOpacity, s.BeamBrightness, s.LineSpeed, s.BandContrast);
        _beam.SetScanning(IsActive && Stage == "Scanning" && s.BeamEnabled);
        _audio.Configure(s.AudioEnabled, s.AudioVolumeDb);
        _audio.Scan(IsActive && _relevant && Stage == "Scanning" && s.BeamEnabled);
        _legs!.StrideLength = s.StrideLength; _legs.CyclesPerSecond = s.GaitSpeed;
        _legs.Lift = s.LegLift; _legs.StanceFraction = s.StanceFraction;
        if (_lastAnimateLegs != s.AnimateLegs && VisualRoot.IsInsideTree()) _legs.ResetPose();
        _lastAnimateLegs = s.AnimateLegs;
        RefreshProcessing();
    }

    // =========================================================
    // Scale is staged behind an Apply button so dragging a slider never repeatedly rebuilds/settles art.
    public void ApplyModelScale()
    {
        bool active = IsActive; if (active) Deactivate();
        float scale = Mathf.Clamp(RuntimeRobotSettings.ModelScale, 0.25f, 4);
        VisualRoot.Scale = Vector3.One * scale;
        _beam.Scale = Vector3.One / Mathf.Max(0.001f, SensorSocket.GlobalBasis.X.Length());
        var collision = GetNode<CollisionShape3D>("CollisionShape3D");
        float height = Mathf.Max(2, RuntimeRobotSettings.HipHeight + RuntimeRobotSettings.BodySize.Y + 2) * scale;
        collision.Shape = new CapsuleShape3D { Radius = 1.8f * scale, Height = Mathf.Max(height, 3.6f * scale) };
        collision.Position = Vector3.Up * height * 0.5f;
        _visibility.Aabb = new Aabb(new Vector3(-7, 0, -7) * scale, new Vector3(14, 13, 14) * scale);
        if (active) Activate(); else _legs?.ResetPose();
    }

    // =========================================================
    // Debug scan pauses the current route without resetting its waypoint; repeated requests are harmless.
    public void ForceScan()
    { if (IsActive && _encounter && Stage != "Scanning" && Stage != "Recovering") BeginScan(); }

    // =========================================================
    // Landings wake a one-shot spatial sound only while art is active/relevant.
    private void OnFootPlanted()
    { if (IsActive && _relevant && RuntimeRobotSettings.AnimateLegs) _audio.Step(); }

    // =========================================================
    // Nonvisual candidates are explicitly registered, independently of beam visibility.
    public ScanSensor Sensor => _sensor;

    // =========================================================
    // Debug treadmill preview changes only the rig, never the physics root position.
    public void SetWalkPreview(bool enabled)
    {
        if (_preview == enabled) return;
        _preview = enabled;
        if (!enabled && _motionSpeed <= 0) _legs?.BeginSettle();
        RefreshProcessing();
    }

    // =========================================================
    // Future patrol passes actual speed here on motion changes; zero starts a bounded settle.
    public void SetMotionSpeed(float metresPerSecond)
    {
        float next = Mathf.Max(0, metresPerSecond);
        if (_motionSpeed == next) return;
        _motionSpeed = next;
        if (next == 0 && !_preview) _legs?.BeginSettle();
        RefreshProcessing();
    }

    // =========================================================
    // Relevance can also be controlled explicitly by later distance integration.
    public void SetVisualRelevant(bool relevant)
    {
        if (relevant && !_relevant && IsActive && VisualRoot.IsInsideTree()) _legs?.ResetPose();
        _relevant = relevant; _audio.Scan(relevant && IsActive && Stage == "Scanning" && RuntimeRobotSettings.BeamEnabled); RefreshProcessing();
    }

    // =========================================================
    // Head rotation is independent of the body and clamped to the configured hardware limit.
    public void SetHeadYaw(float degrees)
    {
        float limit = Mathf.Abs(RuntimeRobotSettings.HeadYawLimitDegrees);
        HeadPivot.Rotation = new Vector3(0, Mathf.DegToRad(Mathf.Clamp(degrees, -limit, limit)), 0);
    }

    // =========================================================
    // Single active callback drives every leg; stopped, hidden or irrelevant art does no animation work.
    public override void _Process(double delta)
    {
        float dt = (float)delta;
        if (RuntimeRobotSettings.AnimateLegs && _relevant && IsVisibleInTree() && (_preview || _motionSpeed > 0 || (_legs?.IsSettling ?? false)))
            _legs?.Tick(dt, _preview ? 1 : _motionSpeed / 2, _preview);
        if (Stage == "Scanning")
        {
            _scanElapsed += dt;
            var s = RuntimeRobotSettings;
            float yaw = Mathf.Sin(_scanElapsed * Mathf.Tau * s.SweepCyclesPerSecond - Mathf.Pi * 0.5f) * s.SweepDegrees * 0.5f;
            SetHeadYaw(yaw);
            HeadPivot.Rotation = new Vector3(Mathf.DegToRad(s.ScanPitchDegrees), HeadPivot.Rotation.Y, 0);
        }
        else if (Stage == "Recovering")
        {
            float fraction = (float)(_stageTimer.TimeLeft / Mathf.Max(0.01f, RuntimeRobotSettings.RecoverySeconds));
            HeadPivot.Rotation = _recoveryRotation * fraction;
        }
        RefreshProcessing();
    }

    private Vector3 _recoveryRotation;

    // =========================================================
    // Arrival is owned by generic patrol; the robot only reports the visual encounter stage.
    private void OnArrived(int waypoint) => SetStage("Settling");

    // =========================================================
    // Patrol's finished dwell requests a scan; pausing here preserves the current waypoint.
    private void BeginScan()
    {
        if (!IsActive || !_encounter) return;
        _patrol.PausePatrol(); _scanElapsed = 0;
        SetHeadYaw(-RuntimeRobotSettings.SweepDegrees * 0.5f);
        HeadPivot.Rotation = new Vector3(Mathf.DegToRad(RuntimeRobotSettings.ScanPitchDegrees), HeadPivot.Rotation.Y, 0);
        _beam.SetScanning(RuntimeRobotSettings.BeamEnabled); _sensor.StartScan(); _audio.Scan(_relevant && RuntimeRobotSettings.BeamEnabled); SetStage("Scanning");
        _stageTimer.Start(Mathf.Max(0.1f, RuntimeRobotSettings.ScanSeconds)); RefreshProcessing();
    }

    // =========================================================
    // One bounded timer owns scan/recovery durations; no duplicate subscriptions on each cycle.
    private void OnStageTimeout()
    {
        if (Stage == "Scanning")
        {
            _beam.SetScanning(false); _sensor.StopScan(); _audio.Scan(false);
            _recoveryRotation = HeadPivot.Rotation; SetStage("Recovering");
            _stageTimer.Start(Mathf.Max(0.01f, RuntimeRobotSettings.RecoverySeconds));
        }
        else if (Stage == "Recovering")
        {
            HeadPivot.Rotation = Vector3.Zero; SetStage("Walking"); _patrol.ResumePatrol();
        }
        RefreshProcessing();
    }

    // =========================================================
    // Keep the coordinator's status event-driven rather than maintaining a polling debug UI.
    private void SetStage(string stage)
    { if (Stage != stage) { Stage = stage; StageChanged?.Invoke(stage); } }

    // =========================================================
    // Generic motion events drive the gait; patrol never touches robot limbs.
    private void OnWalking() { HeadPivot.Rotation = Vector3.Zero; SetStage("Walking"); }

    // =========================================================
    // Activation resumes a valid stance; no timers or external subscriptions are needed.
    protected override void OnActivated()
    {
        _legs?.ResetPose();
        if (_encounter && RuntimeRobotSettings.EncounterEnabled)
        {
            if (_patrol.State == PatrolController.PatrolState.Stopped) _patrol.StartPatrol();
            else if (_patrol.State == PatrolController.PatrolState.Paused) _patrol.ResumePatrol();
        }
        RefreshProcessing();
    }

    // =========================================================
    // Hard disable freezes a settled neutral rig immediately and sleeps without a fade callback.
    protected override void OnDeactivated()
    {
        _stageTimer.Stop(); _beam.SetScanning(false); _sensor.StopScan(); _audio.Stop();
        if (VisualRoot.IsInsideTree()) _legs?.ResetPose();
        // Interrupted scans restart as a normal departure; there is no stale timer or visible beam.
        if (Stage == "Scanning" || Stage == "Recovering") SetStage("Paused");
        SetProcess(false);
    }

    // =========================================================
    // Release local signal connections before a possible remove/re-add cycle.
    protected override void OnEntityExitTree()
    {
        _visibility.ScreenEntered -= OnScreenEntered; _visibility.ScreenExited -= OnScreenExited;
        VisibilityChanged -= OnVisibilityChanged;
        _patrol.SpeedChanged -= SetMotionSpeed;
        _patrol.WalkingStarted -= OnWalking; _patrol.Arrived -= OnArrived;
        _patrol.ReadyToDepart -= BeginScan; _stageTimer.Timeout -= OnStageTimeout;
        _legs!.FootPlanted -= OnFootPlanted;
        _stageTimer.Stop(); _beam.SetScanning(false); _sensor.StopScan(); _audio.Stop();
        _encounter = false;
    }

    // =========================================================
    // Offscreen visual suspension never changes physics or logical actor state.
    private void OnScreenEntered() => SetVisualRelevant(true);

    // =========================================================
    // Cached foot poses remain available when rendering resumes.
    private void OnScreenExited() => SetVisualRelevant(false);

    // =========================================================
    // Hide/show respects explicit actor lifecycle and cancels active encounter work immediately.
    private void OnVisibilityChanged()
    {
        if (!IsVisibleInTree()) { _hiddenWasActive = IsActive; Deactivate(); }
        else if (_hiddenWasActive) { _hiddenWasActive = false; Activate(); }
        RefreshProcessing();
    }

    // =========================================================
    // Logical head sweep runs during scans; gait alone is visibility-gated.
    private void RefreshProcessing() => SetProcess(IsActive && (Stage == "Scanning" || Stage == "Recovering"
        || (RuntimeRobotSettings.AnimateLegs && _relevant && IsVisibleInTree() && (_preview || _motionSpeed > 0 || (_legs?.IsSettling ?? false)))));
}
