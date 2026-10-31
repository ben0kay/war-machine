// DF2 bark: retain authored flat-shaded per-face vertex colours; avoid a white fallback material.
// A shared material receives the DF1 master bark palette once during forest initialization.
shader_type spatial;
render_mode cull_back, depth_draw_opaque;

uniform vec4 bark_fallback_color : source_color = vec4(0.53, 0.51, 0.43, 1.0);

void fragment() {
    vec3 surface_color = COLOR.rgb;
    // White is not a valid DF1 bark face colour. White indicates a missing colour attribute
    // or missing instance-colour stream, so use the master bark colour rather than grey.
    float no_vertex_colour = step(0.97, min(surface_color.r, min(surface_color.g, surface_color.b)));
    ALBEDO = mix(surface_color, bark_fallback_color.rgb, no_vertex_colour);
    ROUGHNESS = 1.0;
    METALLIC = 0.0;
}
