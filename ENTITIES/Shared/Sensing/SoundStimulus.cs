// Immutable gameplay noise snapshot. IDs allow filtering without retaining or following source actor nodes.
using Godot;

public enum SoundKind { Gunshot, Footstep, Melee, Impact, Interaction }

public readonly struct SoundStimulus
{
    public ulong SequenceId { get; }
    public Vector3 WorldPosition { get; }
    public SoundKind Kind { get; }
    public float RadiusMeters { get; }
    public float Intensity { get; }
    public ulong SourceId { get; }
    public ulong EmitterId { get; }

    // =========================================================
    // Constructed by the world emission service after finite/range checks.
    public SoundStimulus(ulong sequenceId, Vector3 position, SoundKind kind, float radius, float intensity, ulong sourceId, ulong emitterId)
    { SequenceId = sequenceId; WorldPosition = position; Kind = kind; RadiusMeters = radius; Intensity = intensity; SourceId = sourceId; EmitterId = emitterId; }

    // =========================================================
    // Simple gameplay falloff, not physical acoustics. HE1 listeners apply their own thresholds/reaction policies.
    public float IntensityAt(Vector3 listenerPosition)
    {
        if (!float.IsFinite(RadiusMeters) || RadiusMeters <= 0 || !float.IsFinite(Intensity) || Intensity <= 0) return 0;
        float distanceSquared = WorldPosition.DistanceSquaredTo(listenerPosition);
        if (!float.IsFinite(distanceSquared) || distanceSquared >= RadiusMeters * RadiusMeters) return 0;
        return Intensity * (1f - Mathf.Sqrt(distanceSquared) / RadiusMeters);
    }
}
