// Click-only visual isolation: track the same art roots as they transfer from living actors into corpses.
// Visibility alone changes; AI, animation, physics, hit detection and corpse simulation keep running.
using Godot;
using System.Collections.Generic;

public sealed class ZombieRenderDebug
{
    private sealed class Entry
    {
        public readonly Node3D Root;
        public readonly ulong Id;
        public bool WasVisible;
        public Entry(Node3D root) { Root = root; Id = root.GetInstanceId(); WasVisible = root.Visible; }
    }
    private readonly List<Entry> _visuals = new();
    private readonly HashSet<ulong> _ids = new();
    public bool Hidden { get; private set; }

    // =========================================================
    // World registers each living zombie once. Its art root survives the existing corpse reparenting.
    public void Register(Node3D visual)
    {
        if (!_ids.Add(visual.GetInstanceId())) return;
        var entry = new Entry(visual); _visuals.Add(entry);
        if (Hidden) visual.Hide();
    }

    // =========================================================
    // Restore each root's own visibility without touching severed parts/stumps or actor lifecycle flags.
    public void SetHidden(bool hidden)
    {
        if (Hidden == hidden) return;
        Hidden = hidden;
        for (int i = _visuals.Count - 1; i >= 0; i--)
        {
            Entry entry = _visuals[i];
            if (!GodotObject.IsInstanceValid(entry.Root) || entry.Root.IsQueuedForDeletion())
            { _ids.Remove(entry.Id); _visuals.RemoveAt(i); continue; }
            if (hidden) { entry.WasVisible = entry.Root.Visible; entry.Root.Hide(); }
            else entry.Root.Visible = entry.WasVisible;
        }
    }

    // =========================================================
    public void Clear() { SetHidden(false); _visuals.Clear(); _ids.Clear(); }
}
