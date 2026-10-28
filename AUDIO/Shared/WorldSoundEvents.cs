// Shared action entry point: independent gameplay hearing notification and optional existing pooled audio playback.
using Godot;
using System;

public sealed class WorldSoundEvents
{
    public SoundDispatcher Hearing { get; } = new();
    private WorldAudio? _audio;
    private ulong _sequence;
    private bool _active = true;

    // =========================================================
    // Null audio is supported for silent gameplay/tests; the normal world supplies its existing voice pool.
    public WorldSoundEvents(WorldAudio? audio = null) { _audio = audio; }

    // =========================================================
    // True means an action event was accepted, NOT that a playback voice was available.
    // Radius grows with sqrt(amplitude); playback gets 20*log10(amplitude) dB, independently of user volume.
    public bool Emit(SoundEmissionSettings settings, SoundKind kind, Vector3 position, ulong sourceId = 0,
        ulong emitterId = 0, AudioCue? playbackCue = null, float gainDb = 0, float loudnessMultiplier = 1f)
    {
        if (!_active || settings == null || !float.IsFinite(position.X) || !float.IsFinite(position.Y)
            || !float.IsFinite(position.Z) || !float.IsFinite(settings.Loudness) || !float.IsFinite(loudnessMultiplier)) return false;
        float loudness = Mathf.Clamp(settings.Loudness, 0, 4) * Mathf.Clamp(loudnessMultiplier, 0, 4);
        loudness = Mathf.Min(loudness, 4);
        if (loudness <= 0) return false;
        float radius = float.IsFinite(settings.HearingRadiusMeters)
            ? Mathf.Min(1000, Mathf.Clamp(settings.HearingRadiusMeters, 0, 1000) * Mathf.Sqrt(loudness)) : 0;
        var stimulus = new SoundStimulus(++_sequence, position, kind, radius, loudness, sourceId, emitterId);
        // Playback rejection/mute/missing stream cannot prevent this separate gameplay notification.
        if (radius > 0) Hearing.Publish(stimulus);
        if (playbackCue != null && GodotObject.IsInstanceValid(_audio) && float.IsFinite(gainDb))
            _audio!.PlayOneShot(playbackCue, position, emitterId, gainDb + 20f * MathF.Log10(loudness));
        return true;
    }

    // =========================================================
    // Invalidates old service handles and releases subscriber references when the owning world exits.
    public void Shutdown() { _active = false; Hearing.Clear(); _audio = null; }
}
