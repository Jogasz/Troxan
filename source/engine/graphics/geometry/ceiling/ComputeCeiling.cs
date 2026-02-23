using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Shaders;

namespace Engine;

internal partial class RayCasting
{
    public static void LoadCeilingAttribs(
        float distanceShade,
        float minimumScreenSize,
        float screenHorizontalOffset,
        float screenVerticalOffset,
        int i,
        float rayAngle,
        float wallHeight,
        float wallWidth,
        float pitch,
        float debugBorder)
    {
        float stepX = wallWidth;
        float quadX1 = screenHorizontalOffset + (i * stepX);
        float quadX2 = screenHorizontalOffset + ((i + 1) * stepX);

        float quadY1 = screenVerticalOffset + minimumScreenSize;
            //Limit to stay inside minimumScreen
        float quadY2 = Math.Max(screenVerticalOffset + minimumScreenSize / 2 + wallHeight / 2 - pitch, screenVerticalOffset);

        //No ceiling can be rendered if the wall's top is on the top of the screen
        if (quadY1 > quadY2)
        {
            ShaderHandler.CeilingVertexAttribList.AddRange(new float[]
            {
                quadX1 + debugBorder,
                quadX2 - debugBorder,
                quadY1 + debugBorder,
                quadY2 - debugBorder,
                rayAngle
            });
        }
    }
}