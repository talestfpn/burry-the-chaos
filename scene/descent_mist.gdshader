shader_type canvas_item;
uniform sampler2D mist_noise : repeat_enable;
uniform vec4 mist_color : source_color = vec4(0.32, 0.48, 0.42, 0.14);
void fragment() {
    vec2 uv = floor(UV * vec2(160.0, 24.0)) / vec2(160.0, 24.0);
    float a = texture(mist_noise, uv * vec2(2.0, 0.7) + vec2(TIME * 0.006, 0.0)).r;
    float b = texture(mist_noise, uv + vec2(-TIME * 0.009, 0.15)).r;
    float edge = smoothstep(0.0, 0.3, UV.y) * smoothstep(1.0, 0.6, UV.y);
    COLOR = vec4(mist_color.rgb, (a + b) * mist_color.a * edge);
}
