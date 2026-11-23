using System;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.ExampleCollab;

/// <summary>
/// 合集里到处要用的小数学。别在各张图的实体里各写一份。
/// </summary>
public static class CollabMath
{
    public static float Approach(float value, float target, float maxDelta)
    {
        if (value < target) return Math.Min(value + maxDelta, target);
        return Math.Max(value - maxDelta, target);
    }

    public static Vector2 Approach(Vector2 value, Vector2 target, float maxDelta)
    {
        Vector2 delta = target - value;
        float length = delta.Length();
        if (length <= maxDelta || length == 0f) return target;
        return value + delta / length * maxDelta;
    }

    public static float EaseInOut(float t)
    {
        t = MathHelper.Clamp(t, 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    /// <summary>把角度差折算到 -PI..PI，用于转向。</summary>
    public static float DeltaAngle(float from, float to)
    {
        float delta = (to - from) % MathHelper.TwoPi;
        if (delta > MathHelper.Pi) delta -= MathHelper.TwoPi;
        if (delta < -MathHelper.Pi) delta += MathHelper.TwoPi;
        return delta;
    }
}
