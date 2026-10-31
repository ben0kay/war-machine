// Billboard colour/alpha recovery: darker rooted blades, subtle lift for baked near-black tips.
// UVs, shared atlas, mesh geometry and alpha mask stay unchanged.
shader_type spatial;
render_mode cull_disabled;
global uniform vec4 wm_foliage_tint;
global uniform float wm_foliage_tint_strength;
uniform sampler2D grass_atlas : source_color, filter_linear_mipmap_anisotropic, repeat_disable;
uniform float alpha_cutoff : hint_range(0.05,0.7) = 0.24;
uniform float root_darkening : hint_range(0.0, 0.8) = 0.52;
uniform float tip_luminance_floor : hint_range(0.0, 0.25) = 0.12;
uniform float wind_strength : hint_range(0.0,0.2) = 0.025;
uniform float wind_speed : hint_range(0.0,5.0) = 0.9;
uniform vec2 wind_direction = vec2(0.82, 0.57);
uniform bool distance_fade_enabled = false;
uniform float sparse_distance = 44.0;
uniform float end_distance = 68.0;
varying flat float presence;
varying float blade_height;
void vertex() {
    blade_height = VERTEX.y; // Local authored height before wind/visibility scaling.
    vec3 base = (MODEL_MATRIX * vec4(0.0, 0.0, 0.0, 1.0)).xyz;
    float d = distance(base.xz, CAMERA_POSITION_WORLD.xz);
    presence = distance_fade_enabled ? 1.0 - smoothstep(sparse_distance, end_distance, d) : 1.0;
    float anchored = smoothstep(0.04, 0.65, VERTEX.y);
    float offset = dot(base.xz, vec2(0.27, 0.71));
    float wave = sin(TIME * wind_speed + offset + VERTEX.y * 1.9);
    VERTEX.xz += normalize(wind_direction) * wave * wind_strength * anchored * anchored;
    VERTEX.y *= presence;
}
void fragment() {
    if (presence < 0.001) discard;
    vec4 t = texture(grass_atlas, UV);
    vec3 original = t.rgb * COLOR.rgb;
    vec3 weights = vec3(0.2126, 0.7152, 0.0722);
    // The source billboard atlas was baked with bright roots and very dark thin tips.
    // This corrects its colour distribution without inverting the correctly authored UVs.
    // A local-space height is stable under the existing instance scale and wind animation.
    float root_weight = 1.0 - smoothstep(0.02, 0.50, blade_height);
    original *= 1.0 - root_darkening * root_weight;
    float tip_weight = smoothstep(0.24, 0.72, blade_height);
    float current_luma = dot(original, weights);
    float desired_floor = tip_luminance_floor * tip_weight;
    vec3 natural_green = vec3(0.24, 0.40, 0.17);
    original += natural_green * (max(desired_floor - current_luma, 0.0)
        / max(dot(natural_green, weights), 0.01));
    float luminance = dot(original, weights);
    float target_luminance = max(dot(wm_foliage_tint.rgb, weights), 0.05);
    vec3 remapped = clamp(wm_foliage_tint.rgb * (luminance / target_luminance), vec3(0.0), vec3(1.0));
    ALBEDO = mix(original, remapped, clamp(wm_foliage_tint_strength * 0.85, 0.0, 1.0));
    ALPHA = t.a * COLOR.a;
    ALPHA_SCISSOR_THRESHOLD = alpha_cutoff;
    ROUGHNESS = 0.94;
    METALLIC = 0.0;
}
