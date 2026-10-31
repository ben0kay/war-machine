// GV1: a single fog-derived visibility envelope for scattered pines, DF2 broadleaf trees and stylized grass.
// Shared authored settings, event-driven updates; never polls the world or performs per-tree CPU LOD checks.
using Godot;

public sealed class VegetationVisibilityPolicy
{
    public event System.Action? Changed;
    public VegetationVisibilitySettings Settings { get; }
    private PlayerDistanceFog? _fog;
    private float _lastDistance;

    public VegetationVisibilityPolicy(VegetationVisibilitySettings settings)
    {
        Settings = settings;
        _lastDistance = ComputeDistance();
    }

    // =========================================================
    // Fog is view-specific: shared world and FPS bind their own actual player/camera fog.
    public void BindFog(PlayerDistanceFog fog)
    {
        if (_fog != null) _fog.SettingsApplied -= OnFogApplied;
        _fog = fog;
        _fog.SettingsApplied += OnFogApplied;
        OnFogApplied();
    }

    // =========================================================
    // Full-opacity fog owns the disappearing boundary; no independent asset-specific maximum exists.
    // When fog is disabled/translucent, use ONE authored fallback distance for every vegetation family.
    private float ComputeDistance()
    {
        if (_fog != null && _fog.FullyOpaque && _fog.OpaqueRadius > 0f)
            return Mathf.Max(24f, _fog.OpaqueRadius + Mathf.Clamp(Settings.FogSafetyBuffer, 8f, 48f));
        return Mathf.Max(24f, Settings.NoFogDistance);
    }

    public float RenderDistance => _lastDistance;
    public float Lod0End => _lastDistance * Mathf.Clamp(Settings.Lod0EndFraction, 0.15f, 0.6f);
    public float Lod1End => _lastDistance * Mathf.Clamp(Settings.Lod1EndFraction,
        Mathf.Clamp(Settings.Lod0EndFraction, 0.15f, 0.6f) + 0.1f, 0.9f);
    public float TransitionMargin => Mathf.Clamp(Settings.LodTransitionMargin, 2f, 16f);
    public bool IsFogDriven => _fog != null && _fog.FullyOpaque;

    // =========================================================
    // Updating the fog slider changes render ranges + streaming residency in all three systems.
    private void OnFogApplied()
    {
        float next = ComputeDistance();
        if (Mathf.Abs(next - _lastDistance) < 0.1f) return;
        _lastDistance = next;
        Changed?.Invoke();
        GD.Print($"GV1 vegetation visibility: {_lastDistance:0}m " +
            $"({(IsFogDriven ? "opaque fog" : "fog off fallback")}), " +
            $"LOD0={Lod0End:0}m, LOD1={Lod1End:0}m.");
    }

    // =========================================================
    public void Release()
    {
        if (_fog != null) _fog.SettingsApplied -= OnFogApplied;
        _fog = null;
        Changed = null;
    }
}
