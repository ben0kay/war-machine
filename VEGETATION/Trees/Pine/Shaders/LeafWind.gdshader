// Per-leaf vertex flutter: each pointed leaf is attached at UV.y=0 and free at UV.y=1.
shader_type spatial;
render_mode cull_back, depth_draw_opaque;

uniform bool use_camera_occlusion = false;
varying float camera_opacity;

uniform vec2 wind_direction = vec2(1.0, 0.4);
uniform float wind_strength : hint_range(0.0, 2.0) = 0.55;
uniform float wind_speed : hint_range(0.0, 6.0) = 1.8;
uniform float flutter_strength : hint_range(0.0, 2.0) = 0.65;

void vertex() {
    camera_opacity = use_camera_occlusion ? INSTANCE_CUSTOM.r : 1.0;
    if (wind_strength > 0.001 && wind_speed > 0.001) {
    vec3 world_vertex = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
    float phase = dot(world_vertex.xz, vec2(0.73, 1.19)) + world_vertex.y * 0.41;
    float breeze = sin(TIME * wind_speed + phase * 0.52) * 0.65
        + sin(TIME * wind_speed * 0.67 + phase * 0.91) * 0.28;
    float flutter = 0.0;
    if (flutter_strength > 0.001) flutter = sin(TIME * wind_speed * 3.6 + phase * 3.4) * flutter_strength;
    vec2 direction = normalize(wind_direction + vec2(0.00001));
    // Displace in world direction so rotated MultiMesh trees all bend with the same breeze.
    vec3 movement_world = vec3(direction.x, breeze * 0.16, direction.y)
        * (breeze * 0.18 + flutter * 0.09) * wind_strength * UV.y;
    VERTEX += transpose(mat3(MODEL_MATRIX)) * movement_world / dot(MODEL_MATRIX[0].xyz, MODEL_MATRIX[0].xyz);
    }
}

void fragment() {
    // Ordered coverage fade retains the opaque path and avoids sorting entire transparent MultiMesh canopies.
    const float bayer[16] = float[16](0.0, 8.0, 2.0, 10.0, 12.0, 4.0, 14.0, 6.0, 3.0, 11.0, 1.0, 9.0, 15.0, 7.0, 13.0, 5.0);
    ivec2 pixel = ivec2(mod(floor(FRAGCOORD.xy), 4.0));
    if (camera_opacity < (bayer[pixel.y * 4 + pixel.x] + 0.5) / 16.0) discard;
    ALBEDO = COLOR.rgb;
    ROUGHNESS = 0.92;
    BACKLIGHT = vec3(0.18);
}
