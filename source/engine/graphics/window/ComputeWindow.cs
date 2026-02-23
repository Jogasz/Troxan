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
            0.3f,
            0.5f,
            0.8f
        });
    }
}
