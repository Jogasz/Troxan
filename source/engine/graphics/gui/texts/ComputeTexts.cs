using OpenTK.Mathematics;

using Shaders;

namespace Engine;

internal partial class Engine
{
    static string CharSheet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()-=_+[]{}\\|;:'\".,<>/?`~ ";

    //Font atlas's size
    Vector2 fontAtlas = (2542, 37);

    float charHeight = 37f;
    float charWidth = 27f;

    //Monospace atlas text renderer
    void LoadTextAttribs(string text, float x, float y, float fontSize, Vector3 color)
    {
        //Single row atlas
        float atlasW = fontAtlas.X;
        float atlasH = fontAtlas.Y;

        float uStep = charWidth / atlasW;
        float v0 = 0f;
        float v1 = 1f;

        //Glyph size in pixels
        float baseline = 1200f;
        float sizeScale = fontSize * (minimumScreenSize / baseline);
        float pixelCharWidth = charWidth * sizeScale;
        float pixelCharHeight = charHeight * sizeScale;

        //Top-left origin
        float penX = x;
        float penY = y - pixelCharHeight;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '\n')
            {
                penX = x;
                penY -= pixelCharHeight;
                continue;
            }

            //Whitespace advance
            if (char.IsWhiteSpace(c))
            {
                penX += pixelCharWidth;
                continue;
            }

            int idx = CharSheet.IndexOf(c);
            if (idx < 0)
            {
                penX += pixelCharWidth;
                continue;
            }

            float u0 = uStep * idx;
            float u1 = uStep * (idx + 1);

            //Per-instance rect + uv + color
            float x1 = penX;
            float x2 = penX + pixelCharWidth;
            float y1 = penY + pixelCharHeight;
            float y2 = penY;

            ShaderHandler.TextsVertexAttribList.AddRange(new float[]
            {
                x1, x2, y1, y2,
                u0, v0, u1, v1,
                color.X, color.Y, color.Z
            });

            penX += pixelCharWidth;
        }
    }
}
