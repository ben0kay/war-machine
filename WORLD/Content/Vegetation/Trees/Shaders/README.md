# Individual leaf wind

`LeafWind.gdshader` affects only generated leaf surfaces. The generator encodes UV.y attachment weights (base=0, tip=1) on small folded leaves. TIME/world position provide independently phased flutter and a shared directional breeze. Shader work is guarded by a valid conditional block: zero wind strength/speed leaves geometry static, with no forbidden return in vertex(). A little backlighting keeps two-sided foliage readable.

Tune wind in TreeMaster; direction/shared strength come from EnvironmentMaster when settings are applied. No shader uniforms are rewritten per frame. Native Forward+ visibility range fading handles near/far and outer distance transitions; imported models retain their own materials.
