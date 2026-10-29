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
        public readonly List<(GeometryInstance3D Mesh, GeometryInstance3D.ShadowCastingSetting Original)> Shadows = new();
        public Entry(Node3D root) { Root = root; Id = root.GetInstanceId(); WasVisible = root.Visible; }
    }
    private readonly List<Entry> _visuals = new();
    private readonly HashSet<ulong> _ids = new();
    public bool Hidden { get; private set; }
    public bool ShadowsDisabled { get; private set; }

    // =========================================================
    // World registers each living zombie once. Its art root survives the existing corpse reparenting.
    public void Register(Node3D visual)
    {
        if (!_ids.Add(visual.GetInstanceId())) return;
        var entry = new Entry(visual); _visuals.Add(entry);
        CacheShadows(visual, entry);
        if (Hidden) visual.Hide();
        if (ShadowsDisabled) ApplyShadows(entry, true);
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

    // Cache geometry once, including currently hidden stumps/limbs. No per-frame tree walk.
    private static void CacheShadows(Node node, Entry entry)
    {
        if (node is GeometryInstance3D mesh) entry.Shadows.Add((mesh, mesh.CastShadow));
        foreach (Node child in node.GetChildren()) CacheShadows(child, entry);
    }

    private static void ApplyShadows(Entry entry, bool disabled)
    {
        foreach (var shadow in entry.Shadows)
            if (GodotObject.IsInstanceValid(shadow.Mesh) && !shadow.Mesh.IsQueuedForDeletion())
                shadow.Mesh.CastShadow = disabled ? GeometryInstance3D.ShadowCastingSetting.Off : shadow.Original;
    }

    // Independent from visibility; the same meshes remain tracked after corpse reparenting.
    public void SetShadowsDisabled(bool disabled)
    {
        if (ShadowsDisabled == disabled) return;
        ShadowsDisabled = disabled;
        for (int i = _visuals.Count - 1; i >= 0; i--)
        {
            Entry entry = _visuals[i];
            if (!GodotObject.IsInstanceValid(entry.Root) || entry.Root.IsQueuedForDeletion())
            { _ids.Remove(entry.Id); _visuals.RemoveAt(i); continue; }
            ApplyShadows(entry, disabled);
        }
    }

    // =========================================================
    public void Clear() { SetHidden(false); SetShadowsDisabled(false); _visuals.Clear(); _ids.Clear(); }
}
