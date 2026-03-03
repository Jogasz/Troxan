using OpenTK.Windowing.GraphicsLibraryFramework;

using Shaders;

namespace Engine;

internal partial class Engine
{
    //Running effect animation (8 frames on one row)
    static float _runningHudAnimTime;

    void LoadHudAttribs()
    {
        float x1 = screenHorizontalOffset;
        float x2 = screenHorizontalOffset + minimumScreenSize;
        float y1 = screenVerticalOffset + minimumScreenSize;
        float y2 = screenVerticalOffset;

        bool isRunning = KeyboardState.IsKeyDown(Keys.W) && KeyboardState.IsKeyDown(Keys.LeftShift);

        //Layer 0 - Running effect (only while running)
        if (isRunning)
        {
            _runningHudAnimTime += deltaTime;

            int frameCount = 8;
            float fps = 12f;
            int frame = (int)(_runningHudAnimTime * fps) % frameCount;

            float u0 = frame / 8f;
            float u1 = (frame + 1) / 8f;

            ShaderHandler.HudVertexAttribList.AddRange(new float[]
            {
                x1, x2, y1, y2,
                0f,
                u0, 0f, u1, 1f
            });
        }
        else
        {
            _runningHudAnimTime = 0f;
        }

        //Layer 1 - Sword
        ShaderHandler.HudVertexAttribList.AddRange(new float[]
        {
            x1, x2, y1, y2,
            1f,
            0f, 0f, 1f, 1f
        });

        //Layer 2 - Empty layer (future shield)
        ShaderHandler.HudVertexAttribList.AddRange(new float[]
        {
            x1, x2, y1, y2,
            -1f,
            0f, 0f, 1f, 1f
        });

        //Layer 3 - Empty layer (future hp/stamina etc.)
        ShaderHandler.HudVertexAttribList.AddRange(new float[]
        {
            x1, x2, y1, y2,
            -1f,
            0f, 0f, 1f, 1f
        });

        //Layer 4 - Container
        ShaderHandler.HudVertexAttribList.AddRange(new float[]
        {
            x1, x2, y1, y2,
            3f,
            0f, 0f, 1f, 1f
        });
    }
}
