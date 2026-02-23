using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using Sources;

namespace Shaders;

internal partial class ShaderHandler
{
    //Instance
    public static ShaderHandler? SpriteShader { get; set; }
    //VBO, VAO
    static int SpriteVAO { get; set; }
    static int SpriteVBO { get; set; }
    //Containers
    public static List<float> SpriteVertexAttribList { get; set; } = new List<float>();
    static float[]? SpriteVertices { get; set; }

    static void LoadSpriteShader(
        string vertexPath,
        string fragmentPath,
        Matrix4 projection,
        float minimumScreenSize,
        Vector2 screenOffset)
    {
        SpriteShader = new ShaderHandler(vertexPath, fragmentPath);
        //VAO, VBO Creating
        SpriteVAO = GL.GenVertexArray();
        SpriteVBO = GL.GenBuffer();
        //VAO, VBO Binding
        GL.BindVertexArray(SpriteVAO);
        GL.BindBuffer(BufferTarget.ArrayBuffer, SpriteVBO);
        //Attribute 0 (Vertices)
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, 10 * sizeof(float), 0);
        //Attribute 1 (Texture UV)
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 10 * sizeof(float), 4 * sizeof(float));
        //Attribute2 (Spite type)
        GL.EnableVertexAttribArray(2);
        GL.VertexAttribPointer(2, 1, VertexAttribPointerType.Float, false, 10 * sizeof(float), 8 * sizeof(float));
        //Attribute3 (Sprite id)
        GL.EnableVertexAttribArray(3);
        GL.VertexAttribPointer(3, 1, VertexAttribPointerType.Float, false, 10 * sizeof(float), 9 * sizeof(float));
        //Divisor
        GL.VertexAttribDivisor(0, 1);
        GL.VertexAttribDivisor(1, 1);
        GL.VertexAttribDivisor(2, 1);
        GL.VertexAttribDivisor(3, 1);
        //Disable face culling to avoid accidentally removing one triangle
        GL.Disable(EnableCap.CullFace);
        //Unbind for safety
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);
        //Uniforms
        SpriteShader.Use();
        SpriteShader.SetMatrix4("uProjection", projection);
        SpriteShader.SetFloat("uMinimumScreenSize", minimumScreenSize);
        SpriteShader.SetVector2("uScreenOffset", screenOffset);
    }

    static void UpdateSpriteUniforms(
        float minimumScreenSize,
        Vector2 screenOffset)
    {
        //SpriteShader
        SpriteShader?.Use();
        SpriteShader?.SetMatrix4("uProjection", projection);
        SpriteShader?.SetFloat("uMinimumScreenSize", minimumScreenSize);
        SpriteShader?.SetVector2("uScreenOffset", screenOffset);
    }

    static void LoadBufferAndClearSprite()
    {
        //Making array
        SpriteVertices = SpriteVertexAttribList.ToArray();
        //Loading buffer
        GL.BindBuffer(BufferTarget.ArrayBuffer, SpriteVBO);
        GL.BufferData(
            BufferTarget.ArrayBuffer,
            SpriteVertices.Length * sizeof(float),
            SpriteVertices,
            BufferUsageHint.DynamicDraw);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        //CLEARING LIST
        SpriteVertexAttribList.Clear();
    }

    static void DrawSprite()
    {
        SpriteShader?.Use();

        //Objects atlas
        Textures.BindTex(Textures.Sprites, 0, TextureUnit.Texture0);
        //Items atlas
        Textures.BindTex(Textures.Sprites, 1, TextureUnit.Texture1);
        //Enemies atlas
        Textures.BindTex(Textures.Sprites, 2, TextureUnit.Texture2);

        SpriteShader?.SetInt("uSprites[0]", 0);
        SpriteShader?.SetInt("uSprites[1]", 1);
        SpriteShader?.SetInt("uSprites[2]", 2);

        //Sprites use explicit alpha in the fragment shader, so enable blending here.
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        //Binding and drawing
        GL.BindVertexArray(SpriteVAO);
        int spriteLen = SpriteVertices?.Length ?? 0;
        int instanceCount = spriteLen / 10;
        if (instanceCount > 0)
        {
            GL.DrawArraysInstanced(PrimitiveType.TriangleStrip, 0, 4, instanceCount);
        }

        //Restore state so other passes aren't affected.
        GL.Disable(EnableCap.Blend);
    }
}