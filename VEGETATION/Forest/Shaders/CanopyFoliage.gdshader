// DF2 shared opaque vertex-colour canopy shader. Per-instance red channel fades only foliage in top-down.
shader_type spatial;
render_mode cull_disabled, depth_draw_opaque;
varying float canopy_opacity;
void vertex() {
    canopy_opacity = INSTANCE_CUSTOM.r;
}
void fragment() {
    const float bayer[16] = float[16](0.0, 8.0, 2.0, 10.0, 12.0, 4.0, 14.0, 6.0, 3.0, 11.0, 1.0, 9.0, 15.0, 7.0, 13.0, 5.0);
    ivec2 cell = ivec2(mod(floor(FRAGCOORD.xy), 4.0));
    if (canopy_opacity < (bayer[cell.y * 4 + cell.x] + 0.5) / 16.0) discard;
    ALBEDO = COLOR.rgb;
    ROUGHNESS = 0.94;
    BACKLIGHT = vec3(0.12);
}
