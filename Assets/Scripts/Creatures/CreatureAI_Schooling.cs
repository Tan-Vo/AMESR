using UnityEngine;

public sealed class CreatureAI_Schooling : CreatureBase
{
    /// <summary>Accepts flock velocity only while free to swim.</summary>
    public void ApplyFlockVelocity(Vector2 velocity)
    {
        if (IsAlive && State != CreatureState.Stunned) Body.linearVelocity = velocity;
    }
    /// <inheritdoc />
    public override void UpdateBehavior() { }
}
