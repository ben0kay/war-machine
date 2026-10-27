// World-owned fixed voice pool and intentional listener. No frame work for idle or one-shot-only playback.
using Godot;

public partial class WorldAudio : Node
{
    [Export] public AudioSettings Settings { get; set; } = null!;
    public AudioSettings RuntimeSettings { get; private set; } = null!;
    private sealed class Voice
    {
        public AudioStreamPlayer3D Spatial = null!;
        public AudioStreamPlayer Flat = null!;
        public AudioCue? Cue;
        public Node3D? Follow;
        public ulong Emitter, Handle;
        public bool Loop;
        public float Gain;
        public bool Active => Cue != null;
    }
    private Voice[] _voices = System.Array.Empty<Voice>();
    private readonly RandomNumberGenerator _random = new();
    private AudioListener3D? _listener;
    private PlayerController? _player;
    private Camera? _camera;
    private ulong _nextHandle;
    private float _loopClock;

    // =========================================================
    public override void _Ready()
    {
        RuntimeSettings = (AudioSettings)Settings.Duplicate(true); _random.Randomize();
        _voices = new Voice[Mathf.Clamp(RuntimeSettings.MaxVoices, 1, 64)];
        for (int i = 0; i < _voices.Length; i++)
        {
            int index = i;
            var v = new Voice {
                Spatial = new AudioStreamPlayer3D { Name = $"Spatial_{i}", MaxPolyphony = 1, PhysicsInterpolationMode = PhysicsInterpolationModeEnum.Off },
                Flat = new AudioStreamPlayer { Name = $"Flat_{i}", MaxPolyphony = 1 } };
            AddChild(v.Spatial); AddChild(v.Flat); _voices[i] = v;
            v.Spatial.Finished += () => Finished(index); v.Flat.Finished += () => Finished(index);
        }
        SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    // Player-centred ears avoid attenuation from the elevated overhead camera; orientation follows the current view.
    public void BindListener(PlayerController player, Camera camera)
    {
        if (IsInstanceValid(_camera)) _camera!.ViewChanged -= UpdateListener;
        if (IsInstanceValid(_player)) _player!.StanceChanged -= OnStance;
        if (IsInstanceValid(_listener)) { _listener!.ClearCurrent(); _listener.QueueFree(); }
        _player = player; _camera = camera;
        _listener = new AudioListener3D { Name = "GameplayAudioListener" }; player.AddChild(_listener);
        OnStance(player.IsCrouching); UpdateListener(); _listener.MakeCurrent();
        player.StanceChanged += OnStance; camera.ViewChanged += UpdateListener;
    }

    // =========================================================
    private void OnStance(bool crouched)
    { if (IsInstanceValid(_listener)) _listener!.Position = Vector3.Up * (crouched ? 1.3f : 1.7f); }

    // =========================================================
    private void UpdateListener()
    { if (IsInstanceValid(_listener) && IsInstanceValid(_camera)) { if (_listener!.GlobalBasis != _camera!.GlobalBasis) _listener.GlobalBasis = _camera.GlobalBasis; } }

    // =========================================================
    public bool PlayOneShot(AudioCue? cue, Vector3 position, ulong emitter = 0, float gainDb = 0)
        => Play(cue, position, emitter, gainDb, null, false) != 0;

    // =========================================================
    // Handle is generation-unique, so stopping an old loop cannot stop a recycled voice.
    public ulong StartLoop(AudioCue cue, Node3D source, ulong emitter = 0)
        => IsInstanceValid(source) ? Play(cue, source.GlobalPosition, emitter == 0 ? source.GetInstanceId() : emitter, 0, source, true) : 0;

    // =========================================================
    // Count all nested limits first; steal only if one eligible voice resolves every saturated constraint.
    private ulong Play(AudioCue? cue, Vector3 position, ulong emitter, float gain, Node3D? follow, bool loop)
    {
        if (!RuntimeSettings.Enabled || cue?.Stream == null || !IsInstanceValid(_listener)) return 0;
        float maxDistance = cue.MaxDistance > 0 ? cue.MaxDistance : RuntimeSettings.DefaultMaxDistance;
        float distance = _listener!.GlobalPosition.DistanceSquaredTo(position);
        if (cue.Spatial && distance > maxDistance * maxDistance) return 0;
        int total = 0, kind = 0, creatures = 0, same = 0, owner = 0, free = -1;
        foreach (var v in _voices)
        {
            if (!v.Active) continue;
            total++; if (v.Loop == loop) kind++;
            if (v.Cue!.Category == SoundCategory.Creatures) creatures++;
            if (SameCue(v.Cue!, cue)) same++;
            if (emitter != 0 && v.Emitter == emitter) owner++;
        }
        bool full = total >= Mathf.Clamp(RuntimeSettings.MaxVoices, 0, _voices.Length);
        int typeCap = loop ? RuntimeSettings.MaxLoopVoices : RuntimeSettings.MaxOneShotVoices;
        if (typeCap <= 0 || RuntimeSettings.MaxVoices <= 0 || RuntimeSettings.MaxSameCueVoices <= 0
            || (emitter != 0 && RuntimeSettings.MaxPerEmitterVoices <= 0)
            || (cue.Category == SoundCategory.Creatures && RuntimeSettings.MaxCreatureVoices <= 0)) return 0;
        bool kindFull = kind >= typeCap, sameFull = same >= RuntimeSettings.MaxSameCueVoices;
        bool ownerFull = emitter != 0 && owner >= RuntimeSettings.MaxPerEmitterVoices;
        bool creatureFull = cue.Category == SoundCategory.Creatures && creatures >= RuntimeSettings.MaxCreatureVoices;
        int victim = -1; float worst = float.PositiveInfinity;
        float score = cue.Priority * 100000f - (cue.Spatial ? distance : 0);
        for (int i = 0; i < _voices.Length; i++)
        {
            var v = _voices[i];
            if (!v.Active) { if (free < 0) free = i; continue; }
            if (kindFull && v.Loop != loop || sameFull && !SameCue(v.Cue!, cue) || ownerFull && v.Emitter != emitter
                || creatureFull && v.Cue!.Category != SoundCategory.Creatures) continue;
            float oldDistance = v.Cue!.Spatial ? _listener.GlobalPosition.DistanceSquaredTo(v.Spatial.GlobalPosition) : 0;
            float oldScore = v.Cue.Priority * 100000f - oldDistance;
            if (oldScore <= score && oldScore < worst) { victim = i; worst = oldScore; }
        }
        int slot = !full && !kindFull && !sameFull && !ownerFull && !creatureFull ? free : victim;
        if (slot < 0) return 0;
        Release(slot);
        var voice = _voices[slot];
        voice.Cue = cue; voice.Emitter = emitter; voice.Follow = follow; voice.Loop = loop; voice.Gain = gain;
        voice.Handle = ++_nextHandle;
        float pitch = 1 + _random.RandfRange(-Mathf.Clamp(cue.PitchVariation, 0, 0.2f), Mathf.Clamp(cue.PitchVariation, 0, 0.2f));
        float volume = Mathf.Clamp(cue.VolumeDb + gain + RuntimeSettings.MasterVolumeDb + CategoryGain(cue.Category), -80, 12);
        if (cue.Spatial)
        {
            voice.Spatial.Stream = cue.Stream; voice.Spatial.VolumeDb = volume; voice.Spatial.PitchScale = pitch;
            voice.Spatial.MaxDistance = Mathf.Max(1, maxDistance); voice.Spatial.UnitSize = 1;
            voice.Spatial.GlobalPosition = position; voice.Spatial.Play();
        }
        else { voice.Flat.Stream = cue.Stream; voice.Flat.VolumeDb = volume; voice.Flat.PitchScale = pitch; voice.Flat.Play(); }
        if (loop) SetProcess(true);
        return voice.Handle;
    }

    // =========================================================
    private static bool SameCue(AudioCue a, AudioCue b)
        => a == b || a.CueId.Length > 0 && a.CueId == b.CueId;

    // =========================================================
    private float CategoryGain(SoundCategory category) => category switch {
        SoundCategory.Weapons => RuntimeSettings.WeaponsVolumeDb, SoundCategory.Creatures => RuntimeSettings.CreaturesVolumeDb,
        SoundCategory.Footsteps => RuntimeSettings.FootstepsVolumeDb, SoundCategory.Weather => RuntimeSettings.WeatherVolumeDb,
        SoundCategory.UI => RuntimeSettings.UiVolumeDb, _ => RuntimeSettings.WorldVolumeDb };

    // =========================================================
    // Non-looping authored streams can repeat; native loop-enabled streams are preferred for seamless ambience.
    private void Finished(int index)
    {
        var v = _voices[index];
        if (!v.Active) return;
        if (v.Loop && RuntimeSettings.Enabled && IsInstanceValid(v.Follow) && v.Follow!.IsInsideTree())
        { if (v.Cue!.Spatial) v.Spatial.Play(); else v.Flat.Play(); }
        else Release(index);
    }

    // =========================================================
    private void Release(int index)
    {
        var v = _voices[index];
        if (IsInstanceValid(v.Spatial)) { v.Spatial.Stop(); v.Spatial.Stream = null; }
        if (IsInstanceValid(v.Flat)) { v.Flat.Stop(); v.Flat.Stream = null; } v.Cue = null; v.Follow = null;
        v.Emitter = 0; v.Handle = 0; v.Loop = false; v.Gain = 0;
    }

    // =========================================================
    public void StopLoop(ulong handle)
    { if (handle != 0) for (int i = 0; i < _voices.Length; i++) if (_voices[i].Handle == handle && _voices[i].Loop) { Release(i); break; } SleepIfNoLoops(); }

    // =========================================================
    public void StopEmitter(ulong emitter)
    { if (emitter != 0) for (int i = 0; i < _voices.Length; i++) if (_voices[i].Emitter == emitter) Release(i); SleepIfNoLoops(); }

    // =========================================================
    public void StopAll()
    { for (int i = 0; i < _voices.Length; i++) Release(i); SetProcess(false); }

    // =========================================================
    // Explicit master changes stop existing playback; pool capacity changes require a scene reload.
    public void ApplySettings() => StopAll();

    // =========================================================
    private void SleepIfNoLoops()
    { foreach (var v in _voices) if (v.Active && v.Loop) return; SetProcess(false); }

    // =========================================================
    // Only active loops wake this one shared 5Hz follow/relevance pass. One-shots stay at their shot position.
    public override void _Process(double delta)
    {
        _loopClock += (float)delta; if (_loopClock < 0.2f) return; _loopClock = 0;
        if (!RuntimeSettings.Enabled) { StopAll(); return; }
        for (int i = 0; i < _voices.Length; i++)
        {
            var v = _voices[i]; if (!v.Active || !v.Loop) continue;
            if (!IsInstanceValid(v.Follow) || !v.Follow!.IsInsideTree()) { Release(i); continue; }
            float max = v.Cue!.MaxDistance > 0 ? v.Cue.MaxDistance : RuntimeSettings.DefaultMaxDistance;
            Vector3 position = v.Follow.GlobalPosition;
            if (v.Cue.Spatial && (!IsInstanceValid(_listener) || _listener!.GlobalPosition.DistanceSquaredTo(position) > max * max))
            { Release(i); continue; }
            if (v.Cue.Spatial) v.Spatial.GlobalPosition = position;
        }
        SleepIfNoLoops();
    }

    // =========================================================
    public override void _ExitTree()
    {
        if (IsInstanceValid(_camera)) _camera!.ViewChanged -= UpdateListener;
        if (IsInstanceValid(_player)) _player!.StanceChanged -= OnStance;
        StopAll();
        if (IsInstanceValid(_listener)) { if (_listener!.IsCurrent()) _listener.ClearCurrent(); _listener.QueueFree(); }
    }
}
