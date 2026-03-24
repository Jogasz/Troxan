//Declaration of shader version and core profile functionality
//Fragment shader is calculating the color output for the pixels
#version 330 core
//==============================================================
//In-and outgoing variables
in float vTexIndex;
in vec2 vUv;

uniform sampler2D uTextures[5];
uniform float uDamageOverlayAlpha;
uniform float uPickupOverlayAlpha;

out vec4 FragColor;
//==============================================================
void main()
{
   if (vTexIndex == -1.0) discard;

	if (vTexIndex == -2.0)
	{
		FragColor = vec4(1.0, 0.0, 0.0, uDamageOverlayAlpha);
		return;
	}

	if (vTexIndex == -3.0)
	{
		FragColor = vec4(1.0, 1.0, 1.0, uPickupOverlayAlpha);
		return;
	}

	vec4 tex = texture(uTextures[int(vTexIndex)], vUv);

	if (distance(tex.rgb, vec3(255,0,220) /255) <0.1) discard;

	FragColor = tex;
}
//==============================================================
