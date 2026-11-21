using System;

public static class WeightedSelection
{
    /// <summary>Selects an index from positive integer weights using a sample in [0,1); returns -1 for no mass.</summary>
    public static int SelectIndex(ReadOnlySpan<int> weights, double sample)
    {
        Validate(sample);
        long total = 0;
        for (int i = 0; i < weights.Length; i++) if (weights[i] > 0) total += weights[i];
        if (total == 0) return -1;
        double target = sample * total;
        long sum = 0;
        for (int i = 0; i < weights.Length; i++)
        {
            if (weights[i] <= 0) continue;
            sum += weights[i];
            if (target < sum) return i;
        }
        return -1;
    }
    /// <summary>Selects from finite positive float weights; invalid weights contribute no probability.</summary>
    public static int SelectIndex(ReadOnlySpan<float> weights, double sample)
    {
        Validate(sample);
        double total = 0;
        for (int i = 0; i < weights.Length; i++) if (Usable(weights[i])) total += weights[i];
        if (total <= 0) return -1;
        double target = sample * total;
        double sum = 0;
        for (int i = 0; i < weights.Length; i++)
        {
            if (!Usable(weights[i])) continue;
            sum += weights[i];
            if (target < sum) return i;
        }
        return -1;
    }
    private static bool Usable(float value) => value > 0 && !float.IsNaN(value) && !float.IsInfinity(value);
    private static void Validate(double sample)
    {
        if (double.IsNaN(sample) || sample < 0 || sample >= 1) throw new ArgumentOutOfRangeException(nameof(sample));
    }
}
