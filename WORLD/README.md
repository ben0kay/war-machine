# WORLD — Ownership and documentation

This folder is the intended home of the **integrated 3D wilderness**: the main playable world scene, terrain/landscape, world generation, region loading, and authored landmarks. It owns world layout and streaming coordination; other systems own vegetation rendering, weather, player movement and future entities.

**Current status:** documentation scaffold only. Paths and classes discussed in plans are proposed until they actually appear in the repository.

## Current plan

- [ENVIRONMENT_IMPLEMENTATION_PLAN.md](ENVIRONMENT_IMPLEMENTATION_PLAN.md) — the initial environment-only implementation phases, audit questions, dependencies, performance and pass criteria.

## When implementation begins

Prefer a single playable `WorldRoot.tscn` used by the project entry point. Add a world/master configuration for routinely tweaked settings, clear ownership boundaries and feature notes alongside implemented systems. Confirm addons from files and editor state before assuming they work.

Read the [root README](../README.md) and [CodePreferences](../CodePreferences.md) before modifying this area. Integrate accepted functionality into the main world rather than leaving it in a temporary test scene.
