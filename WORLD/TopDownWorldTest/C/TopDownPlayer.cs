// Test-only 1.8m low-poly human: physical walking and one visual gait driver, dormant when grounded/idle.
using Godot;

public partial class TopDownPlayer : CharacterBody3D
{
    public event System.Action<Vector3>? PositionChanged;
    private static readonly BoxMesh Box = new() { Size = Vector3.One };
    private TopDownTestSettings _settings = null!;
    private Node3D _art = null!, _leftArm = null!, _rightArm = null!, _leftLeg = null!, _rightLeg = null!;
    private Vector3 _screenRight, _screenForward;
    private float _phase;
    private bool _initialized;

    // =========================================================
    // The test world places the player before enabling physics; no spawn-time fall without terrain data.
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); }

    // =========================================================
    // Cache ground-plane camera axes and build a small original model once.
    public void Initialize(TopDownTestSettings settings, Camera3D camera)
    {
        _settings = settings;
        _screenRight = camera.GlobalBasis.X; _screenRight.Y = 0; _screenRight = _screenRight.Normalized();
        _screenForward = -camera.GlobalBasis.Z; _screenForward.Y = 0; _screenForward = _screenForward.Normalized();
        FloorSnapLength = 0.5f; FloorMaxAngle = Mathf.DegToRad(46);
        if (!_initialized) BuildHuman();
        _initialized = true; Velocity = Vector3.Zero; SetPhysicsProcess(true);
    }

    // =========================================================
    // Keyboard changes wake movement/gravity. No new project-wide actions or explorer controls are introduced.
    public override void _UnhandledInput(InputEvent input)
    {
        if (!_initialized || input is not InputEventKey key || key.Echo) return;
        if (key.PhysicalKeycode == Key.W || key.PhysicalKeycode == Key.A || key.PhysicalKeycode == Key.S
            || key.PhysicalKeycode == Key.D || key.PhysicalKeycode == Key.Shift)
            SetPhysicsProcess(true);
    }

    // =========================================================
    // Ground contact sleeps again after focus returns; no held direction is allowed while the window is unfocused.
    public override void _Notification(int what)
    {
        if (_initialized && (what == NotificationApplicationFocusOut || what == NotificationApplicationFocusIn)) SetPhysicsProcess(true);
    }

    // =========================================================
    // Physics root owns movement. Art joints animate from actual horizontal travel and settle immediately at rest.
    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        Vector2 keys = GetWindow().HasFocus() ? new Vector2(
            (Input.IsPhysicalKeyPressed(Key.D) ? 1 : 0) - (Input.IsPhysicalKeyPressed(Key.A) ? 1 : 0),
            (Input.IsPhysicalKeyPressed(Key.W) ? 1 : 0) - (Input.IsPhysicalKeyPressed(Key.S) ? 1 : 0)) : Vector2.Zero;
        keys = keys.Normalized();
        Vector3 direction = (_screenRight * keys.X + _screenForward * keys.Y).Normalized();
        float speed = Mathf.Max(0.1f, _settings.WalkSpeed) * (Input.IsPhysicalKeyPressed(Key.Shift) ? Mathf.Max(1, _settings.RunMultiplier) : 1);
        Vector3 horizontal = new(Velocity.X, 0, Velocity.Z);
        horizontal = horizontal.MoveToward(direction * speed, Mathf.Max(1, _settings.Acceleration) * dt);
        Vector3 before = GlobalPosition;
        Velocity = new Vector3(horizontal.X, IsOnFloor() ? -0.1f : Velocity.Y - Mathf.Max(1, _settings.Gravity) * dt, horizontal.Z);
        MoveAndSlide();
        Vector3 travel = GlobalPosition - before; travel.Y = 0;
        float actualSpeed = dt > 0 ? travel.Length() / dt : 0;
        if (actualSpeed > 0.05f)
        {
            float yaw = Mathf.Atan2(-travel.X, -travel.Z);
            _art.Rotation = new Vector3(0, Mathf.LerpAngle(_art.Rotation.Y, yaw, 1 - Mathf.Exp(-14 * dt)), 0);
            _phase += dt * Mathf.Tau * Mathf.Max(0.1f, _settings.GaitRate) * actualSpeed / Mathf.Max(0.1f, _settings.WalkSpeed);
            Pose(Mathf.Sin(_phase) * 0.5f, Mathf.Abs(Mathf.Sin(_phase * 2)) * 0.025f);
        }
        else Pose(0, 0);
        if (before.DistanceSquaredTo(GlobalPosition) > 0.0000001f) PositionChanged?.Invoke(GlobalPosition);
        if (IsOnFloor() && keys == Vector2.Zero && horizontal.LengthSquared() < 0.0001f)
        { Velocity = Vector3.Zero; _phase = 0; Pose(0, 0); SetPhysicsProcess(false); }
    }

    // =========================================================
    // One method moves all four joints; no limb scripts, AnimationTree framework or idle animation clock.
    private void Pose(float angle, float bob)
    {
        _art.Position = Vector3.Up * bob;
        _leftArm.Rotation = new Vector3(-angle, 0, 0); _rightArm.Rotation = new Vector3(angle, 0, 0);
        _leftLeg.Rotation = new Vector3(angle, 0, 0); _rightLeg.Rotation = new Vector3(-angle, 0, 0);
    }

    // =========================================================
    // Shared box geometry and four simple materials keep the visible test human cheap and replaceable.
    private void BuildHuman()
    {
        _art = new Node3D { Name = "VisualRoot" }; AddChild(_art);
        var shirt = new StandardMaterial3D { AlbedoColor = new Color(0.1f, 0.55f, 0.62f), Roughness = 0.9f };
        var skin = new StandardMaterial3D { AlbedoColor = new Color(0.76f, 0.58f, 0.4f), Roughness = 1 };
        var trousers = new StandardMaterial3D { AlbedoColor = new Color(0.34f, 0.3f, 0.23f), Roughness = 1 };
        var boots = new StandardMaterial3D { AlbedoColor = new Color(0.1f, 0.12f, 0.14f), Roughness = 1 };
        Part(_art, "Torso", new(0, 1.12f, 0), new(0.5f, 0.6f, 0.3f), shirt);
        Part(_art, "Head", new(0, 1.62f, 0), new(0.32f, 0.36f, 0.32f), skin);
        Part(_art, "Face", new(0, 1.62f, -0.17f), new(0.23f, 0.12f, 0.04f), skin);
        _leftArm = Limb("LeftArm", new(-0.34f, 1.35f, 0)); _rightArm = Limb("RightArm", new(0.34f, 1.35f, 0));
        foreach (var arm in new[] { _leftArm, _rightArm })
        {
            Part(arm, "Sleeve", new(0, -0.18f, 0), new(0.18f, 0.36f, 0.22f), shirt);
            Part(arm, "Hand", new(0, -0.43f, 0), new(0.15f, 0.2f, 0.17f), skin);
        }
        _leftLeg = Limb("LeftLeg", new(-0.14f, 0.85f, 0)); _rightLeg = Limb("RightLeg", new(0.14f, 0.85f, 0));
        foreach (var leg in new[] { _leftLeg, _rightLeg })
        {
            Part(leg, "Trousers", new(0, -0.31f, 0), new(0.21f, 0.62f, 0.25f), trousers);
            Part(leg, "Boot", new(0, -0.71f, -0.06f), new(0.24f, 0.25f, 0.4f), boots);
        }
    }

    // =========================================================
    // Pivot placement is cached at creation, never searched in a frame callback.
    private Node3D Limb(string name, Vector3 position)
    { var pivot = new Node3D { Name = name, Position = position }; _art.AddChild(pivot); return pivot; }

    // =========================================================
    // Art meshes contain no collision or update loops; the root capsule handles all physical movement.
    private static void Part(Node3D parent, string name, Vector3 position, Vector3 size, Material material)
    { parent.AddChild(new MeshInstance3D { Name = name, Mesh = Box, Position = position, Scale = size, MaterialOverride = material }); }
}
