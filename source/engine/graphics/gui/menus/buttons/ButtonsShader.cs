using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using Sources;

namespace Shaders;

internal partial class ShaderHandler
{
    //Instance
    public static ShaderHandler? ButtonsShader { get; set; }
    //VBO, VAO
    static int ButtonsVAO { get; set; }
    static int ButtonsVBO { get; set; }
    //Containers
    public static List<float> ButtonsVertexAttribList { get; set; } = new List<float>();
    static float[]? ButtonsVertices { get; set; }

    internal static void LoadButtonsShader(
        string vertexPath,
        string fragmentPath,
        Matrix4 projection)
    {
        ButtonsShader = new ShaderHandler(vertexPath, fragmentPath);
        //VAO, VBO Creating
        ButtonsVAO = GL.GenVertexArray();
        ButtonsVBO = GL.GenBuffer();
        //VAO, VBO Binding
        GL.BindVertexArray(ButtonsVAO);
        GL.BindBuffer(BufferTarget.ArrayBuffer, ButtonsVBO);

        //Attribute0 (aPos)
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0,4, VertexAttribPointerType.Float, false,9 * sizeof(float),0);

        //Attribute1 (aTexIndex)
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1,1, VertexAttribPointerType.Float, false,9 * sizeof(float),4 * sizeof(float));

        //Attribute2 (aUvRect)
        GL.EnableVertexAttribArray(2);
        GL.VertexAttribPointer(2,4, VertexAttribPointerType.Float, false,9 * sizeof(float),5 * sizeof(float));

        //Divisor
        GL.VertexAttribDivisor(0,1);
        GL.VertexAttribDivisor(1,1);
        GL.VertexAttribDivisor(2,1);

        GL.Disable(EnableCap.CullFace);
        //Unbind for safety
        GL.BindBuffer(BufferTarget.ArrayBuffer,0);
        GL.BindVertexArray(0);

        //Uniforms
        ButtonsShader.Use();
        ButtonsShader.SetMatrix4("uProjection", projection);
    }

    internal static void UpdateButtonsUniforms()
    {
        //ButtonsShader
        ButtonsShader?.Use();
        ButtonsShader?.SetMatrix4("uProjection", projection);
    }

    internal static void LoadBufferAndClearButtons()
    {
        //Making array
        ButtonsVertices = ButtonsVertexAttribList.ToArray();
        //Loading buffer
        GL.BindBuffer(BufferTarget.ArrayBuffer, ButtonsVBO);
        GL.BufferData(
            BufferTarget.ArrayBuffer,
            ButtonsVertices.Length * sizeof(float),
            ButtonsVertices,
            BufferUsageHint.DynamicDraw);
        GL.BindBuffer(BufferTarget.ArrayBuffer,0);
        //CLEARING LIST
        ButtonsVertexAttribList.Clear();
    }

    internal static void DrawButtons()
    {
        ButtonsShader?.Use();

        Textures.BindTex(Textures.Buttons, 0, TextureUnit.Texture0);
        ButtonsShader?.SetInt("uButtonsAtlas", 0);

        //Binding and drawing
        GL.BindVertexArray(ButtonsVAO);
        int menusLen = ButtonsVertices?.Length ??0;

        int instanceCount = menusLen /9;

        if (instanceCount >0)
        {
            GL.DrawArraysInstanced(PrimitiveType.TriangleStrip,0,4, instanceCount);
        }
    }
}