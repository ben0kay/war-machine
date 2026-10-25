# WORLD — Ownership and documentation

This folder is the intended home of the **integrated 3D wilderness**: first the owner's requested playable `testworld.tscn`, then potentially a promoted main world scene, plus terrain/landscape, world generation, region loading, and authored landmarks. It owns world layout and streaming coordination; other systems own vegetation rendering, weather, player movement and future entities.

**Current status:** documentation scaffold only. Paths and classes discussed in plans are proposed until they actually appear in the repository.

## Current plan

- [ENVIRONMENT_IMPLEMENTATION_PLAN.md](ENVIRONMENT_IMPLEMENTATION_PLAN.md) — the initial environment-only implementation phases, audit questions, dependencies, performance and pass criteria.

## When implementation begins

**Current first implementation target: `res://WORLD/testworld.tscn`.** Use it as the directly playable world (not a throwaway feature demo), with a Terrain3D-generated/saved landscape, demo-quality cliff/grass blending, some wind-moving grass and **Sky3D's moving sun**. Build reusable modules and promote them into a future `WorldRoot.tscn` only when authorized; do not build two worlds in parallel. Add a world/master configuration for routinely tweaked settings, clear ownership boundaries and feature notes alongside implemented systems. Confirm addons from files and editor state before assuming they work.

Read the [root README](../README.md) and [CodePreferences](../CodePreferences.md) before modifying this area. Integrate accepted functionality into `testworld.tscn` throughout initial environment development rather than leaving it only in third-party demos.
