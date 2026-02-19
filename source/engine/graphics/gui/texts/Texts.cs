using OpenTK.Mathematics;

namespace Engine;

internal partial class Engine
{
    static string CharSheet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()-=_+[]{}\\|;:'\".,<>/?`~ ";

    //Font atlas's size
    Vector2 fontAtlas = (2542, 37);

    float charHeight = 37f;
    float charWidth = 27f;

    // DrawText now accepts a fontSize parameter (monospaced cells are27x37 in atlas).
    // fontSize is a multiplier; final glyph size also scales with minimumScreenSize so text adapts to window size.
    void DrawText(string text, float x, float y, float fontSize, Vector3 color)
    {
        //Monospaced: each glyph is a fixed-width cell in a single row.
        float atlasW = fontAtlas.X;
        float atlasH = fontAtlas.Y;

        float uStep = charWidth / atlasW;
        float v0 = 0f;
        float v1 = 1f;

        // Compute pixel size for glyphs:
        // base glyph is charWidth x charHeight (27x37). Scale by fontSize and by minimumScreenSize so text adapts to window.
        // Use a baseline reference of1200 px for minimumScreenSize so fontSize==1 is fairly small on typical windows.
        float baseline = 1200f;
        float sizeScale = fontSize * (minimumScreenSize / baseline);
        float pixelCharWidth = charWidth * sizeScale;
        float pixelCharHeight = charHeight * sizeScale;

        // Start pen at provided X,Y as top-left of first character.
        float penX = x;
        float penY = y - pixelCharHeight; // convert top to bottom coordinate used by shader

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '\n')
            {
                penX = x;
                penY -= pixelCharHeight;
                continue;
            }

            // Treat other whitespace (space, tab, etc.) as advance only
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

            //Per instance: aPos(x1,x2,y1,y2) in screen pixels (projection handles it)
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
