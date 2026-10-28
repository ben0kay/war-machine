// Faint light-in-air approximation: one low-poly unlit cone, not volumetric fog or a real light.
shader_type spatial;
render_mode unshaded, blend_mix, depth_draw_never, cull_disabled, shadows_disabled;

uniform vec4 beam_color : source_color = vec4(1.0, 0.82, 0.61, 1.0);
uniform float beam_opacity : hint_range(0.0, 0.12, 0.005) = 0.035;

void fragment() {
    // CylinderMesh UV.y: 0 at the source, 1 at the broad lower end.
    float from_bulb = UV.y;
    float start_fade = smoothstep(0.0, 0.16, from_bulb);
    float end_fade = 1.0 - smoothstep(0.72, 1.0, from_bulb);
    // Fade grazing angles to avoid a sharply cut-out bright polygon outline.
    float facing = abs(dot(normalize(NORMAL), normalize(VIEW)));
    float soft_edge = mix(0.08, 1.0, smoothstep(0.03, 0.78, facing));
    ALBEDO = beam_color.rgb;
    // Halve each surface's opacity for double-sided viewing, including FPS inside the beam.
    ALPHA = beam_color.a * beam_opacity * 0.55 * start_fade * end_fade * soft_edge;
}
