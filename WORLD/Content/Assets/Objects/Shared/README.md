# Shared GLB placeable visuals, size masters and SOC0 static collision

Every catalogued GLB placeable has its own `Configuration/<Asset>Master.tres` with `SizeMultiplier = Vector3(1, 1, 1)`. X controls model width, Y height/thickness and Z depth/length as a **relative multiplier**, not as a size in metres. `Vector3(1.2, 1, 1)` is 20% wider.

`PlaceableVisual.cs` applies that size once on `Model`. On runtime readiness, it optionally invokes `StaticObjectCollision.Build()` to create one `StaticBody3D` and one or more `CollisionShape3D` / `BoxShape3D` primitives beside the model. Collision shapes are **not renderable meshes**, and the GLB is never regenerated or modified.

## SOC0 collision selector — inherited by every PlaceableVisual prefab

The scene root has an Inspector `CollisionMode` dropdown with:
- `0 None`: no physics (loose Book001, PineTree001 high-detail visual comparison, future decorations).
- `1 AutoBox`: derive **one** axis-aligned box from the eight corners of all imported meshes' local bounds after applying master scale. Use for ordinary fixed furniture/hedges, refrigerators, wheelie bins, etc. This intentionally fills decorative holes (under a table or inside shelving).
- `2 CustomBoxes`: enter a list of `StaticCollisionBox` subresources in scene Inspector, each with `Center` and `Size` in **original, unscaled Model-local metres**. This supports a bus shelter with separate roof, posts and rear panel, or a bookshelf with accessible shelf gaps. Invalid/empty custom lists fail safely without a phantom blocker.

Auto and custom boxes are generated **only once per runtime instance**, with physics layer/mask 1 (world solids), matching player/zombie movement, weapon shots and head/torso visibility checks. The physics body is not a child of the nonuniformly scaled Model, avoiding unsupported nonuniform CollisionShape3D transforms. The auto bounds use the final scaled mesh transforms. Custom positions and dimensions are scaled componentwise from the shared `SizeMultiplier`; use axis-aligned boxes in the authored model's local orientation for best accuracy.

For instance variants, duplicate/override the master resource; edit and reinstantiate or reload to rebuild the collider. Changing the size resource at runtime does not live-update physics. A future movable/throwable bin should use the separate IX RigidBody3D system, **not** this fixed collision.

**F1:** Current Bookcase001, Bed001, Table001 and Hedge001 now set `CollisionMode = 1`; their temporary F1 instances are solid until cleared. Book001 and PineTree001 remain `None`. The debug catalogue scanner only enumerates real C# palette enums, so the `int`-backed CollisionMode Inspector enum doesn't add fake F1 variants. No permanent placements are created.

**Performance:** no imported triangle collider, convex decomposition, per-frame mesh/bounds work, RigidBody simulation, timers or extra update loops. The only additional persistent physics work is native static shape presence while each object is active in a scene; resource instances remain shared. F1 previews are limited by the existing max 24 entries. Live collision loading near many objects should still be profiled locally.

**Acceptance:** `dotnet build war-machine.csproj`; in `WORLD/World.tscn` (top-down) and `WORLD/FirstPerson/FirstPersonWorld.tscn` (FPS), F1 spawn solid bookcase/table/bed/hedge and ensure player cannot phase through them. Test orientation, 2× size multiplier, rotated preview, aiming/shooting/LOS through cover, disabled mode, native custom boxes (e.g. table top + legs) and clearing 24 previews. Check no Godot resource/C# import errors. This implementation has **not been compiled or playtested here**.

## WS1 world rendering

Active-world static GLB wrappers inherit fog-aware native ranges from WORLD/Rendering via scene lifecycle registration under PersistentPropsRoot. PlaceableVisual still has no per-object distance loop. Final model scale/spawn transform is captured after Ready. Later explicit transform edits must notify FogRenderPolicy.Refresh; visibility and physics are separate.
