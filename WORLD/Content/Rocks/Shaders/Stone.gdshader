// Shared triplanar stone: packed CC0 terrain textures, normal detail and slope-weighted moss.
shader_type spatial;
render_mode cull_disabled;
uniform sampler2D stone_albedo : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D stone_normal : hint_normal, filter_linear_mipmap_anisotropic, repeat_enable;
uniform float texture_scale : hint_range(0.1, 5.0) = 0.8;
uniform float normal_strength : hint_range(0.0, 2.0) = 0.65;
uniform float moss_amount : hint_range(0.0, 1.0) = 0.35;
uniform float wetness : hint_range(0.0, 1.0) = 0.15;
uniform vec4 moss_color : source_color = vec4(0.18, 0.25, 0.085, 1.0);
varying vec3 world_position;
varying vec3 world_normal;
varying vec3 texture_position;
varying vec3 texture_normal;
varying vec3 rock_basis_x;
varying vec3 rock_basis_y;
varying vec3 rock_basis_z;
void vertex() {
    world_position = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
    world_normal = normalize(MODEL_NORMAL_MATRIX * NORMAL);
    // Per-instance coordinate frame follows each rock's rotation instead of aligning all stones to world axes.
    texture_position = VERTEX + MODEL_MATRIX[3].xyz * 0.137;
    texture_normal = NORMAL;
    rock_basis_x = MODEL_NORMAL_MATRIX[0];
    rock_basis_y = MODEL_NORMAL_MATRIX[1];
    rock_basis_z = MODEL_NORMAL_MATRIX[2];
}
void fragment() {
    vec3 n = normalize(texture_normal);
    vec3 surface_world_normal = normalize(world_normal);
    vec3 weights = pow(abs(n), vec3(4.0));
    weights /= max(dot(weights, vec3(1.0)), 0.0001);
    vec3 p = texture_position * texture_scale;
    vec3 signs = sign(n + vec3(0.00001));
    vec2 ux = p.zy * vec2(signs.x, 1.0);
    vec2 uy = p.xz * vec2(signs.y, 1.0);
    vec2 uz = p.xy * vec2(-signs.z, 1.0);
    vec3 stone = texture(stone_albedo, ux).rgb * weights.x
        + texture(stone_albedo, uy).rgb * weights.y + texture(stone_albedo, uz).rgb * weights.z;
    vec4 rx = texture(stone_normal, ux), ry = texture(stone_normal, uy), rz = texture(stone_normal, uz);
    // Axis tangent frames reorient each normal sample before blending in world space.
    vec3 bx = rx.rgb * 2.0 - 1.0, by = ry.rgb * 2.0 - 1.0, bz = rz.rgb * 2.0 - 1.0;
    vec3 nx = vec3(bx.z * signs.x, bx.y, bx.x * signs.x);
    vec3 ny = vec3(by.x * signs.y, by.z * signs.y, by.y);
    vec3 nz = vec3(-bz.x * signs.z, bz.y, bz.z * signs.z);
    vec3 detailed = normalize(nx * weights.x + ny * weights.y + nz * weights.z);
    float patch = stone.g * 0.65 + sin(world_position.x * 2.1 + sin(world_position.z * 2.7)) * 0.12;
    float moss = smoothstep(0.2, 0.75, surface_world_normal.y) * smoothstep(0.2, 0.65, patch + moss_amount * 0.65) * moss_amount;
    vec3 tint = mix(vec3(0.88), vec3(1.12), clamp(COLOR.rgb, vec3(0.0), vec3(1.0)));
    ALBEDO = mix(stone * tint, moss_color.rgb * (0.65 + stone * 0.7), moss) * (1.0 - wetness * 0.22);
    ROUGHNESS = clamp(mix(rx.a * weights.x + ry.a * weights.y + rz.a * weights.z, 0.94, moss) - wetness * 0.28, 0.3, 1.0);
    NORMAL = normalize((VIEW_MATRIX * vec4(normalize(mat3(rock_basis_x, rock_basis_y, rock_basis_z) * mix(n, detailed, clamp(normal_strength, 0.0, 1.0))), 0.0)).xyz);
}
