using System;

public static class DamageSystem
{
    /// <summary>Resolves scaled damage against armor, with a one-point minimum and overflow saturation.</summary>
    public static int CalculateDamage(int baseDamage, float attackerMultiplier, int targetArmor)
    {
        if (!float.IsFinite(attackerMultiplier) || attackerMultiplier < 0f)
            throw new ArgumentOutOfRangeException(nameof(attackerMultiplier));
        double damage = Math.Max(0, baseDamage) * (double)attackerMultiplier - Math.Max(0, targetArmor);
        return damage >= int.MaxValue ? int.MaxValue : (int)Math.Max(1d, Math.Floor(damage));
    }
}
