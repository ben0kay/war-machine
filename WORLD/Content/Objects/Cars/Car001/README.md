# Car001 — Abandoned compact hatchback

**Reusable stationary scene, now placed in both the active top-down world and inherited FPS test.** Car001 is a simple, weathered, boxy Earth-like car made using Godot C# procedural meshes. The **front-left driver door is already swung outward 58 degrees** when spawned. This is a fixed open pose, **not an animated or interactive opening mechanism**.

Scene: `res://WORLD/Content/Objects/Cars/Car001/Car001.tscn`.

The body is approximately **4.35m long × 1.83m wide × 1.81m tall**. Local forward faces **-Z**, driver side **-X**, base is at **Y=0**. Separate low-poly bonnet/hood, roof, front and rear glass, four tyres and hubs, bumpers, front/rear lights, door handles and interior seats/dashboard. The driver's door and its collision share one yawed hinge pivot.

**Collision:** static body for car shell and separate static body for the open door; Godot collision layer 1. No driving, rigid-body physics, engine sounds, loot, breakage or occupant simulation. Wheels and minor decorative details are non-colliding. An angled camera can see inside the open door.

**Folder convention:** `C/Car001Mesh.cs` exclusively builds the temporary 3D geometry (also `[Tool]` for Godot editor preview after C# build), `Configuration/Car001Settings.cs` and `Car001Master.tres` store tweakable dimensions, body/glass colours and door angle; `Artwork/` is reserved for an eventual imported model. `Car001.tscn` uses the existing CameraOccluder component for scenery fade.

**Reusable placement guidance:** Instance `Car001.tscn` in the active world at a reasonably level terrain position. Ground the car root via Terrain3D height, clear of the player/robot routes and at least 2m of free space on its **-X driver-door side**. Keep the same reusable prefab under Objects. Do not touch the parked first-person world. Coordinate future camera fading with the already included occluder child.

**Validation left for Godot:** C# compile, editor mesh preview, player/robot collision, outward open-door silhouette, terrain grounding and occluder behaviour. Source/resources reviewed only; no local Godot runtime used.

## Active placement

WorldController places the car beside the southeast fuel station at spawn offset (24,42), editable as Car001Offset in WORLD/Configuration/WorldMaster.tres. Root height is sampled once; generated mesh bounds include the open door when reserving tree clearance. Shared static collision and overhead fade are reused; no driving or door animation added. No rebake required. Local build/render/collision checks remain pending.
