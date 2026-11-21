using System;

public static class OxygenMath
{
    /// <summary>Calculates oxygen units consumed per second at the given depth.</summary>
    public static float DrainRate(float depth, bool boosting)
    {
        if (float.IsNaN(depth) || float.IsInfinity(depth)) throw new ArgumentOutOfRangeException(nameof(depth));
        return (0.45f + 0.75f * (float)Math.Pow(Math.Max(0f, depth) / 2500f, 1.5)) * (boosting ? 3.2f : 1f);
    }
}
