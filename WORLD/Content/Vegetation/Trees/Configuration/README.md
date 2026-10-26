# Tree master settings

`TreeMaster.tres` is the saved Inspector-editable resource defined by `../C/TreeSettings.cs`. Changes apply on relaunch or explicit `TreePlacement.ApplySettings()`.

Defaults: 0.36m leaves / 0.18m width before variation and instance scale; three secondary twigs per main branch plus an inner crown cluster; 3,600-leaf cap per near variant. InnerCanopyScale changes inner crown fullness. LeavesPerCluster is a request capped by the shared LeafBudget, not an unbounded multiplier.

Generated LOD defaults: 110m transition with 20m margin, retain roughly one in six leaves at distance, fade out around 240m, distant shadows off. Reduced leaves use coarser silhouettes to preserve coverage. Fades depend on Forward+. Set UseGeneratedLod=false to keep one mesh level; outer distance fading remains. CastShadows governs all shadows; FarCastShadows can additionally enable distant ones. Tree root placement/seed/spacing/slope/clearing remain separate from shape settings. Native ranges apply per chunk/variant batch, not per individual tree.
