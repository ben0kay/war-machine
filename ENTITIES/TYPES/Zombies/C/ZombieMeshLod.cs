// ZM-PERF2B: shared native mesh LOD buffers, selected by Godot's renderer; no distance polling or extra visual nodes.
using Godot;
using System.Collections.Generic;

public static class ZombieMeshLod
{
    private sealed class Pair
    {
        public Mesh Full = null!, Lod = null!;
    }
    private static readonly Dictionary<ulong, Pair> Meshes = new();

    // =========================================================
    // Keep the complete near surface and add a lower-detail index buffer over an appended shared vertex buffer.
    // Both meshes are single-surface triangle art. UVs/normals stay with their own vertices and preserve winding.
    public static Mesh Create(Mesh full, Mesh far)
    {
        if (Meshes.TryGetValue(full.GetInstanceId(), out Pair? cached)) return cached.Lod;
        if (full == far || full.GetSurfaceCount() != 1 || far.GetSurfaceCount() != 1) return full;
        var nearArrays = full.SurfaceGetArrays(0); var farArrays = far.SurfaceGetArrays(0);
        Vector3[] nearVertices = nearArrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
        Vector3[] farVertices = farArrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
        int[] nearIndices = Indices(nearArrays, nearVertices.Length), farIndices = Indices(farArrays, farVertices.Length);
        if (farIndices.Length >= nearIndices.Length) return full;
        int offset = nearVertices.Length;
        for (int i = 0; i < farIndices.Length; i++) farIndices[i] += offset;
        var arrays = new Godot.Collections.Array(); arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = Join(nearVertices, farVertices);
        arrays[(int)Mesh.ArrayType.Normal] = Join(nearArrays[(int)Mesh.ArrayType.Normal].AsVector3Array(),
            farArrays[(int)Mesh.ArrayType.Normal].AsVector3Array());
        arrays[(int)Mesh.ArrayType.TexUV] = Join(nearArrays[(int)Mesh.ArrayType.TexUV].AsVector2Array(),
            farArrays[(int)Mesh.ArrayType.TexUV].AsVector2Array());
        arrays[(int)Mesh.ArrayType.Index] = nearIndices;
        // Object-space geometric error, NOT a metre-based switch distance. Renderer accounts for projection/scale.
        var lods = new Godot.Collections.Dictionary { [0.025f] = farIndices };
        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays, lods: lods);
        mesh.SurfaceSetMaterial(0, full.SurfaceGetMaterial(0));
        var pair = new Pair { Full = full, Lod = mesh };
        Meshes.Add(full.GetInstanceId(), pair); Meshes.Add(mesh.GetInstanceId(), pair);
        return mesh;
    }

    // =========================================================
    // Spawn/reinitialize-only configuration. Template copies and corpses retain the same shared mesh references.
    public static void Configure(Node node, bool enabled, float bias)
    {
        if (node is MeshInstance3D visual && visual.Mesh != null)
        {
            if (Meshes.TryGetValue(visual.Mesh.GetInstanceId(), out Pair? pair))
                visual.Mesh = enabled ? pair.Lod : pair.Full;
            visual.LodBias = float.IsFinite(bias) ? Mathf.Clamp(bias, 0.1f, 4f) : 1f;
        }
        foreach (Node child in node.GetChildren()) Configure(child, enabled, bias);
    }

    // =========================================================
    private static int[] Indices(Godot.Collections.Array arrays, int count)
    {
        if (arrays[(int)Mesh.ArrayType.Index].VariantType != Variant.Type.Nil)
            return arrays[(int)Mesh.ArrayType.Index].AsInt32Array();
        var indices = new int[count];
        for (int i = 0; i < count; i++) indices[i] = i;
        return indices;
    }

    // =========================================================
    private static T[] Join<T>(T[] first, T[] second)
    {
        var result = new T[first.Length + second.Length];
        System.Array.Copy(first, result, first.Length); System.Array.Copy(second, 0, result, first.Length, second.Length);
        return result;
    }
}
