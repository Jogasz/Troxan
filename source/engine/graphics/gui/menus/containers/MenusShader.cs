using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using Sources;

namespace Shaders;

internal partial class ShaderHandler
{
    //Instance
    public static ShaderHandler? MenusShader { get; set; }
    //VBO, VAO
    static int MenusVAO { get; set; }
    static int MenusVBO { get; set; }
    //Containers
    public static List<float> MenusVertexAttribList { get; set; } = new List<float>();
    static float[]? MenusVertices { get; set; }

    static void LoadMenusShader(
        string vertexPath,
        string fragmentPath,
        Matrix4 projection)
    {
        MenusShader = new ShaderHandler(vertexPath, fragmentPath);
        //VAO, VBO Creating
        MenusVAO = GL.GenVertexArray();
        MenusVBO = GL.GenBuffer();
        //VAO, VBO Binding
        GL.BindVertexArray(MenusVAO);
        GL.BindBuffer(BufferTarget.ArrayBuffer, MenusVBO);
        //Attribute0
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
        //Attribute1
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 1, VertexAttribPointerType.Float, false, 5 * sizeof(float), 4 * sizeof(float));
        //Divisor
        GL.VertexAttribDivisor(0, 1);
        GL.VertexAttribDivisor(1, 1);
        //Disable face culling to avoid accidentally removing one triangle
        GL.Disable(EnableCap.CullFace);
        //Unbind for safety
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);
        //Uniforms
        MenusShader.Use();
        MenusShader.SetMatrix4("uProjection", projection);
    }

    internal static void UpdateMenusUniforms()
    {
        //MenusShader
        MenusShader?.Use();
        MenusShader?.SetMatrix4("uProjection", projection);
    }

    internal static void LoadBufferAndClearMenus()
    {
        //Making array
        MenusVertices = MenusVertexAttribList.ToArray();
        //Loading buffer
        GL.BindBuffer(BufferTarget.ArrayBuffer, MenusVBO);
        GL.BufferData(
            BufferTarget.ArrayBuffer,
            MenusVertices.Length * sizeof(float),
            MenusVertices,
            BufferUsageHint.DynamicDraw);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        //CLEARING LIST
        MenusVertexAttribList.Clear();
    }

    internal static void DrawMenus()
    {
        for (int i = 0; i < Textures.Containers.Count; i++)
        {
            Textures.BindTex(Textures.Containers, i, TextureUnit.Texture0 + i);
        }

        MenusShader?.Use();

        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        //Uniforms
        for (int i = 0; i < Textures.Containers.Count; i++)
        {
            MenusShader?.SetInt($"uTextures[{i}]", i);
        }

        //Binding and drawing
        GL.BindVertexArray(MenusVAO);
        int menusLen = MenusVertices?.Length ?? 0;
        int instanceCount = menusLen / 5;
        if (instanceCount > 0)
        {
            GL.DrawArraysInstanced(PrimitiveType.TriangleStrip, 0, 4, instanceCount);
        }

        //Restore state so other passes aren't affected.
        GL.Disable(EnableCap.Blend);

        DrawButtons();
    }
}