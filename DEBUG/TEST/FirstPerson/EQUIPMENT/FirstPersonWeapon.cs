// Centre-screen aim adapter. Reuses the actual firearm, cooldown, muzzle obstruction and damage pipeline.
using Godot;

public partial class FirstPersonWeapon : Node
{
    private PlayerController _player = null!;
    private FirstPersonCamera _camera = null!;
    private FirearmController? _weapon;
    private WeaponSlots _slots = null!;
    private PhysicsRayQueryParameters3D _query = null!;
    private bool _held, _requested;

    // =========================================================
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); }

    // =========================================================
    // The same rifle is a real world-space mesh, so nearby scenery can still obstruct its muzzle.
    public void Initialize(PlayerController player, FirstPersonCamera camera, EquipmentConfig equipment)
    {
        _player = player; _camera = camera;
        if (!equipment.Enabled || equipment.DefaultWeaponScene == null) return;
        _query = new PhysicsRayQueryParameters3D { HitFromInside = true,
            Exclude = new Godot.Collections.Array<Rid> { player.GetRid() } };
        _slots = new WeaponSlots { Name = "WeaponSlots" }; AddChild(_slots);
        _slots.Initialize(equipment, player, camera, camera.TestSettings.WeaponOffset);
        SwitchWeapon(0);
    }

    // =========================================================
    private void SwitchWeapon(int slot)
    {
        var previous = _weapon;
        if (!_slots.Select(slot)) return;
        CancelTrigger();
        if (IsInstanceValid(previous)) previous!.ReadyToFire -= OnReadyToFire;
        _weapon = _slots.Current!; _weapon.ReadyToFire += OnReadyToFire;
        _query.CollisionMask = _weapon.RuntimeSettings.HitMask;
    }

    // =========================================================
    // Observe release even when another UI/input adapter consumes it. Escape always cancels a held trigger.
    public override void _Input(InputEvent input)
    {
        if (input is InputEventMouseButton mouse && mouse.ButtonIndex == MouseButton.Left && !mouse.Pressed) _held = false;
        if (input is InputEventKey key && key.Pressed && key.PhysicalKeycode == Key.Escape) CancelTrigger();
    }

    // =========================================================
    // An uncaptured click belongs to the camera and cannot become a shot.
    public override void _UnhandledInput(InputEvent input)
    {
        if (_weapon == null || Input.MouseMode != Input.MouseModeEnum.Captured) return;
        int slot = WeaponSlots.RequestedSlot(input);
        if (slot >= 0) { SwitchWeapon(slot); GetViewport().SetInputAsHandled(); return; }
        if (input is InputEventMouseButton mouse && mouse.ButtonIndex == MouseButton.Left && mouse.Pressed)
        { _held = true; RequestShot(); GetViewport().SetInputAsHandled(); }
    }

    // =========================================================
    private void RequestShot() { _requested = true; SetPhysicsProcess(true); }
    private void OnReadyToFire() { if (_held && _weapon != null && _weapon.RuntimeSettings.Automatic) RequestShot(); }
    private void CancelTrigger() { _held = false; _requested = false; SetPhysicsProcess(false); }

    // =========================================================
    // Queries happen only for actual requested shots, never every frame while merely looking around.
    public override void _PhysicsProcess(double delta)
    {
        SetPhysicsProcess(false);
        if (!_requested || !IsInstanceValid(_weapon) || !GetWindow().HasFocus() || Input.MouseMode != Input.MouseModeEnum.Captured)
        { CancelTrigger(); return; }
        _requested = false;
        Vector2 centre = _camera.GetViewport().GetVisibleRect().Size * 0.5f;
        Vector3 origin = _camera.ProjectRayOrigin(centre), direction = _camera.ProjectRayNormal(centre);
        _query.From = origin; _query.To = origin + direction * _weapon!.RuntimeSettings.Range;
        var hit = _player.GetWorld3D().DirectSpaceState.IntersectRay(_query);
        Vector3 aim = hit.Count > 0 ? hit["position"].AsVector3() : _query.To;
        _weapon.TryFire(aim, origin);
    }

    // =========================================================
    public override void _Notification(int what)
    { if (what == NotificationApplicationFocusOut) { CancelTrigger(); if (IsInstanceValid(_weapon)) _weapon!.CancelFeedback(); } }

    // =========================================================
    public override void _ExitTree()
    {
        CancelTrigger();
        if (IsInstanceValid(_weapon)) { _weapon!.ReadyToFire -= OnReadyToFire; }
    }
}
