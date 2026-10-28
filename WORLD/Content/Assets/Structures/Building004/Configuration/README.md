# Building004 authored master

`Building004Master.tres` uses `Building004Settings.cs : StructureSettings` with editable 18×14 footprint, ground/upper floor heights, brick/concrete/metal/glass/accent colours and standard shared obstruction/indoor fade settings. Runtime visibility deep-copies its master. Geometry is built once; changing the Inspector resource after startup requires re-instancing or an explicit `StructureVisibility.Rebuild` hook—not a polling update.
