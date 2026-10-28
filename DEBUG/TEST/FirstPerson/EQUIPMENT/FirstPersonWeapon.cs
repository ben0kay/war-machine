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
    private PlayerQuickMelee? _shove;
    private FirearmVisualRecoil? _visualRecoil;

    // =========================================================
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); }

    // =========================================================
    // The same rifle is a real world-space mesh, so nearby scenery can still obstruct its muzzle.
    public void Initialize(PlayerController player, FirstPersonCamera camera, EquipmentConfig equipment, WorldAudio? audio = null)
    {
        _player = player; _camera = camera;
        if (!equipment.Enabled || equipment.DefaultWeaponScene == null) return;
        _query = new PhysicsRayQueryParameters3D { HitFromInside = true,
            Exclude = new Godot.Collections.Array<Rid> { player.GetRid() } };
        _slots = new WeaponSlots { Name = "WeaponSlots" }; AddChild(_slots);
        _slots.Initialize(equipment, player, camera, camera.TestSettings.WeaponOffset, audio);
        SwitchWeapon(0);
        if (_weapon != null) { _shove = new PlayerQuickMelee(player); player.GameplayInputBlockChanged += OnInputBlocked; }
    }

    // =========================================================
    private void SwitchWeapon(int slot)
    {
        var previous = _weapon;
        if (!_slots.Select(slot)) return;
        _shove?.Cancel(); CancelTrigger(); CancelVisualRecoil();
        if (IsInstanceValid(previous))
        { previous!.ReadyToFire -= OnReadyToFire; previous.ShotAccepted -= OnShotAccepted; }
        _weapon = _slots.Current!;
        _weapon.ReadyToFire += OnReadyToFire; _weapon.ShotAccepted += OnShotAccepted;
        // =========================================================
        // Each gun may override its FPS view-model fit; root scaling also moves the real muzzle
        // marker consistently, while the shared top-down mount remains unaffected.
        _weapon.Position = _camera.TestSettings.WeaponOffset + _weapon.RuntimeSettings.FirstPersonOffset;
        _weapon.Scale = Vector3.One * Mathf.Clamp(_weapon.RuntimeSettings.FirstPersonScale, 0.4f, 1.5f);
        _visualRecoil = new FirearmVisualRecoil(_weapon);
        _query.CollisionMask = _weapon.RuntimeSettings.HitMask;
    }

    // =========================================================
    // Observe release even when another UI/input adapter consumes it. Escape always cancels a held trigger.
    public override void _Input(InputEvent input)
    {
        if (input is InputEventMouseButton mouse && mouse.ButtonIndex == MouseButton.Left && !mouse.Pressed) _held = false;
        if (input is InputEventKey key && key.Pressed && key.PhysicalKeycode == Key.Escape)
        { CancelTrigger(); _shove?.Cancel(); CancelVisualRecoil(); _weapon?.CancelFeedback(); }
    }

    // =========================================================
    // An uncaptured click belongs to the camera and cannot become a shot.
    public override void _UnhandledInput(InputEvent input)
    {
        if (_weapon == null || _player.GameplayInputBlocked || Input.MouseMode != Input.MouseModeEnum.Captured) return;
        int slot = WeaponSlots.RequestedSlot(input);
        if (slot >= 0) { SwitchWeapon(slot); GetViewport().SetInputAsHandled(); return; }
        if (PlayerQuickMelee.Requested(input))
        {
            if (GetViewport().GuiGetFocusOwner() == null)
            {
                if (_shove?.TryStart(_weapon!, true) == true) CancelVisualRecoil();
                SetPhysicsProcess(true); GetViewport().SetInputAsHandled();
            }
            return;
        }
        if (input is InputEventMouseButton mouse && mouse.ButtonIndex == MouseButton.Left && mouse.Pressed)
        { _held = true; RequestShot(); GetViewport().SetInputAsHandled(); }
    }

    // =========================================================
    // A real accepted shot starts one additive model pulse, without changing camera look.
    private void OnShotAccepted()
    {
        _visualRecoil?.Kick();
        if (_visualRecoil?.IsActive == true) SetProcess(true);
    }

    // =========================================================
    // Cosmetic recovery is active-only. The shared firearm independently recovers its ballistic state.
    public override void _Process(double delta)
    {
        if (_visualRecoil?.IsActive != true) { SetProcess(false); return; }
        _visualRecoil.Advance((float)delta);
        if (!_visualRecoil.IsActive) SetProcess(false);
    }

    // =========================================================
    private void CancelVisualRecoil()
    { _visualRecoil?.Cancel(); SetProcess(false); }

    // =========================================================
    private void RequestShot() { _requested = true; SetPhysicsProcess(true); }
    private void OnReadyToFire() { if (_held && _weapon != null && _weapon.RuntimeSettings.Automatic) RequestShot(); }
    private void CancelTrigger() { _held = false; _requested = false; SetPhysicsProcess(false); }

    // =========================================================
    // Queries happen only for actual requested shots, never every frame while merely looking around.
    public override void _PhysicsProcess(double delta)
    {
        SetPhysicsProcess(false);
        if (!IsInstanceValid(_weapon) || _player.GameplayInputBlocked || !GetWindow().HasFocus() || Input.MouseMode != Input.MouseModeEnum.Captured
            || GetViewport().GuiGetFocusOwner() != null)
        { CancelTrigger(); _shove?.Cancel(); CancelVisualRecoil(); _weapon?.CancelFeedback(); return; }
        Vector2 centre = _camera.GetViewport().GetVisibleRect().Size * 0.5f;
        Vector3 origin = _camera.ProjectRayOrigin(centre), direction = _camera.ProjectRayNormal(centre);
        if (_requested && _shove?.IsActive != true)
        {
            _requested = false;
            _query.From = origin; _query.To = origin + direction * _weapon!.RuntimeSettings.Range;
            var hit = _player.GetWorld3D().DirectSpaceState.IntersectRay(_query);
            Vector3 aim = hit.Count > 0 ? hit["position"].AsVector3() : _query.To;
            // RC2: camera's up axis rotates the *real 3D shot ray* before random spread.
            // Keep the crosshair and mouse-look unchanged; don't add extra camera recoil.
            _weapon!.TryFire(aim, origin, _camera.GlobalBasis.Y);
        }
        if (_shove?.IsActive == true)
        {
            // Character-height origin with the FPS camera's full yaw/pitch direction.
            float height = Mathf.Clamp(_camera.GlobalPosition.Y - _player.GlobalPosition.Y - 0.30f, 0.65f, 1.5f);
            _shove.Advance((float)delta, _player.GlobalPosition + Vector3.Up * height,
                direction, MeleeAimMode.ThreeDimensional);
            if (_shove.IsActive || _requested) SetPhysicsProcess(true);
        }
    }

    // =========================================================
    private void OnInputBlocked(bool blocked)
    { if (blocked) { CancelTrigger(); _shove?.Cancel(); CancelVisualRecoil(); _weapon?.CancelFeedback(); } }

    // =========================================================
    public override void _Notification(int what)
    {
        if (what == NotificationApplicationFocusOut)
        { CancelTrigger(); _shove?.Cancel(); CancelVisualRecoil(); if (IsInstanceValid(_weapon)) _weapon!.CancelFeedback(); }
    }

    // =========================================================
    public override void _ExitTree()
    {
        CancelTrigger(); _shove?.Cancel(); CancelVisualRecoil();
        if (IsInstanceValid(_player)) _player.GameplayInputBlockChanged -= OnInputBlocked;
        if (IsInstanceValid(_weapon)) { _weapon!.ReadyToFire -= OnReadyToFire; _weapon.ShotAccepted -= OnShotAccepted; }
    }
}
