// MAT1: object-attached procedural paint wear, edge chips and irregular roughness.
// Operates in each mesh's local coordinates (with geometric scale applied), not world space.
shader_type spatial;
render_mode cull_back, depth_draw_opaque;

uniform vec4 base_color : source_color = vec4(0.14, 0.18, 0.21, 1.0);
uniform vec3 bare_metal_color : source_color = vec3(0.31, 0.34, 0.36);
uniform float metallic_level : hint_range(0.0, 1.0) = 0.75;
uniform float base_roughness : hint_range(0.0, 1.0) = 0.60;
uniform float wear_amount : hint_range(0.0, 1.0) = 0.38;
uniform float dirt_amount : hint_range(0.0, 1.0) = 0.28;
uniform float scratch_amount : hint_range(0.0, 1.0) = 0.30;

varying vec3 shape_pos;
varying vec3 primitive_pos;

// Stable object coordinates account for the scale on the generated box/cylinder.
// World position would cause painted texture to "swim" while the robot walks.
void vertex() {
    vec3 scaled = vec3(length(MODEL_MATRIX[0].xyz),
                       length(MODEL_MATRIX[1].xyz),
                       length(MODEL_MATRIX[2].xyz));
    shape_pos = VERTEX * scaled;
    primitive_pos = VERTEX;
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
    float broad = noise3(shape_pos * 2.9 + vec3(4.7, 0.0, 2.1));
    float blotch = noise3(shape_pos * 9.0 + vec3(8.2, 4.6, 1.3));
    // Box primitives are unit cubes: intersecting face limits identify the plate edges.
    vec3 e = smoothstep(vec3(0.42), vec3(0.50), abs(primitive_pos));
    float edge = max(e.x * e.y, max(e.x * e.z, e.y * e.z));
    float chip = max(edge * smoothstep(0.33, 0.72, blotch),
                     smoothstep(0.73, 0.88, blotch) * 0.50) * wear_amount;
    // Thin streaks follow the solid's local vertical direction, with noise breaking them up.
    float brushed = noise3(shape_pos * vec3(21.0, 0.75, 24.0));
    float scratches = smoothstep(0.69, 0.92, brushed) * scratch_amount * 0.42;
    float exposed = clamp(chip + scratches, 0.0, 0.75);
    float dirt = dirt_amount * smoothstep(0.46, 0.84, broad) * 0.48;
    vec3 paint = base_color.rgb * (1.0 - dirt);
    ALBEDO = mix(paint, bare_metal_color, exposed);
    METALLIC = clamp(mix(metallic_level, 0.89, exposed), 0.0, 1.0);
    ROUGHNESS = clamp(base_roughness + (blotch - 0.5) * 0.22 + dirt * 0.15
                      - exposed * 0.18, 0.12, 0.98);
}
