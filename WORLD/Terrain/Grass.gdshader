// Shared GPU grass wind, inspired by Terrain3D's MIT particle grass example.
shader_type spatial;
render_mode cull_disabled;
uniform vec2 wind_direction = vec2(0.928, 0.371);
uniform float wind_strength = 0.22;
uniform float wind_speed = 1.4;
void vertex() {
    vec3 world = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
    float tip = clamp(VERTEX.y + 0.5, 0.0, 1.0);
    float sway = sin(TIME * wind_speed + world.x * 0.12 + world.z * 0.09);
    VERTEX.xz += wind_direction * sway * tip * tip * wind_strength;
}
void fragment() {
    ALBEDO = mix(vec3(0.10, 0.15, 0.035), vec3(0.32, 0.39, 0.12), 1.0 - UV.y);
    ROUGHNESS = 0.9;
    BACKLIGHT = vec3(0.25);
}
