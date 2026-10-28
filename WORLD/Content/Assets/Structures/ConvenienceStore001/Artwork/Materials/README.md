# WT1 — weathered painted-stucco resources

- `StuccoColor.tres` — 256×256 seamless, mipmapped FBm noise with a muted warm-grey Gradient for mottled/weathered paint colour; tinted by the master's `Walls` colour.
- `StuccoNormal.tres` — 256×256 seamless, mipmapped higher-frequency noise exported as a fine-grain **normal map**. The source material's `WallBumpStrength` controls its strength or disables it completely at 0.

Both are `NoiseTexture2D` **texture resources**, not separate procedural wall mesh generators, per-frame noise shaders or exported PNGs. Godot generates each texture and caches it as a resource; each store uses the same resources on its wall materials. The scene uses **StandardMaterial3D** with world-space triplanar mapping, so a long wall segment is not stretched across a single UV tile and shared exterior camera-fade blocker detection still recognizes its opaque walls.

**Change the look without touching C#:** adjust `../../Configuration/ConvenienceStoreMaster.tres` `UseTexturedWalls`, `Walls`, `WallTextureRepeatsPerMeter`, `WallBumpStrength`, `WallRoughness`. Respawn/reinstance after edits. A single store uses two material instances (exterior/interior) that share both textures; no changes to trim, roof or window holes.

**Performance note:** triplanar albedo/normal requires extra GPU texture samples compared with plain paint, so this is a trial for one store, not an assumption that it scales to hundreds of buildings. Local Godot import, smooth/tile appearance, night lighting, camera fading and actual FPS must be checked.
