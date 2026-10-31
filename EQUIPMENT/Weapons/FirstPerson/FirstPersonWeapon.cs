// Centre-screen aim adapter. Reuses the actual firearm, cooldown, muzzle obstruction and damage pipeline.
using Godot;

public partial class FirstPersonWeapon : Node
{
    private PlayerController _player = null!;
    private FirstPersonCamera _camera = null!;
    private FirearmController? _weapon;
    private MeleeWeaponController? _melee;
    private WeaponSlots _slots = null!;
    private PhysicsRayQueryParameters3D _query = null!;
    private bool _held, _requested;
    private PlayerQuickMelee? _shove;
    private FirearmVisualRecoil? _visualRecoil;
    private FirstPersonAimDebugLine? _aimDebugLine;
    private float _pendingCameraRise, _burstCameraRise;
    private double _lastAcceptedShotSeconds = -1.0;
    private bool _adsActive, _adsInitialized;
    private float _adsAmount, _hipFov;
    private Vector2 _swayDegrees;
    private Vector3 _hipPosition, _hipRotationDegrees;
    private bool AdsTransitioning => !Mathf.IsEqualApprox(_adsAmount, _adsActive ? 1f : 0f);
    private bool PhysicalSwayEnabled => IsInstanceValid(_weapon)
        && _weapon!.RuntimeSettings.FpsPhysicalSwayEnabled
        && _weapon.RuntimeSettings.FpsSwayAmplitudeDegrees > 0f;
    // Reused by the existing debug line. This is physical pre-spread aim, not random dispersion.
    public Vector2 CurrentPhysicalSwayDegrees => _swayDegrees;

    // =========================================================
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); }

    // =========================================================
    // The same rifle is a real world-space mesh, so nearby scenery can still obstruct its muzzle.
    public void Initialize(PlayerController player, FirstPersonCamera camera, EquipmentConfig equipment, WorldAudio? audio = null)
    {
        _player = player; _camera = camera;
        if (!equipment.Enabled || equipment.DefaultWeaponScene == null) return;
        camera.MouseCaptureChanged += OnMouseCaptureChanged;
        _query = new PhysicsRayQueryParameters3D { HitFromInside = true,
            Exclude = new Godot.Collections.Array<Rid> { player.GetRid() } };
        _slots = new WeaponSlots { Name = "WeaponSlots" }; AddChild(_slots);
        _slots.Initialize(equipment, player, camera, camera.ViewSettings.WeaponOffset, audio);
        // Development-only continuous tracer. Keep it independent of the recoil/ADS sleep loop.
        if (camera.ViewSettings.DebugAimLineEnabled)
        {
            _aimDebugLine = new FirstPersonAimDebugLine { Name = "UnspreadAimDebugLine" };
            camera.AddChild(_aimDebugLine);
            _aimDebugLine.Initialize(player, camera, this);
        }
        SwitchWeapon(0);
        if (_weapon != null) { _shove = new PlayerQuickMelee(player); player.GameplayInputBlockChanged += OnInputBlocked; player.InteractionModeChanged += OnInputBlocked; }
    }

    // =========================================================
    private void SwitchWeapon(int slot)
    {
        var previous = _weapon;
        if (!_slots.Select(slot)) return;
        _shove?.Cancel(); _melee?.Cancel(); CancelTrigger(); ResetAds(); CancelVisualRecoil();
        if (IsInstanceValid(previous))
        { previous!.ReadyToFire -= OnReadyToFire; previous.ShotAccepted -= OnShotAccepted; }
        _weapon = _slots.Current; _melee = _slots.CurrentMelee;
        _aimDebugLine?.SetWeapon(_weapon);
        // ADS1: held firearm weight is applied on selection, not each physics/render frame.
        if (_player is FirstPersonPlayer fps && IsInstanceValid(fps))
            fps.SetEquippedWeaponMovementMultiplier(_weapon?.RuntimeSettings.EquippedMovementMultiplier ?? 1f);
        _adsInitialized = false; _visualRecoil = null;
        if (_melee != null)
        {
            _melee.SetView(true); _melee.Position = _melee.RuntimeSettings.FirstPersonOffset;
            _query.CollisionMask = _melee.RuntimeSettings.HitMask; return;
        }
        _weapon!.ReadyToFire += OnReadyToFire; _weapon.ShotAccepted += OnShotAccepted;
        // =========================================================
        // Each gun may override its FPS view-model fit; root scaling also moves the real muzzle
        // marker consistently, while the shared top-down mount remains unaffected.
        _weapon.Position = _camera.ViewSettings.WeaponOffset + _weapon.RuntimeSettings.FirstPersonOffset;
        _weapon.Scale = Vector3.One * Mathf.Clamp(_weapon.RuntimeSettings.FirstPersonScale, 0.4f, 1.5f);
        _hipPosition = _weapon.Position; _hipRotationDegrees = _weapon.RotationDegrees; _hipFov = _camera.Fov;
        _adsAmount = 0f; _adsActive = false; _adsInitialized = true;
        _visualRecoil = new FirearmVisualRecoil(_weapon);
        _query.CollisionMask = _weapon.RuntimeSettings.HitMask;
        SetProcess(PhysicalSwayEnabled);
    }

    // =========================================================
    // Observe left-trigger release even when UI consumes it. MMB release does not exit toggled ADS.
    public override void _Input(InputEvent input)
    {
        if (input is InputEventMouseButton mouse && mouse.ButtonIndex == MouseButton.Left && !mouse.Pressed)
            _held = false;
        if (input is InputEventKey key && key.Pressed && key.PhysicalKeycode == Key.Escape)
        { CancelTrigger(); _shove?.Cancel(); _melee?.Cancel(); ResetAds(); CancelVisualRecoil(); _weapon?.CancelFeedback(); }
    }

    // =========================================================
    // An uncaptured click belongs to the camera and cannot become a shot.
    public override void _UnhandledInput(InputEvent input)
    {
        if (_player?.Interaction?.HandlePrimaryInput(input) == true) return;
        if ((_weapon == null && _melee == null) || _player.GameplayInputBlocked || Input.MouseMode != Input.MouseModeEnum.Captured) return;
        int slot = WeaponSlots.RequestedSlot(input);
        if (slot >= 0) { SwitchWeapon(slot); GetViewport().SetInputAsHandled(); return; }
        if (_player.InteractionModeActive) return;
        // Toggle ADS on each accepted MMB press; mouse-up is intentionally ignored.
        if (input is InputEventMouseButton aim && aim.ButtonIndex == MouseButton.Middle && aim.Pressed)
        {
            if (_weapon != null && _shove?.IsActive != true && GetViewport().GuiGetFocusOwner() == null
                && GetViewport().GuiGetHoveredControl() == null && GetWindow().HasFocus())
                SetAdsActive(!_adsActive);
            GetViewport().SetInputAsHandled(); return;
        }
        // Manual R reload is handled by the currently selected shared firearm.
        if (input is InputEventKey key && key.Pressed && !key.Echo && key.PhysicalKeycode == Key.R)
        {
            if (GetViewport().GuiGetFocusOwner() == null)
            { _weapon?.TryReload(); GetViewport().SetInputAsHandled(); }
            return;
        }
        if (PlayerQuickMelee.Requested(input))
        {
            if (GetViewport().GuiGetFocusOwner() == null)
            {
                if (_melee?.IsAttacking != true && _shove?.TryStart(_slots.CurrentItem!, true) == true)
                { ResetAds(); CancelVisualRecoil(); }
                SetPhysicsProcess(true); GetViewport().SetInputAsHandled();
            }
            return;
        }
        if (input is InputEventMouseButton mouse && mouse.ButtonIndex == MouseButton.Left && mouse.Pressed)
        { _held = true; RequestShot(); GetViewport().SetInputAsHandled(); }
    }

    // =========================================================
    // Accepted shots add real camera rise exactly once, using existing per-gun pitch strength.
    // The FPS burst budget is independent of RC0's smaller top-down recoil cap.
    private void OnShotAccepted()
    {
        _player.Vitals.Stamina.SpendPartial(_weapon!.RuntimeSettings.StaminaCostPerShot);
        _visualRecoil?.Kick();
        FirearmSettings settings = _weapon!.RuntimeSettings;
        double now = Time.GetTicksMsec() * 0.001;
        float pause = Mathf.Clamp(settings.FpsRecoilBurstResetSeconds, 0.05f, 2f);
        if (_lastAcceptedShotSeconds < 0 || now - _lastAcceptedShotSeconds >= pause) _burstCameraRise = 0f;
        _lastAcceptedShotSeconds = now;
        float max = Mathf.Clamp(settings.FpsMaxCameraRecoilBurstDegrees, 0f, 85f);
        float step = Mathf.Clamp(settings.RecoilPitchPerShotDegrees, 0f, 15f);
        float allowed = Mathf.Min(step, Mathf.Max(0f, max - _burstCameraRise));
        _burstCameraRise += allowed;
        _pendingCameraRise += allowed;
        if (_visualRecoil?.IsActive == true || _pendingCameraRise > 0f) SetProcess(true);
    }

    // =========================================================
    // Drain queued upward impulses at the weapon's configured camera rise speed.
    // RC0 can recover its own gameplay state for top-down, but NEVER pans FPS down.
    // Mouse compensation is permanent, and the real 3D firing ray uses this camera direction.
    public override void _Process(double delta)
    {
        if (!IsInstanceValid(_camera) || !IsInstanceValid(_weapon))
        { ResetAds(); CancelVisualRecoil(); return; }
        if (_player.GameplayInputBlocked || _player.InteractionModeActive || !GetWindow().HasFocus()
            || Input.MouseMode != Input.MouseModeEnum.Captured || GetViewport().GuiGetFocusOwner() != null)
        {
            ResetAds(); CancelVisualRecoil(); _weapon!.CancelFeedback(); return;
        }
        float dt = float.IsFinite((float)delta) ? Mathf.Clamp((float)delta, 0f, 0.1f) : 0f;
        float speed = Mathf.Clamp(_weapon!.RuntimeSettings.FpsCameraRecoilFollowDegreesPerSecond, 1f, 90f);
        float applied = Mathf.Min(_pendingCameraRise, speed * dt);
        _pendingCameraRise = Mathf.Max(0f, _pendingCameraRise - applied);
        if (applied > 0f) _camera.ApplyWeaponRecoilKick(applied);

        AdvanceAds(dt);
        // Enabled physical sway is the only reason this existing render callback stays awake at rest.
        UpdatePhysicalSway();
        _visualRecoil?.Advance(dt);
        // Sustained model pitch remains temporary; the camera's achieved rise does not recover.
        float extraRise = _weapon.RecoilOffsetDegrees.Y
            * Mathf.Clamp(_weapon.RuntimeSettings.FpsVisualClimbMultiplier, 0f, 2f);
        _visualRecoil?.SetSustainedRise(extraRise);
        if (_pendingCameraRise <= 0.0001f && _visualRecoil?.IsActive != true
            && _weapon.RecoilOffsetDegrees.Y <= 0.0001f && !AdsTransitioning && !PhysicalSwayEnabled)
            SetProcess(false);
    }

    #region ADS0 — FPS-only toggle aim presentation

    // =========================================================
    // Movement penalty applies while ADS is toggled on, regardless of animation progress.
    private void SetAdsActive(bool active)
    {
        if (!_adsInitialized || !IsInstanceValid(_weapon) || _adsActive == active) return;
        _adsActive = active;
        if (_player is FirstPersonPlayer fps && IsInstanceValid(fps))
            fps.SetAdsMovementMultiplier(active ? _weapon!.RuntimeSettings.FpsAdsMovementMultiplier : 1f);
        if (AdsTransitioning) SetProcess(true);
    }

    // =========================================================
    // One animation driver for weapon root + camera FOV. Recoil owns the Visual child only.
    // Smoothstep is evaluated from a linear timeline: per-gun multiplier changes duration.
    private void AdvanceAds(float dt)
    {
        if (!_adsInitialized || !AdsTransitioning) return;
        FirearmSettings settings = _weapon!.RuntimeSettings;
        float seconds = Mathf.Clamp(_camera.ViewSettings.AdsBaseTransitionSeconds, 0.05f, 1f);
        float speed = Mathf.Clamp(settings.FpsAdsAnimationSpeedMultiplier, 0.25f, 3f);
        _adsAmount = Mathf.MoveToward(_adsAmount, _adsActive ? 1f : 0f, dt * speed / seconds);
        float t = _adsAmount * _adsAmount * (3f - 2f * _adsAmount);
        _weapon.Position = _hipPosition + settings.FpsAdsOffset * t;
        _weapon.RotationDegrees = _hipRotationDegrees + Vector3.Right * settings.FpsAdsPitchDegrees * t;
        _camera.Fov = Mathf.Lerp(_hipFov, Mathf.Clamp(settings.FpsAdsFieldOfView, 40f, 110f), t);
    }

    // =========================================================
    // Hard interruption: no stuck FOV, movement penalty or transformed muzzle on reuse.
    private void ResetAds()
    {
        _adsActive = false; _adsAmount = 0f; _swayDegrees = Vector2.Zero;
        if (_player is FirstPersonPlayer fps && IsInstanceValid(fps)) fps.SetAdsMovementMultiplier(1f);
        if (!_adsInitialized) return;
        if (IsInstanceValid(_weapon))
        { _weapon!.Position = _hipPosition; _weapon.RotationDegrees = _hipRotationDegrees; }
        if (IsInstanceValid(_camera)) _camera.Fov = _hipFov;
    }

    #endregion

    #region FPS physical weapon sway — ADS2 foundation

    // =========================================================
    // Tunable runtime switch for the future weapon debug overlay, without a new global manager.
    // Current weapon instances keep their own toggle across slot changes.
    public void SetPhysicalSwayEnabled(bool enabled)
    {
        if (!IsInstanceValid(_weapon)) return;
        _weapon!.RuntimeSettings.FpsPhysicalSwayEnabled = enabled;
        if (!enabled) _swayDegrees = Vector2.Zero;
        if (_adsInitialized)
        {
            float t = _adsAmount * _adsAmount * (3f - 2f * _adsAmount);
            _weapon.RotationDegrees = _hipRotationDegrees
                + new Vector3(_weapon.RuntimeSettings.FpsAdsPitchDegrees * t, 0f, 0f);
        }
        if (enabled && Input.MouseMode == Input.MouseModeEnum.Captured) SetProcess(true);
        else if (!enabled && _pendingCameraRise <= 0.0001f && _visualRecoil?.IsActive != true
            && !AdsTransitioning) SetProcess(false);
    }

    // =========================================================
    // The authored gun root and real muzzle sway together. Camera look/recoil remain untouched.
    // One deterministic oscillator controls both visible gun motion and physical bullet aim.
    private void UpdatePhysicalSway()
    {
        if (!_adsInitialized || !IsInstanceValid(_weapon)) return;
        FirearmSettings settings = _weapon!.RuntimeSettings;
        _swayDegrees = PhysicalSwayEnabled
            ? FirstPersonAimTarget.SamplePhysicalSway(settings, _adsAmount, Time.GetTicksMsec() * 0.001)
            : Vector2.Zero;
        float t = _adsAmount * _adsAmount * (3f - 2f * _adsAmount);
        // Local +X pitches the barrel upward; local -Y yaws it to camera right.
        _weapon.RotationDegrees = _hipRotationDegrees
            + new Vector3(settings.FpsAdsPitchDegrees * t + _swayDegrees.Y, -_swayDegrees.X, 0f);
    }

    // =========================================================
    // The FPS camera owns pointer capture. Only resume continuous sway after capture returns.
    private void OnMouseCaptureChanged(bool captured)
    {
        if (captured)
        {
            if (PhysicalSwayEnabled && !_player.GameplayInputBlocked && !_player.InteractionModeActive)
                SetProcess(true);
            return;
        }
        CancelTrigger(); _shove?.Cancel(); _melee?.Cancel();
        ResetAds(); CancelVisualRecoil(); _weapon?.CancelFeedback();
    }

    #endregion

    // =========================================================
    // Interruptions clear queued rise and gun animation, never forcibly lower FPS aim.
    private void CancelVisualRecoil()
    {
        _pendingCameraRise = 0f;
        _burstCameraRise = 0f;
        _lastAcceptedShotSeconds = -1.0;
        _visualRecoil?.Cancel();
        SetProcess(false);
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
        if ((!IsInstanceValid(_weapon) && !IsInstanceValid(_melee)) || _player.GameplayInputBlocked || _player.InteractionModeActive || !GetWindow().HasFocus() || Input.MouseMode != Input.MouseModeEnum.Captured
            || GetViewport().GuiGetFocusOwner() != null)
        { CancelTrigger(); _shove?.Cancel(); _melee?.Cancel(); ResetAds(); CancelVisualRecoil(); _weapon?.CancelFeedback(); return; }
        Vector2 centre = _camera.GetViewport().GetVisibleRect().Size * 0.5f;
        Vector3 origin = _camera.ProjectRayOrigin(centre), direction = _camera.ProjectRayNormal(centre);
        if (_requested && _shove?.IsActive != true)
        {
            _requested = false;
            if (_melee != null) _melee.TryStart(_player, direction, true);
            else
            {
                // Same unspread camera target used by the optional FPS aim debug line.
                Vector3 aim = FirstPersonAimTarget.Resolve(_player, _query, origin, direction,
                    _weapon!.RuntimeSettings.Range);
                // Physical gun sway redirects the real muzzle-to-target shot before random spread.
                // Recoil is already in the FPS camera's current pitch; do not reapply it.
                aim = FirstPersonAimTarget.ApplyPhysicalSway(aim, _weapon.Muzzle.GlobalPosition,
                    _camera.GlobalBasis, _swayDegrees);
                // The FPS camera itself is already tilted upward by cumulative recoil.
                // FPS camera recoil already affects aim; hip fire adds only random spread,
                // never a second recoil rotation. ADS retains the original base spread.
                float extraSpread = _adsActive ? 0f : _weapon!.RuntimeSettings.FpsHipFireExtraSpreadDegrees;
                _weapon.TryFire(aim, origin, extraSpread);
            }
        }
        if (_melee?.IsAttacking == true)
        {
            float height = Mathf.Clamp(_camera.GlobalPosition.Y - _player.GlobalPosition.Y - 0.30f, 0.4f, 1.5f);
            _melee.Advance((float)delta, _player.GlobalPosition + Vector3.Up * height);
            if (_melee.IsAttacking) SetPhysicsProcess(true);
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
    {
        if (blocked)
        { CancelTrigger(); _shove?.Cancel(); _melee?.Cancel(); ResetAds(); CancelVisualRecoil(); _weapon?.CancelFeedback(); }
        else if (PhysicalSwayEnabled && Input.MouseMode == Input.MouseModeEnum.Captured) SetProcess(true);
    }

    // =========================================================
    public override void _Notification(int what)
    {
        if (what == NotificationApplicationFocusOut)
        { CancelTrigger(); _shove?.Cancel(); _melee?.Cancel(); ResetAds(); CancelVisualRecoil(); if (IsInstanceValid(_weapon)) _weapon!.CancelFeedback(); }
    }

    // =========================================================
    public override void _ExitTree()
    {
        CancelTrigger(); _shove?.Cancel(); _melee?.Cancel(); ResetAds(); CancelVisualRecoil();
        if (_player is FirstPersonPlayer fps && IsInstanceValid(fps)) fps.SetEquippedWeaponMovementMultiplier(1f);
        if (IsInstanceValid(_player)) { _player.GameplayInputBlockChanged -= OnInputBlocked; _player.InteractionModeChanged -= OnInputBlocked; }
        if (IsInstanceValid(_weapon)) { _weapon!.ReadyToFire -= OnReadyToFire; _weapon.ShotAccepted -= OnShotAccepted; }
        if (IsInstanceValid(_camera)) _camera.MouseCaptureChanged -= OnMouseCaptureChanged;
        if (IsInstanceValid(_aimDebugLine)) _aimDebugLine!.QueueFree();
    }
}
