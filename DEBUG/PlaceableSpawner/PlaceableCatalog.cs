// F1 debug-only discovery of canonical GLB placeables and their exported enum variants.
// Cached per menu instance: no scene scanning, reflection, or model instancing during idle.
using Godot;
using System;
using System.Collections.Generic;
using System.Reflection;

public sealed class PlaceableEntry
{
    public string Category { get; }
    public string AssetId { get; }
    public string VariantName { get; }
    public string DisplayName => VariantName.Length == 0 ? AssetId : $"{AssetId} · {VariantName}";
    public bool HoverForPreview { get; }
    public float MinimumSpawnDistance { get; }
    private readonly PackedScene _scene;
    private readonly PropertyInfo? _variantProperty;
    private readonly object? _variantValue;

    // =========================================================
    // Retain one canonical scene and optional exported enum selection per menu entry.
    public PlaceableEntry(string category, string id, string variantName, PackedScene scene,
        bool hover, float distance, PropertyInfo? property = null, object? value = null)
    {
        Category = category; AssetId = id; VariantName = variantName;
        _scene = scene; HoverForPreview = hover; MinimumSpawnDistance = distance;
        _variantProperty = property; _variantValue = value;
    }

    // =========================================================
    // Assign selected palette/finish BEFORE adding the instance to the tree and running _Ready.
    public Node3D? CreateInstance()
    {
        Node node = _scene.Instantiate();
        if (node is not PlaceableVisual visual) { node.Free(); return null; }
        try { _variantProperty?.SetValue(visual, _variantValue); }
        catch (Exception exception)
        {
            GD.PushWarning($"F1 preview {AssetId}: could not select {VariantName}: {exception.Message}");
            visual.Free(); return null;
        }
        return visual;
    }
}

public static class PlaceableCatalog
{
    private const string Root = "res://WORLD/Content/Assets/Objects";

    // =========================================================
    // Called once by F1 at startup; scan only known asset roots, never entire project.
    public static List<PlaceableEntry> Discover()
    {
        var results = new List<PlaceableEntry>();
        Scan(Root + "/Static", results);
        Scan(Root + "/Props", results);
        results.Sort((a, b) =>
        {
            int group = string.Compare(a.Category, b.Category, StringComparison.OrdinalIgnoreCase);
            if (group != 0) return group;
            group = string.Compare(a.AssetId, b.AssetId, StringComparison.OrdinalIgnoreCase);
            return group != 0 ? group : string.Compare(a.VariantName, b.VariantName, StringComparison.OrdinalIgnoreCase);
        });
        return results;
    }

    // =========================================================
    // Canonical asset identity: Folder/Folder.tscn. Ignore Artwork, Configuration, helper scenes.
    private static void Scan(string folder, List<PlaceableEntry> results)
    {
        if (!DirAccess.DirExistsAbsolute(folder)) return;
        string id = folder[(folder.LastIndexOf('/') + 1)..];
        string path = folder + "/" + id + ".tscn";
        if (ResourceLoader.Exists(path)) AddPlaceable(path, id, results);
        foreach (string child in DirAccess.GetDirectoriesAt(folder))
        {
            if (child.StartsWith('.') || child is "Artwork" or "Configuration" or "C" or "Shared") continue;
            Scan(folder + "/" + child, results);
        }
    }

    // =========================================================
    // Only safe standard PlaceableVisual wrappers are eligible; procedural/physics scenes stay excluded.
    // Enum exports on derived visuals (BedVisual, TableVisual, future assets) supply variants automatically.
    private static void AddPlaceable(string path, string id, List<PlaceableEntry> results)
    {
        var scene = ResourceLoader.Load<PackedScene>(path);
        if (scene == null) { GD.PushWarning($"F1 placeable: cannot load {path}"); return; }
        Node preview = scene.Instantiate();
        if (preview is not PlaceableVisual visual) { preview.Free(); return; }
        bool hover = visual.GetMeta("f1_preview_hover", false).AsBool();
        float distance = visual.GetMeta("f1_preview_min_distance", 0f).AsSingle();
        if (!float.IsFinite(distance)) distance = 0f;
        distance = Mathf.Clamp(distance, 0f, 100f);
        string category = Category(path);
        int initialCount = results.Count;
        foreach (PropertyInfo property in visual.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanWrite || !property.PropertyType.IsEnum
                || !Attribute.IsDefined(property, typeof(ExportAttribute))) continue;
            var values = Enum.GetValues(property.PropertyType);
            foreach (object value in values)
            {
                string name = Enum.GetName(property.PropertyType, value) ?? value.ToString() ?? "Variant";
                results.Add(new PlaceableEntry(category, id, PrettyName(name), scene, hover, distance, property, value));
            }
        }
        if (results.Count == initialCount)
            results.Add(new PlaceableEntry(category, id, "", scene, hover, distance));
        visual.Free();
    }

    // =========================================================
    // Directory category is stable and requires no user-maintained menu list or filename registry.
    private static string Category(string scenePath)
    {
        string relative = scenePath[(Root.Length + 1)..];
        string[] parts = relative.Split('/');
        string type = parts[0] == "Props" ? "PROP" : "OBJECT";
        string group = parts.Length > 3 ? string.Join(" / ", parts[1..^2]) : "Other";
        return type + " · " + group;
    }

    // =========================================================
    // Humanise enum values such as WarmSand -> Warm Sand without modifying asset scripts.
    private static string PrettyName(string value)
    {
        var text = new System.Text.StringBuilder(value.Length + 6);
        for (int i = 0; i < value.Length; i++)
        {
            char letter = value[i];
            if (letter == '_') { text.Append(' '); continue; }
            if (i > 0 && char.IsUpper(letter)
                && (char.IsLower(value[i - 1]) || char.IsDigit(value[i - 1]))) text.Append(' ');
            text.Append(letter);
        }
        return text.ToString();
    }
}
