using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Engine;

internal partial class ShaderHandler
{
    //Instance
    public static ShaderHandler? CeilingShader { get; set; }
    //VBO, VAO
    static int CeilingVAO { get; set; }
    static int CeilingVBO { get; set; }
    //Containers
    public static List<float> CeilingVertexAttribList { get; set; } = new List<float>();
    static float[]? CeilingVertices { get; set; }

    static void LoadCeilingShader(
        string vertexPath,
        string fragmentPath,
        Matrix4 projection,
        float minimumScreenSize)
    {
        CeilingShader = new ShaderHandler(vertexPath, fragmentPath);
        //VAO, VBO
        CeilingVAO = GL.GenVertexArray();
        CeilingVBO = GL.GenBuffer();
        //VAO, VBO Binding
        GL.BindVertexArray(CeilingVAO);
        GL.BindBuffer(BufferTarget.ArrayBuffer, CeilingVBO);
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
        CeilingShader.Use();
        CeilingShader.SetMatrix4("uProjMat", projection);
        CeilingShader.SetFloat("uMinimumScreenSize", minimumScreenSize);
        CeilingShader.SetFloat("uTileSize", Settings.Gameplay.TileSize);
        CeilingShader.SetFloat("uDistanceShade", Settings.Graphics.DistanceShade);
    }

    static void UpdateCeilingUniforms(float minimumScreenSize)
    {
        CeilingShader?.Use();
        CeilingShader?.SetMatrix4("uProjMat", projection);
        CeilingShader?.SetFloat("uMinimumScreenSize", minimumScreenSize);
    }

    static void LoadBufferAndClearCeiling()
    {
        //Making array
        CeilingVertices = CeilingVertexAttribList.ToArray();
        //Loading buffer
        GL.BindBuffer(BufferTarget.ArrayBuffer, CeilingVBO);
        GL.BufferData(
            BufferTarget.ArrayBuffer,
            CeilingVertices.Length * sizeof(float),
            CeilingVertices,
            BufferUsageHint.DynamicDraw);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        //CLEARING LIST
        CeilingVertexAttribList.Clear();
    }

    static void DrawCeiling(
        int tileCount,
        float wallWidth,
        Vector2 playerPosition,
        float playerAngle,
        float pitch)
    {
        CeilingShader?.Use();

        for (int i = 0; i < tileCount; i++)
        {
            CeilingShader?.SetInt($"uTextures[{i}]", 3 + i);
        }

        CeilingShader?.SetInt("uMapCeiling", 0);

        CeilingShader?.SetVector2("uMapSize", new Vector2(Level.MapCeiling.GetLength(1), Level.MapCeiling.GetLength(0)));
        CeilingShader?.SetFloat("uStepSize", wallWidth);
        CeilingShader?.SetVector2("uPlayerPos", new Vector2(playerPosition.X, playerPosition.Y));
        CeilingShader?.SetFloat("uPlayerAngle", playerAngle);
        CeilingShader?.SetFloat("uPitch", pitch);

        GL.BindVertexArray(CeilingVAO);
        int ceilLen = CeilingVertices?.Length ?? 0;
        int instanceCount = ceilLen / 5;
        if (instanceCount > 0)
        {
            GL.DrawArraysInstanced(PrimitiveType.TriangleStrip, 0, 4, instanceCount);
        }
    }
}
