using Shaders;

namespace Engine;

internal partial class Engine
{
    void LoadWindowAttribs()
    {
        ShaderHandler.WindowVertexAttribList.AddRange(new float[]
        {
            screenHorizontalOffset,
            screenHorizontalOffset + minimumScreenSize,
            screenVerticalOffset + minimumScreenSize,
            screenVerticalOffset,
            0.0f,
            0.0f,
            0.0f
        });
    }
}
