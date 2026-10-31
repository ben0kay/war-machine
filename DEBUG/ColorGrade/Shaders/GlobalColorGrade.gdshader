// GD1: one optional full-screen colour grade after 3D, before CanvasLayer HUD/debug.
// Draws only when at least one grading setting differs from neutral.
shader_type canvas_item;
render_mode unshaded;

uniform sampler2D screen_tex : hint_screen_texture, repeat_disable, filter_linear;
uniform float hue_degrees : hint_range(-180.0, 180.0) = 0.0;
uniform float saturation : hint_range(0.0, 2.0) = 1.0;
uniform float contrast : hint_range(0.5, 1.5) = 1.0;
uniform float brightness : hint_range(0.5, 1.5) = 1.0;

void fragment() {
    vec3 rgb = texture(screen_tex, SCREEN_UV).rgb;
    // Luma/chroma hue rotation retains luminance better than RGB-channel swapping.
    float luma = dot(rgb, vec3(0.299, 0.587, 0.114));
    float i = dot(rgb, vec3(0.596, -0.275, -0.321));
    float q = dot(rgb, vec3(0.212, -0.523, 0.311));
    float angle = radians(hue_degrees);
    float new_i = i * cos(angle) - q * sin(angle);
    float new_q = i * sin(angle) + q * cos(angle);
    rgb = vec3(luma + 0.956 * new_i + 0.621 * new_q,
               luma - 0.272 * new_i - 0.647 * new_q,
               luma - 1.106 * new_i + 1.703 * new_q);
    float value = dot(rgb, vec3(0.299, 0.587, 0.114));
    rgb = mix(vec3(value), rgb, saturation);
    rgb = (rgb - vec3(0.5)) * contrast + vec3(0.5);
    rgb *= brightness;
    COLOR = vec4(clamp(rgb, vec3(0.0), vec3(1.0)), 1.0);
}
