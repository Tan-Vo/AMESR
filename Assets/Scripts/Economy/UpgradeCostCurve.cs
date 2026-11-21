using System;

public static class UpgradeCostCurve
{
    /// <summary>Prices the next level; currentLevel zero purchases level one.</summary>
    public static int Calculate(int baseCost, double gamma, int currentLevel)
    {
        if (baseCost < 0 || currentLevel < 0 || !double.IsFinite(gamma) || gamma < 1d) throw new ArgumentOutOfRangeException(nameof(currentLevel));
        if (baseCost == 0) return 0;
        double value = baseCost * Math.Pow(gamma, currentLevel);
        return value >= int.MaxValue ? int.MaxValue : (int)Math.Round(value, MidpointRounding.AwayFromZero);
    }
}
