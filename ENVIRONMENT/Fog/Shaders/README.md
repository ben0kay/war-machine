# Visibility fog shader

PlayerDistanceFog.gdshader reconstructs opaque world depth in Forward+/Mobile or Compatibility NDC coordinates, then alpha-blends a player-centred XZ fog ring over the scene. Uses one depth sample; no screen-color copy, volumetric simulation or TIME animation. Settings and known transparent-depth limitations are documented in ../README.md. Engine shader validation remains local.
