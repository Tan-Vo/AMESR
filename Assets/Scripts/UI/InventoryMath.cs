using System;

public static class InventoryMath
{
    /// <summary>Returns how many units fit into an existing stack.</summary>
    public static int Transfer(int source, int destination, int maximum)
    {
        if (source < 0 || destination < 0 || maximum < 1) throw new ArgumentOutOfRangeException(nameof(maximum));
        return Math.Min(source, Math.Max(0, maximum - destination));
    }
}
