#version 330 core
//==============================================================
//In-and outgoing variables
in vec2 vUv;
in vec3 vColor;

out vec4 FragColor;

//Font atlas texture
uniform sampler2D uFontAtlas;
//==============================================================
void main()
{
 vec4 tex = texture(uFontAtlas, vUv);

 //If fully transparent, discard
 if (tex.a <=0.0) discard;

 //Tint the font (atlas is white glyphs)
 FragColor = vec4(vColor,1.0) * tex;
}
