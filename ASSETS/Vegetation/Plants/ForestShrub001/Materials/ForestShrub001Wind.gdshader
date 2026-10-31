// ForestShrub001: opaque faceted leaves / muted stems / pale flower heads.
// COLOR.a is vertex-authored leaf selector, not transparency.
// Matches War Machine's shared foliage tint controls without tinting the pale flowers.
shader_type spatial;
render_mode cull_disabled, depth_draw_opaque;

global uniform vec4 wm_foliage_tint;
global uniform float wm_foliage_tint_strength;
uniform sampler2D shrub_atlas : source_color, filter_linear_mipmap_anisotropic;
uniform float wind_strength : hint_range(0.0, 0.2) = 0.028;
uniform float wind_speed : hint_range(0.0, 5.0) = 1.1;
uniform vec2 wind_direction = vec2(0.82, 0.57);
uniform float palette_response : hint_range(0.0, 1.0) = 0.85;
uniform float foliage_backlight_strength : hint_range(0.0, 0.35) = 0.10;

void vertex() {
    vec3 root_world = (MODEL_MATRIX * vec4(0.0, 0.0, 0.0, 1.0)).xyz;
    float anchored = smoothstep(0.08, 1.2, VERTEX.y);
    float phase = dot(root_world.xz, vec2(0.27, 0.71));
    float wave = sin(TIME * wind_speed + phase + VERTEX.y * 1.7)
        + 0.22 * sin(TIME * wind_speed * 0.62 + phase * 1.34);
    vec2 dir = normalize(wind_direction + vec2(0.00001));
    VERTEX.xz += dir * wave * wind_strength * anchored * anchored;
}

void fragment() {
    // One atlas/material surface: colour variation comes from atlas + vertex colours.
    vec3 authored = texture(shrub_atlas, UV).rgb * COLOR.rgb;
    float foliage_mask = clamp(COLOR.a, 0.0, 1.0);
    vec3 luma_weights = vec3(0.2126, 0.7152, 0.0722);
    float authored_luma = dot(authored, luma_weights);
    float target_luma = max(dot(wm_foliage_tint.rgb, luma_weights), 0.05);
    vec3 palette = clamp(wm_foliage_tint.rgb * authored_luma / target_luma, vec3(0.0), vec3(1.0));
    ALBEDO = mix(authored, palette, clamp(wm_foliage_tint_strength * palette_response * foliage_mask, 0.0, 1.0));
    ROUGHNESS = 0.93;
    SPECULAR = 0.25;
    METALLIC = 0.0;
    vec3 n = normalize(NORMAL);
    if (dot(n, VIEW) < 0.0) n = -n;
    NORMAL = n;
    BACKLIGHT = ALBEDO * foliage_backlight_strength * foliage_mask;
}
