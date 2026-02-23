using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using Sources;

namespace Shaders;

internal partial class ShaderHandler
{
    //Instance
    public static ShaderHandler? WallShader { get; set; }
    //VBO, VAO
    static int WallVAO { get; set; }
    static int WallVBO { get; set; }
    //Containers
    public static List<float> WallVertexAttribList { get; set; } = new List<float>();
    static float[]? WallVertices { get; set; }

    static void LoadWallShader(
        string vertexPath,
        string fragmentPath,
        Matrix4 projection,
        float minimumScreenSize,
        Vector2 screenOffset)
    {
        WallShader = new ShaderHandler(vertexPath, fragmentPath);
        //VAO, VBO
        WallVAO = GL.GenVertexArray();
        WallVBO = GL.GenBuffer();
        //VAO, VBO Binding
        GL.BindVertexArray(WallVAO);
        GL.BindBuffer(BufferTarget.ArrayBuffer, WallVBO);
        //Attribute0 - Vertex
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, 9 * sizeof(float), 0);
        //Attribute1 - wallheight
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 1, VertexAttribPointerType.Float, false, 9 * sizeof(float), 4 * sizeof(float));
        //Attribute2 - rayLength
        GL.EnableVertexAttribArray(2);
        GL.VertexAttribPointer(2, 1, VertexAttribPointerType.Float, false, 9 * sizeof(float), 5 * sizeof(float));
        //Attribute3 - rayTilePosition
        GL.EnableVertexAttribArray(3);
        GL.VertexAttribPointer(3, 1, VertexAttribPointerType.Float, false, 9 * sizeof(float), 6 * sizeof(float));
        //Attribute4 - textureIndex
        GL.EnableVertexAttribArray(4);
        GL.VertexAttribPointer(4, 1, VertexAttribPointerType.Float, false, 9 * sizeof(float), 7 * sizeof(float));
        //Attribute5 - wallSide
        GL.EnableVertexAttribArray(5);
        GL.VertexAttribPointer(5, 1, VertexAttribPointerType.Float, false, 9 * sizeof(float), 8 * sizeof(float));
        //Divisor
        GL.VertexAttribDivisor(0, 1);
        GL.VertexAttribDivisor(1, 1);
        GL.VertexAttribDivisor(2, 1);
        GL.VertexAttribDivisor(3, 1);
        GL.VertexAttribDivisor(4, 1);
        GL.VertexAttribDivisor(5, 1);
        //Disable face culling to avoid accidentally removing one triangle
        GL.Disable(EnableCap.CullFace);
        //Unbind for safety
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);
        //Uniforms
        WallShader.Use();
        WallShader.SetMatrix4("uProjMat", projection);
        WallShader.SetFloat("uTileSize", Settings.Gameplay.TileSize);
        WallShader.SetFloat("uMinimumScreenSize", minimumScreenSize);
        WallShader.SetFloat("uDistanceShade", Settings.Graphics.DistanceShade);
        WallShader.SetVector2("uScreenOffset", screenOffset);
    }

    static void UpdateWallUniforms(
        float minimumScreenSize,
        Vector2 screenOffset)
    {
        WallShader?.Use();
        WallShader?.SetMatrix4("uProjMat", projection);
        WallShader?.SetFloat("uMinimumScreenSize", minimumScreenSize);
        WallShader?.SetVector2("uScreenOffset", screenOffset);
    }

    static void LoadBufferAndClearWall()
    {
        //Making array
        WallVertices = WallVertexAttribList.ToArray();
        //Loading buffer
        GL.BindBuffer(BufferTarget.ArrayBuffer, WallVBO);
        GL.BufferData(
            BufferTarget.ArrayBuffer,
            WallVertices.Length * sizeof(float),
            WallVertices,
            BufferUsageHint.DynamicDraw);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        //CLEARING LIST
        WallVertexAttribList.Clear();
    }

    static void DrawWalls(
        int tileCount,
        float pitch)
    {
        //Binding wall textures
        for (int i = 0; i < Textures.Walls.Count; i++)
        {
            Textures.BindTex(Textures.Walls, i, TextureUnit.Texture0 + i);
        }

        WallShader?.Use();

        for (int i = 0; i < tileCount; i++)
        {
            WallShader?.SetInt($"uTextures[{i}]", i);
        }

        WallShader?.SetFloat("uPitch", pitch);

        GL.BindVertexArray(WallVAO);
        int wallLen = WallVertices?.Length ?? 0;
        int instanceCount = wallLen / 9;
        if (instanceCount > 0)
        {
            GL.DrawArraysInstanced(PrimitiveType.TriangleStrip, 0, 4, instanceCount);
        }
    }
}
