// Tiny original synthesized mechanical cues. Streams are cached; audio wakes only for steps/active scanning.
using Godot;

public sealed class RobotAudio
{
    private static AudioStreamWav? _stepStream, _servoStream;
    private readonly AudioStreamPlayer3D _step, _servo;
    private bool _enabled;

    // =========================================================
    // Create two spatial players once; synthesized PCM avoids external asset/licensing dependencies.
    public RobotAudio(Node3D owner)
    {
        _stepStream ??= MakeSound(false); _servoStream ??= MakeSound(true);
        _step = new AudioStreamPlayer3D { Name = "FootstepAudio", Stream = _stepStream, MaxDistance = 65, UnitSize = 12 };
        _servo = new AudioStreamPlayer3D { Name = "ServoAudio", Stream = _servoStream, MaxDistance = 45, UnitSize = 8 };
        owner.AddChild(_step); owner.AddChild(_servo);
    }

    // =========================================================
    // Sound controls never require polling and OFF explicitly stops both players.
    public void Configure(bool enabled, float volumeDb)
    {
        _enabled = enabled; _step.VolumeDb = volumeDb; _servo.VolumeDb = volumeDb - 8;
        if (!enabled) Stop();
    }

    // =========================================================
    // A diagonal pair landing emits one quiet thump, not four overlapping sound loops.
    public void Step() { if (_enabled) _step.Play(); }

    // =========================================================
    // Servo loop runs only during a scan and shuts down directly on stop/disable.
    public void Scan(bool active)
    { if (_enabled && active) { if (!_servo.Playing) _servo.Play(); } else _servo.Stop(); }

    // =========================================================
    // Unload/deactivation never leaves a spatial sound playing.
    public void Stop() { _step.Stop(); _servo.Stop(); }

    // =========================================================
    // Deterministic damped low thump or subtle looping motor; generated once per application session.
    private static AudioStreamWav MakeSound(bool servo)
    {
        const int rate = 22050;
        int count = servo ? rate : rate / 3;
        byte[] pcm = new byte[count * 2];
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / rate;
            float envelope = servo ? 0.13f : Mathf.Exp(-t * 16) * Mathf.Min(1, t * 160);
            float sound = servo ? Mathf.Sin(Mathf.Tau * 120 * t) + 0.25f * Mathf.Sin(Mathf.Tau * 241 * t)
                : Mathf.Sin(Mathf.Tau * 48 * t) + 0.3f * Mathf.Sin(Mathf.Tau * 113 * t);
            short sample = (short)(Mathf.Clamp(sound * envelope * 0.55f, -1, 1) * short.MaxValue);
            pcm[i * 2] = (byte)(sample & 255); pcm[i * 2 + 1] = (byte)((sample >> 8) & 255);
        }
        return new AudioStreamWav { Format = AudioStreamWav.FormatEnum.Format16Bits, MixRate = rate,
            Data = pcm, LoopMode = servo ? AudioStreamWav.LoopModeEnum.Forward : AudioStreamWav.LoopModeEnum.Disabled,
            LoopBegin = 0, LoopEnd = count };
    }
}
