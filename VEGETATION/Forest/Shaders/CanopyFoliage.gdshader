// DF2 shared opaque vertex-colour canopy shader. Per-instance red channel fades only foliage in top-down.
shader_type spatial;
render_mode cull_disabled, depth_draw_opaque;
// Shared source-preserving tint for DF2 broadleaf crowns as well as stylized pines/grass.
global uniform vec4 wm_foliage_tint;
global uniform float wm_foliage_tint_strength;
varying float canopy_opacity;
void vertex() {
    canopy_opacity = INSTANCE_CUSTOM.r;
}
void fragment() {
    const float bayer[16] = float[16](0.0, 8.0, 2.0, 10.0, 12.0, 4.0, 14.0, 6.0, 3.0, 11.0, 1.0, 9.0, 15.0, 7.0, 13.0, 5.0);
    ivec2 cell = ivec2(mod(floor(FRAGCOORD.xy), 4.0));
    if (canopy_opacity < (bayer[cell.y * 4 + cell.x] + 0.5) / 16.0) discard;
    vec3 source_color = COLOR.rgb;
    vec3 weights = vec3(0.2126, 0.7152, 0.0722);
    float luma = dot(source_color, weights);
    float tint_luma = max(dot(wm_foliage_tint.rgb, weights), 0.05);
    vec3 remapped = clamp(wm_foliage_tint.rgb * (luma / tint_luma), vec3(0.0), vec3(1.0));
    ALBEDO = mix(source_color, remapped, clamp(wm_foliage_tint_strength * 0.85, 0.0, 1.0));
    ROUGHNESS = 0.94;
    BACKLIGHT = vec3(0.12);
}
