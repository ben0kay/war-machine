# WheelieBin001 — dark-green wheelie bin, black lid

**Status (2026-10-28):** Original self-contained GLB 2.0, canonical reusable Godot scene, shared size master and **SOC0 AutoBox** static collision authored in source. **Godot import, rendering, C# compilation and in-game physics still need local validation.** The bin has not been permanently placed in the world.

## Design

A realistic stylized two-wheel household refuse bin: **dark forest-green tapered moulded plastic body**, reinforced corners and front ribbing, raised lip, subtle moulded panel, rear grab handle, **separate matte-black closed lid** with a named rear `LidHinge` pivot, two black rubber tyres and dark-grey hubs/axle. No logo, textures, transparency, external assets or per-frame animation. **Original art.**

- GLB: [Artwork/WheelieBin001.glb](Artwork/WheelieBin001.glb). **1,092 triangles; 8 grouped meshes; 7 opaque PBR materials; about 70 KiB**. Body, wheels and lid use shared imported geometry/material definitions.
- Native approximate physical bounds: **0.68m width × 1.02m height × 0.85m depth** (including wheels/handles). +Y up, +Z **front**, rear wheels/hinge on -Z; ground reference Y=0, wheel contact near Y=0.
- Lid: GLB hierarchy has `LidHinge` at local **(0, 0.951, -0.365)** with `lid_LidShell`, `lid_LidEdges`, `lid_LidHingeDetails` children. This **prepares** future opening animations; no lid movement or lid-specific dynamic collision exists yet.

## Godot scene and authoring

- [WheelieBin001.tscn](WheelieBin001.tscn) is the canonical `WheelieBin001/Model/Artwork` prefab. Root uses the existing `PlaceableVisual.cs` without per-object C#, a copied mesh or a separate collision mesh file.
- [Configuration/WheelieBinMaster.tres](Configuration/WheelieBinMaster.tres) has `SizeMultiplier = Vector3(1,1,1)`, relative X width / Y height / Z depth. Duplicate or override for special-size individual bins; reinstantiate to update visuals and collider.
- **Collision choice:** `CollisionMode = 1 AutoBox` on the scene root. The shared SOC0 builder generates **one `StaticBody3D` + `BoxShape3D` on physics layer 1** at runtime, from final scaled GLB bounds. This is an **immovable** solid refuse bin, not a pushable physics body. Its full bounding box intentionally blocks the decorative wheel/handle gaps.
- The scene is automatically discoverable as `OBJECT · Containers / WheelieBins → WheelieBin001` from the current F1 catalogue. No spawner code or permanent world placements changed. Models preview in their own scene too, but the SOC0 runtime collision exists only in a running game.
- [Object authoring guide](../../../../OBJECT_AUTHORING_GUIDE.md) owns future bin variants and collision choices. If the user wants bins that **tip, roll, can be lifted or thrown**, convert them to IX1/IX2 `RigidBody3D` rather than mixing static and dynamic bodies. If the lid opens, revise the collider so it doesn't remain stuck in the closed position; current hinge is visual structure only.

## Local validation

Pull `implement-td0-td1`; let Godot import the GLB and open `WheelieBin001.tscn` to check green body, black lid, wheels, mesh names and proportions. Run `dotnet build war-machine.csproj` if available, then F6 `WORLD/World.tscn` and press F1. Select `Containers / WheelieBins → WheelieBin001`, spawn an instance and verify that the player cannot pass through; verify top-down and FPS, rotated and resized copies, and terrain grounding. Confirm collision in the **Remote** scene tree or physics collision-shape debug. No in-engine validation has been performed remotely.
