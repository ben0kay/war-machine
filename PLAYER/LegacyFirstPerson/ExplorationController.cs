// Shared explorer: event-driven inspection flight or a collidable first-person capsule, without a visible body.
using Godot;

public partial class ExplorationController : CharacterBody3D
{
    public event System.Action<Vector3>? ViewPositionChanged;
    public event System.Action<bool>? CameraModeChanged;
    [Export] public PlayerConfig Settings { get; set; } = null!;
    public Node3D? Terrain { get; set; }
    public bool ControlsBlocked { get; set; }
    public bool FirstPerson { get; private set; }
    private Camera3D _camera = null!;
    private CollisionShape3D _shape = null!;
    private float _pitch;

    // =========================================================
    // A disabled capsule preserves free-flight behaviour until first-person mode is selected.
    public override void _Ready()
    {
        SetNotifyTransform(true); _camera = GetNode<Camera3D>("Camera3D"); _shape = GetNode<CollisionShape3D>("CollisionShape3D");
        _shape.Shape = new CapsuleShape3D { Radius = Settings.CapsuleRadius, Height = Settings.BodyHeight };
        _shape.Position = Vector3.Up * Settings.BodyHeight * 0.5f;
        _camera.Fov = Settings.FieldOfView; _camera.Far = Settings.FarDistance;
        FloorSnapLength = 0.35f; FloorMaxAngle = Mathf.DegToRad(46);
        Input.MouseMode = Input.MouseModeEnum.Captured; SetPhysicsProcess(false);
    }

    // =========================================================
    // Enter walking mode on the nearest collidable ground below the current camera, checking headroom first.
    public void ToggleCameraMode()
    {
        if (ControlsBlocked) return;
        if (!FirstPerson)
        {
            var ray = PhysicsRayQueryParameters3D.Create(_camera.GlobalPosition + Vector3.Up * 0.1f, _camera.GlobalPosition + Vector3.Down * 2000f, CollisionMask);
            ray.Exclude = new Godot.Collections.Array<Rid> { GetRid() };
            var hit = GetWorld3D().DirectSpaceState.IntersectRay(ray);
            Vector3 feet;
            if (hit.Count > 0) feet = hit["position"].AsVector3() + Vector3.Up * 0.08f;
            else
            {
                // Dynamic Terrain3D collision can be outside reach while flying high above it.
                if (Terrain == null) return;
                feet = _camera.GlobalPosition;
                float height = Terrain.Get("data").AsGodotObject().Call("get_height", feet).AsSingle();
                if (!float.IsFinite(height) || feet.Y < height) { GD.Print("No safe ground below camera."); return; }
                feet.Y = height + 0.08f;
            }
            var check = new PhysicsShapeQueryParameters3D { Shape = _shape.Shape, Transform = new Transform3D(Basis.Identity, feet + Vector3.Up * Settings.BodyHeight * 0.5f), CollisionMask = CollisionMask,
                Exclude = new Godot.Collections.Array<Rid> { GetRid() } };
            if (GetWorld3D().DirectSpaceState.IntersectShape(check, 1).Count > 0)
            { GD.Print("Insufficient space for player here. Move the free camera to clear ground."); return; }
            GlobalPosition = feet; _camera.Position = Vector3.Up * Settings.EyeHeight;
        }
        else { GlobalPosition = _camera.GlobalPosition; _camera.Position = Vector3.Zero; }
        FirstPerson = !FirstPerson; _shape.SetDeferred(CollisionShape3D.PropertyName.Disabled, !FirstPerson);
        Velocity = Vector3.Zero; SetPhysicsProcess(FirstPerson); CameraModeChanged?.Invoke(FirstPerson);
    }

    // =========================================================
    // Input wakes motion; F6 switches modes and Space jumps only in first-person mode.
    public override void _UnhandledInput(InputEvent input)
    {
        if (ControlsBlocked) return;
        if (input is InputEventKey key && key.Pressed && !key.Echo)
        {
            if (key.Keycode == Key.F6) { ToggleCameraMode(); GetViewport().SetInputAsHandled(); return; }
            if (key.Keycode == Key.Escape) Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Captured ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Captured;
            if (FirstPerson && key.Keycode == Key.Space && IsOnFloor() && Input.MouseMode == Input.MouseModeEnum.Captured)
                Velocity = new Vector3(Velocity.X, Settings.JumpSpeed, Velocity.Z);
        }
        if (input is InputEventMouseMotion motion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            RotateY(-motion.Relative.X * Settings.MouseSensitivity);
            _pitch = Mathf.Clamp(_pitch - motion.Relative.Y * Settings.MouseSensitivity, -1.5f, 1.5f);
            _camera.Rotation = new Vector3(_pitch, 0, 0);
        }
        if (input is InputEventKey || input is InputEventMouseButton)
            SetPhysicsProcess(FirstPerson || (Input.MouseMode == Input.MouseModeEnum.Captured && Direction() != Vector3.Zero));
    }

    // =========================================================
    // Walking processes only while moving/falling; resting on the floor sleeps until input wakes it.
    public override void _PhysicsProcess(double delta)
    {
        bool control = !ControlsBlocked && Input.MouseMode == Input.MouseModeEnum.Captured && GetWindow().HasFocus();
        if (FirstPerson)
        {
            Vector3 direction = control ? Direction() : Vector3.Zero; direction.Y = 0; direction = direction.Normalized();
            float speed = Settings.WalkSpeed * (control && Input.IsPhysicalKeyPressed(Key.Shift) ? Settings.SprintMultiplier : 1);
            Velocity = new Vector3(direction.X * speed, Velocity.Y - Settings.Gravity * (float)delta, direction.Z * speed);
            MoveAndSlide();
            if (IsOnFloor() && direction == Vector3.Zero) { Velocity = Vector3.Zero; SetPhysicsProcess(false); }
            return;
        }
        Vector3 flight = control ? Direction() : Vector3.Zero;
        if (flight == Vector3.Zero) { SetPhysicsProcess(false); return; }
        GlobalPosition += flight.Normalized() * Settings.MoveSpeed * (Input.IsPhysicalKeyPressed(Key.Shift) ? Settings.BoostMultiplier : 1) * (float)delta;
    }

    // =========================================================
    public override void _Notification(int what)
    { if (what == NotificationTransformChanged && IsInsideTree()) ViewPositionChanged?.Invoke(GlobalPosition); }

    // =========================================================
    private Vector3 Direction()
    {
        Vector3 direction = Vector3.Zero;
        Vector3 forward = FirstPerson ? GlobalBasis.Z : _camera.GlobalBasis.Z;
        if (Input.IsPhysicalKeyPressed(Key.W) || Input.IsPhysicalKeyPressed(Key.Up)) direction -= forward;
        if (Input.IsPhysicalKeyPressed(Key.S) || Input.IsPhysicalKeyPressed(Key.Down)) direction += forward;
        if (Input.IsPhysicalKeyPressed(Key.A) || Input.IsPhysicalKeyPressed(Key.Left)) direction -= GlobalBasis.X;
        if (Input.IsPhysicalKeyPressed(Key.D) || Input.IsPhysicalKeyPressed(Key.Right)) direction += GlobalBasis.X;
        if (!FirstPerson)
        {
            if (Input.IsPhysicalKeyPressed(Key.E) || Input.IsPhysicalKeyPressed(Key.Space) || Input.IsMouseButtonPressed(MouseButton.Right)) direction += Vector3.Up;
            if (Input.IsPhysicalKeyPressed(Key.Q) || Input.IsPhysicalKeyPressed(Key.Ctrl)) direction -= Vector3.Up;
        }
        return direction;
    }
}
