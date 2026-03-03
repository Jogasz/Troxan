//Declaration of shader version and core profile functionality
    //Vertex shader is translating and constructing verticies
#version 330 core
//==============================================================
//Incoming verticies and colors
    //Verticies X1 X2 Y1 Y2 per instance
layout (location = 0) in vec4 aPos;
    //Texture (U1, U2, V1, V2)
layout (location = 1) in vec4 aUvRect;
    //Sprite type
layout (location = 2) in float aSpriteType;
    //Sprite id
layout (location = 3) in float aSpriteId;
    //Sprite depth in camera-space
layout (location = 4) in float aSpriteDepth;

uniform mat4 uProjection;

out vec2 vUv;

out float vSpriteType;

out float vSpriteId;

out float vSpriteDepth;
//==============================================================

void main()
{
    // We draw 4 vertices per instance (DrawArraysInstanced with count = 4).
    // Use gl_VertexID % 4 to pick corner. The order chosen works with TriangleStrip:
    // 0 -> (x1, y1)  (top-right)
    // 1 -> (x2, y1)  (top-left)
    // 2 -> (x1, y2)  (bottom-right)
    // 3 -> (x2, y2)  (bottom-left)
    int corner = gl_VertexID % 4;
    vec2 pos;

        //Constructing 4 vertex points from the datas
    if (corner == 0)      pos = vec2(aPos.x, aPos.z); // x1,y1
    else if (corner == 1) pos = vec2(aPos.y, aPos.z); // x2,y1
    else if (corner == 2) pos = vec2(aPos.x, aPos.w); // x1,y2
    else                  pos = vec2(aPos.y, aPos.w); // x2,y2

        //UV's for textures
    float u0 = aUvRect.x;
    float v0 = aUvRect.y;
    float u1 = aUvRect.z;
    float v1 = aUvRect.w;

    if (corner == 0) vUv = vec2(u0, v1);
    else if (corner == 1) vUv = vec2(u1, v1);
    else if (corner == 2) vUv = vec2(u0, v0);
    else vUv = vec2(u1, v0);

    gl_Position = uProjection * vec4(pos.xy, 0.0, 1.0);

    vSpriteType = aSpriteType;
    vSpriteId = aSpriteId;
    vSpriteDepth = aSpriteDepth;
}