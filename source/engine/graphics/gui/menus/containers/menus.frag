//Declaration of shader version and core profile functionality
//Fragment shader is calculating the color output for the pixels
#version 330 core
//==============================================================
//In-and outgoing variables
//The input variable from Vertex Shader (same name and type)
in float texIndex;
in vec2 vUv;

 //Textures array (uIn)
uniform sampler2D uTextures[6];

 //Vec4 that defines the final color output that we should calculate ourselves
out vec4 FragColor;
//==============================================================
void main()
{
 FragColor = texture(uTextures[int(texIndex)], vUv);
}
//==============================================================