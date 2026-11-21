using System;

public enum CustomerType { Normal, VIP, Critic }
public enum ReputationRank { Bronze, Silver, Gold, CosmicDiamond }

public static class ServiceMath
{
    /// <summary>Drains the customer's twenty-point patience budget.</summary>
    public static float Patience(float current, CustomerType type, float seconds)
    {
        if (!float.IsFinite(current) || !float.IsFinite(seconds) || seconds < 0f) throw new ArgumentOutOfRangeException(nameof(seconds));
        return Math.Max(0f, current - (type == CustomerType.VIP ? 1.8f : type == CustomerType.Critic ? 1.5f : 1f) * seconds);
    }
    /// <summary>Calculates an integer tip using strict patience thresholds and recipe bonuses.</summary>
    public static int Tip(int price, float patienceFraction, int multiplierPercent = 100)
    {
        if (!float.IsFinite(patienceFraction)) throw new ArgumentOutOfRangeException(nameof(patienceFraction));
        double rate = patienceFraction > 0.8f ? 0.30 : patienceFraction > 0.5f ? 0.15 : patienceFraction > 0.2f ? 0.05 : 0;
        return (int)Math.Min(int.MaxValue, Math.Floor(Math.Max(0, price) * rate * Math.Max(0, multiplierPercent) / 100d));
    }
    /// <summary>Maps reputation points to the four star ranks.</summary>
    public static ReputationRank Rank(int points) => points >= 300 ? ReputationRank.CosmicDiamond : points >= 100 ? ReputationRank.Gold : points >= 30 ? ReputationRank.Silver : ReputationRank.Bronze;
    /// <summary>Returns the reputation award for a satisfied customer.</summary>
    public static int Reputation(CustomerType type) => type == CustomerType.Critic ? 5 : type == CustomerType.VIP ? 3 : 1;
    /// <summary>Returns rank-unlocked seating capacity.</summary>
    public static int Seats(int points) => points >= 100 ? 8 : points >= 30 ? 6 : 4;
}
