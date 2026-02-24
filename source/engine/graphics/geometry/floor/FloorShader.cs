using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using Sources;

namespace Shaders;

internal partial class ShaderHandler
{
    //Instance
    public static ShaderHandler? FloorShader { get; set; }
    //VBO, VAO
    static int FloorVAO { get; set; }
    static int FloorVBO { get; set; }
    //Containers
    public static List<float> FloorVertexAttribList { get; set; } = new List<float>();
    static float[]? FloorVertices { get; set; }

    static void LoadFloorShader(
        string vertexPath,
        string fragmentPath,
        Matrix4 projection,
        Vector2 ClientSize,
        float minimumScreenSize
        )
    {
        FloorShader = new ShaderHandler(vertexPath, fragmentPath);
        //VAO, VBO
        FloorVAO = GL.GenVertexArray();
        FloorVBO = GL.GenBuffer();
        //VAO, VBO Binding
        GL.BindVertexArray(FloorVAO);
        GL.BindBuffer(BufferTarget.ArrayBuffer, FloorVBO);
        //Attribute0
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0,4, VertexAttribPointerType.Float, false,6 * sizeof(float),0);
        //Attribute1
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1,1, VertexAttribPointerType.Float, false,6 * sizeof(float),4 * sizeof(float));
        //Attribute2
        GL.EnableVertexAttribArray(2);
        GL.VertexAttribPointer(2,1, VertexAttribPointerType.Float, false,6 * sizeof(float),5 * sizeof(float));
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
        FloorShader.Use();
        FloorShader.SetMatrix4("uProjMat", projection);
        FloorShader.SetVector2("uClientSize", ClientSize);
        FloorShader.SetFloat("uMinimumScreenSize", minimumScreenSize);
        FloorShader.SetFloat("uTileSize", Settings.Gameplay.TileSize);
        FloorShader.SetFloat("uDistanceShade", Settings.Graphics.DistanceShade);
    }

    static void UpdateFloorUniforms(
        Vector2 ClientSize,
        float minimumScreenSize)
    {
        FloorShader?.Use();
        FloorShader?.SetMatrix4("uProjMat", projection);
        FloorShader?.SetVector2("uClientSize", ClientSize);
        FloorShader?.SetFloat("uMinimumScreenSize", minimumScreenSize);
    }

    static void LoadBufferAndClearFloor()
    {
        //Making array
        FloorVertices = FloorVertexAttribList.ToArray();
        //Loading buffer
        GL.BindBuffer(BufferTarget.ArrayBuffer, FloorVBO);
        GL.BufferData(
            BufferTarget.ArrayBuffer,
            FloorVertices.Length * sizeof(float),
            FloorVertices,
            BufferUsageHint.DynamicDraw);
        GL.BindBuffer(BufferTarget.ArrayBuffer,0);
        //CLEARING LIST
        FloorVertexAttribList.Clear();
    }

    static void DrawFloor(
        int tileCount,
        float wallWidth,
        Vector2 playerPosition,
        float playerAngle,
        float pitch)
    {
        if (Textures.MapFloorTex ==0 || Textures.MapSize.X ==0 || Textures.MapSize.Y ==0)
            return;

        FloorShader?.Use();

        //MapFloor
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, Textures.MapFloorTex);
        FloorShader?.SetInt("uMapFloor",0);

        //Binding wall textures from Texture1
        for (int i =0; i < Textures.Walls.Count; i++)
        {
            Textures.BindTex(Textures.Walls, i, TextureUnit.Texture1 + i);
            FloorShader?.SetInt($"uTextures[{i}]", i +1);
        }

        // IMPORTANT: use the GPU map size (matches the integer textures)
        FloorShader?.SetVector2("uMapSize", new Vector2(Textures.MapSize.X, Textures.MapSize.Y));
        FloorShader?.SetFloat("uStepSize", wallWidth);
        FloorShader?.SetVector2("uPlayerPos", new Vector2(playerPosition.X, playerPosition.Y));
        FloorShader?.SetFloat("uPlayerAngle", playerAngle);
        FloorShader?.SetFloat("uPitch", pitch);

        GL.BindVertexArray(FloorVAO);
        int floorLen = FloorVertices?.Length ??0;
        int instanceCount = floorLen /6;
        if (instanceCount >0)
        {
            GL.DrawArraysInstanced(PrimitiveType.TriangleStrip,0,4, instanceCount);
        }
    }
}