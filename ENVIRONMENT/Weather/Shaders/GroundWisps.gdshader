shader_type fog;

uniform float density = 0.18;
uniform float layer_height = 1.1;
uniform float ground_height = 0.0;
uniform vec2 ground_slope = vec2(0.0);
uniform float noise_scale = 0.35;
uniform float drift_speed = 0.3;
uniform float contrast = 0.8;
uniform vec2 wind_direction = vec2(1.0, 0.4);

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
    vec2 offset = WORLD_POSITION.xz - OBJECT_POSITION.xz;
    float floor_y = ground_height + dot(offset, ground_slope);
    float above_ground = WORLD_POSITION.y - floor_y;
    vec2 drift = wind_direction * TIME * drift_speed;
    vec2 p = WORLD_POSITION.xz * noise_scale;
    float broad = noise_value(p + drift);
    float detail = noise_value(p * 2.3 - drift * 0.6 + vec2(above_ground * 0.7, 19.0));
    float pattern = smoothstep(0.25, 0.75, broad * 0.7 + detail * 0.3);
    float wisps = mix(1.0, mix(0.08, 2.2, pattern), contrast);
    float top = layer_height * mix(0.6, 1.3, broad);
    float height_fade = smoothstep(-0.2, 0.1, above_ground) * (1.0 - smoothstep(top * 0.25, top, above_ground));
    vec2 edge = smoothstep(vec2(0.0), vec2(0.13), UVW.xz) * (vec2(1.0) - smoothstep(vec2(0.87), vec2(1.0), UVW.xz));
    float bounds = step(0.0, UVW.y) * step(UVW.y, 1.0);
    DENSITY = density * wisps * height_fade * edge.x * edge.y * bounds;
    ALBEDO = vec3(0.72, 0.78, 0.8);
}
