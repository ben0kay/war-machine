// STG1: imported ForestGrass001 ribbon geometry, shared atlas and native GPU wind.
// Reads project-global foliage tint alongside stylized pine and DF2 crowns; no particle system.
shader_type spatial;
render_mode cull_disabled, depth_draw_opaque;

global uniform vec4 wm_foliage_tint;
global uniform float wm_foliage_tint_strength;
uniform sampler2D grass_atlas : source_color, filter_linear_mipmap_anisotropic;
uniform float wind_strength : hint_range(0.0, 0.2) = 0.045;
uniform float wind_speed : hint_range(0.0, 5.0) = 1.1;
uniform vec2 wind_direction = vec2(0.82, 0.57);
uniform float foliage_backlight_strength : hint_range(0.0, 0.35) = 0.12;
uniform vec3 foliage_tint : source_color = vec3(1.0);
uniform float palette_response : hint_range(0.0, 1.0) = 0.85;

void vertex() {
    vec3 root_world = (MODEL_MATRIX * vec4(0.0, 0.0, 0.0, 1.0)).xyz;
    float anchored = smoothstep(0.06, 0.74, VERTEX.y);
    float offset = dot(root_world.xz, vec2(0.27, 0.71));
    float wave = sin(TIME * wind_speed + offset + VERTEX.y * 1.9)
        + 0.28 * sin(TIME * wind_speed * 0.61 + offset * 1.4);
    VERTEX.xz += normalize(wind_direction + vec2(0.00001)) * wave
        * wind_strength * anchored * anchored;
}

void fragment() {
    // Original fine-blade atlas and vertex colours survive the static MultiMesh conversion.
    vec3 original = texture(grass_atlas, UV).rgb * COLOR.rgb * foliage_tint;
    vec3 weights = vec3(0.2126, 0.7152, 0.0722);
    float luminance = dot(original, weights);
    float target_luminance = max(dot(wm_foliage_tint.rgb, weights), 0.05);
    vec3 remapped = clamp(wm_foliage_tint.rgb * (luminance / target_luminance), vec3(0.0), vec3(1.0));
    ALBEDO = mix(original, remapped, clamp(wm_foliage_tint_strength * palette_response, 0.0, 1.0));
    ROUGHNESS = 0.93;
    SPECULAR = 0.25;
    METALLIC = 0.0;
    vec3 n = normalize(NORMAL);
    if (dot(n, VIEW) < 0.0) n = -n;
    NORMAL = n;
    BACKLIGHT = ALBEDO * foliage_backlight_strength;
}
