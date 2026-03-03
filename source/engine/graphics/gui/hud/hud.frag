//Declaration of shader version and core profile functionality
//Fragment shader is calculating the color output for the pixels
#version 330 core
//==============================================================
//In-and outgoing variables
in float vTexIndex;
in vec2 vUv;

uniform sampler2D uTextures[4];

out vec4 FragColor;
//==============================================================
void main()
{
	if (vTexIndex < 0.0) discard;

	vec4 tex = texture(uTextures[int(vTexIndex)], vUv);

	if (distance(tex.rgb, vec3(255,0,220) /255) <0.1) discard;

	FragColor = tex;
}
//==============================================================
