using UnityEngine;

public static class Extensions
{
    /// <summary>Rotates a vector counterclockwise in degrees.</summary>
    public static Vector2 Rotate(this Vector2 value, float degrees)
    {
        float r = degrees * Mathf.Deg2Rad;
        return new Vector2(value.x * Mathf.Cos(r) - value.y * Mathf.Sin(r), value.x * Mathf.Sin(r) + value.y * Mathf.Cos(r));
    }
    /// <summary>Returns a vector with a replaced X coordinate.</summary>
    public static Vector2 WithX(this Vector2 value, float x) => new Vector2(x, value.y);
    /// <summary>Returns a vector with a replaced Y coordinate.</summary>
    public static Vector2 WithY(this Vector2 value, float y) => new Vector2(value.x, y);
    /// <summary>Finds the nearest non-null target, or null for an empty collection.</summary>
    public static Transform GetClosest(this Transform origin, Transform[] targets)
    {
        Transform closest = null;
        float distance = float.PositiveInfinity;
        if (origin == null || targets == null) return null;
        foreach (var target in targets)
        {
            if (target == null) continue;
            float candidate = (target.position - origin.position).sqrMagnitude;
            if (candidate < distance) { closest = target; distance = candidate; }
        }
        return closest;
    }
    /// <summary>Maps a number between ranges; degenerate input ranges return the output minimum.</summary>
    public static float Remap(this float value, float fromMin, float fromMax, float toMin, float toMax) =>
        Mathf.Approximately(fromMin, fromMax) ? toMin : toMin + (value - fromMin) / (fromMax - fromMin) * (toMax - toMin);
    /// <summary>Hashes a numeric animator parameter name.</summary>
    public static int ToAnimHash(this int value) => Animator.StringToHash(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
}
