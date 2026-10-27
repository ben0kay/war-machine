shader_type fog;

uniform sampler2D terrain_heights : filter_linear, repeat_disable;
uniform vec2 terrain_origin;
uniform float terrain_span = 80.0;
uniform float terrain_texels = 21.0;
uniform vec2 coverage_center;
uniform float coverage_radius = 30.0;
uniform float density = 0.22;
uniform float layer_height = 1.2;
uniform float noise_scale = 0.25;
uniform float drift_speed = 0.2;
uniform float contrast = 0.85;
uniform float turbulence = 0.15;
uniform vec2 wind_direction = vec2(1.0, 0.4);
uniform vec4 fog_color : source_color = vec4(0.64, 0.71, 0.73, 1.0);

float hash_value(vec2 p) {
    return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}
float noise_value(vec2 p) {
    vec2 cell = floor(p);
    vec2 f = fract(p);
    vec2 u = f * f * (3.0 - 2.0 * f);
    return mix(mix(hash_value(cell), hash_value(cell + vec2(1.0, 0.0)), u.x),
        mix(hash_value(cell + vec2(0.0, 1.0)), hash_value(cell + vec2(1.0, 1.0)), u.x), u.y);
}
void fog() {
    vec2 map_uv = (WORLD_POSITION.xz - terrain_origin) / terrain_span;
    // Pixel-centre mapping: the first/last sample represent the exact world-space endpoints.
    vec2 uv = (map_uv * (terrain_texels - 1.0) + 0.5) / terrain_texels;
    vec2 terrain = textureLod(terrain_heights, uv, 0.0).rg;
    float above = WORLD_POSITION.y - terrain.r;
    vec2 wind = wind_direction * TIME * drift_speed;
    vec2 p = WORLD_POSITION.xz * noise_scale;
    vec2 warp = vec2(noise_value(p * 0.55 - wind * 0.12),
        noise_value(p * 0.55 - wind * 0.12 + vec2(37.0, 11.0))) - 0.5;
    vec2 flow = p - wind * noise_scale + warp * turbulence * 3.0;
    float broad = noise_value(flow);
    float detail = noise_value(flow * 2.3 + vec2(above * 0.8, 19.0) + wind * noise_scale * 0.2);
    float ribbons = smoothstep(0.32, 0.72, broad * 0.68 + detail * 0.32);
    float wisps = mix(1.0, mix(0.08, 1.8, ribbons), contrast);
    float top = layer_height * mix(0.55, 1.45, broad);
    // Persistent foot-level veil plus intermittent knee/waist wisps; density tapers smoothly upward.
    float base = 0.2 * (1.0 - smoothstep(layer_height * 0.12, layer_height * 0.6, above));
    float smoky = wisps * (1.0 - smoothstep(top * 0.12, top, above));
    float envelope = smoothstep(-0.08, 0.12, above);
    float radius = length(WORLD_POSITION.xz - coverage_center);
    float coverage = 1.0 - smoothstep(coverage_radius * 0.6, coverage_radius, radius);
    float valid = smoothstep(0.98, 1.0, terrain.g);
    vec2 in_map = step(vec2(0.0), map_uv) * step(map_uv, vec2(1.0));
    DENSITY = density * (base + smoky * 0.8) * envelope * coverage * valid * in_map.x * in_map.y;
    ALBEDO = fog_color.rgb;
}
