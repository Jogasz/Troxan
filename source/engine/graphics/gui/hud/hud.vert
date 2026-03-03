//Declaration of shader version and core profile functionality
//Vertex shader is translating and constructing verticies
#version 330 core
//==============================================================
//Incoming verticies
layout (location = 0) in vec4 aPos;
layout (location = 1) in float aTexIndex;
layout (location = 2) in vec4 aUvRect;
//==============================================================
//In-and outgoing variables
out float vTexIndex;
out vec2 vUv;

uniform mat4 uProjection;

void main()
{
	int corner = gl_VertexID % 4;
	vec2 pos;

	if (corner == 0)      pos = vec2(aPos.x, aPos.z);
	else if (corner == 1) pos = vec2(aPos.y, aPos.z);
	else if (corner == 2) pos = vec2(aPos.x, aPos.w);
	else                  pos = vec2(aPos.y, aPos.w);

	float u0 = aUvRect.x;
	float v0 = aUvRect.y;
	float u1 = aUvRect.z;
	float v1 = aUvRect.w;

	if (corner == 0)      vUv = vec2(u0, v1);
	else if (corner == 1) vUv = vec2(u1, v1);
	else if (corner == 2) vUv = vec2(u0, v0);
	else                  vUv = vec2(u1, v0);

	gl_Position = uProjection * vec4(pos.xy, 0.0, 1.0);
	vTexIndex = aTexIndex;
}
