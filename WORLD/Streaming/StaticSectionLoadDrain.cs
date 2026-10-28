// Temporary scene-exit cleanup for Godot threaded requests (which have no cancellation API). Frees itself after completion.
using Godot;
using System.Collections.Generic;

public partial class StaticSectionLoadDrain : Node
{
    public readonly List<string> Paths = new();

    // =========================================================
    public override void _Process(double delta)
    {
        for (int i = Paths.Count - 1; i >= 0; i--)
        {
            ResourceLoader.ThreadLoadStatus status = ResourceLoader.LoadThreadedGetStatus(Paths[i]);
            if (status == ResourceLoader.ThreadLoadStatus.InProgress) continue;
            if (status == ResourceLoader.ThreadLoadStatus.Loaded) _ = ResourceLoader.LoadThreadedGet(Paths[i]);
            Paths.RemoveAt(i);
        }
        if (Paths.Count == 0) { SetProcess(false); QueueFree(); }
    }
}
