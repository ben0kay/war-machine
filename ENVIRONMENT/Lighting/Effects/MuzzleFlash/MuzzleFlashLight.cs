// Cached real muzzle illumination; its owner advances lifetime inside the existing shot-effect callback.
using Godot;

public partial class MuzzleFlashLight : OmniLight3D
{
    private LightingSettings _settings = null!;
    private float _remaining;
    private bool _leased;
    private static int _activeLights;
    public bool IsLit => _leased;

    // =========================================================
    public void Initialize(LightingSettings settings)
    {
        _settings = (LightingSettings)settings.Duplicate(true);
        ShadowEnabled = false; OmniAttenuation = 1;
        LightColor = _settings.MuzzleLightColor; OmniRange = Mathf.Clamp(_settings.MuzzleLightRange, 0.5f, 15);
        PhysicsInterpolationMode = PhysicsInterpolationModeEnum.Off;
        Visible = false; LightEnergy = 0; SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    // One lease per cached light, even when rapid fire refreshes an already-active flash.
    public void Trigger(Vector3 position)
    {
        if (!_settings.Enabled || !_settings.MuzzleLightEnabled || _settings.MuzzleLightEnergy <= 0) { StopLight(); return; }
        if (!_leased)
        {
            if (_activeLights >= Mathf.Clamp(_settings.MaxTransientLights, 0, 16)) return;
            _activeLights++; _leased = true;
        }
        _remaining = Mathf.Clamp(_settings.MuzzleLightDurationSeconds, 0.02f, 0.2f);
        GlobalPosition = position; LightEnergy = Mathf.Clamp(_settings.MuzzleLightEnergy, 0, 8); Visible = true;
    }

    // =========================================================
    public void Advance(float delta, Vector3 renderedMuzzle)
    {
        if (!_leased) return;
        _remaining = Mathf.Max(0, _remaining - delta);
        if (_remaining <= 0) StopLight(); else GlobalPosition = renderedMuzzle;
    }

    // =========================================================
    public void StopLight()
    {
        if (_leased) { _activeLights = Mathf.Max(0, _activeLights - 1); _leased = false; }
        _remaining = 0; LightEnergy = 0; Visible = false;
    }

    // =========================================================
    public override void _ExitTree() => StopLight();
}
