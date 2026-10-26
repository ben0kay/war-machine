shader_type spatial;
render_mode unshaded, blend_add, cull_disabled, depth_draw_never;

uniform vec4 beam_color : source_color = vec4(0.05, 0.8, 0.9, 1.0);
uniform float opacity : hint_range(0.0, 1.0) = 0.3;
uniform float brightness = 2.0;
uniform float line_count = 22.0;
uniform float line_speed = 0.8;
uniform float band_contrast = 1.5;

void fragment() {
    float length_fade = smoothstep(0.0, 0.08, UV.y) * (1.0 - smoothstep(0.6, 1.0, UV.y));
    float edge = mix(0.35, 1.0, pow(abs(dot(normalize(NORMAL), normalize(VIEW))), 0.5));
    float lines = pow(0.5 + 0.5 * sin((UV.y * line_count - TIME * line_speed) * 6.283185), 10.0);
    float band_distance = abs(UV.y - fract(TIME * line_speed * 0.35));
    float band = 1.0 - smoothstep(0.0, 0.04, band_distance);
    ALBEDO = beam_color.rgb * brightness;
    ALPHA = opacity * length_fade * edge * (0.55 + band_contrast * (0.55 * lines + 0.7 * band));
}
