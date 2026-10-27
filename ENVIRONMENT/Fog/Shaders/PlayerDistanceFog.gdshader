// Top-down atmospheric visibility: reconstruct opaque world depth and blend a player-centred fog ring.
shader_type spatial;
render_mode unshaded, fog_disabled, cull_disabled, depth_draw_never, depth_test_disabled, shadows_disabled, blend_mix;

uniform sampler2D depth_texture : hint_depth_texture, repeat_disable, filter_nearest;
uniform bool camera_distance = false;
uniform vec2 player_xz = vec2(0.0);
uniform float clear_radius = 24.0;
uniform float fog_radius = 48.0;
uniform float sky_opacity = 1.0;
uniform float opacity = 1.0;
uniform vec4 fog_color : source_color = vec4(0.39, 0.46, 0.48, 1.0);
uniform float edge_variation = 2.0;

void vertex() {
    POSITION = vec4(VERTEX.xy, 1.0, 1.0);
}

void fragment() {
    float depth = texture(depth_texture, SCREEN_UV).r;
    // Reveal the existing sky above a softly fogged horizon in FPS; overhead retains its opaque background.
    #if CURRENT_RENDERER == RENDERER_COMPATIBILITY
    vec4 far_view = INV_PROJECTION_MATRIX * vec4(SCREEN_UV * 2.0 - 1.0, 1.0, 1.0);
    #else
    vec4 far_view = INV_PROJECTION_MATRIX * vec4(SCREEN_UV * 2.0 - 1.0, 0.00001, 1.0);
    #endif
    vec3 ray = normalize((INV_VIEW_MATRIX * vec4(far_view.xyz, 0.0)).xyz);
    float amount = camera_distance ? mix(1.0, sky_opacity, smoothstep(0.0, 0.3, ray.y)) : 1.0;
    if (depth > 0.000001) {
        #if CURRENT_RENDERER == RENDERER_COMPATIBILITY
        vec3 ndc = vec3(SCREEN_UV, depth) * 2.0 - 1.0;
        #else
        vec3 ndc = vec3(SCREEN_UV * 2.0 - 1.0, depth);
        #endif
        vec4 world = INV_VIEW_MATRIX * INV_PROJECTION_MATRIX * vec4(ndc, 1.0);
        vec2 point = world.xz / world.w;
        // Static world-space broad variation softens the circular boundary without shader animation/polling.
        float variation = sin(point.x * 0.17 + sin(point.y * 0.19)) * sin(point.y * 0.13) * edge_variation;
        float distance_to_player = camera_distance ? length(world.xyz / world.w - CAMERA_POSITION_WORLD) : length(point - player_xz);
        amount = smoothstep(clear_radius + variation, fog_radius + variation, distance_to_player);
    }
    ALBEDO = fog_color.rgb;
    ALPHA = amount * opacity;
}
