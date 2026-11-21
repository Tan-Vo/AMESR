using System;

public static class TraumaMath
{
    /// <summary>Decays clamped trauma and returns a finite value.</summary>
    public static float Decay(float trauma, float seconds, float rate)
    {
        if (!float.IsFinite(trauma) || !float.IsFinite(seconds) || !float.IsFinite(rate) || seconds < 0 || rate < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        return Math.Clamp(trauma - seconds * rate, 0f, 1f);
    }
    /// <summary>Maps trauma to quadratic shake amplitude.</summary>
    public static float Amplitude(float trauma) { float value = Math.Clamp(trauma, 0f, 1f); return value * value; }
}
