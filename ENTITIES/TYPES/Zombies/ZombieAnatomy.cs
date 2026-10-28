// Cached procedural-biped visibility adapter shared by standard and runner zombies; pivots remain valid for gait/corpses.
using Godot;
using System.Collections.Generic;

public sealed class ZombieAnatomy : IAnatomyVisualAdapter
{
    private readonly MeshInstance3D[][] _meshes = new MeshInstance3D[4][];
    private readonly MeshInstance3D[] _stumps = new MeshInstance3D[4];
    private readonly bool[] _missing = new bool[4];
    // =========================================================
    public ZombieAnatomy(params Node3D[] limbs)
    {
        for (int i = 0; i < 4; i++)
        {
            var meshes = new List<MeshInstance3D>(); Collect(limbs[i], meshes);
            _meshes[i] = meshes.ToArray();
            _stumps[i] = ZombieStumpGeometry.Create();
            limbs[i].AddChild(_stumps[i]);
        }
    }

    // =========================================================
    private static void Collect(Node node, List<MeshInstance3D> meshes)
    {
        if (node is MeshInstance3D mesh) meshes.Add(mesh);
        foreach (Node child in node.GetChildren()) Collect(child, meshes);
    }

    // =========================================================
    // Set only changed visibility. Never remove joints, change clothing materials or add animation callbacks.
    public void Apply(in AnatomySnapshot snapshot)
    {
        for (int i = 0; i < 4; i++)
        {
            bool missing = snapshot.IsSevered((BodyRegion)(i + (int)BodyRegion.LeftArm));
            if (_missing[i] == missing) continue;
            _missing[i] = missing;
            foreach (MeshInstance3D mesh in _meshes[i])
                if (GodotObject.IsInstanceValid(mesh)) mesh.Visible = !missing;
            if (GodotObject.IsInstanceValid(_stumps[i])) _stumps[i].Visible = missing;
        }
    }
}
