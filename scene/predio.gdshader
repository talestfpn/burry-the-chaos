// CRT with Luminance Preservation (no scanlines) by Harrison Allen
// Public domain (Modificado para Godot 4 Screen Reading)

shader_type canvas_item;

render_mode unshaded;

// A MÁGICA TÁ AQUI: hint_screen_texture faz ele ler a tela do jogo!
uniform sampler2D tex : hint_screen_texture, filter_linear;

uniform int mask_type : hint_enum(
	"Dots:1",
	"Aperture Grille:2",
	"Wide Grille:3",
	"Wide Soft Grille:4",
	"Slot Mask:5",
	"Null:0") = 1;

uniform float curve : hint_range(0.0, 0.5) = 0.0;
uniform float color_offset : hint_range(-0.5, 0.5) = 0.0;
uniform float mask_brightness : hint_range(0, 1) = 1.0;
uniform float aspect : hint_range(0.5, 1.0) = 0.75;

vec2 warp(vec2 uv, float _aspect, float _curve) {
	uv -= 0.5;
	uv.x /= _aspect;
	float warping = dot(uv, uv) * _curve;
	warping -= _curve * 0.25;
	uv /= 1.0 - warping;
	uv.x *= _aspect;
	uv += 0.5;
	return uv;
}

vec3 linear_to_srgb(vec3 col) {
	return mix(
		(pow(col, vec3(1.0 / 2.4)) * 1.055) - 0.055,
		col * 12.92,
		lessThan(col, vec3(0.0031318))
	);
}

vec3 srgb_to_linear(vec3 col) {
	return mix(
		pow((col + 0.055) / 1.055, vec3(2.4)),
		col / 12.92,
		lessThan(col, vec3(0.04045))
	);
}

vec3 sample(vec2 uv) {
	uv *= vec2(textureSize(tex, 0));
	int y = int(uv.y + 0.5) - 1;
	float x = floor(uv.x);
	float ax = x - 1.0;
	float dx = x + 1.0;

	vec3 upper_a = texelFetch(tex, ivec2(int(ax), y), 0).rgb;
	vec3 upper_b = texelFetch(tex, ivec2(int(x), y), 0).rgb;
	vec3 upper_c = texelFetch(tex, ivec2(int(dx), y), 0).rgb;

	y += 1;

	vec3 lower_a = texelFetch(tex, ivec2(int(ax), y), 0).rgb;
	vec3 lower_b = texelFetch(tex, ivec2(int(x), y), 0).rgb;
	vec3 lower_c = texelFetch(tex, ivec2(int(dx), y), 0).rgb;

	upper_a = srgb_to_linear(upper_a);
	upper_b = srgb_to_linear(upper_b);
	upper_c = srgb_to_linear(upper_c);
	lower_a = srgb_to_linear(lower_a);
	lower_b = srgb_to_linear(lower_b);
	lower_c = srgb_to_linear(lower_c);

	vec3 beam = vec3(uv.x - 0.5);
	beam.r -= color_offset;
	beam.b += color_offset;

	vec3 weight_a = smoothstep(1.0, 0.0, (beam - ax));
	vec3 weight_b = smoothstep(1.0, 0.0, abs(beam - x));
	vec3 weight_c = smoothstep(1.0, 0.0, (dx - beam));

	vec3 upper_col = (upper_a * weight_a + upper_b * weight_b + upper_c * weight_c);
	vec3 lower_col = (lower_a * weight_a + lower_b * weight_b + lower_c * weight_c);

	float sawtooth = (uv.y + 0.5) - float(y);
	sawtooth = smoothstep(0.0, 1.0, sawtooth);

	return mix(upper_col, lower_col, sawtooth);
}

vec4 generate_mask(vec2 fragcoord) {
	switch (mask_type) {
		case 1:
			const vec3 pattern1[] = {vec3(1,0,0), vec3(0,1,0), vec3(0,0,1), vec3(0,0,0)};
			ivec2 icoords1 = ivec2(fragcoord);
			return vec4(pattern1[(icoords1.y * 2 + icoords1.x) % 4], 0.25);
		case 2:
			const vec3 pattern2[] = {vec3(0,1,0), vec3(1,0,1)};
			return vec4(pattern2[int(fragcoord.x) % 2], 0.5);
		case 3:
			const vec3 pattern3[] = {vec3(1,0,0), vec3(0,1,0), vec3(0,0,1), vec3(0,0,0)};
			return vec4(pattern3[int(fragcoord.x) % 4], 0.25);
		case 4:
			const vec3 pattern4[] = {vec3(1.0,0.125,0.0), vec3(0.125,1.0,0.125), vec3(0.0,0.125,1.0), vec3(0.125,0.0,0.125)};
			return vec4(pattern4[int(fragcoord.x) % 4], 0.3125);
		case 5:
			const vec3 pattern5[] = {
				vec3(1,0,1), vec3(0,1,0), vec3(1,0,1), vec3(0,1,0),
				vec3(0,0,1), vec3(0,1,0), vec3(1,0,0), vec3(0,0,0),
				vec3(1,0,1), vec3(0,1,0), vec3(1,0,1), vec3(0,1,0),
				vec3(1,0,0), vec3(0,0,0), vec3(0,0,1), vec3(0,1,0)
			};
			ivec2 icoords5 = ivec2(fragcoord) % 4;
			return vec4(pattern5[icoords5.y * 4 + icoords5.x], 0.375);
		default:
			return vec4(0.5);
	}
}

vec3 apply_mask(vec3 linear_color, vec2 fragcoord) {
	vec4 mask = generate_mask(fragcoord);
	linear_color *= mix(mask.w, 1.0, mask_brightness);
	vec3 target_color = linear_color / mask.w;
	vec3 primary_col = clamp(target_color, 0.0, 1.0);
	vec3 secondary = target_color - primary_col;
	secondary /= 1.0 / mask.w - 1.0;
	primary_col *= mask.rgb;
	primary_col += secondary * (1.0 - mask.rgb);
	return primary_col;
}

void fragment() {
	vec2 warped_coords = warp(UV, aspect, curve);
	vec3 col = sample(warped_coords);
	col = apply_mask(col, FRAGCOORD.xy);
	col = linear_to_srgb(col);
	COLOR.rgb = col;
}