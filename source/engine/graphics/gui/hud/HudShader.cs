using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using Sources;

namespace Shaders;

internal partial class ShaderHandler
{
    //Instance
    public static ShaderHandler? HudShader { get; set; }
    //VBO, VAO
    static int HudVAO { get; set; }
    static int HudVBO { get; set; }
    //Containers
    public static List<float> HudVertexAttribList { get; set; } = new List<float>();
    static float[]? HudVertices { get; set; }

    internal static void LoadHudShader(
        string vertexPath,
        string fragmentPath,
        Matrix4 projection)
    {
        HudShader = new ShaderHandler(vertexPath, fragmentPath);
        //VAO, VBO Creating
        HudVAO = GL.GenVertexArray();
        HudVBO = GL.GenBuffer();
        //VAO, VBO Binding
        GL.BindVertexArray(HudVAO);
        GL.BindBuffer(BufferTarget.ArrayBuffer, HudVBO);

        //Attribute0 (aPos)
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, 9 * sizeof(float), 0);

        //Attribute1 (aTexIndex)
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 1, VertexAttribPointerType.Float, false, 9 * sizeof(float), 4 * sizeof(float));

        //Attribute2 (aUvRect)
        GL.EnableVertexAttribArray(2);
        GL.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, 9 * sizeof(float), 5 * sizeof(float));

        //Divisor
        GL.VertexAttribDivisor(0, 1);
        GL.VertexAttribDivisor(1, 1);
        GL.VertexAttribDivisor(2, 1);

        //Disable face culling to avoid accidentally removing one triangle
        GL.Disable(EnableCap.CullFace);

        //Unbind for safety
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);

        //Uniforms
        HudShader.Use();
        HudShader.SetMatrix4("uProjection", projection);
    }

    internal static void UpdateHudUniforms()
    {
        HudShader?.Use();
        HudShader?.SetMatrix4("uProjection", projection);
    }

    internal static void LoadBufferAndClearHud()
    {
        //Making array
        HudVertices = HudVertexAttribList.ToArray();
        //Loading buffer
        GL.BindBuffer(BufferTarget.ArrayBuffer, HudVBO);
        GL.BufferData(
            BufferTarget.ArrayBuffer,
            HudVertices.Length * sizeof(float),
            HudVertices,
            BufferUsageHint.DynamicDraw);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        //CLEARING LIST
        HudVertexAttribList.Clear();
    }

    internal static void DrawHud()
    {
        HudShader?.Use();

        for (int i = 0; i < Textures.HUD.Count; i++)
        {
            Textures.BindTex(Textures.HUD, i, TextureUnit.Texture0 + i);
            HudShader?.SetInt($"uTextures[{i}]", i);
        }

        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        GL.BindVertexArray(HudVAO);
        int hudLen = HudVertices?.Length ?? 0;
        int instanceCount = hudLen / 9;

        if (instanceCount > 0)
        {
            GL.DrawArraysInstanced(PrimitiveType.TriangleStrip, 0, 4, instanceCount);
        }

        GL.Disable(EnableCap.Blend);
    }
}
