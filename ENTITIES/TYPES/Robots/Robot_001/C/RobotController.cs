// Robot_001 art assembly and explicit gait preview only. Patrol and scanning are later phases.
using Godot;

public partial class RobotController : EntityCore
{
    [Export] public RobotSettings RobotMaster { get; set; } = null!;
    public RobotSettings RuntimeRobotSettings { get; private set; } = null!;
    public Node3D HeadPivot { get; private set; } = null!;
    public Node3D SensorSocket { get; private set; } = null!;
    private MechanicalLegs? _legs;
    private VisibleOnScreenNotifier3D _visibility = null!;
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
            var collision = GetNode<CollisionShape3D>("CollisionShape3D");
            float scale = Mathf.Max(0.25f, s.ModelScale);
            float height = Mathf.Max(2, s.HipHeight + s.BodySize.Y + 2) * scale;
            collision.Shape = new CapsuleShape3D { Radius = 1.8f * scale, Height = Mathf.Max(height, 3.6f * scale) };
            collision.Position = Vector3.Up * height * 0.5f;
            _visibility = new VisibleOnScreenNotifier3D { Aabb = new Aabb(new Vector3(-7, 0, -7) * scale, new Vector3(14, 13, 14) * scale) };
            AddChild(_visibility);
            _preview = s.PreviewWalking; _built = true;
        }
        _visibility.ScreenEntered += OnScreenEntered; _visibility.ScreenExited += OnScreenExited;
        VisibilityChanged += RefreshProcessing;
        base._Ready();
    }

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
    { _relevant = relevant; RefreshProcessing(); }

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
        _legs?.Tick((float)delta, _preview ? 1 : _motionSpeed / 2, _preview);
        RefreshProcessing();
    }

    // =========================================================
    // Activation resumes a valid stance; no timers or external subscriptions are needed.
    protected override void OnActivated() { _legs?.ResetPose(); RefreshProcessing(); }

    // =========================================================
    // Hard disable freezes a settled neutral rig immediately and sleeps without a fade callback.
    protected override void OnDeactivated() { _legs?.ResetPose(); SetProcess(false); }

    // =========================================================
    // Release local signal connections before a possible remove/re-add cycle.
    protected override void OnEntityExitTree()
    {
        _visibility.ScreenEntered -= OnScreenEntered; _visibility.ScreenExited -= OnScreenExited;
        VisibilityChanged -= RefreshProcessing;
    }

    // =========================================================
    // Offscreen visual suspension never changes physics or logical actor state.
    private void OnScreenEntered() => SetVisualRelevant(true);

    // =========================================================
    // Cached foot poses remain available when rendering resumes.
    private void OnScreenExited() => SetVisualRelevant(false);

    // =========================================================
    // Wake only for requested motion/preview or the short settling interval.
    private void RefreshProcessing() => SetProcess(IsActive && _relevant && IsVisibleInTree()
        && (_preview || _motionSpeed > 0 || (_legs?.IsSettling ?? false)));
}
