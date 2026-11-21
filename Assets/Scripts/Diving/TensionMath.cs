using System;

public static class TensionMath
{
    /// <summary>Integrates normalized cable tension; resting relieves tension, tugging and tapping increase it.</summary>
    public static float Step(float tension, bool reeling, bool tugging, bool tapped, float deltaTime)
    {
        if (!float.IsFinite(tension) || !float.IsFinite(deltaTime) || deltaTime < 0f)
            throw new ArgumentOutOfRangeException(nameof(deltaTime));
        return Math.Clamp(tension + ((reeling ? 0.12f : -0.3f) + (tugging ? 0.45f : 0f)) * deltaTime + (tapped ? 0.025f : 0f), 0f, 1f);
    }
    /// <summary>Calculates mass-dependent reel speed, rewarding the 40–70% sweet spot.</summary>
    public static float ReelSpeed(float speed, float playerMass, float creatureMass, float tension, bool tapped)
    {
        if (!float.IsFinite(speed) || !float.IsFinite(playerMass) || !float.IsFinite(creatureMass) || !float.IsFinite(tension))
            throw new ArgumentOutOfRangeException(nameof(speed));
        float mass = Math.Max(0.01f, playerMass);
        return Math.Max(0f, speed) * mass / (mass + Math.Max(0f, creatureMass)) *
            (tension >= 0.4f && tension <= 0.7f ? 1.6f : 1f) * (tapped ? 1.4f : 1f);
    }
}
