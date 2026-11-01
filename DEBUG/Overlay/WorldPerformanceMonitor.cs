// PERF0: opt-in F2 runtime profiler; measures frame intervals and explicit C# chunk/WS2 operations.
using Godot;
using System;
using System.Collections.Generic;
using System.Text;

public partial class WorldPerformanceMonitor : CanvasLayer
{
    public bool IsRecording { get; private set; }
    private const int FrameHistory = 240;
    private const double SlowOperationMs = 2.0;
    private readonly float[] _frames = new float[FrameHistory];
    private readonly Dictionary<string, OperationStats> _operations = new();
    private readonly List<SlowOperation> _recent = new(16);
    private int _frameNext, _frameCount, _hitchCount;
    private float _worstFrame, _currentFrame, _lastHitch;
    private double _uiElapsed;

    private Label _framesLabel = null!, _chunksLabel = null!, _operationsLabel = null!, _recentLabel = null!;
    private PerformanceFrameGraph _graph = null!;
    private StylizedGrassPlacement? _grass;
    private PinePlacement? _pines;
    private ForestZonesPlacement? _forest;
    private ContentScatter? _rocks;
    private StaticSectionStreamer? _streaming;

    private sealed class OperationStats
    {
        public int Calls;
        public double LastMs, MaxMs;
    }

    private readonly struct SlowOperation
    {
        public readonly string Name;
        public readonly Vector2I? Chunk;
        public readonly double Ms;
        public readonly ulong AtMs;
        public SlowOperation(string name, Vector2I? chunk, double ms)
        { Name = name; Chunk = chunk; Ms = ms; AtMs = Time.GetTicksMsec(); }
    }

    // =========================================================
    public override void _Ready()
    {
        Layer = 90;
        BuildPanel();
        Visible = false;
        SetProcess(false); SetPhysicsProcess(false);
    }

    // =========================================================
    private void BuildPanel()
    {
        var panel = new PanelContainer {
            AnchorLeft = 1f, AnchorRight = 1f, OffsetLeft = -470f,
            OffsetRight = -12f, OffsetTop = 38f, OffsetBottom = 493f,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        var style = new StyleBoxFlat {
            BgColor = new Color(0.035f, 0.055f, 0.075f, 0.94f),
            BorderColor = new Color(0.33f, 0.52f, 0.58f, 0.8f),
            BorderWidthLeft = 1, BorderWidthTop = 1,
            BorderWidthRight = 1, BorderWidthBottom = 1,
            CornerRadiusTopLeft = 7, CornerRadiusTopRight = 7,
            CornerRadiusBottomLeft = 7, CornerRadiusBottomRight = 7
        };
        panel.AddThemeStyleboxOverride("panel", style);
        AddChild(panel);
        var margin = new MarginContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        margin.AddThemeConstantOverride("margin_left", 12);
        margin.AddThemeConstantOverride("margin_right", 12);
        margin.AddThemeConstantOverride("margin_top", 10);
        margin.AddThemeConstantOverride("margin_bottom", 10);
        panel.AddChild(margin);
        var layout = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        layout.AddThemeConstantOverride("separation", 6);
        margin.AddChild(layout);

        Label title = AddLabel(layout, "WAR MACHINE  |  F2 PERFORMANCE", 16);
        title.AddThemeColorOverride("font_color", new Color(0.68f, 0.92f, 0.82f));
        _framesLabel = AddLabel(layout, "Capturing...", 13);
        _graph = new PerformanceFrameGraph {
            CustomMinimumSize = new Vector2(426, 114),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        layout.AddChild(_graph);
        AddLabel(layout, "Frame intervals: green = below 40 ms  |  red = hitch", 11);
        _chunksLabel = AddLabel(layout, "Foliage/WS2 counters: waiting for world", 12);
        AddLabel(layout, "SLOWEST C# OPERATIONS  (maximum since opening F2)", 12);
        _operationsLabel = AddLabel(layout, "Waiting for chunk work...", 12);
        AddLabel(layout, "RECENT OPERATIONS >= 2 ms", 12);
        _recentLabel = AddLabel(layout, "No slow operations recorded.", 12);
        AddLabel(layout, "C# timings are inclusive; nested operations overlap. GPU and errors need Godot Debugger.", 10);
    }

    // =========================================================
    private static Label AddLabel(Node parent, string value, int fontSize)
    {
        var label = new Label {
            Text = value, MouseFilter = Control.MouseFilterEnum.Ignore,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        parent.AddChild(label);
        return label;
    }

    // =========================================================
    public void Bind(StylizedGrassPlacement? grass, PinePlacement? pines,
        ForestZonesPlacement? forest, ContentScatter? rocks)
    { _grass = grass; _pines = pines; _forest = forest; _rocks = rocks; }

    public void BindStreaming(StaticSectionStreamer streamer) => _streaming = streamer;

    // =========================================================
    public void Toggle()
    {
        IsRecording = !IsRecording;
        Visible = IsRecording;
        if (IsRecording)
        {
            Reset();
            WorldPerformanceProbe.Attach(this);
            SetProcess(true);
        }
        else
        {
            WorldPerformanceProbe.Detach(this);
            SetProcess(false);
        }
    }

    // =========================================================
    private void Reset()
    {
        Array.Clear(_frames, 0, _frames.Length);
        _frameNext = _frameCount = _hitchCount = 0;
        _currentFrame = _worstFrame = _lastHitch = 0f;
        _uiElapsed = 0;
        _operations.Clear(); _recent.Clear();
        RefreshLabels();
    }

    // =========================================================
    // One draw-period sampler only while F2 is shown; not a global simulation callback.
    public override void _Process(double delta)
    {
        if (!IsRecording) return;
        _currentFrame = (float)(delta * 1000.0);
        _frames[_frameNext] = _currentFrame;
        _frameNext = (_frameNext + 1) % FrameHistory;
        _frameCount = Math.Min(FrameHistory, _frameCount + 1);
        _worstFrame = Mathf.Max(_worstFrame, _currentFrame);
        if (_currentFrame >= 40f) { _hitchCount++; _lastHitch = _currentFrame; }
        _uiElapsed += delta;
        if (_uiElapsed < 0.2) return;
        _uiElapsed = 0;
        RefreshLabels();
    }

    // =========================================================
    // Stopwatch scopes on the main thread call this only while capture is enabled.
    public void RecordOperation(string operation, Vector2I? chunk, double milliseconds)
    {
        if (!IsRecording) return;
        if (!_operations.TryGetValue(operation, out OperationStats? stats))
        { stats = new OperationStats(); _operations.Add(operation, stats); }
        stats.Calls++; stats.LastMs = milliseconds;
        stats.MaxMs = Math.Max(stats.MaxMs, milliseconds);
        if (milliseconds < SlowOperationMs) return;
        if (_recent.Count == 12) _recent.RemoveAt(0);
        _recent.Add(new SlowOperation(operation, chunk, milliseconds));
    }

    // =========================================================
    // Update labels 5 times/sec, never once per generated grass clump or tree.
    private void RefreshLabels()
    {
        _framesLabel.Text = $"FPS: {Engine.GetFramesPerSecond():0}  |  frame: {_currentFrame:0.0} ms" +
            $"  |  worst: {_worstFrame:0.0} ms\nHitches >= 40ms: {_hitchCount}  |  latest: {_lastHitch:0.0} ms";
        _graph.ShowHistory(_frames, _frameNext, _frameCount);
        string grass = IsInstanceValid(_grass) ? _grass!.ActiveChunks.ToString() : "—";
        string pines = IsInstanceValid(_pines) ? _pines!.ActiveChunks.ToString() : "—";
        string forest = IsInstanceValid(_forest) ? _forest!.ActiveChunks.ToString() : "—";
        string rocks = IsInstanceValid(_rocks) ? _rocks!.ActiveChunks.ToString() : "—";
        _chunksLabel.Text = $"Chunks  Grass {grass}  |  Pine {pines}  |  Forest {forest}  |  Rocks {rocks}\n" +
            (IsInstanceValid(_streaming)
                ? $"WS2  Resident {_streaming!.ResidentCount}  |  In flight {_streaming.InFlightCount}  |  Loads {_streaming.LoadCount}  |  Unloads {_streaming.UnloadCount}"
                : "WS2  No active building streamer");

        var ranks = new List<KeyValuePair<string, OperationStats>>(_operations);
        ranks.Sort((a, b) => b.Value.MaxMs.CompareTo(a.Value.MaxMs));
        var top = new StringBuilder();
        for (int i = 0; i < Math.Min(ranks.Count, 5); i++)
        {
            var item = ranks[i];
            top.Append(item.Key).Append("   max ").Append(item.Value.MaxMs.ToString("0.00"))
                .Append(" ms   last ").Append(item.Value.LastMs.ToString("0.00"))
                .Append(" ms  (").Append(item.Value.Calls).Append("x)\n");
        }
        _operationsLabel.Text = top.Length == 0 ? "Waiting for chunk work..." : top.ToString().TrimEnd();
        var recent = new StringBuilder();
        ulong now = Time.GetTicksMsec();
        for (int i = _recent.Count - 1; i >= 0 && i >= _recent.Count - 5; i--)
        {
            SlowOperation item = _recent[i];
            recent.Append(item.Name);
            if (item.Chunk.HasValue) recent.Append(" [").Append(item.Chunk.Value.X)
                .Append(",").Append(item.Chunk.Value.Y).Append("]");
            recent.Append("  ").Append(item.Ms.ToString("0.0")).Append(" ms")
                .Append("  (").Append((now - item.AtMs) / 1000.0).Append("s ago)\n");
        }
        _recentLabel.Text = recent.Length == 0 ? "No slow operations recorded." : recent.ToString().TrimEnd();
    }

    // =========================================================
    public override void _ExitTree()
    {
        WorldPerformanceProbe.Detach(this);
        IsRecording = false;
        SetProcess(false);
    }
}
