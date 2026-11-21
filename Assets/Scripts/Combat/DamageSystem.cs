using System;

public static class DamageSystem
{
    /// <summary>Returns effective healing without reviving, wasting supplies, or overflowing health.</summary>
    public static int HealingAmount(int current, int maximum, int requested) =>
        current <= 0 || current >= maximum || requested <= 0 ? 0 : (int)Math.Min((long)maximum - current, requested);
    /// <summary>Resolves scaled damage against armor, with a one-point minimum and overflow saturation.</summary>
    public static int CalculateDamage(int baseDamage, float attackerMultiplier, int targetArmor)
    {
        if (!float.IsFinite(attackerMultiplier) || attackerMultiplier < 0f)
            throw new ArgumentOutOfRangeException(nameof(attackerMultiplier));
        double damage = Math.Max(0, baseDamage) * (double)attackerMultiplier - Math.Max(0, targetArmor);
        return damage >= int.MaxValue ? int.MaxValue : (int)Math.Max(1d, Math.Floor(damage));
    }
}
