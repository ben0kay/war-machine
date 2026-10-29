// ZMAT0: cached 64x64 skin, face, clothing and dried-blood finishes for both zombie types.
// Generates a small palette once, never one image/material per zombie; all choices come from spawn seed.
using Godot;

public static class ZombieAppearance
{
    private const int Size = 64;
    private const int Colours = 4;
    private enum Region { Skin, Face, Shirt, Pants }

    // Four colourways, plus clean/bloody face, shirt and trouser variants (28 shared textures).
    // Static initialization only: no PNG imports, per-frame generation, shaders or live material edits.
    private static readonly Material[] Skin = CreateSet(Region.Skin, Colours);
    private static readonly Material[] Faces = CreateSet(Region.Face, Colours * 2);
    private static readonly Material[] Shirts = CreateSet(Region.Shirt, Colours * 2);
    private static readonly Material[] Pants = CreateSet(Region.Pants, Colours * 2);

    // =========================================================
    // Select independent skin, shirt and trouser variants; only a fraction have blood.
    // The face, clothing and trousers can have different patches without per-actor textures.
    public static (Material Skin, Material Face, Material Shirt, Material Pants) Choose(
        uint seed, float bloodChance = 0.45f)
    {
        int skin = (int)(Mix(seed ^ 0xA341316Cu) % Colours);
        int face = (int)(Mix(seed ^ 0xC8013EA4u) % Colours);
        int shirt = (int)(Mix(seed ^ 0xAD90777Du) % Colours);
        int pants = (int)(Mix(seed ^ 0x7E95761Eu) % Colours);

        bool bloody = Mix(seed ^ 0x3C6EF372u) % 10000u
            < (uint)(Mathf.Clamp(bloodChance, 0f, 1f) * 10000f);
        int location = (int)(Mix(seed ^ 0xB54CDA56u) % 3u);
        if (bloody)
        {
            if (location != 1) face += Colours;      // face or both
            if (location != 0) shirt += Colours;     // shirt or both
            if (location == 2) pants += Colours;     // occasional trouser stain
        }
        return (Skin[skin], Faces[face], Shirts[shirt], Pants[pants]);
    }

    // =========================================================
    // Apply when world placement provides its stable seed, after the builder has made the meshes.
    // One short hierarchy walk; preserves eyes, hair, bruises, wounds, footwear and rig transforms.
    public static void Apply(Node3D visualRoot, uint seed, bool enabled = true, float bloodChance = 0.45f)
    {
        var materials = Choose(enabled ? seed : 0u, enabled ? bloodChance : 0f);
        ApplyChildren(visualRoot, materials);
    }

    // Material categories are shared by both per-part authors and ZM-PERF2A single-surface batches.
    // Names encode the palette explicitly after batching; non-palette bruises/boots keep their own materials.
    public enum PaletteRegion { None, Skin, Face, Shirt, Pants }

    // =========================================================
    public static PaletteRegion PaletteForPart(string name) => name switch
    {
        "SkinBatch" or "ExposedNeck" or "BentNeck" or "ExposedForearm"
            or "Forearm" or "Hand" or "Finger" => PaletteRegion.Skin,
        "FaceBatch" or "Skull" or "Jaw" or "Nose" or "Brow" => PaletteRegion.Face,
        "ShirtBatch" or "SlouchedTorso" or "LeanTorso" or "ShoulderShape"
            or "NarrowShoulders" or "TornCollar" or "ShirtHem" or "Shoulder"
            or "RaggedSleeve" or "Sleeve" or "TornShirtHem" => PaletteRegion.Shirt,
        "PantsBatch" or "Hips" or "Waist" or "Thigh" or "LongThigh"
            or "KneeJoint" or "Shin" or "LongShin" => PaletteRegion.Pants,
        _ => PaletteRegion.None
    };

    // =========================================================
    // Reassign only the palette category. Detail batches keep the exact original static material.
    private static void ApplyChildren(Node node,
        (Material Skin, Material Face, Material Shirt, Material Pants) appearance)
    {
        foreach (Node child in node.GetChildren())
        {
            if (child is MeshInstance3D mesh)
                switch (PaletteForPart(mesh.Name.ToString()))
                {
                    case PaletteRegion.Face: mesh.MaterialOverride = appearance.Face; break;
                    case PaletteRegion.Skin: mesh.MaterialOverride = appearance.Skin; break;
                    case PaletteRegion.Shirt: mesh.MaterialOverride = appearance.Shirt; break;
                    case PaletteRegion.Pants: mesh.MaterialOverride = appearance.Pants; break;
                }
            ApplyChildren(child, appearance);
        }
    }

    // =========================================================
    // Make the reusable fixed-size palette once using dirty fabric, mottled skin and muted blood.
    private static Material[] CreateSet(Region region, int count)
    {
        var palette = new Material[count];
        for (int i = 0; i < count; i++) palette[i] = Create(region, i);
        return palette;
    }

    // =========================================================
    // Localised elliptical dried-blood marks, blurred by procedural noise, remain in UV space.
    // Four base colourways per region; upper four variants carry blood where appropriate.
    private static Material Create(Region region, int variant)
    {
        int baseIndex = variant % Colours;
        bool blood = variant >= Colours;
        Color main = region switch
        {
            Region.Skin => baseIndex switch {
                0 => new Color(0.42f, 0.47f, 0.36f), 1 => new Color(0.54f, 0.51f, 0.41f),
                2 => new Color(0.37f, 0.43f, 0.39f), _ => new Color(0.46f, 0.45f, 0.37f) },
            Region.Face => baseIndex switch {
                0 => new Color(0.43f, 0.45f, 0.36f), 1 => new Color(0.51f, 0.47f, 0.39f),
                2 => new Color(0.36f, 0.41f, 0.38f), _ => new Color(0.47f, 0.42f, 0.34f) },
            Region.Shirt => baseIndex switch {
                0 => new Color(0.33f, 0.30f, 0.25f), 1 => new Color(0.27f, 0.32f, 0.30f),
                2 => new Color(0.35f, 0.29f, 0.27f), _ => new Color(0.29f, 0.32f, 0.24f) },
            _ => baseIndex switch {
                0 => new Color(0.25f, 0.29f, 0.30f), 1 => new Color(0.29f, 0.28f, 0.24f),
                2 => new Color(0.22f, 0.26f, 0.25f), _ => new Color(0.28f, 0.27f, 0.29f) }
        };
        Color stained = main.Darkened(region == Region.Shirt ? 0.39f : 0.28f);
        Color driedBlood = new(0.24f, 0.075f, 0.065f);
        int seed = (int)region * 313 + variant * 43 + 19;
        float cx = 0.28f + Hash(seed, 5, seed + 17) * 0.43f;
        float cy = 0.27f + Hash(7, seed, seed + 29) * 0.48f;
        float radiusX = region == Region.Face ? 0.13f : 0.21f;
        float radiusY = region == Region.Face ? 0.18f : 0.25f;

        using var image = Image.CreateEmpty(Size, Size, false, Image.Format.Rgba8);
        for (int y = 0; y < Size; y++)
        for (int x = 0; x < Size; x++)
        {
            float broad = Noise(x / 11f, y / 11f, seed) * 0.7f
                + Noise(x / 4.5f, y / 4.5f, seed + 31) * 0.3f;
            float grime = Mathf.SmoothStep(0.30f, 0.74f, broad) * (region == Region.Skin ? 0.48f : 0.65f);
            float fleck = (Hash(x, y, seed + 103) - 0.5f) * 0.055f;
            Color tint = main.Lerp(stained, grime);
            if (blood)
            {
                float u = (x + 0.5f) / Size, v = (y + 0.5f) / Size;
                float dx = (u - cx) / radiusX, dy = (v - cy) / radiusY;
                float spot = Mathf.Clamp(1f - dx * dx - dy * dy, 0f, 1f);
                float edge = Mathf.SmoothStep(0.02f, 0.50f, spot);
                float irregular = Mathf.Lerp(0.38f, 1f, Noise(x / 5f, y / 5f, seed + 211));
                tint = tint.Lerp(driedBlood, edge * irregular * 0.55f);
            }
            image.SetPixel(x, y, new Color(
                Mathf.Clamp(tint.R + fleck, 0f, 1f),
                Mathf.Clamp(tint.G + fleck, 0f, 1f),
                Mathf.Clamp(tint.B + fleck, 0f, 1f), 1f));
        }
        image.GenerateMipmaps();
        return new StandardMaterial3D
        {
            AlbedoTexture = ImageTexture.CreateFromImage(image),
            Roughness = 0.96f
        };
    }

    // =========================================================
    // Deterministic integer hashing avoids touching the zombie's gameplay movement RNG.
    private static uint Mix(uint v)
    {
        unchecked
        {
            v ^= v >> 16; v *= 0x7FEB352Du;
            v ^= v >> 15; v *= 0x846CA68Bu;
            return v ^ (v >> 16);
        }
    }

    // =========================================================
    private static float Noise(float x, float y, int seed)
    {
        int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y);
        float dx = x - ix, dy = y - iy;
        dx = dx * dx * (3f - 2f * dx); dy = dy * dy * (3f - 2f * dy);
        return Mathf.Lerp(Mathf.Lerp(Hash(ix, iy, seed), Hash(ix + 1, iy, seed), dx),
            Mathf.Lerp(Hash(ix, iy + 1, seed), Hash(ix + 1, iy + 1, seed), dx), dy);
    }

    // =========================================================
    private static float Hash(int x, int y, int seed)
    {
        uint n = unchecked((uint)(x * 374761393 ^ y * 668265263 ^ seed * 1274126177));
        n ^= n >> 13; n *= 1274126177u; n ^= n >> 16;
        return (n & 0xffff) / 65535f;
    }
}
