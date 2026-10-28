// PineTree001 GLB foliage: one shared shader per colour surface with MultiMesh canopy fade + subtle wind.
// Input mesh requires no UVs; wind weighting comes from authored local leaf height instead.
shader_type spatial;
render_mode cull_back, depth_draw_opaque;

uniform vec4 foliage_tint : source_color = vec4(0.1, 0.25, 0.14, 1.0);
uniform bool use_camera_occlusion = true;
uniform vec2 wind_direction = vec2(1.0, 0.0);
uniform float wind_strength : hint_range(0.0, 2.0) = 0.15;
uniform float wind_speed : hint_range(0.0, 12.0) = 1.8;
varying float camera_opacity;

void vertex() {
    camera_opacity = use_camera_occlusion ? INSTANCE_CUSTOM.r : 1.0;
    if (wind_strength > 0.001 && wind_speed > 0.001) {
        vec3 world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
        float tip = smoothstep(2.5, 13.5, VERTEX.y);
        float phase = dot(world_pos.xz, vec2(0.71, 1.11)) + VERTEX.y * 0.34;
        float breeze = sin(TIME * wind_speed * 0.65 + phase) * 0.65
                     + sin(TIME * wind_speed * 0.41 + phase * 1.73) * 0.25;
        vec2 dir = normalize(wind_direction + vec2(0.00001));
        vec3 world_shift = vec3(dir.x, 0.0, dir.y) * breeze * tip * wind_strength * 0.21;
        VERTEX += inverse(mat3(MODEL_MATRIX)) * world_shift;
    }
}

void fragment() {
    // Same opaque 4x4 coverage fade as the procedural LeafWind shader.
    const float bayer[16] = float[16](0.0, 8.0, 2.0, 10.0, 12.0, 4.0, 14.0, 6.0, 3.0, 11.0, 1.0, 9.0, 15.0, 7.0, 13.0, 5.0);
    ivec2 pixel = ivec2(mod(floor(FRAGCOORD.xy), 4.0));
    if (camera_opacity < (bayer[pixel.y * 4 + pixel.x] + 0.5) / 16.0) discard;
    ALBEDO = foliage_tint.rgb;
    ROUGHNESS = 0.93;
    BACKLIGHT = foliage_tint.rgb * 0.14;
}
