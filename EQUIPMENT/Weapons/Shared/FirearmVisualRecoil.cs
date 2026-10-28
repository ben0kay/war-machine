// RC1/RC2: reusable shot-triggered visual kick; affects only the weapon's Visual subtree.
// The authoritative Muzzle marker, firing ray, recoil state and camera remain untouched.
using Godot;

public sealed class FirearmVisualRecoil
{
    private readonly Node3D? _visual;
    private readonly FirearmSettings _settings;
    private readonly Transform3D _rest;
    private float _amount, _sustainedRiseDegrees;
    public float Amount => _amount;
    public bool IsActive => _amount > 0.0001f || Mathf.Abs(_sustainedRiseDegrees) > 0.0001f;

    // =========================================================
    // Keep the authored visual placement for either camera mode and restore it on every interruption.
    public FirearmVisualRecoil(FirearmController firearm)
    {
        _settings = firearm.RuntimeSettings;
        _visual = firearm.GetNodeOrNull<Node3D>("Visual");
        _rest = GodotObject.IsInstanceValid(_visual) ? _visual!.Transform : Transform3D.Identity;
    }

    // =========================================================
    // Reuse and cap overlapping impulses (especially a 600-RPM machine gun).
    public void Kick()
    {
        if (!GodotObject.IsInstanceValid(_visual)) return;
        if (_settings.VisualRecoilPitchDegrees <= 0f && _settings.VisualRecoilKickMeters <= 0f) return;
        _amount = Mathf.Min(1.5f, _amount + 1f);
        Apply();
    }

    // =========================================================
    // Advanced only during a temporary recoil animation by its view adapter.
    public void Advance(float delta)
    {
        if (!IsActive) return;
        float seconds = Safe(_settings.VisualRecoilRecoverySeconds, 0.01f, 1f);
        float elapsed = float.IsFinite(delta) ? Mathf.Max(0f, delta) : 0f;
        _amount = Mathf.MoveToward(_amount, 0f, elapsed / seconds);
        if (_amount < 0.0001f) _amount = 0f;
        Apply();
    }

    // =========================================================
    // FPS-only additional sustained muzzle rise; does not change the real Muzzle or RC1 behavior.
    public void SetSustainedRise(float degrees)
    {
        float next = Safe(degrees, 0f, 15f);
        if (Mathf.IsEqualApprox(next, _sustainedRiseDegrees)) return;
        _sustainedRiseDegrees = next;
        Apply();
    }

    // =========================================================
    // Always restore authored visual pose, including when an old gun is reused after a switch.
    public void Cancel()
    {
        _amount = 0f; _sustainedRiseDegrees = 0f;
        Apply();
    }

    // =========================================================
    // +X tips the -Z barrel upward; +Z pulls it back towards the stock/shoulder.
    // Both motions are cosmetic: Muzzle is a sibling, so recoil affects ballistics only once.
    private void Apply()
    {
        if (!GodotObject.IsInstanceValid(_visual)) return;
        float angle = Mathf.DegToRad(Safe(_settings.VisualRecoilPitchDegrees, 0f, 20f) * _amount
            + _sustainedRiseDegrees);
        float kick = Safe(_settings.VisualRecoilKickMeters, 0f, 0.2f) * _amount;
        var pitch = new Basis(Vector3.Right, angle);
        _visual!.Transform = _rest * new Transform3D(pitch, Vector3.Back * kick);
    }

    // =========================================================
    private static float Safe(float value, float min, float max)
    { return float.IsFinite(value) ? Mathf.Clamp(value, min, max) : min; }
}
