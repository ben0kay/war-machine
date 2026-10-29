// PH2 short-term exertion buffer. Movement supplies actual sprint effort; recovery alone wakes this node below full.
using Godot;
using System;

public partial class PlayerStamina : Node
{
    public event Action? Changed;
    public float Current { get; private set; }
    public float Maximum { get; private set; } = 100;
    public bool Enabled { get; private set; }
    public bool Exhausted { get; private set; }
    public bool IsExerting => _sprinting;
    public bool IsStopped => _stopped;
    public bool CanSprint => _configured && !_stopped && (!Enabled || (!Exhausted && Current > 0));
    private float _sprintDrain, _jumpCost, _recoveryRate, _delaySeconds, _resumeFraction, _delayRemaining;
    private bool _configured, _stopped, _sprinting, _recovering;

    // =========================================================
    public override void _Ready() { SetProcess(false); SetPhysicsProcess(false); }

    // =========================================================
    // Preserve current state on repeated initialization, just like vitality; scene restart starts full.
    public void Initialize(PlayerStaminaSettings? settings)
    {
        if (_configured) return;
        var profile = settings != null ? (PlayerStaminaSettings)settings.Duplicate(true) : new PlayerStaminaSettings();
        Enabled = profile.Enabled;
        Maximum = Safe(profile.Maximum, 100, 1, 10000); Current = Maximum;
        _sprintDrain = Safe(profile.SprintDrainPerSecond, 10, 0, 1000);
        _jumpCost = Safe(profile.JumpCost, 15, 0, Maximum);
        _recoveryRate = Safe(profile.RecoveryPerSecond, 15, 0.01f, 1000);
        _delaySeconds = Safe(profile.RecoveryDelaySeconds, 1.5f, 0, 60);
        _resumeFraction = Safe(profile.SprintResumeFraction, 0.2f, 0.05f, 1);
        _configured = true; SetPhysicsProcess(false);
    }

    // =========================================================
    // Exact-cost actions are rejected without spending or resetting recovery when the reserve is insufficient.
    public bool TrySpend(float amount)
    {
        if (!_configured || _stopped || !float.IsFinite(amount) || amount < 0) return false;
        if (!Enabled || amount == 0) return true;
        if (Current < amount) return false;
        SpendPartial(amount); return true;
    }

    // =========================================================
    public bool TryJump() => TrySpend(_jumpCost);

    // =========================================================
    // Accepted shots and continuous exertion may spend the remaining fraction; firing is never blocked by stamina.
    public void SpendPartial(float amount)
    {
        if (!_configured || _stopped || !Enabled || !float.IsFinite(amount) || amount <= 0) return;
        float previous = Current; bool wasExhausted = Exhausted;
        Current = Mathf.Max(0, Current - amount);
        if (Current == 0) Exhausted = true;
        _delayRemaining = _delaySeconds;
        ScheduleRecovery();
        if (Current != previous || Exhausted != wasExhausted) Changed?.Invoke();
    }

    // =========================================================
    // Called after MoveAndSlide by the existing player callback, never by a separate movement poll.
    public void RecordSprint(bool exerting, float delta)
    {
        if (!_configured || _stopped || !Enabled) return;
        bool next = exerting && _sprintDrain > 0;
        bool changed = next != _sprinting; _sprinting = next;
        if (_sprinting && float.IsFinite(delta) && delta > 0) SpendPartial(_sprintDrain * delta);
        ScheduleRecovery();
        if (changed) Changed?.Invoke();
    }

    // =========================================================
    // Physics delta is gameplay time: unaffected by accelerated day/night, and paused with the scene tree.
    public override void _PhysicsProcess(double delta)
    {
        if (!_configured || _stopped || !Enabled || _sprinting || Current >= Maximum)
        { SetRecoveryActive(false); return; }
        float dt = (float)delta;
        if (!float.IsFinite(dt) || dt <= 0) return;
        float waiting = Mathf.Min(dt, _delayRemaining);
        _delayRemaining -= waiting; dt -= waiting;
        if (dt <= 0) return;
        Current = Mathf.Min(Maximum, Current + _recoveryRate * dt);
        if (Exhausted && Current >= Maximum * _resumeFraction) Exhausted = false;
        ScheduleRecovery(); Changed?.Invoke();
    }

    // =========================================================
    // Menus/focus release stop exertion through player movement; death/unload stops recovery permanently.
    public void Stop()
    {
        if (_stopped) return;
        _stopped = true; _sprinting = false; _delayRemaining = 0;
        SetRecoveryActive(false); Changed?.Invoke();
    }

    // =========================================================
    private void ScheduleRecovery() => SetRecoveryActive(_configured && Enabled && !_stopped && !_sprinting && Current < Maximum);

    // =========================================================
    private void SetRecoveryActive(bool active)
    {
        if (_recovering == active) return;
        _recovering = active; SetPhysicsProcess(active);
    }

    // =========================================================
    private static float Safe(float value, float fallback, float minimum, float maximum)
        => Mathf.Clamp(float.IsFinite(value) ? value : fallback, minimum, maximum);

    // =========================================================
    public override void _ExitTree() { _stopped = true; SetPhysicsProcess(false); Changed = null; }
}
