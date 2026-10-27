// MAT1: object-attached brushed metal / matte rubber surface finish.
// Same shader resource supports distinct material presets; no dynamic lights or emissions.
shader_type spatial;
render_mode cull_back, depth_draw_opaque;

uniform vec4 base_color : source_color = vec4(0.28, 0.33, 0.36, 1.0);
uniform float metallic_level : hint_range(0.0, 1.0) = 0.80;
uniform float base_roughness : hint_range(0.0, 1.0) = 0.45;
uniform float grain_strength : hint_range(0.0, 1.0) = 0.18;
uniform float grime_amount : hint_range(0.0, 1.0) = 0.20;
uniform float pattern_scale : hint_range(1.0, 80.0) = 18.0;
uniform float grain_stretch : hint_range(0.05, 2.0) = 0.30;

varying vec3 object_pos;

void vertex() {
    vec3 scaled = vec3(length(MODEL_MATRIX[0].xyz),
                       length(MODEL_MATRIX[1].xyz),
                       length(MODEL_MATRIX[2].xyz));
    object_pos = VERTEX * scaled;
}

float hash3(vec3 p) {
    p = fract(p * 0.1031);
    p += dot(p, p.yxz + 33.33);
    return fract((p.x + p.y) * p.z);
}

float noise3(vec3 p) {
    vec3 i = floor(p), f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    float a = mix(hash3(i), hash3(i + vec3(1, 0, 0)), f.x);
    float b = mix(hash3(i + vec3(0, 1, 0)), hash3(i + vec3(1, 1, 0)), f.x);
    float c = mix(hash3(i + vec3(0, 0, 1)), hash3(i + vec3(1, 0, 1)), f.x);
    float d = mix(hash3(i + vec3(0, 1, 1)), hash3(i + vec3(1, 1, 1)), f.x);
    return mix(mix(a, b, f.y), mix(c, d, f.y), f.z);
}

void fragment() {
    vec3 p = object_pos * vec3(pattern_scale, pattern_scale * grain_stretch, pattern_scale);
    float grain = noise3(p + vec3(7.6, 2.7, 0.3));
    float broad = noise3(object_pos * 3.0 + vec3(2.4, 5.1, 6.8));
    float dirt = grime_amount * smoothstep(0.55, 0.85, broad) * 0.28;
    float patina = 1.0 + (grain - 0.5) * grain_strength * 0.45;
    ALBEDO = base_color.rgb * patina * (1.0 - dirt);
    METALLIC = metallic_level;
    ROUGHNESS = clamp(base_roughness + (grain - 0.5) * grain_strength * 0.50
                      + dirt * 0.20, 0.08, 1.0);
}
