using System;

public static class PhaseClockMath
{
    /// <summary>Normalizes a phase countdown; zero-duration phases finish immediately.</summary>
    public static float Progress(float remaining, float duration)
    {
        if (!float.IsFinite(remaining) || !float.IsFinite(duration)) throw new ArgumentOutOfRangeException(nameof(duration));
        return duration <= 0f ? 1f : Math.Clamp(1f - remaining / duration, 0f, 1f);
    }
    /// <summary>Maps phase progress to its complete in-game time window.</summary>
    public static float Hour(float start, float end, float progress) => start + (end - start) * Math.Clamp(progress, 0f, 1f);
}
