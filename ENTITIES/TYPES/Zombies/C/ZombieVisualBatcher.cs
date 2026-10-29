// ZM-PERF2A: combine only same-pivot, same-material zombie geometry into one shared triangle surface.
// Author once per species; subsequent actors duplicate its immutable assembled visual rig, not the meshes.
using Godot;
using System.Collections.Generic;

public static class ZombieVisualBatcher
{
    // =========================================================
    // First instance of each species builds a template before anatomy/stumps/debug nodes are added.
    // Templates stay detached and unanimated. Runtime actors use the exact same ArrayMesh resources.
    public static Node3D Capture(Node3D visualRoot)
    {
        CombineUnder(visualRoot);
        return (Node3D)visualRoot.Duplicate();
    }

    // =========================================================
    // Duplicate only the template's lightweight scene nodes; Godot shares their Mesh/Material resources.
    // The original six limbs, Head, lower-leg offsets and Ankle nodes retain their names and transforms.
    public static void Populate(Node3D destination, Node3D template)
    {
        foreach (Node child in template.GetChildren())
            destination.AddChild(child.Duplicate());
    }

    // =========================================================
    // Group directly under each pivot, never across articulated parents or anatomical region boundaries.
    // Palette mesh names remain explicit, so ZombieAppearance.Apply can reassign spawn-seeded materials.
    private static void CombineUnder(Node3D pivot)
    {
        foreach (Node child in pivot.GetChildren())
            if (child is Node3D sub && child is not MeshInstance3D) CombineUnder(sub);

        var groups = new Dictionary<(ZombieAppearance.PaletteRegion Palette, ulong MaterialId), List<MeshInstance3D>>();
        foreach (Node child in pivot.GetChildren())
        {
            if (child is not MeshInstance3D part || part.Mesh == null || !part.Visible) continue;
            var category = ZombieAppearance.PaletteForPart(part.Name.ToString());
            ulong materialId = category == ZombieAppearance.PaletteRegion.None
                ? part.MaterialOverride?.GetInstanceId() ?? 0UL : 0UL;
            var key = (category, materialId);
            if (!groups.TryGetValue(key, out List<MeshInstance3D>? members))
            { members = new List<MeshInstance3D>(4); groups.Add(key, members); }
            members.Add(part);
        }
        int detail = 0;
        foreach (var group in groups)
        {
            List<MeshInstance3D> members = group.Value;
            if (members.Count < 2) continue; // Never rebuild a perfectly good single mesh.
            ArrayMesh merged = Join(members);
            var batch = new MeshInstance3D {
                Name = group.Key.Palette == ZombieAppearance.PaletteRegion.None
                    ? $"DetailBatch{detail++}" : $"{group.Key.Palette}Batch",
                Mesh = merged, MaterialOverride = members[0].MaterialOverride,
                CastShadow = members[0].CastShadow
            };
            pivot.AddChild(batch);
            foreach (MeshInstance3D piece in members)
            { pivot.RemoveChild(piece); piece.Free(); }
        }
    }

    // =========================================================
    // Bake each part's original nonuniform scale, rotation, position, normal and UV into one
    // triangle stream. One material override and ONE surface per batch (not many material surfaces).
    // Source BoxMesh/SphereMesh/ArrayMesh data is read only during the first species build.
    private static ArrayMesh Join(List<MeshInstance3D> pieces)
    {
        using var surface = new SurfaceTool();
        surface.Begin(Mesh.PrimitiveType.Triangles);
        foreach (MeshInstance3D piece in pieces)
        {
            Mesh mesh = piece.Mesh!;
            Transform3D transform = piece.Transform;
            Basis normalBasis = transform.Basis.Inverse().Transposed();
            for (int s = 0; s < mesh.GetSurfaceCount(); s++)
            {
                if (mesh.SurfaceGetPrimitiveType(s) != Mesh.PrimitiveType.Triangles)
                    continue;
                var arrays = mesh.SurfaceGetArrays(s);
                Vector3[] vertices = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
                Vector3[] normals = arrays[(int)Mesh.ArrayType.Normal].VariantType == Variant.Type.Nil
                    ? System.Array.Empty<Vector3>() : arrays[(int)Mesh.ArrayType.Normal].AsVector3Array();
                Vector2[] uvs = arrays[(int)Mesh.ArrayType.TexUV].VariantType == Variant.Type.Nil
                    ? System.Array.Empty<Vector2>() : arrays[(int)Mesh.ArrayType.TexUV].AsVector2Array();
                int[] indices = arrays[(int)Mesh.ArrayType.Index].VariantType == Variant.Type.Nil
                    ? System.Array.Empty<int>() : arrays[(int)Mesh.ArrayType.Index].AsInt32Array();
                int count = indices.Length > 0 ? indices.Length : vertices.Length;
                for (int i = 0; i < count; i++)
                {
                    int source = indices.Length > 0 ? indices[i] : i;
                    surface.SetNormal(source < normals.Length
                        ? (normalBasis * normals[source]).Normalized() : Vector3.Up);
                    surface.SetUV(source < uvs.Length ? uvs[source] : Vector2.Zero);
                    surface.AddVertex(transform * vertices[source]);
                }
            }
        }
        return surface.Commit();
    }
}
