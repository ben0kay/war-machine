// ForestFern001: true geometric fern leaflets, no alpha cards or sorting.
// Shares War Machine foliage tint and lightweight GPU wind with ForestGrass001.
// COLOR.a is leaf selection (stem = 0, leaflet = 1), NOT opacity.
shader_type spatial;
render_mode cull_disabled, depth_draw_opaque;

global uniform vec4 wm_foliage_tint;
global uniform float wm_foliage_tint_strength;
uniform sampler2D fern_atlas : source_color, filter_linear_mipmap_anisotropic;
uniform float wind_strength : hint_range(0.0, 0.2) = 0.035;
uniform float wind_speed : hint_range(0.0, 5.0) = 1.1;
uniform vec2 wind_direction = vec2(0.82, 0.57);
uniform float palette_response : hint_range(0.0, 1.0) = 0.85;
uniform float foliage_backlight_strength : hint_range(0.0, 0.35) = 0.10;

void vertex() {
    vec3 root_world = (MODEL_MATRIX * vec4(0.0, 0.0, 0.0, 1.0)).xyz;
    float anchored = smoothstep(0.075, 1.50, VERTEX.y);
    float phase = dot(root_world.xz, vec2(0.27, 0.71));
    float wave = sin(TIME * wind_speed + phase + VERTEX.y * 1.55)
        + 0.25 * sin(TIME * wind_speed * 0.58 + phase * 1.37);
    vec2 dir = normalize(wind_direction + vec2(0.00001));
    VERTEX.xz += dir * wave * wind_strength * anchored * anchored;
}

void fragment() {
    vec3 original = texture(fern_atlas, UV).rgb * COLOR.rgb;
    float leaves = clamp(COLOR.a, 0.0, 1.0);
    vec3 weights = vec3(0.2126, 0.7152, 0.0722);
    float lightness = dot(original, weights);
    float target_lightness = max(dot(wm_foliage_tint.rgb, weights), 0.05);
    vec3 matched = clamp(wm_foliage_tint.rgb * (lightness / target_lightness), vec3(0.0), vec3(1.0));
    ALBEDO = mix(original, matched, clamp(wm_foliage_tint_strength * palette_response * leaves, 0.0, 1.0));
    ROUGHNESS = 0.93;
    SPECULAR = 0.25;
    METALLIC = 0.0;
    vec3 n = normalize(NORMAL);
    if (dot(n, VIEW) < 0.0) n = -n;
    NORMAL = n;
    BACKLIGHT = ALBEDO * foliage_backlight_strength * leaves;
}
