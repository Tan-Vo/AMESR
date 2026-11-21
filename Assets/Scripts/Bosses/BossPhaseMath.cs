using System;

public static class BossPhaseMath
{
    /// <summary>Maps health to phases, including exact 66% and 33% boundaries.</summary>
    public static int Phase(int currentHP, int maxHP)
    {
        if (maxHP <= 0) throw new ArgumentOutOfRangeException(nameof(maxHP));
        long hp = Math.Clamp(currentHP, 0, maxHP);
        return hp * 100 <= maxHP * 33L ? 3 : hp * 100 <= maxHP * 66L ? 2 : 1;
    }
}
