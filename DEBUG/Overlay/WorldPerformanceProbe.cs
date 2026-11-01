// PERF0: opt-in main-thread C# timing scopes; zero stopwatch work while F2 is hidden.
using Godot;
using System;
using System.Diagnostics;

public static class WorldPerformanceProbe
{
    private static WorldPerformanceMonitor? _active;

    // =========================================================
    public static void Attach(WorldPerformanceMonitor monitor) => _active = monitor;

    // =========================================================
    public static void Detach(WorldPerformanceMonitor monitor)
    { if (ReferenceEquals(_active, monitor)) _active = null; }

    // =========================================================
    // Copy-free struct; call only from the existing game/main-thread operations.
    public static Scope Measure(string operation, Vector2I? chunk = null)
    {
        var sink = _active;
        return sink != null && sink.IsRecording
            ? new Scope(sink, operation, chunk, Stopwatch.GetTimestamp()) : default;
    }

    public readonly struct Scope : IDisposable
    {
        private readonly WorldPerformanceMonitor? _sink;
        private readonly string? _operation;
        private readonly Vector2I? _chunk;
        private readonly long _start;

        internal Scope(WorldPerformanceMonitor sink, string operation, Vector2I? chunk, long start)
        { _sink = sink; _operation = operation; _chunk = chunk; _start = start; }

        public void Dispose()
        {
            if (_sink == null || _operation == null) return;
            double elapsedMs = (Stopwatch.GetTimestamp() - _start) * 1000.0 / Stopwatch.Frequency;
            _sink.RecordOperation(_operation, _chunk, elapsedMs);
        }
    }
}
