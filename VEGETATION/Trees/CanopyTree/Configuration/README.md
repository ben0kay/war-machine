# DF1 canopy art master

`CanopyTreeMaster.tres` / `CanopyTreeSettings.cs` own **tree appearance only**, not forest centre/size, density, spawn or exclusion settings. Those remain in `VEGETATION/Forest/Configuration/ForestMaster.tres` and future DF2 distribution settings.

Tunables: seed, 17–22m height range, crown start fraction 0.49, crown radius 3.2m, trunk radius 0.39m, 13 major branches, 4 leaf clusters per branch, foliage size and bark/foliage colour palette. `FarFoliageScale=1.2` compensates for omitted clumps in cheaper distant art. Variants 0–2 are seeded silhouettes, not separate prefabs. The younger filler demo in `CanopyTreePreview.tscn` uses `scale=0.65` and `PreviewFarMesh=true`; DF2 may later expose younger-tree scale as a placement parameter without duplicating this art master.

Open [../CanopyTreePreview.tscn](../CanopyTreePreview.tscn) and F6 after editing the resource; restart scene to regenerate the tree preview. No forest/world changes occur from this master at DF1.
