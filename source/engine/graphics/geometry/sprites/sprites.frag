//Declaration of shader version and core profile functionality
 //Fragment shader is calculating the color output for the pixels
#version 330 core
//==============================================================
//In-and outgoing variables
in vec2 vUv;

in float vSpriteType;

in float vSpriteId;

in float vSpriteDepth;

in float vDamageOverlayAlpha;

 //Vec4 that defines the final color output that we should calculate ourselves
out vec4 FragColor;

 // AI CHANGE: use a sprite texture array so each sprite ID can sample its own texture
uniform sampler2D uSprites[3];

uniform sampler1D uWallDepthTex;

uniform int uRayCount;

uniform float uDistanceShade;

uniform float uMinimumScreenSize;

uniform vec2 uScreenOffset;
//==============================================================
//main() method entry point
void main()
{
	vec4 tex = texture(uSprites[int(vSpriteType)], vUv);

	//Minimum screen limit to ensure that pixels arent drawn outside the game screen
	//x: left limit
	//y: right limit
	//z: bottom limit
	//w: top limit
	vec4 minimumScreenLimit = vec4(
		uScreenOffset.x,
		uScreenOffset.x + uMinimumScreenSize,
		uScreenOffset.y,
		uScreenOffset.y + uMinimumScreenSize);

	if (tex.a <= 0.0 ||
		distance(tex.rgb, vec3(255,0,220) /255) <0.1 ||
		gl_FragCoord.x < minimumScreenLimit.x ||
		gl_FragCoord.x > minimumScreenLimit.y ||
		gl_FragCoord.y < minimumScreenLimit.z ||
		gl_FragCoord.y > minimumScreenLimit.w) discard;

	if (uRayCount > 0)
	{
		float xInView = gl_FragCoord.x - uScreenOffset.x;
		float wallWidth = uMinimumScreenSize / float(uRayCount);
		int rayIndex = int(floor(xInView / wallWidth));
		rayIndex = clamp(rayIndex, 0, uRayCount - 1);

		float wallDepth = texelFetch(uWallDepthTex, rayIndex, 0).r;

		if (vSpriteDepth >= wallDepth)
			discard;
	}

	if (vDamageOverlayAlpha > 0.0)
		tex.rgb = mix(tex.rgb, vec3(1.0, 0.0, 0.0), clamp(vDamageOverlayAlpha, 0.0, 1.0));

	float distanceShade = uDistanceShade / 255;
	float shade = vSpriteDepth * distanceShade;

	FragColor = tex - shade;
}
//==============================================================