using System;

namespace Sources;

public static class MathX {
    //Default math variables
    public const float Quadrant1 = (float)Math.PI / 2;
    public const float Quadrant2 = (float)Math.PI;
    public const float Quadrant3 = ((float)Math.PI * 3) / 2;
    public const float Quadrant4 = (float)Math.PI* 2;

    public static float Hypotenuse(float a, float b) {
        return (float)(Math.Sqrt(a * a + b * b));
    }

    public static float Clamp(float num, float min, float max)
    {
        return Math.Min(max, Math.Max(min, num));
    }
}