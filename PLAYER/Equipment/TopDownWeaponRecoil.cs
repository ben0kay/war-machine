// RC1: animated overhead gun/arm kick. Modifies only Visual and shoulder joints, never the real muzzle/aim.
using Godot;

public sealed class TopDownWeaponRecoil
{
    private readonly PlayerController _player;
    private readonly Node3D? _visual;
    private readonly FirearmSettings _settings;
    private readonly Transform3D _rest;
    private float _amount;
    public bool IsActive => _amount > 0.0001f;

    // =========================================================
    // Cache the currently equipped weapon's authored visual transform once per switch.
    public TopDownWeaponRecoil(PlayerController player, FirearmController firearm)
    {
        _player = player;
        _settings = firearm.RuntimeSettings;
        _visual = firearm.GetNodeOrNull<Node3D>("Visual");
        _rest = GodotObject.IsInstanceValid(_visual) ? _visual!.Transform : Transform3D.Identity;
    }

    // =========================================================
    // An accepted shot adds a bounded pulse; 600-RPM bursts cannot tilt the weapon indefinitely.
    public void Kick()
    {
        if (!GodotObject.IsInstanceValid(_visual)) return;
        if (_settings.VisualRecoilPitchDegrees <= 0f && _settings.VisualRecoilKickMeters <= 0f) return;
        _amount = Mathf.Min(1.5f, _amount + 1f);
        Apply();
    }

    // =========================================================
    // Recover only while active; the owning top-down adapter sleeps its process once settled.
    public void Advance(float delta)
    {
        if (!IsActive) return;
        float seconds = Safe(_settings.VisualRecoilRecoverySeconds, 0.01f, 1f);
        _amount = Mathf.MoveToward(_amount, 0f, Mathf.Max(0f, (float.IsFinite(delta) ? delta : 0f)) / seconds);
        if (_amount < 0.0001f) _amount = 0f;
        Apply();
    }

    // =========================================================
    // Focus, UI, Q shove, swap and unload must restore the exact original model and arm pose.
    public void Cancel()
    {
        _amount = 0f;
        Apply();
    }

    // =========================================================
    // +X pitches the local -Z barrel upwards; +Z retracts the gun toward its stock/shoulder.
    // Keep the actual Muzzle marker outside this child so hitscan geometry never double-recoils.
    private void Apply()
    {
        if (GodotObject.IsInstanceValid(_visual))
        {
            float rise = Mathf.DegToRad(Safe(_settings.VisualRecoilPitchDegrees, 0f, 20f) * _amount);
            float kick = Safe(_settings.VisualRecoilKickMeters, 0f, 0.2f) * _amount;
            var tilt = new Basis(Vector3.Right, rise);
            _visual!.Transform = _rest * new Transform3D(tilt, Vector3.Back * kick);
        }
        if (GodotObject.IsInstanceValid(_player)) _player.SetWeaponRecoilPose(_amount);
    }

    // =========================================================
    private static float Safe(float value, float min, float max)
    { return float.IsFinite(value) ? Mathf.Clamp(value, min, max) : min; }
}
