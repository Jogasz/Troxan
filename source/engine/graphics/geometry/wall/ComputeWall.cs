using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Shaders;

namespace Engine;

internal partial class RayCasting
{
    public static void LoadWallAttribs(
        Vector2i ClientSize,
        float distanceShade,
        float minimumScreenSize,
        float screenHorizontalOffset,
        float screenVerticalOffset,
        int tileSize,
        int nthRay,
        float rayLength,
        float rayTilePosition,
        float wallHeight,
        float wallWidth,
        int wallSide,
        int textureIndex,
        float pitch,
        float debugBorder
    )
    {
        float quadX1 = nthRay * wallWidth + screenHorizontalOffset;
        float quadX2 = (nthRay + 1) * wallWidth + screenHorizontalOffset;

            //Limit to stay inside minimumScreen
        float screenLimitTop = screenVerticalOffset + minimumScreenSize;
        float screenLimitBottom = screenVerticalOffset;

        float quadY1 = Math.Min((ClientSize.Y / 2f) + (wallHeight / 2f) - pitch, screenLimitTop);
        float quadY2 = Math.Max((ClientSize.Y / 2f) - (wallHeight / 2f) - pitch, screenLimitBottom);

            //If wall is outside of the minimumScreen, dont render
        if (quadY2 < screenLimitTop && quadY1 > screenLimitBottom)
        {
            ShaderHandler.WallVertexAttribList.AddRange(new float[]
            {
                quadX1 + debugBorder,
                quadX2 - debugBorder,
                quadY1 - debugBorder,
                quadY2 + debugBorder,
                wallHeight, //Quantize
                rayLength, //Shading
                rayTilePosition, //Horizontal texture stepping
                textureIndex, //Selecting correct texture
                wallSide //Flipping wrong sided textures
            });
        }
    }
}
