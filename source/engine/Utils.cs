using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;

namespace Sources;

internal class Utils
{
    public static void SetViewport(int width, int height)
    {
        GL.Viewport(0, 0, width, height);
    }

    public static float NormalizeAngle(float angle)
    {
        if (angle > MathX.Quadrant4)
        {
            angle -= MathX.Quadrant4;
        }

        else if (angle < 0)
        {
            angle += MathX.Quadrant4;
        }

        return angle;
    }
}
