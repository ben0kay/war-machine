# Lessons Learned — War Machine

**Purpose:** A concise, growing record of concrete War Machine failures, their causes, confirmed fixes, and prevention checks. Read with [AGENTS.md](AGENTS.md), [CodePreferences.md](CodePreferences.md), and the root [README.md](README.md) **before coding**. Prefer the actual current-branch source when older notes disagree. Add future lessons only when they are supported by a reproducible failure, source inspection, or a validated correction.

## LL001 — Godot C# `[Tool]` preview exported a non-tool custom Resource (2026-10-31)

**Symptom:** After pulling the DF2.8 multiple-forest-zone integration, the Godot editor emitted repeated errors when loading the War Machine project:

```text
System.InvalidCastException: Unable to cast object of type 'Godot.Resource' to 'CanopyTreeSettings'.
at CanopyTreeVisual.SetGodotClassPropertyValue(...)
```

The FPS test also failed to start reliably during this incident. The repeated cast exception was specifically traced to **editor-time canopy preview resource assignment**; the screenshot alone did not establish that this exception was the direct cause of every game-process exit.

**Root cause:** `VEGETATION/Trees/CanopyTree/CanopyTreeVisual.cs` is marked `[Tool]` and declares `[Export] public CanopyTreeSettings Settings { get; set; }`. Its referenced custom resource script, `VEGETATION/Trees/CanopyTree/CanopyTreeSettings.cs`, originally had `[GlobalClass]` but **not `[Tool]`**. In this editor-loading context, Godot instantiated/deserialized the resource as a plain `Godot.Resource`, which could not be assigned to the strongly typed exported property. Both `CanopyTreeMaster.tres` and `CanopyTreeLeafierMaster.tres` already pointed to the right source script; rewriting tree art or forest settings was unnecessary.

**Fix applied:** Add `[Tool]` to the custom resource class while keeping `[GlobalClass]`; use separate attributes, as in the working source:

```csharp
[Tool]
[GlobalClass]
public partial class CanopyTreeSettings : Resource
{
    // Existing exported fields unchanged.
}
```

Changed script: `VEGETATION/Trees/CanopyTree/CanopyTreeSettings.cs`. The canopy preview scenes, .tres values, meshes, forest tree placement, tint, LOD, chunk logic, terrain and collision were **not** changed for this fix. Corrective commit: [6b2cdb4](https://github.com/ben0kay/war-machine/commit/6b2cdb45616a73a1cf7e48b8cbb1879fda9d2252).

**Observed validation:** After pulling and rebuilding/reopening Godot, the user reported successful startup. The game output showed **three approved forest zones**, with 425, 369 and 387 seeded DF2 trees respectively, 1,181 trees total. This confirms the project proceeded through all three forest initialization paths; it does not by itself measure FPS or guarantee absence of unrelated warnings.

**Prevention checklist:**

- When writing an editor-executed C# `[Tool]` node that exports a *custom typed C# Resource*, check that the referenced resource class is also marked `[Tool]` when it must be constructed/inspected by the editor.
- Keep `[GlobalClass]` for inspector-recognized custom resources where appropriate, and use separate `[Tool]` and `[GlobalClass]` declarations.
- Confirm the `.tres` `[ext_resource type="Script"]` path points to the actual C# resource script. A correct path is necessary but does not replace editor-time type compatibility.
- After pulling changes involving C# scripts or resource classes, **build the Godot .NET solution and reopen/reload the editor** before diagnosing new resource cast errors as world-generation failures. Review the **first substantive error and its stack trace**, not only downstream repeats.
- Distinguish **editor-time resource loading**, **C# compilation**, **scene initialization**, and **runtime game-process crashes**. If startup still fails after the resource errors are fixed, capture the new first runtime exception and isolate recently enabled zones without deleting or regenerating saved terrain.
- Avoid changing an entire forest or disabling features to work around a clearly identified editor resource type mismatch. Confirm the smallest root-cause fix first.

**Scope note:** Do not indiscriminately add `[Tool]` to every C# resource. This lesson applies to resources involved in editor-executed tool scripts; inspect the consumer and actual failure.
