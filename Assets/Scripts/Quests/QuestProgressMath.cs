using System;

public static class QuestProgressMath
{
    /// <summary>Accumulates counters or a maximum-depth/level milestone, bounded by the goal.</summary>
    public static int Advance(int current, int amount, int required, bool milestone)
    {
        if (current < 0 || amount < 0 || required < 1) throw new ArgumentOutOfRangeException(nameof(amount));
        return (int)Math.Min(required, milestone ? Math.Max(current, amount) : (long)current + amount);
    }
}
