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
    private float _pendingCameraRise, _burstCameraRise;
    private double _lastAcceptedShotSeconds = -1.0;
    private bool _adsHeld, _adsInitialized;
    private float _adsAmount, _hipFov;
    private Vector3 _hipPosition, _hipRotationDegrees;
    private bool AdsTransitioning => !Mathf.IsEqualApprox(_adsAmount, _adsHeld ? 1f : 0f);

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
        _slots.Initialize(equipment, player, camera, camera.ViewSettings.WeaponOffset, audio);
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
        _adsAmount = 0f; _adsHeld = false; _adsInitialized = true;
        _visualRecoil = new FirearmVisualRecoil(_weapon);
        _query.CollisionMask = _weapon.RuntimeSettings.HitMask;
    }

    // =========================================================
    // Observe release even when another UI/input adapter consumes it. Escape always cancels a held trigger.
    public override void _Input(InputEvent input)
    {
        if (input is InputEventMouseButton mouse)
        {
            if (mouse.ButtonIndex == MouseButton.Left && !mouse.Pressed) _held = false;
            if (mouse.ButtonIndex == MouseButton.Middle && !mouse.Pressed) SetAdsHeld(false);
        }
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
        // The GUI has first refusal. Only a captured, focused FPS firearm can enter held ADS.
        if (input is InputEventMouseButton aim && aim.ButtonIndex == MouseButton.Middle && aim.Pressed)
        {
            if (_weapon != null && _shove?.IsActive != true && GetViewport().GuiGetFocusOwner() == null
                && GetViewport().GuiGetHoveredControl() == null && GetWindow().HasFocus())
                SetAdsHeld(true);
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
        _visualRecoil?.Advance(dt);
        // Sustained model pitch remains temporary; the camera's achieved rise does not recover.
        float extraRise = _weapon.RecoilOffsetDegrees.Y
            * Mathf.Clamp(_weapon.RuntimeSettings.FpsVisualClimbMultiplier, 0f, 2f);
        _visualRecoil?.SetSustainedRise(extraRise);
        if (_pendingCameraRise <= 0.0001f && _visualRecoil?.IsActive != true
            && _weapon.RecoilOffsetDegrees.Y <= 0.0001f && !AdsTransitioning)
            SetProcess(false);
    }

    #region ADS0 — FPS-only held aim presentation

    // =========================================================
    // Movement penalty applies for the held state, independently of animation progress.
    private void SetAdsHeld(bool held)
    {
        if (!_adsInitialized || !IsInstanceValid(_weapon) || _adsHeld == held) return;
        _adsHeld = held;
        if (_player is FirstPersonPlayer fps && IsInstanceValid(fps))
            fps.SetAdsMovementMultiplier(held ? _weapon!.RuntimeSettings.FpsAdsMovementMultiplier : 1f);
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
        _adsAmount = Mathf.MoveToward(_adsAmount, _adsHeld ? 1f : 0f, dt * speed / seconds);
        float t = _adsAmount * _adsAmount * (3f - 2f * _adsAmount);
        _weapon.Position = _hipPosition + settings.FpsAdsOffset * t;
        _weapon.RotationDegrees = _hipRotationDegrees + Vector3.Right * settings.FpsAdsPitchDegrees * t;
        _camera.Fov = Mathf.Lerp(_hipFov, Mathf.Clamp(settings.FpsAdsFieldOfView, 40f, 110f), t);
    }

    // =========================================================
    // Hard interruption: no stuck FOV, movement penalty or transformed muzzle on reuse.
    private void ResetAds()
    {
        _adsHeld = false; _adsAmount = 0f;
        if (_player is FirstPersonPlayer fps && IsInstanceValid(fps)) fps.SetAdsMovementMultiplier(1f);
        if (!_adsInitialized) return;
        if (IsInstanceValid(_weapon))
        { _weapon!.Position = _hipPosition; _weapon.RotationDegrees = _hipRotationDegrees; }
        if (IsInstanceValid(_camera)) _camera.Fov = _hipFov;
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
                _query.From = origin; _query.To = origin + direction * _weapon!.RuntimeSettings.Range;
                var hit = _player.GetWorld3D().DirectSpaceState.IntersectRay(_query);
                Vector3 aim = hit.Count > 0 ? hit["position"].AsVector3() : _query.To;
                // The FPS camera itself is already tilted upward by cumulative recoil.
                // Using the default firing overload prevents applying the angle twice.
                _weapon!.TryFire(aim, origin);
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
    { if (blocked) { CancelTrigger(); _shove?.Cancel(); _melee?.Cancel(); ResetAds(); CancelVisualRecoil(); _weapon?.CancelFeedback(); } }

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
    }
}
