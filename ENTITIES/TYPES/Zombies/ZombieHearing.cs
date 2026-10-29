// HE1: one discrete gunshot listener and compact pending estimate; the existing brain consumes it.
// One optional blocker ray on brain consumption; no tracking, hearing loop or event history.
using Godot;

public partial class ZombieController
{
    private SoundDispatcher? _soundDispatcher;
    private PhysicsRayQueryParameters3D? _soundBlockerRay;
    private bool _hearingSubscribed, _pendingSound;
    private Vector3 _soundOriginal, _soundEstimate, _soundWeights;
    private float _soundStrength;
    private ulong _nextSoundAt, _lastSoundSequence;

    // =========================================================
    // World injects its own service once. Rebinding/deactivation/exit always detach the previous delegate.
    public void BindHearing(SoundDispatcher? dispatcher)
    {
        UnsubscribeHearing(); _soundDispatcher = dispatcher;
        _pendingSound = false; _nextSoundAt = _lastSoundSequence = 0;
        SubscribeHearing();
    }

    // =========================================================
    private void SubscribeHearing()
    {
        if (_hearingSubscribed || _soundDispatcher == null || !_configured || !IsActive) return;
        _soundDispatcher.Emitted += HearSound; _hearingSubscribed = true;
    }

    // =========================================================
    private void UnsubscribeHearing()
    {
        if (_hearingSubscribed && _soundDispatcher != null) _soundDispatcher.Emitted -= HearSound;
        _hearingSubscribed = false; _pendingSound = false;
    }

    // =========================================================
    // Range/cooldown/priority rejection precede RNG. A rejected weak response also reserves cooldown.
    private void HearSound(SoundStimulus sound)
    {
        if (!_configured || !IsActive || _health.IsDead || IsQueuedForDeletion() || !_config.HearingEnabled
            || sound.Kind != SoundKind.Gunshot || sound.SourceId == GetInstanceId() || IsChasing
            || (_investigation.HasPoint && _investigation.Clue == InvestigationClue.Hit)) return;
        ulong now = Time.GetTicksMsec();
        if (now < _nextSoundAt || sound.SequenceId <= _lastSoundSequence
            || !sound.WorldPosition.IsFinite() || !float.IsFinite(sound.RadiusMeters) || sound.RadiusMeters <= 0
            || !float.IsFinite(sound.Intensity) || sound.Intensity <= 0) return;
        float squared = GlobalPosition.DistanceSquaredTo(sound.WorldPosition);
        if (!float.IsFinite(squared) || squared >= sound.RadiusMeters * sound.RadiusMeters) return;
        float distance = Mathf.Sqrt(squared);
        float strength = sound.Intensity * (1 - distance / sound.RadiusMeters);
        if (strength < SafePolicy(_config.MinimumAudibleIntensity, 0.001f, 4)) return;
        if (_pendingSound || (_investigation.IsActive && _investigation.Clue == InvestigationClue.Sound))
        {
            Vector3 moved = sound.WorldPosition - _soundOriginal; moved.Y = 0;
            float relocation = SafePolicy(_config.SoundNewSourceDistance, 1, 100);
            bool stronger = strength >= _soundStrength * SafePolicy(_config.SoundStrongerRatio, 1.05f, 4);
            bool relocated = moved.LengthSquared() >= relocation * relocation && strength >= _soundStrength * 0.9f;
            if (!stronger && !relocated) return;
        }
        _lastSoundSequence = sound.SequenceId;
        _nextSoundAt = now + (ulong)Mathf.CeilToInt(SafePolicy(_config.SoundRetargetCooldownSeconds, 0.1f, 60) * 1000);
        float near = SafePolicy(_config.SoundUncertaintyDistances.X, 0, 999);
        float far = Mathf.Max(near + 1, SafePolicy(_config.SoundUncertaintyDistances.Y, 1, 1000));
        float normalized = Mathf.Clamp((distance - near) / (far - near), 0, 1);
        float weakness = Mathf.Max(normalized, 1 - Mathf.Clamp(strength, 0, 1));
        float response = Mathf.Lerp(SafePolicy(_config.SoundResponseChance.X, 0, 1),
            SafePolicy(_config.SoundResponseChance.Y, 0, 1), weakness);
        if (_random.Randf() >= response) return;
        float min = SafePolicy(_config.SoundUncertaintyRadii.X, 0, 100);
        float max = Mathf.Max(min, SafePolicy(_config.SoundUncertaintyRadii.Y, 0, 100));
        float radius = Mathf.Lerp(min, max, normalized * normalized);
        float angle = _random.Randf() * Mathf.Tau, radial = Mathf.Sqrt(_random.Randf()) * radius;
        _soundOriginal = sound.WorldPosition;
        _soundEstimate = sound.WorldPosition + new Vector3(Mathf.Cos(angle) * radial, 0, Mathf.Sin(angle) * radial);
        _soundWeights = _config.SoundNearCommitmentWeights.Lerp(_config.SoundFarCommitmentWeights, weakness);
        _soundStrength = strength; _pendingSound = true;
    }

    // =========================================================
    // Consume after the existing visual decision; bullet-hit clues supersede any queued lower-priority sound.
    private void ConsumeHeardSound(ulong now)
    {
        if (!_pendingSound) return;
        _pendingSound = false;
        if (!_config.HearingEnabled || IsChasing
            || (_investigation.HasPoint && _investigation.Clue == InvestigationClue.Hit)) return;
        // Physics timer consumption guarantees a physics-safe query and avoids rays for superseded clues.
        _soundBlockerRay ??= new PhysicsRayQueryParameters3D {
            CollideWithBodies = true, CollideWithAreas = false, HitFromInside = true,
            Exclude = new Godot.Collections.Array<Rid> { GetRid() }
        };
        _soundBlockerRay.From = SightOrigin; _soundBlockerRay.To = _soundOriginal;
        _soundBlockerRay.CollisionMask = _config.SoundBlockerMask;
        bool blocked = _config.SoundBlockerMask != 0
            && GetWorld3D().DirectSpaceState.IntersectRay(_soundBlockerRay).Count > 0;
        Vector3 weights = blocked ? _soundWeights.Lerp(new Vector3(1, 0, 0),
            SafePolicy(_config.SoundBlockedEarlyBias, 0, 1)) : _soundWeights;
        float durationScale = blocked ? SafePolicy(_config.SoundBlockedTimeoutMultiplier, 0.1f, 1) : 1;
        if (_investigation.Remember(_soundEstimate, InvestigationClue.Sound))
            BeginInvestigation(now, weights, durationScale);
    }
}
