// Entrance-only hillside rock: world-aligned packed terrain textures, broad blocks, sharper normal detail.
shader_type spatial;
render_mode cull_disabled;
uniform sampler2D cliff_albedo : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D cliff_normal : hint_normal, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D ground_albedo : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform float texture_scale = 0.25;
uniform float normal_strength = 1.0;
varying vec3 world_position;
varying vec3 world_normal;
void vertex() {
    world_position = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
    world_normal = normalize(MODEL_NORMAL_MATRIX * NORMAL);
}
void fragment() {
    vec3 n = normalize(world_normal);
    vec3 weights = pow(abs(n), vec3(8.0));
    weights /= max(dot(weights, vec3(1.0)), 0.0001);
    vec3 p = world_position * texture_scale;
    vec3 signs = sign(n + vec3(0.00001));
    vec2 ux = p.zy * vec2(signs.x, 1.0);
    vec2 uy = p.xz * vec2(signs.y, 1.0);
    vec2 uz = p.xy * vec2(-signs.z, 1.0);
    vec3 stone = texture(cliff_albedo, ux).rgb * weights.x
        + texture(cliff_albedo, uy).rgb * weights.y + texture(cliff_albedo, uz).rgb * weights.z;
    vec4 rx = texture(cliff_normal, ux), ry = texture(cliff_normal, uy), rz = texture(cliff_normal, uz);
    vec3 bx = rx.rgb * 2.0 - 1.0, by = ry.rgb * 2.0 - 1.0, bz = rz.rgb * 2.0 - 1.0;
    vec3 nx = vec3(bx.z * signs.x, bx.y, bx.x * signs.x);
    vec3 ny = vec3(by.x * signs.y, by.z * signs.y, by.y);
    vec3 nz = vec3(-bz.x * signs.z, bz.y, bz.z * signs.z);
    vec3 detailed = normalize(nx * weights.x + ny * weights.y + nz * weights.z);
    // Match the terrain's cool macro tints; upward ledges pick up its ground texture rather than painted moss.
    float macro = 0.5 + 0.5 * sin(world_position.x * 0.04 + sin(world_position.z * 0.076));
    vec3 tint = mix(vec3(0.855, 0.8625, 0.9), vec3(0.9, 0.885, 0.81), macro);
    float ledge = smoothstep(0.6, 0.9, n.y);
    vec3 ground = texture(ground_albedo, world_position.xz * 0.2).rgb * vec3(0.67451, 0.74902, 0.686275);
    ALBEDO = mix(stone * tint, ground, ledge);
    ROUGHNESS = clamp(rx.a * weights.x + ry.a * weights.y + rz.a * weights.z - 0.05, 0.4, 1.0);
    vec3 shading_normal = normalize(mix(n, detailed, clamp(normal_strength, 0.0, 1.0) * (1.0 - ledge)));
    NORMAL = normalize((VIEW_MATRIX * vec4(shading_normal, 0.0)).xyz);
}
