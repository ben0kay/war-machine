// Input/aim adapter only. Actor rig stays in the player; shot mechanics/settings stay in EQUIPMENT.
using Godot;

public partial class PlayerWeaponController : Node
{
    private PlayerController _player = null!;
    private Camera _camera = null!;
    private FirearmController? _weapon;
    private MeleeWeaponController? _melee;
    private WeaponSlots _slots = null!;
    private PhysicsRayQueryParameters3D _aimQuery = null!;
    private bool _held, _shotRequested, _aimDirty, _initialized;
    private Vector3 _aimPoint;
    private PlayerQuickMelee? _shove;
    private TopDownWeaponRecoil? _visualRecoil;

    // =========================================================
    // No idle frame callbacks. The world supplies ready actor/camera and a shared equipment definition.
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); }

    // =========================================================
    // Spawn whichever firearm scene is configured; this adapter never knows a rifle path or balance value.
    public void Initialize(PlayerController player, Camera camera, EquipmentConfig config, WorldAudio? audio = null)
    {
        _player = player; _camera = camera;
        if (!config.Enabled || config.DefaultWeaponScene == null) return;
        _aimQuery = new PhysicsRayQueryParameters3D {
            Exclude = new Godot.Collections.Array<Rid> { player.GetRid() } };
        _slots = new WeaponSlots { Name = "WeaponSlots" }; AddChild(_slots);
        _slots.Initialize(config, player, player.WeaponMount, Vector3.Zero, audio);
        SwitchWeapon(0);
        if (_weapon == null) return;
        _shove = new PlayerQuickMelee(player);
        player.SetWeaponPose(true); player.GameplayInputBlockChanged += OnInputBlocked;
        player.PositionChanged += OnPlayerMoved; player.WeaponPoseChanged += RequestAim;
        camera.ViewChanged += RequestAim;
        CameraOccluder.AimFadeChanged += RequestAim;
        _initialized = true; RequestAim();
    }

    // =========================================================
    // Cancel old trigger/callback before aiming the replacement; only the active gun can request repeat fire.
    private void SwitchWeapon(int slot)
    {
        var previous = _weapon;
        if (!_slots.Select(slot)) return;
        _shove?.Cancel(); _melee?.Cancel(); CancelTrigger(); CancelVisualRecoil();
        if (IsInstanceValid(previous))
        { previous!.ReadyToFire -= OnReadyToFire; previous.ShotAccepted -= OnShotAccepted; }
        _weapon = _slots.Current; _melee = _slots.CurrentMelee;
        if (_melee != null)
        {
            _melee.SetView(false); _aimQuery.CollisionMask = _melee.RuntimeSettings.HitMask;
            RequestAim(); return;
        }
        _visualRecoil = new TopDownWeaponRecoil(_player, _weapon);
        _weapon.ReadyToFire += OnReadyToFire; _weapon.ShotAccepted += OnShotAccepted;
        _aimQuery.CollisionMask = _weapon.RuntimeSettings.HitMask;
        RequestAim();
    }

    // =========================================================
    // Always see release, even if a UI control consumes that release before unhandled-input dispatch.
    public override void _Input(InputEvent input)
    { if (input is InputEventMouseButton mouse && mouse.ButtonIndex == MouseButton.Left && !mouse.Pressed) _held = false; }

    // =========================================================
    // UI gets first refusal on clicks; cursor changes queue one physics-safe aim update.
    public override void _UnhandledInput(InputEvent input)
    {
        if (!_initialized || _player.GameplayInputBlocked) return;
        int slot = WeaponSlots.RequestedSlot(input);
        if (slot >= 0) { SwitchWeapon(slot); GetViewport().SetInputAsHandled(); return; }
        // Manual R reload uses the same shared firearm API as the FPS view.
        if (input is InputEventKey key && key.Pressed && !key.Echo && key.PhysicalKeycode == Key.R)
        {
            if (GetViewport().GuiGetHoveredControl() == null && GetViewport().GuiGetFocusOwner() == null)
            { _weapon?.TryReload(); GetViewport().SetInputAsHandled(); }
            return;
        }
        if (PlayerQuickMelee.Requested(input))
        {
            // UI has first refusal on keys, as it already does for gunfire.
            if (GetViewport().GuiGetHoveredControl() == null && GetViewport().GuiGetFocusOwner() == null)
            {
                if (_melee?.IsAttacking != true && _shove?.TryStart(_slots.CurrentItem!) == true) CancelVisualRecoil();
                RequestAim(); GetViewport().SetInputAsHandled();
            }
            return;
        }
        if (input is InputEventMouseMotion) RequestAim();
        if (input is InputEventMouseButton mouse && mouse.ButtonIndex == MouseButton.Left && mouse.Pressed)
        { _held = true; _shotRequested = true; RequestAim(); GetViewport().SetInputAsHandled(); }
    }

    // =========================================================
    // An accepted shot drives temporary gun/arm animation without modifying the muzzle or real ray.
    private void OnShotAccepted()
    {
        _visualRecoil?.Kick();
        if (_visualRecoil?.IsActive == true) SetProcess(true);
    }

    // =========================================================
    // Recoil drawing updates only during its recovery; the existing aim physics scheduler is unaffected.
    public override void _Process(double delta)
    {
        if (_visualRecoil == null || !_visualRecoil.IsActive)
        { SetProcess(false); return; }
        _visualRecoil.Advance((float)delta);
        if (!_visualRecoil.IsActive) SetProcess(false);
    }

    // =========================================================
    // Clear the additive animation before a shove takes priority or equipment is interrupted.
    private void CancelVisualRecoil()
    { _visualRecoil?.Cancel(); SetProcess(false); }

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
        if (!_initialized || (!IsInstanceValid(_weapon) && !IsInstanceValid(_melee)) || !GetWindow().HasFocus() || _player.GameplayInputBlocked)
        { CancelTrigger(); _shove?.Cancel(); _melee?.Cancel(); return; }
        if (GetViewport().GuiGetHoveredControl() != null || GetViewport().GuiGetFocusOwner() != null)
        { CancelTrigger(); _shove?.Cancel(); _melee?.Cancel(); return; }
        if (_aimDirty || _shotRequested)
        {
            _aimDirty = false;
            Vector2 cursor = GetViewport().GetMousePosition();
            Vector3 origin = _camera.ProjectRayOrigin(cursor), direction = _camera.ProjectRayNormal(cursor);
            _aimQuery.From = origin; _aimQuery.To = origin + direction * _camera.Far;
            // AIM1: only the cursor passes through known faded building shapes; bullet rays stay solid.
            bool hasHit = FadeAwareAim.TryHit(_player.GetWorld3D().DirectSpaceState, _aimQuery, out Vector3 point);
            // Empty sky/off-terrain uses a plane at the hands' height instead of aiming off to infinity.
            if (hasHit) _aimPoint = point;
            else
            {
                float distance = Mathf.Abs(direction.Y) > 0.0001f ? (_player.WeaponMount.GlobalPosition.Y - origin.Y) / direction.Y : -1;
                _aimPoint = distance > 0 ? origin + direction * distance : _player.WeaponMount.GlobalPosition - _player.WeaponMount.GlobalBasis.Z * (_weapon?.RuntimeSettings.Range ?? _melee!.RuntimeSettings.Range);
            }
            if (_melee?.IsAttacking != true) _player.SetWeaponAim(_aimPoint);
        }
        if (_shotRequested && _shove?.IsActive != true)
        {
            _shotRequested = false;
            // Camera-up is the screen-up recoil reference: the crosshair stays fixed and
            // actual bullet/tracer direction climbs independently from ACC0 random spread.
            if (_melee != null) _melee.TryStart(_player, _aimPoint - _player.GlobalPosition, false);
            else _weapon!.TryFire(_aimPoint, _player.WeaponProbeOrigin, _camera.GlobalBasis.Y);
        }
        if (_melee?.IsAttacking == true)
        {
            _melee.Advance((float)delta, _player.WeaponProbeOrigin);
            if (_melee.IsAttacking) SetPhysicsProcess(true); else RequestAim();
        }
        if (_shove?.IsActive == true)
        {
            // Use world-space horizontal aim; overhead camera height never affects contact.
            Vector3 forward = _aimPoint - _player.GlobalPosition; forward.Y = 0;
            _shove.Advance((float)delta, _player.WeaponProbeOrigin, forward, MeleeAimMode.Horizontal);
            if (_shove.IsActive || _shotRequested) SetPhysicsProcess(true);
        }
    }

    // =========================================================
    // Opening the backpack cancels any held/queued attack immediately, without resetting weapon cooldown.
    private void OnInputBlocked(bool blocked)
    {
        if (!blocked) { RequestAim(); return; }
        CancelTrigger(); _shove?.Cancel(); _melee?.Cancel(); CancelVisualRecoil(); _weapon?.CancelFeedback(); SetPhysicsProcess(false);
    }

    // =========================================================
    // No stuck firing when tabbing away or moving a held click onto the debug sliders.
    private void CancelTrigger() { _held = false; _shotRequested = false; }

    // =========================================================
    public override void _Notification(int what)
    {
        if (!_initialized) return;
        if (what == NotificationApplicationFocusOut) { CancelTrigger(); _shove?.Cancel(); _melee?.Cancel(); CancelVisualRecoil(); _weapon?.CancelFeedback(); SetPhysicsProcess(false); }
        else if (what == NotificationApplicationFocusIn) RequestAim();
    }

    // =========================================================
    // Explicitly release cross-node subscriptions before world unload or replacing this adapter.
    public override void _ExitTree()
    {
        CancelTrigger(); _shove?.Cancel(); _melee?.Cancel(); CancelVisualRecoil();
        if (!_initialized) return;
        if (IsInstanceValid(_player)) { _player.GameplayInputBlockChanged -= OnInputBlocked; _player.PositionChanged -= OnPlayerMoved; _player.WeaponPoseChanged -= RequestAim; _player.SetWeaponPose(false); }
        if (IsInstanceValid(_camera)) _camera.ViewChanged -= RequestAim;
        CameraOccluder.AimFadeChanged -= RequestAim;
        if (IsInstanceValid(_weapon)) { _weapon!.ReadyToFire -= OnReadyToFire; _weapon.ShotAccepted -= OnShotAccepted; }
        _initialized = false;
    }
}
