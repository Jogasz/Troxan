using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Shaders;

internal partial class ShaderHandler
{
    //Instance
    public static ShaderHandler? WindowShader { get; set; }
    //VBO, VAO
    static int WindowVAO { get; set; }
    static int WindowVBO { get; set; }
    //Containers
    public static List<float> WindowVertexAttribList { get; set; } = new List<float>();
    static float[]? WindowVertices { get; set; }

    static void LoadWindowShader(
        string vertexPath,
        string fragmentPath,
        Matrix4 projection)
    {
        WindowShader = new ShaderHandler(vertexPath, fragmentPath);
        //VAO, VBO Creating
        WindowVAO = GL.GenVertexArray();
        WindowVBO = GL.GenBuffer();
        //VAO, VBO Binding
        GL.BindVertexArray(WindowVAO);
        GL.BindBuffer(BufferTarget.ArrayBuffer, WindowVBO);
        //Attribute0
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, 7 * sizeof(float), 0);
        //Attribute1
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 7 * sizeof(float), 4 * sizeof(float));
        //Divisor
        GL.VertexAttribDivisor(0, 1);
        GL.VertexAttribDivisor(1, 1);
        //Disable face culling to avoid accidentally removing one triangle
        GL.Disable(EnableCap.CullFace);
        //Unbind for safety
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);
        //Uniforms
        WindowShader.Use();
        WindowShader.SetMatrix4("uProjection", projection);
    }

    static void UpdateWindowUniforms()
    {
        WindowShader?.Use();
        WindowShader?.SetMatrix4("uProjection", projection);
    }

    static void LoadBufferAndClearWindow()
    {
        //Making array
        WindowVertices = WindowVertexAttribList.ToArray();
        //Loading buffer
        GL.BindBuffer(BufferTarget.ArrayBuffer, WindowVBO);
        GL.BufferData(
            BufferTarget.ArrayBuffer,
            WindowVertices.Length * sizeof(float),
            WindowVertices,
            BufferUsageHint.DynamicDraw);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        //CLEARING LIST
        WindowVertexAttribList.Clear();
    }

    public static void DrawWindow()
    {
        WindowShader?.Use();
        //Binding and drawing
        GL.BindVertexArray(WindowVAO);
        int windowLen = WindowVertices?.Length ?? 0;
        int instanceCount = windowLen / 7;
        if (instanceCount > 0)
        {
            GL.DrawArraysInstanced(PrimitiveType.TriangleStrip, 0, 4, instanceCount);
        }
    }
}