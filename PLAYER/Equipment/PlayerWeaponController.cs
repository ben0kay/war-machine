// Input/aim adapter only. Actor rig stays in the player; shot mechanics/settings stay in EQUIPMENT.
using Godot;

public partial class PlayerWeaponController : Node
{
    private PlayerController _player = null!;
    private Camera _camera = null!;
    private FirearmController? _weapon;
    private PhysicsRayQueryParameters3D _aimQuery = null!;
    private bool _held, _shotRequested, _aimDirty, _initialized;
    private Vector3 _aimPoint;

    // =========================================================
    // No idle frame callbacks. The world supplies ready actor/camera and a shared equipment definition.
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); }

    // =========================================================
    // Spawn whichever firearm scene is configured; this adapter never knows a rifle path or balance value.
    public void Initialize(PlayerController player, Camera camera, EquipmentConfig config)
    {
        _player = player; _camera = camera;
        if (!config.Enabled || config.DefaultWeaponScene == null) return;
        Node instance = config.DefaultWeaponScene.Instantiate();
        if (instance is not FirearmController firearm)
        { GD.PushError("EquipmentMaster requires a FirearmController root."); instance.Free(); return; }
        _weapon = firearm;
        player.WeaponMount.AddChild(firearm); firearm.Equip(player);
        _aimQuery = new PhysicsRayQueryParameters3D { CollisionMask = firearm.RuntimeSettings.HitMask,
            Exclude = new Godot.Collections.Array<Rid> { player.GetRid() } };
        player.SetWeaponPose(true);
        player.PositionChanged += OnPlayerMoved; player.WeaponPoseChanged += RequestAim;
        camera.ViewChanged += RequestAim; firearm.ReadyToFire += OnReadyToFire;
        _initialized = true; RequestAim();
    }

    // =========================================================
    // Always see release, even if a UI control consumes that release before unhandled-input dispatch.
    public override void _Input(InputEvent input)
    { if (input is InputEventMouseButton mouse && mouse.ButtonIndex == MouseButton.Left && !mouse.Pressed) _held = false; }

    // =========================================================
    // UI gets first refusal on clicks; cursor changes queue one physics-safe aim update.
    public override void _UnhandledInput(InputEvent input)
    {
        if (!_initialized) return;
        if (input is InputEventMouseMotion) RequestAim();
        if (input is InputEventMouseButton mouse && mouse.ButtonIndex == MouseButton.Left && mouse.Pressed)
        { _held = true; _shotRequested = true; RequestAim(); GetViewport().SetInputAsHandled(); }
    }

    // =========================================================
    // Only the currently held trigger requests another shot when its firearm's own timer allows it.
    private void OnReadyToFire()
    { if (_held && _weapon != null && _weapon.RuntimeSettings.Automatic) { _shotRequested = true; RequestAim(); } }

    // =========================================================
    private void OnPlayerMoved(Vector3 position) => RequestAim();

    // =========================================================
    // Movement, crouch interpolation, camera follow/projection and mouse input explicitly wake aiming.
    private void RequestAim() { if (_initialized) { _aimDirty = true; SetPhysicsProcess(true); } }

    // =========================================================
    // One camera ray for changed aim, then the firearm's shot-only obstruction/hit rays. Works in both projections.
    public override void _PhysicsProcess(double delta)
    {
        SetPhysicsProcess(false);
        if (!_initialized || !IsInstanceValid(_weapon) || !GetWindow().HasFocus()) { CancelTrigger(); return; }
        if (GetViewport().GuiGetHoveredControl() != null) { CancelTrigger(); return; }
        if (_aimDirty || _shotRequested)
        {
            _aimDirty = false;
            Vector2 cursor = GetViewport().GetMousePosition();
            Vector3 origin = _camera.ProjectRayOrigin(cursor), direction = _camera.ProjectRayNormal(cursor);
            _aimQuery.From = origin; _aimQuery.To = origin + direction * _camera.Far;
            var hit = _player.GetWorld3D().DirectSpaceState.IntersectRay(_aimQuery);
            // Empty sky/off-terrain uses a plane at the hands' height instead of aiming off to infinity.
            if (hit.Count > 0) _aimPoint = hit["position"].AsVector3();
            else
            {
                float distance = Mathf.Abs(direction.Y) > 0.0001f ? (_player.WeaponMount.GlobalPosition.Y - origin.Y) / direction.Y : -1;
                _aimPoint = distance > 0 ? origin + direction * distance : _player.WeaponMount.GlobalPosition - _player.WeaponMount.GlobalBasis.Z * _weapon!.RuntimeSettings.Range;
            }
            _player.SetWeaponAim(_aimPoint);
        }
        if (_shotRequested) { _shotRequested = false; _weapon!.TryFire(_aimPoint, _player.WeaponProbeOrigin); }
    }

    // =========================================================
    // No stuck firing when tabbing away or moving a held click onto the debug sliders.
    private void CancelTrigger() { _held = false; _shotRequested = false; }

    // =========================================================
    public override void _Notification(int what)
    {
        if (!_initialized) return;
        if (what == NotificationApplicationFocusOut) { CancelTrigger(); _weapon?.CancelFeedback(); SetPhysicsProcess(false); }
        else if (what == NotificationApplicationFocusIn) RequestAim();
    }

    // =========================================================
    // Explicitly release cross-node subscriptions before world unload or replacing this adapter.
    public override void _ExitTree()
    {
        CancelTrigger();
        if (!_initialized) return;
        if (IsInstanceValid(_player)) { _player.PositionChanged -= OnPlayerMoved; _player.WeaponPoseChanged -= RequestAim; _player.SetWeaponPose(false); }
        if (IsInstanceValid(_camera)) _camera.ViewChanged -= RequestAim;
        if (IsInstanceValid(_weapon)) { _weapon!.ReadyToFire -= OnReadyToFire; _weapon.Unequip(); _weapon.QueueFree(); }
        _initialized = false;
    }
}
