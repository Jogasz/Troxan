using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Engine;

internal partial class ShaderHandler
{
    //Instance
    public static ShaderHandler? TextsShader { get; set; }
    //VBO, VAO
    static int TextsVAO { get; set; }
    static int TextsVBO { get; set; }
    //Containers
    public static List<float> TextsVertexAttribList { get; set; } = new List<float>();
    static float[]? TextsVertices { get; set; }

    static void LoadTextsShader(
    string vertexPath,
    string fragmentPath,
    Matrix4 projection)
    {
        TextsShader = new ShaderHandler(vertexPath, fragmentPath);

        //VAO, VBO Creating
        TextsVAO = GL.GenVertexArray();
        TextsVBO = GL.GenBuffer();

        //VAO, VBO Binding
        GL.BindVertexArray(TextsVAO);
        GL.BindBuffer(BufferTarget.ArrayBuffer, TextsVBO);

        //Attribute0 (aPos)
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0,4, VertexAttribPointerType.Float, false,11 * sizeof(float),0);

        //Attribute1 (aUvRect)
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1,4, VertexAttribPointerType.Float, false,11 * sizeof(float),4 * sizeof(float));

        //Attribute2 (aColor)
        GL.EnableVertexAttribArray(2);
        GL.VertexAttribPointer(2,3, VertexAttribPointerType.Float, false,11 * sizeof(float),8 * sizeof(float));

        //Divisor
        GL.VertexAttribDivisor(0,1);
        GL.VertexAttribDivisor(1,1);
        GL.VertexAttribDivisor(2,1);

        //Disable face culling to avoid accidentally removing one triangle
        GL.Disable(EnableCap.CullFace);

        //Unbind for safety
        GL.BindBuffer(BufferTarget.ArrayBuffer,0);
        GL.BindVertexArray(0);

        //Uniforms
        TextsShader.Use();
        TextsShader.SetMatrix4("uProjection", projection);
        TextsShader.SetInt("uFontAtlas",0);
    }

    static void UpdateTextsUniforms()
    {
        TextsShader?.Use();
        TextsShader?.SetMatrix4("uProjection", projection);
        TextsShader?.SetInt("uFontAtlas",0);
    }

    internal static void LoadBufferAndClearTexts()
    {
        //Making array
        TextsVertices = TextsVertexAttribList.ToArray();

        //Loading buffer
        GL.BindBuffer(BufferTarget.ArrayBuffer, TextsVBO);
        GL.BufferData(
        BufferTarget.ArrayBuffer,
        TextsVertices.Length * sizeof(float),
        TextsVertices,
        BufferUsageHint.DynamicDraw);
        GL.BindBuffer(BufferTarget.ArrayBuffer,0);

        //CLEARING LIST
        TextsVertexAttribList.Clear();
    }

    internal static void DrawTexts()
    {
        int textsLen = TextsVertices?.Length ??0;
        int instanceCount = textsLen /11;
        if (instanceCount <=0) return;

        //Bind font atlas to unit0 for this pass
        int fontIndex = Texture.textures.Count -1;
        Texture.Bind(fontIndex, TextureUnit.Texture0);

        TextsShader?.Use();

        //Text uses alpha, so enable blending here.
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        GL.BindVertexArray(TextsVAO);
        GL.DrawArraysInstanced(PrimitiveType.TriangleStrip,0,4, instanceCount);

        //Restore state
        GL.Disable(EnableCap.Blend);
    }
}
